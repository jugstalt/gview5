using gView.Blazor.Core.Exceptions;
using gView.Blazor.Core.Services;
using gView.Blazor.Core.Services.Abstraction;
using gView.Carto.Core;
using gView.Carto.Core.Abstractions;
using gView.Carto.Core.Models.Tree;
using gView.Carto.Core.Services;
using gView.Carto.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using gView.Framework.Blazor.Services;
using gView.Framework.Blazor.Services.Abstraction;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;

namespace gView.Carto.Plugins.Services;
public class CartoApplicationScopeService : ApplictionBusyHandler, IApplicationScope, ICartoApplicationScopeService
{
    private readonly IDialogService _dialogService;
    private readonly IEnumerable<IKnownDialogService> _knownDialogs;
    private readonly CartoEventBusService _eventBus;
    private readonly IJSRuntime _jsRuntime;
    private readonly ISnackbar _snackbar;
    private readonly CartoApplicationScopeServiceOptions _options;
    private readonly GeoTransformerService _geoTransformer;

    private ICartoDocument _cartoDocument;

    public CartoApplicationScopeService(IDialogService dialogService,
                                        IEnumerable<IKnownDialogService> knownDialogs,
                                        CartoEventBusService eventBus,
                                        IJSRuntime jsRuntime,
                                        ISnackbar snackbar,
                                        GeoTransformerService geoTransformer,
                                        IOptions<CartoApplicationScopeServiceOptions> options)
    {
        _dialogService = dialogService;
        _knownDialogs = knownDialogs;
        _eventBus = eventBus;
        _jsRuntime = jsRuntime;
        _snackbar = snackbar;
        _geoTransformer = geoTransformer;
        _options = options.Value;

        _cartoDocument = this.Document = new CartoDocument();
    }

    public CartoEventBusService EventBus => _eventBus;

    public ICartoDocument Document
    {
        get
        {
            return _cartoDocument;
        }
        set
        {
            _cartoDocument = value;

            _eventBus.FireCartoDocumentLoadedAsync(value);
        }
    }

    public TocTreeNode? SelectedTocTreeNode { get; private set; }

    public Task SetSelectedTocTreeNode(TocTreeNode? selectedTocTreeNode)
        => _eventBus.FireSelectedTocTreeNodeChangedAsync(this.SelectedTocTreeNode = selectedTocTreeNode);


    public GeoTransformerService GeoTransformer => _geoTransformer;

    #region IDisposable

    public void Dispose()
    {

    }

    #endregion

    #region IApplicationScope

    public IEnumerable<T> GetClipboardElements<T>()
    {
        throw new NotImplementedException();
    }

    public Type? GetClipboardItemType()
    {
        throw new NotImplementedException();
    }

    public string GetToolConfigFilename(params string[] paths)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetToolConfigFiles(params string[] paths)
    {
        throw new NotImplementedException();
    }

    public void SetClipboardItem(ClipboardItem item)
    {
        throw new NotImplementedException();
    }

    async public Task<T?> ShowModalDialog<T>(Type razorComponent,
                                             string title,
                                             T? model = default(T),
                                             ModalDialogOptions? modalDialogOptions = null)
    {
        IDialogReference? dialog = null;
        T? result = default(T);

        var dialogParameters = new DialogParameters
        {
            { "OnDialogClose", new EventCallback<Blazor.Models.Dialogs.DialogResult>(null, OnClose) },
            //{ "OnClose", new EventCallback<Blazor.Models.Dialogs.DialogResult>(null, OnClose) }
        };

        if (model != null)
        {
            dialogParameters.Add("Model", model);
        }

        var dialogOptions = new DialogOptions()
        {
            DisableBackdropClick = true,
            CloseButton = modalDialogOptions?.ShowCloseButton ?? true,
            MaxWidth = modalDialogOptions?.Width switch
            {
                ModalDialogWidth.ExtraExtraLarge => MaxWidth.ExtraExtraLarge,
                ModalDialogWidth.Large => MaxWidth.Large,
                ModalDialogWidth.Medium => MaxWidth.Medium,
                ModalDialogWidth.Small => MaxWidth.Small,
                ModalDialogWidth.ExtraSmall => MaxWidth.ExtraSmall,
                _ => MaxWidth.ExtraLarge
            },
            CloseOnEscapeKey = modalDialogOptions?.CloseOnEscapeKey ?? false,
            FullWidth = modalDialogOptions?.FullWidth ?? false,
        };

        dialog = await _dialogService.ShowAsync(razorComponent, title, dialogParameters, dialogOptions);
        var dialogResult = await dialog.Result;

        if (dialogResult != null && !dialogResult.Canceled)
        {
            return result;
        }

        return default(T?);

        void OnClose(Blazor.Models.Dialogs.DialogResult? data)
        {
            if (data?.Result is T)
            {
                result = (T)data.Result;
            }

            if (dialog != null)
            {
                dialog.Close();
            }
        }
    }

    async public Task<T?> ShowKnownDialog<T>(KnownDialogs dialog,
                                             string? title = null,
                                             T? model = default)
    {
        var knownDialog = _knownDialogs.Where(d => d.Dialog == dialog).FirstOrDefault();

        if (knownDialog == null)
        {
            throw new GeneralException($"Dialog {dialog} is not registered as know dialog");
        }

        model = model ?? Activator.CreateInstance<T>();

        if (model == null)
        {
            throw new GeneralException($"Can't create dialog model");
        }

        return await ShowModalDialog(knownDialog.RazorType,
                                     title ?? knownDialog.Title,
                                     model,
                                     knownDialog.DialogOptions);
    }

    #endregion

    #region Busy Context

    override protected Task HandleBusyStatusChanged(bool isBussy, string message)
        => _eventBus.FireBusyStatusChangedAsync(isBussy, message);

    override async protected ValueTask SetBusyCursor()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsyncIgnoreErrors("window.gview_base.setCursor", "wait");
        }
        catch { }
    }
    override async protected ValueTask SetDefaultCursor()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsyncIgnoreErrors("window.gview_base.setCursor", "default");
        }
        catch { }
    }

    #endregion
}
