using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.Web;
using gView.Framework.XML;
using gView.GraphicsEngine.Abstraction;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.ArcXML.Dataset
{
    public class ArcIMSClass : IWebServiceClass
    {
        internal event EventHandler ModifyResponseOuput = null;

        private string _name;
        private ArcIMSDataset _dataset;
        private GraphicsEngine.Abstraction.IBitmap _legend = null;
        private GeorefBitmap _image = null;
        private List<IWebServiceTheme> _clonedThemes = null;

        public ArcIMSClass(ArcIMSDataset dataset)
        {
            _dataset = dataset;
            if (_dataset != null)
            {
                _name = _dataset._name;
            }
        }

        #region IWebServiceClass Member

        public event AfterMapRequestEventHandler AfterMapRequest = null;

        async public Task<bool> MapRequest(gView.Framework.Carto.IDisplay display)
        {
            if (_dataset == null)
            {
                return false;
            }

            List<IWebServiceTheme> themes = Themes;
            if (themes == null)
            {
                return false;
            }

            #region Check for visible Layers
            bool visFound = false;
            foreach (IWebServiceTheme theme in themes)
            {
                if (!theme.Visible)
                {
                    continue;
                }

                if (theme.MinimumScale > 1 && theme.MinimumScale > display.MapScale)
                {
                    continue;
                }

                if (theme.MaximumScale > 1 && theme.MaximumScale < display.MapScale)
                {
                    continue;
                }

                visFound = true;
                break;
            }
            if (!visFound)
            {
                if (_image != null)
                {
                    _image.Dispose();
                    _image = null;
                }
                return true;
            }
            #endregion

            string server = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "service");
            string user = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "user");
            string pwd = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "pwd");
            IServiceRequestContext context = display.Map as IServiceRequestContext;

            //if ((user == "#" || user == "$") &&
            //    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
            //{
            //    string roles = String.Empty;
            //    if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
            //    {
            //        foreach (string role in context.ServiceRequest.Identity.UserRoles)
            //        {
            //            if (String.IsNullOrEmpty(role)) continue;
            //            roles += "|" + role;
            //        }
            //    }
            //    user = context.ServiceRequest.Identity.UserName + roles;
            //    pwd = context.ServiceRequest.Identity.HashedPassword;
            //}

            dotNETConnector connector = new dotNETConnector();
            if (!String.IsNullOrEmpty(user) || !String.IsNullOrEmpty(pwd))
            {
                connector.setAuthentification(user, pwd);
            }

            if (_dataset.State != DatasetState.opened)
            {
                if (!await _dataset.Open(context))
                {
                    return false;
                }
            }

            ISpatialReference sRef = (display.SpatialReference != null) ?
                display.SpatialReference.Clone() as ISpatialReference :
                null;

            int iWidth = display.ImageWidth;
            int iHeight = display.ImageHeight;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version='1.0' encoding='utf-8'?>");
                sb.Append("<ARCXML version='1.1'>");
                sb.Append("<REQUEST>");
                sb.Append("<GET_IMAGE>");
                sb.Append("<PROPERTIES>");
                IEnvelope bounds = display.DisplayTransformation.TransformedBounds(display);
                if (display.DisplayTransformation.UseTransformation == true)
                {
                    iWidth = (int)(bounds.Width * display.Dpm / display.MapScale);
                    iHeight = (int)(bounds.Height * display.Dpm / display.MapScale);
                }
                sb.Append("<ENVELOPE minx='" + bounds.minx.ToString() + "' miny='" + bounds.miny.ToString() + "' maxx='" + bounds.maxx.ToString() + "' maxy='" + bounds.maxy.ToString() + "' />");
                sb.Append("<IMAGESIZE width='" + iWidth + "' height='" + iHeight + "' />");
                sb.Append("<BACKGROUND color='" + Color2AXL(display.BackgroundColor) + "' transcolor='" + Color2AXL(display.TransparentColor) + "' />");

                string propertyString = _dataset._properties.PropertyString;
                if (propertyString != String.Empty)
                {
                    sb.Append(_dataset._properties.PropertyString);
                }
                else
                {
                    if (sRef != null)
                    {
                        //if (this.SpatialReference != null && !display.SpatialReference.Equals(this.SpatialReference))
                        {
                            string wkt = gView.Framework.Geometry.SpatialReference.ToESRIWKT(sRef);
                            string geotranwkt = gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(sRef);

                            if (wkt != null)
                            {
                                //wkt = "PROJCS[\"MGI_M31\",GEOGCS[\"GCS_MGI\",DATUM[\"D_MGI\",SPHEROID[\"Bessel_1841\",6377397.155,0]],PRIMEM[\"Greenwich\",0.0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"False_Easting\",450000],PARAMETER[\"False_Northing\",-5000000],PARAMETER[\"Central_Meridian\",13.3333333333333],PARAMETER[\"Scale_Factor\",1],PARAMETER[\"latitude_of_origin\",0],UNIT[\"Meter\",1]]";
                                //wkt = "PROJCS[\"MGI_M31\",GEOGCS[\"GCS_MGI\",DATUM[\"D_MGI\",SPHEROID[\"Bessel_1841\",6377397.155,299.1528128]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"False_Easting\",450000.0],PARAMETER[\"False_Northing\",-5000000.0],PARAMETER[\"Central_Meridian\",13.33333333333333],PARAMETER[\"Scale_Factor\",1.0],PARAMETER[\"Latitude_Of_Origin\",0.0],UNIT[\"Meter\",1.0]]";

                                //geotranwkt = "GEOGTRAN[\"MGISTMK_To_WGS_1984\",GEOGCS[\"MGISTMK\",DATUM[\"Militar_Geographische_Institute_STMK\",SPHEROID[\"Bessel_1841\",6377397.155,299.1528128]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],METHOD[\"Position_Vector\"],PARAMETER[\"X_Axis_Translation\",577.326],PARAMETER[\"Y_Axis_Translation\",90.129],PARAMETER[\"Z_Axis_Translation\",463.919],PARAMETER[\"X_Axis_Rotation\",5.1365988],PARAMETER[\"Y_Axis_Rotation\",1.4742],PARAMETER[\"Z_Axis_Rotation\",5.2970436],PARAMETER[\"Scale_Difference\",2.4232]]";
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
                                //sb.Append("<FEATURECOORDSYS string=\"" + wkt + "\" datumtransformid=\"8415\" />");
                                //sb.Append("<FILTERCOORDSYS string=\"" + wkt + "\" datumtransformid=\"8415\" />");
                            }
                        }
                    }
                }

                sb.Append("<LAYERLIST>");
                foreach (IWebServiceTheme theme in themes)
                {
                    sb.Append("<LAYERDEF id='" + theme.LayerID + "' visible='" + (theme.Visible && !theme.Locked).ToString() + "'");
                    XmlNode xmlnode;
                    if (LayerRenderer.TryGetValue(theme.LayerID, out xmlnode))
                    {
                        sb.Append(">\n" + xmlnode.OuterXml + "\n</LAYERDEF>");
                    }
                    else if (theme.FeatureRenderer != null)
                    {
                        string renderer = ObjectFromAXLFactory.ConvertToAXL(theme.FeatureRenderer);
                        sb.Append(">\n" + renderer + "\n</LAYERDEF>");
                    }
                    else
                    {
                        sb.Append("/>");
                    }
                }
                sb.Append("</LAYERLIST>");
                sb.Append("</PROPERTIES>");
                foreach (XmlNode additional in this.AppendedLayers)
                {
                    if (additional != null)
                    {
                        sb.Append(additional.OuterXml);
                    }
                }
                sb.Append("</GET_IMAGE>");
                sb.Append("</REQUEST>");
                sb.Append("</ARCXML>");

#if(DEBUG)
                gView.Framework.system.Logger.LogDebug("Start ArcXML SendRequest");
#endif
                await ArcIMSClass.LogAsync(display as IServiceRequestContext, "GetImage Request", server, service, sb);
                string resp = connector.SendRequest(sb, server, service);
                await ArcIMSClass.LogAsync(display as IServiceRequestContext, "GetImage Response", server, service, resp);
#if(DEBUG)
                gView.Framework.system.Logger.LogDebug("ArcXML SendRequest Finished");
#endif

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                XmlNode outputNode = doc.SelectSingleNode("//IMAGE/OUTPUT");
                XmlNode envelopeNode = doc.SelectSingleNode("//IMAGE/ENVELOPE");

                if (ModifyResponseOuput != null)
                {
                    ModifyResponseOuput(this, new ModifyOutputEventArgs(outputNode));
                }

                if (_image != null)
                {
                    _image.Dispose();
                    _image = null;
                }

#if(DEBUG)
                //gView.Framework.system.Logger.LogDebug("Start ArcXML DownloadImage");
#endif
                IBitmap bitmap = null;
                if (outputNode != null)
                {
#if(DEBUG)
                    gView.Framework.system.Logger.LogDebug("Start ArcXML DownloadImage");
#endif
                    bitmap = WebFunctions.DownloadImage(outputNode/*_dataset._connector.Proxy*/);
#if(DEBUG)
                    gView.Framework.system.Logger.LogDebug("ArcXML DownloadImage Finished");
#endif
                }
                else
                {
                    bitmap = null;
                }
#if(DEBUG)
                //gView.Framework.system.Logger.LogDebug("ArcXML DownloadImage Finished");
#endif

                if (bitmap != null)
                {
                    _image = new GeorefBitmap(bitmap);
                    //_image.MakeTransparent(display.TransparentColor);
                    if (envelopeNode != null &&
                        envelopeNode.Attributes["minx"] != null &&
                        envelopeNode.Attributes["miny"] != null &&
                        envelopeNode.Attributes["maxx"] != null &&
                        envelopeNode.Attributes["maxy"] != null)
                    {
                        _image.Envelope = new Envelope(
                            Convert.ToDouble(envelopeNode.Attributes["minx"].Value.Replace(".", ",")),
                            Convert.ToDouble(envelopeNode.Attributes["miny"].Value.Replace(".", ",")),
                            Convert.ToDouble(envelopeNode.Attributes["maxx"].Value.Replace(".", ",")),
                            Convert.ToDouble(envelopeNode.Attributes["maxy"].Value.Replace(".", ",")));
                    }
                    _image.SpatialReference = display.SpatialReference;

                    if (AfterMapRequest != null)
                    {
                        AfterMapRequest(this, display, _image);
                    }
                }
                return _image != null;
            }
            catch (Exception ex)
            {
                await ArcIMSClass.ErrorLog(context, "MapRequest", server, service, ex);
                return false;
            }
        }

        async public Task<bool> LegendRequest(gView.Framework.Carto.IDisplay display)
        {
            if (_dataset == null)
            {
                return false;
            }

            List<IWebServiceTheme> themes = Themes;
            if (themes == null)
            {
                return false;
            }

            #region Check for visible Layers
            bool visFound = false;
            foreach (IWebServiceTheme theme in themes)
            {
                if (!theme.Visible)
                {
                    continue;
                }

                if (theme.MinimumScale > 1 && theme.MinimumScale > display.MapScale)
                {
                    continue;
                }

                if (theme.MaximumScale > 1 && theme.MaximumScale < display.MapScale)
                {
                    continue;
                }

                visFound = true;
                break;
            }
            if (!visFound)
            {
                if (_legend != null)
                {
                    _legend.Dispose();
                    _legend = null;
                }
                return true;
            }
            #endregion

            string server = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "service");
            string user = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "user");
            string pwd = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "pwd");
            IServiceRequestContext context = display.Map as IServiceRequestContext;

            //if ((user == "#" || user == "$") &&
            //    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
            //{
            //    string roles = String.Empty;
            //    if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
            //    {
            //        foreach (string role in context.ServiceRequest.Identity.UserRoles)
            //        {
            //            if (String.IsNullOrEmpty(role)) continue;
            //            roles += "|" + role;
            //        }
            //    }
            //    user = context.ServiceRequest.Identity.UserName + roles;
            //    pwd = context.ServiceRequest.Identity.HashedPassword;
            //}

            dotNETConnector connector = new dotNETConnector();
            if (!String.IsNullOrEmpty(user) || !String.IsNullOrEmpty(pwd))
            {
                connector.setAuthentification(user, pwd);
            }

            if (_dataset.State != DatasetState.opened)
            {
                if (!await _dataset.Open(context))
                {
                    return false;
                }
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version='1.0' encoding='utf-8'?>");
                sb.Append("<ARCXML version='1.1'>");
                sb.Append("<REQUEST>");
                sb.Append("<GET_IMAGE>");
                sb.Append("<PROPERTIES>");
                sb.Append("<ENVELOPE minx='" + display.Envelope.minx.ToString() + "' miny='" + display.Envelope.miny.ToString() + "' maxx='" + display.Envelope.maxx.ToString() + "' maxy='" + display.Envelope.maxy.ToString() + "' />");
                sb.Append("<IMAGESIZE width='" + display.ImageWidth + "' height='" + display.ImageHeight + "' />");
                sb.Append("<BACKGROUND color='255,255,255' transcolor='255,255,255' />");

                sb.Append(_dataset._properties.PropertyString);

                sb.Append("<LAYERLIST>");
                foreach (IWebServiceTheme theme in themes)
                {
                    sb.Append("<LAYERDEF id='" + theme.LayerID + "' visible='" + (theme.Visible && !theme.Locked).ToString() + "'");
                    XmlNode xmlnode;
                    if (LayerRenderer.TryGetValue(theme.LayerID, out xmlnode))
                    {
                        sb.Append(">\n" + xmlnode.OuterXml + "\n</LAYERDEF>");
                    }
                    else if (theme.FeatureRenderer != null)
                    {
                        string renderer = ObjectFromAXLFactory.ConvertToAXL(theme.FeatureRenderer);
                        sb.Append(">\n" + renderer + "\n</LAYERDEF>");
                    }
                    else
                    {
                        sb.Append("/>");
                    }
                }
                sb.Append("</LAYERLIST>");
                sb.Append("<DRAW map=\"false\" />");
                sb.Append("<LEGEND font=\"Arial\" autoextend=\"true\" columns=\"1\" width=\"165\" height=\"170\" backgroundcolor=\"255,255,255\" layerfontsize=\"11\" valuefontsize=\"10\">");
                sb.Append("<LAYERS />");
                sb.Append("</LEGEND>");
                sb.Append("</PROPERTIES>");
                foreach (XmlNode additional in this.AppendedLayers)
                {
                    sb.Append(additional.OuterXml);
                }
                sb.Append("</GET_IMAGE>");
                sb.Append("</REQUEST>");
                sb.Append("</ARCXML>");

                await ArcIMSClass.LogAsync(display as IServiceRequestContext, "GetLegend Request", server, service, sb);
                string resp = connector.SendRequest(sb, server, service);
                await ArcIMSClass.LogAsync(display as IServiceRequestContext, "GetLegend Response", server, service, resp);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                XmlNode output = doc.SelectSingleNode("//LEGEND");

                if (ModifyResponseOuput != null)
                {
                    ModifyResponseOuput(this, new ModifyOutputEventArgs(output));
                }

                if (_legend != null)
                {
                    _legend.Dispose();
                }

                _legend = WebFunctions.DownloadImage(output);
                return true;
            }
            catch (Exception ex)
            {
                await ArcIMSClass.ErrorLog(context, "LegendRequest", server, service, ex);
                return false;
            }
        }

        GeorefBitmap IWebServiceClass.Image
        {
            get { return _image; }
        }

        GraphicsEngine.Abstraction.IBitmap IWebServiceClass.Legend
        {
            get { return _legend; }
        }

        public IEnvelope Envelope
        {
            get;
            private set;
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
                    return _dataset._themes;
                }
                return new List<IWebServiceTheme>();
            }
        }

        internal ISpatialReference _sptatialReference;
        public ISpatialReference SpatialReference
        {
            get { return _sptatialReference; }
            set
            {
                _sptatialReference = value;
                if (_dataset != null)
                {
                    _dataset.SetSpatialReference(value);
                }
            }
        }

        #endregion

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

        #endregion

        #region IClone Member

        public object Clone()
        {
            ArcIMSClass clone = new ArcIMSClass(_dataset);
            clone._clonedThemes = new List<IWebServiceTheme>();

            var themes = (_clonedThemes != null) ? _clonedThemes : (_dataset?._themes ?? new List<IWebServiceTheme>());

            foreach (IWebServiceTheme theme in themes)
            {
                if (theme == null || theme.Class == null)
                {
                    continue;
                }

                clone._clonedThemes.Add(LayerFactory.Create(theme.Class, theme, clone) as IWebServiceTheme);
            }
            clone.AfterMapRequest = AfterMapRequest;
            clone.ModifyResponseOuput = ModifyResponseOuput;
            return clone;
        }

        #endregion

        private string Color2AXL(GraphicsEngine.ArgbColor col)
        {
            return col.R + "," + col.G + "," + col.B;
        }

        private List<XmlNode> _appendedLayers = new List<XmlNode>();
        internal List<XmlNode> AppendedLayers
        {
            get { return _appendedLayers; }
        }

        private Dictionary<string, XmlNode> _layerRenderer = new Dictionary<string, XmlNode>();
        internal Dictionary<string, XmlNode> LayerRenderer
        {
            get { return _layerRenderer; }
        }

        async public static Task LogAsync(IServiceRequestContext context, string header, string server, string service, string axl)
        {
            if (context == null ||
                context.MapServer == null ||
                context.MapServer.LoggingEnabled(loggingMethod.request_detail_pro) == false)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("\n");
            sb.Append(header);
            sb.Append("\n");
            sb.Append("Server: " + server + " Service: " + service);
            sb.Append("\n");
            sb.Append(axl);

            await context.MapServer.LogAsync(context, "gView.Interoperability.ArcXML", loggingMethod.request_detail_pro, sb.ToString());
        }
        async public static Task LogAsync(IServiceRequestContext context, string header, string server, string service, StringBuilder axl)
        {
            if (context == null ||
                context.MapServer == null ||
                context.MapServer.LoggingEnabled(loggingMethod.request_detail_pro) == false)
            {
                return;
            }

            await LogAsync(context, header, server, service, axl.ToString());
        }
        async public static Task ErrorLog(IServiceRequestContext context, string header, string server, string service, Exception ex)
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

            await context.MapServer.LogAsync(context, server + "-" + service + ": " + header, loggingMethod.error,
                msg.ToString() +
                ex.Source + "\n" +
                ex.StackTrace + "\n");
        }
    }

    internal class ModifyOutputEventArgs : EventArgs
    {
        public XmlNode OutputNode = null;

        public ModifyOutputEventArgs(XmlNode outputNode)
        {
            OutputNode = outputNode;
        }
    }
}
