using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.ComponentModel;

namespace gView.Framework.Symbology
{
    [gView.Framework.system.RegisterPlugIn("A5C2BB98-2B2D-4353-9262-4CDF4C7EB845")]
    public class GlowingTextSymbol : SimpleTextSymbol
    {
        protected IBrush _outlinebrush;
        private SymbolSmoothing _outlineSmothingMode = SymbolSmoothing.None;
        private int _outlineWidth = 1;

        public GlowingTextSymbol()
            : base()
        {
            _outlinebrush = Current.Engine.CreateSolidBrush(ArgbColor.Yellow);
        }
        protected GlowingTextSymbol(IFont font, ArgbColor color, ArgbColor outlinecolor)
            : base(font, color)
        {
            _outlinebrush = Current.Engine.CreateSolidBrush(outlinecolor);
        }

        [Browsable(true)]
        [UseColorPicker()]
        [Category("Glowing")]
        public ArgbColor GlowingColor
        {
            get { return _outlinebrush.Color; }
            set { _outlinebrush.Color = value; }
        }

        [Browsable(true)]
        [Category("Glowing")]
        public SymbolSmoothing GlowingSmoothingmode
        {
            get { return _outlineSmothingMode; }
            set { _outlineSmothingMode = value; }
        }

        [Browsable(true)]
        [Category("Glowing")]
        public int GlowingWidth
        {
            get { return _outlineWidth; }
            set { _outlineWidth = value; }
        }

        #region ISymbol Members

        override public void Release()
        {
            base.Release();
            if (_outlinebrush != null)
            {
                _outlinebrush.Dispose();
            }

            _outlinebrush = null;
        }

        [Browsable(false)]
        override public string Name
        {
            get { return "Glowing Text Symbol"; }
        }

        #endregion

        #region IClone2 Members

        public override object Clone()
        {
            GlowingTextSymbol tSym = _font != null && _brush != null && _outlinebrush != null ?
                new GlowingTextSymbol(Current.Engine.CreateFont(_font.Name, _font.Size, _font.Style), _brush.Color, _outlinebrush.Color) :
                new GlowingTextSymbol();

            tSym.HorizontalOffset = HorizontalOffset;
            tSym.VerticalOffset = VerticalOffset;
            tSym.Angle = Angle;
            tSym._align = _align;
            tSym.Smoothingmode = this.Smoothingmode;
            tSym.GlowingSmoothingmode = this.GlowingSmoothingmode;
            tSym.GlowingWidth = this.GlowingWidth;
            tSym.MinFontSize = this.MinFontSize;
            tSym.MaxFontSize = this.MaxFontSize;
            tSym.IncludesSuperScript = this.IncludesSuperScript;
            tSym.SecondaryTextSymbolAlignments = this.SecondaryTextSymbolAlignments;

            return tSym;
        }

        override public object Clone(CloneOptions options)
        {
            var display = options?.Display;

            if (display == null)
            {
                return this.Clone();
            }

            float fac = 1;
            if (options.ApplyRefScale)
            {
                fac = ReferenceScaleHelper.RefscaleFactor(
                    (float)(display.ReferenceScale / display.MapScale),
                    _font.Size,
                    MinFontSize,
                    MaxFontSize);

                fac = options.LabelRefScaleFactor(fac);
            }
            fac *= options.DpiFactor;

            GlowingTextSymbol tSym = new GlowingTextSymbol(Current.Engine.CreateFont(_font.Name, Math.Max(_font.Size * fac / display.Screen.LargeFontsFactor, 2f), _font.Style), _brush.Color, _outlinebrush.Color);
            tSym.HorizontalOffset = HorizontalOffset * fac;
            tSym.VerticalOffset = VerticalOffset * fac;
            tSym.Angle = Angle;
            tSym._align = _align;
            tSym.Smoothingmode = this.Smoothingmode;
            tSym.GlowingSmoothingmode = this.GlowingSmoothingmode;
            tSym.GlowingWidth = this.GlowingWidth;
            tSym.MinFontSize = this.MinFontSize;
            tSym.MaxFontSize = this.MaxFontSize;
            tSym.IncludesSuperScript = this.IncludesSuperScript;
            tSym.SecondaryTextSymbolAlignments = this.SecondaryTextSymbolAlignments;

            return tSym;
        }

        #endregion

        #region IPersistable Members

        override public void Load(IPersistStream stream)
        {
            base.Load(stream);

            this.GlowingColor = ArgbColor.FromArgb((int)stream.Load("outlinecolor", ArgbColor.Yellow.ToArgb()));
            this.GlowingSmoothingmode = (SymbolSmoothing)stream.Load("outlinesmoothing", (int)this.Smoothingmode);
            this.GlowingWidth = (int)stream.Load("outlinewidth", 1);
        }

        override public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("outlinecolor", this.GlowingColor.ToArgb());
            stream.Save("outlinesmoothing", (int)this.GlowingSmoothingmode);
            stream.Save("outlinewidth", this.GlowingWidth);
        }

        #endregion

        #region ISymbol Member

        [Browsable(false)]
        override public SymbolSmoothing SymbolSmothingMode
        {
            set
            {
                this.Smoothingmode = _outlineSmothingMode = value;
            }
        }

        #endregion

        override protected int DrawingLevels { get { return 2; } }
        override protected void DrawAtPoint(IDisplay display, IPoint point, string text, float angle, IDrawTextFormat format, int level)
        {
            if (_font != null)
            {
                //point.X+=_xOffset;
                //point.Y+=_yOffset;

                try
                {
                    display.Canvas.TranslateTransform(new CanvasPointF((float)point.X, (float)point.Y));
                    if (angle != 0 || _angle != 0 || _rotation != 0)
                    {
                        var transformRotation = angle + _angle + _rotation;

                        if (display.DisplayTransformation.UseTransformation)
                        {
                            transformRotation -= (float)display.DisplayTransformation.DisplayRotation;
                        }

                        display.Canvas.RotateTransform(transformRotation);
                    }

                    display.Canvas.TextRenderingHint = (this.GlowingSmoothingmode == SymbolSmoothing.None) ?
                        TextRenderingHint.SystemDefault :
                        TextRenderingHint.AntiAlias;

                    if (level < 0 || level == 0)
                    {
                        var outlineWidth = _outlineWidth;
                        if (outlineWidth == 0)
                        {
                            outlineWidth = (int)Math.Max(1f, Font.Size / 10f);
                        }

                        //if(display.GraphicsContext.TextRenderingHint== System.Drawing.Text.TextRenderingHint.AntiAlias)
                        //{
                        //    outlineWidth = Math.Max(2, outlineWidth);
                        //}

                        if (outlineWidth > 0)
                        {
                            for (int x = outlineWidth; x >= -outlineWidth; x--)
                            {
                                for (int y = outlineWidth; y >= -outlineWidth; y--)
                                {
                                    if (x == 0 && y == 0)
                                    {
                                        continue;
                                    }

                                    DrawString(display.Canvas, text, _font, _outlinebrush, (float)_xOffset + x, (float)_yOffset + y, format);
                                }
                            }
                        }
                    }

                    if (level < 0 || level == 1)
                    {
                        display.Canvas.TextRenderingHint = (this.Smoothingmode == SymbolSmoothing.None) ?
                            TextRenderingHint.SystemDefault :
                            TextRenderingHint.AntiAlias;

                        DrawString(display.Canvas, text, _font, _brush, (float)_xOffset, (float)_yOffset, format);
                    }

                }
                finally
                {
                    display.Canvas.ResetTransform();
                    display.Canvas.TextRenderingHint = TextRenderingHint.SystemDefault;
                }
            }
        }
    }
}
