using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace The_Grocery_App
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<UIShoppingItem> UI_Grocery_List_Names { get; set; } = new();
        public ObservableCollection<UIShoppingItem> UI_Recipe_List_Names { get; set; } = new();

        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected async override void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                using var The_Stream = await FileSystem.OpenAppPackageFileAsync("GroceryCatalog.xml");
                
                Globals.The_Doc = XDocument.Load(The_Stream); //The entire XML is loaded
                Globals.The_Master_Catalog = Populate_Grocery_Database(Globals.The_Doc); //XML stored inside memory variable


                //Populate Headers (add exceptions so that they are not duplicated when navigating back and forth between pages)
                if (UI_Grocery_List_Names.Any(x => x.UI_Name == "Shopping Lists") is false &&
                    UI_Recipe_List_Names.Any(x => x.UI_Name == "Recipes") is false)
                {
                    UI_Grocery_List_Names.Add(new UIShoppingItem { UI_Name = "Shopping Lists", BackColor = Colors.LawnGreen, Picker_Visibility = false });
                    UI_Recipe_List_Names.Add(new UIShoppingItem { UI_Name = "Recipes", BackColor = Colors.LawnGreen, Picker_Visibility = false });
                }
            }
            catch (Exception ex) 
            {
                await DisplayAlertAsync("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async void OnCounterClicked(object? sender, EventArgs e)
        {

            //int count = Globals.The_Master_Catalog.Categories[5].Category_Items[1].Sizes.Count;          

            //if (Globals.The_Doc != null)
            //{
            //    List<string?> Testes = Get_Item_Qualities(Globals.The_Doc, "Quinoa");

            //    await DisplayAlertAsync("Items Count", count.ToString(), "OK");               
            //}  
            
            
            List<string> list = new List<string>();
            list.AddRange("YES",
                              "NO",
                              "MAYBE",
                              "YOOO",
                              "ALNA",
                              "KULU",
                              "BIG",
                              "NOTORIOUS",
                              "Solise",
                              "Pineapple");

            UI_Grocery_List_Names.Add(new UIShoppingItem { UI_Name = "Item #" + count.ToString(), UI_Sizes = list });
            count = count + 1;

            //Display the action sheet to the user
            //string action = await DisplayActionSheetAsync("Choose an option", "Cancel", null, "Add List", "Add Recipe");

            //Handle the user's choice

            //if (action == "Add List")
            //{
            //    await Shell.Current.GoToAsync(nameof(ShoppingListPage));
            //}


            SemanticScreenReader.Announce(CounterBtn.Text);
        }


        //Get ALL Categories from the XDocument
        public List<string?> Get_Category_Names(XDocument The_Doc)
        {
            List<string?> Cats = new List<string?>(); //List of Categories
            XElement? The_Catalog = The_Doc.Root; //Extract elements from the XDocument (Entire XML)
            var Categories = The_Catalog?.Elements("Category"); //Extract the Categories Elements

            //Loop through the elements and get Category names
            foreach(var Category in Categories ?? Enumerable.Empty<XElement>())
            {
                Cats.Add(Category.Attribute("name")?.Value);
            }

            return Cats;
        }

        //Get Items under a specific Category
        public List<string?> Get_Item_Names(XDocument The_Doc, string Category_Name)
        {
            List<string?> Items_List = new List<string?>();

            XElement? The_Catalog = The_Doc.Root;
            var The_Category = The_Catalog?.Elements("Category").FirstOrDefault(c => c.Attribute("name")?.Value == Category_Name);

            if (The_Category != null)
            {
                var Items_Catalog = The_Category.Elements("Item");

                foreach (var Item in Items_Catalog)
                {
                    Items_List.Add(Item.Attribute("name")?.Value);
                }
            }

            return Items_List;
        }

        //Read ALL Sizes of a specific Item

        public List<string?> Get_Item_Sizes(XDocument The_Doc, string Item_Name)
        {
            List<string?> Item_Sizes_List = new List<string?>();
            XElement? The_Catalog = The_Doc.Root;

            var The_Item = The_Catalog?.Elements("Category")
                                       .Elements("Item").FirstOrDefault(i => i.Attribute("name")?.Value == Item_Name);

            if (The_Item != null)
            {
                var Item_Sizes_Catalog = The_Item.Elements("Sizes").Elements("Size");
                
                foreach(var Item in Item_Sizes_Catalog)
                {
                    Item_Sizes_List.Add(Item.Attribute("value")?.Value);
                }
               

               
            }

            return Item_Sizes_List;
        }

        //Read ALL Qualities of a specific Item

        public List<string?> Get_Item_Qualities(XDocument The_Doc, string Item_Name)
        {
            List<string?> Item_Qualities_List = new List<string?>();
            XElement? The_Catalog = The_Doc.Root;

            var The_Item = The_Catalog?.Elements("Category")
                                       .Elements("Item").FirstOrDefault(i => i.Attribute("name")?.Value == Item_Name);

            if (The_Item != null)
            {
                var Item_Qualities_Catalog = The_Item.Elements("Qualities").Elements("Quality");

                foreach (var Item in Item_Qualities_Catalog)
                {
                    Item_Qualities_List.Add(Item.Attribute("value")?.Value);
                }
            }

            return Item_Qualities_List;
        }


        //A powerful function that populates the memory variable with the entire grocery catalog
        public Grocery Populate_Grocery_Database(XDocument The_Doc)
        {
            Grocery The_Master_Catalog = new Grocery();
            
            List<string?> Category_Names = new List<string?>();
            List<string?> Item_Names = new List<string?>();    

            //Acquire the data and populate The Master Catalog

            //Category Names
            Category_Names = Get_Category_Names(The_Doc);

            //Traverse through the values that have been read and populate the Master Catalog accordingly

            foreach (string? Category in Category_Names)
            {
                Category Category_Catalog = new Category(); //MUST be a new object each iteration (will be referenced)

                if (Category != null)
                {                   
                    Category_Catalog.Name = Category;
                    Item_Names = Get_Item_Names(The_Doc, Category);

                    foreach(string? Item_Name in Item_Names)
                    {
                        Item Items_Catalog = new Item(); //MUST be a new object each iteration (will be referenced)

                        if (Item_Name != null)
                        {
                            Items_Catalog.ItemName = Item_Name;
                            //These will be referenced, hence should be a new object each iteration
                            List<string?> Item_Qualities = new List<string?>(Get_Item_Qualities(The_Doc, Item_Name));
                            List<string?> Item_Sizes = new List<string?>(Get_Item_Sizes(The_Doc, Item_Name));

                            //Insert the respective lists into the Items Catalog
                            Items_Catalog.Sizes = Item_Sizes;
                            Items_Catalog.Qualities = Item_Qualities;
                        }

                        //Add the Item Catalog to list of items inside the Categories Catalog
                        Category_Catalog.Category_Items.Add(Items_Catalog);                     
                    }

                    //Add the Category Catalog to list of Categories inside the Master Catalog
                    The_Master_Catalog.Categories.Add(Category_Catalog);
                }
            }       

            return The_Master_Catalog;
        }

        //Global Variables
        public static class Globals
        {
            public static XDocument? The_Doc;
            public static XElement? The_Catalog;
            public static Grocery The_Master_Catalog { get; set; } = new Grocery();
            public static ObservableCollection<Grocery> The_Master_Catalog_Observable { get; set; } = new ObservableCollection<Grocery>();
        }
    }
}
