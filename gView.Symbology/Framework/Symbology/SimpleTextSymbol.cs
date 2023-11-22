using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.GraphicsEngine.Extensions;
using gView.Symbology.Framework.Symbology.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace gView.Framework.Symbology
{
    [gView.Framework.system.RegisterPlugIn("A5DA4D8D-879F-41a5-9795-F22BE5B85877")]
    public class SimpleTextSymbol : Symbol, ITextSymbol, IPropertyPage, IFontColor, IFontSymbol
    {
        protected string _text;
        protected IFont _font;
        protected IBrush _brush;
        protected float _xOffset = 0, _yOffset = 0, _angle = 0, _hOffset = 0, _vOffset = 0, _rotation = 0;
        protected TextSymbolAlignment _align = TextSymbolAlignment.Center;

        public SimpleTextSymbol()
        {
            _font = Current.Engine.CreateFont("Arial", 10);
            _brush = Current.Engine.CreateSolidBrush(ArgbColor.Black);

            this.ShowInTOC = false;
        }

        protected SimpleTextSymbol(IFont font, ArgbColor color)
        {
            _font = font;
            _brush = Current.Engine.CreateSolidBrush(color);

            this.ShowInTOC = false;
        }

        [Browsable(true)]
        public IFont Font
        {
            get { return _font; }
            set
            {
                if (_font == value)
                {
                    return;
                }

                if (_font != null)
                {
                    _font.Dispose();
                }

                _font = value;
                _measureTextWidth = null;
            }
        }

        [Browsable(true)]
        //[Editor(typeof(gView.Framework.UI.ColorTypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [UseColorPicker()]
        public ArgbColor Color
        {
            get { return _brush.Color; }
            set { _brush.Color = value; }
        }

        virtual protected int DrawingLevels { get { return 1; } }
        private void DrawAtPoint(IDisplay display, IPoint point, string text, float angle, IDrawTextFormat format)
        {
            DrawAtPoint(display, point, text, angle, format, -1);
        }
        virtual protected void DrawAtPoint(IDisplay display, IPoint point, string text, float angle, IDrawTextFormat format, int level)
        {
            if (_font != null)
            {
                //point.X+=_xOffset;
                //point.Y+=_yOffset;

                try
                {
                    display.Canvas.TextRenderingHint =
                        ((this.Smoothingmode == SymbolSmoothing.None) ?
                            TextRenderingHint.SystemDefault :
                            TextRenderingHint.AntiAlias);

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
                    DrawString(display.Canvas, text, _font, _brush, _xOffset, _yOffset, format);
                }
                finally
                {
                    display.Canvas.ResetTransform();
                    display.Canvas.TextRenderingHint = TextRenderingHint.SystemDefault;
                }
            }
        }

        private IDrawTextFormat StringFormatFromAlignment(TextSymbolAlignment symbolAlignment)
        {

            var format = Current.Engine.CreateDrawTextFormat();
            switch (symbolAlignment)
            {
                case TextSymbolAlignment.rightAlignOver:
                    format.Alignment = StringAlignment.Far;
                    format.LineAlignment = StringAlignment.Far;
                    break;
                case TextSymbolAlignment.rightAlignCenter:
                    format.Alignment = StringAlignment.Far;
                    format.LineAlignment = StringAlignment.Center;
                    break;
                case TextSymbolAlignment.rightAlignUnder:
                    format.Alignment = StringAlignment.Far;
                    format.LineAlignment = StringAlignment.Near;
                    break;
                case TextSymbolAlignment.Over:
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Far;
                    break;
                case TextSymbolAlignment.Center:
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    break;
                case TextSymbolAlignment.Under:
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Near;
                    break;
                case TextSymbolAlignment.leftAlignOver:
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Far;
                    break;
                case TextSymbolAlignment.leftAlignCenter:
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Center;
                    break;
                case TextSymbolAlignment.leftAlignUnder:
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Near;
                    break;
            }
            return format;
        }

        public bool SupportsGeometryType(GeometryType geomType) => true;

        #region ILabel Members

        [Browsable(false)]
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _measureTextWidth = null;
                    _measureStringSize = null;
                    _text = PrepareText(value);
                }
            }
        }
        [Browsable(false)]
        public TextSymbolAlignment TextSymbolAlignment
        {
            get { return _align; }
            set { _align = value; }
        }

        [Browsable(false)]
        public TextSymbolAlignment[] SecondaryTextSymbolAlignments { get; set; }

        private IDisplayCharacterRanges _measureTextWidth = null;
        public IDisplayCharacterRanges MeasureCharacterWidth(IDisplay display)
        {
            if (_measureTextWidth != null)
            {
                return _measureTextWidth;
            }

            var format = StringFormatFromAlignment(_align);
            IDisplayCharacterRanges ranges = display.Canvas.DisplayCharacterRanges(_font, format, _text);

            return ranges;
        }

        public List<IAnnotationPolygonCollision> AnnotationPolygon(IDisplay display, IGeometry geometry, TextSymbolAlignment symbolAlignment)
        {
            if (_font == null || _text == null || display.Canvas == null)
            {
                return null;
            }

            List<IAnnotationPolygonCollision> polygons = new List<IAnnotationPolygonCollision>();
            if (geometry is IPoint)
            {
                #region IPoint

                double x = ((IPoint)geometry).X;
                double y = ((IPoint)geometry).Y;
                display.World2Image(ref x, ref y);

                var annotationPolygon = AnnotationPolygon(display, (float)x, (float)y, symbolAlignment);
                annotationPolygon.Rotate((float)x, (float)y, Angle);

                polygons.Add(annotationPolygon);

                #endregion
            }
            else if (geometry is IMultiPoint)
            {
                #region IMultipoint
                IMultiPoint pColl = (IMultiPoint)geometry;
                for (int i = 0; i < pColl.PointCount; i++)
                {
                    double x = pColl[i].X;
                    double y = pColl[i].Y;

                    display.World2Image(ref x, ref y);

                    var annotationPolygon = AnnotationPolygon(display, (float)x, (float)y, symbolAlignment);
                    annotationPolygon.Rotate((float)x, (float)y, Angle);

                    polygons.Add(annotationPolygon);
                }
                #endregion
            }
            else if (geometry is IDisplayPath)
            {
                if (String.IsNullOrEmpty(_text))
                {
                    return null;
                }

                IDisplayPath path = (IDisplayPath)geometry;

                #region Text On Path

                var format = StringFormatFromAlignment(_align);

                IDisplayCharacterRanges ranges = this.MeasureCharacterWidth(display);
                float sizeW = ranges.Width;
                float stat0 = path.Chainage, stat1 = stat0 + sizeW, stat = stat0;

                if (stat0 < 0)
                {
                    return null;
                }

                #region Richtung des Textes

                CanvasPointF? p1_ = path.PointAt(stat0); //SpatialAlgorithms.Algorithm.DisplayPathPoint(path, stat0);
                CanvasPointF? p2_ = path.PointAt(stat1); //SpatialAlgorithms.Algorithm.DisplayPathPoint(path, stat1);
                if (!p1_.HasValue || !p2_.HasValue)
                {
                    return null;
                }

                if (p1_.Value.X > p2_.Value.X)
                {
                    #region Swap Path Direction

                    path.ChangeDirection();
                    stat = stat0 = path.Length - stat1;
                    stat1 = stat0 + sizeW;

                    #endregion
                }

                #endregion

                AnnotationPolygonCollection charPolygons = new AnnotationPolygonCollection();
                float x, y, angle;

                for (int i = 0; i < _text.Length; i++)
                {
                    CanvasRectangleF cSize = ranges[i];
                    int counter = 0;

                    while (true)
                    {
                        CanvasPointF? p1 = path.PointAt(stat); //SpatialAlgorithms.Algorithm.DisplayPathPoint(path, stat);
                        CanvasPointF? p2 = path.PointAt(stat + cSize.Width);
                        if (!p1.HasValue || !p2.HasValue)
                        {
                            return null;
                        }

                        angle = (float)(Math.Atan2(p2.Value.Y - p1.Value.Y, p2.Value.X - p1.Value.X) * 180.0 / Math.PI);

                        //float ccc = 0f;
                        //angle = 0f;
                        //for (float xxx = 0f; xxx <= cSize.Width; xxx += cSize.Width / 10f, ccc += 1f)
                        //{
                        //    p2 = path.PointAt(stat + xxx/*cSize.Width*/); //SpatialAlgorithms.Algorithm.DisplayPathPoint(path, stat + cSize.Width);
                        //    if (p1 == null || p2 == null)
                        //        return null;

                        //    angle += (float)(Math.Atan2(((PointF)p2).Y - ((PointF)p1).Y, ((PointF)p2).X - ((PointF)p1).X) * 180.0 / Math.PI);
                        //}
                        //angle /= ccc;

                        x = p1.Value.X; y = p1.Value.Y;
                        AnnotationPolygon polygon = null;
                        switch (format.LineAlignment)
                        {
                            case StringAlignment.Near:
                                polygon = new Symbology.AnnotationPolygon((float)x + .1f * cSize.Width, (float)y + .2f * cSize.Height,
                                                                           .8f * cSize.Width, .6f * cSize.Height);
                                break;
                            case StringAlignment.Far:
                                polygon = new Symbology.AnnotationPolygon((float)x + .1f * cSize.Width, (float)y - .8f * cSize.Height,
                                                                           .8f * cSize.Width, .6f * cSize.Height);
                                break;
                            default:
                                polygon = new Symbology.AnnotationPolygon((float)x + .1f * cSize.Width, (float)y - .6f * cSize.Height / 2f,
                                                                           .8f * cSize.Width, .6f * cSize.Height);
                                break;
                        }
                        polygon.Rotate((float)x, (float)y, Angle + angle);
                        if (charPolygons.CheckCollision(polygon))
                        {
                            stat += cSize.Width / 10.0f;
                            counter++;
                            if (counter > 7)
                            {
                                return null;
                            }

                            continue;
                        }
                        charPolygons.Add(polygon);

                        //using (System.Drawing.Drawing2D.GraphicsPath grpath = new System.Drawing.Drawing2D.GraphicsPath())
                        //{
                        //    grpath.StartFigure();
                        //    grpath.AddLine(polygon[0], polygon[1]);
                        //    grpath.AddLine(polygon[1], polygon[2]);
                        //    grpath.AddLine(polygon[2], polygon[3]);
                        //    grpath.CloseFigure();

                        //    display.GraphicsContext.FillPath(Brushes.Aqua, grpath);
                        //}

                        break;
                    }
                    stat += cSize.Width;// +_font.Height * 0.1f;
                }

                if (charPolygons.Count > 0)
                {
                    #region Glättung

                    //for (int i = 1; i < charPolygons.Count - 1; i++)
                    //{
                    //    double angle0 = ((AnnotationPolygon)charPolygons[i - 1]).Angle;
                    //    double angle2 = ((AnnotationPolygon)charPolygons[i + 1]).Angle;
                    //    ((AnnotationPolygon)charPolygons[i]).Angle = (angle0 + angle2) * 0.5;

                    //    float x0 = ((AnnotationPolygon)charPolygons[i - 1]).X1;
                    //    float y0 = ((AnnotationPolygon)charPolygons[i - 1]).Y1;
                    //    float x2 = ((AnnotationPolygon)charPolygons[i + 1]).X1;
                    //    float y2 = ((AnnotationPolygon)charPolygons[i + 1]).Y1;
                    //    ((AnnotationPolygon)charPolygons[i]).X1 = (x0 + x2) * 0.5f;
                    //    ((AnnotationPolygon)charPolygons[i]).Y1 = (y0 + y2) * 0.5f;
                    //}

                    #endregion

                    polygons.Add(charPolygons);
                    path.AnnotationPolygonCollision = charPolygons;
                }
                #endregion
            }
            else if (geometry is IPolyline)
            {
                IPolyline pLine = (IPolyline)geometry;
                for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                {
                    IPath path = pLine[iPath];
                    if (path.PointCount == 0)
                    {
                        continue;
                    }

                    #region Simple Method
                    IPoint p1 = path[0], p2;
                    for (int iPoint = 1; iPoint < path.PointCount; iPoint++)
                    {
                        p2 = path[iPoint];
                        double angle = -Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI;

                        //if (display.DisplayTransformation.UseTransformation)
                        //{
                        //    angle -= display.DisplayTransformation.DisplayRotation;
                        //}

                        if (angle < 0)
                        {
                            angle += 360;
                        }

                        if (angle > 90 && angle < 270)
                        {
                            angle -= 180;
                        }

                        var format = StringFormatFromAlignment(_align);
                        double x, y;
                        if (format.Alignment == StringAlignment.Center)
                        {
                            x = p1.X * 0.5 + p2.X * 0.5;
                            y = p1.Y * 0.5 + p2.Y * 0.5;
                        }
                        else if (format.Alignment == StringAlignment.Far)
                        {
                            x = p2.X;
                            y = p2.Y;
                        }
                        else
                        {
                            x = p1.X;
                            y = p1.Y;
                        }
                        display.World2Image(ref x, ref y);

                        var annotationPolygon = AnnotationPolygon(display, (float)x, (float)y, symbolAlignment);
                        annotationPolygon.Rotate((float)x, (float)y, Angle);

                        polygons.Add(annotationPolygon);
                        p1 = p2;
                    }
                    #endregion
                }
            }

            return polygons.Count > 0 ? polygons : null;
        }
        #endregion

        private AnnotationPolygon AnnotationPolygon(IDisplay display, float x, float y, TextSymbolAlignment symbolAlignment)
        {
            return AnnotationPolygon(display, _text, x, y, symbolAlignment);
        }

        private CanvasSizeF? _measureStringSize = null;

        private CanvasSizeF MeasureStringSize(IDisplay display, string text)
        {
            if (text == _text && _measureStringSize.HasValue)
            {
                return _measureStringSize.Value;
            }
            else
            {
                CanvasSizeF size = display.Canvas.MeasureText(text, _font);
                if (text == _text)
                {
                    _measureStringSize = size;
                }
                return size;
            }
        }

        private AnnotationPolygon AnnotationPolygon(IDisplay display, string text, float x, float y, TextSymbolAlignment symbolAlignment)
        {

            var size = MeasureStringSize(display, text);

            return AnnotationPolygon(display, size, x, y, symbolAlignment);
        }

        private AnnotationPolygon AnnotationPolygon(IDisplay display, CanvasSizeF size, float x, float y, TextSymbolAlignment alignment)
        {
            float x1 = 0, y1 = 0;
            switch (alignment)
            {
                case TextSymbolAlignment.rightAlignOver:
                    x1 = x - size.Width;
                    y1 = y - size.Height;
                    break;
                case TextSymbolAlignment.rightAlignCenter:
                    x1 = x - size.Width;
                    y1 = y - size.Height / 2f;
                    break;
                case TextSymbolAlignment.rightAlignUnder:
                    x1 = x - size.Width;
                    y1 = y;
                    break;
                case TextSymbolAlignment.Over:
                    x1 = x - size.Width / 2f;
                    y1 = y - size.Height;
                    break;
                case TextSymbolAlignment.Center:
                    x1 = x - size.Width / 2f;
                    y1 = y - size.Height / 2f;
                    break;
                case TextSymbolAlignment.Under:
                    x1 = x - size.Width / 2f;
                    y1 = y;
                    break;
                case TextSymbolAlignment.leftAlignOver:
                    x1 = x;
                    y1 = y - size.Height;
                    break;
                case TextSymbolAlignment.leftAlignCenter:
                    x1 = x;
                    y1 = y - size.Height / 2f;
                    break;
                case TextSymbolAlignment.leftAlignUnder:
                    x1 = x;
                    y1 = y;
                    break;
            }

            return new AnnotationPolygon(x1, y1, size.Width, size.Height);
        }

        #region ITextSymbol

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
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
                SymbolTransformation.Transform(_angle, _hOffset, _vOffset, out _xOffset, out _yOffset);
            }
        }

        [Browsable(true)]
        [Category("Reference Scaling")]
        public float MaxFontSize { get; set; }

        [Browsable(true)]
        [Category("Reference Scaling")]
        public float MinFontSize { get; set; }

        [Browsable(true)]
        public bool IncludesSuperScript { get; set; }

        #endregion

        #region ISymbol Members

        public void Draw(IDisplay display, IGeometry geometry)
        {
            Draw(display, geometry, _align);
        }

        public void Draw(IDisplay display, IGeometry geometry, TextSymbolAlignment symbolAlignment)
        {
            if (_font == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                #region IPoint

                double x = ((IPoint)geometry).X;
                double y = ((IPoint)geometry).Y;
                display.World2Image(ref x, ref y);
                IPoint p = new gView.Framework.Geometry.Point(x, y);

                DrawAtPoint(display, p, _text, 0, StringFormatFromAlignment(symbolAlignment));

                #endregion
            }
            if (geometry is IMultiPoint)
            {
                #region IMultiPoint

                IMultiPoint pColl = (IMultiPoint)geometry;
                for (int i = 0; i < pColl.PointCount; i++)
                {
                    double x = pColl[i].X;
                    double y = pColl[i].Y;

                    display.World2Image(ref x, ref y);
                    IPoint p = new gView.Framework.Geometry.Point(x, y);

                    DrawAtPoint(display, p, _text, 0, StringFormatFromAlignment(symbolAlignment));
                }

                #endregion
            }
            else if (geometry is IDisplayPath && ((IDisplayPath)geometry).AnnotationPolygonCollision is AnnotationPolygonCollection)
            {
                if (String.IsNullOrEmpty(_text))
                {
                    return;
                }

                IDisplayPath path = (IDisplayPath)geometry;
                AnnotationPolygonCollection apc = path.AnnotationPolygonCollision as AnnotationPolygonCollection;
                var format = StringFormatFromAlignment(symbolAlignment);
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Center;

                float displayRotation = display.DisplayTransformation.UseTransformation ?
                    (float)display.DisplayTransformation.DisplayRotation :
                    0f;

                if (_text.Length == apc.Count)
                {
                    int drawingLevels = this.DrawingLevels; // Für Blockout und Glowing Text -> Zuerst den Blockout für alle Zeichen...
                    for (int level = 0; level < drawingLevels; level++)
                    {
                        for (int i = 0; i < _text.Length; i++)
                        {
                            AnnotationPolygon ap = apc[i] as AnnotationPolygon;
                            if (ap != null)
                            {
                                var centerPoint = ap.CenterPoint;

                                // (+) displayRotation hier => (-) in DrawAtPoint => makes it neutral for Paths...
                                float apAngle = (float)ap.Angle + displayRotation;

                                DrawAtPoint(display, new Geometry.Point(centerPoint.X, centerPoint.Y), _text[i].ToString(), apAngle, format, level);
                            }
                        }
                    }
                }
            }
            else if (geometry is IPolyline)
            {
                IPolyline pLine = (IPolyline)geometry;
                for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                {
                    IPath path = pLine[iPath];
                    if (path.PointCount == 0)
                    {
                        continue;
                    }

                    #region Parallel Text

                    IPoint p1 = path[0], p2;
                    for (int iPoint = 1; iPoint < path.PointCount; iPoint++)
                    {
                        p2 = path[iPoint];
                        double angle = -Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI;

                        //if (display.DisplayTransformation.UseTransformation)
                        //{
                        //    angle -= display.DisplayTransformation.DisplayRotation;
                        //}

                        var format = StringFormatFromAlignment(symbolAlignment);

                        if (angle < 0)
                        {
                            angle += 360;
                        }

                        if (angle > 90 && angle < 270)
                        {
                            if (format.Alignment != StringAlignment.Center)
                            {
                                // swap points & alignment
                                var p_ = p1;
                                p1 = p2;
                                p2 = p_;
                                if (format.Alignment == StringAlignment.Far)
                                {
                                    format.Alignment = StringAlignment.Near;
                                }
                                else if (format.Alignment == StringAlignment.Near)
                                {
                                    format.Alignment = StringAlignment.Far;
                                }
                            }
                            angle -= 180;
                        }


                        double x, y;
                        if (format.Alignment == StringAlignment.Center)
                        {
                            x = p1.X * 0.5 + p2.X * 0.5;
                            y = p1.Y * 0.5 + p2.Y * 0.5;
                        }
                        else if (format.Alignment == StringAlignment.Far)
                        {
                            x = p2.X;
                            y = p2.Y;
                        }
                        else
                        {
                            x = p1.X;
                            y = p1.Y;
                        }
                        display.World2Image(ref x, ref y);
                        IPoint p = new gView.Framework.Geometry.Point(x, y);

                        //_text += "  " + angle.ToString();
                        DrawAtPoint(display, p, _text, (float)angle, format);
                        p1 = p2;
                    }

                    #endregion
                }
            }
            else if (geometry is IPolygon)
            {
                /*
                GraphicsPath path = DisplayOperations.Geometry2GraphicsPath(display, geometry);

                foreach (PointF point in gView.SpatialAlgorithms.Algorithm.LabelPoints(path))
                {
                    DrawAtPoint(display, new gView.Framework.Geometry.Point(point.X, point.Y), 0, stringFormatFromAlignment);
                }
                 * */
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    Draw(display, ((IAggregateGeometry)geometry)[i]);
                }
            }
        }

        virtual public void Release()
        {
            if (_font != null)
            {
                _font.Dispose();
            }

            _font = null;
            if (_brush != null)
            {
                _brush.Dispose();
            }

            _brush = null;
        }

        [Browsable(false)]
        virtual public string Name
        {
            get { return "Simple Text Symbol"; }
        }

        #endregion

        #region IPersistable Members

        new virtual public void Load(IPersistStream stream)
        {
            base.Load(stream);

            string err = "";
            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                ASCIIEncoding encoder = new ASCIIEncoding();
                string soap = (string)stream.Load("font");

                // 
                // Im Size muss Punkt und Komma mit Systemeinstellungen
                // übereinstimmen
                //
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(soap);
                XmlNode sizeNode = doc.SelectSingleNode("//Size");
                if (sizeNode != null)
                {
                    sizeNode.InnerText = sizeNode.InnerText.Replace(".", ",");
                }

                soap = doc.OuterXml;
                //
                //
                //

                ms.Write(encoder.GetBytes(soap), 0, soap.Length);
                ms.Position = 0;
                SoapFormatter formatter = new SoapFormatter();
                _font = (IFont)formatter.Deserialize<IFont>(ms, stream, this);
            }
            catch (Exception ex)
            {
                err = ex.Message;
                ex = null;
            }
            this.Color = ArgbColor.FromArgb((int)stream.Load("color", ArgbColor.Red.ToArgb()));

            HorizontalOffset = (float)stream.Load("xOffset", (float)0);
            VerticalOffset = (float)stream.Load("yOffset", (float)0);
            Angle = (float)stream.Load("Angle", (float)0);
            _align = (TextSymbolAlignment)stream.Load("Alignment", (int)TextSymbolAlignment.Center);

            var secAlignments = (string)stream.Load("secAlignments", null);
            if (!String.IsNullOrWhiteSpace(secAlignments))
            {
                try
                {
                    this.SecondaryTextSymbolAlignments = secAlignments.Split(',')
                        .Select(s => (TextSymbolAlignment)int.Parse(s))
                        .Distinct()
                        .ToArray();
                }
                catch { }
            }

            this.MaxFontSize = (float)stream.Load("maxfontsize", 0f);
            this.MinFontSize = (float)stream.Load("minfontsize", 0f);
            this.IncludesSuperScript = (bool)stream.Load("includessuperscript", false);
        }

        new virtual public void Save(IPersistStream stream)
        {
            base.Save(stream);

            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                SoapFormatter formatter = new SoapFormatter();
                formatter.Serialize(ms, _font);
                ms.Position = 0;
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] b = new byte[ms.Length];
                ms.Read(b, 0, b.Length);
                string soap = encoder.GetString(b);

                stream.Save("font", soap);
            }
            catch { }
            stream.Save("color", this.Color.ToArgb());

            stream.Save("xOffset", HorizontalOffset);
            stream.Save("yOffset", VerticalOffset);
            stream.Save("Angle", Angle);
            stream.Save("Alignment", (int)_align);

            if (this.SecondaryTextSymbolAlignments != null && this.SecondaryTextSymbolAlignments.Distinct().Count() > 1)
            {
                var secAlignments = String.Join(",", this.SecondaryTextSymbolAlignments.Select(s => ((int)s).ToString()).Distinct());
                stream.Save("secAlignments", secAlignments);
            }

            stream.Save("maxfontsize", this.MaxFontSize);
            stream.Save("minfontsize", this.MinFontSize);
            stream.Save("includessuperscript", this.IncludesSuperScript);
        }

        #endregion

        #region IClone2 Members

        public override object Clone()
        {
            SimpleTextSymbol tSym = _font != null && _brush != null ?
                new SimpleTextSymbol(Current.Engine.CreateFont(_font.Name, _font.Size, _font.Style), _brush.Color) :
                new SimpleTextSymbol();

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

        virtual public object Clone(CloneOptions options)
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

            SimpleTextSymbol tSym = new SimpleTextSymbol(Current.Engine.CreateFont(_font.Name, Math.Max(_font.Size * fac / display.Screen.LargeFontsFactor, 2), _font.Style), _brush.Color);
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

        #endregion

        #region IPropertyPage Members

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Symbology.UI.dll");

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SimpleTextSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        public object PropertyPageObject()
        {
            return null;
        }

        #endregion

        #region ISymbolRotation Member

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

        #region IFontColor Member

        [Browsable(false)]
        public ArgbColor FontColor
        {
            get
            {
                return this.Color;
            }
            set
            {
                this.Color = value;
            }
        }

        #endregion

        #region ISymbol Member

        [Browsable(false)]
        virtual public SymbolSmoothing SymbolSmothingMode
        {
            set { this.Smoothingmode = value; }
        }

        public bool RequireClone()
        {
            return false;
        }

        #endregion

        #region Helper

        protected void DrawString(ICanvas canvas, string text, IFont font, IBrush brush, float xOffset, float yOffset, IDrawTextFormat format)
        {
            float alignXOffset = xOffset,
                  alignYOffset = yOffset;

            if (format.Alignment == StringAlignment.Far)
            {
                alignXOffset = -alignXOffset;
            }
            if (format.LineAlignment == StringAlignment.Near)
            {
                alignYOffset = -alignYOffset;
            }

            if (IncludesSuperScript == true)
            {
                if (!text.Contains("^"))
                {
                    canvas.DrawText(text, font, brush, alignXOffset, alignYOffset, format);
                }
                else
                {
                    float fontSize = font.Size;
                    while (!String.IsNullOrWhiteSpace(text))
                    {
                        var pos1 = text.IndexOf("^");
                        var pos2 = text.IndexOf("~");
                        int pos;
                        bool superScript = true;

                        if (pos1 >= 0 && pos2 >= 0)
                        {
                            if (pos1 < pos2)
                            {
                                pos = pos1;
                            }
                            else
                            {
                                pos = pos2;
                                superScript = false;
                            }
                        }
                        else if (pos1 >= 0 && pos2 < 0)
                        {
                            pos = pos1;
                        }
                        else if (pos2 >= 0 && pos1 < 0)
                        {
                            pos = pos2;
                            superScript = false;
                        }
                        else
                        {
                            pos = -1;
                        }

                        string subText = String.Empty;
                        if (pos < 0)
                        {
                            subText = text;
                        }
                        if (pos > 0)
                        {
                            subText = text.Substring(0, text.IndexOf(superScript ? "^" : "~"));
                        }

                        using (var subFont = Current.Engine.CreateFont(font.Name, fontSize, font.Style))
                        {
                            canvas.DrawText(subText, subFont, brush, alignXOffset, alignYOffset, format);
                            var size = canvas.MeasureText(subText, subFont)
                                             .AddPadding(subFont);

                            if (!String.IsNullOrWhiteSpace(subText))
                            {
                                alignXOffset += size.Width - subFont.Size * .2f;
                            }

                            if (superScript)
                            {
                                alignYOffset -= subFont.Size * .4f;
                            }
                            else
                            {
                                alignYOffset += (subFont.Size / .9f) * .4f;
                            }
                        }

                        if (pos >= 0)
                        {
                            text = text.Substring(pos + 1);
                            if (superScript)
                            {
                                fontSize *= .9f;
                            }
                            else
                            {
                                fontSize /= .9f;
                            }
                        }
                        else
                        {
                            text = String.Empty;
                        }
                    }
                }
            }
            else
            {
                canvas.DrawText(text, font, brush, alignXOffset, alignYOffset, format);
            }
        }

        private bool _wrapText = false;
        protected string PrepareText(string text)
        {
            if (IncludesSuperScript == true)
            {
                return text;
            }

            if (_wrapText == true)
            {
                text = text
                    .Replace(" ", "\n")
                    .Replace("-", "-\n")
                    .Replace("/", "/\n");
            }

            return text;
        }

        #endregion
    }
}
