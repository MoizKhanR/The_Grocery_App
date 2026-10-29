namespace The_Grocery_App.Controls;

public partial class AdBanner : ContentView
{
	public AdBanner()
	{
		InitializeComponent();

        //Real banner IDs per platform (set here - OnPlatform inside the ad control's XAML breaks the build)
#if ANDROID
        Banner.AdsId = "ca-app-pub-3337756787254419/9049320629";
#elif IOS
        Banner.AdsId = "ca-app-pub-3337756787254419/4827080567";
#endif

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

#if SCREENSHOTS
        Show_Ads = false; //Store screenshot builds only (-p:Screenshots=true). Release builds never set this.
#endif

        IsVisible = Show_Ads;

        if (Show_Ads)
        {
            Banner.LoadAd();
        }
    }
}