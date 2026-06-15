namespace The_Grocery_App;

using Mscc.GenerativeAI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

[QueryProperty(nameof(Item_Name), "ItemName")]
[QueryProperty(nameof(Item_Category), "ItemCategory")]
public partial class RecipePage : ContentPage, INotifyPropertyChanged
{
    private string _item_Category = string.Empty;

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

    //BASIC API TESTING CODE
    //private async void OnGoButtonClicked(object sender, EventArgs e)
    //{
    //    string The_Prompt = AI_Prompt_Entry.Text;

    //    //Null check

    //    if (string.IsNullOrWhiteSpace(The_Prompt))
    //    {
    //        return;
    //    }

    //    try
    //    {
    //        await DisplayAlertAsync("Step 1", "About to create AI client", "OK");

    //        var Google_AI = new GoogleAI(apiKey: "AQ.Ab8RN6IvfGg5mXPYhLUCAcq0DynfD5Lo0EgX34fHVqfoPUhgBw"); //The API
    //        var The_Model = Google_AI.GenerativeModel(model: "gemini-2.5-flash"); //The Model to be used

    //        await DisplayAlertAsync("Step 2", "About to call Gemini", "OK");
    //        var The_Response = await The_Model.GenerateContent(prompt: The_Prompt); //The Response from the API

    //        await DisplayAlertAsync("Step 3", $"Got response: {The_Response?.Text}", "OK");
    //    }
    //    catch (Exception Ex)
    //    {
    //        await DisplayAlertAsync("Error", Ex.Message, "OK");
    //    }
    //}

    private async void OnGoButtonClicked(object sender, EventArgs e)
    {
        string userPrompt = AI_Prompt_Entry.Text;
        if (string.IsNullOrWhiteSpace(userPrompt)) return;

        Generate_Recipe_GO_Button.IsEnabled = false;
        Generate_Recipe_GO_Button.Text = "Generating...";

        try
        {
            // Build the prompt with catalog context
            string The_Catalog_String = BuildCatalogPrompt();
            string The_Prompt = $@"You are a grocery list assistant. 
                                The user wants to cook: {userPrompt}

                                Here is my grocery catalog grouped by category:
                                {The_Catalog_String}

                                Return ONLY a JSON array of ingredients needed. Each item must have ""name"" and ""category"" 
                                matching EXACTLY from the catalog above. If an ingredient isn't in the catalog, 
                                use the closest match or skip it but search the catalog thoroughly. For example, if the user says 
                                ""chicken breast"" and the catalog has ""chicken breasts"", you should match it. If the user says 
                                ""yogurt"" and the catalog has ""yogurt"" you should match it, but if the catalog only has ""milk"" 
                                you should skip it. if the user asks for a specific type of burger that has shaami kabab in it,
                                and the catalog has shaami kabab, you should include it. Don't add ""Chicken Breast"" because that
                                isn't in the catalog, but do add ""shaami kabab"" if the user asks for a burger that contains it, 
                                because it is in the catalog. Do NOT entertain any requests that aren't directly related to cooking or the grocery list.
                                get the ingredients for the recipe as accurately as possible based on the catalog.

                                Return ONLY valid JSON, no markdown, no backticks, no explanation. Example format:
                                [{{""name"":""Chicken Breast"",""category"":""Poultry""}},{{""name"":""Yogurt"",""category"":""Dairy""}}]";

            // Call Gemini
            var Google_AI = new GoogleAI(apiKey: Secrets.Gemini_API_Key);
            var The_Model = Google_AI.GenerativeModel(model: "gemini-2.5-flash");

            var API_Task = The_Model.GenerateContent(The_Prompt);
            var Timeout_Task = Task.Delay(45000);

            if (await Task.WhenAny(API_Task, Timeout_Task) == Timeout_Task)
            {
                await DisplayAlertAsync("Timeout", "AI didn't respond. Try again in a moment.", "OK");
                return;
            }

            var The_Response = await API_Task;
            string JSON = (The_Response?.Text ?? string.Empty).Trim();

            // Strip markdown code fences if Gemini adds them
            JSON = JSON.Replace("```json", "").Replace("```", "").Trim();

            var Recipe_Items = JsonSerializer.Deserialize<List<RecipeItem>>(JSON);

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

            //Save the XML
            Save_RecipeList_XML(Grouped_Items_Observable);
            //await DisplayAlertAsync("Done!", $"Added {recipeItems.Count} ingredients for your recipe!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            Generate_Recipe_GO_Button.IsEnabled = true;
            Generate_Recipe_GO_Button.Text = "Go";
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

}