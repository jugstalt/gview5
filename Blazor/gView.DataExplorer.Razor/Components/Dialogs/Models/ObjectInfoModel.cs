using gView.Blazor.Models.Dialogs;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class ObjectInfoModel : IDialogResultItem
{
    private IExplorerObject? _exObject;

    public required IExplorerObject ExplorerObject
    {
        get => _exObject!;
        set
        {
            _exObject = value;
        }
    }

    public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}
