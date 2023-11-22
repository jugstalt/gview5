using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Vector
{
    [gView.Framework.system.RegisterPlugIn("23781302-2B04-4ECF-82A8-246A0C0DCA42")]
    public class VectorTileCacheLayerExplorerObject : ExplorerObjectCls<VectorTileCacheDatasetExplorerObject, FeatureClass>, 
                                                      IExplorerSimpleObject, 
                                                      ISerializableExplorerObject
    {
        private string _fcname = "";
        private IFeatureClass? _fc;

        public VectorTileCacheLayerExplorerObject() : base() { }
        public VectorTileCacheLayerExplorerObject(VectorTileCacheDatasetExplorerObject parent, IDatasetElement element)
            : base(parent, 1)
        {
            if (element == null)
            {
                return;
            }

            _fcname = element.Title;

            if (element.Class is IFeatureClass)
            {
                _fc = (IFeatureClass)element.Class;

            }
        }

        #region IExplorerObject Members

        public string Name=> _fcname;

        public string FullName => $@"{Parent.FullName}\{_fcname}";
        public string Type => "Vector Tile Cache Layer";

        public string Icon => "webgis:layer-middle";

        public void Dispose()
        {
            if (_fc != null)
            {
                _fc = null;
            }
        }
        public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(_fc);
        
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

            var parent = new VectorTileCacheDatasetExplorerObject();

            parent = await parent.CreateInstanceByFullName(parts[0] + @"\" + parts[1], cache) as VectorTileCacheDatasetExplorerObject;

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
}
