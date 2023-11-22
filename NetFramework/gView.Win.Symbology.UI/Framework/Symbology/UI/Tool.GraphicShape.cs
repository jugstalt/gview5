using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;

namespace gView.Framework.Symbology.UI
{
    [Serializable]
    public abstract class GraphicShape : Cloner, IClone, IGraphicElement2, IGraphicsElementScaling, IGraphicsElementRotation, IIGraphicsElementTranslation, IGraphicsElementDesigning, IBrushColor, IPenColor, IFontColor, IPenWidth, IPenDashStyle, IFontSymbol, IPersistable, IToolMouseActions
    {
        private IGeometry _template = null;
        private IPoint _origin = new Point(0, 0), _rotationCenter = new Point(0.5, 0.5);
        private double _scaleX = 1.0, _scaleY = 1.0;
        private double _xOffset = 0.0, _yOffset = 0.0;
        private double _angle = 0.0; //30.0 * Math.PI / 180.0;
        private ISymbol _symbol = null;
        private IGraphicElement2 _ghost = null;
        private int _grabberMask = 1 + 2 + 4 + 8 + 16 + 32 + 64 + 128 + 256 + 512 + 1024 + 2048;

        #region IGraphicsElementScaling Member

        virtual public void Scale(double scaleX, double scaleY)
        {
            _scaleX = scaleX;
            _scaleY = scaleY;

            if (Ghost != null)
            {
                Ghost.Scale(scaleX, scaleY);
            }
        }

        virtual public void ScaleX(double scale)
        {
            _scaleX = scale;

            if (Ghost != null)
            {
                Ghost.ScaleX(scale);
            }
        }

        virtual public void ScaleY(double scale)
        {
            _scaleY = scale;

            if (Ghost != null)
            {
                Ghost.ScaleY(scale);
            }
        }

        #endregion

        #region IGraphicsElementRotation Member

        public double Rotation
        {
            get
            {
                return _angle * 180.0 / Math.PI;
            }
            set
            {
                _angle = value * Math.PI / 180.0;

                if (Ghost != null)
                {
                    Ghost.Rotation = _angle;
                }
            }
        }

        #endregion

        #region IIGraphicsElementTranslation Member

        public void Translation(double x, double y)
        {
            _xOffset = x;
            _yOffset = y;

            if (Ghost != null)
            {
                Ghost.Translation(x, y);
            }
        }

        #endregion

        protected IPoint Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }
        protected IGeometry Template
        {
            set
            {
                if (value == null)
                {
                    _template = null;
                    return;
                }

                if (value is IPoint)
                {
                    _template = value.Clone() as IGeometry;
                    TranslateGeometry(_template, -((IPoint)_template).X, -((IPoint)_template).Y);
                }
                else
                {
                    IEnvelope env = value.Envelope;
                    if (env.Width == 0 || env.Height == 0)
                    {
                        return;
                    }

                    _template = value.Clone() as IGeometry;
                    if (_template == null)
                    {
                        return;
                    }

                    TranslateGeometry(_template, -env.LowerLeft.X, -env.LowerLeft.Y);
                    ScaleGeometry(_template, 1.0 / env.Width, 1.0 / env.Height);
                }

                if (!(this is Ghost))
                {
                    if (_template is IPoint)
                    {
                        SimplePointSymbol pSymbol = new SimplePointSymbol();
                        pSymbol.Size = 10;
                        pSymbol.Color = GraphicsEngine.ArgbColor.FromArgb(150, 255, 255, 0);

                        _ghost = new Ghost(_template, pSymbol);
                    }
                    if (_template is IPolygon || _template is IPolyline)
                    {
                        SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
                        lineSymbol.DashStyle = GraphicsEngine.LineDashStyle.Dash;
                        lineSymbol.Color = GraphicsEngine.ArgbColor.Gray;

                        _ghost = new Ghost(_template, lineSymbol);
                    }
                }
            }
        }

        protected void AddGrabber(GrabberIDs id)
        {
            _grabberMask |= (int)Math.Pow(2, (int)id);
        }

        protected void RemoveGrabber(GrabberIDs id)
        {
            if (!UseGrabber(id))
            {
                return;
            }

            _grabberMask -= (int)Math.Pow(2, (int)id);
        }

        protected void RemoveAllGrabbers()
        {
            _grabberMask = 0;
        }

        private bool UseGrabber(GrabberIDs id)
        {
            return (_grabberMask & (int)Math.Pow(2, (int)id)) != 0;
        }

        #region Scaling
        private void ScaleGeometry(IGeometry geometry, double scaleX, double scaleY)
        {
            if (geometry == null || _origin == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                ScalePoint(geometry as IPoint, scaleX, scaleY);
            }
            else if (geometry is IPointCollection)
            {
                ScalePoints(geometry as IPointCollection, scaleX, scaleY);
            }
            else if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    ScalePoints(((IPolyline)geometry)[i] as IPointCollection, scaleX, scaleY);
                }
            }
            else if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    ScalePoints(((IPolygon)geometry)[i] as IPointCollection, scaleX, scaleY);
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    ScaleGeometry(((IAggregateGeometry)geometry)[i], scaleX, scaleY);
                }
            }
            else if (geometry is IEnvelope)
            {
                IEnvelope env = geometry as IEnvelope;
                IPoint lowerLeft = env.LowerLeft;
                IPoint upperRight = env.UpperRight;
                ScalePoint(lowerLeft, scaleX, scaleY);
                ScalePoint(upperRight, scaleX, scaleY);
                env.LowerLeft = lowerLeft;
                env.UpperRight = upperRight;
            }
        }

        private void ScalePoint(IPoint point, double scaleX, double scaleY)
        {
            if (point == null || _origin == null)
            {
                return;
            }

            point.X = (point.X - _origin.X) * scaleX + _origin.X;
            point.Y = (point.Y - _origin.Y) * scaleY + _origin.Y;
        }

        private void ScalePoints(IPointCollection points, double scaleX, double scaleY)
        {
            if (points == null || _origin == null)
            {
                return;
            }

            for (int i = 0; i < points.PointCount; i++)
            {
                ScalePoint(points[i], scaleX, scaleY);
            }
        }
        #endregion

        #region Rotation
        private void RotateGeometry(IGeometry geometry, double cosA, double sinA)
        {
            if (geometry == null || _origin == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                RotatePoint(geometry as IPoint, cosA, sinA);
            }
            else if (geometry is IPointCollection)
            {
                RotatePoints(geometry as IPointCollection, cosA, sinA);
            }
            else if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    RotatePoints(((IPolyline)geometry)[i] as IPointCollection, cosA, sinA);
                }
            }
            else if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    RotatePoints(((IPolygon)geometry)[i] as IPointCollection, cosA, sinA);
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    RotateGeometry(((IAggregateGeometry)geometry)[i], cosA, sinA);
                }
            }
            else if (geometry is IEnvelope)
            {
                IEnvelope env = geometry as IEnvelope;
                IPoint lowerLeft = env.LowerLeft;
                IPoint upperRight = env.UpperRight;
                RotatePoint(lowerLeft, cosA, sinA);
                RotatePoint(upperRight, cosA, sinA);
                env.LowerLeft = lowerLeft;
                env.UpperRight = upperRight;
            }
        }

        private void RotatePoint(IPoint point, double cosA, double sinA)
        {
            if (point == null || _rotationCenter == null || _angle == 0.0)
            {
                return;
            }

            Point rotCenter = new Point(_rotationCenter.X * _scaleX, _rotationCenter.Y * _scaleY);

            double x0 = point.X - rotCenter.X;
            double y0 = point.Y - rotCenter.Y;

            point.X = rotCenter.X + cosA * x0 + sinA * y0;
            point.Y = rotCenter.Y - sinA * x0 + cosA * y0;
        }

        private void RotatePoints(IPointCollection points, double cosA, double sinA)
        {
            if (points == null || _origin == null)
            {
                return;
            }

            for (int i = 0; i < points.PointCount; i++)
            {
                RotatePoint(points[i], cosA, sinA);
            }
        }
        #endregion

        #region Translation
        private void TranslateGeometry(IGeometry geometry, double X, double Y)
        {
            if (geometry == null || _origin == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                TranslatePoint(geometry as IPoint, X, Y);
            }
            else if (geometry is IPointCollection)
            {
                TranslatePoints(geometry as IPointCollection, X, Y);
            }
            else if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    TranslatePoints(((IPolyline)geometry)[i] as IPointCollection, X, Y);
                }
            }
            else if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    TranslatePoints(((IPolygon)geometry)[i] as IPointCollection, X, Y);
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    TranslateGeometry(((IAggregateGeometry)geometry)[i], X, Y);
                }
            }
            else if (geometry is IEnvelope)
            {
                IEnvelope env = geometry as IEnvelope;
                IPoint lowerLeft = env.LowerLeft;
                IPoint upperRight = env.UpperRight;
                TranslatePoint(lowerLeft, X, Y);
                TranslatePoint(upperRight, X, Y);
                env.LowerLeft = lowerLeft;
                env.UpperRight = upperRight;
            }
        }

        private void TranslatePoint(IPoint point, double X, double Y)
        {
            if (point == null)
            {
                return;
            }

            point.X += X;
            point.Y += Y;
        }

        private void TranslatePoints(IPointCollection points, double X, double Y)
        {
            if (points == null)
            {
                return;
            }

            for (int i = 0; i < points.PointCount; i++)
            {
                TranslatePoint(points[i], X, Y);
            }
        }
        #endregion

        #region IGraphicElement2 Member

        virtual public string Name
        {
            get { return "Rectangle"; }
        }

        virtual public System.Drawing.Image Icon
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
                if (_symbol == value)
                {
                    return;
                }

                if (_symbol != null)
                {
                    _symbol.Release();
                }

                _symbol = value;
            }
        }

        virtual public void DrawGrabbers(IDisplay display)
        {
            IMultiPoint grabbers = Grabbers(display, true);
            if (grabbers == null)
            {
                return;
            }

            var smode = display.Canvas.SmoothingMode;
            display.Canvas.SmoothingMode = GraphicsEngine.SmoothingMode.AntiAlias;

            SimplePointSymbol pointSymbol = new SimplePointSymbol();
            pointSymbol.Color = GraphicsEngine.ArgbColor.White;
            pointSymbol.OutlineColor = GraphicsEngine.ArgbColor.Black;
            pointSymbol.OutlineWidth = 1;
            pointSymbol.Size = 8;

            if (display.GraphicsContainer.EditMode == GrabberMode.Pointer)
            {
                Ring ring = new Ring();
                for (int i = 0; i < Math.Min(grabbers.PointCount, 4); i++)
                {
                    ring.AddPoint(grabbers[i]);
                }
                Polygon polygon = new Polygon();
                polygon.AddRing(ring);

                SimpleLineSymbol lSymbol = new SimpleLineSymbol();
                lSymbol.Color = GraphicsEngine.ArgbColor.Gray;
                lSymbol.DashStyle = GraphicsEngine.LineDashStyle.Dash;
                display.Draw(lSymbol, polygon);
                lSymbol.Release();

                for (int i = 0; i < grabbers.PointCount; i++)
                {
                    if (!UseGrabber((GrabberIDs)i))
                    {
                        continue;
                    }

                    if (i > 7 && pointSymbol.Color.Equals(GraphicsEngine.ArgbColor.White))
                    {
                        pointSymbol.Color = GraphicsEngine.ArgbColor.Yellow;
                        DrawRotationGrabberline(display);
                    }
                    display.Draw(pointSymbol, grabbers[i]);
                }
            }
            else if (display.GraphicsContainer.EditMode == GrabberMode.Vertex)
            {
                pointSymbol.Marker = SimplePointSymbol.MarkerType.Square;
                pointSymbol.Size = 5;

                for (int i = 0; i < grabbers.PointCount; i++)
                {
                    display.Draw(pointSymbol, grabbers[i]);
                }
            }

            display.Canvas.SmoothingMode = smode;
        }

        public IGeometry Geometry
        {
            get
            {
                return TransformGeometry();
            }
        }
        #endregion

        private void DrawRotationGrabberline(IDisplay display)
        {
            if (display == null || display.Canvas == null)
            {
                return;
            }

            Envelope env = new Envelope(0, 0, 1, 1);

            Polyline pLine = new Polyline();
            Path path = new Path();
            pLine.AddPath(path);

            double tol = 25.0 * display.MapScale / (96 / 0.0254);  // [m]
            if (display.SpatialReference != null &&
                display.SpatialReference.SpatialParameters.IsGeographic)
            {
                tol = 180.0 * tol / Math.PI / 6370000.0;
            }

            if (_scaleY > 0)
            {
                path.AddPoint(new Point(env.UpperLeft.X + env.Width / 2, env.UpperLeft.Y));
                path.AddPoint(new Point(env.UpperLeft.X + env.Width / 2, env.UpperLeft.Y + tol / _scaleY));
            }
            else
            {
                path.AddPoint(new Point(env.LowerLeft.X + env.Width / 2, env.LowerLeft.Y));
                path.AddPoint(new Point(env.LowerLeft.X + env.Width / 2, env.LowerLeft.Y + tol / _scaleY));
            }

            SimpleLineSymbol lSym = new SimpleLineSymbol();
            ScaleGeometry(pLine, _scaleX, _scaleY);
            RotateGeometry(pLine, Math.Cos(_angle), Math.Sin(_angle));
            TranslateGeometry(pLine, _xOffset, _yOffset);
            display.Draw(lSym, pLine);
            lSym.Release();
        }
        virtual protected IMultiPoint Grabbers(IDisplay display, bool transform)
        {
            MultiPoint coll = new MultiPoint();
            if (display == null || display.GraphicsContainer == null)
            {
                return coll;
            }

            if (display.GraphicsContainer.EditMode == GrabberMode.Pointer)
            {
                Envelope env = new Envelope(0, 0, 1, 1);

                coll.AddPoint(env.UpperLeft);
                coll.AddPoint(env.UpperRight);
                coll.AddPoint(env.LowerRight);
                coll.AddPoint(env.LowerLeft);

                coll.AddPoint(new Point(env.UpperLeft.X + env.Width / 2, env.UpperLeft.Y));
                coll.AddPoint(new Point(env.UpperRight.X, env.LowerRight.Y + env.Height / 2));
                coll.AddPoint(new Point(env.LowerLeft.X + env.Width / 2, env.LowerLeft.Y));
                coll.AddPoint(new Point(env.LowerLeft.X, env.LowerLeft.Y + env.Width / 2));

                double tol = 25.0 * display.MapScale / (96 / 0.0254);  // [m]
                if (display.SpatialReference != null &&
                display.SpatialReference.SpatialParameters.IsGeographic)
                {
                    tol = 180.0 * tol / Math.PI / 6370000.0;
                }

                if (_scaleY > 0)
                {
                    coll.AddPoint(new Point(env.UpperLeft.X + env.Width / 2, env.UpperLeft.Y + tol / _scaleY));
                }
                else
                {
                    coll.AddPoint(new Point(env.LowerLeft.X + env.Width / 2, env.LowerLeft.Y + tol / _scaleY));
                }
            }
            else if (display.GraphicsContainer.EditMode == GrabberMode.Vertex &&
                this is IConstructable && ((IConstructable)this).hasVertices)
            {
                IPointCollection pColl = Vertices(_template.Clone() as IGeometry, -1);
                if (pColl != null)
                {
                    for (int i = 0; i < pColl.PointCount; i++)
                    {
                        coll.AddPoint(pColl[i]);
                    }
                }
            }

            if (transform)
            {
                ScaleGeometry(coll, _scaleX, _scaleY);
                RotateGeometry(coll, Math.Cos(_angle), Math.Sin(_angle));
                TranslateGeometry(coll, _xOffset, _yOffset);
            }

            return coll;
        }

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

        protected IGeometry TransformGeometry()
        {
            IGeometry geometry = _template.Clone() as IGeometry;
            if (geometry == null)
            {
                return null;
            }

            ScaleGeometry(geometry, _scaleX, _scaleY);
            RotateGeometry(geometry, Math.Cos(_angle), Math.Sin(_angle));
            TranslateGeometry(geometry, _xOffset, _yOffset);

            return geometry;
        }

        private void TemplateFromTransformedGeomety(IGeometry geometry)
        {
            if (geometry == null)
            {
                return;
            }

            _template = geometry.Clone() as IGeometry;
            if (_template == null)
            {
                return;
            }

            TranslateGeometry(_template, -_xOffset, -_yOffset);
            RotateGeometry(_template, Math.Cos(-_angle), Math.Sin(-_angle));
            ScaleGeometry(_template, 1.0 / _scaleX, 1.0 / _scaleY);

            if (!(this is Ghost))
            {
                if (_template is IPoint)
                {
                    SimplePointSymbol pSymbol = new SimplePointSymbol();
                    pSymbol.Size = 10;
                    pSymbol.Color = GraphicsEngine.ArgbColor.FromArgb(150, 255, 255, 0);

                    _ghost = new Ghost(_template, pSymbol);
                }
                if (_template is IPolygon || _template is IPolyline)
                {
                    SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
                    lineSymbol.DashStyle = GraphicsEngine.LineDashStyle.Dash;
                    lineSymbol.Color = GraphicsEngine.ArgbColor.Gray;

                    _ghost = new Ghost(_template, lineSymbol);
                }
            }
        }

        #region IGraphicElement Member

        virtual public void Draw(IDisplay display)
        {
            if (_template == null || _symbol == null || display == null)
            {
                return;
            }

            IGeometry geometry = TransformGeometry();
            if (geometry == null)
            {
                return;
            }

            if (display.ReferenceScale > 1 && !(this is Ghost))
            {
                ISymbol sym = _symbol.Clone(new CloneOptions(display, false)) as ISymbol;
                if (sym != null)
                {
                    display.Draw(sym, geometry);
                    sym.Release();
                }
            }
            else
            {
                display.Draw(_symbol, geometry);
            }
        }

        #endregion

        #region IGraphicsElementDesigning Member

        virtual public IGraphicElement2 Ghost
        {
            get
            {
                return _ghost;
            }
        }

        virtual public IHitPositions HitTest(IDisplay display, IPoint point)
        {
            IMultiPoint coll = Grabbers(display, true);
            if (coll == null)
            {
                return null;
            }

            double tol = 5.0 * display.MapScale / (display.Dpi / 0.0254);  // [m]
            if (display.SpatialReference != null &&
                display.SpatialReference.SpatialParameters.IsGeographic)
            {
                tol = 180.0 * tol / Math.PI / 6370000.0;
            }

            for (int i = 0; i < coll.PointCount; i++)
            {
                if (display.GraphicsContainer.EditMode == GrabberMode.Pointer)
                {
                    if (!UseGrabber((GrabberIDs)i))
                    {
                        continue;
                    }
                }

                IPoint p = coll[i];
                if (Math.Sqrt(Math.Pow(p.X - point.X, 2) + Math.Pow(p.Y - point.Y, 2)) < tol)
                {
                    switch (display.GraphicsContainer.EditMode)
                    {
                        case GrabberMode.Pointer:
                            return new HitPosition(HitCursors.Cursor((GrabberIDs)i, (float)(_angle * 180.0 / Math.PI)), i);
                        case GrabberMode.Vertex:
                            return new HitPosition(HitCursors.VertexCursor, i);
                    }
                }
            }

            if (UseGrabber(GrabberIDs.path) && display.GraphicsContainer.EditMode == GrabberMode.Vertex)
            {
                IGeometry geometry = null;

                geometry = _template.Clone() as IGeometry;
                ScaleGeometry(geometry, _scaleX, _scaleY);
                RotateGeometry(geometry, Math.Cos(_angle), Math.Sin(_angle));
                TranslateGeometry(geometry, _xOffset, _yOffset);

                List<IPath> paths = SpatialAlgorithms.Algorithm.GeometryPaths(geometry);
                if (paths == null || paths.Count == 0)
                {
                    return null;
                }

                Polyline pLine = new Polyline(paths);

                if (SpatialAlgorithms.Algorithm.IntersectBox(pLine, new Envelope(
                        point.X - tol / 2, point.Y - tol / 2,
                        point.X + tol / 2, point.Y + tol / 2)))
                {
                    return new HitPosition(System.Windows.Forms.Cursors.Default, (int)GrabberIDs.path);
                }
            }
            if (UseGrabber(GrabberIDs.move) && display.GraphicsContainer.EditMode == GrabberMode.Pointer)
            {
                IGeometry geometry = null;

                if (_template is IPoint)
                {
                    geometry = new Envelope(
                        _xOffset - _scaleX / 2, _yOffset - _scaleX / 2,
                        _xOffset + _scaleX / 2, _yOffset + _scaleY / 2);
                }
                else
                {
                    geometry = _template.Clone() as IGeometry;
                    ScaleGeometry(geometry, _scaleX, _scaleY);
                    RotateGeometry(geometry, Math.Cos(_angle), Math.Sin(_angle));
                    TranslateGeometry(geometry, _xOffset, _yOffset);
                }
                if (SpatialAlgorithms.Algorithm.IntersectBox(geometry, new Envelope(
                        point.X - tol / 2, point.Y - tol / 2,
                        point.X + tol / 2, point.Y + tol / 2)))
                {
                    return new HitPosition(HitCursors.Cursor(GrabberIDs.move, 0), (int)GrabberIDs.move);
                }
            }
            return null;
        }

        virtual public bool TrySelect(IDisplay display, IEnvelope envelope)
        {
            if (_template == null)
            {
                return false;
            }

            IGeometry geometry = _template.Clone() as IGeometry;
            if (geometry == null)
            {
                return false;
            }

            ScaleGeometry(geometry, _scaleX, _scaleY);
            RotateGeometry(geometry, Math.Cos(_angle), Math.Sin(_angle));
            TranslateGeometry(geometry, _xOffset, _yOffset);

            return SpatialAlgorithms.Algorithm.Contains(envelope, geometry);
        }

        virtual public bool TrySelect(IDisplay display, IPoint point)
        {
            if (_template == null)
            {
                return false;
            }

            IGeometry geometry = _template.Clone() as IGeometry;
            if (geometry == null)
            {
                return false;
            }

            ScaleGeometry(geometry, _scaleX, _scaleY);
            RotateGeometry(geometry, Math.Cos(_angle), Math.Sin(_angle));
            TranslateGeometry(geometry, _xOffset, _yOffset);

            //return SpatialAlgorithms.Algorithm.Contains(geometry, point);
            double tol = 5.0 * display.MapScale / (display.Dpi / 0.0254);
            if (display.SpatialReference != null &&
                display.SpatialReference.SpatialParameters.IsGeographic)
            {
                tol = 180.0 * tol / Math.PI / 6370000.0;
            }

            return SpatialAlgorithms.Algorithm.IntersectBox(geometry, new Envelope(point.X - tol / 2, point.Y - tol / 2, point.X + tol / 2, point.Y + tol / 2));
        }

        private double _oldScaleX, _oldScaleY;
        private double _oldXOffset, _oldYOffset;
        private double _oldAngle;
        private IPoint _oldAnglePoint, _fixPoint, _oldFixPoint;
        private IHitPositions _hit = null;
        virtual public void Design(IDisplay display, IHitPositions hit, double dx, double dy)
        {
            if (display == null || display.GraphicsContainer == null || hit == null)
            {
                return;
            }

            switch (display.GraphicsContainer.EditMode)
            {
                case GrabberMode.Pointer:
                    DesignPointer(display, hit, dx, dy);
                    break;
                case GrabberMode.Vertex:
                    DesignVertex(display, hit, dx, dy);
                    break;
            }
        }

        public bool RemoveVertex(IDisplay display, int index)
        {
            IPointCollection pColl = Vertices(_template, index);

            if (_ghost != null)
            {
                _ghost.RemoveVertex(display, index);
            }

            return true;
        }

        public bool AddVertex(IDisplay display, IPoint point)
        {
            IPoint p = new Point(point.X, point.Y);
            TranslateGeometry(p, -_xOffset, -_yOffset);
            RotateGeometry(p, Math.Cos(-_angle), Math.Sin(-_angle));
            ScaleGeometry(p, 1.0 / _scaleX, 1.0 / _scaleY);

            double distance;
            IPoint snapped = SpatialAlgorithms.Algorithm.NearestPointToPath(
                _template, p, out distance, true);

            if (_ghost != null)
            {
                _ghost.AddVertex(display, point);
            }
            return snapped != null;
        }

        #endregion

        private void DesignPointer(IDisplay display, IHitPositions hit, double dx, double dy)
        {
            if (_hit != hit)
            {
                _hit = hit;
                _oldScaleX = _scaleX;
                _oldScaleY = _scaleY;
                _oldXOffset = _xOffset;
                _oldYOffset = _yOffset;
                _oldAngle = _angle;

                IMultiPoint grabbers = Grabbers(display, false);
                _oldAnglePoint = grabbers[8];

                ScaleGeometry(_oldAnglePoint, _scaleX, _scaleY);
                RotateGeometry(_oldAnglePoint, Math.Cos(_angle), Math.Sin(_angle));
                TranslateGeometry(_oldAnglePoint, _xOffset, _yOffset);

                switch ((GrabberIDs)hit.HitID)
                {
                    case GrabberIDs.left:
                        _fixPoint = grabbers[(int)GrabberIDs.right];
                        break;
                    case GrabberIDs.right:
                        _fixPoint = grabbers[(int)GrabberIDs.left];
                        break;
                    case GrabberIDs.lower:
                        _fixPoint = grabbers[(int)GrabberIDs.upper];
                        break;
                    case GrabberIDs.upper:
                        _fixPoint = grabbers[(int)GrabberIDs.lower];
                        break;
                    case GrabberIDs.lowerLeft:
                        _fixPoint = grabbers[(int)GrabberIDs.upperRight];
                        break;
                    case GrabberIDs.upperRight:
                        _fixPoint = grabbers[(int)GrabberIDs.lowerLeft];
                        break;
                    case GrabberIDs.lowerRight:
                        _fixPoint = grabbers[(int)GrabberIDs.upperLeft];
                        break;
                    case GrabberIDs.upperLeft:
                        _fixPoint = grabbers[(int)GrabberIDs.lowerRight];
                        break;
                    default:
                        _fixPoint = null;
                        break;
                }
                if (_fixPoint != null)
                {
                    _oldFixPoint = new Point(_fixPoint.X, _fixPoint.Y);
                    ScaleGeometry(_oldFixPoint, _scaleX, _scaleY);
                    RotateGeometry(_oldFixPoint, Math.Cos(_angle), Math.Sin(_angle));
                    TranslateGeometry(_oldFixPoint, _xOffset, _yOffset);
                }
            }
            if (hit.HitID < 0 || hit.HitID > 9)
            {
                return;
            }

            if ((GrabberIDs)hit.HitID != GrabberIDs.move && (GrabberIDs)hit.HitID != GrabberIDs.rotation)
            {
                double x = dx, y = dy;
                dx = Math.Cos(_angle) * x - Math.Sin(_angle) * y;
                dy = Math.Sin(_angle) * x + Math.Cos(_angle) * y;
            }

            switch ((GrabberIDs)hit.HitID)
            {
                case GrabberIDs.upperRight:
                    _scaleX = _oldScaleX + dx;
                    _scaleY = _oldScaleY + dy;
                    break;
                case GrabberIDs.upperLeft:
                    _xOffset = _oldXOffset + dx;
                    _scaleX = _oldScaleX - dx;
                    _scaleY = _oldScaleY + dy;
                    break;
                case GrabberIDs.lowerLeft:
                    _xOffset = _oldXOffset + dx;
                    _scaleX = _oldScaleX - dx;
                    _yOffset = _oldYOffset + dy;
                    _scaleY = _oldScaleY - dy;
                    break;
                case GrabberIDs.lowerRight:
                    _scaleX = _oldScaleX + dx;
                    _yOffset = _oldYOffset + dy;
                    _scaleY = _oldScaleY - dy;
                    break;
                case GrabberIDs.lower:
                    _yOffset = _oldYOffset + dy;
                    _scaleY = _oldScaleY - dy;
                    SnapToFixPoint();
                    break;
                case GrabberIDs.upper:
                    _scaleY = _oldScaleY + dy;
                    break;
                case GrabberIDs.right:
                    _scaleX = _oldScaleX + dx;
                    break;
                case GrabberIDs.left:
                    _xOffset = _oldXOffset + dx;
                    _scaleX = _oldScaleX - dx;
                    break;
                case GrabberIDs.rotation:
                    IPoint point = new Point(_rotationCenter.X, _rotationCenter.Y);
                    ScaleGeometry(point, _scaleX, _scaleY);
                    TranslateGeometry(point, _xOffset, _yOffset);
                    dx = _oldAnglePoint.X + dx - point.X;
                    dy = _oldAnglePoint.Y + dy - point.Y;

                    //this.Rotation = Math.Atan2(dx, dy) * 180.0 / Math.PI;
                    _angle = Math.Atan2(dx, dy);
                    //if (_scaleY < 0) _angle += Math.PI;
                    //if (_angle > 2.0 * Math.PI) _angle -= 2.0 * Math.PI;
                    break;
                case GrabberIDs.move:
                    _xOffset = _oldXOffset + dx;
                    _yOffset = _oldYOffset + dy;
                    break;
            }
            SnapToFixPoint();
        }
        private void DesignVertex(IDisplay display, IHitPositions hit, double x, double y)
        {
            //IMultiPoint mPoint = Grabbers(display, false);
            IPointCollection pColl = Vertices(_template, -1);

            if (pColl != null || pColl.PointCount > hit.HitID)
            {
                //if (_hit != hit)
                //{
                //    _hit = hit;

                //    _fixPoint = new Point(pColl[hit.HitID].X, pColl[hit.HitID].Y);

                //    _oldFixPoint = new Point(_fixPoint.X, _fixPoint.Y);
                //    ScaleGeometry(_oldFixPoint, _scaleX, _scaleY);
                //    RotateGeometry(_oldFixPoint, Math.Cos(_angle), Math.Sin(_angle));
                //    TranslateGeometry(_oldFixPoint, _xOffset, _yOffset);
                //}

                IPoint point = new Point(x, y);
                TranslateGeometry(point, -_xOffset, -_yOffset);
                RotateGeometry(point, Math.Cos(-_angle), Math.Sin(-_angle));
                ScaleGeometry(point, 1.0 / _scaleX, 1.0 / _scaleY);

                pColl[hit.HitID].X = point.X;
                pColl[hit.HitID].Y = point.Y;

                IGeometry geometry = this.TransformGeometry();
                if (geometry != null)
                {
                    this.Template = geometry;

                    IEnvelope env = geometry.Envelope;
                    this._angle = 0;
                    this.Scale(env.Width, env.Height);
                    this.Translation(env.minx, env.miny);
                }
                //if (!(this is Ghost))
                //{
                //    this.Template = _template;
                //    _scaleX = pColl.Envelope.Width;
                //    _scaleY = pColl.Envelope.Height;
                //}
                //SnapToFixPoint();
            }
        }

        private void SnapToFixPoint()
        {
            if (_fixPoint == null || _oldFixPoint == null)
            {
                return;
            }

            Point f2 = new Point(_fixPoint.X, _fixPoint.Y);

            ScaleGeometry(f2, _scaleX, _scaleY);
            RotateGeometry(f2, Math.Cos(_angle), Math.Sin(_angle));
            TranslateGeometry(f2, _xOffset, _yOffset);

            double dx = _oldFixPoint.X - f2.X;
            double dy = _oldFixPoint.Y - f2.Y;
            if (dx != 0.0 || dy != 0.0)
            {
                _xOffset += dx;
                _yOffset += dy;
            }
        }

        #region IBrushColor Member

        virtual public GraphicsEngine.ArgbColor FillColor
        {
            get
            {
                if (_symbol is IBrushColor)
                {
                    return ((IBrushColor)_symbol).FillColor;
                }
                return GraphicsEngine.ArgbColor.Transparent;
            }
            set
            {
                if (_symbol is IBrushColor)
                {
                    ((IBrushColor)_symbol).FillColor = value;
                }
            }
        }

        #endregion

        #region IPenColor Member

        virtual public GraphicsEngine.ArgbColor PenColor
        {
            get
            {
                if (_symbol is IPenColor)
                {
                    return ((IPenColor)_symbol).PenColor;
                }
                return GraphicsEngine.ArgbColor.Transparent;
            }
            set
            {
                if (_symbol is IPenColor)
                {
                    ((IPenColor)_symbol).PenColor = value;
                }
            }
        }

        #endregion

        #region IFontColor Member

        virtual public GraphicsEngine.ArgbColor FontColor
        {
            get
            {
                if (_symbol is IFontColor)
                {
                    return ((IFontColor)_symbol).FontColor;
                }
                return GraphicsEngine.ArgbColor.Transparent;
            }
            set
            {
                if (_symbol is IFontColor)
                {
                    ((IFontColor)_symbol).FontColor = value;
                }
            }
        }

        #endregion

        #region IPenWidth Member

        public float PenWidth
        {
            get
            {
                if (_symbol is IPenWidth)
                {
                    return ((IPenWidth)_symbol).PenWidth;
                }
                return 0;
            }
            set
            {
                if (_symbol is IPenWidth)
                {
                    ((IPenWidth)_symbol).PenWidth = value;
                }
            }
        }

        public float MaxPenWidth { get; set; }
        public float MinPenWidth { get; set; }

        public DrawingUnit PenWidthUnit
        {
            get { return DrawingUnit.Pixel; }
            set { }
        }

        #endregion

        #region IPenDashStyle Member

        public GraphicsEngine.LineDashStyle PenDashStyle
        {
            get
            {
                if (_symbol is IPenDashStyle)
                {
                    return ((IPenDashStyle)_symbol).PenDashStyle;
                }
                return GraphicsEngine.LineDashStyle.Solid;
            }
            set
            {
                if (_symbol is IPenDashStyle)
                {
                    ((IPenDashStyle)_symbol).PenDashStyle = value;
                }
            }
        }

        #endregion

        #region IFont Member

        virtual public GraphicsEngine.Abstraction.IFont Font
        {
            get
            {
                if (_symbol is IFontSymbol)
                {
                    return ((IFontSymbol)_symbol).Font;
                }
                return GraphicsEngine.Current.Engine.CreateFont("Arial", 10);
            }
            set
            {
                if (_symbol is IFontSymbol)
                {
                    ((IFontSymbol)_symbol).Font = value;
                }
            }
        }

        #endregion

        #region IPersistable Member

        virtual public void Load(IPersistStream stream)
        {
            _scaleX = (double)stream.Load("scaleX", 1.0);
            _scaleY = (double)stream.Load("scaleY", 1.0);
            _xOffset = (double)stream.Load("xOffset", 0.0);
            _yOffset = (double)stream.Load("yOffset", 0.0);
            _angle = (double)stream.Load("Angle");

            if (this is IConstructable && ((IConstructable)this).hasVertices)
            {
                PersistableGeometry pGeometry = stream.Load("Geometry", null, new PersistableGeometry()) as PersistableGeometry;
                if (pGeometry != null && pGeometry.Geometry != null)
                {
                    TemplateFromTransformedGeomety(pGeometry.Geometry);
                }
            }

            if (Ghost != null)
            {
                Ghost.Scale(_scaleX, _scaleY);
                Ghost.Rotation = _angle * 180.0 / Math.PI;
                Ghost.Translation(_xOffset, _yOffset);
            }

        }

        virtual public void Save(IPersistStream stream)
        {
            if (this is IConstructable && ((IConstructable)this).hasVertices && _template != null)
            {
                IGeometry geometry = TransformGeometry();
                if (geometry != null)
                {
                    PersistableGeometry pGeometry = new PersistableGeometry(geometry);
                    stream.Save("Geometry", pGeometry);
                }
            }

            stream.Save("scaleX", _scaleX);
            stream.Save("scaleY", _scaleY);
            stream.Save("xOffset", _xOffset);
            stream.Save("yOffset", _yOffset);
            stream.Save("Angle", _angle);
        }

        #endregion

        #region IToolMouseActions Member

        virtual public void MouseDown(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {

        }

        virtual public void MouseUp(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {

        }

        virtual public void MouseClick(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {

        }

        virtual public void MouseDoubleClick(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            FormSymbol dlg = new FormSymbol(this.Symbol);
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Symbol = dlg.Symbol;
            }
        }

        virtual public void MouseMove(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {

        }

        public void MouseWheel(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {

        }

        #endregion

        static public void AddElementsToContainer(IMapDocument doc, IGraphicElementList elements)
        {
            if (ActiveDisplay(doc) == null || elements == null)
            {
                return;
            }

            IDisplay display = ActiveDisplay(doc);

            display.GraphicsContainer.SelectedElements.Clear();
            foreach (IGraphicElement element in elements)
            {
                if (!display.GraphicsContainer.Elements.Contains(element))
                {
                    display.GraphicsContainer.Elements.Add(element);
                }

                display.GraphicsContainer.SelectedElements.Add(element);
            }
            if (doc.Application is IMapApplication)
            {
                ((IMapApplication)doc.Application).ActiveTool =
                    ((IMapApplication)doc.Application).Tool(new Guid("FEEEE362-116B-406b-8D94-D817A9BAC121"));
            }
        }
        static public void AddElementToContainer(IMapDocument doc, IGraphicElement element)
        {
            if (ActiveDisplay(doc) == null)
            {
                return;
            }

            ActiveDisplay(doc).GraphicsContainer.Elements.Add(element);
            ActiveDisplay(doc).GraphicsContainer.SelectedElements.Clear();
            ActiveDisplay(doc).GraphicsContainer.SelectedElements.Add(element);

            //if (doc != null && doc.Application != null)
            //    doc.Application.RefreshActiveMap(DrawPhase.Graphics);

            if (doc.Application is IMapApplication)
            {
                ((IMapApplication)doc.Application).ActiveTool =
                    ((IMapApplication)doc.Application).Tool(new Guid("FEEEE362-116B-406b-8D94-D817A9BAC121"));
            }
        }

        static private IDisplay ActiveDisplay(IMapDocument doc)
        {
            if (doc == null || doc.FocusMap == null)
            {
                return null;
            }

            return doc.FocusMap.Display;
        }
    }

    public class Ghost : GraphicShape
    {
        public Ghost(IGeometry template, ISymbol symbol)
        {
            Template = template;
            Symbol = symbol;
        }
    }

    public enum GrabberIDs
    {
        upperLeft = 0,
        upperRight = 1,
        lowerRight = 2,
        lowerLeft = 3,
        upper = 4,
        right = 5,
        lower = 6,
        left = 7,
        rotation = 8,
        move = 9,
        vertex = 10,
        path = 11
    }

    public class HitCursors
    {
        static public System.Windows.Forms.Cursor Cursor(GrabberIDs id, float angle)
        {
            switch (id)
            {
                case GrabberIDs.upperLeft:
                case GrabberIDs.lowerRight:
                    return System.Windows.Forms.Cursors.SizeNWSE;
                case GrabberIDs.lowerLeft:
                case GrabberIDs.upperRight:
                    return System.Windows.Forms.Cursors.SizeNESW;
                case GrabberIDs.upper:
                case GrabberIDs.lower:
                    return System.Windows.Forms.Cursors.SizeNS;
                case GrabberIDs.left:
                case GrabberIDs.right:
                    return System.Windows.Forms.Cursors.SizeWE;
                case GrabberIDs.rotation:
                    return HitCursors.RotationCursor;
                case GrabberIDs.move:
                    return System.Windows.Forms.Cursors.SizeAll;
            }
            return System.Windows.Forms.Cursors.Default;
        }

        static public System.Windows.Forms.Cursor RotationCursor = new System.Windows.Forms.Cursor(gView.Framework.system.SystemVariables.StartupDirectory + @"\Cursors\Rotation.cur");
        static public System.Windows.Forms.Cursor VertexCursor = new System.Windows.Forms.Cursor(gView.Framework.system.SystemVariables.StartupDirectory + @"\Cursors\Vertex.cur");
    }

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
    }
}
