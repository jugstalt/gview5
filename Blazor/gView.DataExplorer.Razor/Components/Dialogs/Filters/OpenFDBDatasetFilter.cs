namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenFDBDatasetFilter : ExplorerOpenDialogFilter
{
    public OpenFDBDatasetFilter()
        : base("FDB Dataset")
    {
        this.ExplorerObjectGUIDs.Add(new Guid("06676F47-AA31-4c2f-B703-223FE56F9B1D"));  // AccessFDBDataset
        this.ExplorerObjectGUIDs.Add(new Guid("231E8933-5AD4-4fe3-9DA3-CF806A098902"));  // SqlFDBDataset
    }
}
