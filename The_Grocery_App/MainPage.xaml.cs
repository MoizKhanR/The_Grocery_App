using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace The_Grocery_App
{
    
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<UIShoppingItem> UI_Grocery_List_Names { get; set; } = new();
        public ObservableCollection<UIShoppingItem> UI_Recipe_List_Names { get; set; } = new();
        public string List_Name { get; set; } = string.Empty;

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
                
                Global.The_Doc = XDocument.Load(The_Stream); //The entire XML is loaded
                Global.The_Master_Catalog = Populate_Grocery_Database(Global.The_Doc); //XML stored inside memory variable
                Global.Master_List = Populate_Item_Master_List(); //UIShoppingItem Master List is populated (used for searching items when adding to shopping lists)

                //Save_ShoppingList_Names_XML(UI_Grocery_List_Names);
                //Populate Headers (add exceptions so that they are not duplicated when navigating back and forth between pages)
                if (UI_Grocery_List_Names.Any(x => x.UI_Name == "Shopping Lists") is false &&
                    UI_Recipe_List_Names.Any(x => x.UI_Name == "Recipes") is false)
                {
                    UI_Grocery_List_Names.Add(new UIShoppingItem { UI_Name = "Shopping Lists", BackColor = Colors.ForestGreen, Picker_Visibility = false });
                    UI_Recipe_List_Names.Add(new UIShoppingItem { UI_Name = "Recipes", BackColor = Colors.ForestGreen, Picker_Visibility = false });
                }

                //Load the Shopping List from the XML file (if it exists)
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"ShoppingLists.xml");

                if (File.Exists(File_Path))
                {
                    Load_ShoppingList_Names_XML(File_Path, UI_Grocery_List_Names);
                    //await DisplayAlertAsync("Test", "OnAppearing fired!", "OK");
                }
            }
            catch (Exception ex) 
            {
                await DisplayAlertAsync("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async void OnCounterClicked(object? sender, EventArgs e)
        {
            try
            {
                //Acquire the name of the shopping list from the entry field, if it is NULL, return

                if (Shopping_List_Name_Entry.Text == null || Shopping_List_Name_Entry.Text.Trim() == "")
                {
                    await DisplayAlertAsync("Invalid Name", "Please enter a valid name for the shopping list.", "OK");
                    return;
                }

                string Shopping_List_Name = Shopping_List_Name_Entry.Text; //Name acquired

                //Populate shopping names List
                Global.Shopping_List_Names.Add(Shopping_List_Name); //Name added to the memory variable

                UI_Grocery_List_Names.Add(new UIShoppingItem
                {
                    UI_Name = Shopping_List_Name,
                    Picker_Visibility = false
                }); //Name added to the observable collection, which will automatically update the UI
             

                //Generate the XML
                Save_ShoppingList_Names_XML(UI_Grocery_List_Names); //Generate the XML file for the shopping list names (to be used for saving and loading)

                //await DisplayAlertAsync("Alert", $"XML was successfully generated.", "OK");

                Shopping_List_Name_Entry.Text = ""; //Clear the entry field after adding the shopping list name

                //int count = Global.The_Master_Catalog.Categories[5].Items[1].Sizes.Count;          

                
                //Display the action sheet to the user
                //string action = await DisplayActionSheetAsync("Choose an option", "Cancel", null, "Add List", "Add Recipe");

                //Handle the user's choice

                //if (action == "Add List")
                //{
                //    await Shell.Current.GoToAsync(nameof(ShoppingListPage));
                //}

                SemanticScreenReader.Announce(CounterBtn.Text);
            }
            catch(Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to add shopping list: {ex.Message}", "OK");
            }
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
                            Items_Catalog.ItemCategory = Category;

                            //These will be referenced, hence should be a new object each iteration
                            List<string?> Item_Qualities = new List<string?>(Get_Item_Qualities(The_Doc, Item_Name));
                            List<string?> Item_Sizes = new List<string?>(Get_Item_Sizes(The_Doc, Item_Name));

                            //Insert the respective lists into the Items Catalog
                            Items_Catalog.Sizes = Item_Sizes;
                            Items_Catalog.Qualities = Item_Qualities;
                        }

                        //Add the Item Catalog to list of items inside the Categories Catalog
                        Category_Catalog.Items.Add(Items_Catalog);                     
                    }

                    //Add the Category Catalog to list of Categories inside the Master Catalog
                    The_Master_Catalog.Categories.Add(Category_Catalog);
                }
            }       

            return The_Master_Catalog;
        }

        //Generate a SAVE file for the shopping list NAMES in XML format
        public void Save_ShoppingList_Names_XML(ObservableCollection<UIShoppingItem> Shopping_List_Names)
        {
            try
            {
                XDocument Save_Doc = new XDocument(
                        new XElement("ShoppingLists", 
                            Shopping_List_Names.Where(item => item.UI_Name != null 
                                                      && item.UI_Name != "Shopping Lists").Select(item => 
                                new XElement("ShoppingList",
                                  new XAttribute("name", item.UI_Name!)
                                  )
                                )
                            )
                        );

                //Save the XML document to a file
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"ShoppingLists.xml");
                //Shell.Current.CurrentPage.DisplayAlertAsync("XML",Save_Doc.ToString(), "OK");
                Save_Doc.Save(File_Path);

                //Shell.Current.CurrentPage.DisplayAlertAsync("Message", $"File Path: {File_Path}", "OK");
                //Shell.Current.CurrentPage.DisplayAlertAsync("Message", $"File Exists: {File.Exists(File_Path)}", "OK");

            }
            catch (Exception E)
            {
                Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to save data: {E.Message}", "OK");
            }
        }

        public void Load_ShoppingList_Names_XML(string File_Path, ObservableCollection<UIShoppingItem> The_Observable_List)
        {
            try
            {
                XDocument? Load_Doc = XDocument.Load(File_Path);

                var Doc_Root = Load_Doc?.Root;

                if (Doc_Root is null)
                {
                    // nothing to load or malformed document
                    return;
                }

                foreach(var Element in Doc_Root.Elements("ShoppingList"))
                {
                    string? Name = Element.Attribute("name")?.Value;

                    if(Name != null && !The_Observable_List.Any(item => item.UI_Name == Name))
                    {
                        The_Observable_List.Add(new UIShoppingItem { UI_Name = Name, Picker_Visibility = false });
                    }
                }


            }
            catch (Exception E)
            {
                Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
            }
        }

        //Populate the UIShoppingItem Master List

        public List<UIShoppingItem> Populate_Item_Master_List()
        {
            List<UIShoppingItem> The_Master_List = new List<UIShoppingItem>();

            foreach(var Category in Global.The_Master_Catalog.Categories)
            {
                foreach(var Item in Category.Items)
                {              
                    The_Master_List.Add(new UIShoppingItem
                    {
                        Category_Name = Category.Name,
                        UI_Name = Item.ItemName,
                        UI_Sizes = Item.Sizes.Where(size => size != null).Select(size => size!).ToList(),
                        UI_Quantities = Item.Qualities.Where(quality => quality != null).Select(quality => quality!).ToList(),
                        Picker_Visibility = false,
                        BackColor = Colors.White
                    });
                }
            }

            return The_Master_List;
        }
    }
}
