using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Xml.Linq;

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
        BindingContext = this;
        InitializeComponent();        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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

        if (Item_Exists is false && Group is not null)
        {
            Group.Add(new UIShoppingItem
            {
                UI_Name = Item_Name,
                Category_Name = Item_Category
            });
        }

        //Update the Grouped_Items_Observable collection to reflect the changes in the Master List
        Grouped_Items_Observable = new ObservableCollection<CategoryGroup>(Global.Grouped_Items_Master_List);

        //Save the XML
        Save_GroceryList_XML(Grouped_Items_Observable);
    }

    //Save the list & name it as Global.Selected_Shopping_List + "_GroceryList" + ".xml"
    public void Save_GroceryList_XML(ObservableCollection<CategoryGroup> Grouped_Items_Observable)
    {
        try
        {
            /*  Grouped_Items_Observable.Select(group =>  // group = CategoryGroup
                group.Select(item =>                  // item = UIShoppingItem
                    item.UI_Name                      // UIShoppingItem HAS UI_Name
                                 )
                            ) */

            XDocument Save_Doc = new XDocument(
                    new XElement("GroceryList",
                        Grouped_Items_Observable.Select(group =>
                            new XElement("Category",
                                new XAttribute("name", group.Category_Name ?? ""),
                                group.Select(item =>
                                    new XElement("Item",
                                        new XAttribute("name", item.UI_Name ?? ""),
                                        new XAttribute("size", item.Selected_Size ?? ""),
                                        new XAttribute("quantity", item.Selected_Quantity ?? "")
                                    )
                                )
                            )
                        )
                    )
                );

            //Save the XML document to a file
            string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Shopping_List}_GroceryList.xml");
            
            Save_Doc.Save(File_Path);
           
        }
        catch (Exception E)
        {
            Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to save data: {E.Message}", "OK");
        }
    }

    public void Load_GroceryList_XML(string File_Path, ObservableCollection<CategoryGroup> Grouped_Items_Observable)
    {
        try
        {
            XDocument? Load_Doc = XDocument.Load(File_Path);
            var Doc_Root = Load_Doc?.Root;

            if (Doc_Root is null) return;

            foreach (var Category_Element in Doc_Root.Elements("Category"))
            {
                string? Category_Name = Category_Element.Attribute("name")?.Value;

                if (Category_Name == null) continue;

                // Find or create the group
                var Group = Grouped_Items_Observable.FirstOrDefault(g => g.Category_Name == Category_Name);

                if (Group == null)
                {
                    Group = new CategoryGroup(Category_Name);
                    Grouped_Items_Observable.Add(Group);
                }

                // Add items to the group
                foreach (var Item_Element in Category_Element.Elements("Item"))
                {
                    Group.Add(new UIShoppingItem
                    {
                        UI_Name = Item_Element.Attribute("name")?.Value,
                        Selected_Size = Item_Element.Attribute("size")?.Value,
                        Selected_Quantity = Item_Element.Attribute("quantity")?.Value,
                        Category_Name = Category_Name
                    });
                }
            }
        }
        catch (Exception E)
        {
            Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
        }
    }

    //COMMANDS
    private async void OnListsClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.Navigation.PopToRootAsync();
        return true;
    }
}