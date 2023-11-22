using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataSources.Shape;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.Geometry;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.ShapeFiles
{
    [gView.Framework.system.RegisterPlugIn("665E0CD5-B3DF-436c-91B4-D4C0B3ECA5B9")]
    public class ShapeFileExplorerObject : ExplorerObjectCls<IExplorerObject, IFeatureClass>, 
                                           IExplorerFileObject, 
                                           ISerializableExplorerObject, 
                                           IExplorerObjectRenamable, 
                                           IExplorerObjectDeletable
    {
        private string _filename = "";
        private ShapeDataset? _shapeDataset = null;
        private IDatasetElement? _shape = null;

        public ShapeFileExplorerObject() : base() { }
        public ShapeFileExplorerObject(IExplorerObject parent, string filename)
            : base(parent, 2)
        {
            _filename = filename;
            OpenShape().Wait();
        }

        #region IExplorerFileObject Member

        public string Filter
        {
            get
            {
                return "*.shp";
            }
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
                Task.FromResult<IExplorerFileObject?>(null);
            }
            return Task.FromResult<IExplorerFileObject?>(new ShapeFileExplorerObject(parent, filename));
        }

        #endregion

        #region IExplorerObject Member

        public string Name
        {
            get
            {
                try
                {
                    FileInfo fi = new FileInfo(_filename);
                    return fi.Name;
                }
                catch { return "???"; }
            }
        }

        public string FullName
        {
            get { return _filename; }
        }

        public string Type
        {
            get { return "ESRI Shapefile"; }
        }

        public string Icon
        {
            get
            {
                switch ((_shape?.Class as IFeatureClass)?.GeometryType)
                {
                    case GeometryType.Point:
                    case GeometryType.Multipoint:
                        return "webgis:shape-circle";
                    case GeometryType.Polyline:
                        return "webgis:shape-polyline";
                    case GeometryType.Polygon:
                    case GeometryType.Envelope:
                        return "webgis:shape-polygon";
                    default:
                        return "basic:smiley-hm";
                }
            }
        }

        public void Dispose()
        {
            if (_shapeDataset != null)
            {
                _shapeDataset.Dispose();
                _shapeDataset = null;
            }
        }

        async public Task<object?> GetInstanceAsync()
        {
            if (_shapeDataset == null)
            {
                await OpenShape();
            }

            return ((_shape != null) ? _shape.Class : null);
        }

        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
        {
            IExplorerObject? obj = (cache != null && cache.Contains(FullName)) ? 
                cache[FullName] : 
                await CreateInstance(Parent, FullName);

            if (obj != null)
            {
                cache?.Append(obj);
            }

            return obj;
        }

        #endregion

        #region IExplorerObjectRenamable Member

        public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

        async public Task<bool> RenameExplorerObject(string newName)
        {
            if (_shapeDataset == null)
            {
                await OpenShape();
            }

            if (_shapeDataset != null && _shape != null)
            {
                FileInfo fi = new FileInfo(_filename);
                string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                int pos = newName.LastIndexOf(".");
                if (pos != -1)
                {
                    newName = newName.Substring(0, pos);
                }

                if (_shapeDataset.Rename(name, newName))
                {
                    _shape.Title = newName;

                    if (ExplorerObjectRenamed != null)
                    {
                        ExplorerObjectRenamed(this);
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region IExplorerObjectDeletable Member

        public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

        async public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
        {
            if (_shapeDataset == null)
            {
                await OpenShape();
            }

            if (_shapeDataset != null)
            {
                FileInfo fi = new FileInfo(_filename);
                string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

                if (_shapeDataset.Delete(name))
                {
                    if (ExplorerObjectDeleted != null)
                    {
                        ExplorerObjectDeleted(this);
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        #endregion

        async private Task OpenShape()
        {
            try
            {
                FileInfo fi = new FileInfo(_filename);

                if(!fi.Exists) 
                {
                    throw new System.Exception($"{_filename} does not exits");
                }

                _shapeDataset = new ShapeDataset();
                await _shapeDataset.SetConnectionString(fi.Directory!.FullName);

                if (!await _shapeDataset.Open() || (_shape = await _shapeDataset.Element(fi.Name)) == null)
                {
                    _shapeDataset.Dispose();
                    _shapeDataset = null;
                }
            }
            catch /*(Exception ex)*/
            {
                //System.Windows.Forms.MessageBox.Show(ex.Message, "Error");
                throw;
            }
        }
    }
}
