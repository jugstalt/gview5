using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace gView.Blazor.Core.Models;

public class SelectableEnumerable<T> : IEnumerable<SelectableItem<T>>
    where T : class
{
    private readonly List<SelectableItem<T>> _items;

    public SelectableEnumerable()
    {
        _items = new List<SelectableItem<T>>();
    }

    public SelectableEnumerable(IEnumerable<T> items, bool selected = false)
        : this()
    {
        this.AddRange(items, selected);
    }

    public void AddRange(IEnumerable<T> items, bool selected = false)
    {
        foreach (var element in items)
        {
            _items.Add(new SelectableItem<T>(element, selected));
        }
    }

    public void SetSelected(T item, bool selected)
    {
        foreach (var element in _items.Where(i => i.Item == item))
        {
            element.Selected = selected;
        }
    }

    #region IEnumerable<T>

    public IEnumerator<SelectableItem<T>> GetEnumerator()
    {
        return ((IEnumerable<SelectableItem<T>>)_items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    #endregion

    public IEnumerable<T> SelectedElements =>
        _items.Where(i => i.Selected == true).Select(e => e.Item);

    public bool HasSelected => _items.Any(i => i.Selected == true);
}
