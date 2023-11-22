using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Raster;

[RegisterPlugIn("89676c37-faad-4b18-b879-a170dbf146e7")]
public class TileCacheLayerExplorerObject : ExplorerObjectCls<TileCacheDatasetExplorerObject, IRasterClass>,
                                            IExplorerSimpleObject,
                                            ISerializableExplorerObject
{
    private string _rcName = "";
    private IRasterClass? _rc;

    public TileCacheLayerExplorerObject() : base() { }
    public TileCacheLayerExplorerObject(TileCacheDatasetExplorerObject parent, IDatasetElement element)
        : base(parent, 1)
    {
        if (element == null)
        {
            return;
        }

        _rcName = element.Title;

        if (element.Class is IRasterClass)
        {
            _rc = (IRasterClass)element.Class;

        }
    }

    #region IExplorerObject Members

    public string Name
    {
        get { return _rcName; }
    }

    public string FullName => $@"{Parent.FullName}\{_rcName}";

    public string Type => "Tile Cache Raster";

    public string Icon => "webgis:layer-middle";

    public void Dispose()
    {
        if (_rc != null)
        {
            _rc = null;
        }
    }
    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(_rc);

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return cache[FullName];
        }

        FullName = FullName.Replace("/", @"\");
        int lastIndex = FullName.LastIndexOf(@"\");
        if (lastIndex == -1)
        {
            return null;
        }

        string[] parts = FullName.Split('\\');
        if (parts.Length != 3)
        {
            return null;
        }

        TileCacheDatasetExplorerObject? parent = new TileCacheDatasetExplorerObject();

        parent = await parent.CreateInstanceByFullName(parts[0] + @"\" + parts[1], cache) as TileCacheDatasetExplorerObject;

        if (parent == null)
        {
            return null;
        }

        var childObjects = await parent.ChildObjects();
        if (childObjects != null)
        {
            foreach (IExplorerObject exObject in childObjects)
            {
                if (exObject.Name == parts[2])
                {
                    cache?.Append(exObject);
                    return exObject;
                }
            }
        }
        return null;
    }

    #endregion
}
