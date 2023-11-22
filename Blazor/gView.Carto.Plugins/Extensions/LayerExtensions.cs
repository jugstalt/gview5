using gView.Framework.Data;
using gView.Framework.Geometry;
using MongoDB.Bson.Serialization.Conventions;

namespace gView.Carto.Plugins.Extensions;

internal static class LayerExtensions
{
    static public IEnumerable<ILayer> OrderLayersByGeometryType(this IEnumerable<ILayer> layers)
        => layers.OrderBy(l => l.LayerGeometryTypeOrder());

    static public int LayerGeometryTypeOrder(this ILayer layer)
        => layer.Class switch
        {
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Point) => 0,
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Multipoint) => 1,
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Polyline) => 2,
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Polygon) => 3,
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Envelope) => 4,
            IFeatureClass fClass when (fClass.GeometryType == GeometryType.Unknown) => 5,
            IRasterCatalogClass => 7,
            IRasterClass => 6,
            _ => 99
        };


    static public ILayer? FirstOrHigherIndexOfGeometryTypeOrder(this IEnumerable<ILayer>? layers, ILayer candidate)
    {
        if (layers != null)
        {
            int candidateOrder = candidate.LayerGeometryTypeOrder();

            foreach (var layer in layers)
            {
                if (layer is IGroupLayer)
                {
                    continue;
                }

                if (layer.LayerGeometryTypeOrder() >= candidateOrder)
                {
                    return layer;
                }
            }
        }

        return null;
    }
}
