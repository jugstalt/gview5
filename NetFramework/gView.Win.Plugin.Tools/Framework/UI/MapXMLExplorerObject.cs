using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.system.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.Framework.UI
{
    [gView.Framework.system.RegisterPlugIn("ABE65EF4-6CFA-4d46-8392-5B4A214C1225")]
    public class MapDocumentExplorerObject : ExplorerParentObject, IExplorerFileObject, ISerializableExplorerObject
    {
        private IExplorerIcon _icon = new MapDocumentExploererIcon(0);
        private string _filename = "";
        private MapDocument _mapDocument = null;

        public MapDocumentExplorerObject() : base(null, typeof(MapDocument), 2) { }
        public MapDocumentExplorerObject(IExplorerObject parent, string filename)
            : base(parent, typeof(MapDocument), 2)
        {
            _filename = filename;
        }
        #region IExplorerFileObject Members

        public string Filter
        {
            get { return "*.mxl"; }
        }

        public Task<IExplorerFileObject> CreateInstance(IExplorerObject parent, string filename)
        {
            try
            {
                if (!(new FileInfo(filename).Exists))
                {
                    return Task.FromResult<IExplorerFileObject>(null);
                }
            }
            catch
            {
                return Task.FromResult<IExplorerFileObject>(null); ;
            }

            return Task.FromResult<IExplorerFileObject>(new MapDocumentExplorerObject(parent, filename));
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
                catch
                {
                    return "";
                }
            }
        }

        public string FullName
        {
            get { return _filename; }
        }

        public string Type
        {
            get { return "Map Xml Document"; }
        }

        public IExplorerIcon Icon
        {
            get { return _icon; }
        }

        public Task<object> GetInstanceAsync()
        {
            return Task.FromResult<object>(_mapDocument != null ? _mapDocument : new MapDocument());
        }

        #endregion

        #region IExplorerParentObject Members

        async public override Task<bool> Refresh()
        {
            try
            {
                await base.Refresh();
                _mapDocument = new MapDocument();

                XmlStream stream = new XmlStream("");
                stream.ReadStream(_filename);

                //_mapDocument.LoadMapDocument(_filename);
                await stream.LoadAsync("MapDocument", _mapDocument);

                foreach (IMap map in _mapDocument.Maps)
                {
                    base.AddChildObject(new MapExplorerObject(this, _filename, map));
                }

                return true;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                return false;
            }
        }

        #endregion

        #region ISerializableExplorerObject Member

        public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
        {
            try
            {
                if (cache.Contains(FullName))
                {
                    return Task.FromResult(cache[FullName]);
                }

                FileInfo fi = new FileInfo(FullName);
                if (!fi.Exists || fi.Extension.ToLower() != ".mxl")
                {
                    return null;
                }

                MapDocumentExplorerObject ex = new MapDocumentExplorerObject(null, FullName);
                cache.Append(ex);
                return Task.FromResult<IExplorerObject>(ex);
            }
            catch
            {
                return Task.FromResult<IExplorerObject>(null);
            }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("DD6ABADB-1011-4af1-975F-AB081ABCC400")]
    public class MapExplorerObject : ExplorerParentObject, IExplorerSimpleObject, ISerializableExplorerObject
    {
        private IMap _map = null;
        private string _filename = "";
        private IExplorerIcon _icon = new MapDocumentExploererIcon(1);

        public MapExplorerObject() : base(null, typeof(Map), 2) { }
        internal MapExplorerObject(IExplorerObject parent, string filename, IMap map)
            : base(parent, typeof(Map), 2)
        {
            _map = map;
            _filename = filename;
        }

        #region IExplorerObject Members

        public string Name
        {
            get
            {
                if (_map == null)
                {
                    return "???";
                }

                return _map.Name;
            }
        }

        public string FullName
        {
            get { return _filename + @"\" + this.Name; }
        }

        public string Type
        {
            get { return "Map"; }
        }

        public IExplorerIcon Icon
        {
            get { return _icon; }
        }

        public Task<object> GetInstanceAsync()
        {
            return Task.FromResult<object>(_map);
        }

        public IExplorerObject CreateInstanceByFullName(string FullName)
        {
            return null;
        }

        #endregion

        #region IExplorerParentObject Member

        async public override Task<bool> Refresh()
        {
            await base.Refresh();
            if (_map == null)
            {
                return false;
            }

            //foreach (IDatasetElement element in _map.MapElements)
            //{
            //    if (!(element is ILayer)) continue;
            //    _childs.Add(new FeatureLayerExplorerObject(this, (ILayer)element));
            //}
            foreach (ITocElement element in _map.TOC.Elements)
            {
                if (element.ParentGroup != null)
                {
                    continue;
                }

                if (element.ElementType == TocElementType.ClosedGroup ||
                    element.ElementType == TocElementType.OpenedGroup)
                {
                    base.AddChildObject(new TOCGroupElementExplorerObject(this, element));
                }
                else
                {
                    base.AddChildObject(new TOCElementExplorerObject(this, element));
                }
            }

            return true;
        }

        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
        {
            if (cache.Contains(FullName))
            {
                return cache[FullName];
            }

            FullName = FullName.Replace("/", @"\");
            int lastIndex = FullName.LastIndexOf(@"\");
            if (lastIndex == -1)
            {
                return null;
            }

            string mxlName = FullName.Substring(0, lastIndex);
            string mapName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

            MapDocumentExplorerObject mapDocument = new MapDocumentExplorerObject();
            mapDocument = await mapDocument.CreateInstanceByFullName(mxlName, cache) as MapDocumentExplorerObject;
            if (mapDocument == null || await mapDocument.ChildObjects() == null)
            {
                return null;
            }

            foreach (MapExplorerObject mapObject in await mapDocument.ChildObjects())
            {
                if (mapObject.Name == mapName)
                {
                    cache.Append(mapObject);
                    return mapObject;
                }
            }
            return null;
        }

        #endregion
    }

    //[gView.Framework.system.RegisterPlugIn("CA185303-D338-4101-B7B5-FD55035F58C7")]
    public class FeatureLayerExplorerObject : ExplorerObjectCls, IExplorerSimpleObject
    {
        private ILayer _layer;
        private IExplorerObject _parent;
        private IExplorerIcon _icon = new MapDocumentExploererIcon(1);

        internal FeatureLayerExplorerObject(IExplorerObject parent, ILayer layer)
            : base(parent, typeof(ILayer), 1)
        {
            _parent = parent;
            _layer = layer;
        }
        #region IExplorerObject Member

        public string Name
        {
            get { return (_layer != null) ? _layer.Title : ""; }
        }

        public string FullName
        {
            get
            {
                if (_parent == null || _layer == null)
                {
                    return "";
                }

                return _parent.FullName + "/" + Name;
            }
        }

        public string Type
        {
            get { return "Feature Layer"; }
        }

        public IExplorerIcon Icon
        {
            get { return _icon; }
        }

        public void Dispose()
        {

        }

        public Task<object> GetInstanceAsync()
        {
            return Task.FromResult<object>(null);
        }

        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
        {
            if (cache.Contains(FullName))
            {
                return cache[FullName];
            }

            FullName = FullName.Replace("/", @"\");
            int lastIndex = FullName.LastIndexOf(@"\");
            if (lastIndex == -1)
            {
                return null;
            }

            string mapName = FullName.Substring(0, lastIndex);
            string mflName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

            MapExplorerObject mapObject = new MapExplorerObject();
            mapObject = await mapObject.CreateInstanceByFullName(mapName, cache) as MapExplorerObject;
            if (mapObject == null || await mapObject.ChildObjects() == null)
            {
                return null;
            }

            foreach (FeatureLayerExplorerObject mflObject in await mapObject.ChildObjects())
            {
                if (mflObject.Name == mflName)
                {
                    cache.Append(mflObject);
                    return mflObject;
                }
            }
            return null;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("29C1C584-C4AA-4448-B7A5-2AFFDD076A06")]
    public class TOCElementExplorerObject : ExplorerObjectCls, IExplorerObject
    {
        protected IExplorerObject _parent;
        protected ITocElement _element;
        protected IExplorerIcon _icon;

        public TOCElementExplorerObject() : base(null, typeof(ITocElement), 2) { }
        internal TOCElementExplorerObject(IExplorerObject parent, ITocElement element)
            : base(parent, typeof(ITocElement), 2)
        {
            _parent = parent;
            _element = element;
            _icon = new MapDocumentExploererIcon(2);
        }

        #region IExplorerObject Member

        public string Name
        {
            get { return _element != null ? _element.Name : "???"; }
        }

        public string FullName
        {
            get
            {
                if (_parent == null)
                {
                    return "";
                }

                return _parent.FullName + @"\" + this.Name;
            }
        }

        public string Type
        {
            get { return "TOC Element"; }
        }

        public IExplorerIcon Icon
        {
            get { return _icon; }
        }

        public Task<object> GetInstanceAsync()
        {
            return Task.FromResult<object>(_element);
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {

        }

        #endregion

        #region ISerializableExplorerObject Member

        async public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
        {
            try
            {
                if (cache.Contains(FullName))
                {
                    return cache[FullName];
                }

                FullName = FullName.Replace("/", @"\");
                int lastIndex = FullName.LastIndexOf(@"\");
                if (lastIndex == -1)
                {
                    return null;
                }

                string parentName = FullName.Substring(0, lastIndex);
                string tocName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

                lastIndex = parentName.LastIndexOf(@"\");
                if (lastIndex == -1)
                {
                    return null;
                }

                string mapDocname = parentName.Substring(0, lastIndex);
                FileInfo mapDoc = new FileInfo(mapDocname);
                if (mapDoc.Exists)
                {
                    MapExplorerObject mapObject = new MapExplorerObject();
                    mapObject = await mapObject.CreateInstanceByFullName(parentName, cache) as MapExplorerObject;
                    if (mapObject == null || await mapObject.ChildObjects() == null)
                    {
                        return null;
                    }

                    foreach (TOCElementExplorerObject tocObject in await mapObject.ChildObjects())
                    {
                        if (tocObject.Name == tocName)
                        {
                            cache.Append(tocObject);
                            return tocObject;
                        }
                    }
                }
                else
                {
                    TOCGroupElementExplorerObject parentTocObject = new TOCGroupElementExplorerObject();
                    parentTocObject = await parentTocObject.CreateInstanceByFullName(parentName, cache) as TOCGroupElementExplorerObject;
                    if (parentTocObject == null || await parentTocObject.ChildObjects() == null)
                    {
                        return null;
                    }

                    foreach (TOCElementExplorerObject tocObject in await parentTocObject.ChildObjects())
                    {
                        if (tocObject.Name == tocName)
                        {
                            cache.Append(tocObject);
                            return tocObject;
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("54AD2D1E-7DC0-4fa4-8CBE-4E01226D4FC2")]
    public class TOCGroupElementExplorerObject : TOCElementExplorerObject, IExplorerParentObject
    {
        public TOCGroupElementExplorerObject() { }
        internal TOCGroupElementExplorerObject(IExplorerObject parent, ITocElement element)
        {
            _parent = parent;
            _element = element;
            _icon = new MapDocumentExploererIcon(3);
        }

        #region IExplorerParentObject Member

        async public Task<bool> Refresh()
        {
            await this.DiposeChildObjects();
            _childs = new List<IExplorerObject>();

            if (_element == null || _element.TOC == null)
            {
                return false;
            }

            if (_element.ElementType == TocElementType.ClosedGroup)
            {
                _element.OpenCloseGroup(true);
            }

            List<ITocElement> elements = _element.TOC.GroupedElements(_element);
            foreach (ITocElement element in _element.TOC.GroupedElements(_element))
            {
                if (element.ParentGroup != _element)
                {
                    continue;
                }

                if (element.ElementType == TocElementType.ClosedGroup ||
                    element.ElementType == TocElementType.OpenedGroup)
                {
                    _childs.Add(new TOCGroupElementExplorerObject(this, element));
                }
                else
                {
                    _childs.Add(new TOCElementExplorerObject(this, element));
                }
            }

            return true;
        }

        List<IExplorerObject> _childs = null;
        async public Task<List<IExplorerObject>> ChildObjects()
        {

            if (_childs == null)
            {
                await this.Refresh();
            }

            return _childs;
        }

        public Task<bool> DiposeChildObjects()
        {
            if (_childs == null)
            {
                return Task.FromResult(false);
            }

            foreach (IExplorerObject exObject in _childs)
            {
                if (exObject == null)
                {
                    continue;
                }

                exObject.Dispose();
            }
            _childs = null;

            return Task.FromResult(true);
        }

        #endregion
    }

    internal class MapDocumentExploererIcon : IExplorerIcon
    {
        private int _imageIndex = 0;

        public MapDocumentExploererIcon(int imageIndex)
        {
            _imageIndex = imageIndex;
        }

        #region IExplorerIcon Members

        public Guid GUID
        {
            get
            {
                switch (_imageIndex)
                {
                    case 0:
                        return new Guid("8D32C819-3977-4a4e-8AF7-D4CEC5D3482A");
                    case 1:
                        return new Guid("83CCD388-07A7-4a30-90D3-145A500346E8");
                    case 2:
                        return new Guid("CFC73ED0-B10D-4a12-AD3F-68282C7BB7DF");
                    case 3:
                        return new Guid("ABE65EF4-6CFA-4d46-8392-5B4A214C1225");
                }

                return new Guid();
            }
        }

        public System.Drawing.Image Image
        {
            get
            {
                switch (_imageIndex)
                {
                    case 0:
                        return gView.Win.Plugin.Tools.Properties.Resources.map16;
                    case 1:
                        return gView.Win.Plugin.Tools.Properties.Resources.layers;
                    case 2:
                        return gView.Win.Plugin.Tools.Properties.Resources.layer;
                    case 3:
                        return gView.Win.Plugin.Tools.Properties.Resources.layergroup;
                }
                return null;
            }
        }

        #endregion
    }
}
