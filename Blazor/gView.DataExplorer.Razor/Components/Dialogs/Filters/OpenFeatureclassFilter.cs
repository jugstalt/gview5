using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenFeatureclassFilter : ExplorerOpenDialogFilter
{
    public OpenFeatureclassFilter()
        : base("Feature Class")
    {
        this.ObjectTypes.Add(typeof(IFeatureClass));
    }
}
