namespace The_Grocery_App.Controls;

public partial class ProBanner : ContentView
{
    public ProBanner()
    {
        InitializeComponent();

        Loaded += On_Loaded;
        Unloaded += On_Unloaded;
    }

    void On_Loaded(object? sender, EventArgs e)
    {
        Apply_Premium_State();
        Global.OnPremiumChanged -= On_Premium_Changed; // guard double-subscribe
        Global.OnPremiumChanged += On_Premium_Changed;
    }

    void On_Unloaded(object? sender, EventArgs e)
        => Global.OnPremiumChanged -= On_Premium_Changed;

    void On_Premium_Changed(bool isPremium)
        => MainThread.BeginInvokeOnMainThread(Apply_Premium_State);

    void Apply_Premium_State()
    {
        // Hide the whole banner the moment PRO is owned
        IsVisible = !Global.Is_Premium;
    }

    async void On_Banner_Tapped(object? sender, TappedEventArgs e)
    {
        var The_Page = Window?.Page as ContentPage
                       ?? Shell.Current?.CurrentPage as ContentPage;

        if (The_Page != null)
        {
            await ProPrompt.Show(The_Page);
        }
    }
}