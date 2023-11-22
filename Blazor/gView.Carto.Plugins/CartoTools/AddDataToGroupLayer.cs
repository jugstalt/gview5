using gView.Carto.Core.Models.Tree;
using gView.Carto.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.Data;
using gView.Framework.system;

namespace gView.Carto.Plugins.CartoTools;

[RegisterPlugIn("14A1B7E6-6D73-4062-A7BF-5CB7600A6DB3")]
internal class AddDataToGroupLayer : ICartoTool
{
    public string Name => "Add Data";

    public string ToolTip => "Add spatial data to group";

    public ToolType ToolType => ToolType.Command;

    public string Icon => "basic:database-plus";

    public CartoToolTarget Target => CartoToolTarget.SelectedTocItem;

    public int SortOrder => 0;

    public void Dispose()
    {

    }

    public bool IsEnabled(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();

        return scopeService.SelectedTocTreeNode is TocParentNode;
    }

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();
        var groupLayer = (scopeService.SelectedTocTreeNode as TocParentNode)?.TocElement?.Layers?.FirstOrDefault() as IGroupLayer;

        if (groupLayer is null)
        {
            return false;
        }

        var model = await scopeService.ShowKnownDialog(
                                            KnownDialogs.ExplorerDialog,
                                            title: "Add Data",
                                            model: new ExplorerDialogModel()
                                            {
                                                Filters = new List<ExplorerDialogFilter> {
                                                    new OpenDataFilter()
                                                },
                                                Mode = ExploerDialogMode.Open
                                            });

        if (model == null)
        {
            return false;
        }

        var map = scopeService.Document.Map;
        bool firstLayer = map.MapElements!.Any() != true;
        var layersResult = await model.GetLayers(scopeService.GeoTransformer, map.Display.SpatialReference);

        foreach (var layer in layersResult.layers.OrderLayersByGeometryType().Where(l => l is Layer).Select(l => (Layer)l))
        {
            
            layer.GroupLayer = groupLayer;

            int pos = 1;
            if (map.TOC?.Elements != null)
            {
                var nextLayer = groupLayer.ChildLayer.FirstOrHigherIndexOfGeometryTypeOrder(layer);
                var nextTocElement = scopeService.Document.Map.TOC.Elements.FirstOrDefault(e => e.Layers != null && e.Layers.Contains(nextLayer));
                pos = scopeService.Document.Map.TOC.Elements.IndexOf(nextTocElement);
            }

            scopeService.Document.Map.AddLayer(layer, pos);
        }

        if (layersResult.layersExtent is not null)
        {
            if (firstLayer || layersResult.layersExtent.Intersects(map.Display.Envelope) == false)
            {
                await scopeService.EventBus.FireMapZoomToAsync(layersResult.layersExtent);
            }
        }
        await scopeService.EventBus.FireMapSettingsChangedAsync();

        return true;
    }
}
