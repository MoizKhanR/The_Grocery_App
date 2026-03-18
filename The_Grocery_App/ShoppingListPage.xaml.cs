namespace The_Grocery_App;

public partial class ShoppingListPage : ContentPage
{
	public ShoppingListPage()
	{
		InitializeComponent();
	}

    private async void OnSearchBarFocused(object sender, FocusEventArgs e)
    {
    //    ((SearchBar)sender).Unfocus(); //No keyboard needed, we will navigate to the AddItemPage
    //    await Shell.Current.GoToAsync(nameof(AddItemPage));
    }
}