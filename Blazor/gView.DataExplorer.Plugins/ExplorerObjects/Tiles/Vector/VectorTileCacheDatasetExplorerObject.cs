using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Vector.ContextTools;
using gView.DataSources.VectorTileCache;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Vector
{
    [gView.Framework.system.RegisterPlugIn("9F64AC86-4FE0-4E34-85C5-23BFF2DB42D2")]
    public class VectorTileCacheDatasetExplorerObject : ExplorerParentObject<VectorTileCacheGroupExplorerObject, Dataset>, 
                                                        IExplorerSimpleObject, 
                                                        IExplorerObjectDeletable, 
                                                        IExplorerObjectRenamable,
                                                        ISerializableExplorerObject,
                                                        IExplorerObjectContextTools
    {
        private string _name = String.Empty, _connectionString = String.Empty;
        private Dataset? _dataset = null;
        private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

        public VectorTileCacheDatasetExplorerObject()
            : base() { }
        public VectorTileCacheDatasetExplorerObject(VectorTileCacheGroupExplorerObject parent, string name, string connectionString)
            : base(parent, 0)
        {
            _name = name;
            _connectionString = connectionString;

            _contextTools = new IExplorerObjectContextTool[]
            {
                new UpdateConnectionString()
            };
        }

        internal string GetConnectionString() => _connectionString;
        internal Task<bool> UpdateConnectionString(string connectionString)
        {
            ConfigConnections connStream = new ConfigConnections("VectorTileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");
            connStream.Add(_name, _connectionString = connectionString);

            return Task.FromResult(true);
        }

        #region IExplorerObjectContextTools

        public IEnumerable<IExplorerObjectContextTool> ContextTools => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();

        #endregion

        #region IExplorerObject Members

        public string Name => _name;

        public string FullName=>@$"Tiles\TileCache\{_name}";

        public string Type=> "Vector Tile Cache Dataset";

        public string Icon => "webgis:tiles";

        new public void Dispose()
        {
            base.Dispose();
        }
        async public Task<object?> GetInstanceAsync()
        {
            if (_dataset == null)
            {
                _dataset = new Dataset();

                if (await _dataset.SetConnectionString(_connectionString) && await _dataset.Open())
                {
                    return _dataset;
                }
            }

            return _dataset;
        }

        #endregion

        #region IExplorerParentObject Members

        async public override Task<bool> Refresh()
        {
            await base.Refresh();

            try
            {
                Dataset dataset = new Dataset();
                await dataset.SetConnectionString(_connectionString);
                await dataset.Open();

                var elements = await dataset.Elements();
                if (elements != null)
                {
                    foreach (IDatasetElement element in elements)
                    {
                        if (element.Class is IFeatureClass)
                        {
                            base.AddChildObject(new VectorTileCacheLayerExplorerObject(this, element));
                        }
                    }
                }

                return true;
            }
            catch //(Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
        {
            if (cache?.Contains(FullName) == true)
            {
                return cache[FullName];
            }

            VectorTileCacheGroupExplorerObject? group = new VectorTileCacheGroupExplorerObject();
            if (FullName.IndexOf(group.FullName) != 0 || FullName.Length < group.FullName.Length + 2)
            {
                return null;
            }

            group = (VectorTileCacheGroupExplorerObject?)((cache?.Contains(group.FullName) == true) ? cache[group.FullName] : group);

            if (group != null)
            {
                var childObjects = await group.ChildObjects();
                if (childObjects != null)
                {
                    foreach (IExplorerObject exObject in childObjects)
                    {
                        if (exObject.FullName == FullName)
                        {
                            cache?.Append(exObject);
                            return exObject;
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region IExplorerObjectDeletable Member

        public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

        public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
        {
            ConfigConnections stream = new ConfigConnections("VectorTileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");
            stream.Remove(_name);

            if (ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }

            return Task.FromResult(true);
        }

        #endregion

        #region IExplorerObjectRenamable Member

        public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

        public Task<bool> RenameExplorerObject(string newName)
        {
            bool ret = false;
            ConfigConnections stream = new ConfigConnections("VectorTileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");
            ret = stream.Rename(_name, newName);

            if (ret == true)
            {
                _name = newName;
                if (ExplorerObjectRenamed != null)
                {
                    ExplorerObjectRenamed(this);
                }
            }

            return Task.FromResult(ret);
        }

        #endregion
    }
}
