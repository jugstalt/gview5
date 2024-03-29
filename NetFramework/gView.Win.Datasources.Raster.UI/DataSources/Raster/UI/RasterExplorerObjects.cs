using gView.DataSources.Raster.File;
using gView.Framework.Data;
using gView.Framework.system.UI;
using gView.Framework.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataSources.Raster.UI
{
    [gView.Framework.system.RegisterPlugIn("6F0051F0-F3C7-4eee-BE4B-45340F684FAA")]
    public class RasterFileExplorerObject : ExplorerObjectCls, IExplorerFileObject
    {
        private IExplorerIcon _icon = new RasterIcon();
        private string _filename = "";
        private IRasterClass _class = null;

        public RasterFileExplorerObject() : base(null, typeof(IRasterClass), 2) { }
        private RasterFileExplorerObject(IExplorerObject parent, string filename)
            : base(parent, typeof(IRasterClass), 2)
        {
            _filename = filename;
        }
        #region IExplorerFileObject Members

        public string Filter
        {
            get
            {
                //return "*.jpg|*.png|*.tif|*.tiff|*.pyc|*.sid|*.jp2";
                return "*.jpg|*.png|*.pyc|*.sid|*.jp2";
            }
        }

        public IExplorerIcon Icon
        {
            get
            {
                return _icon;
            }
        }

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

        public string FullName
        {
            get { return _filename; }
        }

        public string Type
        {
            get { return "Raster File"; }
        }

        public void Dispose()
        {
            if (_class != null)
            {
                _class = null;
            }
        }

        public Task<object> GetInstanceAsync()
        {
            if (_class == null)
            {
                try
                {
                    RasterFileDataset dataset = new RasterFileDataset();
                    IRasterLayer layer = (IRasterLayer)dataset.AddRasterFile(_filename);

                    if (layer != null && layer.Class is IRasterClass)
                    {
                        _class = layer.Class as IRasterClass;
                        if (_class is RasterFileClass)
                        {
                            if (!((RasterFileClass)_class).isValid)
                            {
                                _class = null;
                            }
                        }
                    }
                }
                catch { return Task.FromResult<object>(_class); }
            }
            return Task.FromResult<object>(_class);
        }

        async public Task<IExplorerObject> CreateInstanceByFullName(IExplorerObject parent, string FullName)
        {
            var instance = await CreateInstance(parent, FullName);
            return instance;
        }
        public Task<IExplorerFileObject> CreateInstance(IExplorerObject parent, string filename)
        {
            try
            {
                if (!(new FileInfo(filename)).Exists)
                {
                    return Task.FromResult<IExplorerFileObject>(null);
                }
            }
            catch
            {
                return Task.FromResult<IExplorerFileObject>(null);
            }

            return Task.FromResult<IExplorerFileObject>(new RasterFileExplorerObject(parent, filename));
        }
        #endregion

        #region ISerializableExplorerObject Member

        public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
        {
            if (cache.Contains(FullName))
            {
                return Task.FromResult<IExplorerObject>(cache[FullName]);
            }

            return Task.FromResult<IExplorerObject>(null);
        }

        #endregion
    }

    internal class RasterIcon : IExplorerIcon
    {
        #region IExplorerIcon Members

        public Guid GUID
        {
            get { return new Guid("5D01CC9D-1424-46e3-AA22-7282969B7062"); }
        }

        public System.Drawing.Image Image
        {
            get { return (new Icons()).imageList1.Images[0]; }
        }

        #endregion
    }
}
