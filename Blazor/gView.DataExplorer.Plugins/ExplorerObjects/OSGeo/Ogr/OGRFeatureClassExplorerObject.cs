using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataSources.OGR;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Geometry;
using System;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

public class OGRFeatureClassExplorerObject : ExplorerObjectCls<IExplorerObject, IFeatureClass>,
                                             IExplorerFileObject,
                                             ISerializableExplorerObject
{
    private string _filename = "", _type = "", _icon = "";
    private IFeatureClass? _fc;

    public OGRFeatureClassExplorerObject() : base() { }

    private OGRFeatureClassExplorerObject(IExplorerObject parent, string filename)
        : base(parent, 1)
    {

    }
    async static public Task<OGRFeatureClassExplorerObject> Create(IExplorerObject parent, string filename)

    {
        var exObject = new OGRFeatureClassExplorerObject(parent, filename);

        exObject._filename = filename;

        Dataset ds = new Dataset();
        await ds.SetConnectionString(filename);
        if (!await ds.Open())
        {
            if (ds.LastErrorMessage != String.Empty)
            {
                throw new GeneralException("ERROR:" + ds.LastErrorMessage);
            }

            return exObject;
        }

        var elements = await ds.Elements();

        if (elements.Count == 1)
        {
            exObject._fc = elements[0].Class as IFeatureClass;
        }

        if (exObject._fc == null)
        {
            return exObject;
        }

        switch (exObject._fc.GeometryType)
        {
            case GeometryType.Envelope:
            case GeometryType.Polygon:
                exObject._icon = "webgis:shape-polygon";
                exObject._type = "OGR Polygon Featureclass";
                break;
            case GeometryType.Multipoint:
            case GeometryType.Point:
                exObject._icon = "basic:dot-filled";
                exObject._type = "OGR Point Featureclass";
                break;
            case GeometryType.Polyline:
                exObject._icon = "webgis:shape-polyline";
                exObject._type = "OGR Polyline Featureclass";
                break;
        }

        return exObject;
    }

    #region IExplorerFileObject Member

    public string Filter
    {
        get { return "*.e00|*.gml|*.00d|*.adf"; }
    }

    public Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        return Task.FromResult<IExplorerFileObject?>(new OGRFeatureClassExplorerObject(parent, filename));
    }

    #endregion

    #region IExplorerObject Member

    public string Name
    {
        get
        {
            try
            {
                return new FileInfo(_filename).Name;
            }
            catch { return ""; }
        }
    }

    public string FullName => _filename;

    public string Type => _type;

    public string Icon => _icon;

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(_fc);

    #endregion

    #region IDisposable Member

    public void Dispose()
    {

    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
