namespace gView.Blazor.Core.Models;

public class SelectableItem<T>
{
    public SelectableItem(T item, bool selected = false)
    {
        this.Item = item;
        this.Selected = selected;
    }

    public bool Selected { get; set; }
    public T Item { get; }
}
