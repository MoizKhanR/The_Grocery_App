namespace The_Grocery_App.Controls;

public partial class AdBanner : ContentView
{
	public AdBanner()
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
        bool Show_Ads = !Global.Is_Premium;
        IsVisible = Show_Ads;

        if (Show_Ads)
        {
            Banner.LoadAd();
        }
    }
}