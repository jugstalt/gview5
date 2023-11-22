using gView.Blazor.Models.Dialogs;
using gView.Framework.Carto;

namespace gView.Carto.Razor.Components.Dialogs.Models;
public class MapSettingsModel : IDialogResultItem
{
    public Map? Map { get; set; } = null;
}
