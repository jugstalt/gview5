using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Geometry;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

[RegisterPlugIn("137ab461-6e7b-4c43-8be2-515c9ca475d2")]
public class OgrLayerExplorerObject : ExplorerObjectCls<OgrDatasetExplorerObject, IFeatureClass>, 
                                      IExplorerSimpleObject, 
                                      ISerializableExplorerObject
{
    private string _fcname = "", _type = "", _icon = "";
    private IFeatureClass? _fc = null;

    public OgrLayerExplorerObject() : base() { }
    public OgrLayerExplorerObject(OgrDatasetExplorerObject parent, IDatasetElement element)
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
                default:
                    _icon = "webgis:shape-polyline";
                    _type = "Featureclass";
                    break;
            }
        }
    }

    #region IExplorerObject Members

    public string Name => _fcname;

    public string FullName => @$"{Parent.FullName}\{_fcname}";

    public string Type => _type;

    public string Icon => _icon;

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

        OgrDatasetExplorerObject? parent = new OgrDatasetExplorerObject();
        parent = await parent.CreateInstanceByFullName(parts[0] + @"\" + parts[1], cache) as OgrDatasetExplorerObject;
        if (parent == null)
        {
            return null;
        }

        foreach (IExplorerObject exObject in await parent.ChildObjects())
        {
            if (exObject.Name == parts[2])
            {
                cache?.Append(exObject);
                return exObject;
            }
        }
        return null;
    }

    #endregion
}
