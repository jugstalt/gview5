using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataSources.GDAL;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Gdal;

[RegisterPlugIn("BCBE95C6-95C5-432c-8045-918A8B17D270")]
public class GDALRasterFileExplorerObject : 
            ExplorerObjectCls<IExplorerObject, RasterClassV1>, 
            IExplorerFileObject, 
            IPlugInDependencies
{
    private string _filename = "";
    private IRasterClass? _class = null;

    public GDALRasterFileExplorerObject() : base() { }
    private GDALRasterFileExplorerObject(IExplorerObject parent, string filename)
        : base(parent, 2)
    {
        _filename = filename;
    }

    #region IExplorerFileObject Members

    public string Filter
    {
        get { return "*.jpg|*.tif|*.tiff|*.dem|*.xpm|w001001.adf|*.png|*.ecw|*.img|*.gsb|*.jp2|*.sid"; }
    }

    public string Icon => "webgis:georef-image";

    #endregion

    #region IExplorerObject Members

    public string Name
    {
        get
        {
            try
            {
                FileInfo fi = new FileInfo(_filename);
                return fi.Name;
            }
            catch { return ""; }
        }
    }

    public string FullName => _filename;
    public string Type
    {
        get { return "GDAL Raster File"; }
    }

    public void Dispose()
    {
        if (_class != null)
        {
            _class = null;
        }
    }

    public Task<object?> GetInstanceAsync()
    {
        if (_class == null)
        {
            try
            {
                Dataset dataset = new Dataset();
                IRasterLayer layer = (IRasterLayer)dataset.AddRasterFile(_filename);

                if (layer != null && layer.Class is IRasterClass)
                {
                    _class = layer.Class as IRasterClass;
                    if (_class is RasterClassV1)
                    {
                        if (!((RasterClassV1)_class).isValid)
                        {
                            _class = null;
                        }
                    }
                }
            }
            catch { return Task.FromResult<object?>(_class); }
        }
        return Task.FromResult<object?>(_class);
    }

    public Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        try
        {
            if (!(new FileInfo(filename)).Exists)
            {
                return Task.FromResult<IExplorerFileObject?>(null);
            }
        }
        catch
        {
            return Task.FromResult<IExplorerFileObject?>(null);
        }
        return Task.FromResult<IExplorerFileObject?>(new GDALRasterFileExplorerObject(parent, filename));
    }
    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        try
        {
            FileInfo fi = new FileInfo(FullName);
            if (!fi.Exists)
            {
                return null;
            }

            GDALRasterFileExplorerObject rObject = new GDALRasterFileExplorerObject(new NullParentExplorerObject(), FullName);
            if (await rObject.GetInstanceAsync() is IRasterClass)
            {
                cache?.Append(rObject);
                return rObject;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region IPlugInDependencies Member

    public bool HasUnsolvedDependencies()
    {
        return Dataset.hasUnsolvedDependencies;
    }

    #endregion
}
