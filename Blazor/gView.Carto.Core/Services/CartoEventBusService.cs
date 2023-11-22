using gView.Carto.Core.Abstractions;
using gView.Carto.Core.Extensions;
using gView.Carto.Core.Models.Tree;
using gView.Framework.Geometry;
using System;
using System.Threading.Tasks;

namespace gView.Carto.Core.Services;
public class CartoEventBusService
{
    public event Func<bool, string, Task>? OnBusyStatusChangedAsync;
    public Task FireBusyStatusChangedAsync(bool isBusy, string statusText)
        => OnBusyStatusChangedAsync?.FireAsync(isBusy, statusText) ?? Task.CompletedTask;

    public event Func<TocTreeNode?, Task>? OnSelectedTocTreeNodeChangedAsync;
    public Task FireSelectedTocTreeNodeChangedAsync(TocTreeNode? selectedTocTreeNode)
        => OnSelectedTocTreeNodeChangedAsync?.FireAsync(selectedTocTreeNode) ?? Task.CompletedTask;

    public event Func<ICartoDocument, Task>? OnCartoDocumentLoadedAsync;
    public Task FireCartoDocumentLoadedAsync(ICartoDocument cartoDocument)
        => OnCartoDocumentLoadedAsync?.FireAsync(cartoDocument) ?? Task.CompletedTask;

    public event Func<Task>? OnMapSettingsChangedAsync;
    public Task FireMapSettingsChangedAsync()
        => OnMapSettingsChangedAsync?.FireAsync() ?? Task.CompletedTask;

    public event Func<Task>? OnRefreshContentTreeAsync;
    public Task FireRefreshContentTreeAsync()
        => OnRefreshContentTreeAsync?.FireAsync() ?? Task.CompletedTask;

    public event Func<int, Task>? OnRefreshMapAsync;
    public Task FireRefreshMapAsync(int delay = 0)
        => OnRefreshMapAsync?.FireAsync(delay) ?? Task.CompletedTask;

    public event Func<IEnvelope, Task>? OnMapZoomToAsync;
    public Task FireMapZoomToAsync(IEnvelope envelope)
        => OnMapZoomToAsync?.FireAsync(envelope) ?? Task.CompletedTask;
}
