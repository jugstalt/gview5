using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.Symbology;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.Plugins.MapTools.Graphics.Dialogs;
using System;

namespace gView.Plugins.MapTools.Graphics
{
    [gView.Framework.system.RegisterPlugIn("2DEB81E7-F0B2-4fe4-A0E4-2CDEFB0F3F7D")]
    public class Rectangle : GraphicShape
    {
        public Rectangle()
        {
            SimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Color = ArgbColor.FromArgb(40, 255, 255, 100);
            SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            fillSymbol.OutlineSymbol = lineSymbol;
            this.Symbol = fillSymbol;

            Polygon polygon = new Polygon();
            Ring ring = new Ring();
            ring.AddPoint(new Point(0, 0));
            ring.AddPoint(new Point(1, 0));
            ring.AddPoint(new Point(1, 1));
            ring.AddPoint(new Point(0, 1));
            polygon.AddRing(ring);
            this.Template = polygon;
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Rectangle", "Rectangle"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[0]; }
        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("C7FED5D4-A860-46dc-8D6C-10DD899BE3A2")]
    public class Ellipse : GraphicShape
    {
        public Ellipse()
        {
            SimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Color = ArgbColor.FromArgb(60, 255, 255, 100);
            SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            fillSymbol.OutlineSymbol = lineSymbol;
            this.Symbol = fillSymbol;

            Polygon polygon = new Polygon();
            Ring ring = new Ring();
            for (double w = 0; w < 2.0 * Math.PI; w += 2.0 * Math.PI / 50)
            {
                ring.AddPoint(new Point(0.5 + 0.5 * Math.Cos(w), 0.5 + 0.5 * Math.Sin(w)));
            }
            polygon.AddRing(ring);
            this.Template = polygon;
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Ellipse", "Ellipse"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[1]; }
        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("41A95112-E41B-4bb4-B0F9-3A4B98049A19")]
    public class Arrow : GraphicShape
    {
        public Arrow()
        {
            SimpleFillSymbol fillSymbol = new SimpleFillSymbol();
            fillSymbol.Color = ArgbColor.FromArgb(60, 255, 255, 100);
            SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            fillSymbol.OutlineSymbol = lineSymbol;
            this.Symbol = fillSymbol;

            Polygon polygon = new Polygon();
            Ring ring = new Ring();
            ring.AddPoint(new Point(0, 0.33));
            ring.AddPoint(new Point(0.66, 0.33));
            ring.AddPoint(new Point(0.66, 0));
            ring.AddPoint(new Point(1, 0.5));
            ring.AddPoint(new Point(0.66, 1));
            ring.AddPoint(new Point(0.66, 0.66));
            ring.AddPoint(new Point(0, 0.66));
            polygon.AddRing(ring);
            this.Template = polygon;
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Arrow", "Arrow"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[2]; }
        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("8375CC06-353D-48d3-876A-BA0E953A88AB")]
    public class Line : GraphicShape
    {
        public Line()
        {
            SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
            this.Symbol = lineSymbol;

            Polyline polyline = new Polyline();
            Path path = new Path();
            path.AddPoint(new Point(0, 0));
            path.AddPoint(new Point(1, 1));
            polyline.AddPath(path);

            this.Template = polyline;

            RemoveAllGrabbers();
            AddGrabber(GrabberIDs.lowerLeft);
            AddGrabber(GrabberIDs.upperRight);
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Line", "Line"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[3]; }
        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("849E6576-E0BA-4992-8E69-4D47009D3906")]
    public class SimplePoint : GraphicShape
    {
        public SimplePoint()
        {
            SimplePointSymbol pSymbol = new SimplePointSymbol();
            pSymbol.Color = ArgbColor.Red;
            pSymbol.Size = 10;

            this.Symbol = pSymbol;
            this.Template = new Point(0, 0);
            RemoveAllGrabbers();
            AddGrabber(GrabberIDs.move);
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Point", "Point"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[8]; }
        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("FDC92EE8-B350-4b97-886C-E5B5B155ABCA")]
    public class Text : GraphicShape, IToolMouseActions
    {
        private SimpleTextSymbol _symbol;
        private string _text = "Doubleclick Text";
        private Ghost _ghost = null;

        public Text()
        {
            _symbol = new SimpleTextSymbol();
            _symbol.Font = Current.Engine.CreateFont("Arial", 20);
            _symbol.Text = _text;
            _symbol.TextSymbolAlignment = TextSymbolAlignment.leftAlignOver;
            //this.Symbol = _symbol;

            AggregateGeometry aGeometry = new AggregateGeometry();
            Polygon polygon = new Polygon();
            Ring ring = new Ring();
            ring.AddPoint(new Point(0, 0));
            ring.AddPoint(new Point(1, 0));
            ring.AddPoint(new Point(1, 1));
            ring.AddPoint(new Point(0, 1));
            polygon.AddRing(ring);

            aGeometry.AddGeometry(polygon);
            aGeometry.AddGeometry(new Point(0, 0));

            this.Template = aGeometry;

            RemoveAllGrabbers();
            AddGrabber(GrabberIDs.rotation);
            AddGrabber(GrabberIDs.move);
        }

        public override void Scale(double scaleX, double scaleY)
        {
        }
        public override void ScaleX(double scale)
        {
        }
        public override void ScaleY(double scale)
        {
        }

        public override IGraphicElement2 Ghost
        {
            get
            {
                if (_ghost == null)
                {
                    Polygon polygon = new Polygon();
                    Ring ring = new Ring();
                    ring.AddPoint(new Point(0, 0));
                    ring.AddPoint(new Point(1, 0));
                    ring.AddPoint(new Point(1, 1));
                    ring.AddPoint(new Point(0, 1));
                    polygon.AddRing(ring);

                    SimpleLineSymbol lineSymbol = new SimpleLineSymbol();
                    lineSymbol.DashStyle = LineDashStyle.Dash;
                    lineSymbol.Color = ArgbColor.Gray;

                    _ghost = new Ghost(polygon, lineSymbol);
                }
                return _ghost;
            }
        }

        public override void Draw(IDisplay display)
        {
            if (display == null || display.Canvas == null)
            {
                return;
            }

            SimpleTextSymbol sym = _symbol.Clone(new CloneOptions(display, false)) as SimpleTextSymbol;
            sym.Text = _text;
            sym.Angle = (float)this.Rotation;

            var size = display.Canvas.MeasureText(_text, sym.Font);
            double dx = size.Width * display.MapScale / (display.Dpi / 0.0254);  // [m]
            double dy = size.Height * display.MapScale / (display.Dpi / 0.0254);  // [m]
            base.Scale(dx, dy);

            IGeometry geometry = TransformGeometry();
            if (geometry == null)
            {
                return;
            }

            display.Draw(sym, geometry);
            sym.Release();
        }

        public string TextString
        {
            get { return _text; }
            set
            {
                _text = value;
            }
        }
        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Text", "Text"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[4]; }
        }

        #endregion

        #region IFontColor Member

        override public ArgbColor FontColor
        {
            get
            {
                if (_symbol is IFontColor)
                {
                    return ((IFontColor)_symbol).FontColor;
                }
                return ArgbColor.Transparent;
            }
            set
            {
                if (_symbol is IFontColor)
                {
                    ((IFontColor)_symbol).FontColor = value;
                }
            }
        }

        #endregion

        #region IFont Member

        override public IFont Font
        {
            get
            {
                if (_symbol is IFontSymbol)
                {
                    return ((IFontSymbol)_symbol).Font;
                }
                return Current.Engine.CreateFont("Arial", 10);
            }
            set
            {
                if (_symbol is IFontSymbol)
                {
                    ((IFontSymbol)_symbol).Font = value;
                }
            }
        }

        #endregion

        #region IToolMouseActions Member

        public void MouseDown(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void MouseUp(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void MouseClick(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void MouseDoubleClick(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {
            FormText dlg = new FormText(this._text);
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this._text = dlg.SelectedText;
            }
        }

        public void MouseMove(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void MouseWheel(IDisplay display, System.Windows.Forms.MouseEventArgs e)
        {

        }

        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            _text = (string)stream.Load("Text", "Text");
            SimpleTextSymbol sym = stream.Load("ISymbol", null) as SimpleTextSymbol;
            if (sym != null)
            {
                _symbol.Release();
                _symbol = sym;
            }
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("Text", _text);
            stream.Save("ISymbol", _symbol);
        }
        #endregion

        public override void MouseDoubleClick(IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            this.MouseDoubleClick(display, e);
        }
    }

    [gView.Framework.system.RegisterPlugIn("7F5F743C-8269-43fe-9524-F74E39D3FD45")]
    public class Freehand : GraphicShape, IConstructable
    {
        private ISymbol _symbol;
        private IGraphicsContainer _addContainer = null;

        public Freehand()
        {
            _symbol = new SimpleLineSymbol();
        }

        private IPolyline _pLine = null;
        private void AddPoint(double x, double y)
        {
            if (_pLine == null)
            {
                _pLine = new Polyline();
                Path path = new Path();
                _pLine.AddPath(path);
            }
            _pLine[0].AddPoint(new Point(x, y));
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Freehand", "Freehand"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[5]; }
        }

        #endregion

        #region IConstructable Member

        public bool hasVertices
        {
            get { return true; }
        }

        private bool _mousePressed = false;

        public void ConstructMouseDown(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null)
            {
                return;
            }

            Freehand element = new Freehand();

            _addContainer = new GraphicsContainer();
            _addContainer.Elements.Add(element);

            _mousePressed = true;
            double x = e.X;
            double y = e.Y;
            ActiveDisplay(doc).Image2World(ref x, ref y);

            element.AddPoint(x, y);
        }

        public void ConstructMouseUp(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            _mousePressed = false;

            if (ActiveDisplay(doc) == null || _addContainer == null || _addContainer.Elements.Count == 0)
            {
                return;
            }

            Freehand element = _addContainer.Elements[0] as Freehand;
            if (element == null)
            {
                return;
            }

            element.Symbol = _symbol.Clone() as ISymbol;
            element.Template = element._pLine;

            IEnvelope env = element._pLine.Envelope;
            element.Scale(env.Width, env.Height);
            element.Translation(env.minx, env.miny);
            element._pLine = null;

            GraphicShape.AddElementToContainer(doc, element);

            _addContainer = null;
        }

        public void ConstructMouseClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void ConstructMouseDoubleClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void ConstructMouseMove(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (_mousePressed && _addContainer != null && _addContainer.Elements.Count == 1 && ActiveDisplay(doc) != null)
            {
                double x = e.X;
                double y = e.Y;
                ActiveDisplay(doc).Image2World(ref x, ref y);
                ((Freehand)_addContainer.Elements[0]).AddPoint(x, y);

                ActiveDisplay(doc).DrawOverlay(_addContainer, true);
            }
        }

        public void ConstructMouseWheel(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        #endregion

        #region IGraphicElement
        public override void Draw(IDisplay display)
        {
            if (_pLine != null)
            {
                display.Draw(_symbol, _pLine);
            }
            else
            {
                base.Draw(display);
            }
        }
        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = _symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion

        private IDisplay ActiveDisplay(IMapDocument doc)
        {
            if (doc == null || doc.FocusMap == null)
            {
                return null;
            }

            return doc.FocusMap.Display;
        }
    }

    [gView.Framework.system.RegisterPlugIn("0A05D45E-B977-4678-8E35-5DD1C5756CBF")]
    public class GraphicPolyline : GraphicShape, IConstructable
    {
        private ISymbol _symbol;
        private IGraphicsContainer _addContainer = null;

        public GraphicPolyline()
        {
            _symbol = new SimpleLineSymbol();
            ((SimpleLineSymbol)_symbol).Width = 5;
        }

        internal IPolyline _pLine = null;
        private IPoint _moveable = null;
        internal void AddPoint(double x, double y)
        {
            if (_pLine == null)
            {
                _pLine = new Polyline();
                Path path = new Path();
                _pLine.AddPath(path);
            }
            _pLine[0].AddPoint(new Point(x, y));
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Polyline", "Polyline"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[6]; }
        }

        #endregion

        #region IConstructable Member

        public void ConstructMouseDown(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
        }

        public void ConstructMouseUp(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void ConstructMouseClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null)
            {
                return;
            }

            if (_addContainer == null)
            {
                GraphicPolyline element = new GraphicPolyline();
                _addContainer = new GraphicsContainer();
                _addContainer.Elements.Add(element);
            }
            double x = e.X;
            double y = e.Y;
            ActiveDisplay(doc).Image2World(ref x, ref y);

            if (_moveable == null)
            {
                ((GraphicPolyline)_addContainer.Elements[0]).AddPoint(x, y);
            }
            else
            {
                _moveable.X = x;
                _moveable.Y = y;
                _moveable = null;
            }
            ActiveDisplay(doc).DrawOverlay(_addContainer, true);
        }

        public void ConstructMouseDoubleClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null || _addContainer == null || _addContainer.Elements.Count == 0)
            {
                return;
            }

            GraphicPolyline element = _addContainer.Elements[0] as GraphicPolyline;
            if (element == null)
            {
                return;
            }

            // Remove the last 1 Points...
            element._pLine[0].RemovePoint(element._pLine[0].PointCount - 1);
            //element._pLine[0].ReomvePoint(element._pLine[0].PointCount - 1);

            element.Symbol = _symbol.Clone() as ISymbol;
            element.Template = element._pLine;

            IEnvelope env = element._pLine.Envelope;
            element.Scale(env.Width, env.Height);
            element.Translation(env.minx, env.miny);
            element._pLine = null;

            GraphicShape.AddElementToContainer(doc, element);

            _addContainer = null;
            _moveable = null;
        }

        public void ConstructMouseMove(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null || _addContainer == null)
            {
                return;
            }

            double x = e.X;
            double y = e.Y;
            ActiveDisplay(doc).Image2World(ref x, ref y);
            if (_moveable == null)
            {
                _moveable = new Point(x, y);
                ((GraphicPolyline)_addContainer.Elements[0])._pLine[0].AddPoint(_moveable);
            }
            else
            {
                _moveable.X = x;
                _moveable.Y = y;
            }

            ActiveDisplay(doc).DrawOverlay(_addContainer, true);
        }

        public void ConstructMouseWheel(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public bool hasVertices
        {
            get { return true; }
        }

        #endregion

        #region IGraphicElement
        public override void Draw(IDisplay display)
        {
            if (_pLine != null)
            {
                display.Draw(_symbol, _pLine);
            }
            else
            {
                base.Draw(display);
            }
        }
        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = _symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion

        private IDisplay ActiveDisplay(IMapDocument doc)
        {
            if (doc == null || doc.FocusMap == null)
            {
                return null;
            }

            return doc.FocusMap.Display;
        }
    }

    [gView.Framework.system.RegisterPlugIn("EC75A286-748A-4b2b-89E3-CB145FAEB57D")]
    public class GraphicPolygon : GraphicShape, IConstructable
    {
        private ISymbol _symbol;
        private IGraphicsContainer _addContainer = null;

        public GraphicPolygon()
        {
            _symbol = new SimpleFillSymbol();
            ((SimpleFillSymbol)_symbol).OutlineSymbol = new SimpleLineSymbol();
            ((SimpleFillSymbol)_symbol).Color = ArgbColor.FromArgb(150, 255, 255, 0);
        }

        internal GraphicPolygon(IPolygon polygon)
            : this()
        {
            Symbol = _symbol.Clone() as ISymbol;
            if (polygon == null)
            {
                return;
            }

            Template = polygon;

            IEnvelope env = polygon.Envelope;
            Scale(env.Width, env.Height);
            Translation(env.minx, env.miny);
        }

        private IPolygon _polygon = null;
        private IPoint _moveable = null;
        private void AddPoint(double x, double y)
        {
            if (_polygon == null)
            {
                _polygon = new Polygon();
                Ring path = new Ring();
                _polygon.AddRing(path);
            }
            _polygon[0].AddPoint(new Point(x, y));
        }

        #region IGraphicElement2 Member

        override public string Name
        {
            get { return LocalizedResources.GetResString("Shape.Polygon", "Polygon"); }
        }

        override public System.Drawing.Image Icon
        {
            get { return (new Buttons()).ILGraphics.Images[7]; }
        }

        #endregion

        #region IConstructable Member

        public void ConstructMouseDown(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void ConstructMouseUp(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public void ConstructMouseClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null)
            {
                return;
            }

            if (_addContainer == null)
            {
                GraphicPolygon element = new GraphicPolygon();
                _addContainer = new GraphicsContainer();
                _addContainer.Elements.Add(element);
            }
            double x = e.X;
            double y = e.Y;
            ActiveDisplay(doc).Image2World(ref x, ref y);

            if (_moveable == null)
            {
                ((GraphicPolygon)_addContainer.Elements[0]).AddPoint(x, y);
            }
            else
            {
                _moveable.X = x;
                _moveable.Y = y;
                _moveable = null;
            }
            ActiveDisplay(doc).DrawOverlay(_addContainer, true);
        }

        public void ConstructMouseDoubleClick(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null || _addContainer == null || _addContainer.Elements.Count == 0)
            {
                return;
            }

            GraphicPolygon element = _addContainer.Elements[0] as GraphicPolygon;
            if (element == null)
            {
                return;
            }

            // Remove the last 1 Points...
            element._polygon[0].RemovePoint(element._polygon[0].PointCount - 1);
            //element._polygon[0].ReomvePoint(element._polygon[0].PointCount - 1);

            element.Symbol = _symbol.Clone() as ISymbol;
            element.Template = element._polygon;

            IEnvelope env = element._polygon.Envelope;
            element.Scale(env.Width, env.Height);
            element.Translation(env.minx, env.miny);
            element._polygon = null;

            GraphicShape.AddElementToContainer(doc, element);

            _addContainer = null;
            _moveable = null;
        }

        public void ConstructMouseMove(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {
            if (ActiveDisplay(doc) == null || _addContainer == null)
            {
                return;
            }

            double x = e.X;
            double y = e.Y;
            ActiveDisplay(doc).Image2World(ref x, ref y);
            if (_moveable == null)
            {
                _moveable = new Point(x, y);
                ((GraphicPolygon)_addContainer.Elements[0])._polygon[0].AddPoint(_moveable);
            }
            else
            {
                _moveable.X = x;
                _moveable.Y = y;
            }

            ActiveDisplay(doc).DrawOverlay(_addContainer, true);
        }

        public void ConstructMouseWheel(IMapDocument doc, System.Windows.Forms.MouseEventArgs e)
        {

        }

        public bool hasVertices
        {
            get { return true; }
        }

        #endregion

        #region IGraphicElement
        public override void Draw(IDisplay display)
        {
            if (_polygon != null)
            {
                display.Draw(_symbol, _polygon);
            }
            else
            {
                base.Draw(display);
            }
        }
        #endregion

        #region IPersistable
        public override void Load(gView.Framework.IO.IPersistStream stream)
        {
            base.Load(stream);
            this.Symbol = _symbol = (ISymbol)stream.Load("ISymbol", this.Symbol);
        }
        public override void Save(gView.Framework.IO.IPersistStream stream)
        {
            base.Save(stream);
            stream.Save("ISymbol", this.Symbol);
        }
        #endregion

        private IDisplay ActiveDisplay(IMapDocument doc)
        {
            if (doc == null || doc.FocusMap == null)
            {
                return null;
            }

            return doc.FocusMap.Display;
        }
    }
}
