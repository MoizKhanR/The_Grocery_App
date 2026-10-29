namespace The_Grocery_App
{
    public static class HelpInfo
    {
        const string The_Help_Text =
                                    "Welcome to Mama's Grocery! 🛒\n\n" +
                                    "LISTS\n" +
                                    "• Type a name up top, tap Add, pick Shopping List or Recipe\n" +
                                    "• Tap a list to open it\n\n" +
                                    "ADDING ITEMS\n" +
                                    "• Inside a list, hit \"Add Something...\" and search\n" +
                                    "• Tap an item to add it, then set quantity, size and quality if you like\n" +
                                    "• Can't find something? Type it and press the RETURN key - it's added under \"Uncategorized\"\n" +
                                    "• Check the box once it's in your cart\n\n" +
                                    "DELETING\n" +
                                    "• Swipe any item or list to the left and tap \"Delete\"\n\n" +
                                    "SMART RECIPES ✨\n" +
                                    "• Open a recipe, describe what you're after, tap \"Go\"\n" +
                                    "• We build the ingredient list for you\n" +
                                    "• Free users get 3 a day\n" +
                                    "• You can also add items manually, hit \"Add Something...\" and search\n\n" +
                                    "Go PRO 👑\n" +
                                    "• One unlock kills the ads and lifts the recipe limit for good\n" +
                                    "• Tap the gold banner whenever you're ready\n\n" +
                                    "Happy shopping!";

        public static Task Show(Page page)
        {
            return page.DisplayAlertAsync("How to use Mama's Grocery", The_Help_Text, "Got it");
        }
    }
}