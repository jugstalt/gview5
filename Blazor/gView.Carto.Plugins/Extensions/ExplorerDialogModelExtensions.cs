using gView.Blazor.Core.Extensions;
using gView.Blazor.Core.Services;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Data;
using gView.Framework.Geometry;

namespace gView.Carto.Plugins.Extensions;

static internal class ExplorerDialogModelExtensions
{
    async static public Task<(IEnumerable<ILayer> layers, IEnvelope? layersExtent)> GetLayers(
                                                            this ExplorerDialogModel model, 
                                                            GeoTransformerService? geoTransformer,
                                                            ISpatialReference? mapSref)
    {
        IEnvelope? layersExtent = null;

        List<ILayer> layers = new();

        foreach (var exObject in model.Result.ExplorerObjects)
        {
            var instance = await exObject.GetInstanceAsync();

            if (instance is IClass @class)
            {
                var layerResult = @class.ToLayerAndExtent(geoTransformer, mapSref);

                if (layerResult.layer != null)
                {
                    layers.Add(layerResult.layer);
                    layersExtent = layersExtent.TryUnion(layerResult.extent);
                }
            }
            if (instance is IDataset dataset)
            {
                foreach (var datasetELement in await dataset.Elements())
                {
                    if (datasetELement.Class is not null)
                    {
                        var layerResult = datasetELement.Class.ToLayerAndExtent(geoTransformer, mapSref);
                        if (layerResult.layer != null)
                        {
                            layers.Add(layerResult.layer);
                            layersExtent = layersExtent.TryUnion(layerResult.extent);
                        }
                    }
                }
            }
        }

        return (layers, layersExtent);
    }

    
}
