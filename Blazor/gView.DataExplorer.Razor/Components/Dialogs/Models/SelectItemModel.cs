namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class SelectItemModel<T>
{
    public SelectItemModel(T item)
    {
        Item = item;
    }

    public bool Selected { get; set; } = true;
    public T Item { get; set; }
}
