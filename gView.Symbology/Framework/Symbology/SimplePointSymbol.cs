using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.ComponentModel;
using System.Reflection;

namespace gView.Framework.Symbology
{
    [gView.Framework.system.RegisterPlugIn("F73F40DD-BA55-40b1-B372-99F08B66D2D4")]
    public sealed class SimplePointSymbol : Symbol, IPointSymbol, IPropertyPage, ISymbolRotation, IBrushColor, IPenColor, IPenWidth, ISymbolSize, ISymbolWidth
    {
        public enum MarkerType { Circle = 0, Triangle = 1, Square = 2, Cross = 3, Star = 4 }

        private float _size = 5, _symbolWidth = 0, _angle = 0, _rotation = 0;
        //private IPoint _point=new gView.Framework.Geometry.Point();
        private float _xOffset = 0, _yOffset = 0, _hOffset = 0, _vOffset = 0;
        private IBrush _brush;
        private IPen _pen;
        private MarkerType _type = MarkerType.Circle;

        public SimplePointSymbol()
        {
            _brush = Current.Engine.CreateSolidBrush(ArgbColor.Blue);
            _pen = Current.Engine.CreatePen(ArgbColor.Black, 1f);
        }

        private SimplePointSymbol(ArgbColor penColor, float penWidth, ArgbColor brushColor)
        {
            _brush = Current.Engine.CreateSolidBrush(brushColor);
            _pen = Current.Engine.CreatePen(penColor, penWidth);
        }

        [Browsable(true)]
        public float Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = Math.Max(value, 1f);
            }
        }

        [Browsable(true)]
        public float SymbolWidth
        {
            get { return _symbolWidth; }
            set { _symbolWidth = value; }
        }

        [Browsable(true)]
        [UseColorPicker()]
        public ArgbColor Color
        {
            get
            {
                return _brush.Color;
            }
            set
            {
                _brush.Color = value;
            }
        }

        [Browsable(true)]
        [UseWidthPicker()]
        public float OutlineWidth
        {
            get { return _pen.Width; }
            set { _pen.Width = value; }
        }

        [Browsable(true)]
        [UseColorPicker()]
        public ArgbColor OutlineColor
        {
            get { return _pen.Color; }
            set { _pen.Color = value; }
        }

        [Browsable(true)]
        public MarkerType Marker
        {
            get { return _type; }
            set { _type = value; }
        }

        #region IPointSymbol Member

        [Browsable(false)]
        public float HorizontalOffset
        {
            get { return _hOffset; }
            set
            {
                _hOffset = value;
                SymbolTransformation.Transform(_angle, _hOffset, _vOffset, out _xOffset, out _yOffset);
            }
        }

        [Browsable(false)]
        public float VerticalOffset
        {
            get { return _vOffset; }
            set
            {
                _vOffset = value;
                SymbolTransformation.Transform(_angle, _hOffset, _vOffset, out _xOffset, out _yOffset);
            }
        }

        [Browsable(false)]
        public float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                SymbolTransformation.Transform(_angle, _hOffset, _vOffset, out _xOffset, out _yOffset);
            }
        }

        public void DrawPoint(IDisplay display, IPoint point)
        {
            //float x=(float)point.X,y=(float)point.Y;

            try
            {
                float x = _xOffset - _size / 2;
                float y = _yOffset - _size / 2;

                display.Canvas.SmoothingMode = (GraphicsEngine.SmoothingMode)this.Smoothingmode;

                var transformRotation = _angle + _rotation;

                if (display.DisplayTransformation.UseTransformation)
                {
                    transformRotation -= (float)display.DisplayTransformation.DisplayRotation;
                }

                display.Canvas.TranslateTransform(new CanvasPointF((float)point.X, (float)point.Y));
                display.Canvas.RotateTransform(transformRotation);

                switch (_type)
                {
                    case MarkerType.Circle:
                        if (!_brush.Color.IsTransparent)
                        {
                            display.Canvas.FillEllipse(_brush, x, y, _size, _size);
                        }

                        if (!_pen.Color.IsTransparent)
                        {
                            display.Canvas.DrawEllipse(_pen, x, y, _size, _size);
                        }

                        break;
                    case MarkerType.Triangle:
                        using (var gp = Current.Engine.CreateGraphicsPath())
                        {
                            gp.StartFigure();
                            gp.AddLine(x, .866f * _size + y, _size + x, .866f * _size + y);
                            gp.AddLine(_size + x, .866f * _size + y, _size / 2 + x, y);
                            gp.CloseFigure();

                            if (!_brush.Color.IsTransparent)
                            {
                                display.Canvas.FillPath(_brush, gp);
                            }

                            if (!_pen.Color.IsTransparent)
                            {
                                display.Canvas.DrawPath(_pen, gp);
                            }
                        }
                        break;
                    case MarkerType.Square:
                        if (!_brush.Color.IsTransparent)
                        {
                            display.Canvas.FillRectangle(_brush, new CanvasRectangleF(x, y, _size, _size));
                        }

                        if (!_pen.Color.IsTransparent)
                        {
                            display.Canvas.DrawRectangle(_pen, new CanvasRectangleF(x, y, _size, _size));
                        }

                        break;
                    case MarkerType.Cross:
                        float sw = _symbolWidth;

                        using (var gp2 = Current.Engine.CreateGraphicsPath())
                        {
                            gp2.StartFigure();
                            gp2.AddLine(x, y + _size / 2 - sw, x, y + _size / 2 + sw);
                            gp2.AddLine(x + _size / 2 - sw, y + _size / 2 + sw, x + _size / 2 - sw, y + _size);
                            gp2.AddLine(x + _size / 2 + sw, y + _size, x + _size / 2 + sw, y + _size / 2 + sw);
                            gp2.AddLine(x + _size, y + _size / 2 + sw, x + _size, y + _size / 2 - sw);
                            gp2.AddLine(x + _size / 2 + sw, y + _size / 2 - sw, x + _size / 2 + sw, y);
                            gp2.AddLine(x + _size / 2 - sw, y, x + _size / 2 - sw, y + _size / 2 - sw);
                            gp2.CloseFigure();

                            if (!_brush.Color.IsTransparent && sw > 0.0)
                            {
                                display.Canvas.FillPath(_brush, gp2);
                            }

                            if (!_pen.Color.IsTransparent)
                            {
                                display.Canvas.DrawPath(_pen, gp2);
                            }
                        }
                        break;
                    case MarkerType.Star:
                        using (var gp3 = Current.Engine.CreateGraphicsPath())
                        {
                            double w1 = 2.0 * Math.PI / 5.0;
                            double w2 = w1 / 2.0;

                            gp3.StartFigure();
                            for (int i = 0; i < 5; i++)
                            {
                                float x1 = _size / 2 + _size / 2 * (float)Math.Sin(w1 * i);
                                float y1 = _size / 2 - _size / 2 * (float)Math.Cos(w1 * i);
                                float x2 = _size / 2 + _size / 5 * (float)Math.Sin(w1 * i + w2);
                                float y2 = _size / 2 - _size / 5 * (float)Math.Cos(w1 * i + w2);
                                gp3.AddLine(x1 + x, y1 + y, x2 + x, y2 + y);
                            }
                            gp3.CloseFigure();

                            if (!_brush.Color.IsTransparent)
                            {
                                display.Canvas.FillPath(_brush, gp3);
                            }

                            if (!_pen.Color.IsTransparent)
                            {
                                display.Canvas.DrawPath(_pen, gp3);
                            }
                        }
                        break;
                }
            }
            finally
            {
                display.Canvas.ResetTransform();
                display.Canvas.SmoothingMode = SmoothingMode.None;
            }
        }

        #endregion

        #region ISymbol Member

        public bool SupportsGeometryType(GeometryType geomType) => geomType == GeometryType.Point || geomType == GeometryType.Multipoint;

        public void Draw(IDisplay display, IGeometry geometry)
        {
            if (geometry is IPoint)
            {
                double x = ((IPoint)geometry).X;
                double y = ((IPoint)geometry).Y;
                display.World2Image(ref x, ref y);
                IPoint p = new gView.Framework.Geometry.Point(x, y);
                DrawPoint(display, p);
            }
            else if (geometry is IMultiPoint)
            {
                for (int i = 0, to = ((IMultiPoint)geometry).PointCount; i < to; i++)
                {
                    IPoint p = ((IMultiPoint)geometry)[i];
                    Draw(display, p);
                }
            }
        }

        public void Release()
        {
            if (_brush != null)
            {
                _brush.Dispose();
            }

            _brush = null;
            if (_pen != null)
            {
                _pen.Dispose();
            }

            _pen = null;
        }

        [Browsable(false)]
        public string Name
        {
            get
            {
                return "Point Symbol";
            }
        }
        #endregion

        #region IPersistable Member

        [Browsable(false)]
        public string PersistID
        {
            get
            {
                return null;
            }
        }

        new public void Load(IPersistStream stream)
        {
            base.Load(stream);

            HorizontalOffset = (float)stream.Load("xOffset", (float)0);
            VerticalOffset = (float)stream.Load("yOffset", (float)0);
            Angle = (float)stream.Load("Angle", (float)0);
            Size = (float)stream.Load("size", (float)5);
            SymbolWidth = (float)stream.Load("symbolWidth", (float)0);
            OutlineWidth = (float)stream.Load("outlinewidth", (float)1);
            Color = ArgbColor.FromArgb((int)stream.Load("color", ArgbColor.Red.ToArgb()));
            OutlineColor = ArgbColor.FromArgb((int)stream.Load("outlinecolor", ArgbColor.Black.ToArgb()));
            Marker = (MarkerType)stream.Load("marker", (int)MarkerType.Circle);

            this.MaxPenWidth = (float)stream.Load("maxpenwidth", 0f);
            this.MinPenWidth = (float)stream.Load("minpenwidth", 0f);

            this.MaxSymbolSize = (float)stream.Load("maxsymbolsize", 0f);
            this.MinSymbolSize = (float)stream.Load("minsymbolsize", 0f);
        }

        new public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("color", this.Color.ToArgb());
            stream.Save("xOffset", HorizontalOffset);
            stream.Save("yOffset", VerticalOffset);
            stream.Save("Angle", Angle);
            stream.Save("size", Size);
            stream.Save("symbolWidth", SymbolWidth);
            stream.Save("outlinewidth", OutlineWidth);
            stream.Save("outlinecolor", OutlineColor.ToArgb());
            stream.Save("marker", (int)_type);

            stream.Save("maxpenwidth", (float)this.MaxPenWidth);
            stream.Save("minpenwidth", (float)this.MinPenWidth);

            stream.Save("maxsymbolsize", this.MaxSymbolSize);
            stream.Save("minsymbolsize", this.MinSymbolSize);
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPageObject()
        {
            return null;
        }

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Symbology.UI.dll");

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SimplePointSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        #endregion

        #region IClone2
        public object Clone(CloneOptions options)
        {
            var display = options?.Display;

            if (display == null)
            {
                return Clone();
            }

            float fac = 1;

            if (options.ApplyRefScale)
            {
                fac = ReferenceScaleHelper.RefscaleFactor(
                    (float)(display.ReferenceScale / display.MapScale),
                    this.SymbolSize, this.MinSymbolSize, this.MaxSymbolSize);
                fac = options.RefScaleFactor(fac);
            }
            fac *= options.DpiFactor;

            SimplePointSymbol pSym = new SimplePointSymbol(_pen.Color, _pen.Width * fac, _brush.Color);
            pSym._size = Math.Max(_size * fac, 1f);
            pSym._symbolWidth = _symbolWidth * fac;
            pSym.Angle = Angle;
            pSym.Marker = _type;
            pSym.HorizontalOffset = HorizontalOffset * fac;
            pSym.VerticalOffset = VerticalOffset * fac;
            pSym.LegendLabel = _legendLabel;
            pSym.Smoothingmode = this.Smoothingmode;

            pSym.MaxSymbolSize = this.MaxSymbolSize;
            pSym.MinSymbolSize = this.MinSymbolSize;

            return pSym;
        }
        #endregion

        #region ISymbolRotation Members

        [Browsable(false)]
        public float Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
            }
        }

        #endregion

        #region IBrushColor Member

        [Browsable(false)]
        public ArgbColor FillColor
        {
            get
            {
                if (_brush != null)
                {
                    return _brush.Color;
                }

                return ArgbColor.Transparent;
            }
            set
            {
                if (_brush != null)
                {
                    _brush.Color = value;
                }
            }
        }

        #endregion

        #region IPenColor Member

        [Browsable(false)]
        public ArgbColor PenColor
        {
            get
            {
                if (_pen != null)
                {
                    return _pen.Color;
                }

                return ArgbColor.Transparent;
            }
            set
            {
                if (_pen != null)
                {
                    _pen.Color = value;
                }
            }
        }

        #endregion

        #region IPenWidth Member
        [Browsable(false)]
        public float PenWidth
        {
            get
            {
                if (_pen != null)
                {
                    return _pen.Width;
                }

                return 0f;
            }
            set
            {
                if (_pen != null)
                {
                    _pen.Width = value;
                }
            }
        }

        [Browsable(false)]
        public DrawingUnit PenWidthUnit
        {
            get
            {
                return DrawingUnit.Pixel;
            }
            set
            {

            }
        }

        private float _maxPenWidth, _minPenWidth;

        [Browsable(true)]
        [Category("Reference Scaling")]
        [UseWidthPicker()]
        public float MaxPenWidth
        {
            get
            {
                return _maxPenWidth;
            }
            set
            {
                _maxPenWidth = value;
            }
        }

        [Browsable(true)]
        [Category("Reference Scaling")]
        [UseWidthPicker()]
        public float MinPenWidth
        {
            get
            {
                return _minPenWidth;
            }
            set
            {
                _minPenWidth = value;
            }
        }

        #endregion

        #region ISymbolSize Member

        [Browsable(false)]
        public float SymbolSize
        {
            get
            {
                return this.Size;
            }
            set
            {
                this.Size = value;
            }
        }

        [Browsable(true)]
        [Category("Reference Scaling")]
        public float MaxSymbolSize { get; set; }

        [Browsable(true)]
        [Category("Reference Scaling")]
        public float MinSymbolSize { get; set; }

        #endregion

        #region ISymbol Member

        [Browsable(false)]
        public SymbolSmoothing SymbolSmothingMode
        {
            set { this.Smoothingmode = value; }
        }

        public bool RequireClone()
        {
            return false;
        }

        #endregion
    }
}
