using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;
public class OpenRasterClassFilter : ExplorerOpenDialogFilter
{
    public OpenRasterClassFilter()
        : base("RasterClass")
    {
        this.ObjectTypes.Add(typeof(IRasterClass));
    }
}

