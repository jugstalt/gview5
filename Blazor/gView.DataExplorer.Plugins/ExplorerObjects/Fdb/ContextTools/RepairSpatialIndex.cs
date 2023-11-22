using gView.Cmd.Core.Abstraction;
using gView.Cmd.Fdb.Lib;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataSources.MSSqlSpatial;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;

internal class RepairSpatialIndex : IExplorerObjectContextTool
{
    public string Name => "Repair spatial indices...";

    public string Icon => "basic:warning_yellow";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return true;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var scopeService = scope.ToExplorerScopeService();
        var instance = await exObject.GetInstanceAsync();

        List<CommandItem> commandItems = new();

        if (instance is IFeatureDataset)
        {
            var featureDataset = (IFeatureDataset)instance;
            var featureDatasetGuid = PlugInManager.PlugInID(featureDataset);

            foreach(var datasetElement in await featureDataset.Elements())
            {
                if(!(datasetElement.Class is IFeatureClass))
                {
                    continue;
                }

                commandItems.Add(new CommandItem()
                {
                    Command = new RepairSpatialIndexCommand(),
                    Parameters = new Dictionary<string, object>()
                {
                    { "dataset_connstr", featureDataset.ConnectionString },
                    { "dataset_guid", featureDatasetGuid.ToString() },
                    { "dataset_fc", datasetElement.Class.Name },
                }
                });
            }
        }
        else if (instance is IFeatureClass)
        {
            var featureClass = (IFeatureClass)instance;
            var featureDataset = featureClass.Dataset;
            var featureDatasetGuid = PlugInManager.PlugInID(featureDataset);

            commandItems.Add(new CommandItem()
            {
                Command = new RepairSpatialIndexCommand(),
                Parameters = new Dictionary<string, object>()
                {
                    { "dataset_connstr", featureDataset.ConnectionString },
                    { "dataset_guid", featureDatasetGuid.ToString() },
                    { "dataset_fc", featureClass.Name },
                }
            });
        }
        else
        {
            return false;
        }

        if (commandItems.Count > 0)
        {
            await scopeService.ShowKnownDialog(
                        KnownDialogs.ExecuteCommand,
                        $"Repaier spatial indices",
                        new ExecuteCommandModel() { CommandItems = commandItems });
        }

        return true;
    }

}
