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
		await Task.Delay(2500); // Simulate loading time
								
		//Open Main Page after 2.5 seconds of displaying the SplashScreenPage

		if(Application.Current?.Windows.Count > 0) 
		{
			Application.Current.Windows[0].Page = new AppShell();
        }
    }
}