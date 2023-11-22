using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles;

[RegisterPlugIn("B57956A7-A49A-4821-A6B2-83CFB97B1373")]
public class TilesExplorerGroupObject :
                ExplorerParentObject,
                IExplorerGroupObject
{
    public TilesExplorerGroupObject() : base(40) { }

    #region IExplorerGroupObject Members

    public string Icon => "webgis:tiles";

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion

    #region IExplorerObject Members

    public string Name => "Tiles";

    public string FullName => "Tiles";

    public string Type => "Tile Caches";

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
            if (!(exObject is ITilesExplorerGroupObject))
            {
                continue;
            }

            ((ITilesExplorerGroupObject)exObject).SetParentExplorerObject(this);

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
            TilesExplorerGroupObject exObject = new TilesExplorerGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
