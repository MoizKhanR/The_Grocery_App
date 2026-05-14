using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Xml.Linq;

namespace The_Grocery_App
{
    public class UIShoppingItem
    {
        public string? Category_Name { get; set; }
        public string? UI_Name { get; set; }
        public List<string> UI_Sizes { get; set; } = new List<string>();
        public List<string> UI_Quantities { get; set; } = new List<string>();
        public string? Selected_Size { get; set; }
        public string? Selected_Quantity { get; set; }
        public bool Picker_Visibility { get; set; } = true;
        public Color BackColor { get; set; } = Colors.Transparent;


        //Main Page's Tap Command - Navigate to the Shopping List Page with the selected item
        public ICommand Main_Tap_Command => new Command(async () =>
        {
            try
            {
                //string Page_Name = Shell.Current.CurrentPage.GetType().Name;
                if (UI_Name != "Shopping Lists" && UI_Name != "Recipes")
                {
                    //Set the currently selected shopping list
                    Global.Selected_Shopping_List = UI_Name;

                    //Navigate to the shopping list page with the selected item as a query parameter
                    await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?Selected_Item={UI_Name}");
                }

                //Load the respective XML
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Shopping_List}_GroceryList.xml");
                ShoppingListPage? Shopping_List_Page = Shell.Current.CurrentPage as ShoppingListPage;

                if (File.Exists(File_Path) && Shopping_List_Page is not null)
                {
                    Shopping_List_Page.Grouped_Items_Observable.Clear(); // Clear the existing items in the observable collection before loading new items from the XML
                    Shopping_List_Page.Load_GroceryList_XML(File_Path, Shopping_List_Page.Grouped_Items_Observable);
                
                }

                //Update the Master List in the Global class via the Observable Collection
                Global.Grouped_Items_Master_List = Shopping_List_Page!.Grouped_Items_Observable.ToList();

            }
            catch { }
        });

        //AddItemPage's Tap Command - Navigate to the Shopping List Page with the selected item and category
        public ICommand AddItem_Tap_Command => new Command(async () =>
        {
            try
            {
                //Navigate to the shopping list page with the selected item and category as query parameters
                await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?ItemName={UI_Name}&ItemCategory={Category_Name}");

                //Load the respective XML
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Shopping_List}_GroceryList.xml");
                ShoppingListPage? Shopping_List_Page = Shell.Current.CurrentPage as ShoppingListPage;

                if (File.Exists(File_Path) && Shopping_List_Page is not null)
                {
                    Shopping_List_Page.Grouped_Items_Observable.Clear(); // Clear the existing items in the observable collection before loading new items from the XML
                    Shopping_List_Page.Load_GroceryList_XML(File_Path, Shopping_List_Page.Grouped_Items_Observable);
                
                }

                //Update the Master List in the Global class via the Observable Collection
                Global.Grouped_Items_Master_List = Shopping_List_Page!.Grouped_Items_Observable.ToList();

            } catch { }
        });
          
        //Delete an item from the shopping list
        public ICommand Delete_Command => new Command(async () =>
        {
            //Delete "this" (It works because we are in the UIShoppingItem class)
            MainPage? Main_Page = Shell.Current.CurrentPage as MainPage;
            Main_Page?.UI_Grocery_List_Names.Remove(this);

            //Save the new XML
            Main_Page?.Save_ShoppingList_Names_XML(Main_Page.UI_Grocery_List_Names);
        });
    }
}
