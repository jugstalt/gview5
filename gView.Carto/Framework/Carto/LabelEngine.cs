using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace gView.Framework.Carto
{
    internal class LabelEngine2 : ILabelEngine, IDisposable
    {
        private enum OverlapMethod { Pixel = 1, Geometry = 0 };
        private OverlapMethod _method = OverlapMethod.Geometry;
        private GraphicsEngine.Abstraction.IBitmap _bitmap;
        //private Display _display;
        private GraphicsEngine.Abstraction.ICanvas _canvas = null;
        private GraphicsEngine.ArgbColor _back;
        private bool _first = true, _directDraw = false;
        private GridArray<List<IAnnotationPolygonCollision>> _gridArrayPolygons = null;

        public LabelEngine2()
        {
            //_display = new Display(false);
        }

        public void Dispose()
        {
            if (_canvas != null)
            {
                _canvas.Dispose();
                _canvas = null;
            }
            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _gridArrayPolygons = null;
        }

        #region ILabelEngine Member

        public void Init(IDisplay display, bool directDraw)
        {
            try
            {
                if (display == null)
                {
                    return;
                }
                //if (_bm != null && (_bm.Width != display.iWidth || _bm.Height != display.iHeight))
                {
                    Dispose();
                }

                if (_bitmap == null)
                {
                    _bitmap = GraphicsEngine.Current.Engine.CreateBitmap(display.ImageWidth, display.ImageHeight, GraphicsEngine.PixelFormat.Rgba32);
                }

                _canvas = _bitmap.CreateCanvas();

                //using (var brush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.Transparent))
                //{
                //    _canvas.FillRectangle(brush, new GraphicsEngine.CanvasRectangle(0, 0, _bitmap.Width, _bitmap.Height));
                //}
                _bitmap.MakeTransparent();

                _back = _bitmap.GetPixel(0, 0);
                _first = true;
                _directDraw = directDraw;
                //_bm.MakeTransparent(Color.White);

                _gridArrayPolygons = new GridArray<List<IAnnotationPolygonCollision>>(
                                                               new Envelope(0.0, 0.0, display.ImageWidth, display.ImageHeight),
                                                               new int[] { 50, 25, 18, 10, 5, 2 },
                                                               new int[] { 50, 25, 18, 10, 5, 2 });
            }
            catch
            {
                Dispose();
            }
        }

        public LabelAppendResult _TryAppend(IDisplay display, ITextSymbol symbol, IGeometry geometry, bool checkForOverlap)
        {
            if (symbol == null || !(display is Display))
            {
                return LabelAppendResult.WrongArguments;
            }

            IAnnotationPolygonCollision labelPolyon = null;
            if (display.GeometricTransformer != null && !(geometry is IDisplayPath))
            {
                geometry = display.GeometricTransformer.Transform2D(geometry) as IGeometry;
                if (geometry == null)
                {
                    return LabelAppendResult.WrongArguments;
                }
            }
            IEnvelope labelPolyonEnvelope = null;
            if (symbol is ILabel)
            {
                foreach (var symbolAlignment in LabelAlignments((ILabel)symbol))
                {
                    List<IAnnotationPolygonCollision> aPolygons = ((ILabel)symbol).AnnotationPolygon(display, geometry, symbolAlignment);
                    bool outside = true;

                    if (aPolygons != null)
                    {
                        #region Check Outside

                        foreach (IAnnotationPolygonCollision polyCollision in aPolygons)
                        {
                            AnnotationPolygonEnvelope env = polyCollision.Envelope;
                            if (env.MinX < 0 || env.MinY < 0 || env.MaxX > _bitmap.Width || env.MaxY > _bitmap.Height)
                            {
                                return LabelAppendResult.Outside;
                            }
                        }

                        #endregion

                        foreach (IAnnotationPolygonCollision polyCollision in aPolygons)
                        {
                            AnnotationPolygonEnvelope env = polyCollision.Envelope;

                            //int minx = (int)Math.Max(0, env.MinX);
                            //int maxx = (int)Math.Min(_bm.Width - 1, env.MaxX);
                            //int miny = (int)Math.Max(0, env.MinY);
                            //int maxy = (int)Math.Min(_bm.Height, env.MaxY);

                            //if (minx > _bm.Width || maxx <= 0 || miny > _bm.Height || maxy <= 0) continue;  // liegt außerhalb!!

                            int minx = (int)env.MinX, miny = (int)env.MinX, maxx = (int)env.MaxX, maxy = (int)env.MaxY;

                            outside = false;

                            if (!_first && checkForOverlap)
                            {
                                if (_method == OverlapMethod.Pixel)
                                {
                                    #region Pixel Methode

                                    for (int x = minx; x < maxx; x++)
                                    {
                                        for (int y = miny; y < maxy; y++)
                                        {
                                            //if (x < 0 || x >= _bm.Width || y < 0 || y >= _bm.Height) continue;

                                            if (polyCollision.Contains(x, y))
                                            {
                                                //_bm.SetPixel(x, y, Color.Yellow);
                                                if (!_back.Equals(_bitmap.GetPixel(x, y)))
                                                {
                                                    return LabelAppendResult.Overlap;
                                                }
                                            }
                                        }
                                    }

                                    #endregion
                                }
                                else
                                {
                                    #region Geometrie Methode

                                    labelPolyon = polyCollision;
                                    foreach (List<IAnnotationPolygonCollision> indexedPolygons in _gridArrayPolygons.Collect(new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY)))
                                    {
                                        foreach (IAnnotationPolygonCollision lp in indexedPolygons)
                                        {
                                            if (lp.CheckCollision(polyCollision) == true)
                                            {
                                                return LabelAppendResult.Overlap;
                                            }
                                        }
                                    }

                                    #endregion
                                }
                            }
                            else
                            {
                                _first = false;

                                if (_method == OverlapMethod.Geometry)
                                {
                                    #region Geometrie Methode

                                    labelPolyon = polyCollision;

                                    #endregion
                                }
                            }
                            labelPolyonEnvelope = new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY);
                        }
                    }

                    if (outside)
                    {
                        return LabelAppendResult.Outside;
                    }
                }
            }

            if (labelPolyon != null)
            {
                List<IAnnotationPolygonCollision> indexedPolygons = _gridArrayPolygons[labelPolyonEnvelope];
                indexedPolygons.Add(labelPolyon);

                //using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                //{
                //    path.StartFigure();
                //    path.AddLine(labelPolyon[0], labelPolyon[1]);
                //    path.AddLine(labelPolyon[1], labelPolyon[2]);
                //    path.AddLine(labelPolyon[2], labelPolyon[3]);
                //    path.CloseFigure();

                //    _graphics.FillPath(Brushes.Aqua, path);
                //}
            }

            var originalCanvas = display.Canvas;
            ((Display)display).Canvas = _canvas;

            symbol.Draw(display, geometry);

            ((Display)display).Canvas = originalCanvas;

            if (_directDraw)
            {
                symbol.Draw(display, geometry);
            }

            return LabelAppendResult.Succeeded;
        }

        public LabelAppendResult TryAppend(IDisplay display, ITextSymbol symbol, IGeometry geometry, bool checkForOverlap)
        {
            if (symbol == null || !(display is Display))
            {
                return LabelAppendResult.WrongArguments;
            }

            if (display.GeometricTransformer != null && !(geometry is IDisplayPath))
            {
                geometry = display.GeometricTransformer.Transform2D(geometry) as IGeometry;
                if (geometry == null)
                {
                    return LabelAppendResult.WrongArguments;
                }
            }

            var labelApendResult = LabelAppendResult.Succeeded;

            if (symbol is ILabel)
            {
                foreach (var symbolAlignment in LabelAlignments((ILabel)symbol))
                {
                    labelApendResult = LabelAppendResult.Succeeded;

                    List<IAnnotationPolygonCollision> aPolygons = ((ILabel)symbol).AnnotationPolygon(display, geometry, symbolAlignment);

                    labelApendResult = TryAppend(display, symbol, geometry, aPolygons, checkForOverlap, symbolAlignment);
                    if (labelApendResult == LabelAppendResult.Succeeded)
                    {
                        break;
                    }
                }
            }

            return labelApendResult;
        }

        public LabelAppendResult TryAppend(IDisplay display, List<IAnnotationPolygonCollision> aPolygons, IGeometry geometry, bool checkForOverlap)
        {
            bool outside = true;
            IAnnotationPolygonCollision labelPolyon = null;
            IEnvelope labelPolyonEnvelope = null;

            if (aPolygons != null)
            {
                foreach (IAnnotationPolygonCollision polyCollision in aPolygons)
                {
                    AnnotationPolygonEnvelope env = polyCollision.Envelope;

                    int minx = (int)Math.Max(0, env.MinX);
                    int maxx = (int)Math.Min(_bitmap.Width - 1, env.MaxX);
                    int miny = (int)Math.Max(0, env.MinY);
                    int maxy = (int)Math.Min(_bitmap.Height, env.MaxY);

                    if (minx > _bitmap.Width || maxx <= 0 || miny > _bitmap.Height || maxy <= 0)
                    {
                        continue;  // liegt außerhalb!!
                    }

                    outside = false;

                    if (!_first && checkForOverlap)
                    {
                        if (_method == OverlapMethod.Pixel)
                        {
                            #region Pixel Methode

                            for (int x = minx; x < maxx; x++)
                            {
                                for (int y = miny; y < maxy; y++)
                                {
                                    //if (x < 0 || x >= _bm.Width || y < 0 || y >= _bm.Height) continue;

                                    if (polyCollision.Contains(x, y))
                                    {
                                        //_bm.SetPixel(x, y, Color.Yellow);
                                        if (!_back.Equals(_bitmap.GetPixel(x, y)))
                                        {
                                            return LabelAppendResult.Overlap;
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Geometrie Methode
                            labelPolyon = polyCollision;
                            foreach (List<IAnnotationPolygonCollision> indexedPolygons in _gridArrayPolygons.Collect(new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY)))
                            {
                                foreach (IAnnotationPolygonCollision lp in indexedPolygons)
                                {
                                    if (lp.CheckCollision(polyCollision) == true)
                                    {
                                        return LabelAppendResult.Overlap;
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        _first = false;

                        if (_method == OverlapMethod.Geometry)
                        {
                            #region Geometrie Methode

                            labelPolyon = polyCollision;

                            #endregion
                        }
                    }
                    labelPolyonEnvelope = new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY);
                }
                if (outside)
                {
                    return LabelAppendResult.Outside;
                }
            }


            if (labelPolyon != null)
            {
                List<IAnnotationPolygonCollision> indexedPolygons = _gridArrayPolygons[labelPolyonEnvelope];
                indexedPolygons.Add(labelPolyon);
            }

            return LabelAppendResult.Succeeded;
        }

        public void Draw(IDisplay display, ICancelTracker cancelTracker)
        {
            try
            {

                display.Canvas.DrawBitmap(_bitmap, new GraphicsEngine.CanvasPoint(0, 0));

                //_bm.Save(@"c:\temp\label.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            catch { }
        }

        public void Release()
        {
            Dispose();
        }

        public GraphicsEngine.Abstraction.ICanvas LabelCanvas
        {
            get { return _canvas; }
        }

        #endregion

        #region Helper

        private LabelAppendResult TryAppend(IDisplay display, ITextSymbol symbol, IGeometry geometry, List<IAnnotationPolygonCollision> aPolygons, bool checkForOverlap, TextSymbolAlignment symbolAlignment)
        {
            IAnnotationPolygonCollision labelPolyon = null;
            IEnvelope labelPolyonEnvelope = null;

            bool outside = true;

            if (aPolygons != null)
            {
                #region Check Outside

                foreach (IAnnotationPolygonCollision polyCollision in aPolygons)
                {
                    AnnotationPolygonEnvelope env = polyCollision.Envelope;
                    if (env.MinX < 0 || env.MinY < 0 || env.MaxX > _bitmap.Width || env.MaxY > _bitmap.Height)
                    {
                        return LabelAppendResult.Outside;
                    }
                }

                #endregion

                foreach (IAnnotationPolygonCollision polyCollision in aPolygons)
                {
                    AnnotationPolygonEnvelope env = polyCollision.Envelope;

                    //int minx = (int)Math.Max(0, env.MinX);
                    //int maxx = (int)Math.Min(_bm.Width - 1, env.MaxX);
                    //int miny = (int)Math.Max(0, env.MinY);
                    //int maxy = (int)Math.Min(_bm.Height, env.MaxY);

                    //if (minx > _bm.Width || maxx <= 0 || miny > _bm.Height || maxy <= 0) continue;  // liegt außerhalb!!

                    int minx = (int)env.MinX, miny = (int)env.MinX, maxx = (int)env.MaxX, maxy = (int)env.MaxY;

                    outside = false;

                    if (!_first && checkForOverlap)
                    {
                        if (_method == OverlapMethod.Pixel)
                        {
                            #region Pixel Methode

                            for (int x = minx; x < maxx; x++)
                            {
                                for (int y = miny; y < maxy; y++)
                                {
                                    //if (x < 0 || x >= _bm.Width || y < 0 || y >= _bm.Height) continue;

                                    if (polyCollision.Contains(x, y))
                                    {
                                        //_bm.SetPixel(x, y, Color.Yellow);
                                        if (!_back.Equals(_bitmap.GetPixel(x, y)))
                                        {
                                            return LabelAppendResult.Overlap;
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Geometrie Methode

                            labelPolyon = polyCollision;
                            foreach (List<IAnnotationPolygonCollision> indexedPolygons in _gridArrayPolygons.Collect(new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY)))
                            {
                                foreach (IAnnotationPolygonCollision lp in indexedPolygons)
                                {
                                    if (lp.CheckCollision(polyCollision) == true)
                                    {
                                        return LabelAppendResult.Overlap;
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                    else
                    {
                        _first = false;

                        if (_method == OverlapMethod.Geometry)
                        {
                            #region Geometrie Methode

                            labelPolyon = polyCollision;

                            #endregion
                        }
                    }
                    labelPolyonEnvelope = new Envelope(env.MinX, env.MinY, env.MaxX, env.MaxY);
                }
            }

            if (outside)
            {
                return LabelAppendResult.Outside;
            }

            if (labelPolyon != null)
            {
                List<IAnnotationPolygonCollision> indexedPolygons = _gridArrayPolygons[labelPolyonEnvelope];
                indexedPolygons.Add(labelPolyon);

                //using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                //{
                //    path.StartFigure();
                //    path.AddLine(labelPolyon[0], labelPolyon[1]);
                //    path.AddLine(labelPolyon[1], labelPolyon[2]);
                //    path.AddLine(labelPolyon[2], labelPolyon[3]);
                //    path.CloseFigure();

                //    _graphics.FillPath(Brushes.Aqua, path);
                //}
            }

            var originalCanvas = display.Canvas;
            ((Display)display).Canvas = _canvas;

            symbol.Draw(display, geometry, symbolAlignment);

            ((Display)display).Canvas = originalCanvas;

            if (_directDraw)
            {
                symbol.Draw(display, geometry, symbolAlignment);
            }

            return LabelAppendResult.Succeeded;
        }

        private TextSymbolAlignment[] LabelAlignments(ILabel label)
        {
            return label.SecondaryTextSymbolAlignments != null && label.SecondaryTextSymbolAlignments.Length > 0 ?
                label.SecondaryTextSymbolAlignments :
                new TextSymbolAlignment[] { label.TextSymbolAlignment };
        }

        #endregion

        #region Helper Classes
        private class LabelPolygon
        {
            PointF[] _points;

            public LabelPolygon(PointF[] points)
            {
                _points = points;
            }

            public bool CheckCollision(LabelPolygon lp)
            {
                if (HasSeperateLine(this, lp))
                {
                    return false;
                }

                if (HasSeperateLine(lp, this))
                {
                    return false;
                }

                return true;
            }

            public PointF this[int index]
            {
                get
                {
                    if (index < 0 || index >= _points.Length)
                    {
                        return _points[0];
                    }

                    return _points[index];
                }
            }
            private static bool HasSeperateLine(LabelPolygon tester, LabelPolygon cand)
            {
                for (int i = 1; i <= tester._points.Length; i++)
                {
                    PointF p1 = tester[i];
                    Vector2dF ortho = new Vector2dF(p1, tester._points[i - 1]);
                    ortho.ToOrtho();
                    ortho.Normalize();

                    float t_min = 0f, t_max = 0f, c_min = 0f, c_max = 0f;
                    MinMaxAreaForOrhtoSepLine(p1, ortho, tester, ref t_min, ref t_max);
                    MinMaxAreaForOrhtoSepLine(p1, ortho, cand, ref c_min, ref c_max);

                    if ((t_min <= c_max && t_max <= c_min) ||
                        (c_min <= t_max && c_max <= t_min))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static void MinMaxAreaForOrhtoSepLine(PointF p1, Vector2dF ortho, LabelPolygon lp, ref float min, ref float max)
            {
                for (int j = 0; j < lp._points.Length; j++)
                {
                    Vector2dF rc = new Vector2dF(lp[j], p1);
                    float prod = ortho.DotProduct(rc);
                    if (j == 0)
                    {
                        min = max = prod;
                    }
                    else
                    {
                        min = Math.Min(min, prod);
                        max = Math.Max(max, prod);
                    }
                }
            }

            public IEnvelope Envelope
            {
                get
                {
                    double
                        minx = _points[0].X,
                        miny = _points[0].Y,
                        maxx = _points[0].X,
                        maxy = _points[0].Y;
                    for (int i = 1; i < _points.Length; i++)
                    {
                        minx = Math.Min(minx, _points[i].X);
                        miny = Math.Min(miny, _points[i].Y);
                        maxx = Math.Max(maxx, _points[i].X);
                        maxy = Math.Max(maxy, _points[i].Y);
                    }

                    return new Envelope(minx, miny, maxx, maxy);
                }
            }

            #region Helper Classes
            private class Vector2dF
            {
                float _x, _y;

                public Vector2dF(PointF p1, PointF p0)
                {
                    _x = p1.X - p0.X;
                    _y = p1.Y - p0.Y;
                }

                public void ToOrtho()
                {
                    float x = _x;
                    _x = -_y;
                    _y = x;
                }

                public void Normalize()
                {
                    float l = (float)Math.Sqrt(_x * _x + _y * _y);
                    _x /= l;
                    _y /= l;
                }

                public float DotProduct(Vector2dF v)
                {
                    return _x * v._x + _y * v._y;
                }
            }
            #endregion
        }
        #endregion
    }

    #region Old Label Engines
    /*
    internal class LabelEngine : ILabelEngine
    {
        List<Font> _fonts = new List<Font>();
        List<Label> _labels = new List<Label>();
        List<ITextSymbol> _symbols = new List<ITextSymbol>();
        private bool _directDraw = false;

        #region ILabelEngine Member

        public void Init(IDisplay display, bool directDraw)
        {
            _directDraw = directDraw;
        }
        public LabelAppendResult TryAppend(IDisplay display, ITextSymbol symbol, IGeometry geometry, bool chechForOverlap)
        {
            if (symbol == null || geometry == null || display == null || symbol.Text.Trim() == String.Empty) return LabelAppendResult.WrongArguments;

            if (!_fonts.Contains(symbol.Font))
                _fonts.Add((Font)symbol.Font.Clone());
            if (!_symbols.Contains(symbol))
                _symbols.Add(symbol);

            _labels.Add(new Label(_symbols.IndexOf(symbol), _fonts.IndexOf(symbol.Font), geometry, symbol.Text));
            return LabelAppendResult.Succeeded;
        }

        public void Draw(IDisplay display, ICancelTracker cancelTracker)
        {
            foreach (Label label in _labels)
            {
                if (cancelTracker != null && !cancelTracker.Continue) return;

                ITextSymbol txtSymbol = _symbols[label.SymbolID];

                txtSymbol.Text = label.Text;
                txtSymbol.Font = _fonts[label.FontID];
                txtSymbol.Draw(display, label.Geometry);
            }
        }

        public void Release()
        {
            _labels.Clear();

            ReleaseTextSymbols();
            ReleaseFonts();

            //GC.Collect();
        }

        #endregion

        private void ReleaseTextSymbols()
        {
            foreach (ITextSymbol txtSymbol in _symbols)
            {
                txtSymbol.Release();
            }
            _symbols.Clear();
        }
        private void ReleaseFonts()
        {
            foreach (Font font in _fonts)
            {
                if (font == null) continue;
                font.Dispose();
            }
            _fonts.Clear();
        }
    }

    internal class Label
    {
        public int SymbolID, FontID;
        public IGeometry Geometry;
        public string Text;

        public Label(int symbolID, int fontID, IGeometry geometry, string text)
        {
            FontID = fontID;
            SymbolID = symbolID;
            Geometry = geometry;
            Text = text;
        }
    }
    */
    #endregion
}
