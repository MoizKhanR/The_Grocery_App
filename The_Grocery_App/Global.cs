using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static List<string> Recipe_List_Names { get; set; } = new List<string>(); // This is the list of recipe list names that will be used to populate the UI. It can be loaded from an XML file or created from scratch.
        public static List<UIShoppingItem> Master_List { get; set; } = new List<UIShoppingItem>(); // Will contain a list of all items as well as the category they belong to
        public static List<CategoryGroup> Grouped_Items_Master_List { get; set; } = new List<CategoryGroup>(); // Will contain a list of all categories and the items that belong to them (Shopping List Page will use this to display the items in a grouped manner)
                                                                                                               //public static string? Selected_Shopping_List { get; set; } = string.Empty; // This is the selected shopping list name that will be used to name the XML of that shopping list after adding items to it.
        private static string? _selected_Shopping_List;
        public static string? Selected_Shopping_List
        {
            get => _selected_Shopping_List;
            set
            {
                if (_selected_Shopping_List != value)
                {
                    _selected_Shopping_List = value;
                    OnShoppingListChanged?.Invoke(value);
                }
            }
        }

        private static string? _selected_Recipe_List;
        public static string? Selected_Recipe_List
        {
            get => _selected_Recipe_List;
            set
            {
                if (_selected_Recipe_List != value)
                {
                    _selected_Recipe_List = value;
                    OnRecipeListChanged?.Invoke(value);
                }
            }
        }
        // The Event Handler to Detect Change
        public static event Action<string?>? OnShoppingListChanged;
        public static event Action<string?>? OnRecipeListChanged;

        //Track  the page you came from
        public static string Came_From { get; set; } = string.Empty;

        //Premium Status
        public static bool Is_Premium
        {
            get => Preferences.Default.Get(nameof(Is_Premium), false);
            set
            {
                if (Is_Premium != value)
                {
                    Preferences.Default.Set(nameof(Is_Premium), value);
                    OnPremiumChanged?.Invoke(value);
                }
            }
        }

        //Premium Status Change Event Handler
        public static event Action<bool>? OnPremiumChanged;

        //Premium Trial variables

        public const int Free_Recipes_Per_Day = 3;

        public static int Recipes_Used_Today
        {
            get => Preferences.Default.Get(nameof(Recipes_Used_Today), 0);
            set => Preferences.Default.Set(nameof(Recipes_Used_Today), value);
        }
        public static string Recipes_Reset_Day
        {
            get => Preferences.Default.Get(nameof(Recipes_Reset_Day), "");
            set => Preferences.Default.Set(nameof(Recipes_Reset_Day), value);
        }

        public static bool Can_Generate_Recipe()
        {
            if (Is_Premium)
            {
                return true;
            }

            string today = DateTime.Now.ToString("yyyy-MM-dd");

            //Reset if new day
            if (Recipes_Reset_Day != today) 
            { 
                Recipes_Reset_Day = today; Recipes_Used_Today = 0; 
            } 

            return Recipes_Used_Today < Free_Recipes_Per_Day;
        }

        public static void Register_Recipe_Generation()
        {
            if (!Is_Premium)
            {
                Recipes_Used_Today++;
            }
        }

        //Guard Rails 
        public static bool Is_Navigating = false; //Prevent multiple navigations at the same time, which can cause issues with the navigation stack and UI behavior.

        public static bool Master_Loaded = false; //Do not reload master list upon navigating to the main page
    }
}
