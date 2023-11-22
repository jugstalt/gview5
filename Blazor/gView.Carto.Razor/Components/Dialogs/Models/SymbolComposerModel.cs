using gView.Blazor.Models.Dialogs;
using gView.Framework.Symbology;

namespace gView.Carto.Razor.Components.Dialogs.Models;

public class SymbolComposerModel : IDialogResultItem
{
    public ISymbol? Symbol { get; set; }
}
