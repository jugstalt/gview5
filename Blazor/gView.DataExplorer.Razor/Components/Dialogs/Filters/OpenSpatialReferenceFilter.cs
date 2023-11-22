using gView.Framework.Geometry;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenSpatialReferenceFilter : ExplorerOpenDialogFilter
{
    public OpenSpatialReferenceFilter()
        : base("Spatialreference")
    {
        this.ObjectTypes.Add(typeof(ISpatialReference));

        this.BrowseAll = true;
    }
}
