using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.MapServer;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace gView.Interoperability.OGC.Dataset.WMS
{
    [gView.Framework.system.RegisterPlugIn("C2FF55BA-61AF-42aa-9216-E68CDAE8F0DE")]
    public class WMSImportMetadata : IMetadataProvider, IPropertyPage
    {
        #region Declarations
        private IServiceMap _map;
        private string _srsCode = String.Empty, _getFeatureInfo = String.Empty, _getMapInfo = String.Empty;
        private string[] _srsCodes;
        private string[] _getFeatureInfos;
        private string[] _getMapInfos;
        #endregion

        #region Properties
        public string[] SRSCodes
        {
            get { return _srsCodes; }
        }
        public string SRSCode
        {
            get { return _srsCode; }
            set { _srsCode = value; }
        }
        public string[] FeatureInfoFormats
        {
            get { return _getFeatureInfos; }
        }
        public string FeatureInfoFormat
        {
            get { return _getFeatureInfo; }
            set { _getFeatureInfo = value; }
        }
        public string[] GetMapFormats
        {
            get { return _getMapInfos; }
        }
        public string GetMapFormat
        {
            get { return _getMapInfo; }
            set { _getMapInfo = value; }
        }
        #endregion

        #region IMetadataProvider Member

        async public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            if (Object is IServiceMap)
            {
                _map = (IServiceMap)Object;
                if (await ServiceMapIsSVC(_map.MapServer, _map))
                {
                    foreach (IDatasetElement element in _map.MapElements)
                    {
                        if (element.Class is WMSClass)
                        {
                            WMSClass wmsClass = (WMSClass)element.Class;
                            _srsCodes = wmsClass.SRSCodes;
                            _getFeatureInfos = wmsClass.FeatureInfoFormats;
                            _getMapInfos = wmsClass.GetMapFormats;
                            return true;
                        }
                    }
                }

                _map = null;
                return false;
            }
            else if (Object is IMap && _map != null &&
                ((IMap)Object).Name == _map.Name)
            {
                foreach (IDatasetElement element in _map.MapElements)
                {
                    if (element.Class is WMSClass)
                    {
                        WMSClass wmsClass = (WMSClass)element.Class;
                        if (!String.IsNullOrEmpty(_srsCode))
                        {
                            wmsClass.SRSCode = _srsCode;
                        }

                        if (!String.IsNullOrEmpty(_getMapInfo))
                        {
                            wmsClass.SRSCode = _getMapInfo;
                        }

                        if (!String.IsNullOrEmpty(_getFeatureInfo))
                        {
                            wmsClass.FeatureInfoFormat = _getFeatureInfo;
                        }

                        // SpatialReference gleich auf Karte übernehmen...
                        ISpatialReference sRef = wmsClass.SpatialReference;
                        if (sRef != null)
                        {
                            ((IMap)Object).Display.SpatialReference = sRef;
                        }

                        break;
                    }
                }
                _map = null;
                return true;
            }
            return false;
        }

        public string Name
        {
            get { return "WMS Import"; }
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _srsCode = (string)stream.Load("SrsCode", String.Empty);
            _getMapInfo = (string)stream.Load("GetMapFormat", String.Empty);
            _getFeatureInfo = (string)stream.Load("GetFeatureInfoFormat", String.Empty);

            XmlStreamStringArray arr;
            arr = (XmlStreamStringArray)stream.Load("SrsCodes", null, new XmlStreamStringArray());
            if (arr != null)
            {
                _srsCodes = (string[])arr.Value;
            }

            arr = (XmlStreamStringArray)stream.Load("GetMapFormats", null, new XmlStreamStringArray());
            if (arr != null)
            {
                _getMapInfos = (string[])arr.Value;
            }

            arr = (XmlStreamStringArray)stream.Load("GetFeatureInfoFormat", null, new XmlStreamStringArray());
            if (arr != null)
            {
                _getFeatureInfos = (string[])arr.Value;
            }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("SrsCode", _srsCode);
            stream.Save("GetMapFormat", _getMapInfo);
            stream.Save("GetFeatureInfoFormat", _getFeatureInfo);

            stream.Save("SrsCodes", new XmlStreamStringArray(_srsCodes));
            stream.Save("GetMapFormats", new XmlStreamStringArray(_getMapInfos));
            stream.Save("GetFeatureInfoFormat", new XmlStreamStringArray(_getFeatureInfos));
        }

        #endregion

        #region Helper
        async private Task<bool> ServiceMapIsSVC(IMapServer server, IServiceMap map)
        {
            if (server == null || map == null)
            {
                return false;
            }

            foreach (IMapService service in await server.Maps(null))
            {
                if (service == null)
                {
                    continue;
                }

                if (service.Name == map.Name && service.Type == MapServiceType.SVC)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Interoperability.OGC.UI.dll");

            IPlugInParameter p = uiAssembly.CreateInstance("gView.Interoperability.OGC.UI.Dataset.WMS.Metadata_WMS") as IPlugInParameter;
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
    }
}
