namespace The_Grocery_App.Controls;

public partial class AdBanner : ContentView
{
	public AdBanner()
	{
		InitializeComponent();

        Loaded += (_, _) =>
        {
            bool Show_Ads = !Global.Is_Premium;
            IsVisible = Show_Ads;

            if (Show_Ads)
            {
                Banner.LoadAd();
            }
        };
    }
}