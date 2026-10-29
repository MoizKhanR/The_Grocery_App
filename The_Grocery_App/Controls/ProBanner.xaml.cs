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

        if (IsVisible) _ = Play_Shine();
    }

    //A single glint across the gold - eye-catching without being annoying
    async Task Play_Shine()
    {
        try
        {
            await Task.Delay(650);
            double The_Width = Banner_Border.Width;
            if (The_Width <= 0) return;

            Shine.TranslationX = -90;
            Shine.Opacity = 1;
            await Shine.TranslateToAsync(The_Width + 20, 0, 900, Easing.CubicInOut);
            Shine.Opacity = 0;

            await Crown_Label.ScaleToAsync(1.18, 140, Easing.CubicOut);
            await Crown_Label.ScaleToAsync(1.0, 220, Easing.SpringOut);
        }
        catch
        {
            Shine.Opacity = 0;
            Crown_Label.Scale = 1;
        }
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

        //Little press feedback before the prompt opens
        try
        {
            await Banner_Border.ScaleToAsync(0.97, 80, Easing.CubicOut);
            await Banner_Border.ScaleToAsync(1.0, 160, Easing.SpringOut);
        }
        catch { Banner_Border.Scale = 1; }

        if (The_Page != null)
        {
            await ProPrompt.Show(The_Page);
        }
    }
}