
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Xml.Linq;
 
namespace The_Grocery_App
{
    public class UIShoppingItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string? Category_Name { get; set; }
        public string? UI_Name { get; set; }

        private ObservableCollection<string> _ui_Sizes = new();
        public ObservableCollection<string> UI_Sizes
        {
            get => _ui_Sizes;
            set { _ui_Sizes = value; OnPropertyChanged(); OnPropertyChanged(nameof(Size_Index)); }
        }

        private ObservableCollection<string> _ui_Qualities = new();
        public ObservableCollection<string> UI_Qualities
        {
            get => _ui_Qualities;
            set { _ui_Qualities = value; OnPropertyChanged(); OnPropertyChanged(nameof(Quality_Index)); }
        }

        private ObservableCollection<string> _ui_Quantity =
            new ObservableCollection<string>(Enumerable.Range(1, 100).Select(n => n.ToString()));

        public ObservableCollection<string> UI_Quantity
        {
            get => _ui_Quantity;
            set { _ui_Quantity = value; OnPropertyChanged(); }
        }

        // ---- Size (index-based binding; survives CollectionView recycle) ----
        private string? _selected_Size;
        public string? Selected_Size
        {
            get => _selected_Size;
            set { _selected_Size = value; OnPropertyChanged(); OnPropertyChanged(nameof(Size_Index)); }
        }

        public int Size_Index
        {
            get => (Selected_Size != null) ? UI_Sizes.IndexOf(Selected_Size) : -1;
            set
            {
                Selected_Size = (value >= 0 && value < UI_Sizes.Count) ? UI_Sizes[value] : null;
                OnPropertyChanged();
            }
        }

        // ---- Quality (index-based binding; survives CollectionView recycle) ----
        private string? _selected_Quality;
        public string? Selected_Quality
        {
            get => _selected_Quality;
            set { _selected_Quality = value; OnPropertyChanged(); OnPropertyChanged(nameof(Quality_Index)); }
        }

        public int Quality_Index
        {
            get => (Selected_Quality != null) ? UI_Qualities.IndexOf(Selected_Quality) : -1;
            set
            {
                Selected_Quality = (value >= 0 && value < UI_Qualities.Count) ? UI_Qualities[value] : null;
                OnPropertyChanged();
            }
        }

        // ---- Quantity (unchanged; static 1-100 list always matches) ----
        private string? _selected_Quantity;
        public string? Selected_Quantity
        {
            get => _selected_Quantity;
            set { _selected_Quantity = value; OnPropertyChanged(); }
        }

        public bool Picker_Visibility { get; set; } = true;
        public Color BackColor { get; set; } = Colors.Transparent;
        public FontAttributes Item_Font_Attributes { get; set; } = FontAttributes.None;


        //Text Decorations
        private bool _is_bought;
        public bool Pickers_Enabled => !Is_Bought;
        public bool Is_Bought
        {
            get => _is_bought;
            set
            {
                _is_bought = value;
                OnPropertyChanged();                        //Indicates to the UI "Is_Bought changed"
                OnPropertyChanged(nameof(Item_Decoration)); //Indicates to the UI "the strikethrough changed too"
                OnPropertyChanged(nameof(Pickers_Enabled)); //Disable the Pickers when the item is bought
            }
        }

        public TextDecorations Item_Decoration =>
            Is_Bought ? TextDecorations.Strikethrough : TextDecorations.None;


        //Main Page's Tap Command - Navigate to the Shopping List Page with the selected item
        public ICommand Main_ShoppingList_Tap_Command => new Command(async () =>
        {
            if (Global.Is_Navigating)
            {
                return;
            }

            //Ignore taps while navigating
            Global.Is_Navigating = true;

            try
            {
                if (UI_Name != "Shopping Lists")
                {
                    Global.Selected_Shopping_List = UI_Name;
                    await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?Selected_Item={UI_Name}");
                }

                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Shopping_List}_GroceryList.xml");
                ShoppingListPage? Shopping_List_Page = Shell.Current.CurrentPage as ShoppingListPage;

                if (File.Exists(File_Path) && Shopping_List_Page is not null)
                {
                    Shopping_List_Page.Load_GroceryList_XML(File_Path, Shopping_List_Page.Grouped_Items_Observable);
                }

                if (Shopping_List_Page is not null)
                    Global.Grouped_Items_Master_List = Shopping_List_Page.Grouped_Items_Observable.ToList();
            }
            catch { }
            finally
            {
                Global.Is_Navigating = false;   // release guard even if it throws
            }
        });

        //Main Page's Tap Command - Navigate to the Recipes Page with the selected item
        public ICommand Main_Recipes_Tap_Command => new Command(async () =>
        {
            if (Global.Is_Navigating)
            {
                return;
            }

            //Ignore taps while navigating
            Global.Is_Navigating = true;

            try
            {
                //string Page_Name = Shell.Current.CurrentPage.GetType().Name;
                if (UI_Name != "Recipes")
                {
                    //Set the currently selected recipe list
                    Global.Selected_Recipe_List = UI_Name;

                    //Navigate to the shopping list page with the selected item as a query parameter
                    await Shell.Current.GoToAsync($"{nameof(RecipePage)}?Selected_Item={UI_Name}");
                }

                //Load the respective XML
                string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Recipe_List}_RecipeList.xml");
                RecipePage? Recipe_Page = Shell.Current.CurrentPage as RecipePage;

                if (File.Exists(File_Path) && Recipe_Page is not null)
                {
                    //Recipe_Page.Grouped_Items_Observable.Clear(); // Clear the existing items in the observable collection before loading new items from the XML
                    Recipe_Page.Load_RecipeList_XML(File_Path, Recipe_Page.Grouped_Items_Observable);

                }

                //Update the Master List in the Global class via the Observable Collection
                Global.Grouped_Items_Master_List = Recipe_Page!.Grouped_Items_Observable.ToList();
            }
            catch (Exception E)
            {
                //await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Error", E.Message, "OK");
            }
            finally
            {
                Global.Is_Navigating = false; //Release the guard
            }
        });

        //AddItemPage's Tap Command - Navigate to the Shopping List Page with the selected item and category
        public ICommand AddItem_Tap_Command => new Command(async () =>
        {
            try
            {
                if (Global.Came_From == nameof(ShoppingListPage))
                {
                    //Navigate to the shopping list page with the selected item and category as query parameters
                    await Shell.Current.GoToAsync($"{nameof(ShoppingListPage)}?ItemName={UI_Name}&ItemCategory={Category_Name}");

                    //Load the respective XML
                    string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Shopping_List}_GroceryList.xml");
                    ShoppingListPage? Shopping_List_Page = Shell.Current.CurrentPage as ShoppingListPage;
                    //Shopping_List_Page!.Load_GroceryList_XML(File_Path, Shopping_List_Page.Grouped_Items_Observable);

                    //Update the Master List in the Global class via the Observable Collection
                    Global.Grouped_Items_Master_List = Shopping_List_Page!.Grouped_Items_Observable.ToList();
                }

                if (Global.Came_From == nameof(RecipePage))
                {
                    //Navigate to the shopping list page with the selected item and category as query parameters
                    await Shell.Current.GoToAsync($"{nameof(RecipePage)}?ItemName={UI_Name}&ItemCategory={Category_Name}");

                    //Load the respective XML
                    string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Recipe_List}_RecipeList.xml");
                    RecipePage? Recipe_Page = Shell.Current.CurrentPage as RecipePage;
                    //Recipe_Page!.Load_RecipeList_XML(File_Path, Recipe_Page.Grouped_Items_Observable);

                    //Update the Master List in the Global class via the Observable Collection
                    Global.Grouped_Items_Master_List = Recipe_Page!.Grouped_Items_Observable.ToList();
                }
            }
            catch { }
        });

        //Delete an item from the shopping list
        public ICommand Main_Shopping_Delete_Command => new Command(async () =>
        {
            //Delete "this" (It works because we are in the UIShoppingItem class)
            MainPage? Main_Page = Shell.Current.CurrentPage as MainPage;
            Main_Page?.UI_Grocery_List_Names.Remove(this);

            //Delete the respective XML (GroceryList)
            string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{this.UI_Name}_GroceryList.xml");

            if (File.Exists(File_Path))
            {
                File.Delete(File_Path);
                //await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Success", "List deleted.", "OK");
            }

            //Save the new XML
            Main_Page?.Save_ShoppingList_Names_XML(Main_Page.UI_Grocery_List_Names);
        });

        //Delete an item from the recipe list
        public ICommand Main_Recipe_Delete_Command => new Command(async () =>
        {
            //Delete "this" (It works because we are in the UIShoppingItem class)
            MainPage? Main_Page = Shell.Current.CurrentPage as MainPage;
            Main_Page?.UI_Recipe_List_Names.Remove(this);

            //Delete the respective XML (RecipeList)
            string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{this.UI_Name}_RecipeList.xml");

            if (File.Exists(File_Path))
            {
                File.Delete(File_Path);
                //await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Success", "Recipe deleted.", "OK");
            }

            //Save the new XML
            Main_Page?.Save_RecipeList_Names_XML(Main_Page.UI_Recipe_List_Names);
        });

        public ICommand Shopping_List_Delete_Command => new Command(async () =>
        {
            ShoppingListPage? Shopping_List_Page = Shell.Current.CurrentPage as ShoppingListPage;

            var Group = Shopping_List_Page?.Grouped_Items_Observable
                        .FirstOrDefault(g => g.Contains(this));

            if (Group is null)
            {
                return;
            }
            else
            {
                Group.Remove(this);
            }


            if (Group.Count == 0 && Shopping_List_Page is not null)
            {
                Shopping_List_Page.Grouped_Items_Observable.Remove(Group);
            }

            Shopping_List_Page!.Save_GroceryList_XML(Shopping_List_Page.Grouped_Items_Observable);
            Global.Grouped_Items_Master_List = Shopping_List_Page.Grouped_Items_Observable.ToList();
        });

        public ICommand Recipe_Delete_Command => new Command(async () =>
        {
            RecipePage? Recipe_Page = Shell.Current.CurrentPage as RecipePage;

            var Group = Recipe_Page?.Grouped_Items_Observable
                        .FirstOrDefault(g => g.Contains(this));

            if (Group is null)
            {
                return;
            }
            else
            {
                Group.Remove(this);
            }

            if (Group.Count == 0 && Recipe_Page is not null)
            {
                Recipe_Page.Grouped_Items_Observable.Remove(Group);
            }

            Recipe_Page!.Save_RecipeList_XML(Recipe_Page.Grouped_Items_Observable);
            Global.Grouped_Items_Master_List = Recipe_Page.Grouped_Items_Observable.ToList();
        });
    }
}























