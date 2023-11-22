using gView.Blazor.Models.Content;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.Blazor.Models.Extensions;
static public class ContentItemExtensions
{
    static public T SetExplorerObject<T>(this T item, IExplorerObject? explorerObject)
        where T : ContentItem
    {
        item.ExplorerObject = explorerObject;

        return item;
    }

    static public IContentItemResult ToResult(this ContentItem item)
        => new ContentItemResult() { Item = item };
}
