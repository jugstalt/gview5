using gView.Framework.Carto;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Framework.Data
{
    public class MapServerHelper
    {
        private const string MergedObjectIDName = "_GlobalOID";

        public static List<Layers> MapLayers(IServiceMap map, bool useTOC)
        {
            if (useTOC)
            {
                return MapLayersFromTOC(map);
            }

            List<Layers> layersList = new List<Layers>();
            foreach (IDatasetElement element in map.MapElements)
            {
                if (!(element is ILayer))
                {
                    continue;
                }

                ILayer layer = element as ILayer;

                if (layer.Class is IWebServiceClass)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceClass)layer.Class).Themes)
                    {
                        if (!(theme is ILayer))
                        {
                            continue;
                        }

                        string title = layer.Title;
                        string group = String.Empty;
                        if (map.TOC != null)
                        {
                            //ITOCElement tocElement = map.TOC.GetTOCElement(theme.Class);
                            // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                            // verwendet werden kann zB bei gesplitteten Layern...
                            ITocElement tocElement = map.TOC.GetTOCElement((ILayer)theme);
                            if (tocElement != null)
                            {
                                title = tocElement.Name;
                                group = ParentGroupName(tocElement);
                            }
                        }
                        if (!String.IsNullOrEmpty(layer.Namespace))
                        {
                            title = layer.Namespace + ":" + title;
                        }

                        layersList.Add(new Layers(theme));
                        layersList[layersList.Count - 1].Title = title;
                        layersList[layersList.Count - 1].GroupName = group;
                    }
                }
                else
                {
                    string title = layer.Title;
                    string group = String.Empty;
                    if (map.TOC != null)
                    {
                        //ITOCElement tocElement = map.TOC.GetTOCElement(layer.Class);
                        // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                        // verwendet werden kann zB bei gesplitteten Layern...
                        ITocElement tocElement = map.TOC.GetTOCElement(layer);
                        if (tocElement != null)
                        {
                            title = tocElement.Name;
                            group = ParentGroupName(tocElement);
                        }
                    }
                    if (!String.IsNullOrEmpty(layer.Namespace))
                    {
                        title = layer.Namespace + ":" + title;
                    }

                    layersList.Add(new Layers(layer));
                    layersList[layersList.Count - 1].Title = title;
                    layersList[layersList.Count - 1].GroupName = group;
                }
            }
            return layersList;
        }

        private static List<Layers> MapLayersFromTOC(IServiceMap map)
        {
            List<Layers> layersList = new List<Layers>();
            if (map.TOC == null)
            {
                return layersList;
            }

            IToc toc = map.TOC;
            foreach (ITocElement tocElement in toc.Elements)
            {
                if (tocElement == null ||
                    tocElement.ElementType != TocElementType.Layer ||
                    tocElement.Layers.Count == 0
                    )
                {
                    continue;
                }

                Layers layers = new Layers();
                layers.Title = tocElement.Name;
                layers.TocElement = tocElement;
                layers.GroupName = ParentGroupName(tocElement);
                foreach (ILayer layer in tocElement.Layers)
                {
                    if (layer == null)
                    {
                        continue;
                    }

                    layers.Add(layer);
                }
                if (layers.Count > 0)
                {
                    layersList.Add(layers);
                }
            }

            return layersList;
        }

        private static string ParentGroupName(ITocElement element)
        {
            if (element == null || element.ParentGroup == null)
            {
                return String.Empty;
            }

            string group = String.Empty;
            while ((element = element.ParentGroup) != null)
            {
                group = element.Name + ((group != "") ? "/" + group : "");
            }
            return group;
        }

        public static Layers FindMapLayers(IServiceMap map, bool useTOC, string id, List<ILayer> clonedLayers)
        {
            if (useTOC)
            {
                return FindMapLayersFromTOC(map, id, clonedLayers);
            }

            foreach (IDatasetElement element in map.MapElements)
            {
                if (!(element is ILayer))
                {
                    continue;
                }

                ILayer layer = element as ILayer;

                if (layer.SID == id)
                {
                    return new Layers(GetLayerByID(clonedLayers, layer.ID));
                }
                if (element.Class is IWebServiceClass)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceClass)element.Class).Themes)
                    {
                        if (!(theme is ILayer))
                        {
                            continue;
                        }

                        if (theme.SID == id)
                        {
                            return new Layers(GetLayerByID(clonedLayers, theme.ID));
                        }
                    }
                }
            }
            return new Layers();
        }
        private static Layers FindMapLayersFromTOC(IServiceMap map, string id, List<ILayer> clonedLayers)
        {
            if (map.TOC == null)
            {
                return new Layers();
            }

            IToc toc = map.TOC;
            foreach (ITocElement tocElement in toc.Elements)
            {
                if (tocElement == null ||
                    tocElement.ElementType != TocElementType.Layer ||
                    tocElement.Layers.Count == 0
                    )
                {
                    continue;
                }

                foreach (ILayer layer in tocElement.Layers)
                {
                    if (layer.SID == id)
                    {
                        Layers layers = new Layers();
                        foreach (ILayer origLayer in tocElement.Layers)
                        {
                            layers.Add(GetLayerByID(clonedLayers, origLayer.ID));
                        }
                        layers.Title = tocElement.Name;
                        layers.TocElement = tocElement;
                        layers.GroupName = ParentGroupName(tocElement);
                        return layers;
                    }
                }
            }

            return new Layers();
        }

        public static Layers FindMapLayers(IServiceMap map, bool useTOC, string id)
        {
            if (useTOC)
            {
                return FindMapLayersFromTOC(map, id);
            }

            foreach (IDatasetElement element in map.MapElements)
            {
                if (!(element is ILayer))
                {
                    continue;
                }

                ILayer layer = element as ILayer;

                if (layer.SID == id)
                {
                    return new Layers(layer);
                }

                if (element.Class is IWebServiceClass)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceClass)element.Class).Themes)
                    {
                        if (!(theme is ILayer))
                        {
                            continue;
                        }

                        if (theme.SID == id)
                        {
                            return new Layers(theme);
                        }
                    }
                }
            }
            return new Layers();
        }
        private static Layers FindMapLayersFromTOC(IServiceMap map, string id)
        {
            if (map.TOC == null)
            {
                return new Layers();
            }

            IToc toc = map.TOC;
            foreach (ITocElement tocElement in toc.Elements)
            {
                if (tocElement == null ||
                    tocElement.ElementType != TocElementType.Layer ||
                    tocElement.Layers.Count == 0
                    )
                {
                    continue;
                }

                foreach (ILayer layer in tocElement.Layers)
                {
                    if (layer.SID == id)
                    {
                        Layers layers = new Layers(tocElement.Layers);

                        layers.Title = tocElement.Name;
                        layers.TocElement = tocElement;
                        return layers;
                    }
                }
            }

            return new Layers();
        }

        private static ILayer GetLayerByID(List<ILayer> clonedLayers, int id)
        {
            foreach (ILayer layer in clonedLayers)
            {
                if (layer == null)
                {
                    continue;
                }

                if (layer.ID == id)
                {
                    return layer;
                }
            }
            return null;
        }

        public static int GetClassID(IServiceMap map, IClass Class)
        {
            List<IDatasetElement> elements = map.MapElements;
            foreach (IDatasetElement element in map.MapElements)
            {
                if (element != null && element.Class == Class)
                {
                    return element.ID;
                }

                if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceLayer)element).WebServiceClass.Themes)
                    {
                        if (theme != null && theme.Class == Class)
                        {
                            return theme.ID;
                        }
                    }
                }
            }

            return -1;
        }

        async public static Task<bool> ModifyFilter(IServiceMap map, ITableClass tClass, IQueryFilter filter)
        {
            if (filter == null || tClass == null)
            {
                return false;
            }

            string subFields = filter.SubFields;

            if (subFields != "*")
            {
                filter.SubFields = String.Empty;
                foreach (string subField in subFields.Split(' '))
                {
                    string fname = subField;
                    if (subFields.StartsWith(filter.fieldPrefix) &&
                        subFields.EndsWith(filter.fieldPostfix))
                    {
                        fname = fname.Substring(filter.fieldPrefix.Length, fname.Length - filter.fieldPrefix.Length - filter.fieldPostfix.Length);
                    }

                    if (tClass.Fields != null)
                    {
                        if (tClass.FindField(fname) != null ||
                            (tClass is IFeatureClass && ((IFeatureClass)tClass).ShapeFieldName == fname))
                        {
                            filter.AddField(fname);
                        }
                    }
                }
            }

            try
            {
                XmlDocument xmlFilter = new XmlDocument();
                string xml = await Xml.ToXml(tClass, filter);
                xmlFilter.LoadXml(xml);

                XmlNodeList propertyNames = xmlFilter.SelectNodes("//PropertyName");

                foreach (XmlNode propertyName in propertyNames)
                {
                    if (propertyName.InnerText == MergedObjectIDName)
                    {
                        XmlNode literal = propertyName.ParentNode.SelectSingleNode("Literal");
                        if (literal == null)
                        {
                            return false;
                        }

                        long mergedID;
                        if (!long.TryParse(literal.InnerText, out mergedID))
                        {
                            return false;
                        }

                        int classID = GetClassID(map, tClass), propertyClassID = GetClassID(mergedID);

                        if (classID != propertyClassID)
                        {
                            propertyName.ParentNode.ParentNode.RemoveChild(propertyName.ParentNode);
                            continue;
                        }
                        propertyName.InnerText = tClass.IDFieldName;
                        literal.InnerText = GetObjectID(mergedID).ToString();
                    }
                    else if (tClass.Fields.FindField(propertyName.InnerText) == null)
                    {
                        propertyName.ParentNode.ParentNode.RemoveChild(
                            propertyName.ParentNode);
                    }
                }

                // keine Properties übgrig geblieben sind -> nicht abfraten...
                if (propertyNames.Count > 0 && xmlFilter.SelectNodes("//PropertyName").Count == 0)
                {
                    return false;
                }
                IQueryFilter f = Xml.FromXml(tClass, xmlFilter.SelectSingleNode("Filter"));

                // Prüfen, ob vom Filter noch was übrig bleibt...
                if (!String.IsNullOrEmpty(filter.WhereClause) &&
                    String.IsNullOrEmpty(f.WhereClause))
                {
                    return false;
                }

                filter.WhereClause = f.WhereClause;
            }
            catch { return false; }
            return true;
        }


        public static IFeatureClass GetProtoFeatureClass(Layers layers)
        {
            if (layers == null)
            {
                return null;
            }

            if (layers.Count == 1 && layers[0].Class is IFeatureClass)
            {
                return layers[0].Class as IFeatureClass;
            }

            List<IFeatureClass> fcs = new List<IFeatureClass>();
            foreach (ILayer layer in layers)
            {
                if (layer.Class is IFeatureClass)
                {
                    fcs.Add((IFeatureClass)layer.Class);
                }
            }

            if (fcs.Count == 0)
            {
                return null;
            }

            if (fcs.Count == 1)
            {
                return fcs[0];
            }

            FeatureClass featureClass = new FeatureClass();
            featureClass.Name = layers.ID;
            // Geometry, SpatialRef, aus erstem übernehmen
            featureClass.GeometryType = fcs[0].GeometryType;
            featureClass.HasZ = fcs[0].HasM;
            featureClass.HasM = fcs[0].HasZ;
            featureClass.SpatialReference = fcs[0].SpatialReference;

            foreach (IFeatureClass fc in fcs)
            {
                //
                // Merged ID einfügen:
                // DataType: long
                // setzt sich aus Featureclass ID (oberen 32 bit)
                // und Object ID (untere 32 bit) zusammen
                //
                ((FieldCollection)featureClass.Fields).Add(new Field(MergedObjectIDName, FieldType.ID));

                // Fields
                foreach (IField field in fc.Fields.ToEnumerable())
                {
                    if (featureClass.FindField(field.name) != null)
                    {
                        continue;
                    }

                    Field f = new Field(field);

                    //
                    // Wenn eine Featureclasse aus mehreren
                    // zusammengesetzt wird, hat dieses automatisch keine ID mehr,
                    // da IDs ja in den einzelnen Klassen doppelt vorkommen können...
                    // Es wird daher eine Merged ID eingeführt
                    //
                    if (f.type == FieldType.ID)
                    {
                        f.type = FieldType.integer;
                    }

                    if (f.type == FieldType.Shape && featureClass.ShapeFieldName == String.Empty)
                    {
                        featureClass.ShapeFieldName = fc.ShapeFieldName;
                    } ((FieldCollection)featureClass.Fields).Add(f);
                }
                // Envelope
                if (fc.Envelope != null)
                {
                    if (featureClass.Envelope == null)
                    {
                        featureClass.Envelope = new Envelope(fc.Envelope);
                    }
                    else
                    {
                        featureClass.Envelope.Union(fc.Envelope);
                    }
                }
            }

            return featureClass;
        }
        public static long CalculateMergedID(int FeatureClassID, int ObjectID)
        {
            return ObjectID + ((long)FeatureClassID << 32);
        }
        public static int GetObjectID(long mergedID)
        {
            return (int)((mergedID & 0x7fffffff));
        }
        public static int GetClassID(long mergedID)
        {
            return (int)((mergedID & 0x7fffffff00000000) >> 32);
        }
        public static void AppendMergedID(int FeatureClassID, IRow row)
        {
            if (row == null || row.Fields == null)
            {
                return;
            }

            row.Fields.Add(new FieldValue(MergedObjectIDName,
                CalculateMergedID(FeatureClassID, row.OID)));
        }

        public static List<ILayer> FindAdditionalWebServiceLayers(IWebServiceClass wsClass, List<ILayer> layers)
        {
            if (wsClass == null || layers == null)
            {
                return layers;
            }

            List<ILayer> clonedLayers = ListOperations<ILayer>.Clone(layers);
            foreach (IWebServiceTheme theme in wsClass.Themes)
            {
                clonedLayers.Remove(theme);
            }

            return clonedLayers;
        }
        public static IWebServiceClass CloneNonVisibleWebServiceClass(IWebServiceClass wsClass)
        {
            if (wsClass == null)
            {
                return null;
            }

            IWebServiceClass clone = wsClass.Clone() as IWebServiceClass;
            if (clone == null)
            {
                return null;
            }

            foreach (IWebServiceTheme theme in clone.Themes)
            {
                if (theme == null)
                {
                    continue;
                }

                theme.Visible = false;
            }
            return clone;
        }
        public static void CopyWebThemeProperties(IWebServiceClass wsClass, ILayer master)
        {
            if (wsClass == null || master == null)
            {
                return;
            }

            foreach (IWebServiceTheme theme in wsClass.Themes)
            {
                if (theme == null)
                {
                    continue;
                }

                if (theme.Title == master.Title)
                {
                    if (master is IFeatureLayer)
                    {
                        theme.FilterQuery = ((IFeatureLayer)master).FilterQuery;
                        theme.FeatureRenderer = ((IFeatureLayer)master).FeatureRenderer;
                        theme.SelectionRenderer = ((IFeatureLayer)master).SelectionRenderer;
                        theme.LabelRenderer = ((IFeatureLayer)master).LabelRenderer;
                    }
                    theme.Visible = master.Visible;
                    break;
                }
            }
        }
        public static bool HasVisibleThemes(IWebServiceClass wsClass)
        {
            if (wsClass == null)
            {
                return false;
            }

            foreach (IWebServiceTheme theme in wsClass.Themes)
            {
                if (theme == null)
                {
                    continue;
                }

                if (theme.Visible)
                {
                    return true;
                }
            }
            return false;
        }
        #region HelperClass
        public class Layers : List<ILayer>
        {
            private string _title = String.Empty, _group = String.Empty;
            private ITocElement _tocElement = null;

            public Layers()
                : base()
            {
            }
            public Layers(ILayer layer)
                : base()
            {
                if (layer == null)
                {
                    return;
                }

                base.Add(layer);
            }
            public Layers(List<ILayer> layers)
                : base()
            {
                if (layers == null)
                {
                    return;
                }

                foreach (ILayer layer in layers)
                {
                    if (layer == null)
                    {
                        continue;
                    }

                    base.Add(layer);
                }
            }

            public string Title
            {
                get { return _title; }
                internal set { _title = value; }
            }
            public string GroupName
            {
                get { return _group; }
                internal set { _group = value; }
            }
            public string ID
            {
                get
                {
                    if (this.FirstLayer == null)
                    {
                        return "-1";
                    }
                    //return this.FirstLayer.ID.ToString();
                    return this.FirstLayer.SID;
                }
            }
            public ILayer FirstLayer
            {
                get
                {
                    if (this.Count == 0)
                    {
                        return null;
                    }

                    return this[0];
                }
            }
            public IClass FirstClass
            {
                get
                {
                    foreach (ILayer layer in this)
                    {
                        if (layer.Class != null)
                        {
                            return layer.Class;
                        }
                    }
                    return null;
                }
            }
            public new void Add(ILayer layer)
            {
                if (layer == null)
                {
                    return;
                }

                base.Add(layer);
            }

            public ITocElement TocElement
            {
                get { return _tocElement; }
                internal set { _tocElement = value; }
            }

            public double MinScale
            {
                get
                {
                    if (this.Count == 0)
                    {
                        return 0.0;
                    }

                    double minScale = this[0].MinimumScale;
                    if (minScale == 0.0)
                    {
                        return 0.0;
                    }

                    for (int i = 1; i < this.Count; i++)
                    {
                        if (this[i].MinimumScale == 0.0)
                        {
                            return 0.0;
                        }

                        minScale = Math.Min(minScale, this[i].MinimumScale);
                    }
                    return minScale;
                }
            }

            public double MaxScale
            {
                get
                {
                    if (this.Count == 0)
                    {
                        return 0.0;
                    }

                    double maxScale = this[0].MaximumScale;
                    if (maxScale == 0.0)
                    {
                        for (int i = 1; i < this.Count; i++)
                        {
                            if (this[i].MinimumScale == 0.0)
                            {
                                return 0.0;
                            }

                            maxScale = Math.Max(maxScale, this[i].MinimumScale);
                        }
                    }

                    return maxScale;
                }
            }
        }
        #endregion
    }
}
