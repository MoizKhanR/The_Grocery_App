namespace The_Grocery_App;

using Mscc.GenerativeAI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
public partial class RecipePage : ContentPage, INotifyPropertyChanged
{
    //public ObservableCollection<CategoryGroup> Grouped_Items_Observable { get; set; } = new();

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
	}

    //COMMANDS
    private async void OnListsClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
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
get the                         ingredients for the recipe as accurately as possible based on the catalog.

                                Return ONLY valid JSON, no markdown, no backticks, no explanation. Example format:
                                [{{""name"":""Chicken Breast"",""category"":""Poultry""}},{{""name"":""Yogurt"",""category"":""Dairy""}}]";

            // Call Gemini
            var Google_AI = new GoogleAI(apiKey: "AQ.Ab8RN6IvfGg5mXPYhLUCAcq0DynfD5Lo0EgX34fHVqfoPUhgBw");
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

}