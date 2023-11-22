using gView.Framework.Carto.Rendering.UI;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace gView.Framework.Carto.Rendering
{
    [gView.Framework.system.RegisterPlugIn("0be572ae-bb58-4148-a428-084bad28ffab")]
    public class ChartLabelRenderer : Cloner, ILabelRenderer, IPriority, IPropertyPage, ILegendGroup
    {
        public enum chartType { Pie = 0, Bars = 1, Stack = 2 }
        public enum sizeType { ConstantSize = 0, ValueOfEquatesToSize = 1 }

        private chartType _type = chartType.Pie;
        private sizeType _sizeType = sizeType.ConstantSize;
        private SimpleLabelRenderer.labelPriority _labelPriority = SimpleLabelRenderer.labelPriority.normal;
        private Dictionary<string, ISymbol> _symbolTable = new Dictionary<string, ISymbol>();
        private ILineSymbol _outlineSymbol = null;
        private double _size = 50D, _valueEquatesToSize = 100D;
        private IEnvelope _clipEnvelope = null;

        public ChartLabelRenderer()
        {
        }

        #region Members / Properties

        public void SetSymbol(string fieldName, ISymbol symbol)
        {
            if (symbol == null)
            {
                if (_symbolTable.ContainsKey(fieldName))
                {
                    _symbolTable.Remove(fieldName);
                }
            }
            else
            {
                if (!_symbolTable.ContainsKey(fieldName))
                {
                    _symbolTable.Add(fieldName, symbol);
                }
                else
                {
                    ((ISymbol)_symbolTable[fieldName]).Release();
                    _symbolTable[fieldName] = symbol;
                }
            }
        }

        public ISymbol GetSymbol(string fieldName)
        {
            if (_symbolTable.ContainsKey(fieldName))
            {
                return _symbolTable[fieldName];
            }
            return null;
        }

        public string[] FieldNames
        {
            get
            {
                return _symbolTable.Keys.ToArray();
            }
        }

        public ILineSymbol OutlineSymbol
        {
            get { return _outlineSymbol; }
            set { _outlineSymbol = value; }
        }

        public double Size
        {
            get { return _size; }
            set { _size = Math.Max(value, 1D); }
        }

        public double ValueEquatesToSize
        {
            get { return _valueEquatesToSize; }
            set { _valueEquatesToSize = Math.Abs(value); }
        }

        public chartType ChartType
        {
            get { return _type; }
            set { _type = value; }
        }

        public sizeType SizeType
        {
            get { return _sizeType; }
            set { _sizeType = value; }
        }

        public SimpleLabelRenderer.labelPriority LabelPriority
        {
            get
            {
                return _labelPriority;
            }
            set
            {
                _labelPriority = value;
            }
        }

        #endregion

        #region ILabelRenderer Member

        public void PrepareQueryFilter(IDisplay display, IFeatureLayer layer, IQueryFilter filter)
        {
            if (layer.FeatureClass == null)
            {
                return;
            }

            foreach (string fieldName in _symbolTable.Keys)
            {
                if (layer.FeatureClass.Fields.FindField(fieldName) == null)
                {
                    continue;
                }

                filter.AddField(fieldName);
            }
            _clipEnvelope = new Envelope(display.DisplayTransformation.TransformedBounds(display));
        }

        public bool CanRender(IFeatureLayer layer, IMap map)
        {
            if (layer.FeatureClass == null)
            {
                return false;
            }

            int count = 0;
            foreach (IField field in layer.FeatureClass.Fields.ToEnumerable())
            {
                if (field.type == FieldType.biginteger ||
                    field.type == FieldType.Double ||
                    field.type == FieldType.Float ||
                    field.type == FieldType.integer ||
                    field.type == FieldType.smallinteger)
                {
                    count++;
                }
            }
            return count > 1;
        }

        public string Name
        {
            get { return "Chart Renderer"; }
        }

        public LabelRenderMode RenderMode
        {
            get { return (_labelPriority == SimpleLabelRenderer.labelPriority.always) ? LabelRenderMode.RenderWithFeature : LabelRenderMode.UseRenderPriority; }
        }

        public int RenderPriority
        {
            get { return 3 - (int)_labelPriority; }
        }

        public void Draw(IDisplay disp, IFeature feature)
        {
            #region Check Values
            if (_symbolTable.Keys.Count == 0)
            {
                return;
            }

            double[] values = new double[_symbolTable.Keys.Count];
            double valSum = 0.0, valMax = double.MinValue, valMin = double.MaxValue;
            #region Get Values
            int i = 0;
            foreach (string fieldname in _symbolTable.Keys)
            {
                object val = feature[fieldname];
                if (val == null || val == System.DBNull.Value)
                {
                    return;
                }

                try
                {
                    values[i++] = Convert.ToDouble(val);
                    valSum += Math.Abs(values[i - 1]);
                    valMax = Math.Max(valMax, values[i - 1]);
                    valMin = Math.Min(valMin, values[i - 1]);
                }
                catch
                {
                    return;
                }
            }
            #endregion
            #endregion

            IPoint point = null;
            if (feature.Shape is IPoint)
            {
                point = (IPoint)feature.Shape;
                if (disp.LabelEngine.TryAppend(disp,
                    ChartAnnotationPolygon(disp, point, values, valSum, valMin, valMax),
                    feature.Shape, _labelPriority != SimpleLabelRenderer.labelPriority.always) == LabelAppendResult.Succeeded)
                {
                    DrawChart(disp, point, values, valSum, valMin, valMax);
                }
            }
            else if (feature.Shape is IPolyline)
            {
                IEnvelope dispEnv = _clipEnvelope;
                IPolyline pLine = (IPolyline)gView.Framework.SpatialAlgorithms.Clip.PerformClip(dispEnv, feature.Shape);

                if (pLine == null)
                {
                    return;
                }

                for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                {
                    IPath path = pLine[iPath];
                    for (int iPoint = 0; iPoint < path.PointCount - 1; iPoint++)
                    {
                        IPoint p1 = path[iPoint];
                        IPoint p2 = path[iPoint + 1];
                        if (dispEnv.minx <= p1.X && dispEnv.maxx >= p1.X &&
                            dispEnv.miny <= p1.Y && dispEnv.maxy >= p1.Y &&
                            dispEnv.minx <= p2.X && dispEnv.maxx >= p2.X &&
                            dispEnv.miny <= p2.Y && dispEnv.maxy >= p2.Y)
                        {
                            point = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
                            if (disp.LabelEngine.TryAppend(disp,
                                ChartAnnotationPolygon(disp, point, values, valSum, valMin, valMax),
                                pLine, _labelPriority != SimpleLabelRenderer.labelPriority.always) == LabelAppendResult.Succeeded)
                            {
                                DrawChart(disp, point, values, valSum, valMin, valMax);
                                return;
                            }
                        }
                    }
                }
            }
            else if (feature.Shape is IPolygon)
            {
                LabelPolygon(disp, (IPolygon)feature.Shape, values, valSum, valMin, valMax);
            }
        }

        #endregion

        private bool LabelPolygon(IDisplay disp, IPolygon polygon, double[] values, double valSum, double valMin, double valMax)
        {
            IEnvelope env = new Envelope(_clipEnvelope);

            if (polygon is ITopologicalOperation)
            {

                IGeometry g;
                ((ITopologicalOperation)polygon).Clip(env, out g);
                if (g == null)
                {
                    return false;
                }

                if (g is IPolygon)
                {
                    polygon = (IPolygon)g;
                }
            }
            if (polygon == null)
            {
                return false;
            }

            IMultiPoint pColl = gView.Framework.SpatialAlgorithms.Algorithm.PolygonLabelPoints(polygon);
            return LabelPointCollection(disp, polygon, pColl, values, valSum, valMin, valMax);
        }

        private bool LabelPointCollection(IDisplay disp, IPolygon polygon, IMultiPoint pColl, double[] values, double valSum, double valMin, double valMax)
        {
            if (pColl == null)
            {
                return false;
            }

            for (int i = 0; i < pColl.PointCount; i++)
            {
                if (disp.LabelEngine.TryAppend(disp,
                    ChartAnnotationPolygon(disp, pColl[i], values, valSum, valMin, valMax),
                    pColl[i], _labelPriority != SimpleLabelRenderer.labelPriority.always) == LabelAppendResult.Succeeded)
                {
                    DrawChart(disp, pColl[i], values, valSum, valMin, valMax);
                    return true;
                }
            }
            for (int i = 0; i < pColl.PointCount; i++)
            {
                ISmartLabelPoint slp = pColl[i] as ISmartLabelPoint;
                if (slp != null)
                {
                    LabelPointCollection(disp, polygon, slp.AlernativeLabelPoints(disp), values, valSum, valMin, valMax);
                }
            }
            return false;
        }

        private List<IAnnotationPolygonCollision> ChartAnnotationPolygon(IDisplay disp, IPoint point, double[] values, double valSum, double valMin, double valMax)
        {
            IEnvelope chartEnvelope = null;

            if (_type == chartType.Pie)
            {
                #region Measure Pie

                if (valSum == 0.0)
                {
                    return null;
                }

                double r = disp.MapScale / disp.Dpm * _size / 2D;
                switch (_sizeType)
                {
                    case sizeType.ValueOfEquatesToSize:
                        double a = r * r * Math.PI * valSum / _valueEquatesToSize;
                        r = Math.Sqrt(a / Math.PI);
                        break;
                }

                chartEnvelope = new Envelope(point.X - r, point.Y - r, point.X + r, point.Y + r);

                #endregion
            }
            else if (_type == chartType.Bars)
            {
                #region Draw Bars

                double height = disp.MapScale / disp.Dpm * _size;
                double width = disp.MapScale / disp.Dpm * _size / 4D;
                double heightVal = _valueEquatesToSize;

                switch (_sizeType)
                {
                    case sizeType.ConstantSize:
                        if (valSum == 0.0)
                        {
                            return null;
                        }

                        height *= _valueEquatesToSize / valSum;
                        break;
                }

                double left = -(_symbolTable.Keys.Count * width / 2.0) - (_symbolTable.Keys.Count - 1) * (disp.MapScale / disp.Dpm) / 2.0;

                chartEnvelope = new Envelope(point.X + left,
                    point.Y,
                    point.X + left + (width + (disp.MapScale / disp.Dpm)) * _symbolTable.Keys.Count,
                    point.Y + height * (valMax / heightVal));

                #endregion
            }
            else if (_type == chartType.Stack)
            {
                #region Draw Stack

                double height = disp.MapScale / disp.Dpm * _size;
                double width = disp.MapScale / disp.Dpm * _size / 4D;
                double heightVal = _valueEquatesToSize;

                Path outerPath = new Path();

                switch (_sizeType)
                {
                    case sizeType.ConstantSize:
                        if (valSum == 0.0)
                        {
                            return null;
                        }

                        height *= _valueEquatesToSize / valSum;
                        break;
                }

                double left = -width / 2.0;
                chartEnvelope = new Envelope(point.X + left,
                    point.Y,
                    point.X + left + width,
                    point.Y + height * (valSum / heightVal));

                #endregion
            }

            if (chartEnvelope != null)
            {
                double x1 = chartEnvelope.minx, y1 = chartEnvelope.maxy,
                       x2 = chartEnvelope.maxx, y2 = chartEnvelope.miny;
                disp.World2Image(ref x1, ref y1);
                disp.World2Image(ref x2, ref y2);

                return new List<IAnnotationPolygonCollision>()
                {
                    new AnnotationPolygon((float)x1,(float)y1,(float)Math.Abs(x2-x1),(float)Math.Abs(y2-y1))
                };
            }
            return null;
        }

        private void DrawChart(IDisplay disp, IPoint point, double[] values, double valSum, double valMin, double valMax)
        {
            if (!(disp is Display) || disp.LabelEngine.LabelCanvas == null)
            {
                return;
            }

            var originalCanvas = disp.Canvas;
            try
            {
                ((Display)disp).Canvas = disp.LabelEngine.LabelCanvas;

                if (_symbolTable.Keys.Count == 0)
                {
                    return;
                }

                int i = 0;
                if (_type == chartType.Pie)
                {
                    #region Draw Pie

                    if (valSum == 0.0)
                    {
                        return;
                    }

                    double r = disp.MapScale / disp.Dpm * _size / 2D;
                    switch (_sizeType)
                    {
                        case sizeType.ValueOfEquatesToSize:
                            double a = r * r * Math.PI * valSum / _valueEquatesToSize;
                            r = Math.Sqrt(a / Math.PI);
                            break;
                    }

                    i = 0;
                    double startAngle = 0.0;

                    Path outerPath = new Path();
                    foreach (string fieldname in _symbolTable.Keys)
                    {
                        ISymbol symbol = _symbolTable[fieldname];

                        double angle = (Math.PI * 2.0) * Math.Abs(values[i++]) / valSum;

                        Polygon poly = new Polygon();
                        Ring ring = new Ring();
                        poly.AddRing(ring);
                        ring.AddPoint(point);

                        for (double a = startAngle, to = startAngle + angle; a < to; a += 0.01745)
                        {
                            IPoint piePoint = new Point(point.X + r * Math.Cos(a), point.Y + r * Math.Sin(a));
                            ring.AddPoint(piePoint);
                            outerPath.AddPoint(piePoint);
                        }
                        ring.AddPoint(new Point(point.X + r * Math.Cos(startAngle + angle), point.Y + r * Math.Sin(startAngle + angle)));

                        symbol.Draw(disp, poly);

                        //if (_outlineSymbol != null && ring.PointCount > 0)
                        //    outerPath.AddPoints(ring);

                        startAngle += angle;
                    }
                    if (outerPath.PointCount > 0 && _outlineSymbol != null)
                    {
                        outerPath.AddPoint(outerPath[0]);
                        _outlineSymbol.Draw(disp, new Polyline(outerPath));
                    }

                    #endregion
                }
                else if (_type == chartType.Bars)
                {
                    #region Draw Bars

                    double height = disp.MapScale / disp.Dpm * _size;
                    double width = disp.MapScale / disp.Dpm * _size / 4D;
                    double heightVal = _valueEquatesToSize;

                    switch (_sizeType)
                    {
                        case sizeType.ConstantSize:
                            if (valSum == 0.0)
                            {
                                return;
                            }

                            height *= _valueEquatesToSize / valSum;
                            break;
                    }

                    i = 0;
                    double left = -(_symbolTable.Keys.Count * width / 2.0) - (_symbolTable.Keys.Count - 1) * (disp.MapScale / disp.Dpm) / 2.0;
                    foreach (string fieldname in _symbolTable.Keys)
                    {
                        ISymbol symbol = _symbolTable[fieldname];

                        double h = height * (values[i++] / heightVal);
                        if (Math.Abs(h) < disp.MapScale / disp.Dpm)
                        {
                            h = disp.MapScale / disp.Dpm;
                        }

                        Polygon poly = new Polygon();
                        Ring ring = new Ring();
                        poly.AddRing(ring);
                        ring.AddPoint(new Point(point.X + left, point.Y));
                        ring.AddPoint(new Point(point.X + left, point.Y + h));
                        ring.AddPoint(new Point(point.X + left + width, point.Y + h));
                        ring.AddPoint(new Point(point.X + left + width, point.Y));

                        symbol.Draw(disp, poly);

                        if (_outlineSymbol != null && ring.PointCount > 0)
                        {
                            ring.AddPoint(new Point(point.X + left, point.Y));
                            _outlineSymbol.Draw(disp, new Polyline(ring));
                        }
                        left += width + (disp.MapScale / disp.Dpm);
                    }

                    #endregion
                }
                else if (_type == chartType.Stack)
                {
                    #region Draw Stack
                    double height = disp.MapScale / disp.Dpm * _size;
                    double width = disp.MapScale / disp.Dpm * _size / 4D;
                    double heightVal = _valueEquatesToSize;

                    Path outerPath = new Path();

                    switch (_sizeType)
                    {
                        case sizeType.ConstantSize:
                            if (valSum == 0.0)
                            {
                                return;
                            }

                            height *= _valueEquatesToSize / valSum;
                            break;
                    }

                    i = 0;
                    double left = -width / 2.0, bottom = 0D;
                    foreach (string fieldname in _symbolTable.Keys)
                    {
                        ISymbol symbol = _symbolTable[fieldname];

                        double h = height * (values[i++] / heightVal);
                        if (Math.Abs(h) < disp.MapScale / disp.Dpm)
                        {
                            h = disp.MapScale / disp.Dpm;
                        }

                        Polygon poly = new Polygon();
                        Ring ring = new Ring();
                        poly.AddRing(ring);
                        ring.AddPoint(new Point(point.X + left, point.Y + bottom));
                        ring.AddPoint(new Point(point.X + left, point.Y + bottom + h));
                        ring.AddPoint(new Point(point.X + left + width, point.Y + bottom + h));
                        ring.AddPoint(new Point(point.X + left + width, point.Y + bottom));

                        if (_outlineSymbol != null)
                        {
                            if (i == 1)
                            {
                                outerPath.AddPoint(new Point(point.X + left, point.Y + bottom));
                            }
                            if (i == _symbolTable.Keys.Count)
                            {
                                outerPath.AddPoint(new Point(point.X + left, point.Y + bottom + h));
                                outerPath.AddPoint(new Point(point.X + left + width, point.Y + bottom + h));
                                outerPath.AddPoint(new Point(point.X + left + width, point.Y));
                                outerPath.AddPoint(outerPath[0]);
                            }
                        }

                        symbol.Draw(disp, poly);
                        bottom += h;
                    }
                    if (_outlineSymbol != null && outerPath.PointCount > 0)
                    {
                        _outlineSymbol.Draw(disp, new Polyline(outerPath));
                    }
                    #endregion
                }
            }
            finally
            {
                ((Display)disp).Canvas = originalCanvas;
            }
        }

        #region IRenderer Member

        public List<Symbology.ISymbol> Symbols
        {
            get { return new List<Symbology.ISymbol>(); }
        }

        public bool Combine(IRenderer renderer)
        {
            return false;
        }

        #endregion

        #region IPersistable Member

        public void Load(IO.IPersistStream stream)
        {
            _type = (chartType)stream.Load("Type", (int)chartType.Pie);
            _sizeType = (sizeType)stream.Load("SizeType", (int)sizeType.ConstantSize);
            _labelPriority = (SimpleLabelRenderer.labelPriority)stream.Load("labelPriority", (int)SimpleLabelRenderer.labelPriority.normal);
            _size = (double)stream.Load("Size", 50D);
            _valueEquatesToSize = (double)stream.Load("ValueEquatesToSize", 100D);

            ValueMapRendererSymbol sym;
            while ((sym = (ValueMapRendererSymbol)stream.Load("ChartSymbol", null, new ValueMapRendererSymbol())) != null)
            {
                this.SetSymbol(sym._key, sym._symbol);
            }
            _outlineSymbol = stream.Load("Outline", null) as ILineSymbol;
        }

        public void Save(IO.IPersistStream stream)
        {
            stream.Save("Type", (int)_type);
            stream.Save("SizeType", (int)_sizeType);
            stream.Save("labelPriority", (int)_labelPriority);
            stream.Save("Size", _size);
            stream.Save("ValueEquatesToSize", _valueEquatesToSize);

            foreach (string key in _symbolTable.Keys)
            {
                ValueMapRendererSymbol sym = new ValueMapRendererSymbol(key, (ISymbol)_symbolTable[key]);
                stream.Save("ChartSymbol", sym);
            }
            if (_outlineSymbol != null)
            {
                stream.Save("Outline", _outlineSymbol);
            }
        }

        #endregion

        #region IClone2 Member

        public object Clone(CloneOptions options)
        {
            ChartLabelRenderer clone = new ChartLabelRenderer();

            foreach (string key in _symbolTable.Keys)
            {
                ISymbol symbol = (ISymbol)_symbolTable[key];
                if (symbol != null)
                {
                    symbol = (ISymbol)symbol.Clone(options);
                }

                clone._symbolTable.Add(key, symbol);
            }
            clone._labelPriority = _labelPriority;
            clone._type = _type;
            clone._outlineSymbol = (_outlineSymbol != null) ? (ILineSymbol)_outlineSymbol.Clone(options) : null;

            double fac = 1.0;

            var display = options?.Display;

            if (display != null && display.ReferenceScale > 1)
            {
                fac = display.ReferenceScale / Math.Max(display.MapScale, 1D);
            }

            clone._size = Math.Max(_size * fac, 1D);
            clone._valueEquatesToSize = _valueEquatesToSize;
            clone._sizeType = _sizeType;
            clone._clipEnvelope = new Envelope(_clipEnvelope);
            return clone;
        }

        public void Release()
        {
            foreach (string key in _symbolTable.Keys)
            {
                ISymbol symbol = (ISymbol)_symbolTable[key];
                if (symbol == null)
                {
                    continue;
                }

                symbol.Release();
            }
            _symbolTable.Clear();
        }

        #endregion

        #region IPriority Member

        public int Priority
        {
            get
            {
                switch (_labelPriority)
                {
                    case SimpleLabelRenderer.labelPriority.always:
                        return 0;
                    case SimpleLabelRenderer.labelPriority.high:
                        return 100;
                    case SimpleLabelRenderer.labelPriority.normal:
                        return 0;
                    case SimpleLabelRenderer.labelPriority.low:
                        return -100;
                }
                return 0;
            }
        }

        #endregion

        #region IPropertyPage Members

        public object PropertyPage(object initObject)
        {
            if (initObject is IFeatureLayer)
            {
                IFeatureLayer layer = (IFeatureLayer)initObject;
                if (layer.FeatureClass == null)
                {
                    return null;
                }

                string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Carto.Rendering.UI.dll");

                IPropertyPanel2 p = uiAssembly.CreateInstance("gView.Framework.Carto.Rendering.UI.PropertyForm_ChartLabelRenderer") as IPropertyPanel2;
                if (p != null)
                {
                    return p.PropertyPanel(this, (IFeatureLayer)initObject);
                }
            }

            return null;
        }

        public object PropertyPageObject()
        {
            return null;
        }

        #endregion

        #region ILegendGroup Member

        public int LegendItemCount
        {
            get { return _symbolTable.Count; }
        }

        public ILegendItem LegendItem(int index)
        {
            if (index < 0)
            {
                return null;
            }

            if (index <= _symbolTable.Count)
            {
                int count = 0;
                foreach (object lItem in _symbolTable.Values)
                {
                    if (count == index && lItem is ILegendItem)
                    {
                        return (LegendItem)lItem;
                    }

                    count++;
                }
            }
            return null;
        }

        public void SetSymbol(ILegendItem item, ISymbol symbol)
        {
            if (item == symbol)
            {
                return;
            }

            foreach (string key in _symbolTable.Keys)
            {
                if (!(_symbolTable[key] is ILegendItem))
                {
                    continue;
                }

                if (_symbolTable[key] == item)
                {
                    if (symbol is ILegendItem)
                    {
                        ((ILegendItem)symbol).LegendLabel = item.LegendLabel;
                    }

                    _symbolTable[key] = symbol;
                    return;
                }
            }
        }

        #endregion
    }
}
