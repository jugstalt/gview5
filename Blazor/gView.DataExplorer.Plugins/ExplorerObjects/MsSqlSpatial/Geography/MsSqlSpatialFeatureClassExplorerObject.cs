using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Extensions;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.MsSqlSpatial.Geography;

[RegisterPlugIn("F600FA07-2DF7-478A-B683-E8B63D4A298D")]
public class MsSqlSpatialFeatureClassExplorerObject : 
                        ExplorerObjectCls<MsSqlSpatialExplorerObject, IFeatureClass>, 
                        IExplorerSimpleObject, 
                        ISerializableExplorerObject, 
                        IExplorerObjectDeletable
{
    private string _icon = "";
    private string _fcname = "", _type = "";
    private IFeatureClass? _fc = null;

    public MsSqlSpatialFeatureClassExplorerObject() : base() { }
    public MsSqlSpatialFeatureClassExplorerObject(MsSqlSpatialExplorerObject parent, IDatasetElement element)
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
                default:
                    _icon = "webgis:shape-polyline";
                    _type = "Polyline Featureclass";
                    break;
            }
        }
    }

    #region IExplorerObject Members

    public string Name=>_fcname; 

    public string FullName
    {
        get
        {
            if (base.Parent.IsNull())
            {
                return "";
            }

            return base.Parent.FullName + @"\" + this.Name;
        }
    }
    public string Type=> _type;

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
        if (cache != null && cache.Contains(FullName))
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

        MsSqlSpatialExplorerObject? dsObject = new MsSqlSpatialExplorerObject();
        dsObject = await dsObject.CreateInstanceByFullName(dsName, cache) as MsSqlSpatialExplorerObject;
        if (dsObject == null || await dsObject.ChildObjects() == null)
        {
            return null;
        }

        foreach (IExplorerObject exObject in await dsObject.ChildObjects())
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

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

    async public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        var instance = await this.GetInstanceAsync();
        if (instance is IFeatureDatabase)
        {
            if (await ((IFeatureDatabase)instance).DeleteFeatureClass(this.Name))
            {
                if (ExplorerObjectDeleted != null)
                {
                    ExplorerObjectDeleted(this);
                }

                return true;
            }
            else
            {
                throw new GeneralException("ERROR: " + ((IFeatureDatabase)instance).LastErrorMessage);
            }
        }
        return false;
    }

    #endregion
}