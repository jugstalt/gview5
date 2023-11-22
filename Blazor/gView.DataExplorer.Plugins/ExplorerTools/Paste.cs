using gView.CopyFeatureclass.Lib;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[gView.Framework.system.RegisterPlugIn("2C22F66F-BE67-420A-9B99-92B98260FA76")]
internal class Paste : IExplorerTool
{
    public string Name => "Paste";

    public string ToolTip => "";

    public string Icon => "basic:paste";

    public ExplorerToolTarget Target => ExplorerToolTarget.SelectedContextExplorerObjects;

    public bool IsEnabled(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        if (scopeService.GetClipboardItemType() == typeof(IFeatureClass))
        {
            return scopeService.CurrentExplorerObject?.ObjectType != null &&
                scopeService.CurrentExplorerObject.ObjectType.IsAssignableTo(typeof(IFeatureDataset));
        }

        return false;
    }

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        if (scopeService.CurrentExplorerObject is null)
        {
            return false;
        }

        var destination = await scopeService.CurrentExplorerObject.GetInstanceAsync();
        if (destination is IFeatureDataset)
        {
            IFeatureDataset destDataset = (IFeatureDataset)destination;
            var destDatasetGuid = PlugInManager.PlugInID(destDataset);

            if (destDatasetGuid == Guid.Empty)
            {
                throw new Exception("Current dataset no not a valid gView Plugin. Copy featuresclasses is only possible to dataset plugins!");
            }

            List<CommandItem> commandItems = new();

            foreach (var featureClass in scopeService.GetClipboardElements<IFeatureClass>())
            {
                var sourceDataset = featureClass.Dataset;
                var sourceDatasetGuid = PlugInManager.PlugInID(sourceDataset);

                if (sourceDatasetGuid == Guid.Empty)
                {
                    continue;
                }

                commandItems.Add(new CommandItem()
                {
                    Command = new CopyFeatureClassCommand(),
                    Parameters = new Dictionary<string, object>()
                    {
                        { "source_connstr", sourceDataset.ConnectionString },
                        { "source_guid", sourceDatasetGuid.ToString() },
                        { "source_fc", featureClass.Name },
                        { "dest_connstr", destDataset.ConnectionString },
                        { "dest_guid", destDatasetGuid.ToString() },
                        { "dest_fc", featureClass.Name }
                    }
                });
            }

            await scopeService.ShowKnownDialog(
                    KnownDialogs.ExecuteCommand,
                    $"Copy {commandItems.Count} FeatureClasses",
                    new ExecuteCommandModel() { CommandItems = commandItems });

            await scopeService.ForceContentRefresh();
        }

        return false;
    }

    #region IDisposable

    public void Dispose()
    {

    }

    #endregion

    #region IOrder

    public int SortOrder => 22;

    #endregion
}
