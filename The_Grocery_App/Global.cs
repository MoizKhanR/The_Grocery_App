using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace The_Grocery_App
{
    public static class Global
    {
        public static XDocument? The_Doc;
        public static XElement? The_Catalog;
        public static Grocery The_Master_Catalog { get; set; } = new Grocery();
    }
}
