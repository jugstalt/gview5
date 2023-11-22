using gView.Framework.Carto.Rendering.UI;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace gView.Framework.Carto.Rendering
{
    [gView.Framework.system.RegisterPlugIn("92650F7D-CEC9-4418-9EA0-A8B09436AA7A")]
    public class SimpleLabelRenderer : Cloner, ILabelRenderer, ILegendGroup, IPropertyPage, IPriority
    {
        public enum howManyLabels { one_per_name = 0, one_per_feature = 1, one_per_part = 2 }
        public enum labelPriority { always = 0, high = 1, normal = 2, low = 3 }
        public enum CartographicLineLabeling { Parallel = 0, CurvedText = 1, Horizontal = 2, Perpendicular = 3 }
        public enum CartographicPolygonLabelling { Horizontal = 0, Parallel = 1 }

        private string _fieldname, _sizeField, _fontField, _expression;
        private bool _useExpression = false;
        private ITextSymbol _symbol;

        private howManyLabels _howManyLabels = howManyLabels.one_per_feature;
        private labelPriority _labelPriority = labelPriority.normal;
        private List<string> _labelStrings = new List<string>();
        private SymbolRotation _symbolRotation;
        private CartographicLineLabeling _lineLabelling = CartographicLineLabeling.Parallel;

        public SimpleLabelRenderer()
        {
            _fieldname = _sizeField = _fontField = String.Empty;
            _symbol = new SimpleTextSymbol();

            _symbolRotation = new SymbolRotation();
        }

        private SimpleLabelRenderer(ITextSymbol symbol, string fieldname)
        {
            _symbol = symbol;
            _fieldname = fieldname;
            _sizeField = _fontField = String.Empty;
        }

        public string FieldName
        {
            get { return _fieldname; }
            set { _fieldname = value; }
        }

        public string SizeFieldName
        {
            get { return _sizeField; }
            set { _sizeField = value; }
        }

        public string FontField
        {
            get { return _fontField; }
            set { _fontField = value; }
        }

        public string LabelExpression
        {
            get { return _expression; }
            set { _expression = value; }
        }

        public bool UseExpression
        {
            get { return _useExpression; }
            set { _useExpression = value; }
        }

        public howManyLabels HowManyLabels
        {
            get
            {
                return _howManyLabels;
            }
            set
            {
                _howManyLabels = value;
            }
        }

        public labelPriority LabelPriority
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

        public CartographicLineLabeling CartoLineLabelling
        {
            get { return _lineLabelling; }
            set { _lineLabelling = value; }
        }

        public ITextSymbol TextSymbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }

        public SymbolRotation SymbolRotation
        {
            get { return _symbolRotation; }
            set
            {
                if (value == null)
                {
                    _symbolRotation.RotationFieldName = "";
                }
                else
                {
                    _symbolRotation = value;
                }
            }
        }

        private List<string> _expressionFields = new List<string>();
        private void ExtractExpressionFields()
        {
            string expr = _expression;
            _expressionFields = new List<string>();
            while (true)
            {
                if (expr == null || expr == "")
                {
                    break;
                }

                int p1 = expr.IndexOf("[");
                int p2 = expr.IndexOf("]");
                if (p1 >= p2)
                {
                    break;
                }

                _expressionFields.Add(expr.Substring(p1 + 1, p2 - p1 - 1));
                expr = expr.Substring(p2 + 1, expr.Length - p2 - 1);
            }
        }
        private IEnvelope _clipEnvelope = null;

        #region ILabelRenderer Members

        public void PrepareQueryFilter(IDisplay display, IFeatureLayer layer, IQueryFilter filter)
        {
            if (layer.FeatureClass == null)
            {
                return;
            }

            if (_useExpression)
            {
                ExtractExpressionFields();
                foreach (string fieldname in _expressionFields)
                {
                    if (layer.FeatureClass.FindField(fieldname) != null)
                    {
                        filter.AddField(fieldname);
                    }
                }
            }
            else
            {
                if (layer.FeatureClass.FindField(_fieldname) != null)
                {
                    filter.AddField(_fieldname);
                }
            }
            if (_sizeField != String.Empty)
            {
                filter.AddField(_sizeField);
            }

            if (_fontField != String.Empty)
            {
                filter.AddField(_fontField);
            }

            if (layer.FeatureClass.FindField(_symbolRotation.RotationFieldName) != null)
            {
                filter.AddField(_symbolRotation.RotationFieldName);
            }

            _clipEnvelope = new Envelope(display.DisplayTransformation.TransformedBounds(display));
            if (display.GeometricTransformer != null)
            {
                object e = display.GeometricTransformer.InvTransform2D(display.Envelope);
                if (e is IGeometry)
                {
                    _clipEnvelope = ((IGeometry)e).Envelope;
                }
            }
        }

        public bool CanRender(IFeatureLayer layer, IMap map)
        {
            return true;
        }

        public string Name
        {
            get { return "Simple Text Renderer"; }
        }

        public LabelRenderMode RenderMode
        {
            get
            {
                return (_labelPriority == labelPriority.always) ? LabelRenderMode.RenderWithFeature : LabelRenderMode.UseRenderPriority;
            }
        }

        public int RenderPriority
        {
            get
            {
                return 3 - (int)_labelPriority;
            }
        }

        public void Draw(IDisplay disp, IFeature feature)
        {
            if (!(_symbol is ISymbol))
            {
                return;
            }

            string expr = _expression;
            foreach (FieldValue fv in feature.Fields)
            {
                if (fv.Name == _fieldname && !_useExpression)
                {
                    _symbol.Text = fv.Value.ToString();
                }
                if (fv.Name == _symbolRotation.RotationFieldName)
                {
                    try
                    {
                        _symbol.Rotation = (float)_symbolRotation.Convert2DEGAritmetic(Convert.ToDouble(fv.Value));
                    }
                    catch { }
                }
                if (_useExpression)
                {
                    expr = expr.Replace("[" + fv.Name + "]", fv.Value.ToString());
                }
            }
            if (_useExpression)
            {
                if (SimpleScriptInterpreter.IsSimpleScript(expr))
                {
                    expr = new SimpleScriptInterpreter(expr).Interpret();
                }
                _symbol.Text = expr;
            }

            if (String.IsNullOrWhiteSpace(_symbol.Text))
            {
                return;
            }

            if (_howManyLabels == howManyLabels.one_per_name)
            {
                if (_labelStrings.Contains(_symbol.Text.Trim()))
                {
                    return;
                }
            }

            if (feature.Shape is IPoint)
            {
                if (disp.LabelEngine.TryAppend(disp, _symbol, feature.Shape, _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                {
                    if (_howManyLabels == howManyLabels.one_per_name)
                    {
                        _labelStrings.Add(_symbol.Text.Trim());
                    }
                }
            }
            else if (feature.Shape is IMultiPoint)
            {
                var multiPoint = (IMultiPoint)feature.Shape;
                for (int i = 0, i_to = multiPoint.PointCount; i < i_to; i++)
                {
                    if (multiPoint[i] == null)
                    {
                        continue;
                    }

                    if (disp.LabelEngine.TryAppend(disp, _symbol, multiPoint[i], _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                    {
                        if (_howManyLabels == howManyLabels.one_per_name)
                        {
                            _labelStrings.Add(_symbol.Text.Trim());
                        }

                        if (_howManyLabels == howManyLabels.one_per_part)
                        {
                            break;
                        }
                    }
                }
            }
            else if (feature.Shape is IPolyline)
            {
                IPoint point1 = null, point2 = null;
                double maxLenght2 = 0;

                IEnvelope dispEnv = _clipEnvelope; //disp.Envelope;
                //if (disp.GeometricTransformer != null)
                //{
                //    object e = disp.GeometricTransformer.InvTransform2D(disp.Envelope);
                //    if (e is IGeometry) dispEnv = ((IGeometry)e).Envelope;
                //}
                IPolyline pLine = (IPolyline)gView.Framework.SpatialAlgorithms.Clip.PerformClip(dispEnv, feature.Shape);

                if (pLine == null)
                {
                    return;
                }

                if (_lineLabelling == CartographicLineLabeling.CurvedText)
                {
                    #region Text On Path

                    IDisplayCharacterRanges ranges = _symbol.MeasureCharacterWidth(disp);
                    if (ranges == null)
                    {
                        return;
                    }

                    float textWidth = ranges.Width;

                    for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                    {
                        IPath path = pLine[iPath];
                        if (path == null)
                        {
                            continue;
                        }

                        IPointCollection pathPoints = path;
                        if (disp.GeometricTransformer != null)
                        {
                            pathPoints = (IPointCollection)disp.GeometricTransformer.Transform2D(pathPoints);
                        }

                        Geometry.DisplayPath displayPath = new Geometry.DisplayPath();
                        for (int iPoint = 0; iPoint < pathPoints.PointCount; iPoint++)
                        {
                            double x = pathPoints[iPoint].X, y = pathPoints[iPoint].Y;
                            disp.World2Image(ref x, ref y);
                            displayPath.AddPoint(new GraphicsEngine.CanvasPointF((float)x, (float)y));
                        }
                        float pathLenght = displayPath.Length;
                        if (pathLenght == 0.0 || textWidth > pathLenght)
                        {
                            continue;
                        }

                        displayPath.Chainage = pathLenght / 2f - textWidth / 2f;
                        float nextChainage = pathLenght / 10f;
                        bool found = false;
                        while (!found)
                        {
                            if (disp.LabelEngine.TryAppend(disp, _symbol, displayPath, _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                            {
                                found = true;
                                if (_howManyLabels == howManyLabels.one_per_name)
                                {
                                    _labelStrings.Add(_symbol.Text.Trim());
                                }

                                if (_howManyLabels != howManyLabels.one_per_part)
                                {
                                    break;
                                }
                            }
                            if (!found)
                            {
                                displayPath.Chainage = nextChainage;
                                nextChainage += pathLenght / 10f;
                            }
                            if (displayPath.Chainage + textWidth > pathLenght)
                            {
                                break;
                            }
                        }
                    }
                    #endregion
                }
                else if (_lineLabelling == CartographicLineLabeling.Horizontal ||
                         _lineLabelling == CartographicLineLabeling.Perpendicular)
                {
                    #region Horizontal

                    bool found = false;
                    for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                    {
                        IPath path = pLine[iPath];
                        if (path.PointCount < 2)
                        {
                            continue;
                        }

                        switch (this.TextSymbol.TextSymbolAlignment)
                        {
                            case TextSymbolAlignment.rightAlignOver:
                            case TextSymbolAlignment.rightAlignCenter:
                            case TextSymbolAlignment.rightAlignUnder:
                                point1 = path[1];
                                point2 = path[0];  // this will be alignment point in TextSymbol
                                found = true;
                                break;
                            case TextSymbolAlignment.leftAlignOver:
                            case TextSymbolAlignment.leftAlignCenter:
                            case TextSymbolAlignment.leftAlignUnder:
                                point1 = path[path.PointCount - 1];  // this will be alignment point in Textsymbol
                                point2 = path[path.PointCount - 2];
                                found = true;
                                break;
                            default:
                                for (int iPoint = 0, iPointTo = path.PointCount - 1; iPoint < iPointTo; iPoint++)
                                {
                                    IPoint p1 = path[iPoint];
                                    IPoint p2 = path[iPoint + 1];
                                    if (dispEnv.minx <= p1.X && dispEnv.maxx >= p1.X &&
                                        dispEnv.miny <= p1.Y && dispEnv.maxy >= p1.Y &&
                                        dispEnv.minx <= p2.X && dispEnv.maxx >= p2.X &&
                                        dispEnv.miny <= p2.Y && dispEnv.maxy >= p2.Y)
                                    {
                                        double len = /*Math.Sqrt*/((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                                        if (len > maxLenght2)
                                        {
                                            maxLenght2 = len;
                                            point1 = p1;
                                            point2 = p2;
                                        }
                                    }
                                }
                                break;
                        }
                        if (found == true)
                        {
                            break;
                        }
                    }
                    if (point1 != null && point2 != null)
                    {
                        Point p = new Point(point1.X * 0.5 + point2.X * 0.5,
                                            point1.Y * 0.5 + point2.Y * 0.5);

                        if (_lineLabelling == CartographicLineLabeling.Perpendicular)
                        {
                            double angle = Math.Atan2(point2.X - point1.X, point2.Y - point1.Y) * 180.0 / Math.PI;
                            if (angle < 0)
                            {
                                angle += 360;
                            }

                            //if (angle > 90 && angle < 270)
                            //{
                            //    angle -= 180;
                            //}

                            _symbol.Angle = (float)angle;
                        }
                        if (disp.LabelEngine.TryAppend(disp, _symbol, p, _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                        {
                            if (_howManyLabels == howManyLabels.one_per_name)
                            {
                                _labelStrings.Add(_symbol.Text.Trim());
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    #region Parallel Labelling

                    bool found = false;
                    for (int iPath = 0; iPath < pLine.PathCount; iPath++)
                    {
                        IPath path = pLine[iPath];
                        if (path.PointCount < 2)
                        {
                            continue;
                        }

                        switch (this.TextSymbol.TextSymbolAlignment)
                        {
                            case TextSymbolAlignment.rightAlignOver:
                            case TextSymbolAlignment.rightAlignCenter:
                            case TextSymbolAlignment.rightAlignUnder:
                                point1 = path[1];
                                point2 = path[0];  // this will be alignment point in TextSymbol
                                found = true;
                                break;
                            case TextSymbolAlignment.leftAlignOver:
                            case TextSymbolAlignment.leftAlignCenter:
                            case TextSymbolAlignment.leftAlignUnder:
                                point1 = path[path.PointCount - 1];  // this will be alignment point in Textsymbol
                                point2 = path[path.PointCount - 2];
                                found = true;
                                break;
                            default:
                                for (int iPoint = 0, iPointTo = path.PointCount - 1; iPoint < iPointTo; iPoint++)
                                {
                                    IPoint p1 = path[iPoint];
                                    IPoint p2 = path[iPoint + 1];
                                    if (dispEnv.minx <= p1.X && dispEnv.maxx >= p1.X &&
                                        dispEnv.miny <= p1.Y && dispEnv.maxy >= p1.Y &&
                                        dispEnv.minx <= p2.X && dispEnv.maxx >= p2.X &&
                                        dispEnv.miny <= p2.Y && dispEnv.maxy >= p2.Y)
                                    {
                                        double len2 = /*Math.Sqrt*/((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
                                        if (len2 > maxLenght2)
                                        {
                                            maxLenght2 = len2;
                                            //alpha = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X));
                                            point1 = p1;
                                            point2 = p2;
                                        }
                                    }
                                }
                                break;
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                    if (point1 != null && point2 != null)
                    {
                        pLine = new Polyline();
                        pLine.AddPath(new Path());
                        pLine[0].AddPoint(point1);
                        pLine[0].AddPoint(point2);

                        if (disp.LabelEngine.TryAppend(disp, _symbol, pLine, _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                        {
                            if (_howManyLabels == howManyLabels.one_per_name)
                            {
                                _labelStrings.Add(_symbol.Text.Trim());
                            }
                        }
                    }

                    #endregion
                }
            }
            else if (feature.Shape is IPolygon)
            {
                LabelPolygon(disp, (IPolygon)feature.Shape);
            }
        }

        #endregion

        private const int MaxPolygonTotalPointCount = 10000;  // Labelling is too expensive for complex polygons
        private bool LabelPolygon(IDisplay disp, IPolygon polygon)
        {
            //var center = new MultiPoint();
            //center.AddPoint(polygon.Envelope.Center);
            //return LabelPointCollection(disp, polygon, center);

            IEnvelope env = new Envelope(_clipEnvelope); //new Envelope(disp.Envelope);
            //env.Raise(70.0);
            if (polygon is ITopologicalOperation)
            {
                try
                {
                    double tolerance = 1.0 * disp.MapScale / disp.Dpm;  // 1 Pixel

                    // Wichtig bei Flächen mit sehr vielen Vertices... Bundesländer, Länder Thema kann sonst beim Clippen abstürzen
                    //if (polygon.TotalPointCount > MaxPolygonTotalPointCount)
                    //{
                    //polygon = SpatialAlgorithms.Algorithm.SnapOutsidePointsToEnvelope(polygon, env);
                    //polygon = (IPolygon)SpatialAlgorithms.Algorithm.Generalize(polygon, tolerance);
                    //}
                    // For testing polygons with many vertices an clipping
                    // polygon = (IPolygon)SpatialAlgorithms.Algorithm.InterpolatePoints(polygon, 2, true);

                    IGeometry g;
                    if (polygon.TotalPointCount > MaxPolygonTotalPointCount)
                    {
                        g = polygon;
                    }
                    else
                    {
                        ((ITopologicalOperation)polygon).Clip(env, out g);
                    }
                    if (g == null)
                    {
                        return false;
                    }

                    if (g is IPolygon)
                    {
                        polygon = (IPolygon)g;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            if (polygon == null)
            {
                return false;
            }

            if (polygon.TotalPointCount < MaxPolygonTotalPointCount)
            {
                IMultiPoint pColl = gView.Framework.SpatialAlgorithms.Algorithm.PolygonLabelPoints(polygon);
                return LabelPointCollection(disp, polygon, pColl);
            }
            else
            {
                return false;
            }
        }

        private bool LabelPointCollection(IDisplay disp, IPolygon polygon, IMultiPoint pColl)
        {
            if (pColl == null)
            {
                return false;
            }

            switch (_howManyLabels)
            {
                case howManyLabels.one_per_feature:
                case howManyLabels.one_per_name:
                    for (int i = 0; i < pColl.PointCount; i++)
                    {
                        if (disp.LabelEngine.TryAppend(disp, _symbol, pColl[i], _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                        {
                            if (_howManyLabels == howManyLabels.one_per_name)
                            {
                                _labelStrings.Add(_symbol.Text.Trim());
                            }

                            return true;
                        }
                    }
                    for (int i = 0; i < pColl.PointCount; i++)
                    {
                        ISmartLabelPoint slp = pColl[i] as ISmartLabelPoint;
                        if (slp != null)
                        {
                            LabelPointCollection(disp, polygon, slp.AlernativeLabelPoints(disp));
                        }
                    }
                    break;
                case howManyLabels.one_per_part:
                    if (disp.LabelEngine.TryAppend(disp, _symbol, pColl, _labelPriority != labelPriority.always) == LabelAppendResult.Succeeded)
                    {
                        if (_howManyLabels == howManyLabels.one_per_name)
                        {
                            _labelStrings.Add(_symbol.Text.Trim());
                        }

                        return true;
                    }
                    break;
            }

            return false;
        }

        #region IPersistable Members

        public void Load(gView.Framework.IO.IPersistStream stream)
        {
            _fieldname = (string)stream.Load("Fieldname");
            _sizeField = (string)stream.Load("Sizefield");
            _fontField = (string)stream.Load("Fontfield");
            _expression = (string)stream.Load("Expression", "");
            _useExpression = (bool)stream.Load("UseExpression", false);

            _symbol = (ITextSymbol)stream.Load("Symbol");

            _howManyLabels = (howManyLabels)stream.Load("howManyLabels", (int)howManyLabels.one_per_feature);
            _labelPriority = (labelPriority)stream.Load("labelPriority", (int)labelPriority.normal);
            _lineLabelling = (CartographicLineLabeling)stream.Load("lineLabelling", (int)CartographicLineLabeling.Parallel);

            _symbolRotation = (SymbolRotation)stream.Load("SymbolRotation", _symbolRotation, _symbolRotation);
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            stream.Save("Fieldname", _fieldname);
            stream.Save("Sizefield", _sizeField);
            stream.Save("Fontfield", _fontField);
            stream.Save("Expression", _expression);
            stream.Save("UseExpression", _useExpression);

            stream.Save("Symbol", _symbol);

            stream.Save("howManyLabels", (int)_howManyLabels);
            stream.Save("labelPriority", (int)_labelPriority);
            stream.Save("lineLabelling", (int)_lineLabelling);

            if (_symbolRotation.RotationFieldName != "")
            {
                stream.Save("SymbolRotation", _symbolRotation);
            }
        }

        #endregion

        #region IClone2 Members

        public object Clone(CloneOptions options)
        {
            SimpleLabelRenderer renderer = new SimpleLabelRenderer(
                (ITextSymbol)((_symbol is IClone2) ? _symbol.Clone(options) : null),
                _fieldname);

            renderer._howManyLabels = _howManyLabels;
            renderer._labelPriority = _labelPriority;
            renderer._fontField = _fontField;
            renderer._sizeField = _sizeField;
            renderer._expression = _expression;
            renderer._useExpression = _useExpression;
            renderer._symbolRotation = (SymbolRotation)_symbolRotation.Clone();
            renderer._clipEnvelope = _clipEnvelope != null ? new Envelope(_clipEnvelope) : null;
            renderer._lineLabelling = _lineLabelling;
            return renderer;
        }

        public void Release()
        {
            if (_symbol is ISymbol)
            {
                _symbol.Release();
            }

            _symbol = null;
            _labelStrings.Clear();
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

                IPropertyPanel2 p = uiAssembly.CreateInstance("gView.Framework.Carto.Rendering.UI.PropertyForm_SimpleLabelRenderer") as IPropertyPanel2;
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
            get { return _symbol != null ? 1 : 0; }
        }

        public ILegendItem LegendItem(int index)
        {
            if (index == 0)
            {
                return _symbol;
            }

            return null;
        }

        public void SetSymbol(ILegendItem item, ISymbol symbol)
        {
            if (item == _symbol && symbol is ITextSymbol)
            {
                _symbol = symbol as ITextSymbol;
            }
        }

        #endregion

        #region IPriority Member

        public int Priority
        {
            get
            {
                switch (_labelPriority)
                {
                    case labelPriority.always:
                        return 0;
                    case labelPriority.high:
                        return 100;
                    case labelPriority.normal:
                        return 0;
                    case labelPriority.low:
                        return -100;
                }
                return 0;
            }
        }

        #endregion

        #region IRenderer Member

        public List<ISymbol> Symbols
        {
            get { return new List<ISymbol>(new ISymbol[] { _symbol }); }
        }

        public bool Combine(IRenderer renderer)
        {
            return false;
        }
        #endregion
    }
}
