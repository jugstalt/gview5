using gView.Framework.Carto;
using gView.Framework.Carto.Rendering;
using gView.Framework.Carto.UI;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Network;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.XML;
using gView.GraphicsEngine;
using gView.Interoperability.ArcXML.Dataset;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.ArcXML
{
    internal class GET_FEATURES_REQUEST
    {
        public IQueryFilter Filter = null;
        public List<ITableClass> Classes = null;
        public IServiceMap ServiceMap = null;

        async public Task<string> Request()
        {
            int beginrecord = 0;
            int featurelimit = 1000;
            bool envelope = false;
            bool geometry = false;
            if (Filter is IArcXmlGET_FEATURES_Attributes)
            {
                IArcXmlGET_FEATURES_Attributes attributes = Filter as IArcXmlGET_FEATURES_Attributes;
                beginrecord = attributes.beginrecord;
                featurelimit = attributes.featurelimit;
                envelope = attributes.envelope;
                geometry = attributes.geometry;
            }

            ICursor cursor = null;
            int counter = 0;
            bool hasmore = false;
            Envelope env = null;

            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("FEATURES");

            foreach (ITableClass Class in Classes)
            {
                IQueryFilter filter = Filter.Clone() as IQueryFilter;
                if (filter == null)
                {
                    return String.Empty;
                }

                if (Classes.Count > 1)
                {
                    if (!await MapServerHelper.ModifyFilter(ServiceMap, Class, filter))
                    {
                        continue;
                    }
                }

                if (Class != null && Class.Dataset is ArcIMSDataset && filter is IArcXmlGET_FEATURES_Attributes)
                {
                    ((IArcXmlGET_FEATURES_Attributes)filter).Properties = ((ArcIMSDataset)Class.Dataset)._properties;
                }
                if (filter is IBufferQueryFilter &&
                    ((IBufferQueryFilter)filter).RootFilter is IArcXmlGET_FEATURES_Attributes &&
                    ((IBufferQueryFilter)filter).RootFeatureClass != null &&
                    ((IBufferQueryFilter)filter).RootFeatureClass.Dataset is ArcIMSDataset)
                {
                    ((IArcXmlGET_FEATURES_Attributes)((IBufferQueryFilter)filter).RootFilter).Properties =
                        ((ArcIMSDataset)((IBufferQueryFilter)filter).RootFeatureClass.Dataset)._properties;
                }

                filter.Limit = featurelimit;
                filter.BeginRecord = Math.Max(0, beginrecord - 1);

                if (Class is IFeatureClass)
                {
                    cursor = await Class.Search(filter) as IFeatureCursor;
                }
                else if (Class is ITableClass)
                {
                    cursor = await Class.Search(filter) as IRowCursor;
                }
                if (cursor == null)
                {
                    continue;
                }

                if ((filter is ArcXMLQueryFilter || filter is ArcXMLSpatialFilter) &&
                    cursor is AXLFeatureCursor &&
                    Classes.Count == 1)
                {
                    return ((AXLFeatureCursor)cursor).ArcXML;
                }

                bool project = false;
                if (filter.FeatureSpatialReference == null &&
                    Class is IFeatureClass &&
                    ServiceMap != null &&
                    ServiceMap.Display.SpatialReference != null &&
                    !ServiceMap.Display.SpatialReference.Equals(((IFeatureClass)Class).SpatialReference))
                {
                    project = true;
                }

                IRow row;
                while ((row = await ReadNext(cursor)) != null)
                {
                    if ((counter + 1) < beginrecord)
                    {
                        counter++;
                        continue;
                    }

                    if (Classes.Count > 1)
                    {
                        MapServerHelper.AppendMergedID(MapServerHelper.GetClassID(ServiceMap, Class), row);
                    }

                    if (row is IFeature && ((IFeature)row).Shape != null &&
                        ServiceMap != null && ServiceMap.Display.SpatialReference != null
                        && Class is IFeatureClass)
                    {
                        IGeometry shape = ((IFeature)row).Shape;
                        // wenn Filter kein SpatialFilter ist, Shapes Projezieren...
                        //if (!(filter is ISpatialFilter))
                        //{
                        //    ISpatialReference sRef = ((IFeatureClass)Class).SpatialReference;
                        //    if (!ServiceMap.Display.SpatialReference.Equals(sRef))
                        //    {
                        //        ((IFeature)row).Shape = GeometricTransformer.Transform2D(
                        //            shape,
                        //            sRef,
                        //            ServiceMap.Display.SpatialReference);
                        //    }
                        //}

                        // Wenn kein FeatureSpatialReference angeben ist->
                        // Features ins Kartensystemprojezieren
                        if (project)
                        {
                            ISpatialReference sRef = ((IFeatureClass)Class).SpatialReference;
                            if (sRef == null && ServiceMap is IMap)
                            {
                                sRef = ((IMap)ServiceMap).LayerDefaultSpatialReference;
                            } ((Feature)row).Shape = GeometricTransformerFactory.Transform2D(
                                shape,
                                sRef,
                                ServiceMap.Display.SpatialReference);
                        }
                    }

                    xWriter.WriteStartElement("FEATURE");
                    if (envelope && row is ArcXMLFeature && ((ArcXMLFeature)row).Envelope != null)
                    {
                        ArcXMLFeature arcXMLRow = row as ArcXMLFeature;
                        IEnvelope e = arcXMLRow.Envelope;
                        // wenn Filter kein SpatialFilter ist, Shapes Projezieren...
                        //
                        // nicht mehr Projezieren, dass sollte schon durch die
                        // FeatureSpatialReference des Filters gelöst sein
                        // es kommt sonst zu mehrfachen Projezieren!!!
                        //
                        //if (!(filter is ISpatialFilter) && Class is IFeatureClass &&
                        //    ServiceMap != null && ServiceMap.Display.SpatialReference != null)
                        //{
                        //    ISpatialReference sRef = ((IFeatureClass)Class).SpatialReference;
                        //    if (!ServiceMap.Display.SpatialReference.Equals(sRef))
                        //    {
                        //        e = GeometricTransformer.Transform2D(
                        //            e,
                        //            sRef,
                        //            ServiceMap.Display.SpatialReference).Envelope;
                        //    }
                        //}
                        AXL.ENVELOPE(xWriter, e);
                        if (env == null)
                        {
                            env = new Envelope(e);
                        }
                        else
                        {
                            env.Union(e);
                        }
                    }
                    else if (envelope && row is IFeature)
                    {
                        if (((IFeature)row).Shape != null)
                        {
                            IEnvelope e = ((IFeature)row).Shape.Envelope;
                            AXL.ENVELOPE(xWriter, e);
                            if (env == null)
                            {
                                env = new Envelope(e);
                            }
                            else
                            {
                                env.Union(e);
                            }
                        }
                    }
                    if (row.Fields != null)
                    {
                        xWriter.WriteStartElement("FIELDS");
                        foreach (FieldValue fv in row.Fields)
                        {
                            xWriter.WriteStartElement("FIELD");
                            xWriter.WriteAttributeString("name", fv.Name);
                            xWriter.WriteAttributeString("value", fv.Value != null ? fv.Value.ToString() : "");
                            xWriter.WriteEndElement(); // FIELD
                        }
                        xWriter.WriteEndElement(); // FIELDS
                    }
                    if (geometry && row is IFeature)
                    {
                        xWriter.WriteRaw(ArcXMLGeometry.Geometry2AXL(((IFeature)row).Shape));
                    }
                    xWriter.WriteEndElement(); // FEATURE
                    counter++;
                    if (counter == featurelimit + Math.Max(beginrecord - 1, 0))
                    {
                        hasmore = true;
                        counter = counter - Math.Max(beginrecord - 1, 0);
                        cursor.Dispose();
                        break;
                    }
                }
                cursor.Dispose();
            }

            xWriter.WriteStartElement("FEATURECOUNT");
            xWriter.WriteAttributeString("count", counter.ToString());
            xWriter.WriteAttributeString("hasmore", hasmore.ToString().ToLower());
            xWriter.WriteEndElement(); // FEATURECOUNT

            if (env != null)
            {
                AXL.ENVELOPE(xWriter, env);
            }

            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        async private Task<IRow> ReadNext(ICursor cursor)
        {
            if (cursor == null)
            {
                return null;
            }

            if (cursor is IRowCursor)
            {
                return await ((IRowCursor)cursor).NextRow();
            }
            else if (cursor is IFeatureCursor)
            {
                return await ((IFeatureCursor)cursor).NextFeature();
            }

            return null;
        }
    }

    internal class GET_IMAGE_REQUEST
    {
        public Envelope Envelope = null;
        public int iWidth = 0, iHeight = 0;
        public XmlNode layerDefs = null;
        public XmlNodeList LAYERS = null;
        public float Dpi = 96f;
        //public float symbolScaleFactor = 1f;
        private ImageFormat _imageFormat;
        private string _imageExtension;
        private bool _useTOC = true;

        public GET_IMAGE_REQUEST()
        {
            this.ImageFormat = ImageFormat.Png;
        }

        async public Task<string> ImageRequest(IServiceMap map, IServiceRequestContext context, bool useTOC)
        {
            string ret = "";
            _useTOC = useTOC;

            await ModifyLAYERS(map);

            AXL response = new AXL("ARCXML", "1.1");

            map.Display.Dpi = Dpi;
            map.Display.ImageWidth = iWidth;
            map.Display.ImageHeight = iHeight;
            map.Display.Limit = Envelope;
            map.Display.ZoomTo(Envelope);
            //map.Display.drawScaleBar = false;
            map.BeforeRenderLayers += new BeforeRenderLayersEvent(map_BeforeRenderLayers);
            //map.ScaleSymbolFactor = symbolScaleFactor;
            await map.Render();

            string title = map.Name.Replace(",", "_").Replace("/", "_") + "_" + System.Guid.NewGuid().ToString("N") + _imageExtension;
            if (await map.SaveImage(context.MapServer.OutputPath + @"/" + title, _imageFormat) >= 0)
            {
                ret = response.IMAGE(context.MapServer.OutputPath, context.ServiceRequest.OutputUrl, title, map.Display.Envelope);
            }

            return ret;
        }

        public ImageFormat ImageFormat
        {
            get { return _imageFormat; }
            set
            {
                _imageFormat = value;
                if (_imageFormat == ImageFormat.Jpeg)
                {
                    _imageExtension = ".jpg";
                }
                else if (_imageFormat == ImageFormat.Gif)
                {
                    _imageExtension = ".gif";
                }
                else
                {
                    _imageExtension = ".png";
                }
            }
        }

        async public Task<string> LegendRequest(IServiceMap map, IServiceRequestContext context, bool useTOC)
        {
            string ret = "";
            try
            {
                AXL response = new AXL("ARCXML", "1.1");
                _useTOC = useTOC;

                map.Display.ImageWidth = iWidth;
                map.Display.ImageHeight = iHeight;
                map.Display.Limit = Envelope;
                map.Display.ZoomTo(Envelope);
                map.BeforeRenderLayers += new BeforeRenderLayersEvent(map_BeforeRenderLayers);
                var legend = await map.Legend();
                if (legend != null)
                {
                    string title = map.Name.Replace(",", "_") + "_" + System.Guid.NewGuid().ToString("N") + ".png";
                    legend.Save(context.MapServer.OutputPath + @"/" + title, ImageFormat.Png);
                    ret = response.LEGEND(context.MapServer.OutputPath, context.ServiceRequest.OutputUrl, title, map.Display.Envelope);
                }
            }
            catch { }
            return ret;
        }
        string LayerTitle(IServiceMap map, ILayer layer)
        {
            if (map.TOC != null)
            {
                // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                // verwendet werden kann zB bei gesplitteten Layern...
                //ITOCElement tocElement = map.TOC.GetTOCElement(layer.Class);
                ITocElement tocElement = map.TOC.GetTOCElement(layer);
                if (tocElement != null)
                {
                    return tocElement.Name;
                }
            }

            if (layer != null)
            {
                return layer.Title;
            }

            return "";
        }

        async void map_BeforeRenderLayers(IServiceMap sender, List<ILayer> layers)
        {
            if (layers == null)
            {
                return;
            }

            #region Set all layers invisible
            // ArcMap sends layerdef only for visible layers...
            foreach (ILayer layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.Visible = false;
            }
            #endregion

            // Sichtbarkeit schalten
            if (layerDefs != null)
            {
                foreach (XmlNode layerDef in layerDefs)
                {
                    if (layerDef.Attributes["id"] == null ||
                        layerDef.Attributes["visible"] == null)
                    {
                        continue;
                    }

                    try
                    {
                        foreach (ILayer layer in MapServerHelper.FindMapLayers(sender, _useTOC, layerDef.Attributes["id"].Value, layers))
                        {
                            layer.Visible = Convert.ToBoolean(layerDef.Attributes["visible"].Value);

                            if (sender.Display.ReferenceScale > 0D && layer is IFeatureLayer)
                            {
                                if (layerDef.Attributes["applyRefScale"] != null &&
                                    layerDef.Attributes["applyRefScale"].Value.ToLower() == "false")
                                {
                                    ((IFeatureLayer)layer).ApplyRefScale = false;
                                }

                                if (layerDef.Attributes["applyLabelRefScale"] != null &&
                                    layerDef.Attributes["applyLabelRefScale"].Value.ToLower() == "false")
                                {
                                    ((IFeatureLayer)layer).ApplyLabelRefScale = false;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // Renderer
            if (layerDefs != null)
            {
                foreach (XmlNode layerdef in layerDefs.SelectNodes("LAYERDEF[@id]"))
                {
                    MapServerHelper.Layers msLayers = MapServerHelper.FindMapLayers(sender, _useTOC, layerdef.Attributes["id"].Value, layers);

                    foreach (ILayer layer in msLayers)
                    {
                        if (layer is IFeatureLayer)
                        {
                            ArcIMSClass aClass = FindArcIMSClass(layer as IWebServiceTheme);
                            string layerID =
                                (layer is IWebServiceTheme) ?
                                ((IWebServiceTheme)layer).LayerID : "";

                            IFeatureLayer fLayer = (IFeatureLayer)layer;
                            if (fLayer.Visible == false)
                            {
                                continue;
                            }

                            SetLayerObjects(aClass, layerID, fLayer, layerdef, layers);
                        }
                    }
                }
            }

            ILayer element = null;
            // Additional Layers (acetate, selection,...)
            if (LAYERS != null)
            {
                foreach (XmlNode LAYER in LAYERS)
                {
                    element = null;
                    if (LAYER.Attributes["type"] == null)
                    {
                        continue;
                    }

                    if (LAYER.Attributes["type"].Value == "acetate")
                    {
                        foreach (XmlNode OBJECT in LAYER.SelectNodes("OBJECT"))
                        {
                            IGraphicElement grElement = ObjectFromAXLFactory.GraphicElement(OBJECT);
                            if (grElement != null)
                            {
                                sender.Display.GraphicsContainer.Elements.Add(grElement);
                            }
                        }
                    }
                    else if (LAYER.Attributes["type"].Value == "featureclass")
                    {
                        XmlNode datasetNode = LAYER.SelectSingleNode("DATASET[@fromlayer]");
                        if (datasetNode == null)
                        {
                            continue;
                        }

                        XmlNode bufferNode = LAYER.SelectSingleNode("QUERY/BUFFER[@distance]");
                        bufferNode = (bufferNode != null) ?
                            bufferNode :
                            LAYER.SelectSingleNode("SPATIALQUERY/BUFFER[@distance]");
                        XmlNode targetNode = (bufferNode != null) ?
                            bufferNode.SelectSingleNode("TARGETLAYER[@id]") :
                            null;

                        MapServerHelper.Layers aLayers;
                        //IDatasetElement e = null;
                        if (targetNode != null)
                        {
                            aLayers = MapServerHelper.FindMapLayers(sender, _useTOC, targetNode.Attributes["id"].Value, layers);
                        }
                        //e = FindElement(layers, targetNode.Attributes["id"].Value);
                        else
                        {
                            aLayers = MapServerHelper.FindMapLayers(sender, _useTOC, datasetNode.Attributes["fromlayer"].Value, layers);
                        }
                        //e = FindElement(layers, datasetNode.Attributes["fromlayer"].Value);

                        foreach (ILayer e in aLayers)
                        {
                            if (e is IFeatureLayer && e.Class != null)
                            {
                                // Achtung: LAYER Node kann verändert werden...
                                // in ModifyFilter, ReplaceQueryWhereClause
                                // darum: clonen...
                                XmlNode clonedLAYER = LAYER.Clone();
                                if (_useTOC)
                                {
                                    IQueryFilter filter = ParseChildNodesForQueryFilter(LAYER, e.Class as ITableClass);
                                    if (filter != null)
                                    {
                                        if (!await MapServerHelper.ModifyFilter(sender, e.Class as ITableClass, filter))
                                        {
                                            continue;
                                        }

                                        ReplaceQueryWhereClause(clonedLAYER, filter.WhereClause);
                                    }
                                }
                                ArcIMSClass aClass = FindArcIMSClass(e as IWebServiceTheme);
                                if (aClass != null)
                                {
                                    aClass.AppendedLayers.Add(
                                        Transform2LayerIDs(sender, aClass, layers, clonedLAYER));
                                    continue;
                                }
                                else
                                {
                                    if (targetNode == null && bufferNode != null)
                                    {
                                        // Buffer als Graphic !!
                                        IDatasetElement e1 = FindElement(layers, datasetNode.Attributes["fromlayer"].Value);
                                        if (e1 == null || e1.Class == null || !(e1 is IFeatureLayer))
                                        {
                                            continue;
                                        }

                                        IFeatureLayer fLayer = LayerFactory.Create(e1.Class, e1 as IFeatureLayer) as IFeatureLayer;
                                        if (fLayer == null)
                                        {
                                            continue;
                                        }

                                        SetLayerObjects(null, "", fLayer, clonedLAYER, layers);
                                        if (fLayer.FilterQuery is IBufferQueryFilter &&
                                            fLayer.FeatureRenderer is SimpleRenderer)
                                        {
                                            sender.Display.GraphicsContainer.Elements.Add(
                                                await ObjectFromAXLFactory.GraphicElement(
                                                    ((SimpleRenderer)fLayer.FeatureRenderer).Symbol,
                                                    (IBufferQueryFilter)fLayer.FilterQuery));
                                        }
                                        continue;
                                    }
                                    element = LayerFactory.Create(e.Class, e as IFeatureLayer);
                                    element.Visible = true;
                                    layers.Add(element);
                                }
                            }
                        }
                    }
                    if (element == null)
                    {
                        continue;
                    }

                    if (element is IFeatureLayer)
                    {
                        IFeatureLayer fLayer = (IFeatureLayer)element;

                        SetLayerObjects(null, "", fLayer, LAYER, layers);
                    }
                }
            }
        }

        private void SetLayerObjects(ArcIMSClass aClass, string LayerID, IFeatureLayer fLayer, XmlNode layerdef, List<ILayer> layers)
        {
            if (fLayer == null || layerdef == null)
            {
                return;
            }

            foreach (XmlNode child in layerdef.ChildNodes)
            {
                if (child.Name == "QUERY" || child.Name == "SPATIALQUERY")
                {
                    IFeatureClass rootFeatureClass = null;
                    XmlNode datasetNode = layerdef.SelectSingleNode("DATASET[@fromlayer]");
                    if (datasetNode != null)
                    {
                        IDatasetElement e = FindElement(layers, datasetNode.Attributes["fromlayer"].Value);
                        if (e is IFeatureLayer)
                        {
                            rootFeatureClass = ((IFeatureLayer)e).FeatureClass;
                        }
                    }

                    IQueryFilter orig = fLayer.FilterQuery;
                    fLayer.FilterQuery = ObjectFromAXLFactory.Query(child, fLayer.FeatureClass, rootFeatureClass);
                    if (orig != null)
                    {
                        if (fLayer.FilterQuery == null)
                        {
                            fLayer.FilterQuery = orig;
                        }
                        else if (!String.IsNullOrEmpty(fLayer.FilterQuery.WhereClause) &&
                                 !String.IsNullOrEmpty(orig.WhereClause))
                        {
                            fLayer.FilterQuery.WhereClause += " AND " + orig.WhereClause;
                        }
                        else if (String.IsNullOrEmpty(fLayer.FilterQuery.WhereClause) &&
                               !String.IsNullOrEmpty(orig.WhereClause))
                        {
                            fLayer.FilterQuery.WhereClause = orig.WhereClause;
                        }
                    }
                }
                else if (child.Name == "SIMPLERENDERER")
                {
                    if (aClass != null)
                    {
                        aClass.LayerRenderer.Add(LayerID, child);
                    }
                    else
                    {
                        fLayer.FeatureRenderer = ObjectFromAXLFactory.SimpleRenderer(child);
                    }
                }
                else if (child.Name == "VALUEMAPRENDERER")
                {
                    if (aClass != null)
                    {
                        aClass.LayerRenderer.Add(LayerID, child);
                    }
                    else
                    {
                        fLayer.FeatureRenderer = ObjectFromAXLFactory.ValueMapRenderer(child);
                    }
                }
                //else if (child.Name == "SIMPLELABELRENDERER")
                //{
                //    if (aClass != null)
                //        aClass.LayerRenderer.Add(((IWebServiceTheme)element).LayerID, child);
                //    else
                //    {
                //        fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child, fLayer.FeatureClass);
                //    }
                //}
                else if (child.Name == "GROUPRENDERER")
                {
                    if (aClass != null)
                    {
                        aClass.LayerRenderer.Add(LayerID, child);
                    }
                    else
                    {
                        fLayer.FeatureRenderer = ObjectFromAXLFactory.GroupRenderer(child);
                    }
                }
            }
            ParseChildNodesForLabelRenderer(aClass, LayerID, fLayer, layerdef);
        }

        private bool ParseChildNodesForLabelRenderer(ArcIMSClass aClass, string LayerID, IFeatureLayer fLayer, XmlNode layerdef)
        {
            if (fLayer == null || layerdef == null ||
                layerdef.ChildNodes == null || layerdef.ChildNodes.Count == 0)
            {
                return false;
            }

            foreach (XmlNode child in layerdef.ChildNodes)
            {
                if (child.Name == "SIMPLELABELRENDERER")
                {
                    fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child, fLayer.FeatureClass);
                    return true;
                }
                else if (child.Name == "GROUPRENDERER")
                {
                    if (ParseChildNodesForLabelRenderer(aClass, LayerID, fLayer, child))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private IQueryFilter ParseChildNodesForQueryFilter(XmlNode parentNode, ITableClass tc)
        {
            foreach (XmlNode child in parentNode.ChildNodes)
            {
                if (child.Name == "QUERY" || child.Name == "SPATIALQUERY")
                {
                    IQueryFilter filter = ObjectFromAXLFactory.Query(child, tc);
                    return filter;
                }
            }
            return null;
        }
        private void ReplaceQueryWhereClause(XmlNode parentNode, string newWhereClause)
        {
            foreach (XmlNode child in parentNode.ChildNodes)
            {
                if ((child.Name == "QUERY" || child.Name == "SPATIALQUERY") &&
                    child.Attributes["where"] != null)
                {
                    child.Attributes["where"].Value = newWhereClause;
                }
            }
        }
        #region Helper
        private ArcIMSClass FindArcIMSClass(IWebServiceTheme layer)
        {
            if (layer == null)
            {
                return null;
            }

            return layer.ServiceClass as ArcIMSClass;
        }

        private XmlNode Transform2LayerIDs(IServiceMap sender, ArcIMSClass aClass, List<ILayer> layers, XmlNode node)
        {
            if (aClass == null || layers == null || node == null)
            {
                return null;
            }

            XmlNode ret = node.Clone();
            foreach (XmlNode dataset in ret.SelectNodes("DATASET[@fromlayer]"))
            {
                MapServerHelper.Layers themes = MapServerHelper.FindMapLayers(sender, _useTOC, dataset.Attributes["fromlayer"].Value, layers);

                //IWebServiceTheme theme = FindElement(layers, dataset.Attributes["fromlayer"].Value) as IWebServiceTheme;
                foreach (ILayer theme in themes)
                {
                    if (!(theme is IWebServiceTheme) ||
                          ((IWebServiceTheme)theme).ServiceClass != aClass)
                    {
                        return null;
                    }

                    dataset.Attributes["fromlayer"].Value = ((IWebServiceTheme)theme).LayerID;
                }
            }

            foreach (XmlNode target in ret.SelectNodes("SPATIALQUERY/BUFFER/TARGETLAYER[@id]"))
            {
                MapServerHelper.Layers themes = MapServerHelper.FindMapLayers(sender, _useTOC, target.Attributes["id"].Value, layers);

                //IWebServiceTheme theme = FindElement(layers, target.Attributes["id"].Value) as IWebServiceTheme;
                foreach (ILayer theme in themes)
                {
                    if (!(theme is IWebServiceTheme) ||
                          ((IWebServiceTheme)theme).ServiceClass != aClass)
                    {
                        return null;
                    }

                    target.Attributes["id"].Value = ((IWebServiceTheme)theme).LayerID;
                }
            }

            foreach (XmlNode target in ret.SelectNodes("QUERY/BUFFER/TARGETLAYER[@id]"))
            {
                MapServerHelper.Layers themes = MapServerHelper.FindMapLayers(sender, _useTOC, target.Attributes["id"].Value, layers);

                //IWebServiceTheme theme = FindElement(layers, target.Attributes["id"].Value) as IWebServiceTheme;
                foreach (ILayer theme in themes)
                {
                    if (!(theme is IWebServiceTheme) ||
                          ((IWebServiceTheme)theme).ServiceClass != aClass)
                    {
                        return null;
                    }

                    target.Attributes["id"].Value = ((IWebServiceTheme)theme).LayerID;
                }
            }

            return ret;
        }

        private ILayer FindElement(List<ILayer> layers, string id)
        {
            foreach (ILayer element in layers)
            {
                if (element == null)
                {
                    continue;
                }

                //if (element.ID.ToString() == id)
                if (element.SID.ToString() == id)
                {
                    return element;
                }
            }
            return null;
        }

        private class GraphicsFeatureClass : FeatureLayer
        {
        }

        /// <summary>
        /// Überprüft, ob alle zusätzlichen Layer maximal einem darunterliegenden
        /// Webservice abhängen. Wenn nicht werden die Filter entsprechend umgebaut!
        /// zB beim Serviceübergreifenden Buffern...
        /// </summary>
        /// <param name="map"></param>
        async private Task ModifyLAYERS(IServiceMap map)
        {
            if (LAYERS == null)
            {
                return;
            }

            foreach (XmlNode LAYER in LAYERS)
            {
                if (LAYER == null || LAYER.Attributes["type"] == null)
                {
                    continue;
                }

                if (LAYER.Attributes["type"].Value == "featureclass")
                {
                    XmlNode datasetNode = LAYER.SelectSingleNode("DATASET[@fromlayer]");
                    if (datasetNode == null)
                    {
                        continue;
                    }

                    XmlNode queryNode = (LAYER.SelectSingleNode("QUERY") != null) ?
                        LAYER.SelectSingleNode("QUERY") :
                        LAYER.SelectSingleNode("SPATIALQUERY");
                    if (queryNode == null)
                    {
                        continue;
                    }

                    XmlNode bufferNode = queryNode.SelectSingleNode("BUFFER[@distance]");
                    XmlNode targetNode = (bufferNode != null) ?
                        bufferNode.SelectSingleNode("TARGETLAYER[@id]") :
                        null;

                    if (bufferNode != null && targetNode != null)
                    {
                        MapServerHelper.Layers target = MapServerHelper.FindMapLayers(map, _useTOC, targetNode.Attributes["id"].Value);
                        MapServerHelper.Layers fromlayer = MapServerHelper.FindMapLayers(map, _useTOC, datasetNode.Attributes["fromlayer"].Value);

                        if (target.Count == 1 && fromlayer.Count == 1 &&
                            target[0] is IWebServiceTheme)
                        {
                            IWebServiceClass targetWebClass = ((IWebServiceTheme)target[0]).ServiceClass;
                            if ((fromlayer[0] is IWebServiceTheme &&
                               !((IWebServiceTheme)fromlayer[0]).ServiceClass.Equals(targetWebClass)) ||
                               !(fromlayer[0] is IWebServiceTheme))
                            {
                                IQueryFilter filter = ObjectFromAXLFactory.Query(queryNode, target[0].Class as ITableClass, fromlayer[0].Class as IFeatureClass);
                                filter.SetUserData("IServiceRequestContext", map);

                                if (filter is IBufferQueryFilter)
                                {
                                    ((IBufferQueryFilter)filter).RootFilter.SetUserData("IServiceRequestContext", map);
                                    filter = await BufferQueryFilter.ConvertToSpatialFilter((IBufferQueryFilter)filter);
                                }

                                if (target[0].Class is IBeforeQueryEventHandler)
                                {
                                    ((IBeforeQueryEventHandler)target[0].Class).FireBeforeQureyEvent(ref filter);
                                }

                                string axl = AXLFromObjectFactory.Query(filter);
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(axl);
                                LAYER.RemoveChild(queryNode);
                                LAYER.AppendChild(LAYER.OwnerDocument.ImportNode(doc.ChildNodes[0], true));

                                datasetNode.Attributes["fromlayer"].Value = targetNode.Attributes["id"].Value;

                                HatchSymbol symbol = new HatchSymbol();
                                symbol.PenColor = ArgbColor.FromArgb(150, ArgbColor.Red);

                                IGraphicElement grElement = new AcetateGraphicElement(symbol, ((ISpatialFilter)filter).Geometry);
                                map.Display.GraphicsContainer.Elements.Add(grElement);
                            }
                        }
                    }
                }
            }
        }

        private class AcetateGraphicElement : IGraphicElement
        {
            private ISymbol _symbol;
            private IGeometry _geometry;

            public AcetateGraphicElement(ISymbol symbol, IGeometry geometry)
            {
                _symbol = symbol;
                _geometry = geometry;
            }

            #region IGraphicElement Member

            public void Draw(IDisplay display)
            {
                if (_symbol == null || _geometry == null)
                {
                    return;
                }

                display.Draw(_symbol, _geometry);
            }

            #endregion
        }
        #endregion
    }

    internal class GET_RASTER_INFO
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public List<IClass> Classes = null;
        public List<IPoint> Points = null;
        public ISpatialReference SpatialReference = null;
        public IDisplay Display = null;
        public IUserData UserData = null;
        public bool Compact = false, Grid = false;
        public int CompactRows = 0, CompactCols = 0;
        public double GridDx = 1, GridDy = 1;

        async public Task<string> Request()
        {
            if (Classes == null || Points == null)
            {
                return String.Empty;
            }

            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");

            xWriter.WriteStartElement("RESPONSE");

            if (Grid)
            {
                if (Classes.Count == 1 && Classes[0] is IMultiGridIdentify)
                {
                    IMultiGridIdentify mgi = (IMultiGridIdentify)Classes[0];
                    float[] vals = await mgi.MultiGridQuery(Display, Points.ToArray(), GridDx, GridDy, SpatialReference, UserData);

                    xWriter.WriteStartElement("DATA");
                    xWriter.WriteAttributeString("rows", ((int)vals[0]).ToString());
                    xWriter.WriteAttributeString("cols", ((int)vals[1]).ToString());
                    StringBuilder sb = new StringBuilder();
                    for (int i = 2; i < vals.Length; i++)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(" ");
                        }

                        sb.Append(vals[i].ToString());
                    }
                    xWriter.WriteRaw(sb.ToString());
                }
            }
            else
            {
                if (Compact)
                {
                    xWriter.WriteStartElement("DATA");
                    xWriter.WriteAttributeString("rows", CompactRows.ToString());
                    xWriter.WriteAttributeString("cols", CompactCols.ToString());
                }
                foreach (IClass rasterClass in Classes)
                {
                    //if (!(rasterClass is IRasterClass)) continue;

                    if (rasterClass is IMulitPointIdentify)
                    {
                        PointCollection pColl = new PointCollection(Points);
                        using (ICursor cursor = await ((IMulitPointIdentify)rasterClass).MultiPointQuery(Display, pColl, SpatialReference, UserData))
                        {
                            if (Compact)
                            {
                                await AddCompactRasterInfo2(xWriter, cursor, null);
                            }
                            else
                            {
                                await AddRasterInfo(xWriter, cursor, null);
                            }
                        }
                    }
                    else if (rasterClass is IPointIdentify)
                    {
                        using (IPointIdentifyContext pointIdentifyContext = ((IPointIdentify)rasterClass).CreatePointIdentifyContext())
                        {
                            foreach (IPoint point in Points)
                            {
                                using (ICursor cursor = await ((IPointIdentify)rasterClass).PointQuery(Display, point, SpatialReference, UserData, pointIdentifyContext))
                                {
                                    if (Compact)
                                    {
                                        await AddCompactRasterInfo(xWriter, cursor, point);
                                    }
                                    else
                                    {
                                        await AddRasterInfo(xWriter, cursor, point);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        async private Task AddRasterInfo(XmlTextWriter xWriter, ICursor cursor, IPoint point)
        {
            if (cursor == null)
            {
                return;
            }

            if (cursor is IRowCursor)
            {
                IRow row;
                while ((row = await ReadNext(cursor)) != null)
                {
                    if (row.Fields != null)
                    {
                        xWriter.WriteStartElement("RASTER_INFO");
                        if (point != null)
                        {
                            xWriter.WriteAttributeString("x", point.X.ToString());
                            xWriter.WriteAttributeString("y", point.Y.ToString());
                        }
                        else
                        {
                            object x_ = row["x"], y_ = row["y"];
                            if (x_ != null)
                            {
                                xWriter.WriteAttributeString("x", x_.ToString());
                            }

                            if (y_ != null)
                            {
                                xWriter.WriteAttributeString("y", y_.ToString());
                            }
                        }
                        xWriter.WriteStartElement("BANDS");

                        int bandnumber = 0;
                        foreach (FieldValue fv in row.Fields)
                        {
                            double val;
                            if (fv != null && fv.Value != null &&
                                fv.Name.ToLower().StartsWith("band") &&
                                double.TryParse(fv.Value.ToString(), out val))
                            {
                                xWriter.WriteStartElement("BAND");
                                xWriter.WriteAttributeString("name", fv.Name);
                                xWriter.WriteAttributeString("number", bandnumber.ToString());
                                xWriter.WriteAttributeString("value", val.ToString());
                                xWriter.WriteEndElement(); // BAND
                                bandnumber++;
                            }
                            else if (fv != null && fv.Value != null)
                            {
                                xWriter.WriteStartElement("attribute");
                                xWriter.WriteAttributeString("name", fv.Name);
                                if (fv.Value != null)
                                {
                                    xWriter.WriteAttributeString("value", fv.Value.ToString());
                                }
                                else
                                {
                                    xWriter.WriteAttributeString("value", "");
                                }

                                xWriter.WriteEndElement();
                            }
                        }

                        xWriter.WriteEndElement(); // BANDS
                        xWriter.WriteEndElement(); // RASTER_INFO
                    }
                }
            }
            else if (cursor is IUrlCursor)
            {
                xWriter.WriteStartElement("gv_url_info");
                xWriter.WriteAttributeString("url", ((IUrlCursor)cursor).Url);
                xWriter.WriteEndElement();
            }
            else if (cursor is ITextCursor)
            {
                xWriter.WriteStartElement("gv_text_info");
                xWriter.WriteAttributeString("text", ((ITextCursor)cursor).Text.Replace("\r", "").Replace("\n", "\\n"));
                xWriter.WriteEndElement();
            }
        }

        async private Task AddCompactRasterInfo(XmlTextWriter xWriter, ICursor cursor, IPoint point)
        {
            xWriter.WriteRaw(point.X.ToString(_nhi) + "," + point.Y.ToString(_nhi));

            if (cursor != null)
            {
                if (cursor is IRowCursor)
                {
                    IRow row;
                    while ((row = await ReadNext(cursor)) != null)
                    {
                        foreach (FieldValue fv in row.Fields)
                        {
                            double val;
                            if (fv != null && fv.Value != null &&
                                fv.Name.ToLower().StartsWith("band") &&
                                double.TryParse(fv.Value.ToString(), out val))
                            {
                                xWriter.WriteRaw("," + val.ToString().Replace(",", "."));
                            }
                            else
                            {
                                //xWriter.WriteRaw(",0");
                            }
                        }
                        if (point == null)
                        {
                            xWriter.WriteRaw(" ");
                        }
                    }
                }
            }
            xWriter.WriteRaw(" ");
        }

        async private Task AddCompactRasterInfo2(XmlTextWriter xWriter, ICursor cursor, IPoint point)
        {
            List<IPoint> clonePointList = ListOperations<IPoint>.Clone(Points);

            if (cursor is IRowCursor)
            {
                IRow row;
                while ((row = await ReadNext(cursor)) != null)
                {
                    Point p = new Point((double)row["x"], (double)row["y"]);
                    double val = double.NaN;

                    foreach (FieldValue fv in row.Fields)
                    {
                        if (fv != null && fv.Value != null &&
                            fv.Name.ToLower().StartsWith("band") &&
                            double.TryParse(fv.Value.ToString(), out val))
                        {
                            break;
                        }
                    }
                    if (!double.IsNaN(val))
                    {
                        IPoint r = null;
                        foreach (IPoint c in clonePointList)
                        {
                            if (p.Equals(c))
                            {
                                c.Z = val;
                                r = c;
                                break;
                            }
                        }
                        if (r != null)
                        {
                            clonePointList.Remove(r);
                        }
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (IPoint c in Points)
            {
                sb.Append(c.X.ToString(_nhi) + "," + c.Y.ToString(_nhi) + "," + c.Z.ToString(_nhi) + " ");
            }
            xWriter.WriteRaw(sb.ToString());
        }
        async private Task<IRow> ReadNext(ICursor cursor)
        {
            if (cursor == null)
            {
                return null;
            }

            if (cursor is IRowCursor)
            {
                return await ((IRowCursor)cursor).NextRow();
            }
            else if (cursor is IFeatureCursor)
            {
                return await ((IFeatureCursor)cursor).NextFeature();
            }

            return null;
        }
    }

    public class AXL
    {
        string _rootElement, _version;
        public AXL(string rootElement, string version)
        {
            _rootElement = rootElement;
            _version = version;
        }

        //public IServiceMap Map(IMapServer server, string serviceName)
        //{
        //    if (server == null) return null;

        //    return server[serviceName];
        //}

        async public Task<string> GETCLIENTSERVICES(IMapServer server, IIdentity identity)
        {
            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement(_rootElement);
            xWriter.WriteAttributeString("version", _version);

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("SERVICES");

            foreach (IMapService map in await server.Maps(identity))
            {
                xWriter.WriteStartElement("SERVICE");
                xWriter.WriteAttributeString("access", "PUBLIC");
                xWriter.WriteAttributeString("ACCESS", "PUBLIC");
                xWriter.WriteAttributeString("name", map.Fullname);
                xWriter.WriteAttributeString("NAME", map.Fullname);
                xWriter.WriteAttributeString("private", "true");
                xWriter.WriteAttributeString("servicegroup", "ImageServer1");
                xWriter.WriteAttributeString("SERVICEGROUP", "ImageServer1");
                xWriter.WriteAttributeString("status", "ENABLED");
                xWriter.WriteAttributeString("STATUS", "ENABLED");
                xWriter.WriteAttributeString("type", "ImageServer");
                xWriter.WriteAttributeString("TYPE", "ImageServer");
                xWriter.WriteAttributeString("version", "");
                xWriter.WriteAttributeString("VERSION", "");
                xWriter.WriteAttributeString("servicetype", map.Type.ToString());

                xWriter.WriteStartElement("IMAGE");
                xWriter.WriteAttributeString("type", "PNG");
                xWriter.WriteEndElement(); // IMAGE

                xWriter.WriteStartElement("ENVIRONMENT");
                xWriter.WriteStartElement("LOCALE");
                xWriter.WriteAttributeString("country", "AT");
                xWriter.WriteAttributeString("language", "de");
                xWriter.WriteAttributeString("variant", "");
                xWriter.WriteEndElement(); // LOCALE
                xWriter.WriteStartElement("UIFONT");
                xWriter.WriteAttributeString("name", "Arial");
                xWriter.WriteEndElement(); // UIFONG
                xWriter.WriteEndElement(); // ENVIRONMENT

                xWriter.WriteStartElement("CLEANUP");
                xWriter.WriteAttributeString("interval", "5");
                xWriter.WriteEndElement(); // CLEANUP;

                xWriter.WriteEndElement(); // SERVICE
            }
            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        private int GetAxlFieldType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.ID: return -99;
                case FieldType.Shape: return -98;
                case FieldType.boolean: return -7;
                case FieldType.biginteger: return -5;
                case FieldType.character: return 1;
                case FieldType.integer: return 4;
                case FieldType.smallinteger: return 5;
                case FieldType.Float: return 6;
                case FieldType.Double: return 8;
                case FieldType.NString:
                case FieldType.String:
                    return 12;
                case FieldType.Date: return 91;
            }
            return 0;
        }
        private void WriteLayerInfoTOC(XmlTextWriter xw, ILegendGroup lGroup)
        {
            if (lGroup == null)
            {
                return;
            }

            xw.WriteStartElement("TOC");
            xw.WriteStartElement("TOCGROUP");
            for (int i = 0; i < lGroup.LegendItemCount; i++)
            {
                ILegendItem lItem = lGroup.LegendItem(i);
                if (lItem == null)
                {
                    continue;
                }

                xw.WriteStartElement("TOCCLASS");
                xw.WriteAttributeString("label", lItem.LegendLabel);
                xw.WriteAttributeString("description", "");

                using (var bitmap = Current.Engine.CreateBitmap(25, 14))
                {
                    using (var canvas = bitmap.CreateCanvas())
                    {
                        new SymbolPreview(null).Draw(canvas, new CanvasRectangle(0, 0, 25, 14), lItem as ISymbol);
                    }

                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Bmp);
                    xw.WriteRaw(Convert.ToBase64String(ms.ToArray()));
                }

                xw.WriteEndElement(); // TOCCLASS
            }
            xw.WriteEndElement(); // TOCGROUP
            xw.WriteEndElement(); // TOC
        }

        async public Task<string> GET_SERVICE_INFO(IServiceRequestContext context, bool fields, bool envelope, bool renderers, bool toc, bool gv_meta, bool useTOC)
        {
            if (context == null)
            {
                return String.Empty;
            }

            using (IServiceMap map = await context.CreateServiceMapInstance())
            {

                if (map == null)
                {
                    return "";
                }

                double dpm = 96.0 / 0.0254;

                StringBuilder axl = new StringBuilder();
                MemoryStream ms = new MemoryStream();
                XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

                xWriter.WriteStartDocument();
                xWriter.WriteStartElement(_rootElement);

                xWriter.WriteAttributeString("version", _version);

                //xWriter.WriteStartElement("ONLINE_RES");
                //xWriter.WriteAttributeString("service_name", map.Name);
                //xWriter.WriteAttributeString("xlink_href", "http://localhost/gViewPortal/wms.aspx?ServiceName=" + map.Name + "&");
                //xWriter.WriteEndElement(); // ONLINE_RES

                xWriter.WriteStartElement("RESPONSE");
                xWriter.WriteStartElement("SERVICEINFO");
                //xWriter.WriteAttributeString("service", map.Name);
                xWriter.WriteStartElement("ENVIRONMENT");

                xWriter.WriteStartElement("LOCALE");
                string[] culture = System.Globalization.CultureInfo.CurrentCulture.Name.Split('-');
                xWriter.WriteAttributeString("language", culture[0]);
                xWriter.WriteAttributeString("country", (culture.Length > 1) ? culture[1] : culture[0]);
                xWriter.WriteEndElement(); // LOCALE

                xWriter.WriteStartElement("UIFONT");
                xWriter.WriteAttributeString("name", "Arial");
                xWriter.WriteAttributeString("color", "0,0,0");
                xWriter.WriteAttributeString("size", "12");
                xWriter.WriteAttributeString("sytle", "regular");
                xWriter.WriteEndElement(); // UIFONT

                xWriter.WriteStartElement("SEPARATORS");
                xWriter.WriteAttributeString("cs", " ");
                xWriter.WriteAttributeString("ts", ";");
                xWriter.WriteAttributeString("dec", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                xWriter.WriteEndElement(); // SEPARATORS

                xWriter.WriteStartElement("CAPABILITIES");
                xWriter.WriteAttributeString("forbidden", "");
                xWriter.WriteAttributeString("disabledtypes", "");
                xWriter.WriteAttributeString("returngeometry", "xmlmode");
                xWriter.WriteAttributeString("displayrotation", "true");
                xWriter.WriteAttributeString("refscale", "true");
                xWriter.WriteEndElement(); // CAPABILITIES

                xWriter.WriteStartElement("SCREEN");
                xWriter.WriteAttributeString("dpi", "96");
                xWriter.WriteEndElement(); // SCREEN

                xWriter.WriteStartElement("IMAGELIMIT");
                xWriter.WriteAttributeString("pixelcount", "52428800");
                xWriter.WriteEndElement(); // IMAGELIMIT

                xWriter.WriteEndElement(); // ENVIRONMENT

                List<IDatasetElement> layers;

                xWriter.WriteStartElement("PROPERTIES");
                if (map.Display.SpatialReference != null)
                {
                    ISpatialReference sRef = map.Display.SpatialReference;
                    if (sRef.Name.ToLower().StartsWith("epsg:"))
                    {
                        xWriter.WriteStartElement("FEATURECOORDSYS");
                        xWriter.WriteAttributeString("id", sRef.Name.Split(':')[1]);
                        xWriter.WriteEndElement();
                        xWriter.WriteStartElement("FILTERCOORDSYS");
                        xWriter.WriteAttributeString("id", sRef.Name.Split(':')[1]);
                        xWriter.WriteEndElement();
                    }
                    else
                    {
                        xWriter.WriteStartElement("FEATURECOORDSYS");
                        xWriter.WriteAttributeString("string", SpatialReference.ToESRIWKT(sRef));
                        xWriter.WriteEndElement();
                        xWriter.WriteStartElement("FILTERCOORDSYS");
                        xWriter.WriteAttributeString("string", SpatialReference.ToESRIWKT(sRef));
                        xWriter.WriteEndElement();
                    }
                }
                if (envelope)
                {
                    Envelope mapEnvelope = null;
                    layers = map.MapElements;

                    foreach (IDatasetElement element in map.MapElements)
                    {
                        IEnvelope env = null;
                        if (element is IFeatureLayer)
                        {
                            if (((IFeatureLayer)element).FeatureClass == null)
                            {
                                continue;
                            }

                            IGeometry geom = GeometricTransformerFactory.Transform2D(((IFeatureLayer)element).FeatureClass.Envelope, ((IFeatureLayer)element).FeatureClass.SpatialReference, map.Display.SpatialReference);
                            if (geom != null)
                            {
                                env = geom.Envelope;
                            }
                        }
                        else if (element is IRasterLayer && ((IRasterLayer)element).RasterClass != null)
                        {
                            if (((IRasterLayer)element).RasterClass.Polygon == null)
                            {
                                continue;
                            }

                            IGeometry geom = GeometricTransformerFactory.Transform2D(((IRasterLayer)element).RasterClass.Polygon.Envelope, ((IRasterLayer)element).RasterClass.SpatialReference, map.Display.SpatialReference);
                            if (geom != null)
                            {
                                env = geom.Envelope;
                            }
                        }
                        else if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null)
                        {
                            if (((IWebServiceLayer)element).WebServiceClass.Envelope == null)
                            {
                                continue;
                            }

                            IGeometry geom = GeometricTransformerFactory.Transform2D(((IWebServiceLayer)element).WebServiceClass.Envelope, ((IWebServiceLayer)element).WebServiceClass.SpatialReference, map.Display.SpatialReference);
                            if (geom != null)
                            {
                                env = geom.Envelope;
                            }
                        }
                        if (env == null)
                        {
                            continue;
                        }

                        if (mapEnvelope == null)
                        {
                            mapEnvelope = new Envelope(env);
                        }
                        else
                        {
                            mapEnvelope.Union(env);
                        }
                    }
                    ENVELOPE(xWriter, mapEnvelope, "Initial_Extent");
                }

                /*
                if (map.Display.SpatialReference != null)
                {
                    // SpatialReference
                    xWriter.WriteStartElement("SPATIALREFERENCE");
                    xWriter.WriteAttributeString("name", map.Display.SpatialReference.Name);
                    xWriter.WriteAttributeString("param", SpatialReference.ToProj4(map.Display.SpatialReference));
                    xWriter.WriteEndElement();

                    // FEATURECOORDSYS
                    xWriter.WriteStartElement("FEATURECOORDSYS");
                    xWriter.WriteAttributeString("string", SpatialReference.ToESRIWKT(map.Display.SpatialReference));
                    xWriter.WriteEndElement();
                    // FILTERCOORDSYS
                    xWriter.WriteStartElement("FILTERCOORDSYS");
                    xWriter.WriteAttributeString("string", SpatialReference.ToESRIWKT(map.Display.SpatialReference));
                    xWriter.WriteEndElement();
                }
                */

                xWriter.WriteStartElement("MAPUNITS");
                switch (map.Display.DisplayUnits)
                {
                    case GeoUnits.Meters:
                        xWriter.WriteAttributeString("units", "meters");
                        break;
                    case GeoUnits.DecimalDegrees:
                        xWriter.WriteAttributeString("units", "decimal_degrees");
                        break;
                    case GeoUnits.Feet:
                        xWriter.WriteAttributeString("units", "feet");
                        break;
                    default:
                        xWriter.WriteAttributeString("units", "meters");
                        break;
                }
                xWriter.WriteEndElement();  // MAPUNITS

                xWriter.WriteEndElement(); // PROPERTIES

                //layers = map.MapElements;

                //StringBuilder sb = new StringBuilder();
                //DateTime t1 = DateTime.Now;
                string name = String.Empty;

                foreach (MapServerHelper.Layers Layers in MapServerHelper.MapLayers(map, useTOC))
                {
                    //if (!String.IsNullOrEmpty(name))
                    //{
                    //    sb.Append(name + " " + ((TimeSpan)(DateTime.Now - t1)).TotalMilliseconds.ToString() + "ms\n");
                    //    t1 = DateTime.Now;
                    //}
                    if (Layers == null || Layers.FirstLayer == null)
                    {
                        continue;
                    }

                    name = Layers.Title;
                    ILayer element = Layers.FirstLayer;

                    //ITOCElement tocElement = null;
                    //if (map.TOC != null && element is ILayer)
                    //{
                    //    tocElement = map.TOC.GetTOCElement(element as ILayer);
                    //    if (tocElement != null) name = tocElement.name;
                    //}
                    if (element is IFeatureLayer)
                    {
                        IFeatureLayer fLayer = (IFeatureLayer)element;

                        IFeatureClass fClass = MapServerHelper.GetProtoFeatureClass(Layers);
                        if (fClass != null)
                        {
                            xWriter.WriteStartElement("LAYERINFO");
                            xWriter.WriteAttributeString("type", "featureclass");
                            xWriter.WriteAttributeString("name", (name != "") ? name : fLayer.Title.Replace(".", "_"));
                            xWriter.WriteAttributeString("id", Layers.ID);
                            xWriter.WriteAttributeString("visible", fLayer.Visible.ToString().ToLower());
                            if (fLayer.MinimumScale > 1)
                            {
                                xWriter.WriteAttributeString("minscale", (fLayer.MinimumScale / dpm).ToString());
                            }

                            if (fLayer.MaximumScale > 1)
                            {
                                xWriter.WriteAttributeString("maxscale", (fLayer.MaximumScale / dpm).ToString());
                            }

                            //xWriter.WriteAttributeString("aliasname", Layers.Title);
                            if (String.IsNullOrEmpty(Layers.GroupName))
                            {
                                xWriter.WriteAttributeString("group", Layers.GroupName);
                            }

                            xWriter.WriteStartElement("FCLASS");

                            var geomType = fClass.GeometryType;  // if layer is SQL Spatial with undefined geometrytype...
                            if (geomType == GeometryType.Unknown && fLayer.LayerGeometryType != GeometryType.Unknown)  // take the settings from layer-properties
                            {
                                geomType = fLayer.LayerGeometryType;
                            }

                            switch (geomType)
                            {
                                case GeometryType.Envelope:
                                case GeometryType.Polygon:
                                    xWriter.WriteAttributeString("type", "polygon");
                                    break;
                                case GeometryType.Polyline:
                                    xWriter.WriteAttributeString("type", "line");
                                    break;
                                case GeometryType.Point:
                                case GeometryType.Multipoint:
                                    xWriter.WriteAttributeString("type", "point");
                                    break;
                                case GeometryType.Network:
                                    xWriter.WriteAttributeString("type", "network");
                                    break;
                                default:
                                    xWriter.WriteAttributeString("type", "unknown");
                                    break;
                            }
                            if (envelope)
                            {
                                IGeometry geom = GeometricTransformerFactory.Transform2D(fClass.Envelope, fClass.SpatialReference, map.Display.SpatialReference);
                                if (geom != null)
                                {
                                    ENVELOPE(xWriter, geom.Envelope);
                                }
                            }
                            if (fields)
                            {
                                foreach (IField field in fClass.Fields.ToEnumerable())
                                {
                                    if (field.type == FieldType.Shape)
                                    {
                                        continue;
                                    }

                                    xWriter.WriteStartElement("FIELD");
                                    xWriter.WriteAttributeString("name", field.name);
                                    xWriter.WriteAttributeString("type", GetAxlFieldType(field.type).ToString());
                                    xWriter.WriteAttributeString("size", field.size.ToString());
                                    xWriter.WriteAttributeString("precision", field.precision.ToString());
                                    xWriter.WriteEndElement(); // FIELD
                                }
                                if (fClass.ShapeFieldName != String.Empty)
                                {
                                    xWriter.WriteStartElement("FIELD");
                                    xWriter.WriteAttributeString("name", fClass.ShapeFieldName);
                                    xWriter.WriteAttributeString("type", GetAxlFieldType(FieldType.Shape).ToString());
                                    xWriter.WriteEndElement(); // FIELD
                                }
                            }
                            xWriter.WriteEndElement(); // FCLASS
                            if (toc)
                            {
                                this.WriteLayerInfoTOC(xWriter, fLayer.FeatureRenderer as ILegendGroup);
                            }

                            if (renderers)
                            {
                                xWriter.WriteRaw(Renderer(element as IFeatureLayer));
                            }

                            if (gv_meta)
                            {
                                WriteClassMeta(xWriter, fClass);
                            }

                            if (fClass.GeometryType == GeometryType.Network)
                            {
                                WriteNetworkTracers(xWriter);
                            }

                            if (fClass.GeometryType == GeometryType.Network)
                            {

                            }

                            xWriter.WriteEndElement(); // LAYERINFO
                        }
                        else  // Raster (und alles andere)
                        {
                            xWriter.WriteStartElement("LAYERINFO");
                            xWriter.WriteAttributeString("type", "image");
                            xWriter.WriteAttributeString("name", (name != "") ? name : element.Title.Replace(".", "_"));
                            xWriter.WriteAttributeString("id", Layers.ID);
                            xWriter.WriteAttributeString("visible", element.Visible.ToString().ToLower());

                            if (element.MinimumScale > 1)
                            {
                                xWriter.WriteAttributeString("minscale", (element.MinimumScale / dpm).ToString());
                            }

                            if (element.MaximumScale > 1)
                            {
                                xWriter.WriteAttributeString("maxscale", (element.MaximumScale / dpm).ToString());
                            }

                            if (String.IsNullOrEmpty(Layers.GroupName))
                            {
                                xWriter.WriteAttributeString("group", Layers.GroupName);
                            }

                            if (gv_meta)
                            {
                                WriteClassMeta(xWriter, element.Class);
                            }

                            xWriter.WriteEndElement(); // LAYERINFO
                        }
                    }
                    else if (element is IRasterLayer && ((IRasterLayer)element).RasterClass != null)
                    {
                        IRasterLayer rLayer = (IRasterLayer)element;
                        xWriter.WriteStartElement("LAYERINFO");
                        xWriter.WriteAttributeString("type", "image");
                        xWriter.WriteAttributeString("name", (name != "") ? name : rLayer.Title.Replace(".", "_"));
                        xWriter.WriteAttributeString("id", Layers.ID);
                        xWriter.WriteAttributeString("visible", rLayer.Visible.ToString().ToLower());

                        if (rLayer.MinimumScale > 1)
                        {
                            xWriter.WriteAttributeString("minscale", (rLayer.MinimumScale / dpm).ToString());
                        }

                        if (rLayer.MaximumScale > 1)
                        {
                            xWriter.WriteAttributeString("maxscale", (rLayer.MaximumScale / dpm).ToString());
                        }

                        //ITOCElement tocElement = map.TOC.GetTOCElement((ILayer)element);
                        //xWriter.WriteAttributeString("aliasname", Layers.Title);
                        if (String.IsNullOrEmpty(Layers.GroupName))
                        {
                            xWriter.WriteAttributeString("group", Layers.GroupName);
                        }

                        if (envelope && rLayer.RasterClass.Polygon != null)
                        {
                            IGeometry geom = GeometricTransformerFactory.Transform2D(rLayer.RasterClass.Polygon.Envelope, rLayer.RasterClass.SpatialReference, map.Display.SpatialReference);
                            if (geom != null)
                            {
                                ENVELOPE(xWriter, geom.Envelope);
                            }
                        }

                        if (gv_meta)
                        {
                            WriteClassMeta(xWriter, rLayer.Class);
                        }

                        xWriter.WriteEndElement(); // LAYERINFO
                    }
                    else if (element is IGroupLayer)
                    {
                        // brauch ma net. Macht nur Probleme wenn man die als Layer mitübernimmt...
                        // Gruppen können naml. gleich wie Layer heißen und schon krachts...
                        continue;
                    }
                    else  // Raster (und alles andere)
                    {
                        xWriter.WriteStartElement("LAYERINFO");
                        xWriter.WriteAttributeString("type", "image");
                        xWriter.WriteAttributeString("name", (name != "") ? name : element.Title.Replace(".", "_"));
                        xWriter.WriteAttributeString("id", Layers.ID);
                        xWriter.WriteAttributeString("visible", element.Visible.ToString().ToLower());

                        if (element.MinimumScale > 1)
                        {
                            xWriter.WriteAttributeString("minscale", (element.MinimumScale / dpm).ToString());
                        }

                        if (element.MaximumScale > 1)
                        {
                            xWriter.WriteAttributeString("maxscale", (element.MaximumScale / dpm).ToString());
                        }

                        if (String.IsNullOrEmpty(Layers.GroupName))
                        {
                            xWriter.WriteAttributeString("group", Layers.GroupName);
                        }

                        if (gv_meta)
                        {
                            WriteClassMeta(xWriter, element.Class);
                        }

                        xWriter.WriteEndElement(); // LAYERINFO
                    }
                }
                xWriter.WriteEndElement();
                xWriter.WriteEndDocument();
                xWriter.Flush();

                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                axl.Append(sr.ReadToEnd());
                sr.Close();
                ms.Close();
                xWriter.Close();

                return axl.ToString();
            }
        }

        private void WriteClassMeta(XmlWriter xWriter, IClass cls)
        {
            if (xWriter == null || cls == null)
            {
                return;
            }

            xWriter.WriteStartElement("gv_meta");
            xWriter.WriteStartElement("class");

            Type type = cls.GetType();
            xWriter.WriteAttributeString("type", type.FullName);
            foreach (Type iType in type.GetInterfaces())
            {
                xWriter.WriteStartElement("implements");
                xWriter.WriteAttributeString("type", iType.FullName);
                xWriter.WriteEndElement(); // implements
            }
            xWriter.WriteEndElement(); // class
            xWriter.WriteEndElement(); // gv_meta
        }

        private void WriteNetworkTracers(XmlWriter xWriter)
        {
            if (xWriter == null)
            {
                return;
            }

            PlugInManager pluginMan = new PlugInManager();
            xWriter.WriteStartElement("gv_network_tracers");

            foreach (Type tracerType in pluginMan.GetPlugins(typeof(INetworkTracer)))
            {
                INetworkTracer tracer = pluginMan.CreateInstance(tracerType) as INetworkTracer;
                if (tracer == null)
                {
                    continue;
                }

                xWriter.WriteStartElement("gv_network_tracer");
                xWriter.WriteAttributeString("name", tracer.Name);
                xWriter.WriteAttributeString("guid", PlugInManager.PlugInID(tracer).ToString());
                xWriter.WriteEndElement(); // gv_network_tracer
            }

            xWriter.WriteEndElement(); // gv_network_tracers
        }

        public string Renderer(IFeatureLayer layer)
        {
            if (layer == null)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            string fRenderer = ObjectFromAXLFactory.ConvertToAXL(layer.FeatureRenderer);
            string lRenderer = ObjectFromAXLFactory.ConvertToAXL(layer.LabelRenderer);

            if (!lRenderer.Equals(String.Empty)) // LabelRenderer immer in GroupRenderer stecken, auch wenn fRenderer NULL ist!!!
            {                                    // So wirds vom WebGIS erwartet...
                sb.Append("<GROUPRENDERER>\n");
                sb.Append(fRenderer);
                sb.Append(lRenderer);
                sb.Append("</GROUPRENDERER>\n");
            }
            else if (!fRenderer.Equals(String.Empty))
            {
                sb.Append(fRenderer);
            }
            else if (layer.Class is ArcIMSThemeFeatureClass)
            {
                ArcIMSThemeFeatureClass c = layer.Class as ArcIMSThemeFeatureClass;
                if (c.OriginalRendererNode != null)
                {
                    return c.OriginalRendererNode.OuterXml;
                }
            }
            return sb.ToString();
        }

        public string IMAGE(string outputPath, string outputUrl, string Title, IEnvelope envelope)
        {
            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement(_rootElement);
            xWriter.WriteAttributeString("version", _version);

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("IMAGE");
            ENVELOPE(xWriter, envelope);
            xWriter.WriteStartElement("OUTPUT");
            xWriter.WriteAttributeString("file", outputPath + @"/" + Title);
            xWriter.WriteAttributeString("url", outputUrl + "/" + Title);
            xWriter.WriteEndElement();  // OUTPUT

            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        public string LEGEND(string outputPath, string outputUrl, string Title, IEnvelope envelope)
        {
            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement(_rootElement);
            xWriter.WriteAttributeString("version", _version);

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("IMAGE");
            ENVELOPE(xWriter, envelope);
            xWriter.WriteStartElement("LEGEND");
            xWriter.WriteAttributeString("file", outputPath + @"/" + Title);
            xWriter.WriteAttributeString("url", outputUrl + "/" + Title);
            xWriter.WriteEndElement();  // OUTPUT

            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        static public void ENVELOPE(XmlTextWriter xWriter, IEnvelope envelope)
        {
            ENVELOPE(xWriter, envelope, String.Empty);
        }
        static public void ENVELOPE(XmlTextWriter xWriter, IEnvelope envelope, string name)
        {
            if (xWriter == null || envelope == null)
            {
                return;
            }

            xWriter.WriteStartElement("ENVELOPE");
            xWriter.WriteAttributeString("minx", envelope.minx.ToString());
            xWriter.WriteAttributeString("miny", envelope.miny.ToString());
            xWriter.WriteAttributeString("maxx", envelope.maxx.ToString());
            xWriter.WriteAttributeString("maxy", envelope.maxy.ToString());
            if (name != String.Empty)
            {
                xWriter.WriteAttributeString("name", name);
            }

            xWriter.WriteEndElement();
        }

        public string ErrorMessage(string message)
        {
            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement(_rootElement);
            xWriter.WriteAttributeString("version", _version);

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("ERROR");
            xWriter.WriteRaw(message);
            xWriter.WriteEndElement(); // ERROR;
            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }
    }
}
