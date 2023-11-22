using gView.Framework.Data;
using gView.Framework.Symbology;
using gView.Framework.UI;
using System.Collections.Generic;
using System.Linq;

namespace gView.Carto.Core.Extensions;

static public class TocElementExtensions
{
    static public bool HasLegendItems(this ITocElement tocElement)
        => tocElement?.Layers?.Any(l =>
                l is IFeatureLayer fLayer
                && fLayer.FeatureRenderer?.Symbols?.Any() == true) == true;

    static public IEnumerable<ISymbol> GetLegendItems(this ITocElement tocElement)
    {
        List<ISymbol> items = new List<ISymbol>();

        if (tocElement?.Layers != null)
        {
            foreach (IFeatureLayer featureLayer in tocElement.Layers.Where(l => l is IFeatureLayer fLayer && fLayer.FeatureRenderer?.Symbols != null))
            {
                items.AddRange(featureLayer.FeatureRenderer.Symbols);
            }
        }

        return items;
    }
}
