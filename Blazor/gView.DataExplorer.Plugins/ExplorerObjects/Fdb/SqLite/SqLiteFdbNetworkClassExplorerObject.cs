using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.Extensions;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.SqLite;

[RegisterPlugIn("AFDE90FF-D063-4224-BD31-1B30C266D55B")]
public class SqLiteFdbNetworkClassExplorerObject : ExplorerObjectCls<IExplorerObject, SQLiteFDBNetworkFeatureClass>,
                                                   IExplorerSimpleObject,
                                                   IExplorerObjectCreatable
{
    public SqLiteFdbNetworkClassExplorerObject() : base(1) { }

    #region IExplorerObject Member

    public string Name
    {
        get { return String.Empty; }
    }

    public string FullName
    {
        get { return String.Empty; }
    }

    public string Type
    {
        get { return "Network Class"; }
    }

    public string Icon => "webgis:construct-edge-intersect";

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }

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

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        if (parentExObject is SqLiteFdbDatasetExplorerObject)
        {
            return true;
        }

        return false;
    }

    public async Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope scope, IExplorerObject? parentExObject)
    {
        if (parentExObject == null)
        {
            throw new ArgumentNullException(nameof(parentExObject));
        }

        var scopeService = scope.ToExplorerScopeService();

        if (await scopeService.CreateNetworkClass(parentExObject))
        {
            await scopeService.ForceContentRefresh();
        }

        return null;
    }

    #endregion
}
