using gView.Framework.Data;

namespace gView.Framework.Carto
{
    static public class Extensions
    {
        static public bool RequiresFeatureRendererClone(this IFeatureLayer layer, IDisplay display)
        {
            // clone always:
            // Symboly (and so System.Drawing.Pen...) can used in different Bitmaps/GrachicsContext 
            //    if e.g. gView Server Maps... this can cause errors when paint or painting in wrong Bitmap
            //    so you have to clone anyway for the server (display is IServiceMap)
            //    => always clone!!!
            return true;

            //if (layer?.FeatureRenderer != null)
            //{
            //    if (layer.UseWithRefScale(display))
            //    {
            //        return true;
            //    }

            //    if (layer.FeatureRenderer.RequireClone())
            //    {
            //        return true;
            //    }

            //    if (display.dpi != 96.0)
            //    {
            //        return true;
            //    }
            //}

            //return false;
        }

        static public bool RequiresLabelRendererClone(this IFeatureLayer layer, IDisplay display)
        {
            // see above
            return true;

            //if (layer?.LabelRenderer != null)
            //{
            //    if (layer.UseLabelsWithRefScale(display))
            //    {
            //        return true;
            //    }

            //    if (display.dpi != 96.0)
            //    {
            //        return true;
            //    }
            //}

            //return false;
        }

        static public bool UseWithRefScale(this IFeatureLayer layer, IDisplay display)
        {
            return display.ReferenceScale > 1 && layer.FeatureRenderer.UseReferenceScale && layer.ApplyRefScale;
        }

        static public bool UseLabelsWithRefScale(this IFeatureLayer layer, IDisplay display)
        {
            return display.ReferenceScale > 1 && layer.ApplyLabelRefScale;
        }
    }
}
