using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.IO;
using gView.Framework.Symbology;
using gView.Framework.Symbology.UI;
using gView.Framework.Symbology.UI.Controls;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Events;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Graphics
{
    [gView.Framework.system.RegisterPlugIn("D1E12064-72E0-4399-82DD-D2E7165044C4")]
    public class GraphicsToolbar : IToolbar
    {
        private List<Guid> _guids;

        public GraphicsToolbar()
        {
            _guids = new List<Guid>();
            _guids.Add(new Guid("895B7C49-6612-45ca-998A-1B897696799C"));
            _guids.Add(new Guid("FEEEE362-116B-406b-8D94-D817A9BAC121"));
            _guids.Add(new Guid("21D26293-3994-49ba-90F1-A27048F23011"));
            //_guids.Add(new Guid("986ABF29-396A-41aa-93B0-E2DA0AF47509"));
            _guids.Add(new Guid("4FFEB659-3AF4-4aa0-99FD-69A847E0F37A"));
            _guids.Add(new Guid("00000000-0000-0000-0000-000000000000"));
            _guids.Add(new Guid("681FEEC3-5714-45eb-9C86-39F5948783D8"));
            _guids.Add(new Guid("03095F5A-527B-4dd6-93EF-11E7A75A7B86"));
            _guids.Add(new Guid("77886897-7288-4488-AF76-4A874B555039"));
            _guids.Add(new Guid("BEAABECF-3019-4b88-849E-9EA69E4EA0E5"));
            _guids.Add(new Guid("00000000-0000-0000-0000-000000000000"));
            _guids.Add(new Guid("F3E5AF88-2BCE-44a5-AABF-E6ABF2EE1D42"));
            _guids.Add(new Guid("39758B9D-4163-442e-A43C-F8293BC2D630"));
            _guids.Add(new Guid("F87966B0-07E9-41a7-97E0-E97A659B8DA0"));
        }

        #region IToolbar Member

        //public bool Visible
        //{
        //    get
        //    {
        //        return _visible;
        //    }
        //    set
        //    {
        //        _visible = value;
        //    }
        //}

        public string Name
        {
            get { return LocalizedResources.GetResString("Toolbars.Graphics", "Toolbar Graphics"); }
        }

        public List<Guid> GUIDs
        {
            get
            {
                return _guids;
            }
            set
            {
                _guids = value;
            }
        }

        #endregion

        #region IPersistable Member

        void IPersistable.Load(IPersistStream stream)
        {
            //_visible = (bool)stream.Load("visible", true);
        }

        void IPersistable.Save(IPersistStream stream)
        {
            //stream.Save("visible", _visible);
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("895B7C49-6612-45ca-998A-1B897696799C")]
    public class DrawingMenu : ITool, IToolItem
    {
        private IMapDocument _doc = null;
        private ToolStripDropDownButton _menuItem;
        private ToolStripMenuItem _removeItem, _removeSelected, _removeAll;

        public DrawingMenu()
        {
            _menuItem = new ToolStripDropDownButton("Drawing");

            _removeItem = new ToolStripMenuItem("Remove");
            _removeSelected = new ToolStripMenuItem("Selected");
            _removeSelected.Click += new EventHandler(removeSelected_Click);
            _removeAll = new ToolStripMenuItem("All");
            _removeAll.Click += new EventHandler(removeAll_Click);

            _removeItem.DropDownItems.Add(_removeSelected);
            _removeItem.DropDownItems.Add(_removeAll);
            _removeItem.DropDownItems.Add(new ToolStripSeparator());

            PlugInManager compMan = new PlugInManager();
            foreach (var grType in compMan.GetPlugins(gView.Framework.system.Plugins.Type.IGraphicElement2))
            {
                IGraphicElement2 element2 = compMan.CreateInstance<IGraphicElement2>(grType);
                if (element2 == null)
                {
                    continue;
                }

                GraphicElement2MenuItem item = new GraphicElement2MenuItem(element2);
                item.Click += new EventHandler(removeGraphicElement_Click);
                _removeItem.DropDownItems.Add(item);
            }
            _menuItem.DropDownItems.Add(_removeItem);
        }

        void removeGraphicElement_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            GraphicElement2MenuItem item = sender as GraphicElement2MenuItem;
            if (item == null)
            {
                return;
            }

            foreach (IGraphicElement element in _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Clone())
            {
                if (element.GetType().Equals(item.ElementType))
                {
                    _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Remove(element);
                }
            }
            foreach (IGraphicElement element in _doc.FocusMap.Display.GraphicsContainer.Elements.Clone())
            {
                if (element.GetType().Equals(item.ElementType))
                {
                    _doc.FocusMap.Display.GraphicsContainer.Elements.Remove(element);
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        void removeAll_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Clear();
            _doc.FocusMap.Display.GraphicsContainer.Elements.Clear();

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        void removeSelected_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement element in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                _doc.FocusMap.Display.GraphicsContainer.Elements.Remove(element);
            }
            _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Clear();

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        #region ITool Member

        public string Name
        {
            get { return "Drawing"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _menuItem; }
        }

        #endregion
    }

    internal class GraphicElement2MenuItem : ToolStripMenuItem
    {
        private Type _type;

        public GraphicElement2MenuItem(IGraphicElement2 element)
        {
            if (element == null)
            {
                return;
            }

            base.Image = element.Icon;
            base.Text = element.Name;

            _type = element.GetType();
        }

        public Type ElementType
        {
            get { return _type; }
        }
    }

    internal class Rubberband : IGraphicElement
    {
        private SimpleLineSymbol _lineSymbol;
        private SimpleFillSymbol _fillSymbol;
        private Envelope _envelope;

        public Rubberband()
        {
            _lineSymbol = new SimpleLineSymbol();
            _lineSymbol.Color = ArgbColor.Blue;
            _lineSymbol.Width = 1;
            _lineSymbol.DashStyle = LineDashStyle.Dash;
            _fillSymbol = new SimpleFillSymbol();
            _fillSymbol.Color = ArgbColor.Transparent;
            _fillSymbol.OutlineSymbol = _lineSymbol;
            _envelope = new Envelope();
        }

        public void SetEnvelope(double minx, double miny, double maxx, double maxy)
        {
            _envelope.minx = Math.Min(minx, maxx);
            _envelope.miny = Math.Min(miny, maxy);
            _envelope.maxx = Math.Max(minx, maxx);
            _envelope.maxy = Math.Max(miny, maxy);
        }
        public IEnvelope Envelope
        {
            get { return _envelope; }
        }

        #region IGraphicElement Member

        public void Draw(IDisplay display)
        {
            if (display == null)
            {
                return;
            }
            //display.Draw(_fillSymbol, _envelope);
            display.Draw(_lineSymbol, _envelope);
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("FEEEE362-116B-406b-8D94-D817A9BAC121")]
    public class Pointer : ITool, IToolMouseActions, IToolKeyActions
    {
        Rubberband _rubberband;
        IGraphicsContainer _container, _ghostContainer;
        private IMapDocument _doc = null;
        private FillColor _fillColor = null;
        private PenColor _penColor = null;
        private FontColor _fontColor = null;

        public Pointer()
        {
            _rubberband = new Rubberband();
            _container = new GraphicsContainer();
            _container.Elements.Add(_rubberband);
            _ghostContainer = new GraphicsContainer();
        }

        #region ITool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.Pointer", "Pointer"); }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.userdefined; }
        }

        public object Image
        {
            get { return (new Buttons()).imageList1.Images[26]; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;

                if (_doc.Application is IMapApplication)
                {
                    _fillColor = ((IMapApplication)_doc.Application).Tool(new Guid("681FEEC3-5714-45eb-9C86-39F5948783D8")) as FillColor;
                    _penColor = ((IMapApplication)_doc.Application).Tool(new Guid("03095F5A-527B-4dd6-93EF-11E7A75A7B86")) as PenColor;
                    _fontColor = ((IMapApplication)_doc.Application).Tool(new Guid("F3E5AF88-2BCE-44a5-AABF-E6ABF2EE1D42")) as FontColor;
                }
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (!(MapEvent is MapEvent))
            {
                return Task.FromResult(true);
            }

            IMap map = ((MapEvent)MapEvent).Map;
            if (map == null || map.Display == null || map.Display.GraphicsContainer == null)
            {
                return Task.FromResult(true);
            }

            map.Display.GraphicsContainer.EditMode = GrabberMode.Pointer;
            ((MapEvent)MapEvent).drawPhase = DrawPhase.Graphics;
            ((MapEvent)MapEvent).refreshMap = true;

            return Task.FromResult(true);
        }

        #endregion

        #region IToolMouseActions Member

        int _oX = 0, _oY = 0;
        bool _mousePressed = false;
        IHitPositions _hit = null;
        IGraphicElement2 _hitElement = null;

        public void MouseDown(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            _mousePressed = true;
            _oX = e.X;
            _oY = e.Y;

            double x1 = _oX, y1 = _oY;
            display.Image2World(ref x1, ref y1);
            _rubberband.SetEnvelope(x1, y1, x1, y1);

            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseDown(display, e, world);
            }
        }

        public void MouseUp(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            _mousePressed = false;

            if (_hit != null && _hitElement != null)
            {
                double x1 = _oX, y1 = _oY;
                double x2 = e.X, y2 = e.Y;
                display.Image2World(ref x1, ref y1);
                display.Image2World(ref x2, ref y2);

                _hitElement.Design(display, _hit, x2 - x1, y2 - y1);
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                }
            }
            else
            {
                display.ClearOverlay();
                bool redraw = display.GraphicsContainer.SelectedElements.Count > 0;
                if ((_key & Keys.ShiftKey) != Keys.ShiftKey)
                {
                    display.GraphicsContainer.SelectedElements.Clear();
                }

                IEnvelope envelope = _rubberband.Envelope;
                IPoint point = null;
                double tol = 3.0 * display.MapScale / (display.Dpi / 0.0254);  // [m]
                if (envelope.Width < tol && envelope.Height < tol)
                {
                    point = new Point(0.5 * envelope.minx + 0.5 * envelope.maxx, 0.5 * envelope.miny + 0.5 * envelope.maxy);
                }
                else if (envelope.Width < tol)
                {
                    envelope.minx = 0.5 * envelope.minx + 0.5 * envelope.maxx - tol / 2.0;
                    envelope.maxx = 0.5 * envelope.minx + 0.5 * envelope.maxx + tol / 2.0;
                }
                else if (envelope.Height < tol)
                {
                    envelope.miny = 0.5 * envelope.miny + 0.5 * envelope.maxy - tol / 2.0;
                    envelope.maxy = 0.5 * envelope.miny + 0.5 * envelope.maxy + tol / 2.0;
                }

                foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.Elements.Swap())
                {
                    if (grElement is IGraphicElement2)
                    {
                        if (display.GraphicsContainer.SelectedElements.Contains(grElement))
                        {
                            continue;
                        }

                        if ((point != null) ? ((IGraphicElement2)grElement).TrySelect(display, point) : ((IGraphicElement2)grElement).TrySelect(display, _rubberband.Envelope))
                        {
                            display.GraphicsContainer.SelectedElements.Add(grElement);
                            redraw = true;
                            if (point != null)
                            {
                                break; // bei Punktselektion nur ersten selektieren...
                            }
                        }
                    }
                }
                if (redraw)
                {
                    if (_doc.Application is IMapApplication)
                    {
                        ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                    }
                }
            }

            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseUp(display, e, world);
            }

            _hit = null;
            _hitElement = null;
        }

        public void MouseClick(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseClick(display, e, world);
            }
        }

        public void MouseDoubleClick(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseDoubleClick(display, e, world);
            }
        }


        public void MouseMove(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            if (display == null)
            {
                return;
            }

            if (_mousePressed)
            {
                double x1 = _oX, y1 = _oY;
                double x2 = e.X, y2 = e.Y;
                display.Image2World(ref x1, ref y1);
                display.Image2World(ref x2, ref y2);

                if (_hit != null && _hitElement != null && _hitElement.Ghost != null)
                {
                    _hitElement.Ghost.Design(display, _hit, x2 - x1, y2 - y1);
                    display.DrawOverlay(_ghostContainer, true);
                }
                else
                {
                    _rubberband.SetEnvelope(x1, y1, x2, y2);
                    display.DrawOverlay(_container, true);
                }
            }
            else if (display.GraphicsContainer.SelectedElements.Count > 0)
            {
                double x1 = e.X, y1 = e.Y;
                display.Image2World(ref x1, ref y1);

                IPoint p = new Point(x1, y1);
                _ghostContainer.Elements.Clear();
                bool found = false;
                foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Swap())
                {
                    if (grElement is IGraphicElement2)
                    {
                        _hit = ((IGraphicElement2)grElement).HitTest(display, p);
                        if (_hit != null && _doc.Application is IMapApplication)
                        {
                            if (_doc.Application is IGUIApplication)
                            {
                                ((IGUIApplication)_doc.Application).SetCursor(_hit.Cursor as Cursor);
                            }

                            found = true;
                            _hitElement = grElement as IGraphicElement2;
                            if (_hitElement.Ghost != null)
                            {
                                _ghostContainer.Elements.Add(_hitElement.Ghost);
                            }

                            break;
                        }
                    }
                }
                if (!found && _doc.Application is IGUIApplication)
                {
                    ((IGUIApplication)_doc.Application).SetCursor(Cursors.Default);
                }
            }

            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseMove(display, e, world);
            }
        }

        public void MouseWheel(gView.Framework.Carto.IDisplay display, System.Windows.Forms.MouseEventArgs e, IPoint world)
        {
            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseWheel(display, e, world);
            }
        }

        #endregion

        #region IToolKeyActions Member
        private Keys _key = Keys.None;

        public void KeyDown(IDisplay display, KeyEventArgs e)
        {
            if (display == null || display.GraphicsContainer == null)
            {
                return;
            }

            _key = e.KeyCode;
            bool redraw = false;

            if (e.Shift == false && e.Control == false && e.Alt == false)
            {
                if (e.KeyCode == Keys.Escape) // ESC
                {
                    display.GraphicsContainer.SelectedElements.Clear();
                    redraw = true;
                }
                //else if (e.KeyCode == Keys.Delete) // Del
                //{
                //    foreach (IGraphicElement grElement in display.GraphicsContainer.SelectedElements)
                //    {
                //        display.GraphicsContainer.Elements.Remove(grElement);
                //    }
                //    display.GraphicsContainer.SelectedElements.Clear();
                //    redraw = true;
                //}
            }
            if (redraw && _doc != null && _doc.Application != null)
            {
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                }
            }
        }

        public void KeyPress(IDisplay display, KeyPressEventArgs e)
        {

        }

        public void KeyUp(IDisplay display, KeyEventArgs e)
        {
            _key = Keys.None;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("21D26293-3994-49ba-90F1-A27048F23011")]
    public class EditVertices : ITool, IToolMouseActions, IToolContextMenu
    {
        private IMapDocument _doc = null;
        private IGraphicsContainer _ghostContainer;
        private ContextMenuStrip _addVertexMenu, _removeVertexMenu, _currentMenu = null;
        private IPoint _displayPoint = new Point();

        public EditVertices()
        {
            _ghostContainer = new GraphicsContainer();

            _addVertexMenu = new ContextMenuStrip();
            ToolStripMenuItem addvertex = new ToolStripMenuItem("Add Vertex");
            addvertex.Click += new EventHandler(addvertex_Click);
            _addVertexMenu.Items.Add(addvertex);

            _removeVertexMenu = new ContextMenuStrip();
            ToolStripMenuItem removevertex = new ToolStripMenuItem("Remove Vertex");
            removevertex.Click += new EventHandler(removevertex_Click);
            _removeVertexMenu.Items.Add(removevertex);
        }

        #region ITool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.EditVertices", "Edit Vertices"); }
        }

        public bool Enabled
        {
            get
            {
                if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
                {
                    return false;
                }

                foreach (IGraphicElement element in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
                {
                    if (element is IConstructable && ((IConstructable)element).hasVertices)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.userdefined; }
        }

        public object Image
        {
            get { return (new Buttons()).imageList1.Images[27]; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (!(MapEvent is MapEvent))
            {
                return Task.FromResult(true);
            }

            IMap map = ((MapEvent)MapEvent).Map;
            if (map == null || map.Display == null || map.Display.GraphicsContainer == null)
            {
                return Task.FromResult(true);
            }

            map.Display.GraphicsContainer.EditMode = GrabberMode.Vertex;
            ((MapEvent)MapEvent).drawPhase = DrawPhase.Graphics;
            ((MapEvent)MapEvent).refreshMap = true;

            return Task.FromResult(true);
        }

        #endregion

        #region IToolMouseActions Member

        bool _mousePressed = false;
        IHitPositions _hit = null;
        IGraphicElement2 _hitElement = null;

        public void MouseDown(IDisplay display, MouseEventArgs e, IPoint world)
        {
            _mousePressed = e.Button == MouseButtons.Left;
        }

        public void MouseUp(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (!_mousePressed)
            {
                return;
            }

            _mousePressed = false;

            if (_hit != null && _hitElement != null)
            {
                double x2 = e.X, y2 = e.Y;
                display.Image2World(ref x2, ref y2);

                _hitElement.Design(display, _hit, x2, y2);
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                }
            }

            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseUp(display, e, world);
            }

            _hit = null;
            _hitElement = null;
        }

        public void MouseClick(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        public void MouseDoubleClick(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        public void MouseMove(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (display == null || e.Button == MouseButtons.Right)
            {
                return;
            }

            if (_mousePressed)
            {
                double x2 = e.X, y2 = e.Y;
                display.Image2World(ref x2, ref y2);

                if (_hit != null && _hitElement != null && _hitElement.Ghost != null)
                {
                    _hitElement.Ghost.Design(display, _hit, x2, y2);
                    display.DrawOverlay(_ghostContainer, true);
                }

                _currentMenu = null;
            }
            else if (display.GraphicsContainer.SelectedElements.Count > 0)
            {
                double x1 = e.X, y1 = e.Y;
                display.Image2World(ref x1, ref y1);
                _displayPoint.X = x1;
                _displayPoint.Y = y1;

                IPoint p = new Point(x1, y1);
                _ghostContainer.Elements.Clear();
                bool found = false;
                foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements.Swap())
                {
                    if (grElement is IGraphicElement2)
                    {
                        _hit = ((IGraphicElement2)grElement).HitTest(display, p);
                        if (_hit != null && _doc.Application is IMapApplication)
                        {
                            if (_doc.Application is IGUIApplication)
                            {
                                ((IGUIApplication)_doc.Application).SetCursor(_hit.Cursor as Cursor);
                            }

                            if (_hit.Cursor.Equals(HitCursors.VertexCursor))
                            {
                                _currentMenu = _removeVertexMenu;
                            }
                            else if ((GrabberIDs)_hit.HitID == GrabberIDs.path)
                            {
                                _currentMenu = _addVertexMenu;
                            }
                            else
                            {
                                _currentMenu = null;
                            }
                            found = true;
                            _hitElement = grElement as IGraphicElement2;
                            if (_hitElement.Ghost != null)
                            {
                                _ghostContainer.Elements.Add(_hitElement.Ghost);
                            }

                            break;
                        }
                        else
                        {
                            _currentMenu = null;
                        }
                    }
                }
                if (!found && _doc.Application is IGUIApplication)
                {
                    ((IGUIApplication)_doc.Application).SetCursor(Cursors.Default);
                }
            }

            if (_hitElement is IToolMouseActions)
            {
                ((IToolMouseActions)_hitElement).MouseMove(display, e, world);
            }
        }

        public void MouseWheel(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        #endregion

        #region IToolContextMenu Member

        public ContextMenuStrip ContextMenu
        {
            get { return _currentMenu; }
        }

        #endregion

        void removevertex_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null ||
                _hit == null || _hitElement == null ||
                !_hit.Cursor.Equals(HitCursors.VertexCursor))
            {
                return;
            }

            _hitElement.RemoveVertex(_doc.FocusMap.Display, _hit.HitID);

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        void addvertex_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null ||
                _hit == null || _hitElement == null)
            {
                return;
            }

            _hitElement.AddVertex(_doc.FocusMap.Display,
                new Point(_displayPoint.X, _displayPoint.Y));

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("4FFEB659-3AF4-4aa0-99FD-69A847E0F37A")]
    public class AddGraphicElement : IToolMenu
    {
        private List<ITool> _tools;
        private ITool _selectedTool = null;

        public AddGraphicElement()
        {
            _tools = new List<ITool>();

            PlugInManager compMan = new PlugInManager();
            foreach (var elementType in compMan.GetPlugins(gView.Framework.system.Plugins.Type.IGraphicElement2))
            {
                IGraphicElement2 element = compMan.CreateInstance<IGraphicElement2>(elementType);
                if (element == null)
                {
                    continue;
                }

                _tools.Add(new GraphicElementTool(element));
                if (_selectedTool == null)
                {
                    _selectedTool = _tools[0];
                }
            }
        }

        #region IToolMenu Member

        public List<ITool> DropDownTools
        {
            get { return gView.Framework.system.ListOperations<ITool>.Clone(_tools); }
        }

        public ITool SelectedTool
        {
            get
            {
                return _selectedTool;
            }
            set
            {
                if (_tools.IndexOf(value) != -1)
                {
                    _selectedTool = value;
                }
            }
        }

        #endregion

        #region ITool Member

        public string Name
        {
            get { return "Add Graphic Element"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return _selectedTool != null ? _selectedTool.ToolTip : ""; }
        }

        public ToolType toolType
        {
            get { return _selectedTool != null ? _selectedTool.toolType : ToolType.command; }
        }

        public object Image
        {
            get { return _selectedTool != null ? _selectedTool.Image : null; }
        }

        public void OnCreate(object hook)
        {
            foreach (ITool tool in _tools)
            {
                tool.OnCreate(hook);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion
    }

    internal class GraphicElementTool : ITool, IToolMouseActions
    {
        IGraphicElement2 _element = null;
        IMapDocument _doc = null;
        IGraphicsContainer _addContainer;

        public GraphicElementTool(IGraphicElement2 element)
        {
            if (element == null)
            {
                return;
            }

            _element = element;
        }

        #region ITool Member

        public string Name
        {
            get { return _element != null ? _element.Name : ""; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.userdefined; }
        }

        public object Image
        {
            get { return _element != null ? _element.Icon : null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolMouseActions Member

        int _oX = 0, _oY = 0;
        bool _mousePressed = false;

        public void MouseDown(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element == null)
            {
                return;
            }

            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseDown(_doc, e);
                return;
            }

            PlugInManager compMan = new PlugInManager();
            IGraphicElement2 element = compMan.CreateInstance(PlugInManager.PlugInID(_element)) as IGraphicElement2;
            if (element == null)
            {
                return;
            }

            _addContainer = new GraphicsContainer();
            _addContainer.Elements.Add(element);

            _mousePressed = true;
            _oX = e.X;
            _oY = e.Y;

            double x1 = _oX, y1 = _oY;
            display.Image2World(ref x1, ref y1);
            element.Translation(x1, y1);
        }

        public void MouseUp(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseUp(_doc, e);
                return;
            }

            _mousePressed = false;

            if (_addContainer != null && _addContainer.Elements.Count > 0)
            {
                if (display != null && display.GraphicsContainer != null && display.GraphicsContainer.Elements != null)
                {
                    //foreach (IGraphicElement2 element in _addContainer.Elements)
                    //{
                    //    display.GraphicsContainer.Elements.Add(element);

                    //    if (_doc != null && _doc.Application != null)
                    //        _doc.Application.RefreshActiveMap(DrawPhase.Graphics);
                    //}
                    GraphicShape.AddElementsToContainer(_doc, _addContainer.Elements);
                }
                _addContainer = null;
            }
        }

        public void MouseClick(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseClick(_doc, e);
                return;
            }

            if (_addContainer == null)
            {
                return;
            }
        }

        public void MouseDoubleClick(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseDoubleClick(_doc, e);
                return;
            }

            if (_addContainer == null)
            {
                return;
            }
        }

        public void MouseMove(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseMove(_doc, e);
                return;
            }

            if (_mousePressed)
            {
                double x1 = _oX, y1 = _oY;
                double x2 = e.X, y2 = e.Y;
                display.Image2World(ref x1, ref y1);
                display.Image2World(ref x2, ref y2);

                if (_addContainer != null && _addContainer.Elements.Count > 0)
                {
                    Envelope env = new Envelope(x1, y1, x2, y2);
                    foreach (IGraphicElement2 element in _addContainer.Elements)
                    {
                        element.Scale(x2 - x1, y2 - y1);
                        element.Translation(x1, y1);
                    }
                    display.DrawOverlay(_addContainer, true);
                }
            }
        }

        public void MouseWheel(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_element is IConstructable)
            {
                ((IConstructable)_element).ConstructMouseWheel(_doc, e);
                return;
            }

            if (_addContainer == null)
            {
                return;
            }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("681FEEC3-5714-45eb-9C86-39F5948783D8")]
    public class FillColor : ITool, IToolItem
    {
        gView.Framework.Symbology.UI.Controls.ToolStripColorPicker _picker;
        private IMapDocument _doc = null;

        public FillColor()
        {

        }

        void picker_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 18)
            {
                picker_SelectedColor(sender, e);
            }
        }

        void picker_SelectedColor(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IBrushColor)
                {
                    ((IBrushColor)grElement).FillColor = _picker.Color.ToArgbColor();
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        public System.Drawing.Color Color
        {
            get { return _picker.Color; }
        }

        #region ITool Member

        public string Name
        {
            get { return "Fill Color"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = hook as IMapDocument;

                _picker = new gView.Framework.Symbology.UI.Controls.ToolStripColorPicker(System.Drawing.Color.Red,
                    ((IMapApplication)_doc.Application).ApplicationWindow as Form);
                _picker.Image = gView.Win.Plugin.Tools.Properties.Resources.BucketFill;
                _picker.ImageTransparentColor = System.Drawing.Color.Magenta;
                _picker.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
                _picker.AddColorNameToToolTip = false;
                _picker.AllowNoColor = true;

                _picker.SelectedColorChanged += new EventHandler(picker_SelectedColor);
                _picker.MouseDown += new MouseEventHandler(picker_MouseDown);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _picker; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("03095F5A-527B-4dd6-93EF-11E7A75A7B86")]
    public class PenColor : ITool, IToolItem
    {
        gView.Framework.Symbology.UI.Controls.ToolStripColorPicker _picker;
        private IMapDocument _doc = null;

        public PenColor()
        {

        }

        void picker_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 18)
            {
                picker_SelectedColor(sender, e);
            }
        }

        void picker_SelectedColor(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IPenColor)
                {
                    ((IPenColor)grElement).PenColor = _picker.Color.ToArgbColor();
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        public System.Drawing.Color Color
        {
            get { return _picker.Color; }
        }

        #region ITool Member

        public string Name
        {
            get { return "Pen Color"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = hook as IMapDocument;

                _picker = new gView.Framework.Symbology.UI.Controls.ToolStripColorPicker(System.Drawing.Color.Blue,
                    ((IMapApplication)_doc.Application).ApplicationWindow as Form);
                _picker.Image = gView.Win.Plugin.Tools.Properties.Resources.PenDraw;
                _picker.ImageTransparentColor = System.Drawing.Color.Magenta;
                _picker.ImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
                _picker.AddColorNameToToolTip = false;
                _picker.AllowNoColor = true;

                _picker.SelectedColorChanged += new EventHandler(picker_SelectedColor);
                _picker.MouseDown += new MouseEventHandler(picker_MouseDown);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _picker; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("F3E5AF88-2BCE-44a5-AABF-E6ABF2EE1D42")]
    public class FontColor : ITool, IToolItem
    {
        gView.Framework.Symbology.UI.Controls.ToolStripColorPicker _picker;
        private IMapDocument _doc = null;

        public FontColor()
        {

        }

        void picker_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 18)
            {
                picker_SelectedColor(sender, e);
            }
        }

        void picker_SelectedColor(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IFontColor)
                {
                    ((IFontColor)grElement).FontColor = _picker.Color.ToArgbColor();
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        public System.Drawing.Color Color
        {
            get { return _picker.Color; }
        }

        #region ITool Member

        public string Name
        {
            get { return "Font Color"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = hook as IMapDocument;

                _picker = new gView.Framework.Symbology.UI.Controls.ToolStripColorPicker(System.Drawing.Color.Black,
                    ((IMapApplication)_doc.Application).ApplicationWindow as Form);
                _picker.Image = gView.Win.Plugin.Tools.Properties.Resources.TextColor;
                _picker.ImageTransparentColor = System.Drawing.Color.Magenta;
                _picker.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
                _picker.AddColorNameToToolTip = false;

                _picker.SelectedColorChanged += new EventHandler(picker_SelectedColor);
                _picker.MouseDown += new MouseEventHandler(picker_MouseDown);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _picker; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("77886897-7288-4488-AF76-4A874B555039")]
    public class LineWidth : ITool, IToolItem
    {
        ToolStripLineWidthPicker _picker;
        private IMapDocument _doc = null;

        #region ITool Member

        public string Name
        {
            get { return "Line Width Picker"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = hook as IMapDocument;

                _picker = new ToolStripLineWidthPicker(
                    ((IMapApplication)_doc.Application).ApplicationWindow as Form);
                _picker.Image = gView.Win.Plugin.Tools.Properties.Resources.penWidth;
                _picker.PenWidthSelected += new EventHandler(picker_PenWidthSelected);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _picker; }
        }

        #endregion

        void picker_PenWidthSelected(object sender, EventArgs e)
        {
            if (_picker == null)
            {
                return;
            }

            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IPenWidth)
                {
                    ((IPenWidth)grElement).PenWidth = _picker.PenWidth;
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("BEAABECF-3019-4b88-849E-9EA69E4EA0E5")]
    public class DashStyle : ITool, IToolItem
    {
        ToolStripDashStylePicker _picker;
        private IMapDocument _doc = null;

        #region ITool Member

        public string Name
        {
            get { return "Dash Style Picker"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = hook as IMapDocument;

                _picker = new ToolStripDashStylePicker(
                    ((IMapApplication)_doc.Application).ApplicationWindow as Form);
                _picker.Image = gView.Win.Plugin.Tools.Properties.Resources.dashstyle;
                _picker.PenDashStyleSelected += new EventHandler(picker_DashStyleSelected);
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _picker; }
        }

        #endregion

        void picker_DashStyleSelected(object sender, EventArgs e)
        {
            if (_picker == null)
            {
                return;
            }

            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IPenDashStyle)
                {
                    ((IPenDashStyle)grElement).PenDashStyle = _picker.PenDashStyle;
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("39758B9D-4163-442e-A43C-F8293BC2D630")]
    public class FontName : ITool, IToolItem
    {
        private IMapDocument _doc = null;
        private ToolStripComboBox _combo;

        public FontName()
        {
            _combo = new ToolStripComboBox();
            _combo.DropDownStyle = ComboBoxStyle.DropDownList;

            foreach (System.Drawing.FontFamily family in System.Drawing.FontFamily.Families)
            {
                _combo.Items.Add(family.Name);
                if (family.Name == "Arial")
                {
                    _combo.SelectedIndex = _combo.Items.Count - 1;
                }
            }
            if (_combo.SelectedIndex == -1)
            {
                _combo.SelectedIndex = 0;
            }

            _combo.Size = new System.Drawing.Size(90, 20);
            _combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);

            using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1))
            {
                using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bm))
                {
                    _combo.DropDownWidth = _combo.Size.Width;
                    foreach (object obj in _combo.Items)
                    {
                        System.Drawing.SizeF size = gr.MeasureString(obj.ToString(), _combo.Font);

                        if (size.Width + 20 > _combo.DropDownWidth)
                        {
                            _combo.DropDownWidth = (int)size.Width + 20;
                        }
                    }
                }
            }
        }

        #region ITool Member

        public string Name
        {
            get { return "Font"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _combo; }
        }

        #endregion

        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IFontSymbol)
                {
                    var font = ((IFontSymbol)grElement).Font;
                    ((IFontSymbol)grElement).Font = GraphicsEngine.Current.Engine.CreateFont(_combo.SelectedItem.ToString(), font.Size);
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("F87966B0-07E9-41a7-97E0-E97A659B8DA0")]
    public class FontSize : ITool, IToolItem
    {
        private IMapDocument _doc = null;
        private ToolStripComboBox _combo;

        public FontSize()
        {
            _combo = new ToolStripComboBox();
            _combo.DropDownStyle = ComboBoxStyle.DropDownList;

            for (int i = 3; i <= 100; i++)
            {
                _combo.Items.Add(i.ToString());
            }

            for (int i = 105; i <= 300; i += 5)
            {
                _combo.Items.Add(i.ToString());
            }

            for (int i = 310; i <= 500; i += 10)
            {
                _combo.Items.Add(i.ToString());
            }

            _combo.SelectedIndex = 7;
            _combo.Size = new System.Drawing.Size(40, 20);
            _combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);
        }

        #region ITool Member

        public string Name
        {
            get { return "Font Size"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _combo; }
        }

        #endregion

        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IFontSymbol)
                {
                    var font = ((IFontSymbol)grElement).Font;
                    ((IFontSymbol)grElement).Font = GraphicsEngine.Current.Engine.CreateFont(font.Name, (float)Convert.ToDecimal(_combo.SelectedItem.ToString()));
                }
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }
}
