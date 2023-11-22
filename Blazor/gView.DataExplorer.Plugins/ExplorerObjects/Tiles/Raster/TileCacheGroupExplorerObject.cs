using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Raster;

[RegisterPlugIn("0d217509-0df9-4b8d-bd3b-b7390de5abde")]
public class TileCacheGroupExplorerObject : ExplorerParentObject,
                                            ITilesExplorerGroupObject
{
    public TileCacheGroupExplorerObject()
        : base()
    {
    }

    #region IExplorerObject Member

    public string Name => "Raster Tile Caches";

    public string FullName => @"Tiles\RasterTileCache";

    public string Type => "Raster Tile Cache Connections";

    public string Icon => "webgis:tiles";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        if (this.FullName == FullName)
        {
            TileCacheGroupExplorerObject exObject = new TileCacheGroupExplorerObject();
            cache?.Append(exObject);

            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        base.AddChildObject(new TileCacheNewConnectionObject(this));
        base.AddChildObject(new TileCacheLocalCacheProperties(this));

        ConfigConnections conStream = new ConfigConnections("TileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");
        Dictionary<string, string> connStrings = conStream.Connections;
        foreach (string connString in connStrings.Keys)
        {
            base.AddChildObject(new TileCacheDatasetExplorerObject(this, connString, connStrings[connString]));
        }

        return true;
    }

    #endregion

    #region ITilesExplorerGroupObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
