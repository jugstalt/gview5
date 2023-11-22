using gView.DataExplorer.Core.Extensions;
using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Core.Services;

public class ExplorerEventBusService
{
    public event Func<Task>? OnRefreshContentAsync;
    public Task FireSoftRefreshContentAsync()
        => OnRefreshContentAsync?.FireAsync() ?? Task.CompletedTask;

    public event Func<IExplorerObject?, Task>? OnCurrentExplorerObjectChanged;
    public Task FireCurrentExplorerObjectChanged(IExplorerObject? item)
        => OnCurrentExplorerObjectChanged?.FireAsync(item) ?? Task.CompletedTask;

    public event Func<IExplorerObject?, Task>? OnSetCurrentExplorerObjectAsync;
    public Task SetCurrentExplorerObjectAsync(IExplorerObject? item)
        => OnSetCurrentExplorerObjectAsync?.FireAsync(item) ?? Task.CompletedTask;

    public event Func<IEnumerable<IExplorerObject>?, Task>? OnContextExplorerObjectsChanged;
    public Task FireContextExplorerObjectsChanged(IEnumerable<IExplorerObject>? item)
        => OnContextExplorerObjectsChanged?.FireAsync(item) ?? Task.CompletedTask;

    public event Func<IExplorerTabPage?, IExplorerTabPage?, Task>? OnExplorerTabPageChanged;
    public Task FireExplorerTabPageChanged(IExplorerTabPage? newTabPage, IExplorerTabPage? oldTabPage)
        => OnExplorerTabPageChanged?.FireAsync(newTabPage, oldTabPage) ?? Task.CompletedTask;

    public event Func<bool, string, Task>? OnBusyStatusChangedAsync;
    public Task FireBusyStatusChanged(bool isBusy, string statusText)
        => OnBusyStatusChangedAsync?.FireAsync(isBusy, statusText) ?? Task.CompletedTask;
}
