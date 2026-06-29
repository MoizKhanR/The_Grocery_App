using Plugin.InAppBilling;

namespace The_Grocery_App; // same namespace as ProPrompt/ProToolbar so they all see each other

public static class BillingService
{
    const string Remove_Ads_Id = "remove_ads"; // must match your Play Console product ID

    public static async Task<bool> Buy_Remove_Ads()
    {
        var The_Billing = CrossInAppBilling.Current;
        try
        {
            if (!await The_Billing.ConnectAsync()) return false;

            var The_Purchase = await The_Billing.PurchaseAsync(Remove_Ads_Id, ItemType.InAppPurchase);

            if (The_Purchase == null) return false; // user backed out

            if (The_Purchase.State == PurchaseState.Purchased)
            {
                // REQUIRED on Android — acknowledge or Google auto-refunds in 3 days
                await The_Billing.FinalizePurchaseAsync(new[] { The_Purchase.TransactionIdentifier }); // exact arg: trust IntelliSense
                Global.Is_Premium = true;
                return true;
            }
            return false; // PaymentPending etc.
        }
        catch (Exception E)
        {
            System.Diagnostics.Debug.WriteLine(E.Message); // already-owned, unavailable...
            return false;
        }
        finally 
        {
            try
            {
                if (The_Billing.IsConnected)
                {
                    await The_Billing.DisconnectAsync();
                }
            }
            catch { }
        }
    }

    public static async Task<bool> Restore_Remove_Ads()
    {
        var The_Billing = CrossInAppBilling.Current;
        try
        {
            if (!await The_Billing.ConnectAsync()) return false;

            var The_Purchases = await The_Billing.GetPurchasesAsync(ItemType.InAppPurchase);
            bool The_Owner = The_Purchases?.Any(p => p.ProductId == Remove_Ads_Id
                                         && p.State == PurchaseState.Purchased) ?? false;
            Global.Is_Premium = The_Owner;
            return The_Owner;
        }
        catch (Exception E)
        {
            System.Diagnostics.Debug.WriteLine(E.Message);
            return false;
        }
        finally 
        {
            try
            {
                if (The_Billing.IsConnected)
                {
                    await The_Billing.DisconnectAsync();
                }
            }
            catch { }
        }
    }
}