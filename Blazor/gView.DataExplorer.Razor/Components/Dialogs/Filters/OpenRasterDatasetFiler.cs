using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenRasterDatasetFiler : ExplorerOpenDialogFilter
{
    public OpenRasterDatasetFiler()
        : base("Raster Dataset")
    {
        this.ObjectTypes.Add(typeof(IRasterDataset));
    }
}
