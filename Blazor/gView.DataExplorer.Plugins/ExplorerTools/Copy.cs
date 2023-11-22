using gView.DataExplorer.Plugins.Extensions;
using gView.Framework.Blazor.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[gView.Framework.system.RegisterPlugIn("A0204C3B-74A0-4339-A99B-A39C0749E2AC")]
internal class Copy : IExplorerTool
{
    public string Name => "Copy";

    public string ToolTip => "";

    public string Icon => "basic:copy";

    public ExplorerToolTarget Target => ExplorerToolTarget.SelectedContextExplorerObjects;

    public bool IsEnabled(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        return scopeService.ContextExplorerObjects?
            .Where(e => e.ObjectType != null && e.ObjectType.IsAssignableTo(typeof(IFeatureClass)))
            .Count() > 0;
    }

    public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        var featureClasses = scopeService.ContextExplorerObjects?
            .Where(e => e.ObjectType != null && (e.ObjectType is IFeatureClass || e.ObjectType.IsAssignableTo(typeof(IFeatureClass))))
            .Select(async e => await e.GetInstanceAsync())
            .Select(e => e.Result as IFeatureClass)
            .Where(e => e != null)
            .ToArray();

        if (featureClasses != null && featureClasses.Any())
        {
            scopeService.SetClipboardItem(new ClipboardItem(typeof(IFeatureClass))
            {
                Elements = featureClasses
            });

            scopeService.AddToSnackbar("Featureclass(es) in Clipbard. Navigate to Featuredataset and click Paste-Button to copy...");

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    #region IDisposable

    public void Dispose()
    {

    }

    #endregion

    #region IOrder

    public int SortOrder => 21;

    #endregion
}
