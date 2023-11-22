using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class ImageDatasetImportFolderModel : IDialogResultItem
{
    public string Folder { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;

    public List<ProviderModel>? Providers { get; set; }

    #region Models

    public class ProviderModel
    {
        public bool Selected { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Guid PluginGuid { get; set;} = Guid.Empty;
    }

    #endregion
}
