using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenDataFilter : ExplorerOpenDialogFilter
{
    public OpenDataFilter()
        : base("Data")
    {
        this.ObjectTypes.Add(typeof(IFeatureClass));
        this.ObjectTypes.Add(typeof(IFeatureDataset));
        this.ObjectTypes.Add(typeof(IDataset));
        this.ObjectTypes.Add(typeof(IRasterDataset));
        //this.ObjectTypes.Add(typeof(IRasterFile));
        //this.ObjectTypes.Add(typeof(IFeatureLayer));
        //this.ObjectTypes.Add(typeof(IRasterLayer));
        this.ObjectTypes.Add(typeof(IImageDataset));
        this.ObjectTypes.Add(typeof(ILayer));
        this.ObjectTypes.Add(typeof(IRasterClass));
        this.ObjectTypes.Add(typeof(IWebServiceClass));

        this.BrowseAll = true;
    }
}
