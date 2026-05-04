using System;
using System.Collections.Generic;
using System.Text;

namespace The_Grocery_App
{
    public class Item
    {
        string itemName = "";
        string itemCategory = "";
        List<string?> sizes = new List<string?>();
        List<string?> qualities = new List<string?>();

        public string ItemName { get => itemName; set => itemName = value; }
        public string ItemCategory { get => itemCategory; set => itemCategory = value; }
        public List<string?> Sizes { get => sizes; set => sizes = value; }
        public List<string?> Qualities { get => qualities; set => qualities = value; }
    }
}
