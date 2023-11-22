using gView.Blazor.Models.Dialogs;

namespace gView.Razor.Dialogs.Models;

public class PropertyGridDialogModel : IDialogResultItem
{
    public object Instance { get; set; } = new object();
}
