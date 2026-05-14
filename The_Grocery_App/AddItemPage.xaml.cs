using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Linq;

namespace The_Grocery_App;

public partial class AddItemPage : ContentPage, INotifyPropertyChanged
{
    //Set the observable collection with a backing field to notify the UI when the collection changes
    private ObservableCollection<UIShoppingItem> _searched_results { get; set; } = new();
    public ObservableCollection<UIShoppingItem> Searched_Results
    {
        get => _searched_results;
        set
        {
            _searched_results = value;
            OnPropertyChanged(nameof(Searched_Results)); // Notify the UI
        }
    }

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

            foreach(Item item in Global.The_Master_Catalog.Categories.SelectMany(Category => Category.Items))
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

    private async void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            Searched_Results.Clear(); //Clear previous search results

            if(e.NewTextValue == null || e.NewTextValue.Length == 0)
            {
                return; //If the search bar is empty, do not perform any search
            }

            //Get the first character of the whatever the user types & that will be the KEY to search against
            char First_Letter = char.ToUpper(e.NewTextValue[0]);                   

            //Perform Deep search in the background to find items that contain the search term anywhere in their name, not just at the beginning
            
            _ = Deep_Search_Catalog(e.NewTextValue);
          
        }
        catch (Exception E)
        {
            await DisplayAlertAsync("Error", $"Failed to perform search: {E.Message}", "OK");
        }
	}

    public async Task Deep_Search_Catalog(string Search_Term)
    {
        try
        {
            List<UIShoppingItem> Deep_Results = new List<UIShoppingItem>();

            int Limiter = 0;

            await Task.Run(() =>
            {
                char[] Alphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

                for (int Key_Index = 0; Key_Index < Alphabets.Length; Key_Index++)
                {
                    if (Items_By_Letter.ContainsKey(Alphabets[Key_Index]))
                    {
                        foreach (Item item in Items_By_Letter[Alphabets[Key_Index]])
                        {
                            if (item.ItemName.Split(' ').Any(word => word.StartsWith(Search_Term, StringComparison.OrdinalIgnoreCase)))
                            {
                                Deep_Results.Add(new UIShoppingItem { UI_Name = item.ItemName, Category_Name = item.ItemCategory });
                                Limiter++;
                            }
                            if(Limiter >= 30) //Limit the number of search results to prevent performance issues
                            {
                                break;
                            }

                            
                        }
                    }
                }
            });

            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Searched_Results = new ObservableCollection<UIShoppingItem>(Deep_Results);
                OnPropertyChanged(nameof(Searched_Results));
            });
        }
        catch (Exception E)
        {
            await DisplayAlertAsync("Error", $"Failed to perform deep search: {E.Message}", "OK");
        }
    }
}