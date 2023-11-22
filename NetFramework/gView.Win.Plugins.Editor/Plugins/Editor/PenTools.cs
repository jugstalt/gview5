using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Editor.Core;
using gView.Framework.Geometry;
using gView.Framework.LinAlg;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine;
using gView.Plugins.Editor.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace gView.Plugins.Editor
{
    class ParentPenToolMenuItem
    {
        public delegate void SelectedPenToolChangedEventHandler(object sender, IPenTool penTool);
        public event SelectedPenToolChangedEventHandler SelectedPenToolChanged = null;

        private PenToolMenuItem _selected = null;
        private IPoint _world = null, _contextPoint = null, _contextVertex = null, _mouseWorldPoint = null;
        private List<ToolStripItem> _items = new List<ToolStripItem>();
        private Module _module = null;
        private ToolStripMenuItem _toolsItem, _cancelPen;

        public ParentPenToolMenuItem()
        {
            _toolsItem = new ToolStripMenuItem();
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new PenTool(), true));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new OrthoPenTool()));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new ConstructMiddlePoint()));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new ConstructDirectionDirection()));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new ConstructDistanceDistance()));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new ConstructDistanceDirection()));
            _toolsItem.DropDownItems.Add(new PenToolMenuItem(new ConstructDistanceTangent()));
            _items.Add(_toolsItem);

            _items.Add(new ToolStripSeparator());
            ToolStripMenuItem snapTo = new ToolStripMenuItem(Globalisation.GetResString("SnapTo"));
            snapTo.DropDownItems.Add(new CalcToolMenuItem(new SnapToPenTool(this)));
            snapTo.DropDownItems.Add(new CalcToolMenuItem(new SegmentMidpoint(this)));
            snapTo.DropDownItems.Add(new CalcToolMenuItem(new SegmentOrtho(this)));
            _items.Add(snapTo);
            _items.Add(new ToolStripSeparator());

            _items.Add(new CalcToolMenuItem(new DirectionPenTool(this)));
            _items.Add(new CalcToolMenuItem(new DeflectionPenTool(this)));
            _items.Add(new CalcToolMenuItem(new PerpenticualPenTool(this)));
            _items.Add(new CalcToolMenuItem(new ParallelPenTool(this)));
            _items.Add(new ToolStripSeparator());
            _items.Add(new CalcToolMenuItem(new DistancePenTool(this)));
            _items.Add(new CalcToolMenuItem(new SegmentDistance(this)));
            _items.Add(new ToolStripSeparator());
            _items.Add(new CalcToolMenuItem(new AbsolutXYPenTool(this)));
            _items.Add(new CalcToolMenuItem(new DeltaXYPenTool(this)));
            _items.Add(new CalcToolMenuItem(new DirectionDistancePenTool(this)));

            _cancelPen = new ToolStripMenuItem("Cancel");
            _cancelPen.Click += new EventHandler(cancelPen_Click);
            SetStandardPenTool();

            foreach (ToolStripItem item in _toolsItem.DropDownItems)
            {
                if (item is PenToolMenuItem)
                {
                    ((PenToolMenuItem)item).Click += new EventHandler(PenTool_Click);
                }
            }
            foreach (ToolStripItem item in _items)
            {
                if (item is CalcToolMenuItem)
                {
                    ((CalcToolMenuItem)item).Click += new EventHandler(Tool_Click);
                }
                else if (item is ToolStripMenuItem)
                {
                    foreach (ToolStripItem item2 in ((ToolStripMenuItem)item).DropDownItems)
                    {
                        if (item2 is CalcToolMenuItem)
                        {
                            ((CalcToolMenuItem)item2).Click += new EventHandler(Tool_Click);
                        }
                    }
                }
            }
            _toolsItem.Image = _selected.PenTool.Image as System.Drawing.Image;
            _toolsItem.Text = _selected.Text;
        }

        void PenTool_Click(object sender, EventArgs e)
        {
            if (!(sender is PenToolMenuItem))
            {
                return;
            }

            int index = -1;
            if (_selected != null)
            {
                _selected.Checked = false;
                if (_selected.PenTool != null)
                {
                    _selected.Image = _selected.PenTool.Image as System.Drawing.Image;
                }
                index = _toolsItem.DropDownItems.IndexOf(_selected);
            }
            _selected = (PenToolMenuItem)sender;
            _selected.Image = null;
            _selected.Checked = true;

            if (_selected.PenTool != null)
            {
                int pointCount = (_module != null && _module.Sketch != null && _module.Sketch.Part != null) ?
                    _module.Sketch.Part.PointCount :
                    0;

                if (!_selected.PenTool.Activated(_contextPoint, _mouseWorldPoint, _contextVertex))
                {
                    if (index != 1)
                    {
                        SetPenTool(index);
                    }
                    else
                    {
                        SetStandardPenTool();
                    }
                }
                else
                {
                    if (_selected != null && _selected.PenTool != null)
                    {
                        _toolsItem.Image = _selected.PenTool.Image as System.Drawing.Image;
                        _toolsItem.Text = "Active Tool: " + _selected.Text;

                        if (SelectedPenToolChanged != null)
                        {
                            SelectedPenToolChanged(this, _selected.PenTool);
                        }
                    }
                    else
                    {
                        _toolsItem.Image = null;
                        _toolsItem.Text = "Active Tool: ???";
                    }
                }

                if (_module != null &&
                    _module.Sketch != null &&
                    _module.Sketch.Part != null &&
                    _module.Sketch.Part.PointCount == pointCount + 1)
                {
                    this.PerformClick();
                }
            }
        }
        void Tool_Click(object sender, EventArgs e)
        {
            if (_selected == null || _selected.PenTool == null)
            {
                return;
            }

            if (!(sender is CalcToolMenuItem) ||
                ((CalcToolMenuItem)sender).CalcTool == null)
            {
                return;
            }

            object result = ((CalcToolMenuItem)sender).CalcTool.Calc(_contextPoint, _mouseWorldPoint, _contextVertex);
            if (result != null)
            {
                _selected.PenTool.EvaluateCalcToolResult(
                    ((CalcToolMenuItem)sender).CalcTool.ResultType,
                    result);
            }
        }
        void cancelPen_Click(object sender, EventArgs e)
        {
            SetStandardPenTool();
        }

        public void OnCreate(IModule module)
        {
            _module = module as Module;
            foreach (ToolStripItem item in _toolsItem.DropDownItems)
            {
                if (item is PenToolMenuItem &&
                    ((PenToolMenuItem)item).PenTool != null)
                {
                    ((PenToolMenuItem)item).PenTool.OnCreate(module);
                }
            }
            foreach (ToolStripItem item in _items)
            {
                if (item is PenToolMenuItem &&
                    ((PenToolMenuItem)item).PenTool != null)
                {
                    ((PenToolMenuItem)item).PenTool.OnCreate(module);
                }
                else if (item is CalcToolMenuItem &&
                    ((CalcToolMenuItem)item).CalcTool != null)
                {
                    ((CalcToolMenuItem)item).CalcTool.OnCreate(module);
                }
                else if (item is ToolStripMenuItem)
                {
                    foreach (ToolStripItem item2 in ((ToolStripMenuItem)item).DropDownItems)
                    {
                        if (item2 is PenToolMenuItem &&
                            ((PenToolMenuItem)item2).PenTool != null)
                        {
                            ((PenToolMenuItem)item2).PenTool.OnCreate(module);
                        }
                        else if (item2 is CalcToolMenuItem &&
                            ((CalcToolMenuItem)item2).CalcTool != null)
                        {
                            ((CalcToolMenuItem)item2).CalcTool.OnCreate(module);
                        }
                    }
                }
            }
        }
        public void PerformClick()
        {
            if (_selected != null && _selected.PenTool != null)
            {
                if (!_selected.PenTool.MouseClick())
                {
                    SetStandardPenTool();
                }
            }
        }
        public IPoint ContextPoint
        {
            get { return _contextPoint; }
            set { _contextPoint = value; }
        }
        public IPoint MouseWorldPoint
        {
            get { return _mouseWorldPoint; }
            set { _mouseWorldPoint = value; }
        }
        public IPoint ContextVertex
        {
            get { return _contextVertex; }
            set { _contextVertex = value; }
        }
        public List<ToolStripItem> MenuItems
        {
            get
            {
                List<ToolStripItem> items = ListOperations<ToolStripItem>.Clone(_items);

                if (_selected != null &&
                    !(_selected.PenTool is PenTool))
                {
                    _cancelPen.Text = "Cancel" + ((_selected.PenTool != null) ? ": " + _selected.PenTool.Name : "");
                    items.Insert(0, _cancelPen);
                }
                foreach (ToolStripItem item in _items)
                {
                    if (item is CalcToolMenuItem &&
                        ((CalcToolMenuItem)item).CalcTool != null)
                    {
                        if (_selected != null && _selected.PenTool != null)
                        {
                            CalcToolMenuItem citem = (CalcToolMenuItem)item;
                            item.Enabled = (citem.CalcTool.Enabled == true &&
                                          _selected.PenTool.UseCalcToolResultType(citem.CalcTool.ResultType));
                        }
                        else
                        {
                            item.Enabled = false;
                        }
                    }
                }
                return items;
            }
        }
        public bool DrawMover
        {
            get
            {
                if (_selected != null && _selected.PenTool != null)
                {
                    return _selected.PenTool.DrawMover;
                }
                return false;
            }
        }
        private void SetStandardPenTool()
        {
            SetPenTool(0);
        }
        private void SetPenTool(int index)
        {
            if (index < 0 || index >= _toolsItem.DropDownItems.Count)
            {
                return;
            }

            if (_selected != _toolsItem.DropDownItems[index])
            {
                if (_selected != null)
                {
                    _selected.Checked = false;
                }

                if (_selected != null && _selected.PenTool != null)
                {
                    _selected.Image = _selected.PenTool.Image as System.Drawing.Image;
                }

                _selected = _toolsItem.DropDownItems[index] as PenToolMenuItem;
                if (_selected == null)
                {
                    return;
                }

                _selected.Image = null;
                _selected.Checked = true;

                if (_selected != null && _selected.PenTool != null)
                {
                    _toolsItem.Image = _selected.PenTool.Image as System.Drawing.Image;
                    _toolsItem.Text = "Active Tool: " + _selected.Text;
                    if (SelectedPenToolChanged != null)
                    {
                        SelectedPenToolChanged(this, _selected.PenTool);
                    }
                }
                else
                {
                    _toolsItem.Image = null;
                    _toolsItem.Text = "Active Tool: ???";
                }
                //if (_selected.PenTool != null)
                //    _selected.PenTool.Activated(_world, _world);
            }
        }

        public IPenTool ActivePenTool
        {
            get
            {
                return _selected.PenTool;
            }
        }

        #region MenuItemClasses
        private class PenToolMenuItem : ToolStripMenuItem
        {
            private IPenTool _penTool;

            public PenToolMenuItem(IPenTool penTool)
            {
                _penTool = penTool;
                if (_penTool == null)
                {
                    return;
                }

                this.Text = _penTool.Name;
                this.Image = _penTool.Image as System.Drawing.Image;
            }

            public PenToolMenuItem(IPenTool penTool, bool check)
                : this(penTool)
            {
                this.Image = null;
                this.Checked = check;
            }

            public IPenTool PenTool
            {
                get { return _penTool; }
            }
        }
        private class CalcToolMenuItem : ToolStripMenuItem
        {
            private ICalcTool _tool;

            public CalcToolMenuItem(ICalcTool penTool)
            {
                _tool = penTool;
                if (_tool == null)
                {
                    return;
                }

                this.Text = _tool.Name;
                this.Image = _tool.Image as System.Drawing.Image;
            }

            public CalcToolMenuItem(ICalcTool penTool, bool check)
                : this(penTool)
            {
                this.Image = null;
                this.Checked = check;
            }

            public ICalcTool CalcTool
            {
                get { return _tool; }
            }
        }
        #endregion

        public IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            _world = world;
            if (_selected != null && _selected.PenTool != null)
            {
                return _selected.PenTool.CalcPoint(mouseX, mouseY, world);
            }
            else
            {
                return world;
            }
        }

        internal static IPath QueryPathSegment(IMap map, IPoint point)
        {
            return QueryPathSegment(map, point, true);
        }

        internal static IPath QueryPathSegment(IMap map, IPoint point, bool blink)
        {
            if (map == null || map.TOC == null || map.Display == null || point == null)
            {
                return null;
            }

            List<ILayer> layers = map.TOC.VisibleLayers;
            double tol = 6 * map.Display.MapScale / (96 / 0.0254);  // [m]
            if (map.Display.SpatialReference != null &&
                map.Display.SpatialReference.SpatialParameters.IsGeographic)
            {
                tol = (180.0 * tol / Math.PI) / 6370000.0;
            }
            Envelope envelope = new Envelope(point.X - tol / 2, point.Y - tol / 2, point.X + tol / 2, point.Y + tol / 2);
            SpatialFilter filter = new SpatialFilter();
            filter.FilterSpatialReference = map.Display.SpatialReference;
            filter.FeatureSpatialReference = map.Display.SpatialReference;
            filter.Geometry = envelope;

            IPath coll = null;
            double dist = double.MaxValue;
            foreach (ILayer layer in layers)
            {
                if (!(layer is IFeatureLayer) ||
                    ((IFeatureLayer)layer).FeatureClass == null)
                {
                    continue;
                }

                if (layer.MinimumScale > 1 && layer.MinimumScale > map.Display.MapScale)
                {
                    continue;
                }

                if (layer.MaximumScale > 1 && layer.MaximumScale < map.Display.MapScale)
                {
                    continue;
                }

                IFeatureClass fc = ((IFeatureLayer)layer).FeatureClass;

                if (fc.GeometryType != GeometryType.Polyline &&
                    fc.GeometryType != GeometryType.Polygon)
                {
                    continue;
                }

                filter.SubFields = fc.ShapeFieldName;
                double distance = 0.0;
                using (IFeatureCursor cursor = fc.GetFeatures(filter).Result)
                {
                    IFeature feature;
                    while ((feature = cursor.NextFeature().Result) != null)
                    {
                        if (feature.Shape == null)
                        {
                            continue;
                        }

                        int partNr, pointNr;
                        IPoint p = gView.Framework.SpatialAlgorithms.Algorithm.NearestPointToPath(feature.Shape, point, out distance, false, false, out partNr, out pointNr);
                        if (distance <= tol && distance < dist &&
                            partNr >= 0 && pointNr >= 0)
                        {
                            List<IPath> paths = gView.Framework.SpatialAlgorithms.Algorithm.GeometryPaths(feature.Shape);
                            if (paths == null || paths.Count <= partNr)
                            {
                                continue;
                            }

                            IPath path = paths[partNr];
                            if (path == null || path.PointCount <= pointNr - 1)
                            {
                                continue;
                            }

                            coll = new Path();
                            coll.AddPoint(path[pointNr]);
                            coll.AddPoint(path[pointNr + 1]);

                            dist = distance;
                        }
                    }
                }
            }

            if (coll != null && blink)
            {
                Polyline pLine = new Polyline();
                pLine.AddPath(coll);
                map.HighlightGeometry(pLine, 300);
            }
            return coll;
        }
    }

    class StandardPenTool : IPenTool
    {
        protected Module _module = null;

        #region IPenTool Member

        virtual public IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            return world;
        }

        virtual public bool MouseClick()
        {
            return true;
        }

        virtual public void OnCreate(IModule module)
        {
            _module = module as Module;
        }

        virtual public string Name
        {
            get { return String.Empty; }
        }

        virtual public object Image
        {
            get { return null; }
        }

        virtual public bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            return true;
        }

        virtual public bool DrawMover
        {
            get { return true; }
        }

        virtual public bool UseCalcToolResultType(CalcToolResultType type)
        {
            return false;
        }
        virtual public void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
        }
        #endregion

        protected void RefrshSketch()
        {
            if (_module != null &&
                _module.MapDocument != null &&
                _module.MapDocument.Application is IMapApplication)
            {
                ((IMapApplication)_module.MapDocument.Application).RefreshActiveMap(DrawPhase.Graphics);
                Thread.Sleep(300);
            }
        }

        protected void AddPointToSketch(IPoint point)
        {
            if (_module != null &&
                _module.Sketch != null &&
                point != null)
            {
                _module.Sketch.AddPoint(new Point(point));
            }
            RefrshSketch();
        }

        protected void SetStatusText(string text)
        {
            if (_module != null &&
                _module.MapDocument != null &&
                _module.MapDocument.Application is IMapApplication &&
                ((IMapApplication)_module.MapDocument.Application).StatusBar != null)
            {
                ((IMapApplication)_module.MapDocument.Application).StatusBar.Text = text;
            }
        }
        static public IPoint CalcPoint(Module module, IPoint world, double direction, double distance, IPoint refPoint)
        {
            IPoint p1 = null;
            if (!double.IsNaN(direction))
            {
                if (refPoint != null)
                {
                    p1 = refPoint;
                }
                else if (module != null &&
                    module.Sketch != null &&
                    module.Sketch.Part != null &&
                    module.Sketch.Part.PointCount > 0)
                {
                    p1 = module.Sketch.Part[module.Sketch.Part.PointCount - 1];
                }

                if (p1 == null)
                {
                    return null;
                }

                Point r = new Point(Math.Cos(direction), Math.Sin(direction));
                Point r_ = new Point(-r.Y, r.X);

                LinearEquation2 linarg = new LinearEquation2(
                    world.X - p1.X,
                    world.Y - p1.Y,
                    r.X, r_.X,
                    r.Y, r_.Y);

                if (linarg.Solve())
                {
                    double t1 = linarg.Var1;
                    //double t2 = linarg.Var2;

                    return new Point(p1.X + r.X * t1, p1.Y + r.Y * t1);
                }
                return null;
            }


            else if (!double.IsNaN(distance))
            {
                if (refPoint != null)
                {
                    p1 = refPoint;
                }
                else if (module != null &&
                    module.Sketch != null &&
                    module.Sketch.Part != null &&
                    module.Sketch.Part.PointCount > 0)
                {
                    p1 = module.Sketch.Part[module.Sketch.Part.PointCount - 1];
                }

                if (p1 == null)
                {
                    return null;
                }

                double dx = world.X - p1.X;
                double dy = world.Y - p1.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);

                dx /= len; dy /= len;

                return new Point(p1.X + dx * distance, p1.Y + dy * distance);
            }

            return world;

        }

    }
    class PenTool : StandardPenTool
    {
        private double _direction = double.NaN, _distance = double.NaN;

        override public string Name
        {
            get { return Globalisation.GetResString("Pen"); }
        }

        override public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.edit_blue; }
        }

        public override bool MouseClick()
        {
            _direction = double.NaN;
            _distance = double.NaN;
            return true;
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _direction = double.NaN;
            _distance = double.NaN;
            return true;
        }

        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            return StandardPenTool.CalcPoint(_module, world, _direction, _distance, null);
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Direction:
                    return true;
                case CalcToolResultType.Distance:
                    return true;
                case CalcToolResultType.SnapTo:
                    return true;
            }
            return false;
        }
        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (_module == null ||
                _module.Sketch == null)
            {
                return;
            }

            _distance = _direction = double.NaN;
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                case CalcToolResultType.SnapTo:
                    if (result is IPoint)
                    {
                        _module.Sketch.AddPoint((IPoint)result);
                        RefrshSketch();
                    }
                    break;
                case CalcToolResultType.Direction:
                    if (result.GetType() == typeof(double))
                    {
                        _direction = (double)result;
                    }
                    break;
                case CalcToolResultType.Distance:
                    if (result.GetType() == typeof(double))
                    {
                        _distance = (double)result;
                    }
                    break;
            }
        }
    }
    class OrthoPenTool : StandardPenTool
    {
        private double _alpha = double.NaN, _direction = double.NaN;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("Orthogonal");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.ortho;
            }
        }

        public override bool MouseClick()
        {
            _direction = double.NaN;
            return true;
        }
        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _direction = double.NaN;
            return true;
        }

        public override gView.Framework.Geometry.IPoint CalcPoint(int mouseX, int mouseY, gView.Framework.Geometry.IPoint world)
        {
            if (_module == null)
            {
                return base.CalcPoint(mouseX, mouseY, world);
            }

            EditSketch sketch = _module.Sketch;
            IPointCollection part = (sketch != null) ? sketch.Part : null;
            if (sketch == null)
            {
                return base.CalcPoint(mouseX, mouseY, world);
            }
            else
            {
                double alpha = double.NaN;
                IPoint p2 = null;
                if (part != null && part.PointCount >= 2)
                {
                    IPoint p1 = part[part.PointCount - 2];
                    p2 = part[part.PointCount - 1];

                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    alpha = Math.Atan2(dy, dx);
                }
                else if (part != null && part.PointCount == 1 && !double.IsNaN(_direction))
                {
                    p2 = part[part.PointCount - 1];
                    alpha = _direction + Math.PI / 2.0;
                }
                else
                {
                    return base.CalcPoint(mouseX, mouseY, world);
                }
                IPoint resP1 = null;
                IPoint resP2 = null;

                Point r = new Point(Math.Sin(alpha), -Math.Cos(alpha));
                Point r_ = new Point(-r.Y, r.X);

                #region Ortho
                LinearEquation2 linarg = new LinearEquation2(
                    world.X - p2.X,
                    world.Y - p2.Y,
                    r.X, r_.X,
                    r.Y, r_.Y);

                if (linarg.Solve())
                {
                    double t1 = linarg.Var1;
                    double t2 = linarg.Var2;

                    resP1 = new Point(p2.X + r.X * t1, p2.Y + r.Y * t1);
                }
                #endregion

                #region Otrho 2
                if (part != null && part.PointCount >= 2)
                {
                    r = new Point(Math.Cos(alpha), Math.Sin(alpha));
                    r_ = new Point(-r.Y, r.X);

                    linarg = new LinearEquation2(
                        world.X - p2.X,
                        world.Y - p2.Y,
                        r.X, r_.X,
                        r.Y, r_.Y);

                    if (linarg.Solve())
                    {
                        double t1 = linarg.Var1;
                        double t2 = linarg.Var2;

                        resP2 = new Point(p2.X + r.X * t1, p2.Y + r.Y * t1);
                    }
                }
                #endregion

                if (resP1 != null && resP2 != null)
                {
                    if (gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(world, resP1) <=
                        gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(world, resP2))
                    {
                        _alpha = Math.Atan2(resP1.Y - p2.Y, resP1.X - p2.X);
                        return new Point(resP1.X, resP1.Y);
                    }
                    else
                    {
                        _alpha = Math.Atan2(resP2.Y - p2.Y, resP2.X - p2.X);
                        return new Point(resP2.X, resP2.Y);
                    }
                }
                else if (resP1 != null)
                {
                    _alpha = Math.Atan2(resP1.Y - p2.Y, resP1.X - p2.X);
                    return new Point(resP1.X, resP1.Y);
                }
                else
                {
                    _alpha = Math.Atan2(resP2.Y - p2.Y, resP2.X - p2.X);
                    return new Point(resP2.X, resP2.Y);
                }
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            if (_module == null)
            {
                return false;
            }

            switch (type)
            {
                case CalcToolResultType.Distance:
                    return !double.IsNaN(_alpha);
                case CalcToolResultType.AbsolutPos:
                    if (_module.Sketch == null || _module.Sketch.Part == null)
                    {
                        return true;
                    }

                    return _module.Sketch.Part.PointCount < 2;
                case CalcToolResultType.Direction:
                    if (_module.Sketch == null || _module.Sketch.Part == null)
                    {
                        return false;
                    }

                    return _module.Sketch.Part.PointCount == 1;
                case CalcToolResultType.SnapTo:
                    return true;
            }
            return false;
        }

        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (result == null || _module == null || _module.Sketch == null)
            {
                return;
            }

            if (type == CalcToolResultType.Distance &&
                result.GetType() == typeof(double) &&
                _module.Sketch.Part != null &&
                _module.Sketch.Part.PointCount > 0 &&
                !double.IsNaN(_alpha))
            {
                IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];
                double dist = (double)result;

                _module.Sketch.AddPoint(
                    new Point(p1.X + Math.Cos(_alpha) * dist,
                              p1.Y + Math.Sin(_alpha) * dist));

                RefrshSketch();
            }
            else if (type == CalcToolResultType.AbsolutPos &&
                    result is IPoint &&
                    (_module.Sketch.Part == null ||
                    _module.Sketch.Part.PointCount < 2))
            {
                _module.Sketch.AddPoint(new Point((IPoint)result));
                RefrshSketch();
            }
            else if (type == CalcToolResultType.Direction &&
               _module.Sketch.Part != null &&
               _module.Sketch.Part.PointCount == 1 &&
               result.GetType() == typeof(double))
            {
                _direction = (double)result;
            }
            else if (type == CalcToolResultType.SnapTo &&
                     result is IPoint)
            {
                _module.Sketch.AddPoint(new Point((IPoint)result));
                RefrshSketch();
            }
        }
    }
    class ConstructPenTool : StandardPenTool
    {
        static IGraphicsContainer _constContainer;
        private static SimplePointSymbol _pointSymbol = null;
        private static SimpleLineSymbol _lineSymbol = null;

        public ConstructPenTool()
        {
            _constContainer = new GraphicsContainer();

            if (_lineSymbol == null)
            {
                _lineSymbol = new SimpleLineSymbol();
                _lineSymbol.Color = ArgbColor.Gray;
            }

            if (_pointSymbol == null)
            {
                _pointSymbol = new SimplePointSymbol();
                _pointSymbol.Marker = SimplePointSymbol.MarkerType.Square;
                _pointSymbol.Color = ArgbColor.Red;
                _pointSymbol.Size = 8;
                _pointSymbol.PenColor = ArgbColor.Yellow;
                _pointSymbol.SymbolWidth = 2;
            }
        }

        protected void DrawConstructionSketch(IGeometry geometry)
        {
            if (_module == null || _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null)
            {
                return;
            }

            _constContainer.Elements.Clear();
            if (geometry is IPoint ||
                geometry is IMultiPoint)
            {
                _constContainer.Elements.Add(new PointGraphics(geometry));
            }
            else if (geometry is IPolyline ||
                    geometry is IPolygon)
            {
                _constContainer.Elements.Add(new LineGraphics(geometry));
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    IGeometry g = ((IAggregateGeometry)geometry)[i];
                    if (g is IPoint ||
                        g is IMultiPoint)
                    {
                        _constContainer.Elements.Add(new PointGraphics(g));
                    }
                    else if (g is IPolyline ||
                             g is IPolygon)
                    {
                        _constContainer.Elements.Add(new LineGraphics(g));
                    }
                }
            }

            //((IMapApplication)_module.MapDocument.Application).DrawReversibleGeometry(geometry, System.Drawing.Color.Gray);
            _module.MapDocument.FocusMap.Display.DrawOverlay(_constContainer, true);
        }

        #region Helper
        internal static void BuildCircle(Path path, IPoint middle, double raduis, double maxVertexDistance)
        {
            if (path == null || middle == null)
            {
                return;
            }

            double to = 2.0 * Math.PI;

            //double circum = 2.0 * raduis * Math.PI;
            //int numPoints = (int)(((double)(circum / maxVertexDistance)) + 1);
            //double step = 2.0 * Math.PI / numPoints;


            double step = Math.PI / 30.0;

            path.RemoveAllPoints();

            for (double w = 0.0; w < to; w += step)
            {
                path.AddPoint(new Point(middle.X + raduis * Math.Cos(w),
                                        middle.Y + raduis * Math.Sin(w)));
            }
            if (path.PointCount > 0)
            {
                path.AddPoint(path[0]);
            }
        }
        internal static void BuildRay(Path path, IPoint p1, IPoint p2, IEnvelope env)
        {
            if (path == null || p1 == null || p2 == null ||
                env == null)
            {
                return;
            }

            double minLength = Math.Max(env.Width, env.Height);
            double length = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(p1, p2);
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            path.RemoveAllPoints();
            path.AddPoint(new Point(p2.X + dx / length * minLength,
                                    p2.Y + dy / length * minLength));
            path.AddPoint(new Point(p2.X - dx / length * minLength,
                                    p2.Y - dy / length * minLength));
        }
        #endregion

        #region HelperClasses
        private class LineGraphics : IGraphicElement
        {
            private IGeometry _geometry;

            public LineGraphics(IGeometry geometry)
            {
                _geometry = geometry;
            }
            #region IGraphicElement Member

            public void Draw(IDisplay display)
            {
                if (display != null && _geometry != null)
                {
                    display.Draw(ConstructPenTool._lineSymbol, _geometry);
                }
            }

            #endregion
        }

        private class PointGraphics : IGraphicElement
        {
            private IGeometry _geometry;

            public PointGraphics(IGeometry geometry)
            {
                _geometry = geometry;
            }
            #region IGraphicElement Member

            public void Draw(IDisplay display)
            {
                if (display != null && _geometry != null)
                {
                    display.Draw(ConstructPenTool._pointSymbol, _geometry);
                }
            }

            #endregion
        }
        #endregion

        virtual public Path ConstructionPath
        {
            get { return null; }
        }
        static protected Path ReducePath(Path path, int reduce)
        {
            if (path == null)
            {
                return null;
            }

            Path p = new Path();
            for (int i = 0; i < path.PointCount - reduce; i++)
            {
                p.AddPoint(path[i]);
            }

            return p;
        }
    }
    class ConstructMiddlePoint : ConstructPenTool
    {
        private IPoint _point1 = null, _world = null;
        private AggregateGeometry _geometry = null;
        private Polyline _polyLine = null;
        private double _distance = double.NaN, _direction = double.NaN;
        private bool _fixed = false;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("MiddlepointTool");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.const_midpoint;
            }
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _geometry = null;
            _point1 = null;
            _polyLine = null;
            _distance = _direction = double.NaN;
            _fixed = false;

            SetStatusText(Globalisation.GetResString("S6", String.Empty));
            return true;
        }

        public override bool MouseClick()
        {
            if (_point1 == null)
            {
                _point1 = new Point(_world);

                _geometry = new AggregateGeometry();
                _polyLine = new Polyline();
                _polyLine.AddPath(new Path());
                _polyLine[0].AddPoint(new Point(_world));
                _polyLine[0].AddPoint(new Point(_world));
                _geometry.AddGeometry(_polyLine);
                _geometry.AddGeometry(_point1);

                SetStatusText(Globalisation.GetResString("S7", String.Empty));
                return true;
            }
            else if (_fixed)
            {
                _point1.X = _polyLine[0][0].X / 2.0 + _world.X / 2.0;
                _point1.Y = _polyLine[0][0].Y / 2.0 + _world.Y / 2.0;

                _polyLine[0][1].X = _world.X;
                _polyLine[0][1].Y = _world.Y;
            }
            SetStatusText("");
            return false;
        }

        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            if (world == null)
            {
                return null;
            }

            if (_point1 == null || _polyLine == null || _geometry == null)
            {
                _world = world;
                return null;
            }
            else if (!_fixed)
            {
                _world = StandardPenTool.CalcPoint(_module, world, _direction, _distance,
                    _polyLine[0][0]);
            }
            _point1.X = _polyLine[0][0].X / 2.0 + _world.X / 2.0;
            _point1.Y = _polyLine[0][0].Y / 2.0 + _world.Y / 2.0;

            _polyLine[0][1].X = _world.X;
            _polyLine[0][1].Y = _world.Y;

            base.DrawConstructionSketch(_geometry);

            return _point1;
        }

        public override bool DrawMover
        {
            get
            {
                return false;
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Direction:
                    return true;
                case CalcToolResultType.Distance:
                    return true;
                case CalcToolResultType.SnapTo:
                    return true;
            }
            return false;
        }
        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (_module == null ||
                _module.Sketch == null)
            {
                return;
            }

            _distance = _direction = double.NaN;
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                case CalcToolResultType.SnapTo:
                    if (result is IPoint)
                    {
                        _fixed = (_point1 != null);

                        _world = (IPoint)result;
                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Direction:
                    if (result.GetType() == typeof(double))
                    {
                        _direction = (double)result;
                    }
                    break;
                case CalcToolResultType.Distance:
                    if (result.GetType() == typeof(double))
                    {
                        _distance = (double)result;
                    }
                    break;
            }
        }

        public override Path ConstructionPath
        {
            get
            {
                if (_polyLine != null && _polyLine.PathCount == 1)
                {
                    return ConstructPenTool.ReducePath(_polyLine[0] as Path, 1);
                }

                return null;
            }
        }
    }
    class ConstructDirectionDirection : ConstructPenTool
    {
        private IPoint _p11 = null, _p12 = null, _p21 = null, _p22 = null,
                       _p = null, _world = null;
        private Polyline _polyLine1 = null, _polyLine2 = null;
        private int _clickPos = 0;
        private AggregateGeometry _geometry = null;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("IntersectionTool");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.const_intersect;
            }
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _clickPos = 0;

            _geometry = null;
            _polyLine1 = null;
            _polyLine2 = null;
            _p11 = _p12 = _p21 = _p22 = _p = null;
            SetStatusText(Globalisation.GetResString("S9", String.Empty));

            return true;
        }
        public override bool MouseClick()
        {
            if (_module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return true;
            }

            _clickPos++;
            if (_geometry == null)
            {
                _geometry = new AggregateGeometry();
            }
            if (_clickPos == 1)
            {
                _polyLine1 = new Polyline();
                _polyLine1.AddPath(new Path());
                _p11 = new Point(_world);
                _geometry.AddGeometry(_polyLine1);
                SetStatusText(Globalisation.GetResString("S10", String.Empty));
                return true;
            }
            else if (_clickPos == 2)
            {
                _p12 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine1[0], _p11, _p12, _module.MapDocument.FocusMap.Display.Envelope);
                SetStatusText(Globalisation.GetResString("S11", String.Empty));
                return true;
            }
            else if (_clickPos == 3)
            {
                _polyLine2 = new Polyline();
                _polyLine2.AddPath(new Path());
                _p21 = new Point(_world);
                _geometry.AddGeometry(_polyLine2);
                SetStatusText(Globalisation.GetResString("S12", String.Empty));
                return true;
            }
            else if (_clickPos == 4)
            {
                _p22 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine2[0], _p21, _p22, _module.MapDocument.FocusMap.Display.Envelope);

                // Lösungen gerechnen
                Solve();
            }
            SetStatusText("");
            return false;
        }
        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            _world = world;
            if (_world == null ||
                _module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return null;
            }

            if (_clickPos == 1 && _p11 != null && _polyLine1 != null && _polyLine1.PathCount == 1)
            {
                _p12 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine1[0], _p11, _p12, _module.MapDocument.FocusMap.Display.Envelope);
            }
            if (_clickPos == 3 && _p21 != null && _polyLine2 != null && _polyLine2.PathCount == 1)
            {
                _p22 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine2[0], _p21, _p22, _module.MapDocument.FocusMap.Display.Envelope);
                Solve();
            }
            base.DrawConstructionSketch(_geometry);
            return _p;
        }

        public override bool DrawMover
        {
            get
            {
                return false;
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Direction:
                    return _clickPos == 1 || _clickPos == 3;
            }
            return false;
        }

        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (result == null)
            {
                return;
            }

            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    if (result is IPoint)
                    {
                        _world = (IPoint)result;
                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Direction:
                    if (result.GetType() == typeof(double))
                    {
                        double direction = (double)result;
                        if (_clickPos == 1 && _p11 != null)
                        {
                            _world = new Point(_p11.X + Math.Cos(direction), _p11.Y + Math.Sin(direction));
                        }
                        else if (_clickPos == 3 && _p21 != null)
                        {
                            _world = new Point(_p21.X + Math.Cos(direction), _p21.Y + Math.Sin(direction));
                        }
                        else
                        {
                            break;
                        }

                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
            }
        }

        public override Path ConstructionPath
        {
            get
            {
                if (_clickPos > 2 && _polyLine2 != null && _polyLine2.PathCount == 1)
                {
                    return ConstructPenTool.ReducePath((Path)_polyLine2[0], 1);
                }

                if (_clickPos < 2 && _polyLine1 != null && _polyLine1.PathCount == 1)
                {
                    return ConstructPenTool.ReducePath((Path)_polyLine1[0], 1);
                }

                return null;
            }
        }

        private void Solve()
        {
            if (_p == null)
            {
                _p = new Point();
                _geometry.AddGeometry(_p);
            }
            IPoint p = gView.Framework.SpatialAlgorithms.Algorithm.SegmentIntersection(
                _p11, _p12, _p21, _p22, false);
            if (p == null)
            {
                _geometry.RemoveGeometry(3);
                _p = null;
            }
            else
            {
                _p.X = p.X;
                _p.Y = p.Y;
            }
        }
    }
    class ConstructDistanceDistance : ConstructPenTool
    {
        private IPoint _p1 = null, _p2 = null, _p = null, _world = null, _middle1 = null, _middle2 = null;
        private double _radius1 = -1, _radius2 = -1;
        private AggregateGeometry _geometry = null;
        private Polyline _polyLine1 = null, _polyLine2 = null;
        private int _clickPos = 0;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("DistanceDistanceTool");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.const_dist_dist;
            }
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _clickPos = 0;

            _geometry = null;
            _polyLine1 = null;
            _polyLine2 = null;
            _p1 = _p2 = _p = null;
            _radius1 = _radius2 = -1;
            _middle1 = _middle2 = null;

            SetStatusText(Globalisation.GetResString("S1", String.Empty));
            return true;
        }
        public override bool MouseClick()
        {
            _clickPos++;
            if (_geometry == null)
            {
                _geometry = new AggregateGeometry();
            }
            if (_clickPos == 1)
            {
                _polyLine1 = new Polyline();
                _polyLine1.AddPath(new Path());
                _middle1 = new Point(_world);
                _geometry.AddGeometry(_polyLine1);
                SetStatusText(Globalisation.GetResString("S2", String.Empty));
            }
            else if (_clickPos == 2)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
                SetStatusText(Globalisation.GetResString("S3", String.Empty));
            }
            else if (_clickPos == 3)
            {
                _polyLine2 = new Polyline();
                _polyLine2.AddPath(new Path());
                _middle2 = new Point(_world);
                _geometry.AddGeometry(_polyLine2);
                SetStatusText(Globalisation.GetResString("S4", String.Empty));
            }
            else if (_clickPos == 4)
            {
                _radius2 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle2);
                ConstructPenTool.BuildCircle((Path)_polyLine2[0], _middle2, _radius2, 1.0);

                // Lösungen gerechnen
                double dx = _middle2.X - _middle1.X;
                double dy = _middle2.Y - _middle1.Y;

                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > _radius1 + _radius2)
                {
                    SetStatusText("");
                    return false;
                }
                double a = (_radius1 * _radius1 - _radius2 * _radius2 + d * d) / (2.0 * d);
                double h2 = _radius1 * _radius1 - a * a;
                if (h2 < 0.0)
                {
                    SetStatusText("");
                    return false;
                }
                double h = Math.Sqrt(h2);

                _p1 = new Point(_middle1.X + (a / d) * dx - (h / d) * dy,
                                _middle1.Y + (a / d) * dy + (h / d) * dx);
                _p2 = new Point(_middle1.X + (a / d) * dx + (h / d) * dy,
                                _middle1.Y + (a / d) * dy - (h / d) * dx);
                SetStatusText(Globalisation.GetResString("S5", String.Empty));
            }
            else
            {
                SetStatusText("");
                return false;
            }
            return true;
        }
        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            _world = world;
            if (_world == null)
            {
                return null;
            }

            if (_clickPos == 1 && _middle1 != null && _polyLine1 != null && _polyLine1.PathCount == 1)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
            }
            else if (_clickPos == 3 && _middle2 != null && _polyLine2 != null && _polyLine2.PathCount == 1)
            {
                _radius2 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle2);
                ConstructPenTool.BuildCircle((Path)_polyLine2[0], _middle2, _radius2, 1.0);
            }
            else if (_clickPos == 4 && _p1 != null && _p2 != null)
            {
                double dist1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p1);
                double dist2 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p2);

                if (_p == null)
                {
                    _p = new Point();
                    _geometry.AddGeometry(_p);
                }
                if (dist1 < dist2)
                {
                    _p.X = _p1.X; _p.Y = _p1.Y;
                }
                else
                {
                    _p.X = _p2.X; _p.Y = _p2.Y;
                }
            }
            base.DrawConstructionSketch(_geometry);
            return _p;
        }

        public override bool DrawMover
        {
            get
            {
                return false;
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Distance:
                    return _clickPos == 1 || _clickPos == 3;
            }
            return false;
        }

        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (result == null)
            {
                return;
            }

            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    if (result is IPoint)
                    {
                        _world = (IPoint)result;
                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Distance:
                    if (result.GetType() == typeof(double))
                    {
                        double distance = (double)result;
                        if (_clickPos == 1 && _middle1 != null)
                        {
                            _world = new Point(_middle1.X + distance, _middle1.Y);
                        }
                        else if (_clickPos == 3 && _middle2 != null)
                        {
                            _world = new Point(_middle2.X + distance, _middle2.Y);
                        }
                        else
                        {
                            break;
                        }

                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
            }
        }

        public override Path ConstructionPath
        {
            get
            {
                if (_clickPos == 1 && _middle1 != null)
                {
                    Path p = new Path();
                    p.AddPoint(_middle1);
                    return p;
                }
                else if (_clickPos == 3 && _middle2 != null)
                {
                    Path p = new Path();
                    p.AddPoint(_middle2);
                    return p;
                }
                return null;
            }
        }
    }
    class ConstructDistanceDirection : ConstructPenTool
    {
        private IPoint _p1 = null, _p2 = null, _p = null, _world = null,
                _middle1 = null, _p11 = null, _p12 = null;
        private double _radius1 = -1;
        private AggregateGeometry _geometry = null;
        private Polyline _polyLine1 = null, _polyLine2 = null;
        private int _clickPos = 0;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("DistanceDirectionTool");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.const_dist_dir;
            }
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _clickPos = 0;

            _p1 = _p2 = _p = null;
            _middle1 = _p11 = _p12 = null;
            _radius1 = -1;
            _geometry = null;
            _polyLine1 = _polyLine2 = null;
            SetStatusText(Globalisation.GetResString("S13", String.Empty));

            return true;
        }
        public override bool MouseClick()
        {
            if (_module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return true;
            }

            _clickPos++;
            if (_geometry == null)
            {
                _geometry = new AggregateGeometry();
            }
            if (_clickPos == 1)
            {
                _polyLine1 = new Polyline();
                _polyLine1.AddPath(new Path());
                _middle1 = new Point(_world);
                _geometry.AddGeometry(_polyLine1);
                SetStatusText(Globalisation.GetResString("S14", String.Empty));
            }
            else if (_clickPos == 2)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
                SetStatusText(Globalisation.GetResString("S15", String.Empty));
            }
            else if (_clickPos == 3)
            {
                _polyLine2 = new Polyline();
                _polyLine2.AddPath(new Path());
                _p11 = new Point(_world);
                _geometry.AddGeometry(_polyLine2);
                SetStatusText(Globalisation.GetResString("S16", String.Empty));
            }
            else if (_clickPos == 4)
            {
                _p12 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine2[0], _p11, _p12, _module.MapDocument.FocusMap.Display.Envelope);

                if (Solve())
                {
                    SetStatusText(Globalisation.GetResString("S5", String.Empty));
                    return true;
                }
                else
                {
                    SetStatusText("");
                    return false;
                }
            }
            else
            {
                SetStatusText("");
                return false;
            }
            return true;
        }
        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            _world = world;
            if (_world == null ||
                _module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return null;
            }

            if (_world == null)
            {
                return null;
            }

            if (_clickPos == 1 && _middle1 != null && _polyLine1 != null && _polyLine1.PathCount == 1)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
            }
            if (_clickPos == 3 && _p11 != null && _polyLine2 != null && _polyLine2.PathCount == 1)
            {
                _p12 = new Point(_world);
                ConstructPenTool.BuildRay((Path)_polyLine2[0], _p11, _p12, _module.MapDocument.FocusMap.Display.Envelope);
            }
            else if (_clickPos == 4)
            {
                if (_p1 == null || _p2 == null)
                {
                    return null;
                }

                double dist1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p1);
                double dist2 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p2);

                if (_p == null)
                {
                    _p = new Point();
                    _geometry.AddGeometry(_p);
                }
                if (dist1 < dist2)
                {
                    _p.X = _p1.X; _p.Y = _p1.Y;
                }
                else
                {
                    _p.X = _p2.X; _p.Y = _p2.Y;
                }
            }
            base.DrawConstructionSketch(_geometry);
            return _p;
        }
        public override bool DrawMover
        {
            get
            {
                return false;
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Distance:
                    return _clickPos == 1;
                case CalcToolResultType.Direction:
                    return _clickPos == 3;
            }
            return false;
        }
        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (result == null)
            {
                return;
            }

            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    if (result is IPoint)
                    {
                        _world = (IPoint)result;
                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Distance:
                    if (result.GetType() == typeof(double))
                    {
                        double distance = (double)result;
                        if (_clickPos == 1 && _middle1 != null)
                        {
                            _world = new Point(_middle1.X + distance, _middle1.Y);
                        }
                        else
                        {
                            break;
                        }

                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Direction:
                    if (result.GetType() == typeof(double))
                    {
                        double direction = (double)result;
                        if (_clickPos == 3 && _p11 != null)
                        {
                            _world = new Point(_p11.X + Math.Cos(direction), _p11.Y + Math.Sin(direction));
                        }
                        else
                        {
                            break;
                        }

                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
            }
        }
        public override Path ConstructionPath
        {
            get
            {
                if (_clickPos == 1 && _middle1 != null)
                {
                    Path p = new Path();
                    p.AddPoint(_middle1);
                    return p;
                }
                else if (_clickPos == 3 && _polyLine2 != null)
                {
                    return ConstructPenTool.ReducePath(_polyLine2[0] as Path, 1);
                }
                return null;
            }
        }

        private bool Solve()
        {
            if (_middle1 == null || _p11 == null || _p12 == null || _radius1 < 0)
            {
                return false;
            }

            _p1 = _p2 = null;

            double dx = _p12.X - _p11.X, dy = _p12.Y - _p11.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            IPoint ps = gView.Framework.SpatialAlgorithms.Algorithm.SegmentIntersection(
                _p11, _p12, _middle1, new Point(_middle1.X - dy, _middle1.Y + dx), false);
            if (ps == null)
            {
                return false;
            }

            double dist = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(ps, _middle1);
            if (dist > _radius1)
            {
                return false;
            }

            dx /= len; dy /= len;
            double s_2 = Math.Sqrt(_radius1 * _radius1 - dist * dist);

            _p1 = new Point(ps.X + dx * s_2, ps.Y + dy * s_2);
            _p2 = new Point(ps.X - dx * s_2, ps.Y - dy * s_2);
            return true;

            //_p1 = _p2 = null;
            //// alles auf Mittelpunkt reduzieren
            //IPoint p11 = gView.Framework.SpatialAlgorithms.Algorithm.PointDifference(_p11, _middle1);
            //IPoint p12 = gView.Framework.SpatialAlgorithms.Algorithm.PointDifference(_p12, _middle1);

            //// Geradengleichung: ax+by=c ... a,b normalvektor auf gerade, c durch einsetzen
            //double a = p11.Y - p12.Y;
            //double b = p12.X - p11.X;
            //double c = p11.X * a + p11.Y * b;

            //// Quadratische Gleichung: x² + px + q = 0
            //double p = -2 * a / (a * a + b * b);
            //double q = (c * c - b * b * _radius1 * _radius1) / (a * a + b * b);

            //double D = (p / 2) * (p / 2) - q;
            //if (D < 0.0) return;

            //double x1 = -p / 2 + Math.Sqrt(D);
            //double x2 = -p / 2 - Math.Sqrt(D);

            //// y=sqrt(r*r-x*x)
            //double y1 = Math.Sqrt(_radius1 * _radius1 - x1 * x1);
            //double y2 = Math.Sqrt(_radius1 * _radius1 - x2 * x2);

            //// zurückrechnen
            //_p1 = new Point(_middle1.X + x1, _middle1.Y + y1);
            //_p2 = new Point(_middle1.X + x2, _middle1.Y + y2);
        }
    }
    class ConstructDistanceTangent : ConstructPenTool
    {
        private IPoint _p1 = null, _p2 = null, _p = null, _world = null,
                _middle1 = null, _p11 = null;
        private double _radius1 = -1;
        private AggregateGeometry _geometry = null;
        private Polyline _polyLine1 = null, _polyLine2 = null;
        private int _clickPos = 0;

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("DistanceTangentTool");
            }
        }
        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.const_dist_tan;
            }
        }

        public override bool Activated(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            _clickPos = 0;

            _p1 = _p2 = _p = null;
            _middle1 = _p11 = null;
            _radius1 = -1;
            _geometry = null;
            _polyLine1 = _polyLine2 = null;
            SetStatusText(Globalisation.GetResString("S13", String.Empty));

            return true;
        }
        public override bool MouseClick()
        {
            if (_module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return true;
            }

            _clickPos++;
            if (_geometry == null)
            {
                _geometry = new AggregateGeometry();
            }
            if (_clickPos == 1)
            {
                _polyLine1 = new Polyline();
                _polyLine1.AddPath(new Path());
                _middle1 = new Point(_world);
                _geometry.AddGeometry(_polyLine1);
                SetStatusText(Globalisation.GetResString("S14", String.Empty));
            }
            else if (_clickPos == 2)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
                SetStatusText(Globalisation.GetResString("S15", String.Empty));
            }
            else if (_clickPos == 3)
            {
                _polyLine2 = new Polyline();
                _polyLine2.AddPath(new Path());
                _p11 = new Point(_world);
                _geometry.AddGeometry(_polyLine2);

                if (Solve())
                {
                    SetStatusText(Globalisation.GetResString("S5", String.Empty));
                    return true;
                }
                else
                {
                    SetStatusText("");
                    return false;
                }
            }
            else
            {
                SetStatusText("");
                return false;
            }
            return true;
        }
        public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        {
            _world = world;
            if (_world == null ||
                _module == null ||
                _module.MapDocument == null ||
                _module.MapDocument.FocusMap == null ||
                _module.MapDocument.FocusMap.Display == null ||
                _module.MapDocument.FocusMap.Display.Envelope == null)
            {
                return null;
            }

            if (_world == null)
            {
                return null;
            }

            if (_clickPos == 1 && _middle1 != null && _polyLine1 != null && _polyLine1.PathCount == 1)
            {
                _radius1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _middle1);
                ConstructPenTool.BuildCircle((Path)_polyLine1[0], _middle1, _radius1, 1.0);
            }
            else if (_clickPos == 3)
            {
                if (_p1 == null)
                {
                    return null;
                }

                double dist1 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p1);
                double dist2 = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_world, _p2);

                if (_p == null)
                {
                    _p = new Point();
                    _geometry.AddGeometry(_p);
                }
                if (dist1 < dist2)
                {
                    _p.X = _p1.X; _p.Y = _p1.Y;
                    ConstructPenTool.BuildRay((Path)_polyLine2[0], _p11, _p1, _module.MapDocument.FocusMap.Display.Envelope);
                }
                else
                {
                    _p.X = _p2.X; _p.Y = _p2.Y;
                    ConstructPenTool.BuildRay((Path)_polyLine2[0], _p11, _p2, _module.MapDocument.FocusMap.Display.Envelope);
                }
            }
            base.DrawConstructionSketch(_geometry);
            return _p;
        }
        public override bool DrawMover
        {
            get
            {
                return false;
            }
        }

        public override bool UseCalcToolResultType(CalcToolResultType type)
        {
            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    return true;
                case CalcToolResultType.Distance:
                    return _clickPos == 1;
            }
            return false;
        }
        public override void EvaluateCalcToolResult(CalcToolResultType type, object result)
        {
            if (result == null)
            {
                return;
            }

            switch (type)
            {
                case CalcToolResultType.AbsolutPos:
                    if (result is IPoint)
                    {
                        _world = (IPoint)result;
                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
                case CalcToolResultType.Distance:
                    if (result.GetType() == typeof(double))
                    {
                        double distance = (double)result;
                        if (_clickPos == 1 && _middle1 != null)
                        {
                            _world = new Point(_middle1.X + distance, _middle1.Y);
                        }
                        else
                        {
                            break;
                        }

                        MouseClick();
                        base.DrawConstructionSketch(_geometry);
                    }
                    break;
            }
        }
        public override Path ConstructionPath
        {
            get
            {
                if (_clickPos == 1 && _middle1 != null)
                {
                    Path p = new Path();
                    p.AddPoint(_middle1);
                    return p;
                }
                return null;
            }
        }

        private bool Solve()
        {
            if (_middle1 == null || _p11 == null || _radius1 < 0)
            {
                return false;
            }

            _p1 = _p2 = null;

            double d = gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(_p11, _middle1);
            if (d < _radius1)
            {
                return false;
            }

            double s = Math.Sqrt(d * d - _radius1 * _radius1);
            double h = _radius1 * s / d;
            double d_ = Math.Sqrt(_radius1 * _radius1 - h * h);

            double dx = (_p11.X - _middle1.X) / d;
            double dy = (_p11.Y - _middle1.Y) / d;

            IPoint Z = new Point(_middle1.X + dx * d_, _middle1.Y + dy * d_);

            _p1 = new Point(Z.X - dy * h, Z.Y + dx * h);
            _p2 = new Point(Z.X + dy * h, Z.Y - dx * h);
            return true;
        }
    }

    class StandardCalcTool : ICalcTool
    {
        protected Module _module = null;
        protected ParentPenToolMenuItem _parent = null;

        public StandardCalcTool(ParentPenToolMenuItem parent)
        {
            _parent = parent;
        }

        #region ICalcTool Member

        virtual public string Name
        {
            get { return String.Empty; }
        }

        virtual public object Image
        {
            get { return null; }
        }

        virtual public void OnCreate(IModule module)
        {
            _module = module as Module;
        }

        virtual public object Calc(IPoint world, IPoint worldMouse, IPoint vertex)
        {
            return null;
        }

        virtual public CalcToolResultType ResultType
        {
            get { return CalcToolResultType.None; }
        }
        virtual public bool Enabled
        {
            get
            {
                return false;
            }
        }
        #endregion

        protected Path CurrentPath
        {
            get
            {
                if (_parent.ActivePenTool is ConstructPenTool)
                {
                    return ((ConstructPenTool)_parent.ActivePenTool).ConstructionPath;
                }

                if (_module != null &&
                    _module.Sketch != null &&
                    _module.Sketch.Part is Path)
                {
                    return _module.Sketch.Part as Path;
                }

                return null;
            }
        }
    }

    class PerpenticualPenTool : StandardCalcTool
    {
        public PerpenticualPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("PerpenticulaTo");
            }
        }

        public override object Image
        {
            get
            {
                return null;
            }
        }


        //public override bool MouseClick()
        //{
        //    return false;
        //}
        //public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        //{
        //    if (double.IsNaN(_alpha))
        //    {
        //        _world = world;
        //    }
        //    else if (_module != null &&
        //        _module.Sketch != null &&
        //        _module.Sketch.Part != null &&
        //        _module.Sketch.Part.PointCount > 0)
        //    {
        //        return CalcPoint(world);
        //    }

        //    return null;
        //}
        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = segment[0];
                IPoint p2 = segment[1];

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                return (double)Math.Atan2(dy, dx) + Math.PI / 2.0;
            }
            else
            {
                return null;
            }
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.Direction;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
        //virtual public IPoint CalcPoint(IPoint world)
        //{
        //    IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];

        //    Point r = new Point(Math.Sin(_alpha), -Math.Cos(_alpha));
        //    Point r_ = new Point(-r.Y, r.X);

        //    LinearEquation2 linarg = new LinearEquation2(
        //        world.X - p1.X,
        //        world.Y - p1.Y,
        //        r.X, r_.X,
        //        r.Y, r_.Y);

        //    if (linarg.Solve())
        //    {
        //        double t1 = linarg.Var1;
        //        //double t2 = linarg.Var2;

        //        return new Point(p1.X + r.X * t1, p1.Y + r.Y * t1);
        //    }
        //    return null;
        //}
    }

    class ParallelPenTool : PerpenticualPenTool
    {
        public ParallelPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("ParallelTo");
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = segment[0];
                IPoint p2 = segment[1];

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                return (double)Math.Atan2(dy, dx);
            }
            else
            {
                return null;
            }
        }

        //public override IPoint CalcPoint(IPoint world)
        //{
        //    IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];

        //    Point r = new Point(Math.Cos(_alpha), Math.Sin(_alpha));
        //    Point r_ = new Point(-r.Y, r.X);

        //    LinearEquation2 linarg = new LinearEquation2(
        //        world.X - p1.X,
        //        world.Y - p1.Y,
        //        r.X, r_.X,
        //        r.Y, r_.Y);

        //    if (linarg.Solve())
        //    {
        //        double t1 = linarg.Var1;
        //        //double t2 = linarg.Var2;

        //        return new Point(p1.X + r.X * t1, p1.Y + r.Y * t1);
        //    }
        //    return null;
        //}
    }

    class SnapToPenTool : StandardCalcTool
    {
        public SnapToPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("SemgentUseDirection");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = segment[0];
                IPoint p2 = segment[1];
                IPoint r1 = new Point(p2.X - p1.X, p2.Y - p1.Y);

                IPoint p0 = base.CurrentPath[base.CurrentPath.PointCount - 1];
                IPoint r0 = new Point(vertex.X - p0.X, vertex.Y - p0.Y);

                LinearEquation2 linarg = new LinearEquation2(
                    p0.X - p1.X,
                    p0.Y - p1.Y,
                    r1.X, -r0.X,
                    r1.Y, -r0.Y);

                if (linarg.Solve())
                {
                    double t1 = linarg.Var1;
                    if (t1 >= 0.0 && t1 <= 1.0)
                    {
                        return new Point(p1.X + t1 * r1.X,
                                         p1.Y + t1 * r1.Y);
                    }
                }
            }
            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.SnapTo;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }

    class DirectionPenTool : StandardCalcTool
    {
        public DirectionPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }
        public override string Name
        {
            get
            {
                return Globalisation.GetResString("Direction");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }
        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (vertex == null)
            {
                vertex = world;
            }

            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 1];
            if (p1 == null)
            {
                return null;
            }

            double dx = vertex.X - p1.X;
            double dy = vertex.Y - p1.Y;

            FormDirection dlg = new FormDirection();
            dlg.Direction = Math.Atan2(dy, dx);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.Direction;
            }
            else
            {
                return null;
            }
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.Direction;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
        //public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        //{
        //    if (double.IsNaN(_alpha)) return null;

        //    if (_module != null &&
        //        _module.Sketch != null &&
        //        _module.Sketch.Part != null &&
        //        _module.Sketch.Part.PointCount > 0)
        //    {
        //        IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];

        //        Point r = new Point(Math.Cos(_alpha), Math.Sin(_alpha));
        //        Point r_ = new Point(-r.Y, r.X);

        //        LinearEquation2 linarg = new LinearEquation2(
        //            world.X - p1.X,
        //            world.Y - p1.Y,
        //            r.X, r_.X,
        //            r.Y, r_.Y);

        //        if (linarg.Solve())
        //        {
        //            double t1 = linarg.Var1;
        //            //double t2 = linarg.Var2;

        //            return new Point(p1.X + r.X * t1, p1.Y + r.Y * t1);
        //        }
        //    }

        //    return null;
        //}
    }

    class DeflectionPenTool : DirectionPenTool
    {
        public DeflectionPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("Deflection");
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount < 2)
            {
                return null;
            }

            IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 2];
            IPoint p2 = base.CurrentPath[base.CurrentPath.PointCount - 1];

            if (p1 == null)
            {
                return null;
            }

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            FormDirection dlg = new FormDirection();
            dlg.Direction = 0.0;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return Math.Atan2(dy, dx) + dlg.Direction;
            }
            else
            {
                return null;
            }
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.Direction;
            }
        }

        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 1)
                {
                    return true;
                }

                return false;
            }
        }
    }

    class DistancePenTool : StandardCalcTool
    {
        public DistancePenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("Distance");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (vertex == null)
            {
                vertex = world;
            }

            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 1];
            if (p1 == null)
            {
                return null;
            }

            double dx = vertex.X - p1.X;
            double dy = vertex.Y - p1.Y;

            FormDistance dlg = new FormDistance();
            dlg.Distance = Math.Sqrt(dx * dx + dy * dy);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.Distance;
            }
            else
            {
                return null;
            }
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.Distance;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
        //public override IPoint CalcPoint(int mouseX, int mouseY, IPoint world)
        //{
        //    if (double.IsNaN(_length)) return null;

        //    if (_module != null &&
        //        _module.Sketch != null &&
        //        _module.Sketch.Part != null &&
        //        _module.Sketch.Part.PointCount > 0)
        //    {
        //        IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];

        //        double dx = world.X - p1.X;
        //        double dy = world.Y - p1.Y;
        //        double len = Math.Sqrt(dx * dx + dy * dy);

        //        dx /= len; dy /= len;

        //        return new Point(p1.X + dx * _length, p1.Y + dy * _length);
        //    }

        //    return null;
        //}
    }

    class DirectionDistancePenTool : StandardCalcTool
    {
        public DirectionDistancePenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("DirectionDistance");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 1];
            if (p1 == null)
            {
                return null;
            }

            double dx = vertex.X - p1.X;
            double dy = vertex.Y - p1.Y;

            FormDirectionDistance dlg = new FormDirectionDistance();
            dlg.Direction = Math.Atan2(dy, dx);
            dlg.Distance = Math.Sqrt(dx * dx + dy * dy);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                double alpha = dlg.Direction;
                double length = dlg.Distance;

                dx = Math.Cos(alpha) * length;
                dy = Math.Sin(alpha) * length;

                return new Point(p1.X + dx, p1.Y + dy);
            }
            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.AbsolutPos;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }

    class AbsolutXYPenTool : StandardCalcTool
    {
        public AbsolutXYPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("AbsolutXY");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            IPoint p = null;

            if (_module != null &&
                _module.Sketch != null &&
                _module.Sketch.Part != null &&
                vertex != null)
            {
                p = vertex;
            }
            else if (_module != null &&
                _module.Sketch == null)
            {
                _module.CreateStandardFeature();
                p = world;
            }
            else
            {
                p = world;
            }
            if (p == null)
            {
                return null;
            }

            FormXY dlg = new FormXY();
            dlg.Text = "Absolut X,Y";
            dlg.X = p.X;
            dlg.Y = p.Y;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return new Point(dlg.X, dlg.Y);
            }

            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.AbsolutPos;
            }
        }
        public override bool Enabled
        {
            get
            {
                return true;
            }
        }
    }

    class DeltaXYPenTool : StandardCalcTool
    {
        public DeltaXYPenTool(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("DeltaXY");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPoint p1 = _module.Sketch.Part[_module.Sketch.Part.PointCount - 1];
            if (p1 == null)
            {
                return null;
            }

            double dx = vertex.X - p1.X;
            double dy = vertex.Y - p1.Y;

            FormXY dlg = new FormXY();
            dlg.Text = "Delta X,Y";
            dlg.X = dx;
            dlg.Y = dy;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                dx = dlg.X;
                dy = dlg.Y;

                return new Point(p1.X + dx, p1.Y + dy);
            }

            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.AbsolutPos;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }

    class SegmentMidpoint : StandardCalcTool
    {
        public SegmentMidpoint(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("SegmentMidpoint");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (vertex == null)
            {
                vertex = world;
            }

            if (vertex == null)
            {
                return null;
            }

            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = segment[0];
                IPoint p2 = segment[1];

                if (_module != null && _module.Sketch == null)
                {
                    _module.CreateStandardFeature();
                }

                return new Point(p1.X / 2.0 + p2.X / 2.0,
                                 p1.Y / 2.0 + p2.Y / 2.0);
            }
            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.AbsolutPos;
            }
        }
        public override bool Enabled
        {
            get
            {
                return true;
            }
        }
    }

    class SegmentDistance : StandardCalcTool
    {
        public SegmentDistance(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("SegmentDistance");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (vertex == null)
            {
                vertex = world;
            }

            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 1];
                double distance;
                Polyline pLine = new Polyline();
                pLine.AddPath(segment);

                if (gView.Framework.SpatialAlgorithms.Algorithm.NearestPointToPath(
                    pLine, p1, out distance, false, true) != null)
                {
                    return distance;
                }
            }
            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.Distance;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }

    class SegmentOrtho : StandardCalcTool
    {
        public SegmentOrtho(ParentPenToolMenuItem parent)
            : base(parent)
        {
        }

        public override string Name
        {
            get
            {
                return Globalisation.GetResString("SegmentOrthogonal");
            }
        }
        public override object Image
        {
            get
            {
                return null;
            }
        }

        public override object Calc(IPoint world, IPoint mouseWorld, IPoint vertex)
        {
            if (vertex == null)
            {
                vertex = world;
            }

            if (base.CurrentPath == null ||
                base.CurrentPath.PointCount == 0 ||
                vertex == null)
            {
                return null;
            }

            IPath segment = ParentPenToolMenuItem.QueryPathSegment(_module.MapDocument.FocusMap, mouseWorld);
            if (segment != null && segment.PointCount == 2)
            {
                IPoint p1 = base.CurrentPath[base.CurrentPath.PointCount - 1];
                double distance;
                Polyline pLine = new Polyline();
                pLine.AddPath(segment);

                return gView.Framework.SpatialAlgorithms.Algorithm.NearestPointToPath(
                    pLine, p1, out distance, false, false);
            }
            return null;
        }

        public override CalcToolResultType ResultType
        {
            get
            {
                return CalcToolResultType.AbsolutPos;
            }
        }
        public override bool Enabled
        {
            get
            {
                if (base.CurrentPath != null &&
                    base.CurrentPath.PointCount > 0)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
