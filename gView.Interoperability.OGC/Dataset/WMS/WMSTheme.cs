using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.OGC.Dataset.WMS
{
    public interface IWMSStyle
    {
        string Style { get; }
    }

    class WMSStyle : IWMSStyle
    {
        protected string _style;

        public string Style
        {
            get { return _style; }
            internal set { _style = value; }
        }
    }

    class WMSThemeClass : WMSStyle, IClass
    {
        protected WMSDataset _dataset;
        protected string _name;

        public WMSThemeClass(WMSDataset dataset, string name, string style, XmlNode layerNode)
        {
            _dataset = dataset;
            _name = name;
            _style = style;

            if (layerNode != null)
            {
                //if (style != String.Empty)
                //{
                //    XmlNode styleNode=layerNode.SelectSingleNode("Style[@Name='" + style + "'");
                //    if (styleNode != null && layerNode.Attributes["Title"] != null)
                //        _name += " (Style=" + styleNode.Attributes["Title"].Value + ")";
                //}
            }
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

        #endregion
    }

    class WMSQueryableThemeClass : WMSThemeClass, IPointIdentify
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private WMSClass.GetFeatureInfo _getFeatureInfo;
        private WMSClass.SRS _srs;
        private WMSClass.WMSExceptions _exceptions;

        public WMSQueryableThemeClass(WMSDataset dataset, string name, string style, XmlNode layerNode, WMSClass.GetFeatureInfo getFeatureInfo, WMSClass.SRS srs, WMSClass.WMSExceptions exceptions)
            : base(dataset, name, style, layerNode)
        {
            _getFeatureInfo = getFeatureInfo;
            _srs = srs;
            _exceptions = exceptions;
        }

        #region IPointIdentify Member

        public IPointIdentifyContext CreatePointIdentifyContext() => new DummyPointIdentifyContext();
        public Task<ICursor> PointQuery(gView.Framework.Carto.IDisplay display, gView.Framework.Geometry.IPoint point, ISpatialReference sRef, IUserData userdata, IPointIdentifyContext context)
        {
            if (display == null || point == null)
            {
                return Task.FromResult<ICursor>(null);
            }

            IEnvelope dispEnvelope = display.Envelope;
            if (sRef != null)
            {
                ISpatialReference mySRef = SpatialReference.FromID(_srs.Srs[_srs.SRSIndex]);
                if (mySRef != null && !mySRef.Equals(sRef))
                {
                    // TODO:
                    // Stimmt net ganz, eigentlich wird beim Projezieren aus dem
                    // Envelope ein Polygon, auch der Punkt, der als X-Pixel, Y-Pixel
                    // übergeben wird, sollte sich ändern...
                    // --> World2Image stimmt nicht 100%
                    //
                    dispEnvelope = GeometricTransformerFactory.Transform2D(dispEnvelope, sRef, mySRef).Envelope;
                }
            }
            double x = point.X, y = point.Y;
            display.World2Image(ref x, ref y);

            StringBuilder request = new StringBuilder("VERSION=1.1.1&REQUEST=GetFeatureInfo");
            request.Append("&QUERY_LAYERS=" + this.Name);
            request.Append("&QUERYLAYERS=" + this.Name);
            request.Append("&LAYERS=" + this.Name);
            //request.Append("&LAYERS=" + this.Name);
            request.Append("&EXCEPTIONS=" + _exceptions.Formats[0]);
            request.Append("&SRS=" + _srs.Srs[_srs.SRSIndex]);
            request.Append("&WIDTH=" + display.ImageWidth);
            request.Append("&HEIGHT=" + display.ImageHeight);
            request.Append("&INFOFORMAT=" + _getFeatureInfo.Formats[_getFeatureInfo.FormatIndex]);
            request.Append("&INFO_FORMAT=" + _getFeatureInfo.Formats[_getFeatureInfo.FormatIndex]);
            request.Append("&BBOX=" + dispEnvelope.minx.ToString(_nhi) + "," +
                dispEnvelope.miny.ToString(_nhi) + "," +
                dispEnvelope.maxx.ToString(_nhi) + "," +
                dispEnvelope.maxy.ToString(_nhi));
            request.Append("&X=" + (int)x);
            request.Append("&Y=" + (int)y);

            string response;

            if (_getFeatureInfo.Formats[_getFeatureInfo.FormatIndex].ToLower().StartsWith("xsl/"))
            {
                return Task.FromResult<ICursor>(new UrlCursor(WMSDataset.Append2Url(_getFeatureInfo.Get_OnlineResource, request.ToString())));
            }
            else
            {
                switch (_getFeatureInfo.Formats[_getFeatureInfo.FormatIndex].ToLower())
                {
                    case "text/plain":
                        response = WebFunctions.HttpSendRequest(WMSDataset.Append2Url(_getFeatureInfo.Get_OnlineResource, request.ToString()));
                        return Task.FromResult<ICursor>(new TextCursor(response));
                    case "text/html":
                        return Task.FromResult<ICursor>(new UrlCursor(WMSDataset.Append2Url(_getFeatureInfo.Get_OnlineResource, request.ToString())));
                    case "text/xml":
                        response = WebFunctions.HttpSendRequest(WMSDataset.Append2Url(_getFeatureInfo.Get_OnlineResource, request.ToString()));
                        return Task.FromResult<ICursor>(new RowCursor(Xml2Rows(response)));
                    case "application/vnd.ogc.gml":
                        response = WebFunctions.HttpSendRequest(WMSDataset.Append2Url(_getFeatureInfo.Get_OnlineResource, request.ToString()));
                        return Task.FromResult<ICursor>(new RowCursor(Gml2Rows(response)));
                }
            }
            return null;
        }

        #endregion

        private List<IRow> Xml2Rows(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                IDictionary<string, string> dic = ns.GetNamespacesInScope(XmlNamespaceScope.All);

                string nsKey = String.Empty;

                if (xml.Contains("http://www.gviewgis.com/wms"))
                {
                    ns.AddNamespace("gview_wms", "http://www.gviewgis.com/wms");
                    nsKey = "gview_wms";
                }

                List<IRow> rows = new List<IRow>();

                if (!String.IsNullOrEmpty(nsKey))
                {
                    foreach (XmlNode feature in doc.SelectNodes("gview_wms:FeatureInfoResponse/gview_wms:FeatureInfoCollection/gview_wms:FeatureInfo", ns))
                    {
                        Row row = new Row();

                        foreach (XmlNode field in feature.SelectNodes("gview_wms:Field", ns))
                        {
                            XmlNode fieldName = field.SelectSingleNode("gview_wms:FieldName", ns);
                            XmlNode fieldValue = field.SelectSingleNode("gview_wms:FieldValue", ns);

                            if (fieldName != null && fieldValue != null)
                            {
                                row.Fields.Add(new FieldValue(
                                    fieldName.InnerText,
                                    fieldValue.InnerText));
                            }
                        }
                        rows.Add(row);
                    }
                }
                else
                {
                    foreach (XmlNode feature in doc.SelectNodes("//FEATURE"))
                    {
                        Row row = new Row();

                        foreach (XmlNode field in feature.SelectNodes("FIELDS/FIELD"))
                        {
                            if (field.Attributes["name"] == null ||
                                field.Attributes["value"] == null)
                            {
                                continue;
                            }

                            row.Fields.Add(new FieldValue(
                                field.Attributes["name"].Value,
                                field.Attributes["value"].Value));
                        }
                        rows.Add(row);
                    }
                }
                return rows;
            }
            catch
            {
                return null;
            }
        }

        private List<IRow> Gml2Rows(string gml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(gml);
                if (doc.ChildNodes.Count == 0)
                {
                    return null;
                }

                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("gml", "htpp://www.opengis.net/gml");

                XmlNode parent = doc.ChildNodes[1];
                ns.AddNamespace("myns", parent.NamespaceURI);

                List<IRow> rows = new List<IRow>();

                foreach (XmlNode layerNode in parent.ChildNodes)
                {
                    foreach (XmlNode featureNode in layerNode.ChildNodes)
                    {
                        Row row = new Row();
                        foreach (XmlNode attrNode in featureNode.ChildNodes)
                        {
                            if (attrNode.NamespaceURI == "http://www.opengis.net/gml")
                            {
                                continue;
                            }
                            //if (attrNode.ChildNodes.Count > 1) continue;

                            row.Fields.Add(new FieldValue(attrNode.Name, attrNode.InnerText));
                        }
                        rows.Add(row);
                    }
                }

                return rows;
            }
            catch { return null; }
        }
        private class RowCursor : IRowCursor
        {
            List<IRow> _rows;
            int pos = 0;
            public RowCursor(List<IRow> rows)
            {
                _rows = rows;
            }

            #region IRowCursor Member

            public Task<IRow> NextRow()
            {

                if (_rows == null || pos >= _rows.Count)
                {
                    return null;
                }

                return Task.FromResult<IRow>(_rows[pos++]);
            }

            #endregion

            #region IDisposable Member

            public void Dispose()
            {

            }

            #endregion
        }
    }
}
