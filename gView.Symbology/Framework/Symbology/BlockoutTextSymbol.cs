using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.GraphicsEngine.Extensions;
using System;
using System.ComponentModel;

namespace gView.Framework.Symbology
{
    [RegisterPlugInAttribute("A06F8B12-394E-4F8E-8B9A-6025F96F6F4F")]
    public class BlockoutTextSymbol : SimpleTextSymbol
    {
        protected IBrush _outlinebrush;

        public BlockoutTextSymbol()
            : base()
        {
            _outlinebrush = Current.Engine.CreateSolidBrush(ArgbColor.Yellow);
        }

        protected BlockoutTextSymbol(IFont font, ArgbColor color, ArgbColor outlinecolor)
            : base(font, color)
        {
            _outlinebrush = Current.Engine.CreateSolidBrush(outlinecolor);
        }

        [Browsable(true)]
        [UseColorPicker()]
        public ArgbColor ColorOutline
        {
            get { return _outlinebrush.Color; }
            set { _outlinebrush.Color = value; }
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
            get { return "Blockout Text Symbol"; }
        }

        #endregion ISymbol Members

        #region IClone2 Members

        public override object Clone()
        {
            BlockoutTextSymbol tSym = _font != null && _brush != null && _outlinebrush != null ?
                new BlockoutTextSymbol(Current.Engine.CreateFont(_font.Name, _font.Size, _font.Style), _brush.Color, _outlinebrush.Color) :
                new BlockoutTextSymbol();

            tSym.HorizontalOffset = HorizontalOffset;
            tSym.VerticalOffset = VerticalOffset;
            tSym.Angle = Angle;
            tSym._align = _align;
            tSym.Smoothingmode = this.Smoothingmode;
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

            BlockoutTextSymbol tSym = new BlockoutTextSymbol(Current.Engine.CreateFont(_font.Name, Math.Max(_font.Size * fac / display.Screen.LargeFontsFactor, 2f), _font.Style), _brush.Color, _outlinebrush.Color);
            tSym.HorizontalOffset = HorizontalOffset * fac;
            tSym.VerticalOffset = VerticalOffset * fac;
            tSym.Angle = Angle;
            tSym._align = _align;
            tSym.Smoothingmode = this.Smoothingmode;
            tSym.MinFontSize = this.MinFontSize;
            tSym.MaxFontSize = this.MaxFontSize;
            tSym.IncludesSuperScript = this.IncludesSuperScript;
            tSym.SecondaryTextSymbolAlignments = this.SecondaryTextSymbolAlignments;

            return tSym;
        }

        #endregion IClone2 Members

        #region IPersistable Members

        override public void Load(IPersistStream stream)
        {
            base.Load(stream);

            this.ColorOutline = ArgbColor.FromArgb((int)stream.Load("outlinecolor", ArgbColor.Yellow.ToArgb()));
        }

        override public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("outlinecolor", this.ColorOutline.ToArgb());
        }

        #endregion IPersistable Members

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

                    if (level < 0 || level == 0)
                    {
                        var size = display.Canvas.MeasureText(text, _font).AddPadding(_font);
                        var rect = new CanvasRectangleF(0f, 0f, size.Width, size.Height);

                        switch (format.Alignment)
                        {
                            case StringAlignment.Center:
                                rect.Offset(-size.Width / 2, 0f);
                                break;

                            case StringAlignment.Far:
                                rect.Offset(-size.Width, 0f);
                                break;
                        }
                        switch (format.LineAlignment)
                        {
                            case StringAlignment.Center:
                                rect.Offset(0f, -size.Height / 2);
                                break;

                            case StringAlignment.Far:
                                rect.Offset(0f, -size.Height);
                                break;
                        }

                        rect = rect.AddOffsetPadding(_font, format);

                        display.Canvas.SmoothingMode = (SmoothingMode)this.Smoothingmode;
                        display.Canvas.FillRectangle(_outlinebrush, rect);
                        display.Canvas.SmoothingMode = SmoothingMode.None;
                    }

                    if (level < 0 || level == 1)
                    {
                        display.Canvas.TextRenderingHint = (this.Smoothingmode == SymbolSmoothing.None) ?
                            TextRenderingHint.SystemDefault :
                            TextRenderingHint.AntiAlias;

                        DrawString(display.Canvas, text, _font, _brush, _xOffset, _yOffset, format);

                        display.Canvas.TextRenderingHint = TextRenderingHint.SystemDefault;
                    }
                }
                finally
                {
                    display.Canvas.ResetTransform();
                }
            }
        }
    }
}