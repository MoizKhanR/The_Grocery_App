using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace The_Grocery_App
{
    public class Grocery
    {
        //List<Category> categories = new List<Category>(); //No need for backing field
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
