using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.GeoJson.ContextTools;
using gView.DataSources.GeoJson;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.GeoJson
{
    public class GeoJsonServiceExplorerObject : ExplorerParentObject<GeoJsonServiceGroupObject, IFeatureDataset>,
                                                IExplorerSimpleObject,
                                                IExplorerObjectDeletable,
                                                IExplorerObjectRenamable,
                                                ISerializableExplorerObject,
                                                IExplorerObjectContextTools
    {
        private string _connectionString = "";
        private string _name = "";
        private IFeatureDataset? _dataset;
        private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

        public GeoJsonServiceExplorerObject()
            : base() { }

        public GeoJsonServiceExplorerObject(GeoJsonServiceGroupObject parent, string name, string connectionString)
            : base(parent, 1)
        {
            _name = name;
            _connectionString = connectionString;

            _contextTools = new IExplorerObjectContextTool[]
            {
                new UpdateConnectionString()
            };
        }

        internal string GetConnectionString()
        {
            return _connectionString;
        }

        internal Task<bool> UpdateConnectionString(string connectionString)
        {
            ConfigConnections connStream = new ConfigConnections(GeoJsonServiceGroupObject.ConfigName, GeoJsonServiceGroupObject.EncKey);
            connStream.Add(_name, connectionString);

            _connectionString = connectionString;

            return Task.FromResult(true);
        }

        #region IExplorerObjectContextTools

        public IEnumerable<IExplorerObjectContextTool> ContextTools => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();

        #endregion

        #region IExplorerObject Members

        public string Name => _name;

        public string FullName => @$"{this.Parent.FullName}\{_name}";

        public string Type => "GeoJsonService Connection";

        public string Icon => "basic:code-c";

        async public Task<object?> GetInstanceAsync()
        {
            if (_dataset != null)
            {
                _dataset.Dispose();
            }

            _dataset = new GeoJsonServiceDataset();
            await _dataset.SetConnectionString(_connectionString);
            await _dataset.Open();

            return _dataset;
        }

        #endregion

        #region IExplorerParentObject Members

        async public override Task<bool> Refresh()
        {
            await base.Refresh();
            if (_connectionString == null)
            {
                return false;
            }

            var dataset = new GeoJsonServiceDataset();
            await dataset.SetConnectionString(_connectionString);
            await dataset.Open();

            List<IDatasetElement> elements = await dataset.Elements();

            if (elements == null)
            {
                return false;
            }

            foreach (IDatasetElement element in elements)
            {
                if (element.Class is IFeatureClass)
                {
                    base.AddChildObject(new GeoJsonServiceFeatureClassExplorerObject(this, element));
                }
            }

            return true;
        }

        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
        {
            if (cache?.Contains(FullName) == true)
            {
                return cache[FullName];
            }

            GeoJsonServiceGroupObject? group = new GeoJsonServiceGroupObject();
            if (FullName.IndexOf(group.FullName) != 0 || FullName.Length < group.FullName.Length + 2)
            {
                return null;
            }

            group = (GeoJsonServiceGroupObject?)((cache?.Contains(group.FullName) == true) ? cache[group.FullName] : group);

            if (group != null)
            {
                foreach (IExplorerObject exObject in await group.ChildObjects())
                {
                    if (exObject.FullName == FullName)
                    {
                        cache?.Append(exObject);
                        return exObject;
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
            //ConfigTextStream stream = new ConfigTextStream("postgis_connections", true, true);
            //stream.Remove(this.Name, _connectionString);
            //stream.Close();
            //if (ExplorerObjectDeleted != null) ExplorerObjectDeleted(this);
            //return true;

            bool ret = false;
            if (_connectionString != null)
            {
                ConfigConnections stream = new ConfigConnections(GeoJsonServiceGroupObject.ConfigName, GeoJsonServiceGroupObject.EncKey);
                ret = stream.Remove(_name);
            }

            if (ret && ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }

            return Task.FromResult(ret);
        }

        #endregion

        #region IExplorerObjectRenamable Member

        public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

        public Task<bool> RenameExplorerObject(string newName)
        {
            bool ret = false;
            if (_connectionString != null)
            {
                ConfigConnections stream = new ConfigConnections(GeoJsonServiceGroupObject.ConfigName, GeoJsonServiceGroupObject.EncKey);
                ret = stream.Rename(_name, newName);
            }
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
