using System.Collections.ObjectModel;
using System.ComponentModel;

namespace The_Grocery_App;

[QueryProperty(nameof(Item_Name), "ItemName")]
[QueryProperty(nameof(Item_Category), "ItemCategory")]
public partial class ShoppingListPage : ContentPage, INotifyPropertyChanged
{
    private string _item_Category = string.Empty;
    public string Item_Category
    {
        get => _item_Category;
        set
        {
            _item_Category = value;
            Add_Item_To_List(); // fires after BOTH values are set
        }
    }

    //public string Item_Category { get; set; } = string.Empty;
    public string Item_Name { get; set; } = string.Empty;

    //public ObservableCollection<CategoryGroup> Grouped_Items_Observable { get; set; } = new();

    private ObservableCollection<CategoryGroup> _grouped_Items_observable = new();
    public ObservableCollection<CategoryGroup> Grouped_Items_Observable
    {
        get => _grouped_Items_observable;
        set
        {
            _grouped_Items_observable = value;
            OnPropertyChanged(nameof(Grouped_Items_Observable)); // notify the UI
        }
    }
    public ShoppingListPage()
	{
		InitializeComponent();
        BindingContext = this;
    }

    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();
    //    Add_Item_To_List();
    //}
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

    public void Add_Item_To_List()
    {
        if (string.IsNullOrEmpty(Item_Name) || string.IsNullOrEmpty(Item_Category)) return;
        
        //Search the Master List for the Category Group, if it doesn't exist, create it and add it to the Master List
        var Group = Global.Grouped_Items_Master_List.FirstOrDefault(g => g.Category_Name == Item_Category);

        
        if (Group == null)
        {
            
            Group = new CategoryGroup(Item_Category);
            Global.Grouped_Items_Master_List.Add(Group);
        }

        //Now check to see if the Item_Name already exists in the Group, if it does, don't add it again
        bool? Item_Exists = Group?.Any(item => item.UI_Name == Item_Name) ?? false;

        if (Item_Exists is false)
        {
            Group.Add(new UIShoppingItem
            {
                UI_Name = Item_Name,
                Category_Name = Item_Category
            });
        }

        //Update the Grouped_Items_Observable collection to reflect the changes in the Master List
        Grouped_Items_Observable = new ObservableCollection<CategoryGroup>(Global.Grouped_Items_Master_List);

    }

    //Test

    //public void Add_Item_To_List()
    //{
    //    //if (string.IsNullOrEmpty(Item_Name) || string.IsNullOrEmpty(Item_Category)) return;

    //    //Search the Master List for the Category Group, if it doesn't exist, create it and add it to the Master List
    //    var Group = Grouped_Items_Observable.FirstOrDefault(g => g.Category_Name == Item_Category);

    //    if (Group == null)
    //    {
    //        Group = new CategoryGroup(Item_Category);
    //        Grouped_Items_Observable.Add(Group);
    //    }

    //    Group.Add(new UIShoppingItem
    //    {
    //        UI_Name = Item_Name,
    //        Category_Name = Item_Category
    //    });

    //    //Update the Grouped_Items_Observable collection to reflect the changes in the Master List
    //    //Grouped_Items_Observable = new ObservableCollection<CategoryGroup>(Global.Grouped_Items_Master_List);

    //}
}