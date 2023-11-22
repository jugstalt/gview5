using gView.Framework.Data;
using gView.Framework.Data.Metadata;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.Web;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.MapServer;
using gView.Server.Connector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.Server
{
    [gView.Framework.system.RegisterPlugIn("654AF5E2-6159-4342-ABA8-0332CD71D990")]
    public class MapServerDataset : DatasetMetadata, IFeatureDataset, IPersistable
    {
        internal string _connection = "";
        internal string _name = "";

        //internal List<string> _layerIDs = new List<string>();
        internal List<IWebServiceTheme> _themes = new List<IWebServiceTheme>();

        internal bool _opened = false;
        private IWebServiceClass _class = null;
        private IEnvelope _envelope;
        private DatasetState _state = DatasetState.unknown;

        public MapServerDataset()
        {
        }

        public MapServerDataset(string connection, string name)
        {
            _connection = connection;
            _name = name;

            _class = new MapServerClass(this);
        }

        #region IFeatureDataset Member

        public Task<IEnvelope> Envelope()
        {
            return Task.FromResult(_envelope);
        }

        public Task<ISpatialReference> GetSpatialReference()
        {
            return Task.FromResult<ISpatialReference>(_class?.SpatialReference);
        }

        public void SetSpatialReference(ISpatialReference sRef)
        {
        }

        #endregion IFeatureDataset Member

        #region IDataset Member

        public void Dispose()
        {
        }

        public string ConnectionString
        {
            get
            {
                return _connection + ";service=" + _name;
            }
            set
            {
                _connection = "server=" + ConfigTextStream.ExtractValue(value, "server") +
                                ";user=" + ConfigTextStream.ExtractValue(value, "user") +
                                ";pwd=" + ConfigTextStream.ExtractValue(value, "pwd");
                _name = ConfigTextStream.ExtractValue(value, "service");
            }
        }

        public Task<bool> SetConnectionString(string connectionString)
        {
            this.ConnectionString = connectionString;
            return Task.FromResult(true);
        }

        public string DatasetGroupName
        {
            get { return "gView.MapServer"; }
        }

        public string DatasetName
        {
            get { return "MapServer Map"; }
        }

        public string ProviderName
        {
            get { return "gView"; }
        }

        public DatasetState State
        {
            get { return _state; }
        }

        public Task<bool> Open()
        {
            try
            {
                _opened = true;
                _themes.Clear();

                ServerConnection server = new ServerConnection(ConfigTextStream.ExtractValue(_connection, "server"));
                string axl = "<ARCXML version=\"1.1\"><REQUEST><GET_SERVICE_INFO fields=\"true\" envelope=\"true\" renderer=\"false\" extensions=\"false\" gv_meta=\"true\" /></REQUEST></ARCXML>";
                axl = server.Send(_name, axl, "BB294D9C-A184-4129-9555-398AA70284BC",
                    ConfigTextStream.ExtractValue(_connection, "user"),
                    ConfigTextStream.ExtractValue(_connection, "pwd"));

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(axl);

                if (_class == null)
                {
                    _class = new MapServerClass(this);
                }

                double dpi = 96.0;
                XmlNode screen = doc.SelectSingleNode("//ENVIRONMENT/SCREEN");
                if (screen != null)
                {
                    if (screen.Attributes["dpi"] != null)
                    {
                        dpi = Convert.ToDouble(screen.Attributes["dpi"].Value.Replace(".", ","));
                    }
                }
                double dpm = (dpi / 0.0254);

                XmlNode spatialReference = doc.SelectSingleNode("//PROPERTIES/SPATIALREFERENCE");
                if (spatialReference != null)
                {
                    if (spatialReference.Attributes["param"] != null)
                    {
                        SpatialReference sRef = new SpatialReference();
                        gView.Framework.Geometry.SpatialReference.FromProj4(sRef, spatialReference.Attributes["param"].Value);

                        if (spatialReference.Attributes["name"] != null)
                        {
                            sRef.Name = spatialReference.Attributes["name"].Value;
                        }

                        _class.SpatialReference = sRef;
                    }
                }
                else
                {
                    XmlNode FeatureCoordSysNode = doc.SelectSingleNode("ARCXML/RESPONSE/SERVICEINFO/PROPERTIES/FEATURECOORDSYS");
                    if (FeatureCoordSysNode != null)
                    {
                        if (FeatureCoordSysNode.Attributes["id"] != null)
                        {
                            _class.SpatialReference = gView.Framework.Geometry.SpatialReference.FromID("epsg:" + FeatureCoordSysNode.Attributes["id"].Value);
                        }
                        else if (FeatureCoordSysNode.Attributes["string"] != null)
                        {
                            _class.SpatialReference = gView.Framework.Geometry.SpatialReference.FromWKT(FeatureCoordSysNode.Attributes["string"].Value);
                        }

                        // TODO: Geogr. Datum aus "datumtransformid" und "datumtransformstring"
                        //if (_sRef != null && FeatureCoordSysNode.Attributes["datumtransformstring"] != null)
                        //{
                        //}
                    }
                }

                foreach (XmlNode envelopeNode in doc.SelectNodes("//ENVELOPE"))
                {
                    if (_envelope == null)
                    {
                        _envelope = (new Envelope(envelopeNode)).MakeValid();
                    }
                    else
                    {
                        _envelope.Union((new Envelope(envelopeNode)).MakeValid());
                    }
                }
                foreach (XmlNode layerNode in doc.SelectNodes("//LAYERINFO[@id]"))
                {
                    bool visible = true;

                    ISpatialReference sRef = _class.SpatialReference;
                    /*
                    spatialReference = doc.SelectSingleNode("//PROPERTIES/SPATIALREFERENCE");
                    if (spatialReference != null)
                    {
                        if (spatialReference.Attributes["param"] != null)
                        {
                            sRef = new SpatialReference();
                            gView.Framework.Geometry.SpatialReference.FromProj4(sRef, spatialReference.Attributes["param"].Value);

                            if (spatialReference.Attributes["name"] != null)
                                ((SpatialReference)sRef).Name = spatialReference.Attributes["name"].Value;
                        }
                    }
                    else
                    {
                        XmlNode FeatureCoordSysNode = doc.SelectSingleNode("ARCXML/RESPONSE/SERVICEINFO/PROPERTIES/FEATURECOORDSYS");
                        if (FeatureCoordSysNode != null)
                        {
                            if (FeatureCoordSysNode.Attributes["id"] != null)
                            {
                                sRef = gView.Framework.Geometry.SpatialReference.FromID("epsg:" + FeatureCoordSysNode.Attributes["id"].Value);
                            }
                            else if (FeatureCoordSysNode.Attributes["string"] != null)
                            {
                                sRef = gView.Framework.Geometry.SpatialReference.FromWKT(FeatureCoordSysNode.Attributes["string"].Value);
                            }

                            // TODO: Geogr. Datum aus "datumtransformid" und "datumtransformstring"
                            //if (_sRef != null && FeatureCoordSysNode.Attributes["datumtransformstring"] != null)
                            //{
                            //}
                        }
                    }
                    */

                    if (layerNode.Attributes["visible"] != null)
                    {
                        bool.TryParse(layerNode.Attributes["visible"].Value, out visible);
                    }

                    IClass themeClass = null;
                    IWebServiceTheme theme = null;
                    if (layerNode.Attributes["type"] != null && layerNode.Attributes["type"].Value == "featureclass")
                    {
                        themeClass = new MapThemeFeatureClass(this, layerNode.Attributes["id"].Value);
                        ((MapThemeFeatureClass)themeClass).Name = layerNode.Attributes["name"] != null ? layerNode.Attributes["name"].Value : layerNode.Attributes["id"].Value;
                        ((MapThemeFeatureClass)themeClass).fieldsFromAXL = layerNode.InnerXml;
                        ((MapThemeFeatureClass)themeClass).SpatialReference = sRef;

                        XmlNode FCLASS = layerNode.SelectSingleNode("FCLASS[@type]");
                        if (FCLASS != null)
                        {
                            ((MapThemeFeatureClass)themeClass).fClassTypeString = FCLASS.Attributes["type"].Value;
                        }
                        theme = LayerFactory.Create(themeClass, _class) as IWebServiceTheme;
                        if (theme == null)
                        {
                            continue;
                        }

                        theme.Visible = visible;
                    }
                    else if (layerNode.Attributes["type"] != null && layerNode.Attributes["type"].Value == "image")
                    {
                        if (layerNode.SelectSingleNode("gv_meta/class/implements[@type='gView.Framework.Data.IPointIdentify']") != null)
                        {
                            themeClass = new MapThemeQueryableRasterClass(this, layerNode.Attributes["id"].Value);
                            ((MapThemeQueryableRasterClass)themeClass).Name = layerNode.Attributes["name"] != null ? layerNode.Attributes["name"].Value : layerNode.Attributes["id"].Value;
                        }
                        else
                        {
                            themeClass = new MapThemeRasterClass(this, layerNode.Attributes["id"].Value);
                            ((MapThemeRasterClass)themeClass).Name = layerNode.Attributes["name"] != null ? layerNode.Attributes["name"].Value : layerNode.Attributes["id"].Value;
                        }
                        theme = new WebServiceTheme(
                            themeClass,
                            themeClass.Name,
                            layerNode.Attributes["id"].Value,
                            visible,
                            _class);
                    }
                    else
                    {
                        continue;
                    }

                    try
                    {
                        if (layerNode.Attributes["minscale"] != null)
                        {
                            theme.MinimumScale = Convert.ToDouble(layerNode.Attributes["minscale"].Value.Replace(".", ",")) * dpm;
                        }

                        if (layerNode.Attributes["maxscale"] != null)
                        {
                            theme.MaximumScale = Convert.ToDouble(layerNode.Attributes["maxscale"].Value.Replace(".", ",")) * dpm;
                        }
                    }
                    catch { }
                    _themes.Add(theme);
                }
                _state = DatasetState.opened;
                return Task.FromResult(true);
            }
            catch (Exception /*ex*/)
            {
                _state = DatasetState.unknown;
                _class = null;
                return Task.FromResult(false);
            }
        }

        public string LastErrorMessage
        {
            get { return ""; }
            set { }
        }

        public int order
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public IDatasetEnum DatasetEnum
        {
            get { return null; }
        }

        public Task<List<IDatasetElement>> Elements()
        {
            List<IDatasetElement> elements = new List<IDatasetElement>();
            if (_class != null)
            {
                elements.Add(new DatasetElement(_class));
            }
            return Task.FromResult(elements);
        }

        public string Query_FieldPrefix
        {
            get { return ""; }
        }

        public string Query_FieldPostfix
        {
            get { return ""; }
        }

        public gView.Framework.FDB.IDatabase Database
        {
            get { return null; }
        }

        public Task<IDatasetElement> Element(string title)
        {
            if (_class != null && title == _class.Name)
            {
                return Task.FromResult<IDatasetElement>(new DatasetElement(_class));
            }

            return Task.FromResult<IDatasetElement>(null);
        }

        public Task RefreshClasses()
        {
            return Task.CompletedTask;
        }

        #endregion IDataset Member

        #region IPersistable Member

        public string PersistID
        {
            get { return ""; }
        }

        public Task<bool> LoadAsync(IPersistStream stream)
        {
            Load(stream);
            return Task.FromResult(true);
        }

        public void Load(IPersistStream stream)
        {
            this.ConnectionString = (string)stream.Load("ConnectionString", "");
            _class = new MapServerClass(this);
            Open();
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("ConnectionString", this.ConnectionString);
        }

        #endregion IPersistable Member
    }

    public class MapServerClass : IWebServiceClass
    {
        private string _name;
        private MapServerDataset _dataset;
        private IBitmap _legend = null;
        private GeorefBitmap _image = null;
        private ISpatialReference _sRef = null;
        private List<IWebServiceTheme> _clonedThemes = null;

        public MapServerClass(MapServerDataset dataset)
        {
            _dataset = dataset;
            if (_dataset != null)
            {
                _name = _dataset._name;
            }
        }

        #region IClass Member

        public string Name
        {
            get { return _name; }
        }

        public string Aliasname
        {
            get { return (_dataset != null) ? _dataset.DatasetName : ""; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion IClass Member

        #region IWebServiceClass Member

        public event AfterMapRequestEventHandler AfterMapRequest;

        async public Task<bool> MapRequest(gView.Framework.Carto.IDisplay display)
        {
            if (_dataset == null)
            {
                return false;
            }

            if (!_dataset._opened)
            {
                await _dataset.Open();
            }

            IServiceRequestContext context = display.Map as IServiceRequestContext;
            try
            {
                ISpatialReference sRef = (display.SpatialReference != null) ?
                display.SpatialReference.Clone() as ISpatialReference :
                null;

                int iWidth = display.ImageWidth, iHeight = display.ImageHeight;

                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version='1.0' encoding='utf-8'?>");
                sb.Append("<ARCXML version='1.1'>");
                sb.Append("<REQUEST>");
                sb.Append("<GET_IMAGE>");
                sb.Append("<PROPERTIES>");
                sb.Append("<ENVELOPE minx='" + display.Envelope.minx.ToString() + "' miny='" + display.Envelope.miny.ToString() + "' maxx='" + display.Envelope.maxx.ToString() + "' maxy='" + display.Envelope.maxy.ToString() + "' />");
                sb.Append("<IMAGESIZE width='" + iWidth + "' height='" + iHeight + "' />");
                sb.Append("<BACKGROUND color='255,255,255' transcolor='255,255,255' />");
                //if (display.SpatialReference != null && !display.SpatialReference.Equals(_sRef))
                //{
                //    string map_param = gView.Framework.Geometry.SpatialReference.ToProj4(display.SpatialReference);
                //    sb.Append("<SPATIALREFERENCE name='" + display.SpatialReference.Name + "' param='" + map_param + "' />");
                //}

                if (sRef != null)
                {
                    string map_param = gView.Framework.Geometry.SpatialReference.ToProj4(display.SpatialReference);
                    sb.Append("<SPATIALREFERENCE name='" + display.SpatialReference.Name + "' param='" + map_param + "' />");

                    string wkt = gView.Framework.Geometry.SpatialReference.ToESRIWKT(sRef);
                    string geotranwkt = gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(sRef);

                    if (wkt != null)
                    {
                        wkt = wkt.Replace("\"", "&quot;");
                        geotranwkt = geotranwkt.Replace("\"", "&quot;");
                        if (!String.IsNullOrEmpty(geotranwkt))
                        {
                            sb.Append("<FEATURECOORDSYS string=\"" + wkt + "\" datumtransformstring=\"" + geotranwkt + "\" />");
                            sb.Append("<FILTERCOORDSYS string=\"" + wkt + "\" datumtransformstring=\"" + geotranwkt + "\" />");
                        }
                        else
                        {
                            sb.Append("<FEATURECOORDSYS string=\"" + wkt + "\" />");
                            sb.Append("<FILTERCOORDSYS string=\"" + wkt + "\" />");
                        }
                    }
                }



                sb.Append("<LAYERLIST>");
                foreach (IWebServiceTheme theme in Themes)
                {
                    sb.Append("<LAYERDEF id='" + theme.LayerID + "' visible='" + (theme.Visible && !theme.Locked).ToString() + "' />");
                }
                sb.Append("</LAYERLIST>");
                sb.Append("</PROPERTIES>");
                sb.Append("</GET_IMAGE>");
                sb.Append("</REQUEST>");
                sb.Append("</ARCXML>");

                string user = ConfigTextStream.ExtractValue(_dataset._connection, "user");
                string pwd = Identity.HashPassword(ConfigTextStream.ExtractValue(_dataset._connection, "pwd"));

                if ((user == "#" || user == "$") &&
                    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
                {
                    string roles = String.Empty;
                    if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
                    {
                        foreach (string role in context.ServiceRequest.Identity.UserRoles)
                        {
                            if (String.IsNullOrEmpty(role))
                            {
                                continue;
                            }

                            roles += "|" + role;
                        }
                    }
                    user = context.ServiceRequest.Identity.UserName + roles;
                    // ToDo:
                    //pwd = context.ServiceRequest.Identity.HashedPassword;
                }

#if(DEBUG)
                //Logger.LogDebug("Start gView Mapserver Request");
#endif
                ServerConnection service = new ServerConnection(ConfigTextStream.ExtractValue(_dataset._connection, "server"));
                string resp = service.Send(_name, sb.ToString(), "BB294D9C-A184-4129-9555-398AA70284BC", user, pwd);

#if(DEBUG)
                //Logger.LogDebug("gView Mapserver Request Finished");
#endif

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                IBitmap bitmap = null;
                XmlNode output = doc.SelectSingleNode("//OUTPUT[@file]");
                if (_image != null)
                {
                    _image.Dispose();
                    _image = null;
                }

                try
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(output.Attributes["file"].Value);
                    if (fi.Exists)
                    {
                        bitmap = Current.Engine.CreateBitmap(fi.FullName);
                    }
                }
                catch { }

                if (bitmap == null)
                {
                    bitmap = WebFunctions.DownloadImage(output);
                }

                if (bitmap != null)
                {
                    _image = new GeorefBitmap(bitmap);
                    _image.SpatialReference = display.SpatialReference;
                    _image.Envelope = display.Envelope;

                    AfterMapRequest?.Invoke(this, display, _image);
                }

                return _image != null;
            }
            catch (Exception ex)
            {
                MapServerClass.ErrorLog(context, "MapRequest", ConfigTextStream.ExtractValue(_dataset._connection, "server"), _name, ex);
                return false;
            }
        }

        public Task<bool> LegendRequest(gView.Framework.Carto.IDisplay display)
        {
            return Task.FromResult(false);
        }

        GeorefBitmap IWebServiceClass.Image
        {
            get { return _image; }
        }

        IBitmap IWebServiceClass.Legend
        {
            get { return _legend; }
        }

        public IEnvelope Envelope
        {
            get
            {
                if (_dataset == null)
                {
                    return null;
                }

                if (!_dataset._opened)
                {
                    _dataset.Open();
                }

                return _dataset.Envelope().Result;
            }
        }

        public List<IWebServiceTheme> Themes
        {
            get
            {
                if (_clonedThemes != null)
                {
                    return _clonedThemes;
                }

                if (_dataset != null)
                {
                    if (!_dataset._opened)
                    {
                        _dataset.Open();
                    }

                    return _dataset._themes;
                }
                return new List<IWebServiceTheme>();
            }
        }

        public ISpatialReference SpatialReference
        {
            get
            {
                return _sRef;
            }
            set
            {
                _sRef = value;
            }
        }

        #endregion IWebServiceClass Member

        #region IClone Member

        public object Clone()
        {
            MapServerClass clone = new MapServerClass(_dataset);
            clone._clonedThemes = new List<IWebServiceTheme>();

            foreach (IWebServiceTheme theme in Themes)
            {
                if (theme == null || theme.Class == null)
                {
                    continue;
                }

                clone._clonedThemes.Add(LayerFactory.Create(theme.Class, theme as ILayer, clone) as IWebServiceTheme);
            }
            clone.AfterMapRequest = AfterMapRequest;
            return clone;
        }

        #endregion IClone Member

        public static void Log(IServiceRequestContext context, string header, string server, string service, StringBuilder axl)
        {
            if (context == null ||
                context.MapServer == null ||
                context.MapServer.LoggingEnabled(loggingMethod.request_detail_pro) == false)
            {
                return;
            }
        }

        public static void ErrorLog(IServiceRequestContext context, string header, string server, string service, Exception ex)
        {
            if (context == null ||
                context.MapServer == null ||
                context.MapServer.LoggingEnabled(loggingMethod.error) == false)
            {
                return;
            }

            StringBuilder msg = new StringBuilder();
            if (ex != null)
            {
                msg.Append(ex.Message + "\n");
                Exception inner = ex;
                while ((inner = inner.InnerException) != null)
                {
                    msg.Append(inner.Message + "\n");
                }
            }
        }
    }

    internal class MapThemeFeatureClass : gView.Framework.XML.AXLFeatureClass
    {
        public MapThemeFeatureClass(IDataset dataset, string id)
            : base(dataset, id)
        {
            if (dataset is IFeatureDataset)
            {
                this.SpatialReference = ((IFeatureDataset)dataset).GetSpatialReference().Result;
            }
        }

        protected override Task<string> SendRequest(IUserData userData, string axlRequest)
        {
            if (_dataset == null)
            {
                return Task.FromResult(String.Empty);
            }

            string server = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "service");

            IServiceRequestContext context = (userData != null) ? userData.GetUserData("IServiceRequestContext") as IServiceRequestContext : null;
            string user = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "user");
            string pwd = Identity.HashPassword(ConfigTextStream.ExtractValue(_dataset.ConnectionString, "pwd"));

            if ((user == "#" || user == "$") &&
                    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
            {
                string roles = String.Empty;
                if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
                {
                    foreach (string role in context.ServiceRequest.Identity.UserRoles)
                    {
                        if (String.IsNullOrEmpty(role))
                        {
                            continue;
                        }

                        roles += "|" + role;
                    }
                }
                user = context.ServiceRequest.Identity.UserName + roles;
                // ToDo:
                //pwd = context.ServiceRequest.Identity.HashedPassword;
            }

            ServerConnection conn = new ServerConnection(server);
            string resp = conn.Send(service, axlRequest, "BB294D9C-A184-4129-9555-398AA70284BC", user, pwd);

            try
            {
                return Task.FromResult(conn.Send(service, axlRequest, "BB294D9C-A184-4129-9555-398AA70284BC", user, pwd));
            }
            catch (Exception ex)
            {
                MapServerClass.ErrorLog(context, "Query", server, service, ex);
                return Task.FromResult(String.Empty);
            }
        }
    }

    internal class MapThemeClass : IClass
    {
        private IDataset _dataset;
        private string _name;

        public MapThemeClass(IDataset dataset, string name)
        {
            _dataset = dataset;
            _name = name;
        }

        #region IClass Member

        public string Name
        {
            get { return _name; }
        }

        public string Aliasname
        {
            get { return _name; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion IClass Member
    }

    internal class MapThemeRasterClass : gView.Framework.XML.AXLRasterClass
    {
        public MapThemeRasterClass(IDataset dataset, string id)
            : base(dataset, id)
        {
        }
    }

    internal class MapThemeQueryableRasterClass : gView.Framework.XML.AXLQueryableRasterClass
    {
        public MapThemeQueryableRasterClass(IDataset dataset, string id)
            : base(dataset, id)
        {
        }

        protected override Task<string> SendRequest(IUserData userData, string axlRequest)
        {
            if (_dataset == null)
            {
                return Task.FromResult(String.Empty);
            }

            string server = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "service");

            IServiceRequestContext context = (userData != null) ? userData.GetUserData("IServiceRequestContext") as IServiceRequestContext : null;
            string user = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "user");
            string pwd = Identity.HashPassword(ConfigTextStream.ExtractValue(_dataset.ConnectionString, "pwd"));

            if ((user == "#" || user == "$") &&
                    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
            {
                string roles = String.Empty;
                if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
                {
                    foreach (string role in context.ServiceRequest.Identity.UserRoles)
                    {
                        if (String.IsNullOrEmpty(role))
                        {
                            continue;
                        }

                        roles += "|" + role;
                    }
                }
                user = context.ServiceRequest.Identity.UserName + roles;
                // ToDo:
                //pwd = context.ServiceRequest.Identity.HashedPassword;
            }

            ServerConnection conn = new ServerConnection(server);
            try
            {
                return Task.FromResult(conn.Send(service, axlRequest, "BB294D9C-A184-4129-9555-398AA70284BC", user, pwd));
            }
            catch (Exception ex)
            {
                MapServerClass.ErrorLog(context, "Query", server, service, ex);
                return Task.FromResult(String.Empty);
            }
        }
    }
}