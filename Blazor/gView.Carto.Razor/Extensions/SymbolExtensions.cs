using gView.Carto.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Symbology;

namespace gView.Carto.Razor.Extensions;

public static class SymbolExtensions
{
    async public static Task<ISymbol> Compose(this ISymbol symbol, IApplicationScope scope)
    {
        var model = await scope.ShowModalDialog(
            typeof(gView.Carto.Razor.Components.Dialogs.SymbolComposerDialog),
            "Symbol Composer",
            new SymbolComposerModel()
            {
                Symbol = symbol.Clone() as ISymbol,
            });

        return model?.Symbol ?? symbol;
    }

    public static bool HasSameSymbolBaseType(this ISymbol? symbol, Type symbolType)
        => symbol switch
        {
            ISymbolCollection => false,
            IPointSymbol when typeof(IPointSymbol).IsAssignableFrom(symbolType) => true,
            ILineSymbol when typeof(ILineSymbol).IsAssignableFrom(symbolType) => true,
            IFillSymbol when typeof(IFillSymbol).IsAssignableFrom(symbolType) => true,
            ITextSymbol when typeof(ITextSymbol).IsAssignableFrom(symbolType) => true,
            _ => false
        };
}
