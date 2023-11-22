using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Core.Models.Tree;

class DummyExplorerObject : IExplorerObject
{
    public string Name => string.Empty;

    public string FullName => string.Empty;

    public string? Type => string.Empty;

    public string Icon => string.Empty;

    public IExplorerObject? ParentExplorerObject => null;

    public Type? ObjectType => null;

    public int Priority => 0;

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        return Task.FromResult<IExplorerObject?>(null);
    }

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }
}
