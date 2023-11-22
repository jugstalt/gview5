using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Framework.XML
{
    internal class AXLLayer : Layer
    {
        protected string m_id = "";

        public virtual string id
        {
            get
            {
                if (m_id == "")
                {
                    return Title;
                }

                return m_id;
            }
            set
            {
                m_id = value;
            }
        }
    }

    internal class AXLFeatureLayer : AXLLayer, IFeatureLayer, IFeatureSelection
    {
        private IFeatureRenderer m_renderer = null, m_selectionrenderer = null;
        private ILabelRenderer m_labelrenderer = null;
        private IFeatureClass m_featureclass = null;
        private IQueryFilter m_filterQuery = null;
        private FieldCollection _fields = new FieldCollection();

        private IDSelectionSet m_selectionset;
        private IDataset m_parent;

        public AXLFeatureLayer(IDataset parent)
        {
            m_parent = parent;
        }

        public override string id
        {
            set
            {
                m_id = value;
                m_featureclass = new AXLFeatureClass(m_parent, m_id);
            }
        }

        #region IFeatureSelection

        public event FeatureSelectionChangedEvent FeatureSelectionChanged;
        public event BeforeClearSelectionEvent BeforeClearSelection; // { add { throw new NotSupportedException(); } remove { } }

        public ISelectionSet SelectionSet
        {
            get
            {
                return m_selectionset;
            }
            set
            {
                if (m_selectionset != null)
                {
                    m_selectionset.Dispose();
                }

                if (value is IDSelectionSet)
                {
                    m_selectionset = (IDSelectionSet)value;
                }
                else
                {
                    m_selectionset = null;
                }
            }
        }

        async public Task<bool> Select(IQueryFilter filter, CombinationMethod methode)
        {
            if (this.FeatureClass == null)
            {
                return false;
            }

            ISelectionSet selSet = await this.FeatureClass.Select(filter);

            SelectionSet = selSet;
            FireSelectionChangedEvent();

            return true;
        }
        public void ClearSelection()
        {
            if (m_selectionset != null)
            {
                BeforeClearSelection?.Invoke(this);

                m_selectionset.Dispose();
                m_selectionset = null;
                FireSelectionChangedEvent();
            }
        }
        public void FireSelectionChangedEvent()
        {
            FeatureSelectionChanged?.Invoke(this);
        }
        #endregion

        private AXLRenderer m_axlrenderer;

        public void extractRendererFromLayerinfo(XmlNode layerinfo)
        {
            if (layerinfo.SelectNodes("SIMPLERENDERER").Count > 0)
            {
                m_axlrenderer = new AXLRenderer(layerinfo.SelectNodes("SIMPLERENDERER")[0].OuterXml);
            }
            else if (layerinfo.SelectNodes("GROUPRENDERER").Count > 0)
            {
                m_axlrenderer = new AXLRenderer(layerinfo.SelectNodes("GROUPRENDERER")[0].OuterXml);
            }
            else if (layerinfo.SelectNodes("VALUEMAPRENDERER").Count > 0)
            {
                m_axlrenderer = new AXLRenderer(layerinfo.SelectNodes("VALUEMAPRENDERER")[0].OuterXml);
            }
            else if (layerinfo.SelectNodes("VALUEMAPLABELRENDERER").Count > 0)
            {
                m_axlrenderer = new AXLRenderer(layerinfo.SelectNodes("VALUEMAPLABELRENDERER")[0].OuterXml);
            }
            else if (layerinfo.SelectNodes("SCALEDEPENDENTRENDERER").Count > 0)
            {
                m_axlrenderer = new AXLRenderer(layerinfo.SelectNodes("SCALEDEPENDENTRENDERER")[0].OuterXml);
            }
            else
            {
                m_axlrenderer = null;
            }
        }

        internal AXLRenderer Renderer
        {
            get
            {
                return m_axlrenderer;
            }
        }


        #region IFeatureLayer Member
        public IFeatureRenderer FeatureRenderer
        {
            get { return m_renderer; }
            set { m_renderer = value; }
        }
        public IFeatureRenderer SelectionRenderer
        {
            get { return m_selectionrenderer; }
            set { m_selectionrenderer = value; }
        }
        public ILabelRenderer LabelRenderer
        {
            get { return m_labelrenderer; }
            set { m_labelrenderer = value; }
        }
        public IQueryFilter FilterQuery
        {
            get { return m_filterQuery; }
            set { m_filterQuery = value; }
        }
        public IFeatureClass FeatureClass
        {
            get { return m_featureclass; }
        }
        public IFieldCollection Fields
        {
            get { return _fields; }
        }

        public bool ApplyRefScale
        {
            get
            {
                return true;
            }
            set
            {
            }
        }
        public bool ApplyLabelRefScale
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public float MaxRefScaleFactor { get; set; }
        public float MaxLabelRefScaleFactor { get; set; }

        public FeatureLayerJoins Joins
        {
            get { return null; }
            set { }
        }

        public GeometryType LayerGeometryType
        {
            get
            {
                return this.FeatureClass != null ? this.FeatureClass.GeometryType : GeometryType.Unknown;
            }
            set { }
        }

        #endregion
    }

    internal class AXLRasterLayer : AXLLayer, IRasterLayer
    {
        public IPolygon Polygon { get { return null; } }

        public void BeginPaint() { }
        public void EndPaint() { }
        public ArgbColor GetPixel(double X, double Y) { return ArgbColor.Transparent; }
        public IBitmap Bitmap { get { return null; } }

        public double oX { get { return 0; } }
        public double oY { get { return 0; } }
        public double dx1 { get { return 0; } }
        public double dx2 { get { return 0; } }
        public double dy1 { get { return 0; } }
        public double dy2 { get { return 0; } }

        public ISpatialReference SpatialReference
        {
            get { return null; }
            set { }
        }


        public IRasterDataset Dataset
        {
            get { return null; }
        }

        #region IRasterLayer Member

        public InterpolationMethod InterpolationMethod
        {
            get { return InterpolationMethod.Fast; }
            set { }
        }

        public float Opacity
        {
            get
            {
                return 0.0f;
            }
            set
            {

            }
        }

        public ArgbColor TransparentColor
        {
            get
            {
                return ArgbColor.Transparent;
            }
            set
            {

            }
        }

        public GraphicsEngine.Filters.FilterImplementations FilterImplementation
        {
            get { return GraphicsEngine.Filters.FilterImplementations.Default; }
            set { }
        }

        public IRasterClass RasterClass { get { return null; } }

        #endregion
    }


    internal class AXLWriter
    {
        protected StringBuilder m_axl;
        //protected StringWriter m_sw;
        protected MemoryStream m_ms;
        protected XmlTextWriter m_xWriter;
        protected Encoding m_encoding = Encoding.UTF8;

        public AXLWriter()
            : this(Encoding.UTF8)
        {
        }

        public AXLWriter(Encoding encoding)
        {
            m_axl = new StringBuilder();
            //m_sw = new StringWriter(m_axl);
            m_ms = new MemoryStream();
            m_xWriter = new XmlTextWriter(m_ms, m_encoding = encoding);
        }

        public string Request
        {
            get
            {
                //m_xWriter.WriteEndDocument();
                //m_xWriter.Close();
                //m_sw.Close();
                //return m_axl.ToString();

                m_xWriter.WriteEndDocument();
                m_xWriter.Flush();
                m_ms.Flush();

                m_ms.Position = 0;
                StreamReader sr = new StreamReader(m_ms);
                m_axl.Append(sr.ReadToEnd());
                sr.Close();
                m_ms.Close();
                m_xWriter.Close();

                return m_axl.ToString();
            }
        }

        public XmlTextWriter xmlWriter
        {
            get
            {
                return m_xWriter;
            }
        }


        public void WriteStartRequest(string request)
        {
            m_xWriter.WriteStartDocument();
            m_xWriter.WriteStartElement("ARCXML");
            m_xWriter.WriteAttributeString("version", "1.1");
            m_xWriter.WriteStartElement("REQUEST");
            m_xWriter.WriteStartElement(request);  // zB. GET_IMAGE
        }
        public void WriteStartElement(string element)
        {
            m_xWriter.WriteStartElement(element);
        }
        public void WriteEndElement()
        {
            m_xWriter.WriteEndElement();
        }
        public void WriteRaw(string raw)
        {
            m_xWriter.WriteRaw(raw);
        }
        public void WriteAttribute(string tag, string val)
        {
            m_xWriter.WriteAttributeString(tag, val);
        }
        public void WriteEnvelope(double minx, double miny, double maxx, double maxy)
        {
            m_xWriter.WriteStartElement("ENVELOPE");
            m_xWriter.WriteAttributeString("maxx", maxx.ToString());
            m_xWriter.WriteAttributeString("minx", minx.ToString());
            m_xWriter.WriteAttributeString("maxy", maxy.ToString());
            m_xWriter.WriteAttributeString("miny", miny.ToString());
            m_xWriter.WriteEndElement();
        }
        public void WriteImageSize(int width, int height, double dpi)
        {
            m_xWriter.WriteStartElement("IMAGESIZE");
            m_xWriter.WriteAttributeString("width", width.ToString());
            m_xWriter.WriteAttributeString("height", height.ToString());
            m_xWriter.WriteAttributeString("dpi", dpi.ToString());
            m_xWriter.WriteEndElement();
        }
        public void WriteBackground(string col, bool trans)
        {
            m_xWriter.WriteStartElement("BACKGROUND");
            m_xWriter.WriteAttributeString("color", col);
            if (trans)
            {
                m_xWriter.WriteAttributeString("transcolor", col);
            }

            m_xWriter.WriteEndElement();
        }
        public void WriteLayerdefInvisible(AXLLayer layer)
        {
            m_xWriter.WriteStartElement("LAYERDEF");
            m_xWriter.WriteAttributeString("id", layer.id);
            m_xWriter.WriteAttributeString("visible", false.ToString());
            m_xWriter.WriteEndElement(); // LAYERDEF
        }
        public void WriteLayerdef(AXLLayer layer, double mapscale, double refscale, double dpi)
        {
            m_xWriter.WriteStartElement("LAYERDEF");
            bool vis = layer.Visible;
            if (vis)
            {
                if (layer.MinimumScale > 0.0 && layer.MinimumScale > mapscale)
                {
                    vis = false;
                }

                if (layer.MaximumScale > 0.0 && layer.MaximumScale < mapscale)
                {
                    vis = false;
                }
            }

            if (layer is AXLFeatureLayer)
            {
                if (((AXLFeatureLayer)layer).FeatureRenderer != null)
                {
                    vis = false;
                }
            }
            m_xWriter.WriteAttributeString("id", layer.id);
            m_xWriter.WriteAttributeString("visible", vis.ToString());
            if (layer is AXLFeatureLayer)
            {
                if (((AXLFeatureLayer)layer).Renderer != null)
                {
                    m_xWriter.WriteRaw(Globals.Umlaute2Esri(((AXLFeatureLayer)layer).Renderer.modifyRenderer(mapscale, refscale, dpi)));
                }
            }
            m_xWriter.WriteEndElement(); // LAYERDEF
        }
        public void AXLaddGeometryFromFeature(XmlNode feature)
        {
            foreach (XmlNode node in feature.ChildNodes)
            {
                if (node.Name == "POLYGON")
                {
                    //m_xWriter.WriteStartElement("SPATIALQUERY");
                    //m_xWriter.WriteAttributeString("subfields","#ALL#");
                    //m_xWriter.WriteStartElement("SPATIALFILTER");
                    //m_xWriter.WriteAttributeString("relation","area_intersection");
                    //m_xWriter.WriteStartElement("BUFFER");
                    //m_xWriter.WriteAttributeString("distance","30");
                    //m_xWriter.WriteAttributeString("units","meters");
                    //m_xWriter.WriteEndElement(); // BUFFER
                    m_xWriter.WriteRaw(node.OuterXml);
                    //m_xWriter.WriteEndElement();  // SPATIALFILTER
                    //m_xWriter.WriteEndElement();  // SPATIALQUERY
                    return;
                }
            }
        }

        /*
        public void AXLaddGeometryOfSelectedFeatures(int maxQueryResults,int beginrecord,string layerID)
        {
            // REQUEST erzeugen
            StringBuilder axl=new StringBuilder();
            StringWriter sw=new StringWriter(axl);
            XmlTextWriter xmlWriter=new XmlTextWriter(sw);
			
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("ARCXML");
            xmlWriter.WriteAttributeString("version","1.1");
            xmlWriter.WriteStartElement("REQUEST");
            xmlWriter.WriteStartElement("GET_FEATURES");
            xmlWriter.WriteAttributeString("outputmode","newxml");
            xmlWriter.WriteAttributeString("geometry","true");
            xmlWriter.WriteAttributeString("envelope","true");
            xmlWriter.WriteAttributeString("featurelimit",maxQueryResults.ToString());
            xmlWriter.WriteAttributeString("beginrecord",beginrecord.ToString());
			
            xmlWriter.WriteStartElement("LAYER");
            xmlWriter.WriteAttributeString("id",layerID);
            xmlWriter.WriteEndElement();

            AXLaddQuery();

            xmlWriter.WriteEndDocument();

            string resp=m_connector.SendRequest(axl,m_server,m_serviceName,"Query");

            // REQUEST auswerten
            XmlTextReader xml=null;
            XmlDocument xmldoc=null;
            XmlNodeList node=null;

            xml=new XmlTextReader(resp,XmlNodeType.Element,null);
            xmldoc=new XmlDocument();
            xmldoc.Load(xml);
            node=xmldoc.GetElementsByTagName("FEATURECOUNT");

            if(node.Count==0) return;
            XmlNodeList features=xmldoc.SelectNodes("//FEATURES/FEATURE");

            foreach(XmlNode feature in features)
            {
                AXLaddGeometryFromFeature(ref m_xWriter,feature);	
            }
        }
        */
        public void AXLaddBuffer(FeatureQueryResponse fqr, bool target)
        {
            m_xWriter.WriteStartElement("BUFFER");
            m_xWriter.WriteAttributeString("distance", fqr.bufferDist.ToString());
            m_xWriter.WriteAttributeString("bufferunits", "meters");
            if (target)
            {
                m_xWriter.WriteStartElement("TARGETLAYER");
                m_xWriter.WriteAttributeString("id", fqr.bufferID.ToString());
                m_xWriter.WriteEndElement(); // TARGETLAYER
                //m_xWriter.WriteStartElement("SPATIALQUERY");
                //m_xWriter.WriteAttributeString("subfields","#ALL#");
                //m_xWriter.WriteEndElement();
            }
            m_xWriter.WriteEndElement(); // BUFFER
        }


        // gibt zurück ob Karte Selektion hat...
        public bool AXLaddSelection(FeatureQueryResponse fqr, string selectionColor)
        {
            if (!(fqr.layer is AXLFeatureLayer))
            {
                return false;
            }

            if (fqr.selectionMethode == featureQueryMethode.None ||
                fqr.selectionMethode == featureQueryMethode.ID)
            {
                return false;
            }

            if (fqr.selectionMethode == featureQueryMethode.Geometry && !fqr.selGeometry.isValid)
            {
                return false;
            }

            m_xWriter.WriteStartElement("LAYER");
            m_xWriter.WriteAttributeString("type", "featureclass");
            m_xWriter.WriteAttributeString("name", "FeatureSelektion");
            m_xWriter.WriteAttributeString("visible", "true");
            m_xWriter.WriteAttributeString("id", "theSelection");

            m_xWriter.WriteStartElement("DATASET");
            m_xWriter.WriteAttributeString("fromlayer", fqr.layer.id.ToString());
            m_xWriter.WriteEndElement();	// DATASET

            if (fqr.selectionMethode == featureQueryMethode.Geometry)
            {
                m_xWriter.WriteStartElement("SPATIALQUERY");
                if (fqr.appendWhereFilter != "")
                {
                    m_xWriter.WriteAttributeString("where", fqr.appendWhereFilter);
                }

                m_xWriter.WriteStartElement("SPATIALFILTER");
                m_xWriter.WriteAttributeString("relation", "area_intersection");
                fqr.selGeometry.createAXL(ref m_xWriter);

                m_xWriter.WriteEndElement(); // SPATIALFILTER
                m_xWriter.WriteEndElement(); // SPATIALQUERY
            }
            else if (fqr.selectionMethode == featureQueryMethode.Query)
            {
                m_xWriter.WriteStartElement("QUERY");
                m_xWriter.WriteAttributeString("where", fqr.selQuery);
                m_xWriter.WriteEndElement();  // SPATIALQUERY,QUERY
            }

            AXLaddHighlightSymbol(((AXLFeatureLayer)fqr.layer).FeatureClass.GeometryType, selectionColor, 0.6);

            m_xWriter.WriteEndElement(); // LAYER

            return true;
        }
        public void AXLaddQuery(FeatureQueryResponse fqr)
        {
            AXLaddQuery(fqr, false, false);
        }
        public void AXLaddQuery(FeatureQueryResponse fqr, bool onlyHighlight)
        {
            AXLaddQuery(fqr, false, false, onlyHighlight);
        }
        public void AXLaddQuery(FeatureQueryResponse fqr, bool insertBuffer, bool insertBufferTarget)
        {
            AXLaddQuery(fqr, insertBuffer, insertBufferTarget, false);
        }
        public void AXLaddQuery(FeatureQueryResponse fqr, bool insertBuffer, bool insertBufferTarget, bool onlyHighlight)
        {
            bool selected = false;
            if (!onlyHighlight)
            {
                selected = fqr.selectFeatures;
            }

            if ((selected && insertBuffer == false && fqr.selectionMethode == featureQueryMethode.Geometry) ||
                (!selected && insertBuffer == false && fqr.highlightMethode == featureQueryMethode.Geometry) ||
                (insertBuffer == true && fqr.bufferMethode == featureQueryMethode.Geometry))
            {
                if (fqr.dispBuffer == true && fqr.bufferMethode == featureQueryMethode.ID)
                {
                    //
                    // Falls "Alle anzeigen" von einem Puffer-Erbebnis aufgerufen wird, soll nicht über den Envelope
                    // selektiert werden. Dabei würden die ursprünglichen, zum Buffer aufgezogenen Features selektiert werden.
                    // --> nur das wirklich gepufferte Feature selektieren... (Atlas3)
                    //
                    m_xWriter.WriteStartElement("QUERY");
                    if (fqr.appendWhereFilter != "")
                    {
                        m_xWriter.WriteAttributeString("where",/*activeLayerIDField*/fqr.layer.id + " = " + fqr.bufferID.ToString());
                    }

                    m_xWriter.WriteEndElement(); // QUERY
                }
                else
                {
                    m_xWriter.WriteStartElement("SPATIALQUERY");

                    if (insertBuffer)
                    {
                        m_xWriter.WriteAttributeString("subfields", "#ALL#");
                        AXLaddBuffer(fqr, insertBufferTarget);
                    }
                    if (fqr.appendWhereFilter != "")
                    {
                        m_xWriter.WriteAttributeString("where", fqr.appendWhereFilter);
                    }

                    m_xWriter.WriteStartElement("SPATIALFILTER");
                    m_xWriter.WriteAttributeString("relation", "area_intersection");
                    fqr.selGeometry.createAXL(ref m_xWriter);
                    /*
                    m_xWriter.WriteStartElement("ENVELOPE");
                    m_xWriter.WriteAttributeString("minx",m_selenv_minx.ToString());
                    m_xWriter.WriteAttributeString("miny",m_selenv_miny.ToString());
                    m_xWriter.WriteAttributeString("maxx",m_selenv_maxx.ToString());
                    m_xWriter.WriteAttributeString("maxy",m_selenv_maxy.ToString());
                    m_xWriter.WriteEndElement(); // ENVELOPE;
                    */
                    m_xWriter.WriteEndElement(); // SPATIALFILTER
                    m_xWriter.WriteEndElement(); // SPATIALQUERY
                }
            }
            else if (((fqr.highlightMethode == featureQueryMethode.ID && fqr.highlightID != -1) &&
                insertBuffer == false) && (fqr.layer is AXLFeatureLayer))
            {
                /*
                m_xWriter.WriteStartElement("SPATIALQUERY");
                m_xWriter.WriteAttributeString("where",activeLayerIDField+" = "+m_highlightID.ToString());
                m_xWriter.WriteAttributeString("featurelimit","1");
				
                m_xWriter.WriteStartElement("SPATIALFILTER");
                m_xWriter.WriteAttributeString("relation","area_intersection");
                m_xWriter.WriteStartElement("ENVELOPE");
                m_xWriter.WriteAttributeString("minx",m_selenv_minx.ToString());
                m_xWriter.WriteAttributeString("miny",m_selenv_miny.ToString());
                m_xWriter.WriteAttributeString("maxx",m_selenv_maxx.ToString());
                m_xWriter.WriteAttributeString("maxy",m_selenv_maxy.ToString());
                m_xWriter.WriteEndElement(); // ENVELOPE;
                m_xWriter.WriteEndElement(); // SPATIALFILTER
                */
                //m_xWriter.WriteEndElement(); // SPATIALQUERY

                m_xWriter.WriteStartElement("QUERY");
                m_xWriter.WriteAttributeString("where", ((AXLFeatureLayer)fqr.layer).FeatureClass.IDFieldName + " = " + fqr.highlightID.ToString());
                m_xWriter.WriteAttributeString("featurelimit", "1");
                m_xWriter.WriteEndElement(); // QUERY
            }
            else if ((fqr.bufferID != -1 && insertBuffer == true && fqr.bufferMethode == featureQueryMethode.ID) && (fqr.layer is AXLFeatureLayer))
            {
                m_xWriter.WriteStartElement("SPATIALQUERY");
                m_xWriter.WriteAttributeString("subfields", "#ALL#");
                m_xWriter.WriteAttributeString("where", ((AXLFeatureLayer)fqr.layer).FeatureClass.IDFieldName + " = " + fqr.bufferID.ToString());
                AXLaddBuffer(fqr, insertBufferTarget);
                m_xWriter.WriteEndElement(); // SPATIALQUERY
            }
            else if ((selected && fqr.selectionMethode == featureQueryMethode.Query) ||
                (!selected && fqr.highlightMethode == featureQueryMethode.Query))
            {
                if (insertBuffer && fqr.bufferMethode == featureQueryMethode.Query)
                {
                    m_xWriter.WriteStartElement("SPATIALQUERY");
                    m_xWriter.WriteAttributeString("subfields", "#ALL#");
                    m_xWriter.WriteAttributeString("where", fqr.selQuery);
                    AXLaddBuffer(fqr, insertBufferTarget);
                }
                else
                {
                    m_xWriter.WriteStartElement("QUERY");
                    m_xWriter.WriteAttributeString("where", fqr.selQuery);
                }

                m_xWriter.WriteEndElement();  // SPATIALQUERY,QUERY
            }
        }
        public void AXLaddHighlightSymbol(GeometryType type, string color, double trans)
        {
            if (type == GeometryType.Unknown)
            {
                return;
            }

            m_xWriter.WriteStartElement("SIMPLERENDERER");
            switch (type)
            {
                case GeometryType.Polygon:
                    m_xWriter.WriteStartElement("SIMPLEPOLYGONSYMBOL");
                    m_xWriter.WriteAttributeString("transparency", trans.ToString());
                    m_xWriter.WriteAttributeString("fillcolor", color);
                    m_xWriter.WriteEndElement(); // SIMPLEPOLYGONSYMBOL
                    break;
                case GeometryType.Polyline:
                    m_xWriter.WriteStartElement("SIMPLELINESYMBOL");
                    m_xWriter.WriteAttributeString("transparency", trans.ToString());
                    m_xWriter.WriteAttributeString("color", color);
                    m_xWriter.WriteAttributeString("width", "10");
                    m_xWriter.WriteEndElement(); // SIMPLELINESYMBOL
                    break;
                default: // point
                    m_xWriter.WriteStartElement("SIMPLEMARKERSYMBOL");
                    m_xWriter.WriteAttributeString("transparency", trans.ToString());
                    m_xWriter.WriteAttributeString("color", color);
                    m_xWriter.WriteAttributeString("width", "20");
                    m_xWriter.WriteEndElement(); // SIMPLEMARKERSYMBOL
                    break;
            }
            m_xWriter.WriteEndElement();
        }
        public void AXLaddFeatureCoordsys(string attr)
        {
            if (attr == null || attr == "")
            {
                return;
            }

            m_xWriter.WriteRaw("<FEATURECOORDSYS " + attr + " />");
        }
        public void AXLaddFilterCoordsys(string attr)
        {
            if (attr == null || attr == "")
            {
                return;
            }

            m_xWriter.WriteRaw("<FILTERCOORDSYS " + attr + " />");
        }

        public void AXLaddFeatureCoordsys(IMSSpatialReference sRef)
        {
            if (sRef == null)
            {
                return;
            }

            AXLaddFeatureCoordsys(CoordSysParams(sRef));
        }
        public void AXLaddFilterCoordsys(IMSSpatialReference sRef)
        {
            if (sRef == null)
            {
                return;
            }

            AXLaddFilterCoordsys(CoordSysParams(sRef));
        }

        private string CoordSysParams(IMSSpatialReference sRef)
        {
            string attr = "";

            if (sRef.ID != "")
            {
                attr += "id=\"" + sRef.ID + "\" ";
            }
            else if (sRef.String != "")
            {
                attr += "string=\"" + sRef.String.Replace("\"", "&quot;") + "\" ";
            }

            if (sRef.datumID != "")
            {
                attr += "datumtransformid=\"" + sRef.datumID + "\" ";
            }
            else if (sRef.datumString != "")
            {
                attr += "datumtransformstring=\"" + sRef.datumString.Replace("\"", "&quot;") + "\" ";
            }

            return attr;
        }
    }
    internal class axlShapes
    {
        public axlShapes() { }

        static public void openObject(ref XmlTextWriter xWriter)
        {
            xWriter.WriteStartElement("OBJECT");
            xWriter.WriteAttributeString("units", "database");
        }
        static public void closeObject(ref XmlTextWriter xWriter)
        {
            xWriter.WriteEndElement();
        }
        static public void LineSymbol(ref XmlTextWriter xWriter, double width, string col)
        {
            xWriter.WriteStartElement("SIMPLELINESYMBOL");
            xWriter.WriteAttributeString("antialiasing", "true");
            xWriter.WriteAttributeString("width", width.ToString());
            xWriter.WriteAttributeString("color", col);
            xWriter.WriteEndElement();  // SIMPLELINESYMBOL
        }
        static public void PolygonSymbol(ref XmlTextWriter xWriter, string col, string bordercol, double borderwidth, double trans)
        {
            xWriter.WriteStartElement("SIMPLEPOLYGONSYMBOL");
            xWriter.WriteAttributeString("fillcolor", col);
            xWriter.WriteAttributeString("boundarycolor", bordercol);
            xWriter.WriteAttributeString("boundarywidth", borderwidth.ToString());
            xWriter.WriteAttributeString("transparency", trans.ToString());
            xWriter.WriteEndElement();  // SIMPLEPOLYGONSYMBOL
        }
        static public void PointSymbol(ref XmlTextWriter xWriter, double width, double trans, string col)
        {
            xWriter.WriteStartElement("SIMPLEMARKERSYMBOL");
            xWriter.WriteAttributeString("transparency", trans.ToString());
            xWriter.WriteAttributeString("color", col);
            xWriter.WriteAttributeString("width", width.ToString());
            xWriter.WriteEndElement(); // SIMPLEMARKERSYMBOL
        }
        static public void Point(ref XmlTextWriter xWriter, xyPoint p)
        {
            xWriter.WriteStartElement("POINT");
            xWriter.WriteAttributeString("x", p.x.ToString());
            xWriter.WriteAttributeString("y", p.y.ToString());
            xWriter.WriteEndElement();  // POINT
        }
        static public void Line(ref XmlTextWriter xWriter, xyPoint p1, xyPoint p2)
        {
            xWriter.WriteStartElement("POLYLINE");
            xWriter.WriteStartElement("PATH");

            Point(ref xWriter, p1);
            Point(ref xWriter, p2);

            xWriter.WriteEndElement();  // PATH
            xWriter.WriteEndElement();  // POLYLINE
        }
        static public void Line(ref XmlTextWriter xWriter, xyPoint p1, xyPoint p2, xyPoint p3)
        {
            xWriter.WriteStartElement("POLYLINE");
            xWriter.WriteStartElement("PATH");

            Point(ref xWriter, p1);
            Point(ref xWriter, p2);
            Point(ref xWriter, p3);

            xWriter.WriteEndElement();  // PATH
            xWriter.WriteEndElement();  // POLYLINE
        }
        static public void Text(ref XmlTextWriter xWriter, xyPoint p, string text, double size, double angle, string col, double offset)
        {
            if (angle < 0)
            {
                angle += 360.0;
            }

            double o = (angle) / 180.0 * Math.PI;
            p.x -= Math.Sin(o) * offset;
            p.y += Math.Cos(o) * offset;
            xWriter.WriteStartElement("TEXT");
            xWriter.WriteAttributeString("coords", p.x.ToString() + " " + p.y.ToString());
            xWriter.WriteAttributeString("label", text);
            xWriter.WriteStartElement("TEXTMARKERSYMBOL");

            xWriter.WriteAttributeString("angle", angle.ToString());  // nicht beim ArcMAP Service
            xWriter.WriteAttributeString("antialiasing", "true");
            xWriter.WriteAttributeString("fontsize", size.ToString());
            xWriter.WriteAttributeString("font", "Arial");
            xWriter.WriteAttributeString("fontcolor", col);
            xWriter.WriteAttributeString(/*"blockout"*/"outline", "255,255,255");
            xWriter.WriteEndElement();  // TEXTMARKERSYMBOL
            xWriter.WriteEndElement();  // TEXT
        }
        static public void createMeasureAXL(ref XmlTextWriter xWriter, xyPoint p1, xyPoint p2, double scale)
        {
            double sf = scale / 1000.0;

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 3.0, "255,255,255");
            Line(ref xWriter, p1, p2);
            closeObject(ref xWriter);

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 1.0, "0,0,0");
            Line(ref xWriter, p1, p2);
            closeObject(ref xWriter);

            double al = Math.Atan2(p2.y - p1.y, p2.x - p1.x);
            if (al < 0.0)
            {
                al += 2.0 * Math.PI;
            }

            xyPoint pp1 = new xyPoint(p1.x + Math.Cos(al + 0.5) * 2 * sf, p1.y + Math.Sin(al + 0.5) * 2 * sf);
            xyPoint pp2 = new xyPoint(p1.x + Math.Cos(al - 0.5) * 2 * sf, p1.y + Math.Sin(al - 0.5) * 2 * sf);

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 3.0, "255,255,255");
            Line(ref xWriter, pp1, p1, pp2);
            closeObject(ref xWriter);

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 1.0, "0,0,0");
            Line(ref xWriter, pp1, p1, pp2);
            closeObject(ref xWriter);

            pp1.x = p2.x + Math.Cos(al + 0.5 + Math.PI) * 2 * sf;
            pp1.y = p2.y + Math.Sin(al + 0.5 + Math.PI) * 2 * sf;
            pp2.x = p2.x + Math.Cos(al - 0.5 + Math.PI) * 2 * sf;
            pp2.y = p2.y + Math.Sin(al - 0.5 + Math.PI) * 2 * sf;

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 3.0, "255,255,255");
            Line(ref xWriter, pp1, p2, pp2);
            closeObject(ref xWriter);

            openObject(ref xWriter);
            LineSymbol(ref xWriter, 1.0, "0,0,0");
            Line(ref xWriter, pp1, p2, pp2);
            closeObject(ref xWriter);

            double len = Math.Round(Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y)), 2), sign = 1.0;
            string text = String.Format("{0:0.00}", len)/*+" m"*/;
            double al_text = al;

            if (al > 0.5 * Math.PI && al < 1.5 * Math.PI)
            {
                al_text -= Math.PI;
                sign = -1.0;
            }

            pp1.x = p1.x + Math.Cos(al) * (len / 2.0 - (text.Length / 2) * 2 * sf * sign);
            pp1.y = p1.y + Math.Sin(al) * (len / 2.0 - (text.Length / 2) * 2 * sf * sign);

            openObject(ref xWriter);
            Text(ref xWriter, pp1, text, 12.0, al_text * 180.0 / Math.PI, "0,0,0", 1.0 * sf);
            closeObject(ref xWriter);
        }
    }
}
