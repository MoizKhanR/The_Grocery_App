namespace The_Grocery_App;

public partial class SplashScreenPage : ContentPage
{
	public SplashScreenPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
	{
		base.OnAppearing();

		await Task.Delay(3500); // Simulate loading time
								
		//Open Main Page after 2.5 seconds of displaying the SplashScreenPage

		//Soft fade + tiny zoom out of the splash instead of a hard cut
		try
		{
			await Task.WhenAll(
				Splash_Image.FadeToAsync(0, 350, Easing.CubicIn),
				Splash_Image.ScaleToAsync(1.06, 350, Easing.CubicIn));
		}
		catch { }

		if(Application.Current?.Windows.Count > 0) 
		{
			//First launch (or first launch after this update): show the walkthrough, then the app
			Application.Current.Windows[0].Page = OnboardingPage.Has_Been_Seen
				? new AppShell()
				: new OnboardingPage(First_Run: true);
        }
    }
}