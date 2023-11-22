using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.Web;
using gView.GraphicsEngine.Abstraction;
using gView.Interoperability.OGC.Dataset.WFS;
using gView.Interoperability.OGC.SLD;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.OGC.Dataset.WMS
{
    public class WMSClass : IWebServiceClass
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        private static object lockThis = new object();

        private string _name;
        private WMSDataset _dataset;
        private List<IWebServiceTheme> _themes = new List<IWebServiceTheme>();
        private IBitmap _legend = null;
        private GeorefBitmap _image = null;
        private IEnvelope _envelope;
        private GetCapabilities _getCapabilities;
        private GetMap _getMap;
        private GetFeatureInfo _getFeatureInfo;
        private DescribeLayer _describeLayer;
        private GetLegendGraphic _getLegendGraphic;
        private GetStyles _getStyles;
        private WMSExceptions _exceptions;
        private SRS _srs;
        private UserDefinedSymbolization _userDefinedSymbolization;
        private bool _use_SLD_BODY = true;
        private ISpatialReference _sRef = null;
        private List<IWebServiceTheme> _clonedThemes = null;

        public WMSClass(WMSDataset dataset)
        {
            _dataset = dataset;
            if (_dataset != null)
            {
                _name = _dataset._name;
            }
        }

        internal void Init(string CapabilitiesString, WFSDataset wfsDataset)
        {
            try
            {
                _themes = new List<IWebServiceTheme>();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(CapabilitiesString);

                XmlNode CapabilitiesNode = doc.SelectSingleNode("WMT_MS_Capabilities/Capability");
                _getCapabilities = new GetCapabilities(CapabilitiesNode.SelectSingleNode("Request/GetCapabilities"));
                _getMap = new GetMap(CapabilitiesNode.SelectSingleNode("Request/GetMap"));
                _getFeatureInfo = new GetFeatureInfo(CapabilitiesNode.SelectSingleNode("Request/GetFeatureInfo"));
                _describeLayer = new DescribeLayer(CapabilitiesNode.SelectSingleNode("Request/DescribeLayer"));
                _getLegendGraphic = new GetLegendGraphic(CapabilitiesNode.SelectSingleNode("Request/GetLegendGraphic"));
                _getStyles = new GetStyles(CapabilitiesNode.SelectSingleNode("Request/GetStyles"));
                _exceptions = new WMSExceptions(CapabilitiesNode.SelectSingleNode("Exception"));
                _userDefinedSymbolization = new UserDefinedSymbolization(CapabilitiesNode.SelectSingleNode("UserDefinedSymbolization"));

                XmlNode service = CapabilitiesNode.SelectSingleNode("Layer");
                XmlNode title = service.SelectSingleNode("Title");
                if (title != null)
                {
                    _name = title.InnerText;
                }

                _srs = new SRS(service);
                this.SRSCode = _srs.Srs[_srs.SRSIndex];

                foreach (XmlNode layer in service.SelectNodes("Layer"))
                {
                    string name = "", Title = "";
                    XmlNode nameNode = layer.SelectSingleNode("Name");
                    XmlNode titleNode = layer.SelectSingleNode("Title");

                    if (nameNode == null)
                    {
                        continue;
                    }

                    name = Title = nameNode.InnerText;
                    if (titleNode != null)
                    {
                        Title = titleNode.InnerText;
                    }

                    XmlNodeList styles = layer.SelectNodes("Style");

                    WFSFeatureClass wfsFc = null;
                    if (wfsDataset != null)
                    {
                        IDatasetElement wfsElement = wfsDataset[name];
                        if (wfsElement != null && wfsElement.Class is WFSFeatureClass)
                        {
                            wfsFc = wfsElement.Class as WFSFeatureClass;
                        }
                    }

                    if (styles.Count == 0)
                    {
                        IClass wClass = null;
                        if (wfsFc != null)
                        {
                            wClass = wfsFc;
                        }
                        else if (layer.Attributes["queryable"] != null && layer.Attributes["queryable"].Value == "1")
                        {
                            wClass = new WMSQueryableThemeClass(_dataset, name, String.Empty, layer, _getFeatureInfo, _srs, _exceptions);
                        }
                        else
                        {
                            wClass = new WMSThemeClass(_dataset, name, String.Empty, layer);
                        }

                        IWebServiceTheme theme;
                        if (wClass is WFSFeatureClass)
                        {
                            theme = new WebServiceTheme2(
                                wClass, Title, name, true, this);
                        }
                        else
                        {
                            theme = new WebServiceTheme(
                                wClass, Title, name, true, this
                                );
                        }

                        _themes.Add(theme);
                    }
                    else
                    {
                        foreach (XmlNode style in styles)
                        {
                            string sName = "", sTitle = "";
                            XmlNode sNameNode = style.SelectSingleNode("Name");
                            XmlNode sTitleNode = style.SelectSingleNode("Title");

                            if (sNameNode == null)
                            {
                                continue;
                            }

                            sName = sTitle = sNameNode.InnerText;
                            if (sTitleNode != null)
                            {
                                sTitle = sTitleNode.InnerText;
                            }

                            IClass wClass = null;
                            if (wfsFc is WFSFeatureClass)
                            {
                                wClass = wfsFc;
                                ((WFSFeatureClass)wClass).Style = sName;
                            }
                            else if (layer.Attributes["queryable"] != null && layer.Attributes["queryable"].Value == "1")
                            {
                                wClass = new WMSQueryableThemeClass(_dataset, name, sName, layer, _getFeatureInfo, _srs, _exceptions);
                            }
                            else
                            {
                                wClass = new WMSThemeClass(_dataset, name, sName, layer);
                            }

                            IWebServiceTheme theme;
                            if (wClass is WFSFeatureClass)
                            {
                                theme = new WebServiceTheme2(
                                    wClass, Title + " (Style=" + sTitle + ")", name, true, this);
                            }
                            else
                            {
                                theme = new WebServiceTheme(
                                    wClass, Title + " (Style=" + sTitle + ")", name, true, this
                                    );
                            }

                            _themes.Add(theme);
                        }
                    }
                }
                doc = null;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
            }
        }

        public string[] SRSCodes
        {
            get
            {
                if (_srs == null)
                {
                    return null;
                }

                string[] codes = new string[_srs.Srs.Count];
                int i = 0;
                foreach (string code in _srs.Srs)
                {
                    codes[i++] = code;
                }

                return codes;
            }
        }

        public string SRSCode
        {
            get
            {
                if (_srs == null || _srs.Srs == null || _srs.Srs.Count <= _srs.SRSIndex)
                {
                    return null;
                }

                return _srs.Srs[_srs.SRSIndex];
            }
            set
            {
                if (_srs == null || value == null)
                {
                    return;
                }

                for (int i = 0; i < _srs.Srs.Count; i++)
                {
                    if (_srs.Srs[i].ToLower() == value.ToLower())
                    {
                        _srs.SRSIndex = i;
                        break;
                    }
                }

                _sRef = gView.Framework.Geometry.SpatialReference.FromID(_srs.Srs[_srs.SRSIndex]);
                if (_srs.LatLonBoundingBox != null)
                {
                    _envelope = GeometricTransformerFactory.Transform2D(
                        _srs.LatLonBoundingBox,
                        gView.Framework.Geometry.SpatialReference.FromID("EPSG:4326"),
                        _sRef).Envelope;
                }
            }
        }

        public string[] FeatureInfoFormats
        {
            get
            {
                if (_getFeatureInfo == null || _getFeatureInfo.Formats == null)
                {
                    return null;
                }

                string[] formats = new string[_getFeatureInfo.Formats.Count];
                int i = 0;
                foreach (string format in _getFeatureInfo.Formats)
                {
                    formats[i++] = format;
                }

                return formats;
            }
        }

        public string FeatureInfoFormat
        {
            get
            {
                if (_getFeatureInfo == null || _getFeatureInfo.Formats == null ||
                    _getFeatureInfo.Formats.Count <= _getFeatureInfo.FormatIndex)
                {
                    return null;
                }

                return _getFeatureInfo.Formats[_getFeatureInfo.FormatIndex];
            }
            set
            {
                if (_getFeatureInfo == null || _getFeatureInfo.Formats == null)
                {
                    return;
                }

                for (int i = 0; i < _getFeatureInfo.Formats.Count; i++)
                {
                    if (_getFeatureInfo.Formats[i].ToLower() == value.ToLower())
                    {
                        _getFeatureInfo.FormatIndex = i;
                        break;
                    }
                }
            }
        }

        public string[] GetMapFormats
        {
            get
            {
                if (_getMap == null || _getMap.Formats == null)
                {
                    return null;
                }

                string[] formats = new string[_getMap.Formats.Count];
                int i = 0;
                foreach (string format in _getMap.Formats)
                {
                    formats[i++] = format;
                }

                return formats;
            }
        }

        public string GetMapFormat
        {
            get
            {
                if (_getMap == null || _getMap.Formats == null)
                {
                    return null;
                }

                return _getMap.Formats[_getMap.FormatIndex];
            }
            set
            {
                if (_getMap == null || _getMap.Formats == null)
                {
                    return;
                }

                for (int i = 0; i < _getMap.Formats.Count; i++)
                {
                    if (_getMap.Formats[i].ToLower() == value.ToLower())
                    {
                        _getMap.FormatIndex = i;
                        break;
                    }
                }
            }
        }

        public bool SupportSLD
        {
            get
            {
                if (_userDefinedSymbolization == null)
                {
                    return false;
                }

                return _userDefinedSymbolization.SupportSLD;
            }
        }

        public bool UseSLD_BODY
        {
            get { return _use_SLD_BODY; }
            set { _use_SLD_BODY = false; }
        }

        #region IWebServiceClass Member

        public event AfterMapRequestEventHandler AfterMapRequest = null;

        async public Task<bool> MapRequest(gView.Framework.Carto.IDisplay display)
        {
            if (_srs == null)
            {
                return false;
            }

            if (!_dataset.IsOpened)
            {
                if (!await _dataset.Open())
                {
                    return false;
                }
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

            #endregion Check for visible Layers

            int iWidth = display.ImageWidth;
            int iHeight = display.ImageHeight;

            IEnvelope displayEnv = display.Envelope;

            if (display.SpatialReference != null && !display.SpatialReference.Equals(this.SpatialReference))
            {
                displayEnv = GeometricTransformerFactory.Transform2D(displayEnv, display.SpatialReference, this.SpatialReference).Envelope;
                iHeight = (int)((displayEnv.Height / displayEnv.Width) * iWidth);
            }

            StyledLayerDescriptorWriter sldWriter = null;
            StringBuilder request = new StringBuilder("VERSION=1.1.1&REQUEST=GetMap");
            StringBuilder layers = new StringBuilder(), styles = new StringBuilder();
            foreach (IWebServiceTheme theme in themes)
            {
                if (!theme.Visible || theme.Locked ||
                    (!(theme.Class is WMSThemeClass) &&
                     !(theme.Class is WFSFeatureClass)))
                {
                    continue;
                }

                if (layers.Length > 0)
                {
                    layers.Append(",");
                    styles.Append(",");
                }
                layers.Append(theme.Class.Name);
                if (theme.Class is IWMSStyle)
                {
                    styles.Append(((IWMSStyle)theme.Class).Style);
                }

                if (theme.FeatureRenderer != null &&
                    _userDefinedSymbolization.SupportSLD &&
                    _userDefinedSymbolization.UserStyle)
                {
                    SLDRenderer sldRenderer = null;
                    if (theme.FeatureRenderer is SLDRenderer)
                    {
                        sldRenderer = (SLDRenderer)theme.FeatureRenderer;
                    }
                    else
                    {
                        //if (theme.FilterQuery is ISpatialFilter)
                        //{
                        //    IGeometry pGeometry = GeometricTransformer.Transform2D(
                        //        ((ISpatialFilter)theme.FilterQuery).Geometry,
                        //        display.SpatialReference,
                        //        this.SpatialReference);

                        //    IGeometry oGeometry = ((ISpatialFilter)theme.FilterQuery).Geometry;
                        //    ((ISpatialFilter)theme.FilterQuery).Geometry = pGeometry;
                        //    sldRenderer = new SLDRenderer(theme);
                        //    ((ISpatialFilter)theme.FilterQuery).Geometry = oGeometry;
                        //}
                        //else
                        {
                            sldRenderer = await SLDRenderer.CreateAync(theme);
                            if (display.SpatialReference != null)
                            {
                                sldRenderer.SetDefaultSrsName(display.SpatialReference.Name);
                            }
                        }
                    }
                    if (sldWriter == null)
                    {
                        sldWriter = new StyledLayerDescriptorWriter();
                    }

                    sldWriter.WriteNamedLayer(theme.Class.Name, sldRenderer);
                }
            }
            request.Append("&LAYERS=" + layers.ToString());
            if (sldWriter != null)
            {
                string sld = sldWriter.ToLineString();
                if (!_use_SLD_BODY && !string.IsNullOrEmpty(MapServerConfig.DefaultOutputPath))
                {
                    string sldFilename = "style_" + Guid.NewGuid().ToString("N") + ".sld";
                    StreamWriter sw = new StreamWriter(MapServerConfig.DefaultOutputPath + @"/" + sldFilename);
                    sw.WriteLine(sldWriter.ToString());
                    sw.Close();

                    request.Append("&SLD=" + MapServerConfig.DefaultOutputUrl + "/" + sldFilename);
                }
                else
                {
                    request.Append("&SLD_BODY=" + sld.Replace("#", "%23"));
                }
            }
            else
            {
                request.Append("&STYLES=" + styles.ToString());
            }
            if (_exceptions != null && _exceptions.Formats.Count > 0)
            {
                request.Append("&EXCEPTIONS=" + _exceptions.Formats[0]);
            }
            request.Append("&SRS=" + _srs.Srs[_srs.SRSIndex]);
            request.Append("&WIDTH=" + iWidth);
            request.Append("&HEIGHT=" + iHeight);
            request.Append("&FORMAT=" + _getMap.Formats[_getMap.FormatIndex]);
            request.Append("&BBOX=" +
                displayEnv.minx.ToString(_nhi) + "," +
                displayEnv.miny.ToString(_nhi) + "," +
                displayEnv.maxx.ToString(_nhi) + "," +
                displayEnv.maxy.ToString(_nhi));
            //request.Append("&BGCOLOR=FFFFFF");
            request.Append("&TRANSPARENT=TRUE");

            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }
            IBitmap bitmap = null;
            //if (_getMap.Post_OnlineResource != String.Empty && sldWriter != null)
            //{
            //    //bm = WebFunctions.DownloadImage(WMSDataset.Append2Url(_getMap.Post_OnlineResource, request.ToString() + "&SLD="),
            //    //    UTF8Encoding.UTF8.GetBytes(sldWriter.ToString()));
            //    bm = WebFunctions.DownloadImage(_getMap.Post_OnlineResource,
            //        UTF8Encoding.UTF8.GetBytes(request.ToString()));
            //}
            //else
            {
#if(DEBUG)
                gView.Framework.system.Logger.LogDebug("Start WMS DownloadImage");
#endif
                string url = WMSDataset.Append2Url(_getMap.Get_OnlineResource, request.ToString());
                try
                {
                    bitmap = WebFunctions.DownloadImage(url,
                        ConfigTextStream.ExtractValue(_dataset._connectionString, "usr"),
                        ConfigTextStream.ExtractValue(_dataset._connectionString, "pwd"));
                }
                catch (Exception ex)
                {
                    await WMSClass.ErrorLogAsync(display.Map as IServiceRequestContext, "MapRequest", url, ex);
                    return false;
                }
#if(DEBUG)
                gView.Framework.system.Logger.LogDebug("WMS DownloadImage Finished");
#endif
            }
            if (bitmap != null)
            {
                _image = new GeorefBitmap(bitmap);
                _image.Envelope = displayEnv;
                _image.SpatialReference = this.SpatialReference;

                if (AfterMapRequest != null)
                {
                    AfterMapRequest(this, display, _image);
                }
            }
            return _image != null;
        }

        public Task<bool> LegendRequest(gView.Framework.Carto.IDisplay display)
        {
            return Task.FromResult(false);
        }

        public GeorefBitmap Image
        {
            get { return _image; }
        }

        public IBitmap Legend
        {
            get { return _legend; }
        }

        public gView.Framework.Geometry.IEnvelope Envelope
        {
            get
            {
                IEnvelope ret = null;
                if (_srs != null &&
                    _srs.SRSIndex >= 0 && _srs.SRSIndex < _srs.Srs.Count)
                {
                    ret = _srs.Envelope(_srs.Srs[_srs.SRSIndex]);
                }

                if (ret == null)
                {
                    ret = new Envelope(-180, -90, 180, 90);
                }

                return ret;
            }
        }

        public gView.Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                return _sRef;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                for (int i = 0; i < _srs.Srs.Count; i++)
                {
                    if (_srs.Srs[i].ToLower() == value.Name.ToLower())
                    {
                        _srs.SRSIndex = i;
                        _sRef = value;
                        break;
                    }
                }
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

                return _themes;
            }
        }

        #endregion IWebServiceClass Member

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

        #region IClone Member

        public object Clone()
        {
            WMSClass clone = new WMSClass(_dataset);
            clone._clonedThemes = new List<IWebServiceTheme>();

            foreach (IWebServiceTheme theme in Themes)
            {
                if (theme == null || theme.Class == null)
                {
                    continue;
                }

                WebServiceTheme clonedTheme = new WebServiceTheme(
                    theme.Class, theme.Title, theme.LayerID, theme.Visible, clone);
                clonedTheme.ID = theme.ID;
                clone._clonedThemes.Add(clonedTheme);
            }

            clone._exceptions = this._exceptions;
            clone._srs = (SRS)this._srs.Clone();
            clone._sRef = this._sRef;
            clone._envelope = this._envelope;
            clone._getCapabilities = this._getCapabilities;
            clone._getFeatureInfo = this._getFeatureInfo;
            clone._getMap = this._getMap;
            clone._userDefinedSymbolization = this._userDefinedSymbolization;
            clone.AfterMapRequest = AfterMapRequest;
            return clone;
        }

        #endregion IClone Member

        #region HelperClasses

        internal class GetRequest
        {
            public string Get_OnlineResource = "";
            public string Post_OnlineResource = "";
            public List<string> Formats = new List<string>();

            public GetRequest(XmlNode node)
            {
                if (node == null)
                {
                    return;
                }

                XmlNode onlineResource = node.SelectSingleNode("DCPType/HTTP/Get/OnlineResource");
                if (onlineResource != null && onlineResource.Attributes["xlink:href"] != null)
                {
                    Get_OnlineResource = onlineResource.Attributes["xlink:href"].Value;
                }

                onlineResource = node.SelectSingleNode("DCPType/HTTP/Post/OnlineResource");
                if (onlineResource != null && onlineResource.Attributes["xlink:href"] != null)
                {
                    Post_OnlineResource = onlineResource.Attributes["xlink:href"].Value;
                }

                foreach (XmlNode format in node.SelectNodes("Format"))
                {
                    Formats.Add(format.InnerText);
                }
            }
        }

        internal class GetCapabilities : GetRequest
        {
            public GetCapabilities(XmlNode node)
                : base(node)
            {
            }
        }

        internal class GetMap : GetRequest
        {
            public int FormatIndex = -1;

            public GetMap(XmlNode node)
                : base(node)
            {
                for (int i = 0; i < Formats.Count; i++)
                {
                    switch (Formats[i].ToLower())
                    {
                        case "image/png":
                            FormatIndex = i;
                            break;

                        case "image/jpg":
                        case "image/jpeg":
                            if (FormatIndex == -1)
                            {
                                FormatIndex = i;
                            }

                            break;
                    }
                }

                if (FormatIndex == -1)
                {
                    FormatIndex = 0;
                }
            }
        }

        internal class GetFeatureInfo : GetRequest
        {
            public int FormatIndex = -1;

            public GetFeatureInfo(XmlNode node)
                : base(node)
            {
                for (int i = 0; i < Formats.Count; i++)
                {
                    switch (Formats[i].ToLower())
                    {
                        case "text/xml":
                            FormatIndex = i;
                            break;

                        case "text/html":
                            if (FormatIndex == -1)
                            {
                                FormatIndex = i;
                            }

                            break;
                    }
                }

                if (FormatIndex == -1)
                {
                    FormatIndex = 0;
                }
            }
        }

        internal class DescribeLayer : GetRequest
        {
            public int FormatIndex = -1;

            public DescribeLayer(XmlNode node)
                : base(node)
            {
                if (FormatIndex == -1 && Formats.Count > 0)
                {
                    FormatIndex = 0;
                }
            }
        }

        internal class GetLegendGraphic : GetRequest
        {
            public int FormatIndex = -1;

            public GetLegendGraphic(XmlNode node)
                : base(node)
            {
                if (FormatIndex == -1 && Formats.Count > 0)
                {
                    FormatIndex = 0;
                }
            }
        }

        internal class GetStyles : GetRequest
        {
            public int FormatIndex = -1;

            public GetStyles(XmlNode node)
                : base(node)
            {
                if (FormatIndex == -1 && Formats.Count > 0)
                {
                    FormatIndex = 0;
                }
            }
        }

        internal class WMSExceptions
        {
            public List<string> Formats = new List<string>();

            public WMSExceptions(XmlNode node)
            {
                foreach (XmlNode format in node.SelectNodes("Format"))
                {
                    Formats.Add(format.InnerText);
                }
            }
        }

        internal class SRS : IClone
        {
            public int SRSIndex = -1;
            public List<string> Srs = new List<string>();
            public IEnvelope LatLonBoundingBox = null;
            private Dictionary<string, IEnvelope> _boundaryBoxes = new Dictionary<string, IEnvelope>();

            private SRS()
            {
            }

            public SRS(XmlNode node)
                : this(node, null, String.Empty)
            {
            }

            public SRS(XmlNode node, XmlNamespaceManager ns, string nsName)
            {
                if (node == null)
                {
                    return;
                }

                if (nsName != String.Empty)
                {
                    nsName += ":";
                }

                foreach (XmlNode srs in node.SelectNodes(nsName + "SRS", ns))
                {
                    // Manche schreiben den SRS code in eine Zeile mit Leerzeichen
                    foreach (string srscode in srs.InnerText.Split(' '))
                    {
                        Srs.Add(srscode);
                        if (srscode.ToUpper() != "EPSG:4326" && SRSIndex == -1 &&
                            srscode.ToLower().StartsWith("EPSG:"))
                        {
                            SRSIndex = Srs.Count - 1;
                        }
                    }
                }

                if (Srs.Count == 0)
                {
                    Srs.Add("EPSG:4326"); // WGS84 immer verwenden, wenn nix anders vorgegeben wird...
                }

                if (SRSIndex == -1)
                {
                    SRSIndex = 0;
                }

                foreach (XmlNode box in node.SelectNodes(nsName + "BoundingBox[@SRS]", ns))
                {
                    if (box.Attributes["minx"] == null ||
                        box.Attributes["miny"] == null ||
                        box.Attributes["maxx"] == null ||
                        box.Attributes["maxy"] == null)
                    {
                        continue;
                    }

                    IEnvelope env = new Envelope(
                        double.Parse(box.Attributes["minx"].Value, _nhi),
                        double.Parse(box.Attributes["miny"].Value, _nhi),
                        double.Parse(box.Attributes["maxx"].Value, _nhi),
                        double.Parse(box.Attributes["maxy"].Value, _nhi));

                    _boundaryBoxes.Add(box.Attributes["SRS"].Value.ToUpper(), env);
                }

                XmlNode latlonbox = node.SelectSingleNode(nsName + "LatLonBoundingBox", ns);
                if (latlonbox != null &&
                        latlonbox.Attributes["minx"] != null &&
                        latlonbox.Attributes["miny"] != null &&
                        latlonbox.Attributes["maxx"] != null &&
                        latlonbox.Attributes["maxy"] != null)
                {
                    LatLonBoundingBox = new Envelope(
                        double.Parse(latlonbox.Attributes["minx"].Value, _nhi),
                        double.Parse(latlonbox.Attributes["miny"].Value, _nhi),
                        double.Parse(latlonbox.Attributes["maxx"].Value, _nhi),
                        double.Parse(latlonbox.Attributes["maxy"].Value, _nhi));
                }
            }

            public IEnvelope Envelope(string srs)
            {
                IEnvelope env = null;
                _boundaryBoxes.TryGetValue(srs.ToUpper(), out env);

                if (env == null && LatLonBoundingBox != null)
                {
                    // Projezieren
                    ISpatialReference sRef = gView.Framework.Geometry.SpatialReference.FromID(srs);
                    ISpatialReference epsg_4326 = gView.Framework.Geometry.SpatialReference.FromID("epsg:4326");

                    IGeometry geom = GeometricTransformerFactory.Transform2D(LatLonBoundingBox, epsg_4326, sRef);
                    if (geom != null)
                    {
                        env = geom.Envelope;
                    }
                }

                return (env != null) ? env : new Envelope(-180, -90, 180, 90);
            }

            #region IClone Member

            public object Clone()
            {
                SRS clone = new SRS();
                clone.SRSIndex = this.SRSIndex;
                clone.Srs = this.Srs;
                clone.LatLonBoundingBox = this.LatLonBoundingBox;
                clone._boundaryBoxes = this._boundaryBoxes;

                return clone;
            }

            #endregion IClone Member
        }

        internal class UserDefinedSymbolization
        {
            public bool SupportSLD = false;
            public bool UserLayer = false;
            public bool UserStyle = false;
            public bool RemoteWFS = false;

            public UserDefinedSymbolization(XmlNode node)
            {
                if (node == null)
                {
                    return;
                }

                if (node.Attributes["SupportSLD"] != null)
                {
                    SupportSLD = node.Attributes["SupportSLD"].Value == "1";
                }

                if (node.Attributes["UserLayer"] != null)
                {
                    UserLayer = node.Attributes["UserLayer"].Value == "1";
                }

                if (node.Attributes["UserStyle"] != null)
                {
                    UserStyle = node.Attributes["UserStyle"].Value == "1";
                }

                if (node.Attributes["RemoteWFS"] != null)
                {
                    RemoteWFS = node.Attributes["RemoteWFS"].Value == "1";
                }
            }
        }

        #endregion HelperClasses

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

        async public static Task Log(IServiceRequestContext context, string header, string server, string service, StringBuilder axl)
        {
            if (context == null ||
                context.MapServer == null ||
                context.MapServer.LoggingEnabled(loggingMethod.request_detail_pro) == false)
            {
                return;
            }

            await LogAsync(context, header, server, service, axl.ToString());
        }

        async public static Task ErrorLogAsync(IServiceRequestContext context, string header, string url, Exception ex)
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
            await context.MapServer.LogAsync(context, header + ": " + url, loggingMethod.error,
                msg.ToString() +
                ex.Source + "\n" +
                ex.StackTrace + "\n");
        }

        /*

        #region IMetadata Member

        public void ReadMetadata(IPersistStream stream)
        {
            XmlStreamOption getmap = stream.Load("GetMapFormat") as XmlStreamOption;
            if (getmap != null)
                this.GetMapFormat = getmap.Value.ToString();

            XmlStreamOption getfeatures = stream.Load("GetFeatureInfoFormat") as XmlStreamOption;
            if (getfeatures != null)
                this.FeatureInfoFormat = getfeatures.Value.ToString();

            XmlStreamOption srscodes = stream.Load("EPSGCodes") as XmlStreamOption;
            if (srscodes != null)
                this.SRSCode = srscodes.Value.ToString();
        }

        public void WriteMetadata(IPersistStream stream)
        {
            XmlStreamOption<string> getmap = new XmlStreamOption<string>(this.GetMapFormats, this.GetMapFormat);
            getmap.DisplayName = "GetMap Request Format";
            getmap.Description = "The GetMap Image Format";
            stream.Save("GetMapFormat", getmap);

            XmlStreamOption<string> getfeatures = new XmlStreamOption<string>(this.FeatureInfoFormats, this.FeatureInfoFormat);
            getfeatures.DisplayName = "GetFeatureInfo Request Format";
            getfeatures.Description = "The FeatureInfo Request Format";
            stream.Save("GetFeatureInfoFormat", getfeatures);

            XmlStreamOption<string> srscodes = new XmlStreamOption<string>(this.SRSCodes, this.SRSCode);
            stream.Save("EPSGCodes", srscodes);
        }

        #endregion IMetadata Member

         * */
    }
}