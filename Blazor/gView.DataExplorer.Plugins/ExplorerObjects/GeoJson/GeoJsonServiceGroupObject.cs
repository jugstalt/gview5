using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.VectorData;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.GeoJson
{
    [RegisterPlugIn("57AF3D70-6BAD-4284-B2F7-9C0362D5F7F5")]
    public class GeoJsonServiceGroupObject : ExplorerParentObject,
                                             IVectorDataExplorerGroupObject
    {
        internal const string ConfigName = "GeoJsonService";
        internal const string EncKey = "546B0513-D71D-4490-9E27-94CD5D72C64A";

        public GeoJsonServiceGroupObject()
            : base()
        {
        }

        #region IExplorerObject Member

        public string Name => "GeoJson Service Connections";

        public string FullName => @"VectorData\GeoJsonServiceConnections";

        public string Type => "GeoJson Service Connections";

        public string Icon => "basic:code-c-box";

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
                GeoJsonServiceGroupObject exObject = new GeoJsonServiceGroupObject();
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

            base.AddChildObject(new GeoJsonServiceNewConnectionObject(this));

            ConfigConnections conStream = new ConfigConnections(ConfigName, EncKey);
            Dictionary<string, string> DbConnectionStrings = conStream.Connections;
            foreach (string name in DbConnectionStrings.Keys)
            {
                var connectionString = DbConnectionStrings[name];
                base.AddChildObject(new GeoJsonServiceExplorerObject(this, name, connectionString));
            }

            return true;
        }

        #endregion

        #region IExplorerGroupObject

        public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
        {
            base.Parent = parentExplorerObject;
        }

        #endregion
    }
}
