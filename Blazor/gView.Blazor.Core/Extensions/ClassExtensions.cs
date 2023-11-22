using gView.Blazor.Core.Services;
using gView.Framework.Data;
using gView.Framework.Geometry;
using static gView.Framework.Data.MapServerHelper;

namespace gView.Blazor.Core.Extensions;

static public class ClassExtensions
{
    public static IEnvelope? ClassEnvelope(this IClass @class)
    {
        return @class switch
        {
            IFeatureClass fClass => fClass.Envelope,
            IRasterClass rClass => rClass.Polygon?.Envelope,
            IWebServiceClass wClass => wClass.Envelope,
            _ => null
        };
    }

    public static IEnvelope? GetProjectedEnvelope(this IClass @class, GeoTransformerService geoTransformer, ISpatialReference sref)
    {
        var extent = @class.ClassEnvelope();
        var classSref = @class.ClassSpatialReference();

        if (extent is null || classSref is  null)
        {
            return null;  // better null than wrong (unprojected)
        }

        return geoTransformer.Transform(extent, classSref, sref).Envelope;
    }

    public static ISpatialReference? ClassSpatialReference(this IClass @class)
    {
        return @class switch
        {
            IFeatureClass fClass => fClass.SpatialReference,
            IRasterClass rClass => rClass.SpatialReference,
            IWebServiceClass wClass => wClass.SpatialReference,
            _ => null
        };
    }

    public static (ILayer layer,IEnvelope? extent) ToLayerAndExtent(
                                this IClass @class,
                                GeoTransformerService? geoTransformer = null,
                                ISpatialReference? sRef = null)
    {
        var layer = LayerFactory.Create(@class);

        IEnvelope? extent = null;
        if (geoTransformer is not null && sRef is not null)
        {
            extent = @class.ClassEnvelope();
            var layerSRef = @class.ClassSpatialReference();

            if (extent is not null && layerSRef is not null)
            {
                extent = geoTransformer.Transform(extent, layerSRef, sRef).Envelope;
            }
        }

        return (layer, extent);
    }
}
