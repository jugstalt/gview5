using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.SqLite;

[RegisterPlugIn("EEDCCBB2-588E-418A-B048-4B6C210A25AE")]
public class SqLiteFdbLinkedFeatureclassExplorerObject : IExplorerSimpleObject,
                                                         IExplorerObjectCreatable
{
    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return parentExObject is SqLiteFdbDatasetExplorerObject;
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope scope, IExplorerObject? parentExObject)
    {
        SqLiteFdbDatasetExplorerObject? parent = (SqLiteFdbDatasetExplorerObject?)parentExObject;
        if (parent == null)
        {
            return null;
        }

        IFeatureDataset? dataset = await parent.GetInstanceAsync() as IFeatureDataset;
        if (dataset == null)
        {
            return null;
        }

        AccessFDB? fdb = dataset.Database as AccessFDB;
        if (fdb == null)
        {
            return null;
        }

        var model = await scope.ToExplorerScopeService().ShowKnownDialog(
            Framework.Blazor.KnownDialogs.ExplorerDialog,
            model: new ExplorerDialogModel()
            {
                Filters = new List<ExplorerDialogFilter> {
                    new OpenFeatureclassFilter()
                },
                Mode = ExploerDialogMode.Open
            });

        if (model?.Result?.ExplorerObjects != null )
        {
            //IExplorerObject? ret = null;

            foreach (var exObject in model.Result.ExplorerObjects)
            {
                var exObjectInstance = await exObject.GetInstanceAsync();
                if (exObjectInstance is IFeatureClass)
                {
                    int fcid = await fdb.CreateLinkedFeatureClass(dataset.DatasetName, (IFeatureClass)exObjectInstance);
                }
            }

            await scope.ToExplorerScopeService().ForceContentRefresh();
        }

        return null;
    }

    #endregion

    #region IExplorerObject Member

    public string Name
    {
        get { return "Linked Featureclass"; }
    }

    public string FullName
    {
        get { return "Linked Featureclass"; }
    }

    public string Type
    {
        get { return "Linked Featureclass"; }
    }

    public string Icon => "basic:open-in-window";

    public IExplorerObject? ParentExplorerObject
    {
        get { return null; }
    }

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }

    public Type? ObjectType
    {
        get { return null; }
    }

    public int Priority { get { return 1; } }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {

    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
