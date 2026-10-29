namespace The_Grocery_App;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

[QueryProperty(nameof(Item_Name), "ItemName")]
[QueryProperty(nameof(Item_Category), "ItemCategory")]
public partial class RecipePage : ContentPage, INotifyPropertyChanged
{
    //One HttpClient for the whole app (45 seconds, same as the old Gemini timeout)
    static readonly HttpClient Proxy_Http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };

    //What the proxy sends back
    class Proxy_Recipe_Reply
    {
        public List<RecipeItem>? items { get; set; }
    }

    private string _item_Category = string.Empty;
    public string? Page_Name { get; set; } = Global.Selected_Recipe_List;
    public string Item_Category
    {
        get => _item_Category;
        set
        {
            _item_Category = value;
            Add_Item_To_List(); //Fires after BOTH values are set
        }
    }
    public string Item_Name { get; set; } = string.Empty;

    private ObservableCollection<CategoryGroup> _grouped_Items_observable = new();
    public ObservableCollection<CategoryGroup> Grouped_Items_Observable
    {
        get => _grouped_Items_observable;
        set
        {
            _grouped_Items_observable = value;
            OnPropertyChanged(nameof(Grouped_Items_Observable)); // notify the UI
        }
    }
    public RecipePage()
	{
		InitializeComponent();
        BindingContext = this;
        Global.OnRecipeListChanged += OnRecipeChanged;
    }

    //Recipe list changes, we clear the Observable Collection
    private void OnRecipeChanged(string? newList)
    {
        //Save the XML before you leave foo
        Save_RecipeList_XML(Grouped_Items_Observable);
        Grouped_Items_Observable.Clear();
    }

    protected async override void OnAppearing()
    {        
        base.OnAppearing();

        //The smart list generation button & the entry is invisible by default
        Generate_Recipe_GO_Button.IsVisible = false;
        AI_Prompt_Entry.IsVisible = false;
    }
    protected override void OnDisappearing()
    {
        //Save the XML before you leave foo
        Save_RecipeList_XML(Grouped_Items_Observable);
        base.OnDisappearing();
        Global.OnRecipeListChanged -= OnRecipeChanged;
    }

    //COMMANDS
    private async void OnListsClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }

    private async void Add_Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            //Set the page we're "coming from" when we arrive at the AddItemPage
            Global.Came_From = nameof(RecipePage);

            //((SearchBar)sender).Unfocus(); //No keyboard needed, we will navigate to the AddItemPage
            await Shell.Current.GoToAsync(nameof(AddItemPage));
        }
        catch (Exception E)
        {
            await DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
        }
    }

    private void OnGenerateSmartListButtonClicked(object sender, EventArgs e)
    {
        Generate_Recipe_GO_Button.IsVisible = true;
        AI_Prompt_Entry.IsVisible = true;
    }
    private async void OnGoButtonClicked(object sender, EventArgs e)
    {
        Generate_Smart_List();
    }

    private async void Generate_Smart_List()
    {
        Grouped_Items_Observable.Clear();

        string User_Prompt = AI_Prompt_Entry.Text;

        if (string.IsNullOrWhiteSpace(User_Prompt))
        {
            return;
        }

        //Gating
        if (!Global.Can_Generate_Recipe())
        {
            bool Go = await DisplayAlertAsync("Daily limit reached",
                $"You've used your {Global.Free_Recipes_Per_Day} free recipes today. Go PRO for unlimited.",
                "Go PRO", "Maybe later");

            if (Go)
            {
                await ProPrompt.Show(this);
            }
            else
            {
                return;
            }
        }

        Generate_Recipe_GO_Button.IsEnabled = false;
        Generate_Recipe_GO_Button.Text = "Generating...";
        Add_Item_Button.IsEnabled = false;
        Generate_Recipe_Button.IsEnabled = false;
        AI_Prompt_Entry.IsEnabled = false;

        try
        {
            //Ask OUR proxy (Cloudflare Worker) - it holds the Gemini key + the prompt, the app only sends the dish + the catalog
            string The_Catalog_String = BuildCatalogPrompt();

            string Request_JSON = JsonSerializer.Serialize(new
            {
                recipe = User_Prompt,
                catalog = The_Catalog_String,
                device = Global.Device_Id
            });

            using var The_Request = new HttpRequestMessage(HttpMethod.Post, Global.Proxy_Url)
            {
                Content = new StringContent(Request_JSON, Encoding.UTF8, "application/json")
            };
            The_Request.Headers.Add("X-App-Token", Secrets.App_Token);

            HttpResponseMessage The_Response;

            try
            {
                The_Response = await Proxy_Http.SendAsync(The_Request);
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync("Timeout", "AI didn't respond. Try again in a moment.", "OK");
                return;
            }

            //429 = daily limit reached, 503 = Gemini is overloaded right now
            if (The_Response.StatusCode == HttpStatusCode.TooManyRequests || The_Response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                await DisplayAlertAsync("Kitchen's busy", "Our AI chef is swamped right now. Try again in a little while, or add items manually.", "OK");
                return;
            }

            if (!The_Response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Hmm", "Couldn't build the list right now. Check your internet and try again.", "OK");
                return;
            }

            string JSON = (await The_Response.Content.ReadAsStringAsync()).Trim();

            //The proxy answers { "items": [ { "name": "...", "category": "..." } ] }
            var Proxy_Reply = JsonSerializer.Deserialize<Proxy_Recipe_Reply>(JSON);
            var Recipe_Items = Proxy_Reply?.items;

            if (Recipe_Items == null || Recipe_Items.Count == 0)
            {
                await DisplayAlertAsync("Hmm", "No ingredients found. Try describing your recipe differently.", "OK");
                return;
            }

            //Convert to UIShoppingItems and add to CollectionView
            foreach (var recipe in Recipe_Items)
            {
                var Master_Item = Global.Master_List.FirstOrDefault(i => i.UI_Name == recipe.name);

                var new_Item = new UIShoppingItem
                {
                    UI_Name = recipe.name,
                    Category_Name = recipe.category,
                    UI_Sizes = new ObservableCollection<string>(Master_Item?.UI_Sizes ?? Enumerable.Empty<string>()),
                    UI_Qualities = new ObservableCollection<string>(Master_Item?.UI_Qualities ?? Enumerable.Empty<string>()),
                    Picker_Visibility = true
                };

                //Find or create the category group
                var Group = Grouped_Items_Observable.FirstOrDefault(g => g.Category_Name == recipe.category);
                if (Group == null)
                {
                    Group = new CategoryGroup(recipe.category);
                    Grouped_Items_Observable.Add(Group);
                }

                //Don't add duplicates
                if (!Group.Any(item => item.UI_Name == recipe.name))
                {
                    Group.Add(new_Item);
                    await Task.Delay(300); //Small delay to allow UI to update smoothly
                }

            }

            //Keep the master in sync, like every other path does
            Global.Grouped_Items_Master_List = Grouped_Items_Observable.ToList();

            //Save the XML
            Save_RecipeList_XML(Grouped_Items_Observable);

            //Register the recipe generation in the global to track usage
            Global.Register_Recipe_Generation();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            Generate_Recipe_GO_Button.IsEnabled = true;
            Generate_Recipe_GO_Button.IsVisible = false;
            Generate_Recipe_GO_Button.Text = "Go";
            AI_Prompt_Entry.IsEnabled = true;
            AI_Prompt_Entry.IsVisible = false;
            AI_Prompt_Entry.Placeholder = "Describe the recipe you want to create...";

            Add_Item_Button.IsEnabled = true;
            Generate_Recipe_Button.IsEnabled = true;
            
        }
    }   
    //Helper functions
    private string BuildCatalogPrompt()
    {
        try
        {
            var Grouped = Global.Master_List
                .GroupBy(item => item.Category_Name)
                .OrderBy(g => g.Key);

            // Build a string representation of the catalog for the prompt
            var Catalog_String = new StringBuilder();

            foreach (var Group in Grouped)
            {
                var itemNames = Group.Select(i => i.UI_Name);
                Catalog_String.AppendLine($"{Group.Key}: {string.Join(", ", itemNames)}");
            }

            return Catalog_String.ToString();
        }
        catch (Exception Ex) { return Ex.Message; }
        
    }

    //Add items manually

    public void Add_Item_To_List()
    {
        if (string.IsNullOrEmpty(Item_Name) || string.IsNullOrEmpty(Item_Category)) return;

        //Search the Master List for the Category Group, if it doesn't exist, create it and add it to the Master List
        var Group = Global.Grouped_Items_Master_List.FirstOrDefault(g => g.Category_Name == Item_Category);

        if (Group == null)
        {
            Group = new CategoryGroup(Item_Category);
            Global.Grouped_Items_Master_List.Add(Group);   // add into what's on screen
        }

        //Now check to see if the Item_Name already exists in the Group, if it does, don't add it again
        bool? Item_Exists = Group?.Any(item => item.UI_Name == Item_Name) ?? false;

        if (Item_Exists is false && Group is not null)
        {

            var Master_Item = Global.Master_List.FirstOrDefault(i => i.UI_Name == Item_Name);

            var new_Item = new UIShoppingItem
            {
                UI_Name = Item_Name,
                UI_Sizes = new ObservableCollection<string>(Master_Item?.UI_Sizes ?? Enumerable.Empty<string>()),
                UI_Qualities = new ObservableCollection<string>(Master_Item?.UI_Qualities ?? Enumerable.Empty<string>()),
                Category_Name = Item_Category,
                Picker_Visibility = true,
            };

            Group.Add(new_Item);

        }

        //Update the Grouped_Items_Observable collection to reflect the changes in the Master List
        Grouped_Items_Observable = new ObservableCollection<CategoryGroup>(Global.Grouped_Items_Master_List);

        //Save the XML
        Save_RecipeList_XML(Grouped_Items_Observable);
    }

    //Save the list & name it as Global.Selected_Shopping_List + "_RecipeList" + ".xml"
    public void Save_RecipeList_XML(ObservableCollection<CategoryGroup> Grouped_Items_Observable)
    {
        try
        {
            /*  Grouped_Items_Observable.Select(group =>  // group = CategoryGroup
                group.Select(item =>                  // item = UIShoppingItem
                    item.UI_Name                      // UIShoppingItem HAS UI_Name
                                 )
                            ) */

            XDocument Save_Doc = new XDocument(
                    new XElement("RecipeList",
                        Grouped_Items_Observable.Select(group =>
                            new XElement("Category",
                                new XAttribute("name", group.Category_Name ?? ""),
                                group.Select(item =>
                                    new XElement("Item",
                                        new XAttribute("name", item.UI_Name ?? ""),
                                        new XAttribute("size", item.Selected_Size ?? ""),
                                        new XAttribute("quality", item.Selected_Quality ?? ""),
                                        new XAttribute("quantity", item.Selected_Quantity ?? ""),
                                        new XAttribute("is_bought", item.Is_Bought.ToString() ?? "")
                                    )
                                )
                            )
                        )
                    )
                );

            //Save the XML document to a file
            string File_Path = Path.Combine(FileSystem.AppDataDirectory, $"{Global.Selected_Recipe_List}_RecipeList.xml");

            Save_Doc.Save(File_Path);

        }
        catch (Exception E)
        {
            Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to save data: {E.Message}", "OK");
        }
    }

    public void Load_RecipeList_XML(string File_Path, ObservableCollection<CategoryGroup> Grouped_Items_Observable)
    {
        try
        {
            XDocument? Load_Doc = XDocument.Load(File_Path);
            var Doc_Root = Load_Doc?.Root;

            if (Doc_Root is null) return;

            foreach (var Category_Element in Doc_Root.Elements("Category"))
            {
                string? Category_Name = Category_Element.Attribute("name")?.Value;

                if (Category_Name == null) continue;

                // Find or create the group
                var Group = Grouped_Items_Observable.FirstOrDefault(g => g.Category_Name == Category_Name);

                if (Group == null)
                {
                    Group = new CategoryGroup(Category_Name);
                    Grouped_Items_Observable.Add(Group);
                }

                // Add items to the group
                foreach (var Item_Element in Category_Element.Elements("Item"))
                {
                    //Get the Item from the Master List to populate the Sizes and Qualities in the UIShoppingItem
                    var Master_Item = Global.Master_List.FirstOrDefault(The_Item => The_Item.UI_Name == Item_Element.Attribute("name")?.Value);

                    Group.Add(new UIShoppingItem
                    {
                        UI_Name = Item_Element.Attribute("name")?.Value,
                        UI_Sizes = new ObservableCollection<string>(Master_Item?.UI_Sizes ?? Enumerable.Empty<string>()),
                        UI_Qualities = new ObservableCollection<string>(Master_Item?.UI_Qualities ?? Enumerable.Empty<string>()),
                        Selected_Size = Item_Element.Attribute("size")?.Value,
                        Selected_Quality = Item_Element.Attribute("quality")?.Value,
                        Selected_Quantity = Item_Element.Attribute("quantity")?.Value,
                        Is_Bought = bool.Parse(Item_Element.Attribute("is_bought")?.Value ?? "false"),
                        Category_Name = Category_Name
                    });
                }
            }
        }
        catch (Exception E)
        {
            Shell.Current.CurrentPage.DisplayAlertAsync("Error", $"Failed to load data: {E.Message}", "OK");
        }
    }

    async void OnHelpClicked(object? sender, EventArgs e)
    {
        await HelpInfo.Show(this);
    }

}