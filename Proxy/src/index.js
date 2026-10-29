// Mama's Grocery proxy (Cloudflare Worker)
//
// The app sends:   POST /recipe   { recipe, catalog, device }   + header X-App-Token
// We answer with:  { items: [ { name, category } ] }
//
// Why this exists:
//  1) The Gemini key lives HERE (as a Cloudflare secret) - never inside the app, so nobody can steal it again.
//  2) The prompt lives HERE too - the key can only ever be used to build grocery lists, nothing else.
//  3) Daily limits per phone + for the whole app, so a script or a bug can never run up a bill.

export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        if (request.method !== "POST" || url.pathname !== "/recipe") {
            return Json({ error: "not_found" }, 404);
        }

        //Only our app knows this token (it's a speed bump, the daily limits are the real protection)
        if (request.headers.get("X-App-Token") !== env.APP_TOKEN) {
            return Json({ error: "unauthorized" }, 401);
        }

        //Read + clean what the app sent
        let body;
        try {
            body = await request.json();
        } catch {
            return Json({ error: "bad_request" }, 400);
        }

        const recipe = String(body.recipe ?? "").trim().slice(0, 300);
        const catalog = String(body.catalog ?? "").trim().slice(0, 60000);   //The whole catalog is ~35k characters
        const device = String(body.device ?? "").slice(0, 64) || request.headers.get("CF-Connecting-IP") || "unknown";

        if (recipe === "" || catalog === "") {
            return Json({ error: "bad_request" }, 400);
        }

        //Daily limits (one counter for the whole app)
        const counter = env.COUNTER.get(env.COUNTER.idFromName("usage"));
        const check = await (await counter.fetch("https://counter/check", {
            method: "POST",
            body: JSON.stringify({ device, action: "check" })
        })).json();

        if (!check.ok) {
            return Json({ error: "busy" }, 429);
        }

        //Ask Gemini (main model, main again after a short pause, then the fallback)
        const prompt = Build_Prompt(recipe, catalog);
        const attempts = [env.GEMINI_MODEL, env.GEMINI_MODEL, env.GEMINI_FALLBACK_MODEL];
        let was_busy = false;

        for (let i = 0; i < attempts.length; i++) {
            const model = attempts[i];
            if (i === 1) await new Promise(resolve => setTimeout(resolve, 1200));

            const result = await Ask_Gemini(model, prompt, env.GEMINI_KEY);

            if (result.ok) {
                //Only answers count towards the limits
                await counter.fetch("https://counter/add", {
                    method: "POST",
                    body: JSON.stringify({ device, action: "add" })
                });

                return Json(result.data, 200);
            }

            if (result.busy) was_busy = true;
            console.log(`${model} failed: ${result.error}`);
        }

        return was_busy ? Json({ error: "busy" }, 503) : Json({ error: "ai_failed" }, 502);
    }
};

//Same rules the app used to send - now nobody can change them
function Build_Prompt(recipe, catalog) {
    return `You are a grocery list assistant inside a grocery shopping app.
The user wants to cook: "${recipe}"

Here is the store's grocery catalog grouped by category (Category: item, item, ...):
${catalog}

Rules:
- Work out the ingredients needed for that dish, as accurately as possible.
- Every item you return MUST use a "name" and "category" copied EXACTLY from the catalog above.
- Search the catalog thoroughly. If the user says "chicken breast" and the catalog has "Chicken Breasts", use "Chicken Breasts".
- If an ingredient isn't in the catalog, use the closest real match - or skip it. Never invent items that aren't in the catalog.
  Example: if the catalog has "Milk" but no "Yogurt", skip yogurt. If the user wants a burger with shaami kabab and the catalog has "Shaami Kabab", include it.
- The user may write in any language or mix languages (Roman Urdu, Spanish, Arabic...). Understand it, but always return catalog names.
- Only answer cooking / food requests. If the request isn't about cooking or groceries, return an empty list.
- No duplicates.

Return ONLY a valid JSON array, no markdown, no explanation. Example:
[{"name":"Chicken Breasts","category":"Meat And Seafood"},{"name":"Yogurt","category":"Dairy And Eggs"}]`;
}

async function Ask_Gemini(model, prompt, key) {
    try {
        //Pasted keys sometimes carry a stray space or line break - clean it
        const clean_key = String(key ?? "").trim();

        const request_body = JSON.stringify({
            contents: [{ role: "user", parts: [{ text: prompt }] }],
            generationConfig: {
                temperature: 0.2,
                responseMimeType: "application/json",
                maxOutputTokens: 4096
            }
        });

        //Key in the header first (Google's recommended way)...
        let response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent`, {
            method: "POST",
            headers: { "Content-Type": "application/json", "x-goog-api-key": clean_key },
            body: request_body
        });

        //...and if Google says "invalid key", try the ?key= style (some key types only work that way)
        if (response.status === 400) {
            response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${encodeURIComponent(clean_key)}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: request_body
            });
        }

        if (!response.ok) {
            //Keep Google's own message - it says exactly what's wrong (bad key, bad model name...)
            const details = (await response.text()).replace(/\s+/g, " ").slice(0, 300);
            return { ok: false, busy: response.status === 429 || response.status === 503, error: `HTTP ${response.status} ${details}` };
        }

        const data = await response.json();
        const reply = (data?.candidates?.[0]?.content?.parts ?? []).map(part => part.text ?? "").join("");

        //Cut out anything before the first [ and after the last ] (just in case)
        const first = reply.indexOf("[");
        const last = reply.lastIndexOf("]");
        if (first < 0 || last <= first) return { ok: false, error: "reply wasn't a JSON array" };

        const parsed = JSON.parse(reply.slice(first, last + 1));

        //Only pass back clean rows, no duplicates
        const seen = new Set();
        const items = (Array.isArray(parsed) ? parsed : [])
            .filter(item => item && typeof item.name === "string" && typeof item.category === "string")
            .map(item => ({ name: item.name.trim().slice(0, 80), category: item.category.trim().slice(0, 60) }))
            .filter(item => item.name !== "" && item.category !== "")
            .filter(item => {
                const key = item.name.toLowerCase();
                if (seen.has(key)) return false;
                seen.add(key);
                return true;
            })
            .slice(0, 60);

        return { ok: true, data: { items } };
    } catch (error) {
        return { ok: false, error: String(error) };
    }
}

function Json(data, status) {
    return new Response(JSON.stringify(data), { status, headers: { "Content-Type": "application/json" } });
}

//Usage counter: today's count for the whole app + for each phone
//Lives in one Durable Object so every request sees the same numbers. Wipes itself every new day (UTC).
export class Usage_Counter {
    constructor(state, env) {
        this.state = state;
        this.env = env;
    }

    async fetch(request) {
        const { device, action } = await request.json();
        const storage = this.state.storage;
        const today = new Date().toISOString().slice(0, 10);

        //New day? Start fresh
        if ((await storage.get("day")) !== today) {
            await storage.deleteAll();
            await storage.put("day", today);
        }

        const app_total = (await storage.get("app")) ?? 0;
        const device_key = `device:${device}`;
        const device_total = (await storage.get(device_key)) ?? 0;

        if (app_total >= Number(this.env.GLOBAL_DAILY_LIMIT)) return Response.json({ ok: false, reason: "app" });
        if (device_total >= Number(this.env.DEVICE_DAILY_LIMIT)) return Response.json({ ok: false, reason: "device" });

        //"check" = just look, "add" = an answer was delivered, count it
        if (action === "add") {
            await storage.put({ app: app_total + 1, [device_key]: device_total + 1 });
        }

        return Response.json({ ok: true });
    }
}
