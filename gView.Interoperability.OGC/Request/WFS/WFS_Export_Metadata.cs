using gView.Framework.Carto;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace gView.Interoperability.OGC
{
    [gView.Framework.system.RegisterPlugIn("A2342AC5-9FD6-47a4-A25A-F684C375C895")]
    public class WFS_Export_Metadata : IMetadataProvider, IPropertyPage, IEpsgMetadata, IPropertyModel
    {
        private Metadata _metadata = null;
        private IMap _map = null;

        #region IMetadataProvider Member

        public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            if (Object is IMap)
            {
                _map = (IMap)Object;
                if (_metadata == null)
                {
                    _metadata = new Metadata(_map.Display?.SpatialReference?.Name);
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public string Name
        {
            get { return "WFS Export"; }
        }
        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            //string epsg = String.Empty;
            //if (map.Display != null && map.Display.SpatialReference != null)
            //    epsg = map.Display.SpatialReference.Name;

            _metadata = new Metadata(String.Empty);
            stream.Load("WFS_Export", null, _metadata);
        }

        public void Save(IPersistStream stream)
        {
            if (_metadata == null && _map != null)
            {
                string epsg = String.Empty;
                if (_map.Display != null && _map.Display.SpatialReference != null)
                {
                    epsg = _map.Display.SpatialReference.Name;
                }

                _metadata = new Metadata(epsg);
            }
            stream.Save("WFS_Export", _metadata);
        }

        #endregion

        #region Classes

        internal class Metadata : IPersistable
        {
            private string _epsg;
            private IndexList<string> _epsgCodes = new IndexList<string>();

            public Metadata(Metadata metadata)
            {
                if (metadata != null)
                {
                    _epsg = metadata._epsg;
                    _epsgCodes.AddRange(metadata._epsgCodes);
                }
            }

            public Metadata(string epsg)
            {
                _epsg = epsg;

                SetDefaultEPSGCodes();
            }

            public Metadata() { }

            public string[] EPSGCodes
            {
                get
                {
                    return _epsgCodes.ToArray();
                }
                internal set
                {
                    if (value == null)
                    {
                        return;
                    }

                    _epsgCodes = new IndexList<string>();
                    foreach (string code in value)
                    {
                        _epsgCodes.Add(code);
                    }
                }
            }
            private void SetDefaultEPSGCodes()
            {
                _epsgCodes = new IndexList<string>();
                if (!String.IsNullOrEmpty(_epsg))
                {
                    _epsgCodes.Add(_epsg.ToUpper());
                }

                foreach (string srs in WMSConfig.SRS.Split(';'))
                {
                    _epsgCodes.Add(srs.ToUpper());
                }
            }
            #region IPersistable Member

            public void Load(IPersistStream stream)
            {
                _epsgCodes = new IndexList<string>();
                XmlStreamStringArray epsg = stream.Load("EPSG_Codes") as XmlStreamStringArray;
                if (epsg != null &&
                    epsg.Value != null &&
                    epsg.Value.GetType() == typeof(string[]) &&
                    ((string[])epsg.Value).Length > 0)
                {
                    foreach (string epsgCode in (string[])epsg.Value)
                    {
                        _epsgCodes.Add(epsgCode.ToUpper());
                    }
                }
                else
                {
                    SetDefaultEPSGCodes();
                }
            }

            public void Save(IPersistStream stream)
            {
                XmlStreamStringArray epsg = new XmlStreamStringArray(_epsgCodes.ToArray());
                stream.Save("EPSG_Codes", epsg);
            }

            #endregion
        }

        #endregion

        internal Metadata Data => _metadata;

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Interoperability.OGC.UI.dll");

            IPlugInParameter p = uiAssembly.CreateInstance("gView.Interoperability.OGC.UI.Dataset.WFS.WFSMetadata") as IPlugInParameter;
            if (p != null)
            {
                p.Parameter = this;
            }

            return p;
        }

        public object PropertyPageObject()
        {
            return this;
        }

        #endregion

        #region IPropertyModel

        public Type PropertyModelType => typeof(Metadata);

        public object GetPropertyModel()
        {
            return new Metadata(_metadata);
        }

        public void SetPropertyModel(object propertyModel)
        {
            this.EpsgCodes = (propertyModel as Metadata)?.EPSGCodes;
        }

        #endregion

        #region IEPSGMetadata
        public string[] EpsgCodes
        {
            get
            {
                if (_metadata != null)
                {
                    return _metadata.EPSGCodes;
                }

                return null;
            }
            set
            {
                if (_metadata != null)
                {
                    _metadata.EPSGCodes = value;
                }
            }
        }
        #endregion
    }
}
