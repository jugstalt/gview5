using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
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
    [gView.Framework.system.RegisterPlugIn("71E22086-D511-4a41-AAE1-BBC78572F277")]
    public sealed class TrueTypeMarkerSymbol : LegendItem,
                                               IPropertyPage,
                                               IPointSymbol,
                                               ISymbolRotation,
                                               IFontColor,
                                               ISymbolPositioningUI,
                                               ISymbolSize,
                                               ISymbolCurrentGraphicsEngineDependent
    {
        private float _xOffset = 0, _yOffset = 0, _angle = 0, _rotation = 0, _hOffset = 0, _vOffset = 0;
        private IBrush _brush;
        private IFont _font;
        private char _char = 'A';
        private Dictionary<string, Offset> _engineOffset = new Dictionary<string, Offset>();

        public TrueTypeMarkerSymbol()
        {
            _brush = Current.Engine.CreateSolidBrush(ArgbColor.Black);
            _font = Current.Engine.CreateFont("Arial", 10f);
        }
        private TrueTypeMarkerSymbol(IFont font, ArgbColor color)
        {
            _brush = Current.Engine.CreateSolidBrush(color);
            _font = font;
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

        public IFont Font
        {
            get { return _font; }
            set
            {
                if (_font != null)
                {
                    _font.Dispose();
                }

                _font = value;
            }
        }

        [Browsable(true)]
        [UseCharacterPicker()]
        public byte Charakter
        {
            get { return (byte)_char; }
            set { _char = (char)value; }
        }

        #region IPointSymbol Member

        private double _lastRotation = 0;

        public void DrawPoint(IDisplay display, IPoint point)
        {
            if (_font != null)
            {
                //point.X+=_xOffset;
                //point.Y+=_yOffset;

                display.Canvas.TextRenderingHint = GraphicsEngine.TextRenderingHint.AntiAlias;
                var format = Current.Engine.CreateDrawTextFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                //format.FormatFlags = System.Drawing.StringFormatFlags.DirectionRightToLeft;

                try
                {
                    var transformRatation = _angle + _rotation;
                    if (display.DisplayTransformation.UseTransformation)
                    {
                        transformRatation -= (float)display.DisplayTransformation.DisplayRotation;
                    }

                    display.Canvas.TranslateTransform(new CanvasPointF((float)point.X, (float)point.Y));
                    display.Canvas.RotateTransform(transformRatation);

                    double xo = _xOffset, yo = _yOffset;

                    if (_angle != 0 || _rotation != 0 || _lastRotation != 0)
                    {
                        if (_rotation != _lastRotation)
                        {
                            PerformSymbolTransformation(_rotation);
                            _lastRotation = _rotation;
                        }

                        double cos_a = Math.Cos((-_angle - _rotation) / 180.0 * Math.PI);
                        double sin_a = Math.Sin((-_angle - _rotation) / 180.0 * Math.PI);

                        xo = _xOffset * cos_a + _yOffset * sin_a;
                        yo = -_xOffset * sin_a + _yOffset * cos_a;
                    }

                    display.Canvas.DrawText(_char.ToString(), _font, _brush, (float)xo, (float)yo, format);
                }
                finally
                {
                    display.Canvas.ResetTransform();
                }
            }
        }

        [Browsable(false)]
        public float HorizontalOffset
        {
            get { return _hOffset; }
            set
            {
                _hOffset = value;
                RefreshEngineOffset();
                PerformSymbolTransformation(0);
            }
        }

        [Browsable(false)]
        public float VerticalOffset
        {
            get { return _vOffset; }
            set
            {
                _vOffset = value;
                RefreshEngineOffset();
                PerformSymbolTransformation(0);
            }
        }

        [Browsable(false)]
        public float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                PerformSymbolTransformation(0);
            }
        }

        #endregion

        #region ISymbol Member

        public void Release()
        {
            if (_brush != null)
            {
                _brush.Dispose();
            }

            _brush = null;
            if (_font != null)
            {
                _font.Dispose();
            }

            _font = null;
        }

        [Browsable(false)]
        public string Name
        {
            get
            {
                return "True Type Marker Symbol";
            }
        }

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

        #endregion

        #region IPersistable Member

        public new void Load(IPersistStream stream)
        {
            base.Load(stream);

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
                _font = (IFont)formatter.Deserialize<IFont>(ms, stream, this, true);
            }
            catch { }

            _char = (char)stream.Load("char", 'A');
            _brush.Color = ArgbColor.FromArgb((int)stream.Load("color", ArgbColor.Black.ToArgb()));
            var defaultHorizontalOffset = (float)stream.Load("x", 0f);
            var defaultVerticalOffset = (float)stream.Load("y", 0f);
            Angle = (float)stream.Load("a", 0f);

            this.MaxSymbolSize = (float)stream.Load("maxsymbolsize", 0f);
            this.MinSymbolSize = (float)stream.Load("minsymbolsize", 0f);

            #region Load Offset

            _engineOffset.Clear();
            string engineOffsetKeys = (string)stream.Load("engine-offset-keys", String.Empty);
            if (!String.IsNullOrEmpty(engineOffsetKeys))
            {
                foreach (var key in engineOffsetKeys.Split(','))
                {
                    _engineOffset[key] = new Offset()
                    {
                        HorizontalOffset = (float)stream.Load($"{key}.x", defaultHorizontalOffset),
                        VerticalOffset = (float)stream.Load($"{key}.y", defaultVerticalOffset)
                    };
                }
            }
            foreach (var engineName in Engines.RegisteredGraphicsEngineNames())
            {
                if (!_engineOffset.ContainsKey(engineName))
                {
                    _engineOffset[engineName] = new Offset()
                    {
                        HorizontalOffset = defaultHorizontalOffset,
                        VerticalOffset = defaultVerticalOffset
                    };
                }
            }

            #endregion

            SetCurrentEngineOffset();
        }

        public new void Save(IPersistStream stream)
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
            stream.Save("char", _char);
            stream.Save("color", _brush.Color.ToArgb());
            stream.Save("x", HorizontalOffset);
            stream.Save("y", VerticalOffset);
            stream.Save("a", Angle);

            stream.Save("maxsymbolsize", this.MaxSymbolSize);
            stream.Save("minsymbolsize", this.MinSymbolSize);

            if (_engineOffset != null && _engineOffset.Count > 0)
            {
                stream.Save("engine-offset-keys", String.Join(",", _engineOffset.Select(e => e.Key)));
                foreach (var key in _engineOffset.Keys)
                {
                    stream.Save($"{key}.x", _engineOffset[key].HorizontalOffset);
                    stream.Save($"{key}.y", _engineOffset[key].VerticalOffset);
                }
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

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SimplePointSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        #endregion

        #region IClone2

        public override object Clone()
        {
            TrueTypeMarkerSymbol marker = Font != null && _brush != null ?
                new TrueTypeMarkerSymbol(Current.Engine.CreateFont(Font.Name, Font.Size, Font.Style), _brush.Color) :
                new TrueTypeMarkerSymbol();

            marker.Angle = Angle;
            marker.HorizontalOffset = HorizontalOffset;
            marker.VerticalOffset = VerticalOffset;

            marker._char = _char;
            marker.LegendLabel = _legendLabel;

            marker.MaxSymbolSize = this.MaxSymbolSize;
            marker.MinSymbolSize = this.MinSymbolSize;

            CloneEngineOffsets(marker._engineOffset);

            return marker;
        }

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
                    this.SymbolSize,
                    this.MinSymbolSize,
                    this.MaxSymbolSize);

                fac = options.RefScaleFactor(fac);
            }
            fac *= options.DpiFactor;

            TrueTypeMarkerSymbol marker = new TrueTypeMarkerSymbol(Current.Engine.CreateFont(Font.Name, Math.Max(Font.Size * fac / display.Screen.LargeFontsFactor, 2f), _font.Style), _brush.Color);
            marker.Angle = Angle;
            marker.HorizontalOffset = HorizontalOffset * fac;
            marker.VerticalOffset = VerticalOffset * fac;

            marker._char = _char;
            marker.LegendLabel = _legendLabel;

            marker.MaxSymbolSize = this.MaxSymbolSize;
            marker.MinSymbolSize = this.MinSymbolSize;

            CloneEngineOffsets(marker._engineOffset);

            return marker;
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

        #region IFontColor Member

        [Browsable(false)]
        public ArgbColor FontColor
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

        #region ISymbolPositioningUI Member

        public void HorizontalMove(float x)
        {
            float[] c = SymbolTransformation.Rotate(-this.Angle, HorizontalOffset, VerticalOffset);
            c[0] += x;
            c = SymbolTransformation.Rotate(this.Angle, c[0], c[1]);
            this.HorizontalOffset = c[0];
            this.VerticalOffset = c[1];
        }

        public void VertiacalMove(float y)
        {
            float[] c = SymbolTransformation.Rotate(-this.Angle, HorizontalOffset, VerticalOffset);
            c[1] += y;
            c = SymbolTransformation.Rotate(this.Angle, c[0], c[1]);
            this.HorizontalOffset = c[0];
            this.VerticalOffset = c[1];
        }

        #endregion

        #region ISymbolSize Member

        [Browsable(false)]
        public float SymbolSize
        {
            get
            {
                if (_font != null)
                {
                    return _font.Size;
                }

                return 0;
            }
            set
            {
                this.Font = Current.Engine.CreateFont(
                    (_font != null ? _font.Name : "Arial"), value);
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
            set { }
        }

        public bool RequireClone()
        {
            return false;
        }

        #endregion

        #region Helper

        private void PerformSymbolTransformation(float rotation)
        {
            var offset = new CanvasPointF(_hOffset, _vOffset);
            //Current.Engine.DrawTextOffestPointsToFontUnit(ref offset);

            SymbolTransformation.Transform(_angle + rotation, offset.X, offset.Y, out _xOffset, out _yOffset);
        }

        private void RefreshEngineOffset()
        {
            var offset = _engineOffset.ContainsKey(Current.Engine.EngineName) ?
                _engineOffset[Current.Engine.EngineName] :
                new Offset();

            offset.VerticalOffset = this.VerticalOffset;
            offset.HorizontalOffset = this.HorizontalOffset;

            _engineOffset[Current.Engine.EngineName] = offset;
        }

        private void SetCurrentEngineOffset()
        {
            if (_engineOffset.ContainsKey(Current.Engine.EngineName))
            {
                _hOffset = _engineOffset[Current.Engine.EngineName].HorizontalOffset;
                _vOffset = _engineOffset[Current.Engine.EngineName].VerticalOffset;
            }
            PerformSymbolTransformation(0);
        }

        private void CloneEngineOffsets(Dictionary<string, Offset> cloneTo)
        {
            foreach (var key in _engineOffset.Keys)
            {
                if (cloneTo.ContainsKey(key))
                {
                    cloneTo[key] = new Offset(_engineOffset[key]);
                }
                else
                {
                    cloneTo[key] = new Offset(_engineOffset[key]);
                }
            }
        }

        #endregion

        #region ISymbolCurrentGraphicsEngineDependent

        public void CurrentGraphicsEngineChanged()
        {
            SetCurrentEngineOffset();
        }

        #endregion

        #region Helper Classes

        private struct Offset
        {
            public Offset(Offset offset)
            {
                this.HorizontalOffset = offset.HorizontalOffset;
                this.VerticalOffset = offset.VerticalOffset;
            }
            public float HorizontalOffset { get; set; }
            public float VerticalOffset { get; set; }
        }

        #endregion
    }
}
