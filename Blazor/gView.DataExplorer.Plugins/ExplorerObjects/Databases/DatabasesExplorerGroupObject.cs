using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Databases;

[RegisterPlugIn("19C41989-E828-4AF6-87B8-629DDAB178CC")]
internal class DatabasesExplorerGroupObject : ExplorerParentObject,
                                              IExplorerGroupObject
{
    public DatabasesExplorerGroupObject() : base(10) { }

    #region IExplorerGroupObject Members

    public string Icon => "basic:database";

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion

    #region IExplorerObject Members

    public string Name => "Databases";

    public string FullName => "Databases";

    public string Type => "Databases Sources";

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
            if (!(exObject is IDatabasesExplorerGroupObject))
            {
                continue;
            }

            ((IDatabasesExplorerGroupObject)exObject).SetParentExplorerObject(this);

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
            DatabasesExplorerGroupObject exObject = new DatabasesExplorerGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

}
