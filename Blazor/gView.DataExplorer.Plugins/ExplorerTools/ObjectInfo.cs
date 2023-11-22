using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[gView.Framework.system.RegisterPlugIn("6B0B060E-45BC-4686-BEDE-23E61C849814")]
internal class ObjectInfo : IExplorerTool
{
    #region IExplorerTool

    public string Name => "Info";

    public string ToolTip => "";

    public string Icon => "webgis:identify";

    public ExplorerToolTarget Target => ExplorerToolTarget.SelectedContextExplorerObjects;

    public bool IsEnabled(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        if (scopeService.ContextExplorerObjects != null &&
            scopeService.ContextExplorerObjects.Count() == 1)
        {
            return true;
            //var pluginAttribute = scopeService.ContextExplorerObjects.First()
            //                                .ObjectType
            //                                .GetCustomAttribute<RegisterPlugInAttribute>();

            //return pluginAttribute != null;
        }

        return false;
    }

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        if (scopeService.ContextExplorerObjects is not null &&
            scopeService.ContextExplorerObjects.Count() == 1)
        {
            var exObject = scopeService.ContextExplorerObjects.First();
            var instance = await exObject.GetInstanceAsync();

            var pluginAttribute = instance?.GetType()
                                           .GetCustomAttribute<RegisterPlugInAttribute>();

            ObjectInfoModel model = new ObjectInfoModel() { ExplorerObject = exObject };

            if (instance is not null)
            {
                model.Properties.Add("TypeName", instance.GetType().Name);
                model.Properties.Add("Type", instance.GetType().ToString());
            }

            if (pluginAttribute is not null)
            {
                model.Properties.Add("Plugin-Guid", pluginAttribute.Value.ToString());
            }

            if (instance is IDataset)
            {
                model.Properties.Add("ConnectionString", ((IDataset)instance).ConnectionString);
            }

            await scopeService.ShowModalDialog(
                typeof(Razor.Components.Dialogs.ObjectInfoDialog),
                this.Name,
                model);
        }

        return true;
    }

    #endregion

    #region IOrder

    public int SortOrder => 16;

    #endregion

    #region IDisposable

    public void Dispose()
    {

    }

    #endregion
}
