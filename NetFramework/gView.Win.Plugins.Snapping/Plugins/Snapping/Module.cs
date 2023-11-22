using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Snapping.Core;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Plugins.Snapping
{
    [gView.Framework.system.RegisterPlugIn(gView.Framework.Snapping.Core.Globals.ModuleGuidString, Obsolete = true)]
    public class Module : IMapApplicationModule, ISnapModule, IPersistable
    {
        private IMapDocument _doc = null;
        private Dictionary<IMap, List<ISnapSchema>> _schemas = null;

        #region IMapApplicationModule Member

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                _schemas = new Dictionary<IMap, List<ISnapSchema>>();

                foreach (IMap map in _doc.Maps)
                {
                    _schemas.Add(map, new List<ISnapSchema>());
                    map.LayerRemoved += new LayerRemovedEvent(map_LayerRemoved);
                    map.NewExtentRendered += new NewExtentRenderedEvent(map_NewExtentRendered);
                }

                if (_doc is IMapDocumentEvents)
                {
                    ((IMapDocumentEvents)_doc).MapAdded += new MapAddedEvent(_doc_MapAdded);
                    ((IMapDocumentEvents)_doc).MapDeleted += new MapDeletedEvent(_doc_MapDeleted);
                    ((IMapDocumentEvents)_doc).AfterSetFocusMap += new AfterSetFocusMapEvent(_doc_AfterSetFocusMap);
                }
            }
        }
        #endregion

        #region ISnapModule Member
        public List<ISnapSchema> this[IMap map]
        {
            get
            {
                if (_schemas == null)
                {
                    return null;
                }

                if (_schemas.ContainsKey(map))
                {
                    return _schemas[map];
                }

                return null;
            }
        }

        public ISnapSchema ActiveSnapSchema
        {
            get
            {
                SchemaCombo combo = this.SnapSchemaCombo;
                if (combo != null)
                {
                    return combo.ActiveSnapSchema;
                }

                return null;
            }
            set
            {
                SchemaCombo combo = this.SnapSchemaCombo;
                if (combo != null)
                {
                    combo.ActiveSnapSchema = value;
                }
            }
        }

        public void RefreshGUI()
        {
            SchemaCombo combo = this.SnapSchemaCombo;
            if (combo != null)
            {
                combo.RefreshGUI();
            }
        }

        public void Snap(ref double X, ref double Y)
        {
            ISnapSchema schema = this.ActiveSnapSchema;
            if (schema == null)
            {
                return;
            }

            foreach (ISnapLayer sLayer in schema)
            {
                List<IGeometry> geometries;
                if (!_snapGeometries.TryGetValue(sLayer, out geometries))
                {
                    continue;
                }

                if (sLayer.FeatureLayer == null || !sLayer.FeatureLayer.Visible)
                {
                    continue;
                }

                double dist = double.MaxValue;
                IPoint ret = null;
                foreach (IGeometry geometry in geometries)
                {
                    if (geometry == null)
                    {
                        continue;
                    }
                    //IEnvelope env = geometry.Envelope;
                    //if (env == null) continue;

                    //if (env.minx > X || env.maxx < X ||
                    //    env.miny > Y || env.maxy < Y) continue;

                    double distance = -1.0, x = X, y = Y;
                    bool found = false;
                    if (Bit.Has(sLayer.Methode, SnapMethode.Vertex) &&
                        SnapVertex(geometry, ref x, ref y, out distance))
                    {
                        found = true;
                    }
                    else if (Bit.Has(sLayer.Methode, SnapMethode.EndPoint) &&
                        SnapEndPoint(geometry, ref x, ref y, out distance))
                    {
                        found = true;
                    }
                    else if (Bit.Has(sLayer.Methode, SnapMethode.Edge) &&
                        SnapEdge(geometry, ref x, ref y, out distance))
                    {
                        found = true;
                    }

                    if (found && distance < dist)
                    {
                        dist = distance;
                        if (ret == null)
                        {
                            ret = new Point(x, y);
                        }
                        else
                        {
                            ret.X = x; ret.Y = y;
                        }
                    }
                }

                if (ret != null)
                {
                    X = ret.X;
                    Y = ret.Y;
                }
            }
        }

        public int SnapTolerance
        {
            get { return _pixTolerance; }
            set { _pixTolerance = value; }
        }
        #endregion

        internal IMapDocument MapDocument
        {
            get { return _doc; }
        }

        void _doc_MapDeleted(IMap map)
        {
            if (_schemas == null)
            {
                return;
            }

            if (_schemas.ContainsKey(map))
            {
                _schemas.Remove(map);
            }

            map.NewExtentRendered -= new NewExtentRenderedEvent(map_NewExtentRendered);
        }
        void _doc_MapAdded(IMap map)
        {
            if (_schemas == null || map == null)
            {
                return;
            }

            if (!_schemas.ContainsKey(map))
            {
                _schemas.Add(map, new List<ISnapSchema>());
            }

            map.LayerRemoved += new LayerRemovedEvent(map_LayerRemoved);
            map.NewExtentRendered += new NewExtentRenderedEvent(map_NewExtentRendered);
        }
        void _doc_AfterSetFocusMap(IMap map)
        {
            this.RefreshGUI();
        }

        void map_LayerRemoved(IMap sender, ILayer layer)
        {
            if (!(layer is IFeatureLayer))
            {
                return;
            }

            List<ISnapSchema> schemas = this[sender];
            if (schemas == null)
            {
                return;
            }

            foreach (ISnapSchema schema in schemas)
            {
                if (schema == null)
                {
                    continue;
                }

                schema.Remove(layer as IFeatureLayer);
            }
        }
        async void map_NewExtentRendered(IMap sender, IEnvelope extent)
        {
            if (_doc == null || _doc.FocusMap != sender)
            {
                return;
            }

            await LoadGeometry(sender, this.ActiveSnapSchema, extent);
        }

        private Dictionary<ISnapLayer, List<IGeometry>> _snapGeometries = new Dictionary<ISnapLayer, List<IGeometry>>();
        async internal Task LoadGeometry(IMap map, ISnapSchema schema, IEnvelope envelope)
        {
            _snapGeometries.Clear();

            IGUIApplication guiApp = (_doc != null) ? _doc.Application as IGUIApplication : null;

            if (map == null || schema == null || envelope == null ||
                map.Display == null || map.Display.MapScale >= schema.MaxScale)
            {
                return;
            }

            foreach (ISnapLayer sLayer in schema)
            {
                if (sLayer == null || sLayer.FeatureLayer == null ||
                    sLayer.FeatureLayer.FeatureClass == null ||
                    _doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null)
                {
                    continue;
                }

                if (sLayer.FeatureLayer.MinimumScale >= 1 && sLayer.FeatureLayer.MinimumScale > _doc.FocusMap.Display.MapScale)
                {
                    continue;
                }

                if (sLayer.FeatureLayer.MaximumScale >= 1 && sLayer.FeatureLayer.MaximumScale < _doc.FocusMap.Display.MapScale)
                {
                    continue;
                }

                SpatialFilter filter = new SpatialFilter();
                filter.FeatureSpatialReference = map.Display.SpatialReference;
                filter.FilterSpatialReference = map.Display.SpatialReference;
                filter.Geometry = envelope;
                filter.SpatialRelation = spatialRelation.SpatialRelationIntersects;
                filter.AddField(sLayer.FeatureLayer.FeatureClass.ShapeFieldName);

                if (guiApp != null)
                {
                    guiApp.StatusBar.Text = "Query Snaplayer: " + sLayer.FeatureLayer.Title;
                    guiApp.StatusBar.Refresh();
                }

                List<IGeometry> geometries = new List<IGeometry>();
                using (IFeatureCursor cursor = await sLayer.FeatureLayer.FeatureClass.GetFeatures(filter))
                {
                    IFeature feature;
                    while ((feature = await cursor.NextFeature()) != null)
                    {
                        if (feature.Shape != null)
                        {
                            geometries.Add(feature.Shape);
                        }
                    }
                }
                _snapGeometries.Add(sLayer, geometries);
            }
            if (guiApp != null)
            {
                guiApp.StatusBar.Text = null;
                guiApp.StatusBar.Refresh();
            }
        }

        private bool SnapVertex(IGeometry geometry, ref double X, ref double Y, out double distance)
        {
            distance = -1.0;

            IPoint p = new Point(X, Y), s = null;
            double dist = double.MaxValue, d;

            IPointCollection pColl = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPoints(geometry, false);
            int pointCount = pColl.PointCount;
            for (int i = 0; i < pointCount; i++)
            {
                d = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(p, pColl[i]);
                if (d < dist)
                {
                    dist = d;
                    s = pColl[i];
                }
            }

            if (dist < SnapDistance)
            {
                X = s.X;
                Y = s.Y;
                distance = dist;
                return true;
            }

            return false;
        }
        private bool SnapEndPoint(IGeometry geometry, ref double X, ref double Y, out double distance)
        {
            distance = -1.0;

            IPoint p = new Point(X, Y), s = null;
            double dist = double.MaxValue, d;

            IPointCollection pColl = gView.Framework.SpatialAlgorithms.Algorithm.StartEndPoints(geometry, false);
            int pointCount = pColl.PointCount;
            for (int i = 0; i < pointCount; i++)
            {
                d = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(p, pColl[i]);
                if (d < dist)
                {
                    dist = d;
                    s = pColl[i];
                }
            }

            if (dist < SnapDistance)
            {
                X = s.X;
                Y = s.Y;
                distance = dist;
                return true;
            }

            return false;
        }
        private bool SnapEdge(IGeometry geometry, ref double X, ref double Y, out double distance)
        {
            distance = -1.0;

            IPoint p = new Point(X, Y);
            double dist;

            IPoint c = gView.Framework.SpatialAlgorithms.Algorithm.NearestPointToPath(
                    geometry,
                    p,
                    out dist,
                    false);
            if (c != null && dist < SnapDistance)
            {
                distance = dist;
                X = c.X;
                Y = c.Y;
                return true;
            }
            return false;
        }

        private int _pixTolerance = 12;
        private double SnapDistance
        {
            get
            {
                if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null)
                {
                    return 0.0;
                }

                double tol = _pixTolerance * _doc.FocusMap.Display.MapScale / (96 / 0.0254);  // [m]
                if (_doc.FocusMap.Display.SpatialReference != null &&
                    _doc.FocusMap.Display.SpatialReference.SpatialParameters.IsGeographic)
                {
                    tol = (180.0 * tol / Math.PI) / 6370000.0;
                }
                return tol;
            }
        }

        #region Helper
        private SchemaCombo SnapSchemaCombo
        {
            get
            {
                if (_doc == null || !(_doc.Application is IGUIApplication))
                {
                    return null;
                }

                SchemaCombo combo = ((IGUIApplication)_doc.Application).Tool(new Guid("9CDE6BD1-317E-478b-8828-B169A6688CC5")) as SchemaCombo;
                return combo;
            }
        }
        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            if (_doc == null)
            {
                return;
            }

            _schemas.Clear();
            foreach (IMap map in _doc.Maps)
            {
                _schemas.Add(map, new List<ISnapSchema>());
            }

            while (true)
            {
                MapPersister mapper = stream.Load("Map", null, new MapPersister(this)) as MapPersister;
                if (mapper == null)
                {
                    break;
                }
            }

            RefreshGUI();
        }

        public void Save(IPersistStream stream)
        {
            foreach (IMap map in _schemas.Keys)
            {
                stream.Save("Map", new MapPersister(map, _schemas[map]));
            }
        }

        private class MapPersister : IPersistable
        {
            IMap _map = null;
            List<ISnapSchema> _schemas = null;
            Module _module = null;

            public MapPersister(IMap map, List<ISnapSchema> schemas)
            {
                _map = map;
                _schemas = schemas;
            }
            public MapPersister(Module module)
            {
                _module = module;
            }
            #region IPersistable Member

            public void Load(IPersistStream stream)
            {
                if (_module == null || _module.MapDocument == null || _module.MapDocument.Maps == null)
                {
                    return;
                }

                string mapName = (string)stream.Load("Name", "");
                foreach (IMap map in _module.MapDocument.Maps)
                {
                    if (map.Name == mapName)
                    {
                        List<ISnapSchema> schemas = _module[map];
                        schemas.Clear();

                        while (true)
                        {
                            ISnapSchema schema = stream.Load("SnapSchema", null, new SnapSchema(map)) as ISnapSchema;
                            if (schema == null)
                            {
                                break;
                            }

                            schemas.Add(schema);
                        }
                        break;
                    }
                }
            }

            public void Save(IPersistStream stream)
            {
                if (_map == null || _schemas == null)
                {
                    return;
                }

                stream.Save("Name", _map.Name);
                foreach (ISnapSchema schema in _schemas)
                {
                    stream.Save("SnapSchema", schema);
                }
            }

            #endregion
        }
        #endregion
    }

    internal class SnapLayer : ISnapLayer, IPersistable
    {
        private IFeatureLayer _layer;
        private SnapMethode _methode;

        public SnapLayer(IFeatureLayer layer, SnapMethode methode)
        {
            _layer = layer;
            _methode = methode;
        }
        internal SnapLayer(IMap map)
        {
            _map = map;
        }
        #region ISnapLayer Member

        public SnapMethode Methode
        {
            get { return _methode; }
            internal set { _methode = value; }
        }

        public IFeatureLayer FeatureLayer
        {
            get { return _layer; }
            internal set { _layer = value; }
        }

        #endregion

        #region IPersistable Member

        IMap _map = null;
        public void Load(IPersistStream stream)
        {
            if (_map == null)
            {
                return;
            }

            int layerID = (int)stream.Load("LayerID", -1);
            _methode = (SnapMethode)stream.Load("Method", SnapMethode.None);

            foreach (IDatasetElement element in _map.MapElements)
            {
                if (element is IFeatureLayer && element.ID == layerID)
                {
                    _layer = element as IFeatureLayer;
                    break;
                }
            }
            _map = null;
        }

        public void Save(IPersistStream stream)
        {
            if (_layer == null || _methode == SnapMethode.None)
            {
                return;
            }

            stream.Save("LayerID", _layer.ID);
            stream.Save("Method", (int)_methode);
        }

        #endregion
    }

    internal class SnapSchema : ISnapSchema, IPersistable
    {
        private List<ISnapLayer> _snapLayers = new List<ISnapLayer>();
        private string _name;
        private double _maxScale = 5000.0;

        public SnapSchema(string name)
        {
            _name = name;
        }
        public SnapSchema(IMap map)
        {
            _map = map;
        }

        #region ISnapSchema Member

        public string Name
        {
            get { return _name; }
            internal set { _name = value; }
        }

        public double MaxScale
        {
            get { return _maxScale; }
            internal set { _maxScale = value; }
        }

        public void Clear()
        {
            _snapLayers.Clear();
        }

        public void Add(ISnapLayer layer)
        {
            if (layer == null || layer.FeatureLayer == null ||
                hasFeatureLayer(layer.FeatureLayer))
            {
                return;
            }

            _snapLayers.Add(layer);
        }

        public void Remove(ISnapLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            Remove(layer.FeatureLayer);
        }

        public void Remove(IFeatureLayer layer)
        {
            foreach (ISnapLayer sLayer in ListOperations<ISnapLayer>.Clone(_snapLayers))
            {
                if (sLayer.FeatureLayer == layer)
                {
                    _snapLayers.Remove(sLayer);
                }
            }
        }

        #endregion

        #region IEnumerable<ISnapLayer> Member

        public IEnumerator<ISnapLayer> GetEnumerator()
        {
            return _snapLayers.GetEnumerator();
        }

        #endregion

        #region IEnumerable Member

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _snapLayers.GetEnumerator();
        }

        #endregion

        private bool hasFeatureLayer(IFeatureLayer layer)
        {
            if (layer == null)
            {
                return false;
            }

            foreach (ISnapLayer sLayer in _snapLayers)
            {
                if (sLayer.FeatureLayer == layer)
                {
                    return true;
                }
            }

            return false;
        }

        #region IPersistable Member
        IMap _map = null;
        public void Load(IPersistStream stream)
        {
            if (_map == null)
            {
                return;
            }

            _name = (string)stream.Load("Name");
            _maxScale = (double)stream.Load("MaxScale", 5000.0);
            while (true)
            {
                ISnapLayer sLayer = stream.Load("SnapLayer", null, new SnapLayer(_map)) as ISnapLayer;
                if (sLayer == null)
                {
                    break;
                }

                if (sLayer.FeatureLayer != null && sLayer.Methode != SnapMethode.None)
                {
                    _snapLayers.Add(sLayer);
                }
            }
            _map = null;
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("Name", _name);
            stream.Save("MaxScale", _maxScale);
            foreach (ISnapLayer sLayer in _snapLayers)
            {
                stream.Save("SnapLayer", sLayer);
            }
        }

        #endregion
    }
}
