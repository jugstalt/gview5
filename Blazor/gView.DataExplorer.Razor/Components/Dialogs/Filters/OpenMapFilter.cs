using gView.Framework.Carto;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenMapFilter : ExplorerOpenDialogFilter
{
    public OpenMapFilter()
        : base("Map")
    {
        this.ObjectTypes.Add(typeof(IMap));

        this.BrowseAll = true;
    }
}
