using gView.Carto.Plugins.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.system;

namespace gView.Carto.Plugins.CartoTools;

[RegisterPlugIn("20BB9506-D6AE-4A81-AC2D-733DEE4465A4")]
internal class MapSettings : ICartoTool
{
    public string Name => "Map Settings";

    public string ToolTip => "";

    public ToolType ToolType => ToolType.Click;

    public string Icon => "basic:settings";

    public CartoToolTarget Target => CartoToolTarget.Map;

    public int SortOrder => 99;

    public void Dispose()
    {

    }

    public bool IsEnabled(IApplicationScope scope) => true;

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();
        var original = scopeService.Document.Map as Map;
        var clone = original?.Clone() as Map;

        if (original is null || clone is null)
        {
            return false;
        }

        clone.ZoomTo(original.Display.Envelope);
        clone.Display.ImageWidth = original.Display.ImageWidth;
        clone.Display.ImageHeight = original.Display.ImageHeight;

        var model = await scopeService.ShowModalDialog(typeof(gView.Carto.Razor.Components.Dialogs.MapSettingsDialog),
                                                    "Map Settings",
                                                    new Razor.Components.Dialogs.Models.MapSettingsModel()
                                                    {
                                                        Map = clone
                                                    });

        if (model?.Map != null)
        {
            #region General

            original.Name = clone.Name;

            original.ReferenceScale = clone.ReferenceScale;
            original.Display.MapUnits = clone.Display.MapUnits;
            original.Display.DisplayUnits = clone.Display.DisplayUnits;
            original.Display.BackgroundColor = clone.Display.BackgroundColor;

            #endregion

            #region Spatial Reference

            original.SpatialReference = clone.SpatialReference;
            original.LayerDefaultSpatialReference = clone.LayerDefaultSpatialReference;

            #endregion

            #region Description

            original.Title = clone.Title;
            original.SetLayerDescription(Map.MapDescriptionId, clone.GetLayerDescription(Map.MapDescriptionId));
            original.SetLayerCopyrightText(Map.MapCopyrightTextId, clone.GetLayerCopyrightText(Map.MapCopyrightTextId));

            #endregion

            #region Resources

            // clone and original shares resource container
            // changes are live (no cancel!)

            #endregion

            await scopeService.EventBus.FireRefreshMapAsync();
        }

        return true;
    }
}
