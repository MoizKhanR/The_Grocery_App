using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace The_Grocery_App
{
    public static class Global
    {
        public static XDocument? The_Doc; // This is the XDocument that will be used to store the catalog data in memory. It can be loaded from an XML file or created from scratch.
        public static XElement? The_Catalog; // This is the XElement that will be used to store the catalog data in memory. It can be loaded from an XML file or created from scratch.
        public static Grocery The_Master_Catalog { get; set; } = new Grocery(); // This is the master catalog that will be used to store the catalog data in memory. It can be loaded from an XML file or created from scratch.
        public static List<string> Shopping_List_Names { get; set; } = new List<string>(); // This is the list of shopping list names that will be used to populate the UI. It can be loaded from an XML file or created from scratch.
        public static List<UIShoppingItem> Master_List { get; set; } = new List<UIShoppingItem>(); // Will contain a list of all items as well as the category they belong to
        public static List<CategoryGroup> Grouped_Items_Master_List { get; set; } = new List<CategoryGroup>(); // Will contain a list of all categories and the items that belong to them (Shopping List Page will use this to display the items in a grouped manner)
        public static string? Selected_Shopping_List { get; set; } = string.Empty; // This is the selected shopping list name that will be used to name the XML of that shopping list after adding items to it.
    }
}
