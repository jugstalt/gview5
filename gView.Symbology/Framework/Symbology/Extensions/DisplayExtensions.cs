using gView.Framework.Carto;

namespace gView.Symbology.Framework.Symbology.Extensions
{
    static public class DisplayExtensions
    {
        const int LegendItemSymbolWidth = 20;
        const int LegendItemSymbolHeight = 20;

        static public bool IsLegendItemSymbol(this IDisplay display)
        {
            return display.ImageWidth == LegendItemSymbolWidth &&
                   display.ImageHeight == LegendItemSymbolHeight;
        }

        static public GraphicsEngine.CanvasRectangle ToLegendItemSymbolRect(this GraphicsEngine.CanvasRectangle rect)
        {
            rect.Left = rect.Top = 0;
            rect.Width = LegendItemSymbolWidth;
            rect.Height = LegendItemSymbolHeight;

            return rect;
        }
    }
}
