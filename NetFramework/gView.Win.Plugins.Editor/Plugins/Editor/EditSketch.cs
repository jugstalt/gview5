using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace gView.Plugins.Editor
{
    class EditSketch : IGraphicElement2, IGraphicsElementDesigning
    {
        private IGeometry _geometry = null;
        private ISymbol _symbol = null;
        private int _actPartNr = 0;

        public EditSketch(IGeometry geometry)
        {
            _geometry = geometry; //geometry.Clone() as IGeometry;

            if (geometry is IPoint ||
                geometry is IMultiPoint)
            {
                _symbol = new SimplePointSymbol();
                ((SimplePointSymbol)_symbol).Size = 14;
                ((SimplePointSymbol)_symbol).Marker = SimplePointSymbol.MarkerType.Star;
                ((SimplePointSymbol)_symbol).Color = ArgbColor.Yellow;
            }
            else if (geometry is IPolyline)
            {
                _symbol = new SimpleLineSymbol();
                ((SimpleLineSymbol)_symbol).Color = ArgbColor.Yellow;
                ((SimpleLineSymbol)_symbol).Width = 2.0f;
                ((SimpleLineSymbol)_symbol).PenDashStyle = LineDashStyle.Solid;
            }
            else if (geometry is IPolygon)
            {
                SimpleLineSymbol outlineSymbol = new SimpleLineSymbol();
                outlineSymbol.Color = ArgbColor.Orange;
                outlineSymbol.Width = 2.0f;
                outlineSymbol.PenDashStyle = LineDashStyle.Solid;
                _symbol = new SimpleFillSymbol();
                ((SimpleFillSymbol)_symbol).Color = ArgbColor.FromArgb(125, ArgbColor.Yellow);
                ((SimpleFillSymbol)_symbol).OutlineSymbol = outlineSymbol;
            }
        }

        public void AddPoint(IPoint point)
        {
            if (point == null)
            {
                return;
            }

            if (_geometry is IPoint && _actPartNr == 0)
            {
                ((IPoint)_geometry).X = point.X;
                ((IPoint)_geometry).Y = point.Y;
                ((IPoint)_geometry).Z = point.Z;
            }
            else if (_geometry is IMultiPoint)
            {
                IMultiPoint mPoint = (IMultiPoint)_geometry;
                mPoint.AddPoint(point);
            }
            else if (_geometry is IPolyline)
            {
                IPolyline pLine = (IPolyline)_geometry;
                if (_actPartNr >= pLine.PathCount)
                {
                    gView.Framework.Geometry.Path path = new gView.Framework.Geometry.Path();
                    path.AddPoint(point);
                    pLine.AddPath(path);
                    _actPartNr = pLine.PathCount - 1;
                }
                else
                {
                    pLine[_actPartNr].AddPoint(point);
                }
            }
            else if (_geometry is IPolygon)
            {
                IPolygon poly = (IPolygon)_geometry;
                if (_actPartNr >= poly.RingCount)
                {
                    Ring ring = new Ring();
                    ring.AddPoint(point);
                    poly.AddRing(ring);
                    _actPartNr = poly.RingCount - 1;
                }
                else
                {
                    poly[_actPartNr].AddPoint(point);
                }
            }
        }

        public GeometryType GeometryType
        {
            get
            {
                if (_geometry is IPoint)
                {
                    return GeometryType.Point;
                }
                else if (_geometry is IMultiPoint)
                {
                    return GeometryType.Multipoint;
                }
                else if (_geometry is IPolyline)
                {
                    return GeometryType.Polyline;
                }
                else if (_geometry is IPolygon)
                {
                    return GeometryType.Polygon;
                }

                return GeometryType.Unknown;
            }
        }

        public IPointCollection Part
        {
            get
            {
                if (_geometry is Polyline)
                {
                    IPolyline pLine = (IPolyline)_geometry;
                    if (pLine.PathCount > _actPartNr)
                    {
                        return pLine[_actPartNr];
                    }
                }
                else if (_geometry is IPolygon)
                {
                    IPolygon poly = (IPolygon)_geometry;
                    if (poly.RingCount > _actPartNr)
                    {
                        return poly[_actPartNr];
                    }
                }
                return null;
            }
        }

        public void ClosePart()
        {
            if (Part != null && Part.PointCount > 2)
            {
                Part.AddPoint(Part[0]);
            }
        }

        public int PartCount
        {
            get
            {
                if (_geometry is Polyline)
                {
                    IPolyline pLine = (IPolyline)_geometry;
                    return pLine.PathCount;
                }
                else if (_geometry is IPolygon)
                {
                    IPolygon poly = (IPolygon)_geometry;
                    return poly.RingCount;
                }
                return 1;
            }
        }

        public int ActivePartNumber
        {
            get { return _actPartNr; }
            set
            {
                if (value < 0)
                {
                    return;
                }

                if (_geometry is Polyline)
                {
                    IPolyline pLine = (IPolyline)_geometry;
                    if (pLine.PathCount >= value)
                    {
                        _actPartNr = value;
                    }
                }
                else if (_geometry is IPolygon)
                {
                    IPolygon poly = (IPolygon)_geometry;
                    if (poly.RingCount >= value)
                    {
                        _actPartNr = value;
                    }
                }
            }
        }

        #region IGraphicElement2 Member

        public string Name
        {
            get { return "Editor.Sketch"; }
        }

        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        public gView.Framework.Symbology.ISymbol Symbol
        {
            get
            {
                return _symbol;
            }
            set
            {
                _symbol = value;
            }
        }

        public void DrawGrabbers(IDisplay display)
        {
            if (_geometry == null || display == null)
            {
                return;
            }

            IPointCollection pColl = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPoints(_geometry, false);
            if (pColl == null || pColl.PointCount == 0)
            {
                return;
            }

            SimplePointSymbol pointSymbol = new SimplePointSymbol();
            pointSymbol.Color = ArgbColor.White;
            pointSymbol.OutlineColor = ArgbColor.Black;
            pointSymbol.OutlineWidth = 1;
            pointSymbol.Marker = SimplePointSymbol.MarkerType.Square;
            pointSymbol.Size = 5;

            for (int i = 0; i < pColl.PointCount; i++)
            {
                IPoint point = pColl[i];
                if (point == null)
                {
                    continue;
                }

                display.Draw(pointSymbol, point);
            }
        }

        public gView.Framework.Geometry.IGeometry Geometry
        {
            get { return _geometry; }
        }

        #endregion

        #region IGraphicElement Member

        public void Draw(IDisplay display)
        {
            if (display != null && _symbol != null && _geometry != null)
            {
                display.Draw(_symbol, _geometry);
            }
        }

        #endregion

        #region IGraphicsElementScaling Member

        public void Scale(double scaleX, double scaleY)
        {

        }

        public void ScaleX(double scale)
        {

        }

        public void ScaleY(double scale)
        {

        }

        #endregion

        #region IGraphicsElementRotation Member

        public double Rotation
        {
            get
            {
                return 0.0;
            }
            set
            {

            }
        }

        #endregion

        #region IIGraphicsElementTranslation Member

        public void Translation(double x, double y)
        {
        }

        #endregion

        #region IGraphicsElementDesigning Member

        public IGraphicElement2 Ghost
        {
            get { return null; }
        }

        public IHitPositions HitTest(IDisplay display, gView.Framework.Geometry.IPoint point)
        {
            if (_geometry == null)
            {
                return null;
            }

            IPointCollection pColl = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPoints(_geometry, true);
            if (pColl == null || pColl.PointCount == 0)
            {
                return null;
            }

            double tol = 5.0 * display.MapScale / (display.Dpi / 0.0254);  // [m]
            if (display.SpatialReference != null &&
                display.SpatialReference.SpatialParameters.IsGeographic)
            {
                tol = 180.0 * tol / Math.PI / 6370000.0;
            }

            for (int i = 0; i < pColl.PointCount; i++)
            {
                IPoint p = pColl[i];
                if (p == null)
                {
                    continue;
                }

                if (gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(p, point) < tol)
                {
                    return new HitPosition(HitPosition.VertexCursor, i);
                }
            }

            List<IPath> paths = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPaths(_geometry);
            if (paths == null || paths.Count == 0)
            {
                return null;
            }

            Polyline pLine = new Polyline(paths);

            if (gView.Framework.SpatialAlgorithms.Algorithm.IntersectBox(pLine, new Envelope(
                    point.X - tol / 2, point.Y - tol / 2,
                    point.X + tol / 2, point.Y + tol / 2)))
            {
                return new HitPosition(System.Windows.Forms.Cursors.Default, -1);
            }
            return null;
        }

        public void Design(IDisplay display, IHitPositions hit, double dx, double dy)
        {
            if (_geometry == null)
            {
                return;
            }

            IPointCollection pColl = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPoints(_geometry, false);
            if (pColl == null || pColl.PointCount == 0)
            {
                return;
            }

            if (hit.HitID >= 0 && hit.HitID < pColl.PointCount)
            {
                pColl[hit.HitID].X = dx;
                pColl[hit.HitID].Y = dy;
            }
        }

        public bool TrySelect(IDisplay display, gView.Framework.Geometry.IEnvelope envelope)
        {
            return false;
        }

        public bool TrySelect(IDisplay display, gView.Framework.Geometry.IPoint point)
        {
            return false;
        }

        public bool RemoveVertex(IDisplay display, int index)
        {
            Vertices(_geometry, index);

            // "leere" Parts entfernen!
            if (_geometry is Polyline)
            {
                IPolyline pLine = (IPolyline)_geometry;
                for (int i = 0; i < pLine.PathCount; i++)
                {
                    if (pLine[i] == null)
                    {
                        continue;
                    }

                    if (pLine[i].PointCount == 0)
                    {
                        pLine.RemovePath(i);
                        i = -1;
                    }
                }
            }
            else if (_geometry is IPolygon)
            {
                IPolygon poly = (IPolygon)_geometry;
                for (int i = 0; i < poly.RingCount; i++)
                {
                    if (poly[i] == null)
                    {
                        continue;
                    }

                    if (poly[i].PointCount == 0)
                    {
                        poly.RemoveRing(i);
                        i = -1;
                    }
                }
            }

            return true;
        }

        public bool AddVertex(IDisplay display, gView.Framework.Geometry.IPoint point)
        {
            double distance;
            IPoint snapped = gView.Framework.SpatialAlgorithms.Algorithm.NearestPointToPath(
                _geometry, point, out distance, true);
            return snapped != null;
        }

        #endregion

        #region Helper Classes
        internal class HitPosition : IHitPositions
        {
            private System.Windows.Forms.Cursor _cursor;
            private int _id;

            public HitPosition(System.Windows.Forms.Cursor cursor, int id)
            {
                _cursor = cursor;
                _id = id;
            }

            #region HitPositions Member

            public object Cursor
            {
                get { return _cursor; }
            }

            public int HitID
            {
                get { return _id; }
            }

            #endregion

            static private Cursor _vertexCursor = null;
            static internal Cursor VertexCursor
            {
                get
                {
                    if (_vertexCursor == null)
                    {
                        MemoryStream ms = new MemoryStream();
                        ms.Write(global::gView.Win.Plugins.Editor.Properties.Resources.Vertex, 0, global::gView.Win.Plugins.Editor.Properties.Resources.Vertex.Length);
                        ms.Position = 0;
                        _vertexCursor = new Cursor(ms);
                    }
                    return _vertexCursor;
                }
            }
        }
        #endregion

        #region Helper
        private IPointCollection Vertices(IGeometry geometry, int removeAt)
        {
            IPointCollection pColl = new PointCollection();
            Vertices(geometry as IGeometry, pColl, removeAt);
            return pColl;
        }
        private void Vertices(IGeometry geometry, IPointCollection pColl, int removeAt)
        {
            if (geometry == null || pColl == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                Vertices(geometry as IPoint, pColl, removeAt);
            }
            else if (geometry is IMultiPoint)
            {
                Vertices(geometry as IPointCollection, pColl, removeAt);
            }
            else if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    Vertices(((IPolyline)geometry)[i] as IPointCollection, pColl, removeAt);
                }
            }
            else if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    Vertices(((IPolygon)geometry)[i] as IPointCollection, pColl, removeAt);
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    Vertices(((IAggregateGeometry)geometry)[i] as IGeometry, pColl, removeAt);
                }
            }
        }
        private void Vertices(IPoint p, IPointCollection vertices, int removeAt)
        {
            if (p == null || vertices == null)
            {
                return;
            }

            vertices.AddPoint(p);
        }
        private void Vertices(IPointCollection pColl, IPointCollection vertices, int removeAt)
        {
            if (pColl == null || vertices == null)
            {
                return;
            }

            for (int i = 0; i < pColl.PointCount; i++)
            {
                if (vertices.PointCount == removeAt)
                {
                    pColl.RemovePoint(i);
                }

                vertices.AddPoint(pColl[i]);
            }
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is EditSketch))
            {
                return false;
            }

            if (this == obj)
            {
                return true;
            }

            EditSketch sketch = (EditSketch)obj;

            if (_geometry != null && !_geometry.Equals(sketch._geometry))
            {
                return false;
            }

            if (_geometry == null && sketch._geometry != null)
            {
                return false;
            }

            if (_symbol != null && sketch._symbol != null &&
                !_symbol.GetType().Equals(sketch._symbol.GetType()))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
