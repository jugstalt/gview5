using gView.Blazor.Core.Extensions.DependencyInjection;
using gView.Carto.Plugins.Extensions.DependencyInjection;
using gView.Carto.Web.Components;
using gView.DataExplorer.Plugins.Extensions.DependencyInjection;
using gView.Razor.Leaflet.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

builder.Services.AddExplorerDesktopApplicationService();
builder.Services.AddCartoDesktopApplicationService();
builder.Services.AddCartoApplicationScopeService(config =>
{
    config.ConfigRootPath = Path.Combine("C:\\temp", "gview-explorer");
});

builder.Services.AddKnownExplorerDialogsServices();
builder.Services.AddFrameworkServices();
builder.Services.AddIconService();
builder.Services.AddLeafletService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

gView.GraphicsEngine.Current.UseSecureDisposingOnUserInteractiveUIs = true;
gView.GraphicsEngine.Current.Engine = new gView.GraphicsEngine.Skia.SkiaGraphicsEngine(96.0f);
//gView.GraphicsEngine.Current.Encoder = new gView.GraphicsEngine.GdiPlus.GdiBitmapEncoding();
gView.GraphicsEngine.Current.Encoder = new gView.GraphicsEngine.Skia.SkiaBitmapEncoding();

app.Run();
