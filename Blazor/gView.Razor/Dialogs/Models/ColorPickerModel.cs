using gView.Blazor.Models.Dialogs;
using gView.GraphicsEngine;

namespace gView.Razor.Dialogs.Models;

public class ColorPickerModel : IDialogResultItem
{
    public ArgbColor ArgbColor { get; set; }
}
