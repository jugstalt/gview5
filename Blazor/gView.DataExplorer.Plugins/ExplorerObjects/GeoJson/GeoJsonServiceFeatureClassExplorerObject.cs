using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Geometry;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.GeoJson
{
    [gView.Framework.system.RegisterPlugIn("368020E0-216E-46B8-8DAD-BAAC38D75B92")]
    public class GeoJsonServiceFeatureClassExplorerObject : ExplorerObjectCls<GeoJsonServiceExplorerObject, IFeatureClass>,
                                                            IExplorerSimpleObject,
                                                            ISerializableExplorerObject
    {
        private string _fcname = "", _type = "", _icon = "";
        private IFeatureClass? _fc;

        public GeoJsonServiceFeatureClassExplorerObject() : base() { }
        public GeoJsonServiceFeatureClassExplorerObject(GeoJsonServiceExplorerObject parent, IDatasetElement element)
            : base(parent, 1)
        {
            if (element == null || !(element.Class is IFeatureClass))
            {
                return;
            }

            _fcname = element.Title;

            if (element.Class is IFeatureClass)
            {
                _fc = (IFeatureClass)element.Class;
                switch (_fc.GeometryType)
                {
                    case GeometryType.Envelope:
                    case GeometryType.Polygon:
                        _icon = "webgis:shape-polygon";
                        _type = "Polygon Featureclass";
                        break;
                    case GeometryType.Multipoint:
                    case GeometryType.Point:
                        _icon = "basic:dot-filled";
                        _type = "Point Featureclass";
                        break;
                    case GeometryType.Polyline:
                        _icon = "webgis:shape-polyline";
                        _type = "Polyline Featureclass";
                        break;
                }
            }
        }

        internal string ConnectionString
        {
            get
            {
                if (TypedParent == null)
                {
                    return "";
                }

                return TypedParent.GetConnectionString();
            }
        }

        #region IExplorerObject Members

        public string Name=> _fcname;

        public string FullName
        {
            get
            {
                if (Parent == null)
                {
                    return "";
                }

                return $@"{Parent.FullName}\{this.Name}";
            }
        }
        public string Type => _type;

        public string Icon=> _icon;

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

            string dsName = FullName.Substring(0, lastIndex);
            string fcName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

            GeoJsonServiceExplorerObject? dsObject = new GeoJsonServiceExplorerObject();
            dsObject = await dsObject.CreateInstanceByFullName(dsName, cache) as GeoJsonServiceExplorerObject;

            if (dsObject == null)
            {
                return null;
            }

            var childObjects = await dsObject.ChildObjects();
            if (childObjects == null)
            {
                return null;
            }

            foreach (IExplorerObject exObject in childObjects)
            {
                if (exObject.Name == fcName)
                {
                    cache?.Append(exObject);
                    return exObject;
                }
            }
            return null;
        }

        #endregion
    }
}
