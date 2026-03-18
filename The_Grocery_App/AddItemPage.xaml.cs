using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace The_Grocery_App;

public partial class AddItemPage : ContentPage
{
    public ObservableCollection<UIShoppingItem> Searched_Results { get; set; } = new();
    Dictionary<char, List<Item>> Items_By_Letter = new(); //A list whose indexes are basically characters,
                                                          //and each index contains a list of items that start with that character.
                                                          //This is for efficient searching.
    public AddItemPage()
	{
		InitializeComponent();
        BindingContext = this;
    }

    protected async override void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            //Populate the list of items by letter for efficient searching

            foreach(Item item in Global.The_Master_Catalog.Categories.SelectMany(category => category.Items))
            {
                char First_Letter = char.ToUpper(item.ItemName[0]);

                //If a list with a specific Index (KEY) does not exist, create a new list at that index (KEY)
                if (!Items_By_Letter.ContainsKey(First_Letter))
                {
                    Items_By_Letter[First_Letter] = new List<Item>();
                }

                //Add the item to the list at the index (KEY) corresponding to its first letter
                Items_By_Letter[First_Letter].Add(item);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load data: {ex.Message}", "OK");
        }
    }

    //Happens when the user clicks on the search bar
    private async void SearchBarEngine(object sender, FocusEventArgs e)
    {
        try
        {
            //((SearchBar)sender).Unfocus(); //No keyboard needed, we will navigate to the AddItemPage
            //await Shell.Current.GoToAsync(nameof(AddItemPage));
            
            Items_Search_Bar.ClearValue(SearchBar.TextProperty); //Clear the search bar after the user clicks on it
            
        }
        catch (Exception E)
        {
            await DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
        }
    }

    //Algorithm for Search Results are generated as the user types in the search bar

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            Searched_Results.Clear(); //Clear previous search results

            //Get the first character of the whatever the user types & that will be the KEY to search against
            char First_Letter = char.ToUpper(e.NewTextValue[0]);

            if(Items_By_Letter.ContainsKey(First_Letter))
            {
                //Search only through the list of items that start with the same letter as the user's input
                foreach (Item item in Items_By_Letter[First_Letter])
                {
                    if (item.ItemName.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))
                    {
                        Searched_Results.Add(new UIShoppingItem { UI_Name = item.ItemName });
                    }
                }
            }        

        }
        catch (Exception E)
        {
            DisplayAlertAsync("Error", $"Failed to perform search: {E.Message}", "OK");
        }
    }
}