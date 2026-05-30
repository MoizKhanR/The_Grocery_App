namespace The_Grocery_App;

public partial class RecipePage : ContentPage
{
	public RecipePage()
	{
		InitializeComponent();
	}

    //COMMANDS
    private async void OnListsClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }
}