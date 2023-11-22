using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Vector
{
    [gView.Framework.system.RegisterPlugIn("AD895DDE-6951-4B9B-87B5-298767AF06D2")]
    public class VectorTileCacheGroupExplorerObject : ExplorerParentObject,
                                                      ITilesExplorerGroupObject
    {
        public VectorTileCacheGroupExplorerObject()
            : base()
        {
        }

        #region IExplorerObject Member

        public string Name => "Vector Tile Caches";

        public string FullName => @"Tiles\VectorTileCache";

        public string Type => "Vector Tile Cache Connections";

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
                VectorTileCacheGroupExplorerObject exObject = new VectorTileCacheGroupExplorerObject();
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

            base.AddChildObject(new VectorTileCacheNewConnectionObject(this));

            ConfigConnections conStream = new ConfigConnections("VectorTileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");
            Dictionary<string, string> connStrings = conStream.Connections;
            foreach (string connString in connStrings.Keys)
            {
                base.AddChildObject(new VectorTileCacheDatasetExplorerObject(this, connString, connStrings[connString]));
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
}
