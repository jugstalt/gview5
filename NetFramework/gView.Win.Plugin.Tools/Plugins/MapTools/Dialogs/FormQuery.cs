using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Dialogs
{
    internal partial class FormQuery : Form, IDockableWindow
    {
        private FormIdentify _parent = null;
        private BackgroundWorker _worker = new BackgroundWorker();
        private BackgroundWorker _worker2 = new BackgroundWorker();
        private CancelTracker _cancelTracker = new CancelTracker();
        private QueryThemeCombo _combo = null;
        private object lockThis = new object();

        public FormQuery(FormIdentify parent)
        {
            if (parent == null)
            {
                return;
            }

            InitializeComponent();

            _parent = parent;
            if (!Modal)
            {
                //this.TopLevel = false;
                //this.TopMost = true;
                //this.Parent = parent._doc.Application.ApplicationWindow;
                if (parent._doc.Application is IMapApplication)
                {
                    this.Owner = ((IMapApplication)parent._doc.Application).ApplicationWindow as Form;
                }
            }

            _combo = this.QueryCombo;
            if (_combo == null)
            {
                return;
            }

            _combo.SelectedItemChanged += new QueryThemeCombo.SelectedItemChangedEvent(QueryCombo_SelectedItemChanged);

            this.ThemeMode = _combo.ThemeMode;

            _worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            _worker2.DoWork += new DoWorkEventHandler(worker2_DoWork);
        }

        public QueryThemeMode ThemeMode
        {
            set
            {
                if (value == QueryThemeMode.Default)
                {
                    panelCustom.Visible = false;
                    panelStandard.Visible = true;
                    panelStandard.Dock = DockStyle.Fill;
                }
                else
                {
                    panelStandard.Visible = false;
                    panelCustom.Visible = true;
                    panelCustom.Dock = DockStyle.Fill;

                    BuildQueryForm();
                }
            }
        }

        #region Worker DefaultQuery
        private enum SearchType { allfields, field, displayfield }
        private SearchType _searchType = SearchType.allfields;
        private IdentifyMode _mode = IdentifyMode.visible;
        private string _queryField = "";
        private string _queryVal = "";
        private bool _useWildcards = false;
        private IMap _focusMap = null;

        async private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (_parent == null || _parent.MapDocument == null ||
                    _parent.MapDocument.FocusMap == null ||
                    _parent.MapDocument.FocusMap.Display == null)
                {
                    return;
                }

                ISpatialReference sRef =
                    (_parent.MapDocument.FocusMap.Display.SpatialReference != null) ?
                        _parent.MapDocument.FocusMap.Display.SpatialReference.Clone() as ISpatialReference :
                        null;

                StartProgress();

                List<IDatasetElement> elements = e.Argument as List<IDatasetElement>;
                if (elements == null)
                {
                    return;
                }

                foreach (IDatasetElement element in elements)
                {
                    if (!(element is IFeatureLayer))
                    {
                        continue;
                    }

                    IFeatureLayer layer = element as IFeatureLayer;
                    if (!(element.Class is IFeatureClass))
                    {
                        continue;
                    }

                    IFeatureClass fc = element.Class as IFeatureClass;
                    if (fc.Fields == null)
                    {
                        continue;
                    }

                    string val = _queryVal.Replace("*", "%");
                    string sval = _useWildcards ? AppendWildcards(val) : val;

                    //
                    // Collect Fields
                    //
                    FieldCollection queryFields = new FieldCollection();
                    foreach (IField field in fc.Fields.ToEnumerable())
                    {
                        if (field.type == FieldType.binary || field.type == FieldType.Shape)
                        {
                            continue;
                        }

                        if (field.name.Contains(":"))
                        {
                            continue;  // No Joined Fields
                        }

                        if (_searchType == SearchType.allfields)
                        {
                            queryFields.Add(field);
                        }
                        else if (_searchType == SearchType.field)
                        {
                            if (field.aliasname == _queryField)
                            {
                                queryFields.Add(field);
                            }
                        }
                        else if (_searchType == SearchType.displayfield)
                        {
                        }
                    }
                    if (queryFields.Count == 0)
                    {
                        continue;
                    }

                    //
                    // Build SQL Where Clause
                    //
                    StringBuilder sql = new StringBuilder();
                    foreach (IField field in queryFields.ToEnumerable())
                    {
                        switch (field.type)
                        {
                            case FieldType.character:
                            case FieldType.String:
                            case FieldType.NString:
                                if (sql.Length > 0)
                                {
                                    sql.Append(" OR ");
                                }

                                sql.Append(field.name);
                                sql.Append(sval.IndexOf("%") == -1 ? "=" : " like ");
                                sql.Append("'" + sval + "'");
                                break;
                            case FieldType.integer:
                            case FieldType.smallinteger:
                            case FieldType.biginteger:
                            case FieldType.Double:
                            case FieldType.Float:
                                if (IsNumeric(val))
                                {
                                    if (sql.Length > 0)
                                    {
                                        sql.Append(" OR ");
                                    }

                                    sql.Append(field.name + "=" + val);
                                }
                                break;
                        }
                    }
                    if (sql.Length == 0)
                    {
                        continue;
                    }

                    if (!_cancelTracker.Continue)
                    {
                        return;
                    }

                    string title = fc.Name;
                    if (_focusMap != null && _focusMap.TOC != null)
                    {
                        ITocElement tocElement = _focusMap.TOC.GetTOCElement(element as ILayer);
                        if (tocElement != null)
                        {
                            title = tocElement.Name;
                        }
                    }
                    //
                    // Query
                    //
                    QueryFilter filter = new QueryFilter();
                    filter.SubFields = "*";
                    filter.WhereClause = sql.ToString();
                    filter.FeatureSpatialReference = _parent.MapDocument.FocusMap.Display.SpatialReference;

                    SetMsgText("Query Table " + title, 1);
                    SetMsgText("", 2);
                    using (IFeatureCursor cursor = (await fc.Search(filter)) as IFeatureCursor)
                    {
                        if (cursor == null)
                        {
                            continue;
                        }

                        int counter = 0;
                        IFeature feature;
                        List<IFeature> features = new List<IFeature>();
                        while ((feature = await cursor.NextFeature()) != null)
                        {
                            if (!_cancelTracker.Continue)
                            {
                                return;
                            }
                            //this.AddFeature(feature, fc.SpatialReference, title);
                            features.Add(feature);
                            counter++;
                            if (counter % 100 == 0)
                            {
                                SetMsgText(counter + " Features...", 2);
                                this.AddFeature(features, sRef, layer, title, null, null, null);
                                features = new List<IFeature>();
                            }
                        }
                        if (features.Count > 0)
                        {
                            this.AddFeature(features, sRef, layer, title, null, null, null);
                        }

                        if (_mode == IdentifyMode.topmost && counter > 0)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                StopProgress();
            }
        }

        private bool IsNumeric(string val)
        {
            double num;
            return double.TryParse(val, out num);
        }

        private string AppendWildcards(string val)
        {
            if (val.Length == 0)
            {
                return "%";
            }

            val = "%" + val + "%";
            return val.Replace("%%", "%");
        }
        #endregion

        #region Worker Userdef Query 
        private Dictionary<int, string> _userdefValues = null;
        private QueryTheme _theme = null;

        async void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_theme == null || _theme.Nodes == null || _theme.Nodes.Count == 0 || _userdefValues == null)
            {
                return;
            }

            if (_parent == null || _parent.MapDocument == null ||
                    _parent.MapDocument.FocusMap == null ||
                    _parent.MapDocument.FocusMap.Display == null)
            {
                return;
            }

            ISpatialReference sRef =
                (_parent.MapDocument.FocusMap.Display.SpatialReference != null) ?
                    _parent.MapDocument.FocusMap.Display.SpatialReference.Clone() as ISpatialReference :
                    null;

            try
            {
                StartProgress();

                foreach (QueryThemeTable table in _theme.Nodes)
                {
                    if (table.QueryFieldDef == null)
                    {
                        continue;
                    }

                    IFeatureLayer layer = table.GetLayer(_parent._doc) as IFeatureLayer;
                    if (layer == null || !(layer.Class is IFeatureClass))
                    {
                        continue;
                    }

                    IFeatureClass fc = layer.Class as IFeatureClass;

                    #region SQL
                    StringBuilder sql = new StringBuilder();
                    int actPromptID = -99;
                    foreach (DataRow fieldDef in table.QueryFieldDef.Select("", "Prompt"))
                    {
                        string logic = " OR ";
                        if ((int)fieldDef["Prompt"] != actPromptID)
                        {
                            actPromptID = (int)fieldDef["Prompt"];
                            logic = ") AND (";
                        }

                        string val = "";
                        if (!_userdefValues.TryGetValue((int)fieldDef["Prompt"], out val))
                        {
                            continue;
                        }

                        if (val == "")
                        {
                            continue;
                        }

                        val = val.Replace("*", "%");

                        IField field = fc.FindField(fieldDef["Field"].ToString());
                        if (field == null)
                        {
                            continue;
                        }

                        switch (field.type)
                        {
                            case FieldType.biginteger:
                            case FieldType.Double:
                            case FieldType.Float:
                            case FieldType.smallinteger:
                            case FieldType.integer:
                                if (IsNumeric(val))
                                {
                                    if (sql.Length > 0)
                                    {
                                        sql.Append(logic);
                                    }

                                    sql.Append(field.name + fieldDef["Operator"] + val);
                                }
                                break;
                            case FieldType.String:
                                string op = fieldDef["Operator"].ToString();
                                string v = val;
                                if (v.IndexOf("%") != -1)
                                {
                                    op = " like ";
                                }
                                else if (op.ToLower() == " like ")
                                {
                                    v += "%";
                                }
                                if (sql.Length > 0)
                                {
                                    sql.Append(logic);
                                }

                                sql.Append(field.name + op + "'" + v + "'");
                                break;
                        }
                    }
                    if (sql.Length == 0)
                    {
                        continue;
                    }

                    sql.Insert(0, "(");
                    sql.Append(")");
                    #endregion

                    if (!_cancelTracker.Continue)
                    {
                        return;
                    }

                    #region Layer Title
                    string title = fc.Name;
                    if (_focusMap != null && _focusMap.TOC != null)
                    {
                        ITocElement tocElement = _focusMap.TOC.GetTOCElement(layer);
                        if (tocElement != null)
                        {
                            title = tocElement.Name;
                        }
                    }
                    #endregion

                    #region Fields
                    FieldCollection fields = null;
                    IField primaryDisplayField = null;

                    if (layer != null && layer.Fields != null && table.VisibleFieldDef != null && table.VisibleFieldDef.UseDefault == false)
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
                            fields.Add(f);
                        }
                    }
                    #endregion

                    #region QueryFilter
                    QueryFilter filter = new QueryFilter();
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

                        if (layer is IFeatureLayer && ((IFeatureLayer)layer).FeatureClass != null)
                        {
                            filter.AddField(((IFeatureLayer)layer).FeatureClass.ShapeFieldName);
                        }
                    }

                    filter.WhereClause = sql.ToString();
                    filter.FeatureSpatialReference = _parent.MapDocument.FocusMap.Display.SpatialReference;
                    #endregion

                    SetMsgText("Query Table " + title, 1);
                    SetMsgText("", 2);

                    #region Query
                    using (IFeatureCursor cursor = await fc.Search(filter) as IFeatureCursor)
                    {
                        if (cursor == null)
                        {
                            continue;
                        }

                        int counter = 0;
                        IFeature feature;
                        List<IFeature> features = new List<IFeature>();
                        while ((feature = await cursor.NextFeature()) != null)
                        {
                            if (!_cancelTracker.Continue)
                            {
                                return;
                            }
                            //this.AddFeature(feature, fc.SpatialReference, title);
                            features.Add(feature);
                            counter++;
                            if (counter % 100 == 0)
                            {
                                SetMsgText(counter + " Features...", 2);
                                ManualResetEvent resetEvent = new ManualResetEvent(false);

                                this.AddFeature(features, sRef, layer, title, fields, primaryDisplayField, resetEvent);
                                features = new List<IFeature>();

                                resetEvent.WaitOne();
                            }
                        }
                        if (features.Count > 0)
                        {
                            this.AddFeature(features, sRef, layer, title, fields, primaryDisplayField, null);
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                StopProgress();
            }
        }

        #endregion

        #region Callbacks
        private delegate void StartProgressCallback();
        private void StartProgress()
        {
            if (progressDisk.InvokeRequired)
            {
                StartProgressCallback d = new StartProgressCallback(StartProgress);
                this.Invoke(d);
            }
            else
            {
                _parent.Clear();
                progressDisk.Visible = true;
                progressDisk.Start(100);
            }
        }

        private delegate void StopProgressCallback();
        private void StopProgress()
        {
            if (progressDisk.InvokeRequired)
            {
                StopProgressCallback d = new StopProgressCallback(StopProgress);
                this.Invoke(d);
            }
            else
            {
                btnQuery.Enabled = true;
                btnStop.Enabled = false;
                progressDisk.Stop();
                progressDisk.Visible = false;
                _parent.WriteFeatureCount();
                lblMsg1.Text = lblMsg2.Text = "";
            }
        }

        private delegate void AddFeatureCallback(List<IFeature> features, ISpatialReference sRef, IFeatureLayer layer, string category, IFieldCollection fields, IField primaryDisplayField, ManualResetEvent resetEvent);
        private void AddFeature(List<IFeature> features, ISpatialReference sRef, IFeatureLayer layer, string category, IFieldCollection fields, IField primaryDisplayField, ManualResetEvent resetEvent)
        {
            if (_parent.InvokeRequired)
            {
                AddFeatureCallback d = new AddFeatureCallback(AddFeature);
                this.Invoke(d, new object[] { features, sRef, layer, category, fields, primaryDisplayField, resetEvent });
            }
            else
            {
                foreach (IFeature feature in features)
                {
                    _parent.AddFeature(feature, sRef, layer, category, fields, primaryDisplayField);
                }
                features.Clear();
                if (resetEvent != null)
                {
                    resetEvent.Set();
                }
            }
        }

        private delegate void SetMsgTextCallback(string text, int msg);
        private void SetMsgText(string text, int msg)
        {
            if (lblMsg1.InvokeRequired)
            {
                SetMsgTextCallback d = new SetMsgTextCallback(SetMsgText);
                this.Invoke(d, new object[] { text, msg });
            }
            else
            {
                if (msg == 1)
                {
                    lblMsg1.Text = text;
                }
                else if (msg == 2)
                {
                    lblMsg2.Text = text;
                }
            }
        }
        #endregion

        private void btnQuery_Click(object sender, EventArgs e)
        {
            if (_parent == null || _combo == null)
            {
                return;
            }

            if (_combo.ThemeMode == QueryThemeMode.Default)
            {
                _queryVal = cmbQueryText.Text;
                _queryField = cmbField.SelectedItem != null ? cmbField.SelectedItem.ToString() : "";
                _useWildcards = chkWildcards.Checked;
                _focusMap = _parent._doc != null ? _parent._doc.FocusMap : null;
                _mode = _parent.Mode;

                if (btnAllFields.Checked)
                {
                    _searchType = SearchType.allfields;
                }
                else if (btnField.Checked)
                {
                    _searchType = SearchType.field;
                }
                else if (btnDisplayField.Checked)
                {
                    _searchType = SearchType.displayfield;
                }
                else
                {
                    return;
                }

                if (!cmbQueryText.Items.Contains(_queryVal))
                {
                    cmbQueryText.Items.Add(_queryVal);
                }

                _cancelTracker.Reset();
                btnQuery.Enabled = false;
                btnStop.Enabled = true;
                _worker.RunWorkerAsync(_parent.AllQueryableLayers);
            }
            else
            {
                if (_combo.UserDefinedQueries == null)
                {
                    return;
                }

                foreach (QueryTheme theme in _combo.UserDefinedQueries.Queries)
                {
                    if (theme.Text == lblQueryName.Text)
                    {
                        if (theme.PromptDef == null)
                        {
                            return;
                        }

                        _userdefValues = new Dictionary<int, string>();
                        foreach (DataRow row in theme.PromptDef.Rows)
                        {
                            foreach (Control ctrl in panelCustomFields.Controls)
                            {
                                if (ctrl.Name == "txt" + row["ID"].ToString())
                                {
                                    _userdefValues.Add((int)row["ID"], ctrl.Text);
                                }
                            }
                        }
                        _theme = theme;
                        _focusMap = _parent._doc != null ? _parent._doc.FocusMap : null;

                        _cancelTracker.Reset();
                        btnQuery.Enabled = false;
                        btnStop.Enabled = true;
                        _worker2.RunWorkerAsync();
                    }
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            lock (_cancelTracker)
            {
                _cancelTracker.Cancel();
            }
        }

        private void btnSearch_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == btnAllFields || sender == btnDisplayField)
            {
                cmbField.Enabled = false;
            }
            else
            {

                cmbField.Enabled = true;

                FillFieldsCombo();
            }
        }

        #region IDockableWindow Member


        public DockWindowState DockableWindowState
        {
            get
            {
                return DockWindowState.none;
            }
            set
            {

            }
        }

        public Image Image
        {
            get { return null; }
        }

        #endregion

        private void FillFieldsCombo()
        {
            string selected = cmbField.SelectedItem as string;

            cmbField.Items.Clear();
            List<IDatasetElement> elements = _parent.AllQueryableLayers;
            if (elements == null)
            {
                return;
            }

            foreach (IDatasetElement element in elements)
            {
                if (!(element.Class is IFeatureClass))
                {
                    continue;
                }

                IFeatureClass fc = element.Class as IFeatureClass;
                if (fc.Fields == null)
                {
                    continue;
                }

                foreach (IField field in fc.Fields.ToEnumerable())
                {
                    if (field.type == FieldType.binary || field.type == FieldType.Shape)
                    {
                        continue;
                    }

                    if (!cmbField.Items.Contains(field.aliasname))
                    {
                        cmbField.Items.Add(field.aliasname);
                    }
                }
            }
            if (selected != null && cmbField.Items.Contains(selected))
            {
                cmbField.SelectedItem = selected;
            }

            if (cmbField.SelectedIndex == -1 && cmbField.Items.Count > 0)
            {
                cmbField.SelectedIndex = 0;
            }
        }

        private void cmbField_DropDown(object sender, EventArgs e)
        {
            FillFieldsCombo();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        private QueryThemeCombo QueryCombo
        {
            get
            {
                if (_parent == null || _parent._doc == null || !(_parent._doc.Application is IGUIApplication))
                {
                    return null;
                }

                return ((IGUIApplication)_parent._doc.Application).Tool(new Guid("51A2CF81-E343-4c58-9A42-9207C8DFBC01")) as QueryThemeCombo;
            }
        }

        #region CustomQuery 
        private QueryTheme GetQueryTheme(string name)
        {
            if (_combo == null || _combo.UserDefinedQueries == null)
            {
                return null;
            }

            foreach (QueryTheme theme in _combo.UserDefinedQueries.Queries)
            {
                if (theme.Text == name)
                {
                    return theme;
                }
            }
            return null;
        }
        private void BuildQueryForm()
        {
            panelCustomFields.Controls.Clear();
            if (_combo == null)
            {
                return;
            }

            QueryTheme query = GetQueryTheme(_combo.Text);
            if (query == null)
            {
                return;
            }

            lblQueryName.Text = query.Text;
            if (query.PromptDef == null)
            {
                return;
            }

            int y = 5;
            foreach (DataRow row in query.PromptDef.Rows)
            {
                System.Windows.Forms.Label label = new System.Windows.Forms.Label();
                label.Text = row["Prompt"].ToString() + ":" + (((bool)row["Obliging"]) ? "*" : "  ");
                label.Location = new System.Drawing.Point(10, y);
                label.Size = new Size(150, 21);
                label.TextAlign = ContentAlignment.MiddleRight;
                panelCustomFields.Controls.Add(label);

                Control txtBox = CustomInputControl.Create(row);
                if (txtBox == null)
                {
                    continue;
                }

                txtBox.Location = new System.Drawing.Point(163, y);
                txtBox.Size = new Size(190, 21);
                txtBox.Name = "txt" + row["ID"].ToString();
                panelCustomFields.Controls.Add(txtBox);
                y += 26;
            }
        }
        private void QueryCombo_SelectedItemChanged(string text)
        {
            if (_combo == null)
            {
                return;
            }

            if (_combo.ThemeMode == QueryThemeMode.Custom && text != lblQueryName.Text)
            {
                BuildQueryForm();
            }
        }
        #endregion
    }

    internal class CustomInputControl
    {
        static public Control Create(DataRow row)
        {
            if (row == null)
            {
                return null;
            }

            return new CustomQueryTextBox(row);
        }
    }
    internal interface ICustomInputControl
    {
        string UserText { get; }
    }
    internal class CustomQueryTextBox : TextBox, ICustomInputControl
    {
        private int _promptID = -1;

        public CustomQueryTextBox(DataRow row)
        {
            _promptID = (int)row["ID"];
        }

        #region ICustomInputControl Member

        public string UserText
        {
            get { return this.Text; }
        }

        #endregion
    }
}