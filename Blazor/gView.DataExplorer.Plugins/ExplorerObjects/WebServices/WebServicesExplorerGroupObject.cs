using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.WebServices;

[RegisterPlugIn("9651F17B-F7F9-433F-8811-0A7486A81BFE")]
public class WebServicesExplorerGroupObject :
                    ExplorerParentObject,
                    IExplorerGroupObject
{
    public WebServicesExplorerGroupObject() : base(20) { }

    #region IExplorerGroupObject Members

    public string Icon => "basic:globe-table";

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion

    #region IExplorerObject Members

    public string Name => "WebServices";

    public string FullName => "WebServices";

    public string Type => "Web Service Connections";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        PlugInManager compMan = new PlugInManager();
        foreach (var compType in compMan.GetPlugins(gView.Framework.system.Plugins.Type.IExplorerObject))
        {
            IExplorerObject exObject = compMan.CreateInstance<IExplorerObject>(compType);
            if (!(exObject is IWebServicesExplorerGroupObject))
            {
                continue;
            }

            ((IWebServicesExplorerGroupObject)exObject).SetParentExplorerObject(this);

            base.AddChildObject(exObject);
        }

        return true;
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        if (this.FullName == FullName)
        {
            WebServicesExplorerGroupObject exObject = new WebServicesExplorerGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
