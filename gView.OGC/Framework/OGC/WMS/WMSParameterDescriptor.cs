using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.Web;
using gView.OGC.Framework.OGC.Exceptions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Color = System.Drawing.Color;

namespace gView.Framework.OGC.WMS
{
    public enum WMSRequestType
    {
        GetCapabilities,
        GetMap,
        GetFeatureInfo,
        GetLegendGraphic,
        DescriptTiles,
        GetTile,

        GenerateTiles
    }

    public enum WMSExceptionType
    {
        se_xml,
        se_blank,
        se_in_image
    }

    public enum WMSInfoFormat
    {
        gml,
        html,
        text,
        xml,
        xsl,
        geojson
    }

    public enum WMSImageFormat
    {
        png,
        gif,
        jpeg,
        bmp,
        kml
    }


    public class WMSParameterDescriptor
    {
        #region request parameters

        private int miWidth = 200;
        private int miHeight = 200;
        private int miSRS = 4326;
        private int miFeatureInfoX = 0;
        private int miFeatureInfoY = 0;
        private int miFeatureInfoMaxRows = 0;

        private WMSRequestType meRequest = WMSRequestType.GetCapabilities;
        private WMSImageFormat meFormat = WMSImageFormat.png;
        private WMSExceptionType meExceptions = WMSExceptionType.se_xml;
        private WMSInfoFormat meInfoFormat = WMSInfoFormat.html;
        private string meInfoFormatXsl = String.Empty;
        private IEnvelope moBox = null;
        private string[] msLayers = new string[0];
        private string[] msQueryLayers = new string[0];
        private string msVersion = "1.1.1";
        private bool mbTransparent = false;
        private Color moBgColor = Color.White;
        private string sldString = String.Empty;

        private string msLayer = String.Empty, msStyle = String.Empty;
        private double miScale = 0.0;
        private int miTileRow = 0, miTileCol = 0;

        private int _bboxsrs = 0, _zoomlevel = 0;
        private string _requestKey = String.Empty;

        public double dpi = 96.0;

        #endregion

        public WMSParameterDescriptor()
        {

        }

        #region Parse
        public (byte[] data, string contentType) ParseParameters(string[] parameters)
        {
            return ParseParameters(new Parameters(parameters));
        }
        private (byte[] data, string contentType) ParseParameters(Parameters request)
        {
            if (request["VERSION"] == null && request["WMTVER"] == null)
            {
                if (this.Request != WMSRequestType.GetCapabilities)
                {
                    return WriteError("mandatory VERSION parameter is either missing or erronous. Must be: 'WMTVER=1.0.0' or 'VERSION=1.x.x'");
                }
                else
                {
                    this.Version = "1.1.1";//default version
                }
            }
            else
            {
                if (request["WMTVER"] != null && request["VERSION"] == null)
                {
                    this.Version = request["WMTVER"];
                }
                else if (request["WMTVER"] == null && request["VERSION"] != null)
                {
                    this.Version = request["VERSION"];
                }
            }

            //check for exception format
            if (request["EXCEPTIONS"] != null)
            {
                if (request["EXCEPTIONS"] != "application/vnd.ogc.se_inimage" &&
                    request["EXCEPTIONS"] != "application/vnd.ogc.se_blank" &&
                    request["EXCEPTIONS"] != "application/vnd.ogc.se_xml")
                {
                    return WriteError($"Invalid exception format {request["EXCEPTIONS"]} must be 'application/vnd.ogc.se_inimage', 'application/vnd.ogc.se_blank' or 'application/vnd.ogc.se_xml'.");
                }
                else
                {
                    switch (request["EXCEPTIONS"])
                    {
                        case "application/vnd.ogc.se_inimage":
                            this.Exceptions = WMSExceptionType.se_in_image;
                            break;
                        case "application/vnd.ogc.se_blank":
                            this.Exceptions = WMSExceptionType.se_blank;
                            break;
                            /* SE_XML is already the default value
                        default: 
                             * this.Exceptions = WMSExceptionType.SE_XML;
                             * break;
                             */

                    }
                }
            }
            if (request["REQUEST"] == null)
            {
                return WriteError("mandatory REQUEST parameter is missing");
            }
            else
            {
                if (request["REQUEST"].ToUpper().IndexOf("MAP") != -1)
                {
                    this.Request = WMSRequestType.GetMap;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("CAPABILITIES") != -1)
                {
                    this.Request = WMSRequestType.GetCapabilities;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("FEATUREINFO") != -1)
                {
                    this.Request = WMSRequestType.GetFeatureInfo;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("GETLEGENDGRAPHIC") != -1)
                {
                    this.Request = WMSRequestType.GetLegendGraphic;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("DESCRIPETILES") != -1)
                {
                    this.Request = WMSRequestType.DescriptTiles;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("GETTILE") != -1)
                {
                    this.Request = WMSRequestType.GetTile;
                }
                else if (request["REQUEST"].ToUpper().IndexOf("GENERATETILES") != -1)
                {
                    this.Request = WMSRequestType.GenerateTiles;
                }
                else
                {
                    return WriteError("REQUEST parameter is either missing, erroneous, or not supported. Supported values are: 'GetMap', 'map', 'GetCapabilities','capabilities'");
                }
            }

            #region Nicht Standard (für GenerateTiles Request: WMS2Tiles für Bing Map Controll!!)
            if (this.Request == WMSRequestType.GenerateTiles)
            {
                if (request["BBOXSRS"] != null)
                {
                    if (request["BBOXSRS"].IndexOf("EPSG:") == -1)
                    {
                        return WriteError("only EPSG based coordinate systems are supported!", "InvalidBBOXSRS");
                    }

                    string[] srsid = request["BBOXSRS"].Split(':');
                    this.BBoxSRS = int.Parse(srsid[srsid.Length - 1]);
                }
                if (request["ZOOMLEVEL"] != null)
                {
                    this.ZoomLevel = int.Parse(request["ZOOMLEVEL"]);
                }
                if (request["REQUESTKEY"] != null)
                {
                    _requestKey = request["REQUESTKEY"];
                }
                if (request["TILEROW"] == null)
                {
                    return WriteError("mandatory TILEROW parameter is missing.");
                }
                else
                {
                    this.TileRow = int.Parse(request["TILEROW"]);
                }

                if (request["TILECOL"] == null)
                {
                    return WriteError("mandatory TILECOL parameter is missing.");
                }
                else
                {
                    this.TileCol = int.Parse(request["TILECOL"]);
                }
            }
            #endregion

            if (this.Request != WMSRequestType.GetCapabilities && this.Request != WMSRequestType.DescriptTiles)
            {
                if (request["SRS"] == null && request["CRS"] == null)
                {
                    return WriteError("mandatory SRS, CRS parameter is missing.");
                }
                else
                {
                    if (request["SRS"] != null)
                    {
                        if (request["SRS"].IndexOf("EPSG:") == -1)
                        {
                            return WriteError("only EPSG based coordinate systems are supported!", "InvalidSRS");
                        }

                        string[] srsid = request["SRS"].Split(':');
                        this.SRS = int.Parse(srsid[srsid.Length - 1]);
                    }
                    else if (request["CRS"] != null)
                    {
                        if (request["CRS"].IndexOf("EPSG:") == -1)
                        {
                            return WriteError("only EPSG based coordinate systems are supported!", "InvalidCRS");
                        }

                        string[] srsid = request["CRS"].Split(':');
                        this.SRS = int.Parse(srsid[srsid.Length - 1]);
                    }
                }

                if (this.Request == WMSRequestType.GetMap || this.Request == WMSRequestType.GetTile || this.Request == WMSRequestType.GetLegendGraphic)
                {
                    if (request["FORMAT"] == null)
                    {
                        return WriteError("mandatory FORMAT parameter is missing.");
                    }
                    else
                    {
                        switch (request["FORMAT"].ToLower())
                        {
                            case "image/gif":
                                this.Format = WMSImageFormat.gif; break;
                            case "image/bmp":
                                this.Format = WMSImageFormat.bmp; break;
                            case "image/jpg":
                            case "image/jpeg":
                                this.Format = WMSImageFormat.jpeg; break;
                            case "image/png":
                                this.Format = WMSImageFormat.png; break;
                            case "gif":
                                this.Format = WMSImageFormat.gif; break;
                            case "png":
                                this.Format = WMSImageFormat.png; break;
                            case "jpg":
                                this.Format = WMSImageFormat.jpeg; break;
                            case "application/vnd.google-earth.kml+xml":
                                this.Format = WMSImageFormat.kml; break;
                            default:
                                return WriteError("Format " + request["FORMAT"] + " is not supported.", "InvalidFormat");
                        }
                    }
                }

                if (this.Request == WMSRequestType.GetTile)
                {
                    if (request["LAYER"] == null)
                    {
                        return WriteError("mandatory LAYER parameter is missing.");
                    }
                    else
                    {
                        this.Layer = request["LAYER"];
                    }

                    if (request["STYLE"] == null)
                    {
                        return WriteError("mandatory STYLE parameter is missing.");
                    }
                    else
                    {
                        this.Style = request["STYLE"];
                    }

                    if (request["SCALE"] == null)
                    {
                        return WriteError("mandatory SCALE parameter is missing.");
                    }
                    else
                    {
                        this.Scale = request["SCALE"].Replace(",", ".").ToDouble();
                    }

                    if (request["TILEROW"] == null)
                    {
                        return WriteError("mandatory TILEROW parameter is missing.");
                    }
                    else
                    {
                        this.TileRow = int.Parse(request["TILEROW"]);
                    }

                    if (request["TILECOL"] == null)
                    {
                        return WriteError("mandatory TILECOL parameter is missing.");
                    }
                    else
                    {
                        this.TileCol = int.Parse(request["TILECOL"]);
                    }
                }
                else if (this.Request == WMSRequestType.GetLegendGraphic)
                {
                    if (request["LAYER"] == null)
                    {
                        return WriteError("mandatory LAYER parameter is missing.");
                    }
                    else
                    {
                        this.Layer = request["LAYER"];
                    }
                }
                else if (this.Request == WMSRequestType.GetMap || this.Request == WMSRequestType.GetFeatureInfo)
                {
                    if (request["HEIGHT"] == null)
                    {
                        return WriteError("mandatory HEIGHT parameter is missing.");
                    }
                    else
                    {
                        this.Height = int.Parse(request["HEIGHT"]);
                    }

                    if (request["WIDTH"] == null)
                    {
                        return WriteError("mandatory WIDTH parameter is missing.");
                    }
                    else
                    {
                        this.Width = int.Parse(request["WIDTH"]);
                    }

                    if (request["BBOX"] == null)
                    {
                        return WriteError("mandatory BBOX parameter is missing.");
                    }
                    string[] bbox = request["BBOX"].Split(',');
                    if (bbox.Length != 4)
                    {
                        return WriteError("Invalid BBOX parameter. Must consist of 4 elements of type double or integer");
                    }

                    double MinX = bbox[0].ToDouble();
                    double MinY = bbox[1].ToDouble();
                    double MaxX = bbox[2].ToDouble();
                    double MaxY = bbox[3].ToDouble();
                    Envelope box = new Envelope(MinX, MinY, MaxX, MaxY);
                    if (box.minx >= box.maxx ||
                        box.miny >= box.maxy)
                    {
                        return WriteError("Invalid BBOX parameter. MinX must not be greater than MaxX, and MinY must not be greater than MaxY");
                    }

                    this.BBOX = box;

                    string LayerTag = "LAYERS";
                    if (this.Request == WMSRequestType.GetFeatureInfo)
                    {
                        LayerTag = "QUERY_LAYERS";
                    }

                    if (request[LayerTag] == null)
                    {
                        return WriteError("mandatory LAYERS parameter is missing.");
                    }
                    else
                    {
                        String sLayers = request[LayerTag];
                        if (LayerTag == "LAYERS")
                        {
                            this.Layers = sLayers.Split(",".ToCharArray());
                        }
                        else if (LayerTag == "QUERY_LAYERS")
                        {
                            this.QueryLayers = sLayers.Split(",".ToCharArray());
                        }
                    }
                    if (request["TRANSPARENT"] != null && request["TRANSPARENT"].ToUpper() == "TRUE")
                    {
                        this.Transparent = true;
                    }
                    if (request["BGCOLOR"] != null)
                    {
                        this.BgColor = ColorTranslator.FromHtml(request["BGCOLOR"]);
                    }
                    if (request["STYLES"] != null)
                    {
                        String[] styles = request["STYLES"].Split(",".ToCharArray());
                        for (int i = 0; i < styles.Length; i++)
                        {
                            if (styles[i] != null && styles[i] != String.Empty)
                            {
                                return WriteError("The monoGIS does not support named styles!.", "StyleNotDefined");
                            }
                        }
                    }
                    if (request["DPI"] != null)
                    {
                        this.dpi = request["DPI"].ToDouble();
                    }
                    if (this.Request == WMSRequestType.GetMap)
                    {
                        if (request["SLD"] != null)
                        {
                            sldString = WebFunctions.DownloadXml(request["SLD"]);
                            if (sldString == null)
                            {
                                sldString = String.Empty;
                            }
                        }
                        else if (request["SLD_BODY"] != null)
                        {
                            sldString = request["SLD_BODY"];
                        }
                    }
                }

            }
            if (this.Request == WMSRequestType.GetFeatureInfo)
            {
                if (request["QUERYLAYERS"] == null)
                {
                    return WriteError("mandatory QUERYLAYERS parameter is missing.");
                }
                else
                {
                    String sQueryLayers = request["QUERYLAYERS"];
                    this.QueryLayers = sQueryLayers.Split(",".ToCharArray());
                }
                if (request["X"] == null)
                {
                    return WriteError("mandatory X parameter is missing.");
                }
                else
                {
                    this.FeatureInfoX = Convert.ToInt32(request["X"]);
                    if (this.FeatureInfoX > this.Width || this.FeatureInfoX < 0)
                    {
                        return WriteError("invalid X parameter, must be greater than 0 and lower than Width parameter.");
                    }
                }
                if (request["Y"] == null)
                {
                    return WriteError("mandatory Y parameter is missing.");
                }
                else
                {
                    this.FeatureInfoY = Convert.ToInt32(request["Y"]);
                    if (this.FeatureInfoY > this.Height || this.FeatureInfoY < 0)
                    {
                        return WriteError("invalid Y parameter, must be greater than 0 and lower than HEIGHT parameter.");
                    }
                }
                if (request["FEATURECOUNT"] != null)
                {
                    this.FeatureInfoMaxRows = Convert.ToInt32(request["FEATURECOUNT"]);
                    if (this.FeatureInfoMaxRows <= 0)
                    {
                        return WriteError("invalid FEATURECOUNT parameter, must be greater than 0.");
                    }
                }
                if (request["INFOFORMAT"] != null)
                {
                    switch (request["INFOFORMAT"].ToLower())
                    {
                        case "gml":
                        case "application/vnd.ogc.gml":
                            this.InfoFormat = WMSInfoFormat.gml;
                            break;
                        case "text/html":
                            this.InfoFormat = WMSInfoFormat.html;
                            break;
                        case "text/xml":
                            this.InfoFormat = WMSInfoFormat.xml;
                            break;
                        case "text/plain":
                            this.InfoFormat = WMSInfoFormat.text;
                            break;
                        default:
                            if (request["INFOFORMAT"].ToLower().StartsWith("xsl/"))
                            {
                                this.InfoFormat = WMSInfoFormat.xsl;
                                this.InfoFormatXsl = request["INFOFORMAT"].ToLower();
                            }
                            else
                            {
                                return WriteError("invalid INFORMAT parameter, may be: text/html, GML or application/vnd.ogc.gml.");
                            }
                            break;
                    }
                }
                else if (request["INFO_FORMAT"] != null)
                {
                    switch (request["INFO_FORMAT"].ToLower())
                    {
                        case "gml":
                        case "application/vnd.ogc.gml":
                            this.InfoFormat = WMSInfoFormat.gml;
                            break;
                        case "text/html":
                            this.InfoFormat = WMSInfoFormat.html;
                            break;
                        case "text/xml":
                            this.InfoFormat = WMSInfoFormat.xml;
                            break;
                        case "text/plain":
                            this.InfoFormat = WMSInfoFormat.text;
                            break;
                        case "application/json":
                        case "application/geojeon":
                            this.InfoFormat = WMSInfoFormat.geojson;
                            break;
                        default:
                            if (request["INFO_FORMAT"].ToLower().StartsWith("xsl/"))
                            {
                                this.InfoFormat = WMSInfoFormat.xsl;
                                this.InfoFormatXsl = request["INFO_FORMAT"].ToLower();
                            }
                            else
                            {
                                return WriteError("invalid INFORMAT parameter, may be: text/html, application/json or application/vnd.ogc.gml.");
                            }
                            break;
                    }
                }
            }

            return (null, null);
        }

        public void ParseParameters(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> dict)
        {
            var parameters = dict.Keys.Select(k => k + "=" + dict[k].ToString()).ToArray();
            var parseResult = ParseParameters(parameters);

            if(parseResult.data != null)
            {
                throw new ParseParametersException(parseResult.data, parseResult.contentType);
            }
        }

        #endregion

        #region ErrorReport
        private (byte[] data, string contentType) WriteError(String msg)
        {
            return WriteError(msg, null);
        }

        private (byte[] data, string contentType) WriteError(String msg, String code)
        {
            if (this.Format == WMSImageFormat.kml)
            {
                return WriteKMLError(msg, code);
            }

            if (this.Exceptions == WMSExceptionType.se_xml)
            {
                string sMsg = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no"" ?>
<!DOCTYPE ServiceExceptionReport SYSTEM
 ""http://www.digitalearth.gov/wmt/xml/exception_1_1_0.dtd"">
<ServiceExceptionReport version=""1.1.0"">
  <ServiceException>
    message.
  </ServiceException>     
</ServiceExceptionReport>";
                sMsg = sMsg.Replace("message.", msg);
                if (code != null)
                {
                    sMsg = sMsg.Replace("<ServiceException>", "<ServiceException code=\"" + code + "\">");
                }

                return (Encoding.UTF8.GetBytes(sMsg),
                    this.Version == "1.1.1" ? "application/vnd.ogc.se_xml" : "text/xml");
            }
            else if (this.Exceptions == WMSExceptionType.se_in_image)
            {
                using (Bitmap bitmap = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb))
                using (Graphics gr = Graphics.FromImage(bitmap))
                {
                    if (!this.Transparent)
                    {
                        gr.Clear(this.BgColor);
                    }
                    else
                    {
                        bitmap.MakeTransparent(this.BgColor);
                        gr.Clear(this.BgColor);
                    }
                    Font f = new Font("Tahoma", 12, FontStyle.Regular);
                    SizeF sz = gr.MeasureString(msg, f);
                    RectangleF rect = new RectangleF(5f, 5f, sz.Width, sz.Height);
                    gr.DrawString(msg, f, new SolidBrush(Color.Black), rect);
                    gr.Save();

                    MemoryStream memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, SystemDrawingGetImageFormat());

                    return (memoryStream.ToArray(), this.MimeType);
                }
            }
            else
            {
                using (Bitmap bitmap = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb))
                using (Graphics gr = Graphics.FromImage(bitmap))
                {
                    if (!this.Transparent)
                    {
                        gr.Clear(this.BgColor);
                    }
                    else
                    {
                        bitmap.MakeTransparent(this.BgColor);
                        gr.Clear(this.BgColor);
                    }
                    gr.Save();

                    MemoryStream memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, SystemDrawingGetImageFormat());

                    return (memoryStream.ToArray(), this.MimeType);
                }
            }

        }

        protected (byte[] data, string contentType) WriteKMLError(string msg, string code)
        {
            String sRet = String.Empty;

            sRet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            sRet += "<kml xmlns=\"http://earth.google.com/kml/2.0\">";
            sRet += "<Folder>";
            sRet += "<name>ERROR</name>";
            sRet += "<description>" + msg + "</description>";
            sRet += "</Folder>";
            sRet += "</kml>";

            return (Encoding.UTF8.GetBytes(sRet), "application/vnd.google-earth.kml+xml");
        }

        #endregion

        #region Helper
        private Bitmap CreateTransparentGif(Bitmap gif, ColorPalette cp)
        {
            //please note: the following code has been adopted from the following page:
            //GDI+ FAQ, http://www.bobpowell.net/giftransparency.htm
            //Creates a new GIF image with a modified colour palette
            if (cp != null)
            {
                //Create a new 8 bit per pixel image
                Bitmap bm = new Bitmap(gif.Width, gif.Height, PixelFormat.Format8bppIndexed);
                //get it's palette
                ColorPalette ncp = bm.Palette;
                //copy all the entries from the old palette removing any transparency
                int n = 0;
                foreach (Color c in cp.Entries)
                {
                    ncp.Entries[n++] = Color.FromArgb(255, c);
                }
                //Set the second entry as transparent color
                ncp.Entries[ncp.Entries.Length - 1] = Color.FromArgb(0, ncp.Entries[ncp.Entries.Length - 1]);

                //re-insert the palette
                bm.Palette = ncp;
                //now to copy the actual bitmap data
                //lock the source and destination bits
                BitmapData src = ((Bitmap)gif).LockBits(new Rectangle(0, 0, gif.Width, gif.Height), ImageLockMode.ReadOnly, gif.PixelFormat);
                BitmapData dst = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.WriteOnly, bm.PixelFormat);

                //uses pointers so we need unsafe code.
                //the project is also compiled with /unsafe
                unsafe
                {
                    //steps through each pixel
                    for (int y = 0; y < gif.Height; y++)
                    {
                        for (int x = 0; x < gif.Width; x++)
                        {
                            //transferring the bytes
                            ((byte*)dst.Scan0.ToPointer())[(dst.Stride * y) + x] = ((byte*)src.Scan0.ToPointer())[(src.Stride * y) + x];
                        }
                    }
                }

                //all done, unlock the bitmaps
                ((Bitmap)gif).UnlockBits(src);
                bm.UnlockBits(dst);
                return bm;
            }
            else
            {
                return null;
            }
        }

        public GraphicsEngine.ImageFormat GetImageFormat()
        {
            switch (this.Format)
            {
                case WMSImageFormat.gif:
                    return GraphicsEngine.ImageFormat.Gif;
                case WMSImageFormat.bmp:
                    return GraphicsEngine.ImageFormat.Bmp;
                case WMSImageFormat.jpeg:
                    return GraphicsEngine.ImageFormat.Jpeg;
                case WMSImageFormat.png:
                    return GraphicsEngine.ImageFormat.Png;
                default: return GraphicsEngine.ImageFormat.Png;
            }
        }

        public System.Drawing.Imaging.ImageFormat SystemDrawingGetImageFormat()
        {
            switch (this.Format)
            {
                case WMSImageFormat.gif:
                    return System.Drawing.Imaging.ImageFormat.Gif;
                case WMSImageFormat.bmp:
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                case WMSImageFormat.jpeg:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                case WMSImageFormat.png:
                    return System.Drawing.Imaging.ImageFormat.Png;
                default: return System.Drawing.Imaging.ImageFormat.Png;
            }
        }

        public string GetContentType()
        {
            switch (this.Format)
            {
                case WMSImageFormat.gif:
                    return "image/gif";
                case WMSImageFormat.bmp:
                    return "image/bmp";
                case WMSImageFormat.jpeg:
                    return "image/jpg";

                case WMSImageFormat.png:
                default:
                    return "image/png";
            }
        }

        #endregion

        public int FeatureInfoX
        {
            get { return miFeatureInfoX; }
            set { miFeatureInfoX = value; }
        }

        public int FeatureInfoY
        {
            get { return miFeatureInfoY; }
            set { miFeatureInfoY = value; }
        }

        public int FeatureInfoMaxRows
        {
            get { return miFeatureInfoMaxRows; }
            set { miFeatureInfoMaxRows = value; }
        }

        public int Width
        {
            get { return miWidth; }
            set { miWidth = value; }
        }

        public int Height
        {
            get { return miHeight; }
            set { miHeight = value; }
        }

        public int SRS
        {
            get { return miSRS; }
            set { miSRS = value; }
        }

        public int BBoxSRS
        {
            get { return _bboxsrs; }
            set { _bboxsrs = value; }
        }

        public int ZoomLevel
        {
            get { return _zoomlevel; }
            set { _zoomlevel = value; }
        }

        public string RequestKey
        {
            get { return _requestKey; }
        }

        public WMSRequestType Request
        {
            get { return meRequest; }
            set { meRequest = value; }
        }

        public WMSExceptionType Exceptions
        {
            get { return meExceptions; }
            set { meExceptions = value; }
        }

        public WMSInfoFormat InfoFormat
        {
            get { return meInfoFormat; }
            set { meInfoFormat = value; }
        }

        public string InfoFormatXsl
        {
            get { return meInfoFormatXsl; }
            set { meInfoFormatXsl = value; }
        }

        public string Version
        {
            get { return msVersion; }
            set { msVersion = value; }
        }

        public IEnvelope BBOX
        {
            get { return moBox; }
            set { moBox = value; }
        }

        public WMSImageFormat Format
        {
            get { return meFormat; }
            set { meFormat = value; }
        }

        public string[] Layers
        {
            get { return msLayers; }
            set { msLayers = value; }
        }

        public string[] QueryLayers
        {
            get { return msQueryLayers; }
            set { msQueryLayers = value; }
        }

        public Color BgColor
        {
            get { return moBgColor; }
            set { moBgColor = value; }
        }

        public bool Transparent
        {
            get { return mbTransparent; }
            set { mbTransparent = value; }
        }

        public string MimeType
        {
            get
            {
                switch (Format)
                {
                    case WMSImageFormat.bmp:
                        return "image/bmp";
                    case WMSImageFormat.gif:
                        return "image/gif";
                    case WMSImageFormat.jpeg:
                        return "image/jpeg";
                    case WMSImageFormat.png:
                        return "image/png";
                    case WMSImageFormat.kml:
                        return "application/vnd.google-earth.kml+xml";

                    default:
                        return "image/png";
                }
            }
        }

        public string Layer
        {
            get { return msLayer; }
            set { msLayer = value; }
        }
        public string Style
        {
            get { return msStyle; }
            set { msStyle = value; }
        }
        public double Scale
        {
            get { return miScale; }
            set { miScale = value; }
        }
        public int TileRow
        {
            get { return miTileRow; }
            set { miTileRow = value; }
        }
        public int TileCol
        {
            get { return miTileCol; }
            set { miTileCol = value; }
        }

        private class Parameters
        {
            private Dictionary<string, string> _parameters = new Dictionary<string, string>();

            public Parameters(string[] list)
            {
                if (list == null)
                {
                    return;
                }

                foreach (string l in list)
                {
                    string[] p = l.Split('=');

                    string p1 = p[0].Trim().ToUpper(), pp;
                    StringBuilder p2 = new StringBuilder();
                    for (int i = 1; i < p.Length; i++)
                    {
                        if (p2.Length > 0)
                        {
                            p2.Append("=");
                        }

                        p2.Append(p[i]);
                    }
                    if (_parameters.TryGetValue(p1, out pp))
                    {
                        continue;
                    }

                    _parameters.Add(p1, p2.ToString());
                }
            }

            public string this[string parameter]
            {
                get
                {
                    string o;
                    if (!_parameters.TryGetValue(parameter.ToUpper(), out o))
                    {
                        return null;
                    }

                    return o;
                }
            }
        }

        public string SLD_BODY
        {
            get { return sldString; }
        }
    }
}
