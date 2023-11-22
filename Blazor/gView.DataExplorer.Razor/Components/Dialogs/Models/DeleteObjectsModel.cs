using gView.Blazor.Models.Dialogs;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class DeleteObjectsModel : IDialogResultItem
{
    public required IEnumerable<SelectItemModel<IExplorerObject>> ExplorerObjects { get; set; }

    public IEnumerable<IExplorerObject> SelectedExplorerItems
        => ExplorerObjects
                    .Where(i => i.Selected)
                    .Select(i => i.Item);
}
