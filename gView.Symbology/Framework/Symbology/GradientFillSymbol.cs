using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System.ComponentModel;
using System.Reflection;

namespace gView.Framework.Symbology
{
    [RegisterPlugInAttribute("E043E059-47E9-42A0-ACF0-FB1012DC8AA2")]
    public sealed class GradientFillSymbol : LegendItem, IFillSymbol, IPenColor, IPenDashStyle, IPenWidth, IPropertyPage
    {
        public enum GradientRectType { Feature = 0, Display = 1 }

        private ColorGradient _gradient;
        private ISymbol _outlineSymbol = null;
        private GradientRectType _rectType = GradientRectType.Feature;

        public GradientFillSymbol()
        {
            _gradient = new ColorGradient(ArgbColor.Red, ArgbColor.Blue);
            _gradient.Angle = 45f;
        }

        public GradientFillSymbol(ColorGradient gradient)
        {
            _gradient = new ColorGradient(gradient);
        }

        ~GradientFillSymbol()
        {
            this.Release();
        }

        [Browsable(true)]
        [Category("Fill Symbol")]
        [ReadOnly(true)]
        [UseColorGradientPicker]
        public ColorGradient ColorGradient
        {
            get { return _gradient; }
            set
            {
                if (_gradient != null)
                {
                    _gradient = value;
                }
            }
        }

        [Browsable(true)]
        [Category("Fill Symbol")]
        public GradientRectType GradientRectangle
        {
            get { return _rectType; }
            set { _rectType = value; }
        }

        [Browsable(true)]
        [DisplayName("Symbol")]
        [Category("Outline Symbol")]
        [UseLineSymbolPicker()]
        public ISymbol OutlineSymbol
        {
            get
            {
                return _outlineSymbol;
            }
            set
            {
                _outlineSymbol = value;
            }
        }

        [Browsable(true)]
        [DisplayName("Smoothingmode")]
        [Category("Outline Symbol")]
        public SymbolSmoothing SmoothingMode
        {
            get
            {
                if (_outlineSymbol is Symbol)
                {
                    return ((Symbol)_outlineSymbol).Smoothingmode;
                }

                return SymbolSmoothing.None;
            }
            set
            {
                if (_outlineSymbol is Symbol)
                {
                    ((Symbol)_outlineSymbol).Smoothingmode = value;
                }
            }
        }

        [Browsable(true)]
        [DisplayName("Color")]
        [Category("Outline Symbol")]
        [UseColorPicker()]
        public ArgbColor OutlineColor
        {
            get { return PenColor; }
            set { PenColor = value; }
        }

        [Browsable(true)]
        [DisplayName("DashStyle")]
        [Category("Outline Symbol")]
        [UseDashStylePicker()]
        public LineDashStyle OutlineDashStyle
        {
            get { return PenDashStyle; }
            set { PenDashStyle = value; }
        }

        [Browsable(true)]
        [Category("Outline Symbol")]
        [DisplayName("Width")]
        [UseWidthPicker()]
        public float OutlineWidth
        {
            get { return PenWidth; }
            set { PenWidth = value; }
        }

        #region IPenColor Member

        [Browsable(false)]
        public ArgbColor PenColor
        {
            get
            {
                if (_outlineSymbol is IPenColor)
                {
                    return ((IPenColor)_outlineSymbol).PenColor;
                }
                return ArgbColor.Transparent;
            }
            set
            {
                if (_outlineSymbol is IPenColor)
                {
                    ((IPenColor)_outlineSymbol).PenColor = value;
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
                if (_outlineSymbol is IPenWidth)
                {
                    return ((IPenWidth)_outlineSymbol).PenWidth;
                }
                return 0;
            }
            set
            {
                if (_outlineSymbol is IPenWidth)
                {
                    ((IPenWidth)_outlineSymbol).PenWidth = value;
                }
            }
        }

        [Browsable(false)]
        public DrawingUnit PenWidthUnit
        {
            get
            {
                if (_outlineSymbol is IPenWidth)
                {
                    return ((IPenWidth)_outlineSymbol).PenWidthUnit;
                }
                return DrawingUnit.Pixel;
            }
            set
            {
                if (_outlineSymbol is IPenWidth)
                {
                    ((IPenWidth)_outlineSymbol).PenWidthUnit = value;
                }
            }
        }

        [Browsable(true)]
        [Category("Reference Scaling")]
        [UseWidthPicker()]
        public float MaxPenWidth
        {
            get
            {
                if (_outlineSymbol is IPenWidth)
                {
                    return ((IPenWidth)_outlineSymbol).MaxPenWidth;
                }
                return 0;
            }
            set
            {
                if (_outlineSymbol is IPenWidth)
                {
                    ((IPenWidth)_outlineSymbol).MaxPenWidth = value;
                }
            }
        }

        [Browsable(true)]
        [Category("Reference Scaling")]
        [UseWidthPicker()]
        public float MinPenWidth
        {
            get
            {
                if (_outlineSymbol is IPenWidth)
                {
                    return ((IPenWidth)_outlineSymbol).MinPenWidth;
                }
                return 0;
            }
            set
            {
                if (_outlineSymbol is IPenWidth)
                {
                    ((IPenWidth)_outlineSymbol).MinPenWidth = value;
                }
            }
        }

        #endregion

        #region IPenDashStyle Member

        [Browsable(false)]
        public LineDashStyle PenDashStyle
        {
            get
            {
                if (_outlineSymbol is IPenDashStyle)
                {
                    return ((IPenDashStyle)_outlineSymbol).PenDashStyle;
                }
                return LineDashStyle.Solid;
            }
            set
            {
                if (_outlineSymbol is IPenDashStyle)
                {
                    ((IPenDashStyle)_outlineSymbol).PenDashStyle = value;
                }
            }
        }

        #endregion

        #region IFillSymbol Member

        public void FillPath(IDisplay display, IGraphicsPath path)
        {
            //display.GraphicsContext.SmoothingMode = (SmoothingMode)this.Smoothingmode;

            if (_gradient != null)
            {
                CanvasRectangleF rect =
                    (_rectType == GradientRectType.Feature ?
                    path.GetBounds() :
                    new CanvasRectangleF(0, 0, display.ImageWidth, display.ImageHeight));

                using (var brush = _gradient.CreateNewLinearGradientBrush(rect))
                {
                    display.Canvas.FillPath(brush, path);
                }
            }
            //if (_outlineSymbol != null)
            //{
            //    if (_outlineSymbol is ILineSymbol)
            //        ((ILineSymbol)_outlineSymbol).DrawPath(display, path);
            //    else if (_outlineSymbol is SymbolCollection)
            //    {
            //        foreach (SymbolCollectionItem item in ((SymbolCollection)_outlineSymbol).Symbols)
            //        {
            //            if (!item.Visible) continue;
            //            if (item.Symbol is ILineSymbol)
            //            {
            //                ((ILineSymbol)item.Symbol).DrawPath(display, path);
            //            }
            //        }
            //    }
            //}
        }

        #endregion

        #region ISymbol Member

        public bool SupportsGeometryType(GeometryType geomType) => geomType == GeometryType.Polygon;

        public void Draw(IDisplay display, IGeometry geometry)
        {
            var gp = DisplayOperations.Geometry2GraphicsPath(display, geometry);
            if (gp != null)
            {
                this.FillPath(display, gp);

                SimpleFillSymbol.DrawOutlineSymbol(display, _outlineSymbol, geometry, gp);
                gp.Dispose(); gp = null;
            }
        }

        public void Release()
        {
            if (_outlineSymbol != null)
            {
                _outlineSymbol.Release();
                _outlineSymbol = null;
            }
        }


        [Browsable(false)]
        public string Name
        {
            get
            {
                return "Gradient Fill Symbol";
            }
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

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SimpleFillSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        #endregion

        #region IPersistable Member

        new public void Load(IPersistStream stream)
        {
            base.Load(stream);

            _gradient = (ColorGradient)stream.Load("gradient", _gradient, _gradient);
            _rectType = (GradientRectType)stream.Load("recttype", (int)GradientRectType.Feature);
            _outlineSymbol = (ISymbol)stream.Load("outlinesymbol");
        }

        new public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("gradient", _gradient);
            stream.Save("recttype", (int)_rectType);
            if (_outlineSymbol != null)
            {
                stream.Save("outlinesymbol", _outlineSymbol);
            }
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
                fac = (float)(display.ReferenceScale / display.MapScale);
                fac = options.RefScaleFactor(fac);
            }

            if (display.Dpi != 96.0)
            {
                fac *= (float)(display.Dpi / 96.0);
            }

            GradientFillSymbol fSym = new GradientFillSymbol(_gradient);
            if (_outlineSymbol != null)
            {
                fSym._outlineSymbol = (ISymbol)_outlineSymbol.Clone(options);
            }

            fSym._rectType = _rectType;
            fSym.LegendLabel = _legendLabel;
            return fSym;
        }
        #endregion

        #region ISymbol Member

        [Browsable(false)]
        public SymbolSmoothing SymbolSmothingMode
        {
            set
            {
                if (_outlineSymbol != null)
                {
                    _outlineSymbol.SymbolSmothingMode = value;
                }
            }
        }

        public bool RequireClone()
        {
            return _outlineSymbol != null && _outlineSymbol.RequireClone();
        }

        #endregion
    }
}
