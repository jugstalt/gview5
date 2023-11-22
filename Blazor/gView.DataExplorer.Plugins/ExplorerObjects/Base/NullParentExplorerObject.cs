using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base;

public class NullParentExplorerObject : IExplorerParentObject, 
                                        IExplorerObject
{
    #region IExplorerObject

    public string Name => "";

    public string FullName => "";

    public string? Type => "";

    public string Icon => "";

    public IExplorerObject? ParentExplorerObject => null;

    public Type? ObjectType => null;

    public int Priority => 0;

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject

    public Task<IEnumerable<IExplorerObject>> ChildObjects()
        => Task.FromResult<IEnumerable<IExplorerObject>>(Array.Empty<IExplorerObject>());

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DiposeChildObjects()
        => Task.FromResult(true);

    public Task<bool> Refresh()
        => Task.FromResult(true);

    public bool RequireRefresh() => false;

    public bool HandleRefreshException(Exception exception) => true;

    #endregion

}
