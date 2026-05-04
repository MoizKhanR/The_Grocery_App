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

        public void Generate_GroceryList_XML(string List_Name, List<UIShoppingItem> Items)
        {
            try
            {
                //Generate a SAVE file for the shopping list in XML format

                XDocument Save_Doc = new XDocument(
                        new XElement("ShoppingList",
                            new XAttribute("name", List_Name),
                        Items.Select(item =>
                            new XElement("Item",
                                new XAttribute("name", item.UI_Name ?? ""),
                                new XAttribute("size", item.Selected_Size ?? ""),
                                new XAttribute("quantity", item.Selected_Quantity ?? "")
                            )
                        )
                    )
                );

                //Save the XML document to a file
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{List_Name}.xml");
                Save_Doc.Save( File_Path );
            }
            catch (Exception E) 
            {
                Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to save data: {E.Message}", "OK");
            }
        }

        //Load the saved data from an XML file and populate the shopping list
        public void Load_GroceryList_From_XML(string File_Path)
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

                //Load a shopping list from an XML file
                var Items = Doc_Root.Elements("Item").Select(e => new UIShoppingItem
                {
                    UI_Name = e.Attribute("name")?.Value,
                    Selected_Size = e.Attribute("size")?.Value,
                    Selected_Quantity = e.Attribute("quantity")?.Value
                });
            } 
            catch(Exception E) 
            {
                Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
            }
        }

        //Main Page's Tap Command - Navigate to the Shopping List Page with the selected item
        public ICommand Main_Tap_Command => new Command(async () =>
        {
            try
            {
                //string Page_Name = Shell.Current.CurrentPage.GetType().Name;
                if (UI_Name != "Shopping Lists" && UI_Name != "Recipes")
                {
                    await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?Selected_Item={UI_Name}");
                }                          
            }
            catch { }
        });

        //AddItemPage's Tap Command - Navigate to the Shopping List Page with the selected item and category
        public ICommand AddItem_Tap_Command => new Command(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?ItemName={UI_Name}&ItemCategory={Category_Name}");

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
