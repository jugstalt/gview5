using gView.Framework.DataExplorer.Abstraction;

namespace gView.Blazor.Models.Content;
public class ContentItem : IContentItem
{
    public IExplorerObject? ExplorerObject { get; internal set; }
}