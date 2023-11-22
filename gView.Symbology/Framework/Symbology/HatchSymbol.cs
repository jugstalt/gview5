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
    [gView.Framework.system.RegisterPlugIn("E37D7D86-DF11-410f-ADD1-EA89C1E89605")]
    public sealed class HatchSymbol : LegendItem, IFillSymbol, IPersistable, IPropertyPage, IBrushColor, IPenColor, IPenWidth, IPenDashStyle
    {
        private IBrushCollection _brush;
        private ArgbColor _forecolor;
        private ArgbColor _backcolor;
        private ISymbol _outlineSymbol = null;
        private HatchStyle _hatchStyle;

        public HatchSymbol()
        {
            _forecolor = ArgbColor.Red;
            _backcolor = ArgbColor.Transparent;
            _brush = Current.Engine.CreateHatchBrush(_hatchStyle = HatchStyle.BackwardDiagonal, ForeColor, BackColor);
        }

        private HatchSymbol(ArgbColor foreColor, ArgbColor backColor, HatchStyle hatchStyle)
        {
            _forecolor = foreColor;
            _backcolor = backColor;
            _brush = Current.Engine.CreateHatchBrush(_hatchStyle = hatchStyle, _forecolor, _backcolor);
        }

        ~HatchSymbol()
        {
            this.Release();
        }


        [Browsable(true)]
        [Category("Fill Symbol")]
        [UseHatchStylePicker()]
        public HatchStyle HatchStyle
        {
            get
            {
                return _hatchStyle;
            }
            set
            {
                if (_brush != null)
                {
                    _brush.Dispose();
                }

                _brush = Current.Engine.CreateHatchBrush(_hatchStyle = value, ForeColor, BackColor);
            }
        }

        [Browsable(true)]
        [Category("Fill Symbol")]
        [UseColorPicker()]
        public ArgbColor ForeColor
        {
            get
            {
                return _forecolor;
            }
            set
            {
                _forecolor = value;

                HatchStyle hs = this.HatchStyle;
                if (_brush != null)
                {
                    _brush.Dispose();
                }

                _brush = Current.Engine.CreateHatchBrush(hs, ForeColor, BackColor);
            }
        }


        [Browsable(true)]
        [Category("Fill Symbol")]
        [UseColorPicker()]
        public ArgbColor BackColor
        {
            get
            {
                return _backcolor;
            }
            set
            {
                _backcolor = value;

                HatchStyle hs = this.HatchStyle;
                if (_brush != null)
                {
                    _brush.Dispose();
                }

                _brush = Current.Engine.CreateHatchBrush(hs, ForeColor, BackColor);
            }
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

        #region IFillSymbol Member

        public void FillPath(IDisplay display, IGraphicsPath path)
        {
            if (_forecolor.A > 0 || _backcolor.A > 0)
            {
                display.Canvas.FillPath(_brush, path);
            }
            //if(_outlineSymbol!=null) 
            //{
            //    if(_outlineSymbol is ILineSymbol)
            //        ((ILineSymbol)_outlineSymbol).DrawPath(display,path);
            //    else if(_outlineSymbol is SymbolCollection) 
            //    {
            //        foreach(SymbolCollectionItem item in ((SymbolCollection)_outlineSymbol).Symbols) 
            //        {
            //            if(!item.Visible) continue;
            //            if(item.Symbol is ILineSymbol) 
            //            {
            //                ((ILineSymbol)item.Symbol).DrawPath(display,path);
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
            if (_brush != null)
            {
                _brush.Dispose();
                _brush = null;
            }
            if (_outlineSymbol != null)
            {
                _outlineSymbol.Release();
            }
        }


        [Browsable(false)]
        public string Name
        {
            get
            {
                return "Hatch Symbol";
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

            this.ForeColor = ArgbColor.FromArgb((int)stream.Load("forecolor", ArgbColor.Red.ToArgb()));
            this.BackColor = ArgbColor.FromArgb((int)stream.Load("backcolor", ArgbColor.Transparent.ToArgb()));
            this.HatchStyle = (HatchStyle)stream.Load("hatchstyle", HatchStyle.Horizontal);
            _outlineSymbol = (ISymbol)stream.Load("outlinesymbol");
        }

        new public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("forecolor", _forecolor.ToArgb());
            stream.Save("backcolor", _backcolor.ToArgb());
            stream.Save("hatchstyle", (int)_hatchStyle);
            if (_outlineSymbol != null)
            {
                stream.Save("outlinesymbol", _outlineSymbol);
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
            fac *= options.DpiFactor;

            HatchSymbol hSym = new HatchSymbol(_forecolor, _backcolor, _hatchStyle);
            if (_outlineSymbol != null)
            {
                hSym._outlineSymbol = (ISymbol)_outlineSymbol.Clone(options);
            }

            hSym.LegendLabel = _legendLabel;

            return hSym;
        }
        #endregion

        #region IBrushColor Member

        [Browsable(false)]
        public ArgbColor FillColor
        {
            get
            {
                return this.ForeColor;
            }
            set
            {
                this.ForeColor = value;
            }
        }

        #endregion

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
