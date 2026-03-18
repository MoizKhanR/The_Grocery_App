namespace The_Grocery_App;

public partial class ShoppingListPage : ContentPage
{
	public ShoppingListPage()
	{
		InitializeComponent();
	}

    private async void OnSearchBarFocused(object sender, EventArgs e)
    {
        try
        {
            //((SearchBar)sender).Unfocus(); //No keyboard needed, we will navigate to the AddItemPage
            await Shell.Current.GoToAsync(nameof(AddItemPage));
        } catch(Exception E) {
            await DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
        }
    }
}