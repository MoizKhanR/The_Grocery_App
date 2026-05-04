using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace The_Grocery_App
{
    public class CategoryGroup : ObservableCollection<UIShoppingItem>
    {
        public string? Category_Name { get; set; } = string.Empty;

        public CategoryGroup(string category_name) : base() //before a child can do anything, the parent needs to be fully set up first
        {
            Category_Name = category_name;
        }
    }
}
