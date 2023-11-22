namespace gView.Framework.system
{
    public class WMSConfig
    {
        //static private string _onlineResource = "http://localhost/gViewPortal/wms.aspx?";
        static private string _srs = "epsg:4326";
        //static private string _imageFormat = "image/png;image/jpeg";
        //static private string _getFeatureInfoFormat = "text/plain;text/html;gml;text/xml";

        //static private bool _loaded = false;

        static public string SRS
        {
            get
            {
                return _srs;
            }
            set { _srs = value; }
        }
    }
}
