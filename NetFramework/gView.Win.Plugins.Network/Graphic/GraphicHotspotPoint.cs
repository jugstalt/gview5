using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.GraphicsEngine;
using System;

namespace gView.Plugins.Network.Graphic
{
    public class GraphicHotspotPoint : IGraphicElement
    {
        private string _text = String.Empty;
        private IPoint _point = null;
        private SimpleLineSymbol _lineSymbol = new SimpleLineSymbol();
        private GlowingTextSymbol _textSymbol = new GlowingTextSymbol();

        public GraphicHotspotPoint(IPoint point, string text)
        {
            _point = point;
            _text = text;

            _lineSymbol.Color = ArgbColor.Red;
            _lineSymbol.Width = 3;
            _textSymbol.Color = ArgbColor.Black;
            _textSymbol.GlowingColor = ArgbColor.Yellow;
            _textSymbol.Smoothingmode = gView.Framework.Symbology.SymbolSmoothing.AntiAlias;
            _textSymbol.Font = GraphicsEngine.Current.Engine.CreateFont("Arial", 10);
            _textSymbol.TextSymbolAlignment = gView.Framework.Symbology.TextSymbolAlignment.leftAlignOver;
            _textSymbol.Text = _text;
        }

        #region IGraphicElement Member

        public void Draw(IDisplay display)
        {
            if (display == null || _point == null)
            {
                return;
            }

            Point p1 = new Point(_point.X, _point.Y + (20 * display.MapScale / (96 / 0.0254)));
            Path path = new Path();
            path.AddPoint(_point);
            path.AddPoint(p1);
            Polyline pLine = new Polyline();
            pLine.AddPath(path);

            display.Draw(_lineSymbol, pLine);
            display.Draw(_textSymbol, p1);
        }

        #endregion
    }

    public class GraphicStartPoint : GraphicHotspotPoint
    {
        public GraphicStartPoint(IPoint point)
            : base(point, "Start")
        {
        }
    }

    public class GraphicTargetPoint : GraphicHotspotPoint
    {
        public GraphicTargetPoint(IPoint point)
            : base(point, "Target")
        {
        }
    }

    public class GraphicFlagPoint : GraphicHotspotPoint
    {
        public GraphicFlagPoint(IPoint point, string text)
            : base(point, text)
        {
        }
    }
}
