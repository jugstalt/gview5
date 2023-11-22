using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.VectorData;

[RegisterPlugIn("7633E43A-FDBF-464A-B332-139C46A6B582")]
public class VectorDataExplorerGroupObject :
                ExplorerParentObject,
                IExplorerGroupObject
{
    public VectorDataExplorerGroupObject() : base(30) { }

    #region IExplorerGroupObject Members

    public string Icon => "webgis:construct-ortho";

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion

    #region IExplorerObject Members

    public string Name => "Vector Data";

    public string FullName => "VectorData";

    public string Type => "Vector Data Sources";

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
            if (!(exObject is IVectorDataExplorerGroupObject))
            {
                continue;
            }

            ((IVectorDataExplorerGroupObject)exObject).SetParentExplorerObject(this);

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
            VectorDataExplorerGroupObject exObject = new VectorDataExplorerGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
