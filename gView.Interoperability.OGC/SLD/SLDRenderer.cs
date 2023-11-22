using gView.Framework.Carto;
using gView.Framework.Carto.Rendering;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.Xml;
using gView.Framework.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.OGC.SLD
{
    [gView.Framework.system.RegisterPlugIn("60A6D860-F90F-454a-B1F3-EA26E945CD78")]
    public class SLDRenderer : Cloner, IFeatureRenderer, IPropertyPage, ILegendGroup, IToArcXml
    {
        private bool _useRefScale = true;
        private List<Rule> _rules = new List<Rule>();
        private GmlVersion _gmlVersion = GmlVersion.v1;

        public List<Rule> Rules
        {
            get { return _rules; }
        }

        public SLDRenderer()
        {
        }
        public SLDRenderer(string sldString)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sldString);

                XmlNamespaceManager ns = null;
                if (doc.ChildNodes[0].NamespaceURI != String.Empty)
                {
                    ns = new XmlNamespaceManager(doc.NameTable);
                    ns.AddNamespace("SLD", "http://www.opengis.org/sld");
                    ns.AddNamespace("OGC", "http://www.opengis.org/ogc");
                    ns.AddNamespace("GML", "http://www.opengis.org/gml");
                }

                FromXmlNodes((ns != null) ? doc.SelectNodes("//SLD:Rule", ns) : doc.SelectNodes("//Rule"), ns);
            }
            catch { }
        }
        async static public Task<SLDRenderer> CreateAync(IFeatureLayer featureLayer)
        {
            var renderer = new SLDRenderer();

            if (featureLayer == null)
            {
                return renderer;
            }

            if (featureLayer.FeatureRenderer is SimpleRenderer)
            {
                SimpleRenderer sRenderer = (SimpleRenderer)featureLayer.FeatureRenderer;

                gView.Framework.OGC.WFS.Filter filter = null;
                Rule.FilterType filterType = Rule.FilterType.None;
                if (featureLayer.FilterQuery != null)
                {
                    filter = await gView.Framework.OGC.WFS.Filter.CreataAsync(featureLayer.FeatureClass, featureLayer.FilterQuery, renderer._gmlVersion);
                    filterType = (filter != null) ? Rule.FilterType.OgcFilter : Rule.FilterType.None;
                }
                Rule rule = new Rule(filter, sRenderer.Symbol);
                rule.filterType = filterType;

                renderer._rules.Add(rule);
            }
            else if (featureLayer.FeatureRenderer is ValueMapRenderer)
            {
            }
            else if (featureLayer.FeatureRenderer is ScaleDependentRenderer)
            {
            }
            else if (featureLayer.FeatureRenderer is ScaleDependentLabelRenderer)
            {
            }

            return renderer;
        }
        private void FromXmlNodes(XmlNodeList rules, XmlNamespaceManager ns)
        {
            if (rules == null || rules.Count == 0)
            {
                return;
            }

            foreach (XmlNode ruleNode in rules)
            {
                Rule rule = new Rule(ruleNode, ns);
                if (rule.Symbol != null)
                {
                    _rules.Add(rule);
                }
            }
        }

        public string ToXmlString(string layerName)
        {
            return ToXmlString(layerName, false);
        }
        public string ToXmlString(string layerName, bool useNamespace)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

            if (useNamespace)
            {
                sw.WriteLine("<NamedLayer xmlns=\"http://www.opengis.net/sld\" >");
            }
            else
            {
                sw.WriteLine("<NamedLayer>");
            }

            sw.WriteLine("<Name>" + layerName + "</Name>");
            sw.WriteLine("<UserStyle>");
            sw.WriteLine("<Title>Style " + layerName + "</Title>");
            sw.WriteLine("<FeatureTypeStyle>");

            foreach (Rule rule in _rules)
            {
                rule.XmlString(sw, useNamespace);
            }

            sw.WriteLine("</FeatureTypeStyle>");
            sw.WriteLine("</UserStyle>");
            sw.WriteLine("</NamedLayer>");
            sw.Flush();

            ms.Position = 0;
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, (int)ms.Length);
            sw.Close();

            string ret = Encoding.UTF8.GetString(bytes).Trim();
            return ret;
        }

        public void SetDefaultSrsName(string srsName)
        {
            foreach (Rule rule in _rules)
            {
                if (rule != null && rule.Filter != null)
                {
                    rule.Filter.SetDefaultSrsName(srsName);
                }
            }
        }

        #region IFeatureRenderer Member

        public void Draw(IDisplay disp, gView.Framework.Data.IFeature feature)
        {
            bool rendered = false;
            foreach (Rule rule in _rules)
            {
                if (rule.Symbol == null ||
                    !rule.RenderFeature(disp, feature, _featureSRef))
                {
                    continue;
                }

                if (rule.filterType == Rule.FilterType.ElseFilter &&
                    rendered)
                {
                    continue;
                }

                disp.Draw(rule.Symbol, feature.Shape);
                rendered = true;

                // Erste Regel zieht  ???
                // break;
            }
        }

        public void StartDrawing(IDisplay display) { }

        public void FinishDrawing(IDisplay disp, ICancelTracker cancelTracker)
        {
        }

        private ISpatialReference _featureSRef = null;
        public void PrepareQueryFilter(IFeatureLayer layer, IQueryFilter filter)
        {
            List<string> propertyNames = new List<string>();
            foreach (Rule rule in _rules)
            {
                if (rule == null || rule.Filter == null)
                {
                    continue;
                }

                foreach (string pname in rule.Filter.PropertyNames)
                {
                    if (propertyNames.Contains(pname))
                    {
                        continue;
                    }

                    propertyNames.Add(pname);
                }
            }

            foreach (string propertyName in propertyNames)
            {
                filter.AddField(propertyName);
            }

            if (layer != null && layer.FeatureClass != null)
            {
                _featureSRef = layer.FeatureClass.SpatialReference;
            }
            else
            {
                _featureSRef = null;
            }
        }

        public bool CanRender(gView.Framework.Data.IFeatureLayer layer, IMap map)
        {
            //if (layer != null &&
            //    (layer.Class is WMSThemeClass ||
            //    layer.Class is WFSFeatureClass)) return true;
            if (layer == null && layer.FeatureClass == null &&
                layer.FeatureClass.GeometryType == GeometryType.Network)
            {
                return false;
            }

            return true;
        }

        public bool HasEffect(gView.Framework.Data.IFeatureLayer layer, IMap map)
        {
            return true;
        }

        public bool UseReferenceScale
        {
            get
            {
                return _useRefScale;
            }
            set
            {
                _useRefScale = value;
            }
        }

        public string Name
        {
            get { return "SLD Renderer"; }
        }

        public string Category
        {
            get { return "OGC"; }
        }

        public bool RequireClone()
        {
            return _rules?.Where(r => r?.Symbol != null && r.Symbol.RequireClone())
                          .FirstOrDefault() != null;
        }

        #endregion

        #region IPersistable Member

        public void Load(gView.Framework.IO.IPersistStream stream)
        {
            _useRefScale = (bool)stream.Load("UseRefScale", true);

            _rules.Clear();
            Rule rule;
            while ((rule = (Rule)stream.Load("Rule", null, new Rule())) != null)
            {
                _rules.Add(rule);
            }
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            stream.Save("UseRefScale", _useRefScale);

            foreach (Rule rule in _rules)
            {
                stream.Save("Rule", rule);
            }
        }

        #endregion

        #region IClone2 Member

        public object Clone(CloneOptions options)
        {
            SLDRenderer clone = new SLDRenderer();

            clone._useRefScale = _useRefScale;
            clone._featureSRef = _featureSRef;
            foreach (Rule rule in _rules)
            {
                if (rule == null)
                {
                    continue;
                }

                clone._rules.Add(rule.Clone(options) as Rule);
            }
            return clone;
        }

        public void Release()
        {
            foreach (Rule rule in _rules)
            {
                if (rule == null)
                {
                    continue;
                }

                rule.Release();
            }
            _rules.Clear();
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            if (!(initObject is IFeatureLayer))
            {
                return null;
            }

            try
            {
                string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Interoperability.OGC.UI.dll");

                gView.Framework.Carto.Rendering.UI.IPropertyPanel p = uiAssembly.CreateInstance("gView.Interoperability.OGC.UI.SLD.PropertyForm_SLDRenderer") as gView.Framework.Carto.Rendering.UI.IPropertyPanel;
                if (p != null)
                {
                    return p.PropertyPanel(this, (IFeatureLayer)initObject);
                }
            }
            catch (Exception /*ex*/)
            {

            }
            return null;
        }

        public object PropertyPageObject()
        {
            return this;
        }

        #endregion

        #region Classes
        public class Rule : IPersistable, IClone2, IToArcXml
        {
            private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

            public enum FilterType { None = 0, ElseFilter = 2, OgcFilter = 1 }
            private gView.Framework.OGC.WFS.Filter _filter;
            private ISymbol _symbol;
            private double _minScale = 0, _maxScale = 0;
            private FilterType _filterType = FilterType.None;
            private GmlVersion _gmlVersion = GmlVersion.v1;

            internal Rule() { }
            public Rule(ISymbol symbol)
            {
                _filter = null;
                _symbol = symbol;
            }
            public Rule(gView.Framework.OGC.WFS.Filter filter, ISymbol symbol)
            {
                _filter = filter;
                _symbol = symbol;
            }

            public Rule(XmlNode ruleNode)
                : this(ruleNode, null)
            {
            }
            public Rule(XmlNode ruleNode, XmlNamespaceManager ns)
            {
                XmlNode filterNode = (ns != null) ? ruleNode.SelectSingleNode("OGC:Filter", ns) : ruleNode.SelectSingleNode("Filter");
                XmlNode elseFilterNode = (ns != null) ? ruleNode.SelectSingleNode("OGC:ElseFilter", ns) : ruleNode.SelectSingleNode("ElseFilter");

                StyledLayerDescriptorReader sldReader = new StyledLayerDescriptorReader(ns);
                if (filterNode != null)
                {
                    this.Filter = new gView.Framework.OGC.WFS.Filter(filterNode.OuterXml, _gmlVersion);
                    this.filterType = FilterType.OgcFilter;
                }
                else if (elseFilterNode != null)
                {
                    this.filterType = FilterType.ElseFilter;
                }
                else
                {
                    this.filterType = FilterType.None;
                }

                this.Symbol = sldReader.ReadSymbol(ruleNode);

                XmlNode minScaleDominator = (ns != null) ? ruleNode.SelectSingleNode("SLD:MinScaleDominator", ns) : ruleNode.SelectSingleNode("MinScaleDominator");
                XmlNode maxScaleDominator = (ns != null) ? ruleNode.SelectSingleNode("SLD:MaxScaleDominator", ns) : ruleNode.SelectSingleNode("MaxScaleDominator");

                double scaleFac = 1.33488; // UMN ???

                if (minScaleDominator != null)
                {
                    double.TryParse(minScaleDominator.InnerText, System.Globalization.NumberStyles.Any, _nhi, out _maxScale);
                    _maxScale /= scaleFac;
                }
                if (maxScaleDominator != null)
                {
                    double.TryParse(maxScaleDominator.InnerText, System.Globalization.NumberStyles.Any, _nhi, out _minScale);
                    _minScale /= scaleFac;
                }
            }

            public gView.Framework.OGC.WFS.Filter Filter
            {
                get { return _filter; }
                set { _filter = value; }
            }
            public ISymbol Symbol
            {
                get { return _symbol; }
                set
                {
                    _symbol = value;
                    SetLegendText();
                }
            }

            public double MinScale
            {
                get { return _minScale; }
                set
                {
                    _minScale = value;
                    SetLegendText();
                }
            }
            public double MaxScale
            {
                get { return _maxScale; }
                set
                {
                    _maxScale = value;
                    SetLegendText();
                }
            }

            public FilterType filterType
            {
                get { return _filterType; }
                set
                {
                    _filterType = value;
                    SetLegendText();
                }
            }

            public bool RenderFeature(IDisplay display, IFeature feature, ISpatialReference featureSRef)
            {
                if (this.MaxScale > 1 && this.MaxScale > display.MapScale + 0.5)
                {
                    return false;
                }

                if (this.MinScale > 1 && this.MinScale < display.MapScale - 0.5)
                {
                    return false;
                }

                if (_filter != null && _filterType == FilterType.OgcFilter)
                {
                    if (!_filter.Check(feature, featureSRef))
                    {
                        return false;
                    }
                }
                return true;
            }

            public string ToXmlString()
            {
                return ToXmlString(false);
            }
            public string ToXmlString(bool useNamespace)
            {
                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

                XmlString(sw, useNamespace);

                sw.Flush();

                ms.Position = 0;
                byte[] bytes = new byte[ms.Length];
                ms.Read(bytes, 0, (int)ms.Length);
                sw.Close();

                string ret = Encoding.UTF8.GetString(bytes).Trim();
                return ret;
            }

            internal void XmlString(StreamWriter sw, bool useNameSpace)
            {
                if (useNameSpace)
                {
                    sw.WriteLine("<Rule xmlns=\"http://www.opengis.net/sld\" >");
                }
                else
                {
                    sw.WriteLine("<Rule>");
                }

                if (_filter != null && _filterType == FilterType.OgcFilter)
                {
                    sw.WriteLine(_filter.ToXmlString(useNameSpace));
                }
                else if (_filterType == FilterType.ElseFilter)
                {
                    sw.WriteLine("<ElseFilter/>");
                }

                double scaleFac = 1.33488; // UMN ???

                if (_maxScale > 1)
                {
                    sw.WriteLine("<MinScaleDenominator>" + (_maxScale / scaleFac).ToString(_nhi) + "</MinScaleDenominator>");
                }

                if (_minScale > 1)
                {
                    sw.WriteLine("<MaxScaleDenominator>" + (_minScale / scaleFac).ToString(_nhi) + "</MaxScaleDenominator>");
                }

                if (_symbol != null)
                {
                    StyledLayerDescriptorWriter sldWriter = new StyledLayerDescriptorWriter(false);
                    sldWriter.WriteSymbol(_symbol);
                    sw.WriteLine(sldWriter.ToString());
                }

                sw.WriteLine("</Rule>");
            }

            #region IPersistable Member

            public void Load(IPersistStream stream)
            {
                _minScale = (double)stream.Load("MinScale", 0.0);
                _maxScale = (double)stream.Load("MaxScale", 0.0);

                _symbol = (ISymbol)stream.Load("Symbol");

                _filterType = (FilterType)stream.Load("FilterType", (int)FilterType.None);
                _filter = (gView.Framework.OGC.WFS.Filter)stream.Load("Filter");

                SetLegendText();
            }

            public void Save(IPersistStream stream)
            {
                if (_minScale != 0.0)
                {
                    stream.Save("MinScale", _minScale);
                }

                if (_maxScale != 0.0)
                {
                    stream.Save("MaxScale", _maxScale);
                }

                stream.Save("Symbol", _symbol);

                stream.Save("FilterType", (int)_filterType);
                if (_filter != null)
                {
                    stream.Save("Filter", _filter);
                }
            }

            #endregion

            #region IClone2 Member

            public object Clone(CloneOptions options)
            {
                Rule clone = new Rule();
                if (_symbol != null)
                {
                    clone.Symbol = _symbol.Clone(options) as ISymbol;
                }

                clone.Filter = _filter;
                clone.filterType = _filterType;
                clone.MinScale = _minScale;
                clone.MaxScale = _maxScale;

                return clone;
            }

            public void Release()
            {
                if (_symbol != null)
                {
                    _symbol.Release();
                }
            }

            #endregion

            private void SetLegendText()
            {
                if (_symbol is ILegendItem)
                {
                    ILegendItem lItem = (ILegendItem)_symbol;
                    if (lItem.LegendLabel != String.Empty &&
                        !lItem.LegendLabel.StartsWith("Rule:"))
                    {
                        return;
                    }

                    string legendText = String.Empty;

                    if (_minScale > 1 && _maxScale > 1)
                    {
                        legendText = "1:" + Math.Round(_minScale).ToString() + " - 1:" + Math.Round(_maxScale).ToString();
                    }
                    else if (_minScale > 1)
                    {
                        legendText = "< 1:" + Math.Round(_minScale).ToString();
                    }
                    else if (_maxScale > 1)
                    {
                        legendText = "> 1:" + Math.Round(_maxScale).ToString();
                    }

                    if (_filter != null && _filterType == FilterType.OgcFilter)
                    {
                        legendText += " " + _filter.WhereClause;
                    }
                    else if (_filterType == FilterType.ElseFilter)
                    {
                        legendText += " ElseFilter";
                    }
                    if (legendText.Trim() != String.Empty)
                    {
                        lItem.LegendLabel = "Rule: " + legendText.Trim();
                    }
                }
            }

            #region IToArcXml Member

            public string ArcXml
            {
                get
                {
                    string symbolAxl = String.Empty, filterAxl = String.Empty;

                    if (filterType == FilterType.OgcFilter && _filter != null)
                    {
                        string xml = _filter.ToXmlString();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xml);

                        IQueryFilter queryFilter = gView.Framework.OGC.WFS.Filter.FromWFS(doc.SelectSingleNode("Filter"), _gmlVersion);
                        filterAxl = AXLFromObjectFactory.Query(queryFilter);
                    }

                    string symbol = ObjectFromAXLFactory.ConvertToAXL(_symbol);

                    StringBuilder sb = new StringBuilder();
                    sb.Append(filterAxl);
                    sb.Append("<SIMPLERENDERER>");
                    sb.Append(symbol);
                    sb.Append("</SIMPLERENDERER>");

                    return sb.ToString();
                }
            }

            #endregion
        }
        #endregion

        #region ILegendGroup Member

        public int LegendItemCount
        {
            get { return _rules.Count; }
        }

        public ILegendItem LegendItem(int index)
        {
            if (index < 0 || index >= _rules.Count ||
                _rules[index] == null)
            {
                return null;
            }

            return _rules[index].Symbol as ILegendItem;
        }

        public void SetSymbol(ILegendItem item, ISymbol symbol)
        {
            foreach (Rule rule in _rules)
            {
                if (rule == null)
                {
                    continue;
                }

                if (rule.Symbol == item)
                {
                    rule.Symbol = symbol;
                }
            }
        }

        #endregion

        #region IToArcXml Member

        public string ArcXml
        {
            get
            {
                if (_rules.Count == 1)
                {
                    return _rules[0].ArcXml;
                }

                return String.Empty;
            }
        }

        #endregion

        #region IRenderer Member

        public List<ISymbol> Symbols
        {
            get
            {
                List<ISymbol> symbols = new List<ISymbol>();

                foreach (Rule rule in _rules)
                {
                    symbols.Add(rule.Symbol);
                }

                return symbols;
            }
        }

        public bool Combine(IRenderer renderer)
        {
            return false;
        }
        #endregion
    }
}
