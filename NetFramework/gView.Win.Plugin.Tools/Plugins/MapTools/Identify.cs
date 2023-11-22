using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Events;
using gView.Plugins.MapTools.Dialogs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("F13D5923-70C8-4c6b-9372-0760D3A8C08C")]
    public class Identify : gView.Framework.UI.ITool, gView.Framework.UI.IToolWindow, IPersistable
    {
        private IMapDocument _doc = null;
        private FormIdentify _dlg = null;
        private ToolType _type = ToolType.click;
        private double _tolerance = 3.0;

        public Identify()
        {
            //_dlg = new FormIdentify();
        }

        #region ITool Members

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.Identify", "Identify"); }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return "Identify"; }
        }

        public ToolType toolType
        {
            get
            {
                if (_dlg == null)
                {
                    return _type;
                }

                if (_type == ToolType.rubberband)
                {
                    QueryThemeCombo combo = QueryCombo;
                    if (combo == null)
                    {
                        return _type;
                    }

                    if (combo.ThemeMode == QueryThemeMode.Default)
                    {
                        List<IDatasetElement> allQueryableElements = _dlg.AllQueryableLayers;
                        if (allQueryableElements == null)
                        {
                            return _type;
                        }

                        foreach (IDatasetElement element in allQueryableElements)
                        {
                            if (element.Class is IPointIdentify/* && queryPoint != null*/)
                            {
                                return ToolType.click;
                            }
                        }
                    }

                    return _type;
                }
                else
                {
                    return _type;
                }
            }
            set { _type = value; }
        }

        public object Image
        {
            get
            {
                return gView.Win.Plugin.Tools.Properties.Resources.info;
            }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                _dlg = new FormIdentify();
                _dlg.MapDocument = _doc;
            }
            if (hook is Control)
            {

            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (_dlg == null || !(MapEvent is MapEvent))
            {
                return true;
            }

            Envelope envelope = null;
            IMap map = null;

            double tol = 0;

            map = ((MapEvent)MapEvent).Map;
            // 3 Pixel Toleranz
            if (map is IDisplay)
            {
                tol = _tolerance * ((IDisplay)map).MapScale / (96 / 0.0254);  // [m]
                if (map.Display.SpatialReference != null &&
                    map.Display.SpatialReference.SpatialParameters.IsGeographic)
                {
                    tol = (180.0 * tol / Math.PI) / 6370000.0;
                }
            }

            ISpatialReference mapSR = (map.Display.SpatialReference != null) ? map.Display.SpatialReference.Clone() as ISpatialReference : null;

            IPoint queryPoint = null;
            if (MapEvent is MapEventClick)
            {
                MapEventClick ev = (MapEventClick)MapEvent;
                map = ev.Map;
                if (map == null || map.Display == null)
                {
                    return true;
                }

                queryPoint = new Point(ev.x, ev.y);
                envelope = new Envelope(ev.x - tol / 2, ev.y - tol / 2, ev.x + tol / 2, ev.y + tol / 2);
                _dlg.Clear();
                _dlg.SetLocation(ev.x, ev.y);
            }
            else if (MapEvent is MapEventRubberband)
            {
                MapEventRubberband ev = (MapEventRubberband)MapEvent;
                map = ev.Map;
                if (map == null || map.Display == null)
                {
                    return true;
                }

                envelope = new Envelope(ev.minX, ev.minY, ev.maxX, ev.maxY);
                if (envelope.Width < tol)
                {
                    envelope.minx = 0.5 * envelope.minx + 0.5 * envelope.maxx - tol / 2.0;
                    envelope.maxx = 0.5 * envelope.minx + 0.5 * envelope.maxx + tol / 2.0;
                }
                if (envelope.Height < tol)
                {
                    envelope.miny = 0.5 * envelope.miny + 0.5 * envelope.maxy - tol / 2.0;
                    envelope.maxy = 0.5 * envelope.miny + 0.5 * envelope.maxy + tol / 2.0;
                }
                _dlg.Clear();
                //_dlg.setLocation(envelope.);
            }
            else
            {
                return true;
            }

            QueryThemeCombo combo = QueryCombo;

            if (combo == null || combo.ThemeMode == QueryThemeMode.Default)
            {
                #region ThemeMode Default
                IdentifyMode mode = _dlg.Mode;

                int counter = 0;
                List<IDatasetElement> allQueryableElements = _dlg.AllQueryableLayers;
                if (allQueryableElements == null)
                {
                    return true;
                }

                foreach (IDatasetElement element in allQueryableElements)
                {
                    #region IPointIdentify
                    if (element.Class is IPointIdentify/* && queryPoint != null*/)
                    {
                        IPoint iPoint = null;
                        if (queryPoint != null)
                        {
                            iPoint = queryPoint;
                        }
                        else if (queryPoint == null && envelope != null)
                        {
                            if (envelope.Width < 3.0 * tol && envelope.Height < 3.0 * tol)
                            {
                                iPoint = new Point((envelope.minx + envelope.maxx) * 0.5, (envelope.miny + envelope.maxy) * 0.5);
                            }
                        }

                        if (iPoint != null)
                        {
                            using (var pointIdentifyContext = ((IPointIdentify)element.Class).CreatePointIdentifyContext())
                            using (ICursor cursor = await ((IPointIdentify)element.Class).PointQuery(map.Display, queryPoint, map.Display.SpatialReference, new UserData(), pointIdentifyContext))
                            {
                                if (cursor is IRowCursor)
                                {
                                    IRow row;
                                    while ((row = await ((IRowCursor)cursor).NextRow()) != null)
                                    {
                                        _dlg.AddFeature(new Feature(row), mapSR, null, element.Title);
                                        counter++;
                                    }
                                }
                                else if (cursor is ITextCursor)
                                {
                                    _dlg.IdentifyText += ((ITextCursor)cursor).Text;
                                }
                                else if (cursor is IUrlCursor)
                                {
                                    _dlg.IdentifyUrl = ((IUrlCursor)cursor).Url;
                                }
                            }
                        }
                    }
                    #endregion

                    if (!(element is IFeatureLayer))
                    {
                        continue;
                    }

                    IFeatureLayer layer = (IFeatureLayer)element;

                    IFeatureClass fc = layer.Class as IFeatureClass;
                    if (fc == null)
                    {
                        continue;
                    }

                    #region QueryFilter
                    SpatialFilter filter = new SpatialFilter();
                    filter.Geometry = envelope;
                    filter.FilterSpatialReference = map.Display.SpatialReference;
                    filter.FeatureSpatialReference = map.Display.SpatialReference;
                    filter.SpatialRelation = spatialRelation.SpatialRelationIntersects;

                    if (layer.FilterQuery != null)
                    {
                        filter.WhereClause = layer.FilterQuery.WhereClause;
                    }

                    IFieldCollection fields = layer.Fields;
                    if (fields == null)
                    {
                        filter.SubFields = "*";
                    }
                    else
                    {
                        foreach (IField field in fields.ToEnumerable())
                        {
                            if (!field.visible)
                            {
                                continue;
                            }

                            filter.AddField(field.name);
                        }
                        if (layer.Fields.PrimaryDisplayField != null)
                        {
                            filter.AddField(layer.Fields.PrimaryDisplayField.name);
                        }
                    }
                    #endregion

                    #region Layer Title
                    string primaryFieldName = (layer.Fields.PrimaryDisplayField != null) ? layer.Fields.PrimaryDisplayField.name : "";
                    string title = element.Title;

                    if (map.TOC != null)
                    {
                        ITocElement tocElement = map.TOC.GetTOCElement(layer);
                        if (tocElement != null)
                        {
                            title = tocElement.Name;
                        }
                    }
                    #endregion

                    #region Query
                    using (IFeatureCursor cursor = (IFeatureCursor)await fc.Search(filter))
                    {
                        if (cursor != null)
                        {
                            IFeature feature;
                            while ((feature = await cursor.NextFeature()) != null)
                            {
                                _dlg.AddFeature(feature, mapSR, layer, title);
                                counter++;
                            }
                            cursor.Dispose();
                        }
                    }
                    #endregion

                    if (mode == IdentifyMode.topmost && counter > 0)
                    {
                        break;
                    }
                }
                #endregion
            }
            else if (combo.ThemeMode == QueryThemeMode.Custom)
            {
                #region ThemeMode Custom
                foreach (QueryTheme theme in combo.UserDefinedQueries.Queries)
                {
                    if (theme.Text == combo.Text)
                    {
                        foreach (QueryThemeTable table in theme.Nodes)
                        {
                            IFeatureLayer layer = table.GetLayer(_doc) as IFeatureLayer;
                            if (layer == null || !(layer.Class is IFeatureClass))
                            {
                                continue;
                            }

                            IFeatureClass fc = layer.Class as IFeatureClass;

                            #region Fields
                            IFieldCollection fields = null;
                            IField primaryDisplayField = null;

                            if (layer.Fields != null && table.VisibleFieldDef != null && table.VisibleFieldDef.UseDefault == false)
                            {
                                fields = new FieldCollection();

                                foreach (IField field in layer.Fields.ToEnumerable())
                                {
                                    if (table.VisibleFieldDef.PrimaryDisplayField == field.name)
                                    {
                                        primaryDisplayField = field;
                                    }

                                    DataRow[] r = table.VisibleFieldDef.Select("Visible=true AND Name='" + field.name + "'");
                                    if (r.Length == 0)
                                    {
                                        continue;
                                    }

                                    Field f = new Field(field);
                                    f.visible = true;
                                    f.aliasname = (string)r[0]["Alias"];
                                    ((FieldCollection)fields).Add(f);
                                }
                            }
                            else
                            {
                                fields = layer.Fields;
                                primaryDisplayField = layer.Fields.PrimaryDisplayField;
                            }
                            #endregion

                            #region QueryFilter
                            SpatialFilter filter = new SpatialFilter();
                            filter.Geometry = envelope;
                            filter.FilterSpatialReference = map.Display.SpatialReference;
                            filter.FeatureSpatialReference = map.Display.SpatialReference;
                            filter.SpatialRelation = spatialRelation.SpatialRelationIntersects;

                            if (layer.FilterQuery != null)
                            {
                                filter.WhereClause = layer.FilterQuery.WhereClause;
                            }

                            if (fields == null)
                            {
                                filter.SubFields = "*";
                            }
                            else
                            {
                                foreach (IField field in fields.ToEnumerable())
                                {
                                    if (!field.visible)
                                    {
                                        continue;
                                    }

                                    filter.AddField(field.name);
                                }
                                if (primaryDisplayField != null)
                                {
                                    filter.AddField(primaryDisplayField.name);
                                }
                            }
                            #endregion

                            #region Layer Title
                            string primaryFieldName = (layer.Fields.PrimaryDisplayField != null) ? layer.Fields.PrimaryDisplayField.name : "";
                            string title = layer.Title;

                            if (map.TOC != null)
                            {
                                ITocElement tocElement = map.TOC.GetTOCElement(layer);
                                if (tocElement != null)
                                {
                                    title = tocElement.Name;
                                }
                            }
                            #endregion

                            #region Query
                            using (IFeatureCursor cursor = (IFeatureCursor)await fc.Search(filter))
                            {
                                IFeature feature;
                                while ((feature = await cursor.NextFeature()) != null)
                                {
                                    _dlg.AddFeature(feature, mapSR, layer, title, fields, primaryDisplayField);
                                }
                                cursor.Dispose();
                            }
                            #endregion
                        }
                    }
                }
                #endregion
            }

            _dlg.WriteFeatureCount();
            _dlg.ShowResult();

            if (_doc.Application is IGUIApplication)
            {
                IGUIApplication appl = (IGUIApplication)_doc.Application;

                foreach (IDockableWindow win in appl.DockableWindows)
                {
                    if (win == _dlg)
                    {
                        appl.ShowDockableWindow(win);
                        return true;
                    }
                }

                appl.AddDockableWindow(_dlg, null);
                appl.ShowDockableWindow(_dlg);
            }

            return true;
        }

        private object ICursor(IPointIdentify iPointIdentify)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IToolWindow Members

        public IDockableWindow ToolWindow
        {
            get { return _dlg; }
        }

        #endregion

        public double Tolerance
        {
            get { return _tolerance; }
            set { _tolerance = value; }
        }

        private QueryThemeCombo QueryCombo
        {
            get
            {
                if (_doc == null || !(_doc.Application is IGUIApplication))
                {
                    return null;
                }

                return ((IGUIApplication)_doc.Application).Tool(new Guid("51A2CF81-E343-4c58-9A42-9207C8DFBC01")) as QueryThemeCombo;
            }
        }

        private Find FindTool
        {
            get
            {
                if (_doc == null || !(_doc.Application is IGUIApplication))
                {
                    return null;
                }

                return ((IGUIApplication)_doc.Application).Tool(new Guid("ED5B0B59-2F5D-4b1a-BAD2-3CABEF073A6A")) as Find;
            }
        }

        internal QueryThemes UserDefinedQueries
        {
            get
            {
                QueryThemeCombo combo = QueryCombo;
                if (combo == null)
                {
                    return null;
                }

                return combo.UserDefinedQueries;
            }
            set
            {
                QueryThemeCombo combo = QueryCombo;
                if (combo == null)
                {
                    return;
                }

                combo.UserDefinedQueries = value;
            }
        }

        internal QueryThemeMode ThemeMode
        {
            get
            {
                QueryThemeCombo combo = QueryCombo;
                if (combo == null)
                {
                    return QueryThemeMode.Default;
                }

                return combo.ThemeMode;
            }
            set
            {
                QueryThemeCombo combo = QueryCombo;
                if (combo != null)
                {
                    combo.ThemeMode = value;
                    combo.RebuildCombo();
                }

                Find find = FindTool;
                if (find != null)
                {
                    find.ThemeMode = value;
                }
            }
        }

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _type = (ToolType)stream.Load("ToolType", (int)ToolType.click);
            _tolerance = (double)stream.Load("Tolerance", 3.0);

            UserDefinedQueries = stream.Load("UserDefinedQueries", null, new QueryThemes(null)) as QueryThemes;
            ThemeMode = (QueryThemeMode)stream.Load("QueryMode", (int)QueryThemeMode.Default);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("ToolType", (int)_type);
            stream.Save("Tolerance", _tolerance);

            if (UserDefinedQueries != null)
            {
                stream.Save("UserDefinedQueries", UserDefinedQueries);
            }

            stream.Save("QueryMode", (int)ThemeMode);
        }

        #endregion
    }
}
