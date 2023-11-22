using gView.Blazor.Core.Models;
using System.Collections.Generic;

namespace gView.Blazor.Core.Extensions;

static public class EnumerableExtensions
{
    static public SelectableEnumerable<T> ToSelectable<T>(this IEnumerable<T> items, bool selected = false)
        where T : class
        => new SelectableEnumerable<T>(items, selected);
}
