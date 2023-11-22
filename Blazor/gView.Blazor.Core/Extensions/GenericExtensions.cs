using System;
using System.Collections.Generic;
using System.Linq;

namespace gView.Blazor.Core.Extensions;

public enum GenericAddToStrategy
{
    RemoveOthers,
    AddOrRemoveIfExists,
    AddAllBetweenLastItem
}

static public class GenericExtensions
{
    static public IEnumerable<T> AddToSelection<T>(this T item,
                                                   IEnumerable<T> selectedItems,
                                                   IEnumerable<T> allItems,
                                                   GenericAddToStrategy strategy)
    {
        List<T> list = new List<T>();

        if (strategy == GenericAddToStrategy.RemoveOthers)
        {
            // only this item
            list.Add(item);
        }
        else if (strategy == GenericAddToStrategy.AddOrRemoveIfExists)
        {
            // add existing do list
            if (selectedItems != null)
            {
                list.AddRange(selectedItems.Where(e => e != null));
            }

            // remove if exits
            if (list.Contains(item))
            {
                list.Remove(item);
            }
            else  // add as new
            {
                list.Add(item);
            }
        }
        else if (strategy == GenericAddToStrategy.AddAllBetweenLastItem)
        {
            int index1 = allItems.IndexOf(item),
                index2 = allItems.IndexOf(selectedItems.LastOrDefault());

            if (index1 == -1 || index2 == -1)  // change nothing?
            {
                return selectedItems;
            }

            int startIndex = Math.Min(index1, index2),
                endIndex = Math.Max(index1, index2) + 1;

            list.AddRange(allItems.ToArray()[startIndex..endIndex]);
        }

        return list.Where(e => e != null).ToArray();
    }

    static public int IndexOf<T>(this IEnumerable<T> source, T item)
        => source.Select((x, index) => new { Item = x, Index = index })
                 .FirstOrDefault(x => Equals(x.Item, item))?.Index ?? -1;

    static public void AddItems<T>(this ICollection<T> collectoin, IEnumerable<T> items)
    {
        if (collectoin != null && items != null)
        {
            foreach (var item in items)
            {
                collectoin.Add(item);
            }
        }
    }

    static public void ReplaceItems<T>(this ICollection<T> list, IEnumerable<T> items)
    {
        if (list != null)
        {
            list.Clear();
            list.AddItems(items);
        }
    }
}
