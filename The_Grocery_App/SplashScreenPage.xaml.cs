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
								
		//Application.Current.MainPage = new AppShell();

		if(Application.Current?.Windows.Count > 0) 
		{
			Application.Current.Windows[0].Page = new AppShell();
        }
    }
}