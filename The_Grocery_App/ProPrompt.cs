using The_Grocery_App;

public static class ProPrompt
{
    public static async Task Show(Page page)
    {
        string The_Choice = await page.DisplayActionSheetAsync(
            "Remove Ads and get unlimited Recipe Prompts!",
            "Cancel", null,
            "Unlock PRO", "Restore Purchase");

        if (The_Choice == "Unlock PRO")
        {
            bool ok = await BillingService.Buy_Remove_Ads();
            await page.DisplayAlertAsync(ok ? "Welcome to PRO 🎉" : "Error",
                ok ? "Ads gone, recipes unlimited." : "Purchase didn't go through.", "OK");
        }
        else if (The_Choice == "Restore Purchase")
        {
            bool ok = await BillingService.Restore_Remove_Ads();
            await page.DisplayAlertAsync(ok ? "Restored 🎉" : "Nothing to restore",
                ok ? "PRO's back." : "No previous purchase found.", "OK");
        }

    }
}