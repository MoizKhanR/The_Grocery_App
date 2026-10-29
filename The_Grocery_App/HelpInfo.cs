namespace The_Grocery_App
{
    //The "?" button on every page opens the same swipeable walkthrough new users see on first launch
    public static class HelpInfo
    {
        public static Task Show(Page page)
        {
            return page.Navigation.PushModalAsync(new OnboardingPage(First_Run: false));
        }
    }
}
