using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.ToolCommands;
class ToolObject : ExplorerObjectCls<IExplorerObject, IExplorerToolCommand>,
                   IExplorerObject,
                   IExplorerObjectDoubleClick
{
    private IExplorerToolCommand? _toolCommand;

    public ToolObject() : base() { }
    public ToolObject(IExplorerObject parent, IExplorerToolCommand toolCommand)
        : base(parent, 1)
    {
        _toolCommand = toolCommand;
    }

    #region IExplorerObject Members

    public string Name => _toolCommand?.Name ?? "Unknown";

    public string FullName => $"Tools/{Name}";
    public string Type => _toolCommand?.ToolTip ?? "";

    public string Icon => String.IsNullOrEmpty(_toolCommand?.Icon) ? "basic:tools" : _toolCommand.Icon;

    public void Dispose()
    {
    }

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }

    public Task<IExplorerObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        if (_toolCommand == null)
        {
            return Task.FromResult<IExplorerObject?>(null);
        }

        return Task.FromResult<IExplorerObject?>(new ToolObject(parent, _toolCommand));
    }

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        try
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region IExplorerObjectDoubleClick

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        if (_toolCommand != null)
        {
            await _toolCommand.OnEvent(appScope);
        }
    }

    #endregion
}
