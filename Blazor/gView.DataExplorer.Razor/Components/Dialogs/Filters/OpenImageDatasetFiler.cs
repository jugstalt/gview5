using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenImageDatasetFiler : ExplorerOpenDialogFilter
{
    public OpenImageDatasetFiler()
        : base("Image Dataset")
    {
        this.ObjectTypes.Add(typeof(IImageDataset));
    }
}
