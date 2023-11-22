using gView.Framework.system;

namespace gView.Framework.Symbology
{
    static class ReferenceScaleHelper
    {
        static public float PenWidth(float penWidth, IPenWidth symbol, Carto.IDisplay display)
        {
            float dpiFactor = display == null || display.Dpi == 96.0f ?
                1f :
                (float)(display.Dpi / 96.0);

            if (symbol.MinPenWidth > 0f && penWidth < symbol.MinPenWidth * dpiFactor)
            {
                return symbol.MinPenWidth * dpiFactor;
            }

            if (symbol.MaxPenWidth > 0 && penWidth > symbol.MaxPenWidth * dpiFactor)
            {
                return symbol.MaxPenWidth * dpiFactor;
            }

            return penWidth;
        }

        static public float CalcPixelUnitFactor(CloneOptions options)
        {
            float fac = 1f;

            var display = options.Display;
            if (display != null)
            {
                if (options.ApplyRefScale)
                {
                    fac = (float)(display.ReferenceScale / display.MapScale);
                    fac = options.RefScaleFactor(fac);
                }
                fac *= options.DpiFactor;
            }

            return fac;
        }

        static public float RefscaleFactor(float factor, float sizeValue, float minSizeValue, float maxSizeValue)
        {
            if (sizeValue == 0)
            {
                return factor;
            }

            if (minSizeValue > 0 && sizeValue * factor < minSizeValue)
            {
                return minSizeValue / sizeValue;
            }
            if (maxSizeValue > 0 && sizeValue * factor > maxSizeValue)
            {
                return maxSizeValue / sizeValue;
            }

            return factor;
        }
    }
}
