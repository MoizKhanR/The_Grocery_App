using System;
using System.Collections.Generic;
using System.Text;

namespace The_Grocery_App
{
    public class Category
    {
        string name = "";
        List<Item> items = new List<Item>();

        public string Name { get => name; set => name = value; }
        public List<Item> Category_Items { get => items; set => items = value; }
    }
}
