using gView.Carto.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.Data;
using gView.Framework.system;
using System.ComponentModel;

namespace gView.Carto.Plugins.CartoTools;

[RegisterPlugIn("7C9CA04B-7DFC-4028-8CEF-25D2A02272ED")]
internal class AddData : ICartoInitialTool
{
    public string Name => "Add Data";

    public string ToolTip => "Add spatial data to map";

    public ToolType ToolType => ToolType.Command;

    public string Icon => "basic:database-plus";

    public CartoToolTarget Target => CartoToolTarget.Map;

    public int SortOrder => 1;

    public void Dispose()
    {

    }

    public bool IsEnabled(IApplicationScope scope) => true;

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();

        var model = await scopeService.ShowKnownDialog(KnownDialogs.ExplorerDialog,
                                                       title: "Add Data",
                                                       model: new ExplorerDialogModel()
                                                       {
                                                           Filters = new List<ExplorerDialogFilter> {
                                                                new OpenDataFilter()
                                                           },
                                                           Mode = ExploerDialogMode.Open
                                                       });

        if(model == null)
        {
            return false;
        }

        var map = scopeService.Document.Map;
        bool firstLayer = map.MapElements!.Any() != true;
        var layersResult = await model.GetLayers(scopeService.GeoTransformer, map.Display.SpatialReference);
        
        foreach(var layer in layersResult.layers.OrderLayersByGeometryType())
        {
            scopeService.Document.Map.AddLayer(layer);
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
