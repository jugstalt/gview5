using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenFeatureDatasetOrFolder : ExplorerOpenDialogFilter
{
    public OpenFeatureDatasetOrFolder()
        : base("Feature dataset or folder")
    {
        this.ObjectTypes.Add(typeof(IFeatureDataset));
        this.ExplorerObjectGUIDs.Add(new Guid("458E62A0-4A93-45cf-B14D-2F958D67E522"));
        //this.ExplorerObjectGUIDs.Add(new Guid("A610B342-E911-4c52-8E35-72A69B52440A"));
    }
}
