using gView.Cmd.Core.Abstraction;
using gView.Cmd.Fdb.Lib;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
internal class TruncateFeatureClass : IExplorerObjectContextTool
{
    public string Name => "Truncate...";

    public string Icon => "basic:warning_yellow";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return true;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var scopeService = scope.ToExplorerScopeService();
        var featureClass = await exObject.GetInstanceAsync() as IFeatureClass;
        var fdb = featureClass?.Dataset?.Database as AccessFDB;

        if (featureClass == null || fdb == null)
        {
            throw new Exception("Instance is not an valid FDB Featureclass!");
        }

        IDictionary<string, object>? parameters = null;
        ICommand? command = null;

        var featureDataset = featureClass.Dataset;
        var featureDatasetGuid = PlugInManager.PlugInID(featureDataset);

        command = new TruncateFeatureClassCommand();
        parameters = new Dictionary<string, object>()
            {
                { "dataset_connstr", featureDataset.ConnectionString },
                { "dataset_guid", featureDatasetGuid.ToString() },
                { "dataset_fc", featureClass.Name }
            };

        await scopeService.ShowKnownDialog(
                    KnownDialogs.ExecuteCommand,
                    $"Rebuild spatial index",
                    new ExecuteCommandModel()
                    {
                        CommandItems = new[]
                        {
                            new CommandItem()
                            {
                                Command = command,
                                Parameters = parameters
                            }
                        }
                    });

        return true;
    }
}
