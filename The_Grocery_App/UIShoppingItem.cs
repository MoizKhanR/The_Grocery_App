using System;
using System.Collections.Generic;
using System.Text;

namespace The_Grocery_App
{
    public class UIShoppingItem
    {
        public string? UI_Name { get; set; }
        public List<string> UI_Sizes { get; set; } = new List<string>();
        public List<string> UI_Quantities { get; set; } = new List<string>();
        public string? Selected_Size { get; set; }
        public string? Selected_Quantity { get; set; }
        public bool Picker_Visibility { get; set; } = true;
        public Color BackColor { get; set; } = Colors.Transparent;
    }
}
