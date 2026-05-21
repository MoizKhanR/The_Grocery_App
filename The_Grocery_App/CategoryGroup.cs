using System.Collections.ObjectModel;
using System.ComponentModel;
using The_Grocery_App;

public class CategoryGroup : ObservableCollection<UIShoppingItem>
{
    // No need to declare INotifyPropertyChanged — ObservableCollection already has it

    private string? _category_Name = string.Empty;
    public string? Category_Name
    {
        get => _category_Name;
        set
        {
            _category_Name = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Category_Name)));
        }
    }

    public CategoryGroup(string category_name) : base()
    {
        _category_Name = category_name;
    }
}