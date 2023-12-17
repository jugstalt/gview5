﻿using gView.CopyFeatureclass.Lib;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Core.Data;
using gView.Framework.Core.Common;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gView.Framework.DataExplorer.Services.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[RegisterPlugIn("2C22F66F-BE67-420A-9B99-92B98260FA76")]
internal class Paste : IExplorerTool
{
    public string Name => "Paste";

    public string ToolTip => "";

    public string Icon => "basic:paste";

    public ExplorerToolTarget Target => ExplorerToolTarget.SelectedContextExplorerObjects;

    public bool IsEnabled(IExplorerApplicationScopeService scope)
    {
        if (scope.GetClipboardItemType() == typeof(IFeatureClass))
        {
            return scope.CurrentExplorerObject?.ObjectType != null &&
                   scope.CurrentExplorerObject.ObjectType.IsAssignableTo(typeof(IFeatureDataset));
        }

        return false;
    }

    async public Task<bool> OnEvent(IExplorerApplicationScopeService scope)
    {
        if (scope.CurrentExplorerObject is null)
        {
            return false;
        }

        var destination = await scope.CurrentExplorerObject.GetInstanceAsync();
        if (destination is IFeatureDataset)
        {
            IFeatureDataset destDataset = (IFeatureDataset)destination;
            var destDatasetGuid = PlugInManager.PlugInID(destDataset);

            if (destDatasetGuid == Guid.Empty)
            {
                throw new Exception("Current dataset no not a valid gView Plugin. Copy featuresclasses is only possible to dataset plugins!");
            }

            List<CommandItem> commandItems = new();

            foreach (var featureClass in scope.GetClipboardElements<IFeatureClass>())
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

            await scope.ShowKnownDialog(
                    KnownDialogs.ExecuteCommand,
                    $"Copy {commandItems.Count} FeatureClasses",
                    new ExecuteCommandModel() { CommandItems = commandItems });

            await scope.ForceContentRefresh();
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
