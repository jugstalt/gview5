using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects;
internal class ExceptionObject : IExplorerObject
{
    private readonly Exception _exception;
    public ExceptionObject(Exception exception)
    {
        _exception = exception;
    }

    public string Name => "Exception";

    public string FullName => "";

    public string? Type => _exception.Message;

    public string Icon => "basic:warning_red";

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

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);
}
