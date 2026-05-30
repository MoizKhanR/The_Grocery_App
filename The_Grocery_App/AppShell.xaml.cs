namespace The_Grocery_App
{
    public partial class AppShell : Shell
    {
       
        public AppShell()
        {          
            InitializeComponent();
            Routing.RegisterRoute(nameof(ShoppingListPage), typeof(ShoppingListPage));
            Routing.RegisterRoute(nameof(RecipePage), typeof(RecipePage));
            Routing.RegisterRoute(nameof(AddItemPage), typeof(AddItemPage));
        }
    }
}
