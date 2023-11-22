using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using System;

namespace gView.Framework.Carto.UI
{
    public class SymbolPreview
    {
        public static IMap CurrentMap = null;

        private readonly IMap _map;

        public SymbolPreview(IMap map)
        {
            _map = map ?? CurrentMap;
        }

        private static IGeometry GeometryFromSymbol(ISymbol symbol, IEnvelope env)
        {
            IGeometry geometry = null;

            if (symbol is ISymbolCollection)
            {
                if (((ISymbolCollection)symbol).Symbols.Count > 0)
                {
                    geometry = GeometryFromSymbol(((ISymbolCollection)symbol).Symbols[0].Symbol, env);
                }
            }
            else if (symbol is IPointSymbol || symbol is ITextSymbol)
            {
                geometry = new gView.Framework.Geometry.Point((env.minx + env.maxx) / 2, Math.Abs(env.miny - env.maxy) / 2);
            }
            else if (symbol is ILineSymbol)
            {
                IPoint p1 = new gView.Framework.Geometry.Point(env.minx + 5, Math.Abs(env.miny - env.maxy) / 2);
                IPoint p2 = new gView.Framework.Geometry.Point(env.maxx - 5, Math.Abs(env.miny - env.maxy) / 2);
                IPath path = new Path();
                path.AddPoint(p1);
                path.AddPoint(p2);
                IPolyline line = new Polyline();
                line.AddPath(path);
                geometry = line;
            }
            else if (symbol is IFillSymbol)
            {
                IPoint p1 = new gView.Framework.Geometry.Point(env.minx + 5, 5);
                IPoint p2 = new gView.Framework.Geometry.Point(env.maxx - 5, 5);
                IPoint p3 = new gView.Framework.Geometry.Point(env.maxx - 5, Math.Abs(env.miny - env.maxy) - 5);
                IPoint p4 = new gView.Framework.Geometry.Point(env.minx + 5, Math.Abs(env.miny - env.maxy) - 5);
                IRing ring = new Ring();
                ring.AddPoint(p1);
                ring.AddPoint(p2);
                ring.AddPoint(p3);
                ring.AddPoint(p4);
                IPolygon polygon = new Polygon();
                polygon.AddRing(ring);
                geometry = polygon;
            }

            return geometry;
        }

        public void Draw(GraphicsEngine.Abstraction.ICanvas canvas, GraphicsEngine.CanvasRectangle rectangle, ISymbol symbol)
        {
            Draw(canvas, rectangle, symbol, true);
        }
        public void Draw(GraphicsEngine.Abstraction.ICanvas canvas, GraphicsEngine.CanvasRectangle rectangle, ISymbol symbol, bool cls)
        {
            if (symbol == null || canvas == null)
            {
                return;
            }

            Display display = new gView.Framework.Carto.Display(_map);
            display.Dpi = GraphicsEngine.Current.Engine.ScreenDpi; //96f; // canvas.DpiX;

            IEnvelope env = display.Limit = new Envelope(0, rectangle.Top + rectangle.Height, rectangle.Left + rectangle.Width, 0);
            display.ImageWidth = (int)env.maxx;
            display.ImageHeight = (int)env.maxy;
            display.ZoomTo(env.minx, env.miny, env.maxx, env.maxy);

            IGeometry geometry = GeometryFromSymbol(symbol, new Envelope(rectangle.Left,
                                                                         rectangle.Height + rectangle.Top,
                                                                         rectangle.Width + rectangle.Left,
                                                                         rectangle.Top));
            if (geometry == null)
            {
                return;
            }

            if (PlugInManager.PlugInID(symbol).Equals(KnownObjects.Symbology_PolygonMask))
            {
                return;
            }

            display.Canvas = canvas;

            if (cls)
            {
                using (var brush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.White))
                {
                    canvas.FillRectangle(brush, rectangle);
                }
            }

            ISymbol sym = symbol.Clone(new CloneOptions(display, false)) as ISymbol;
            if (sym != null)
            {
                if (sym is ITextSymbol)
                {
                    ((ITextSymbol)sym).Text = "Text";
                }

                sym.Draw(display, geometry);
                sym.Release();
            }
            else
            {
                symbol.Draw(display, geometry);
            }
        }
    }
}
