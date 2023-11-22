using gView.Blazor.Models.Dialogs;
using gView.Framework.Carto;
using gView.Framework.Data;

namespace gView.Carto.Razor.Components.Dialogs.Models;

public class LayerSettingsModel : IDialogResultItem
{
    public Map? Map { get; set; } = null;
    public ILayer? Layer { get; set; } = null;
}
