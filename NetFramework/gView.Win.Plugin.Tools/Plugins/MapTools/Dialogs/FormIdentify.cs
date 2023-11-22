using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.UI;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Dialogs
{
    internal enum IdentifyMode { visible, selectable, all, topmost, layer }

    internal partial class FormIdentify : UserControl, IDockableToolWindow
    {
        public enum ShowMode { Features, Text, HTML, Xml }
        internal IMapDocument _doc;
        private FormQuery _dlgQuery = null;

        public FormIdentify()
        {
            InitializeComponent();

            this.Name = LocalizedResources.GetResString("String.QueryResults", "Query Results");

            this.showMode = ShowMode.Features;
        }

        public IMapDocument MapDocument
        {
            set
            {
                _doc = value;
            }
            get
            {
                return _doc;
            }
        }

        public void SetLocation(double x, double y)
        {
            txtLocation.Text = "Location: (" + x.ToString() + "  " + y.ToString() + ")";
        }

        public void Clear()
        {
            treeObjects.Nodes.Clear();
            listValues.Items.Clear();

            txtIdentify.Text = String.Empty;
            webBrowserControl.Url = null;
            this.showMode = ShowMode.Features;
        }

        public void AddFeature(IFeature feature, ISpatialReference sRef, IFeatureLayer layer)
        {
            AddFeature(feature, sRef, layer, "???");
        }
        public void AddFeature(IFeature feature, ISpatialReference sRef, IFeatureLayer layer, string Category)
        {
            AddFeature(feature, sRef, layer, Category, null, null);
        }
        public void AddFeature(IFeature feature, ISpatialReference sRef, IFeatureLayer layer, string Category, IFieldCollection fields, IField primaryDisplayField)
        {
            if (feature == null)
            {
                return;
            }

            if (layer != null)
            {
                if (fields == null)
                {
                    fields = layer.Fields;
                }

                if (primaryDisplayField == null)
                {
                    primaryDisplayField = layer.Fields.PrimaryDisplayField;
                }
            }

            if (Category == "")
            {
                Category = "Results";
            }

            CategoryTreeNode parent = null;

            foreach (CategoryTreeNode node in treeObjects.Nodes)
            {
                if (node.Category == Category)
                {
                    parent = node;
                    break;
                }
            }
            if (parent == null)
            {
                parent = new CategoryTreeNode(Category, _doc, layer);
                treeObjects.Nodes.Add(parent);
            }

            parent.Nodes.Add(new FeatureTreeNode(_doc, feature, sRef, layer, (primaryDisplayField != null) ? primaryDisplayField.name : "", 1));

            if (fields != null && feature.Fields != null)
            {
                List<FieldValue> fvs = gView.Framework.system.ListOperations<FieldValue>.Clone(feature.Fields);
                feature.Fields.Clear();

                foreach (IField field in fields.ToEnumerable())
                {
                    if (!field.visible)
                    {
                        continue;
                    }

                    for (int i = 0; i < fvs.Count; i++)
                    {
                        if (fvs[i].Name == field.name)
                        {
                            feature.Fields.Add(new FieldValue(field.aliasname, fvs[i].Value));
                            fvs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            if (treeObjects.SelectedNode == null)
            {
                treeObjects.SelectedNode = parent.Nodes[0];

                if (_doc != null && _doc.FocusMap != null && _doc.FocusMap.Display != null)
                {
                    IGeometry shape = feature.Shape;
                    if (sRef != null && !sRef.Equals(_doc.FocusMap.Display.SpatialReference))
                    {
                        shape = GeometricTransformerFactory.Transform2D(shape, sRef, _doc.FocusMap.Display.SpatialReference);
                    }
                    _doc.FocusMap.HighlightGeometry(shape, 300);
                }
                //parent.ExpandAll();
                parent.Expand();
                if (parent.Nodes.Count > 0)
                {
                    parent.Nodes[0].Expand();
                }
            }
        }
        public void WriteFeatureCount()
        {
            foreach (TreeNode node in treeObjects.Nodes)
            {
                if (node is CategoryTreeNode)
                {
                    int count = node.Nodes.Count;
                    node.Text = ((CategoryTreeNode)node).Category + " (" + count.ToString() + ((count == 1) ? " Feature)" : " Features)");
                }
            }
        }

        #region IDockableWindow Members

        private DockWindowState _dockState = DockWindowState.none;
        public DockWindowState DockableWindowState
        {
            get
            {
                return _dockState;
            }
            set
            {
                _dockState = value;
            }
        }

        public Image Image
        {
            get { return (new Buttons()).imageList1.Images[10]; }
        }
        #endregion

        #region Tree Events

        private void treeObjects_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listValues.Items.Clear();

            if (treeObjects.SelectedNode is FeatureTreeNode)
            {
                listValues.Items.Clear();
                IFeature feature = ((FeatureTreeNode)treeObjects.SelectedNode).Feature;

                if (feature == null)
                {
                    return;
                }

                foreach (FieldValue fv in feature.Fields)
                {
                    string[] value = new string[2];
                    value[0] = fv.Name;
                    value[1] = (fv.Value == null) ? System.DBNull.Value.ToString() : fv.Value.ToString();
                    ListViewItem item = new ListViewItem(value);
                    listValues.Items.Add(item);
                }
                if (_doc.FocusMap != null)
                {
                    //_doc.FocusMap.HighlightGeometry(feature.Shape, 300);
                }
            }
            else if (treeObjects.SelectedNode != null)
            {
                //treeObjects.SelectedNode = treeObjects.SelectedNode.Nodes[0];
            }
        }

        private void treeObjects_MouseClick(object sender, MouseEventArgs e)
        {
            if (_doc == null)
            {
                return;
            }

            if (_doc.FocusMap == null)
            {
                return;
            }

            if (_doc.FocusMap.Display == null)
            {
                return;
            }

            if (_doc.Application == null)
            {
                return;
            }

            TreeNode node = treeObjects.GetNodeAt(e.X, e.Y);
            if (node is FeatureTreeNode && e.Button == MouseButtons.Left)
            {
                IFeature feature = ((FeatureTreeNode)node).Feature;
                ISpatialReference sRef = ((FeatureTreeNode)node).SpatialReference;
                ILayer layer = ((FeatureTreeNode)node).Layer;

                if (feature != null && feature.Shape != null)
                {
                    if (e.X > 50)
                    {
                        IGeometry shape = feature.Shape;
                        if (sRef != null && !sRef.Equals(_doc.FocusMap.Display.SpatialReference))
                        {
                            shape = GeometricTransformerFactory.Transform2D(shape, sRef, _doc.FocusMap.Display.SpatialReference);
                        }
                        _doc.FocusMap.HighlightGeometry(shape, 300);
                    }
                    else if (e.X >= 38)
                    {
                        IEnvelope envelope = feature.Shape.Envelope;
                        if (sRef != null && !sRef.Equals(_doc.FocusMap.Display.SpatialReference))
                        {
                            IGeometry geom = GeometricTransformerFactory.Transform2D(envelope,
                                sRef,
                                _doc.FocusMap.Display.SpatialReference);
                            if (geom == null)
                            {
                                return;
                            }

                            envelope = geom.Envelope;
                        }
                        _doc.FocusMap.Display.ZoomTo(envelope);
                        if (layer != null && layer.MaximumZoomToFeatureScale > _doc.FocusMap.Display.MapScale)
                        {
                            _doc.FocusMap.Display.MapScale = layer.MaximumZoomToFeatureScale;
                        }
                        if (_doc.Application is IMapApplication)
                        {
                            ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
                        }
                    }
                }
            }
            //else if(node is CategoryTreeNode)
            //{
            //    var categoryTreeNode = (CategoryTreeNode)node;
            //    MessageBox.Show(categoryTreeNode.Category);
            //}
        }

        private void treeObjects_MouseDown(object sender, MouseEventArgs e)
        {

        }

        async private void treeObjects_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node is RelationTreeNode)
            {
                IFeatureCursor cursor = null;
                RelationTreeNode rtn = (RelationTreeNode)e.Node;

                try
                {

                    rtn.Nodes.Clear();
                    FeatureTreeNode ftn = (FeatureTreeNode)rtn.Parent;

                    ITableRelation relation = rtn.TableRelation;
                    ILayer target = null;
                    if (relation.LeftTable == ftn.Layer && ftn.Feature[relation.LeftTableField] != null)
                    {
                        target = relation.RightTable as ILayer;
                        cursor = relation.GetRightRows(TargetFields(target), ftn.Feature[relation.LeftTableField]) as IFeatureCursor;
                    }
                    else if (relation.RightTable == ftn.Layer)
                    {
                        target = relation.LeftTable as ILayer;
                        cursor = relation.GetLeftRows(TargetFields(target), ftn.Feature[relation.RightTableField]) as IFeatureCursor;
                    }
                    if (cursor == null || target == null)
                    {
                        return;
                    }

                    string primaryDisplayField = String.Empty;
                    if (target is IFeatureLayer && ((IFeatureLayer)target).Fields.PrimaryDisplayField != null)
                    {
                        primaryDisplayField = ((IFeatureLayer)target).Fields.PrimaryDisplayField.name;
                    }

                    IFeature feature = null;
                    while ((feature = await cursor.NextFeature()) != null)
                    {
                        rtn.Nodes.Add(new FeatureTreeNode(_doc, feature,
                            _doc.FocusMap.Display.SpatialReference,
                            target,
                            (target is IFeatureLayer ? ((IFeatureLayer)target).Fields.PrimaryDisplayField.name : String.Empty),
                            1));
                    }
                }
                finally
                {
                    if (cursor != null)
                    {
                        cursor.Dispose();
                    }

                    if (rtn.Nodes.Count == 0)
                    {
                        rtn.Nodes.Add(new TreeNode());
                        rtn.Collapse();
                    }
                }
            }
        }

        #endregion

        public IdentifyMode Mode
        {
            get
            {
                if (_doc == null && !(_doc.Application is IGUIApplication))
                {
                    return IdentifyMode.visible;
                }

                QueryThemeCombo queryCombo = ((IGUIApplication)_doc.Application).Tool(new Guid("51A2CF81-E343-4c58-9A42-9207C8DFBC01")) as QueryThemeCombo;
                if (queryCombo == null)
                {
                    return IdentifyMode.visible;
                }

                return queryCombo.Mode;
            }
        }
        public List<IDatasetElement> AllQueryableLayers
        {
            get
            {
                if (_doc == null && !(_doc.Application is IGUIApplication))
                {
                    return new List<IDatasetElement>();
                }

                QueryThemeCombo queryCombo = ((IGUIApplication)_doc.Application).Tool(new Guid("51A2CF81-E343-4c58-9A42-9207C8DFBC01")) as QueryThemeCombo;
                if (queryCombo == null)
                {
                    return new List<IDatasetElement>();
                }

                return queryCombo.QueryableDatasetElements;
            }
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            if (_dlgQuery == null)
            {
                _dlgQuery = new FormQuery(this);
                /*
                if (_doc != null && _doc.Application is IMapApplication)
                {
                    IMapApplication app = _doc.Application as IMapApplication;
                    app.AddDockableWindow(_dlgQuery, DockWindowState.none);
                }
                 * */
                _dlgQuery.Show();
            }

            //_doc.Application.ShowDockableWindow(_dlgQuery);
            _dlgQuery.Visible = true;
            _dlgQuery.BringToFront();

        }

        public ShowMode showMode
        {
            set
            {
                tabControl1.TabPages.Remove(tabPageFeatures);
                tabControl1.TabPages.Remove(tabPageHTML);
                tabControl1.TabPages.Remove(tabPageText);

                switch (value)
                {
                    case ShowMode.Features:
                        tabControl1.TabPages.Add(tabPageFeatures);
                        break;
                    case ShowMode.HTML:
                        tabControl1.TabPages.Add(tabPageHTML);
                        break;
                    case ShowMode.Text:
                        tabControl1.TabPages.Add(tabPageText);
                        break;
                }
            }
        }
        public string IdentifyText
        {
            get { return txtIdentify.Text; }
            set
            {
                txtIdentify.Text = value.Replace("\r\n", "\n").Replace("\n", "\r\n");

                if (!String.IsNullOrEmpty(txtIdentify.Text) &&
                    !tabControl1.TabPages.Contains(tabPageText))
                {
                    tabControl1.TabPages.Add(tabPageText);
                }
            }
        }
        public string IdentifyUrl
        {
            get
            {
                if (webBrowserControl.Url == null)
                {
                    return String.Empty;
                }

                return webBrowserControl.Url.AbsoluteUri;
            }
            set
            {
                try
                {
                    if (tabControl1.TabPages.Contains(tabPageHTML))
                    {
                        return;
                    }

                    if (String.IsNullOrEmpty(value))
                    {
                        webBrowserControl.Url = null;
                        tabControl1.TabPages.Remove(tabPageHTML);
                    }
                    else
                    {
                        webBrowserControl.Url = new Uri(value);
                        if (!tabControl1.TabPages.Contains(tabPageHTML))
                        {
                            tabControl1.TabPages.Add(tabPageHTML);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: " + ex.Message + "\n" + value);
                }
            }
        }

        public void ShowResult()
        {
            if (treeObjects.Nodes.Count == 0)
            {
                if (tabControl1.TabPages.Contains(tabPageText))
                {
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPageText);
                }
                else if (tabControl1.TabPages.Contains(tabPageHTML))
                {
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPageHTML);
                }
            }
            else
            {
                if (tabControl1.TabPages.Contains(tabPageFeatures))
                {
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(tabPageFeatures);
                }
            }
        }

        private string TargetFields(ILayer target)
        {
            StringBuilder fields = new StringBuilder();
            if (target is IFeatureLayer && ((IFeatureLayer)target).Fields != null)
            {
                foreach (IField field in ((IFeatureLayer)target).Fields.ToEnumerable())
                {
                    if (!field.visible)
                    {
                        continue;
                    }

                    if (fields.Length > 0)
                    {
                        fields.Append(" ");
                    }

                    fields.Append(field.name);
                }
            }
            if (fields.Length == 0)
            {
                fields.Append("*");
            }

            return fields.ToString();
        }
    }

    internal class FeatureTreeNode : TreeNode
    {
        private readonly IFeature _feature;
        private readonly ILayer _layer;
        private readonly ISpatialReference _sRef;

        public FeatureTreeNode(IMapDocument mapDocument, IFeature feature, ISpatialReference sRef, ILayer layer, string primaryFieldName)
        {
            if (feature == null)
            {
                return;
            }

            if (primaryFieldName != "")
            {
                object val = feature[primaryFieldName];
                if (val == null)
                {
                    base.Text = System.DBNull.Value.ToString();
                }
                else
                {
                    base.Text = val.ToString();
                }
            }
            else
            {
                bool found = false;
                foreach (FieldValue fv in feature.Fields)
                {
                    if (fv.Value == null)
                    {
                        base.Text = System.DBNull.Value.ToString();
                    }
                    else if (fv.Value.GetType() == typeof(System.String))
                    {
                        if (fv.Value.ToString().Trim().Length > 0)
                        {
                            base.Text = fv.Value.ToString();
                            found = true;
                            break;
                        }
                    }

                }
                if (!found)
                {
                    base.Text = feature.OID.ToString();
                }
            }

            _feature = feature;
            _layer = layer;
            _sRef = sRef;

            if (mapDocument != null && mapDocument.TableRelations != null)
            {
                foreach (ITableRelation tableRelation in mapDocument.TableRelations.GetRelations(_layer))
                {
                    this.Nodes.Add(new RelationTreeNode(tableRelation, _layer));
                }
            }
        }
        public FeatureTreeNode(IMapDocument mapDocument, IFeature feature, ISpatialReference sRef, ILayer layer, string primaryFieldName, int ImageIndex)
            : this(mapDocument, feature, sRef, layer, primaryFieldName)
        {
            base.ImageIndex = base.SelectedImageIndex = ImageIndex;
        }
        public IFeature Feature
        {
            get { return _feature; }
        }
        public ISpatialReference SpatialReference
        {
            get
            {
                return _sRef;
            }
        }
        public ILayer Layer
        {
            get { return _layer; }
        }
    }

    internal class RelationTreeNode : TreeNode
    {
        public RelationTreeNode(ITableRelation tableRelation, IDatasetElement element)
        {
            this.TableRelation = tableRelation;
            this.DatasetElement = element;

            IDatasetElement target = (tableRelation.LeftTable == element) ? tableRelation.RightTable : tableRelation.LeftTable;
            base.Text = tableRelation.RelationName + " (" + target.Title + ")";
            base.ImageIndex = 0;

            base.Nodes.Add(new TreeNode());
        }

        public ITableRelation TableRelation { get; set; }
        public IDatasetElement DatasetElement { get; set; }
    }

    internal class CategoryTreeNode : TreeNode
    {
        private readonly IMapDocument _mapDocument;
        private readonly IMap _map;

        public CategoryTreeNode(string category,
                                IMapDocument mapDocument,
                                IFeatureLayer featureLayer)
        {
            this.Category = category;
            _mapDocument = mapDocument;
            _map = _mapDocument.FocusMap;
            this.FeatureLayer = featureLayer;

            if (this.FeatureLayer?.Class?.Dataset != null)
            {
                this.ContextMenu = new ContextMenu(
                    new MenuItem[]
                    {
                    new MenuItem("Layer Properties...", OnLayerProperties)
                    });
            }
        }

        public string Category
        {
            get; private set;
        }

        public IFeatureLayer FeatureLayer { get; private set; }

        private void OnLayerProperties(object sender, EventArgs e)
        {
            var dlg = new FormLayerProperties(_mapDocument, _map,
                                              FeatureLayer.Class.Dataset,
                                              FeatureLayer);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (_mapDocument.Application is IMapApplication)
                {
                    ((IMapApplication)_mapDocument.Application).RefreshTOCElement((ILayer)FeatureLayer);
                    ((IMapApplication)_mapDocument.Application).RefreshActiveMap(DrawPhase.All);
                }
                if (_mapDocument.FocusMap != null)
                {
                    _mapDocument.FocusMap.TOC.Reset();
                }
            }
        }
    }

    internal class ModeItem
    {
        private IdentifyMode _mode;
        IFeatureLayer _layer;

        public ModeItem(IdentifyMode mode)
        {
            _mode = mode;
        }
        public ModeItem(IFeatureLayer layer)
        {
            _mode = IdentifyMode.layer;
            _layer = layer;
        }

        public IdentifyMode Mode
        {
            get
            {
                return _mode;
            }
        }
        public IFeatureLayer Layer
        {
            get { return _layer; }
        }
        public override string ToString()
        {
            switch (_mode)
            {
                case IdentifyMode.visible:
                    return "<Visible Layers>";
                case IdentifyMode.selectable:
                    return "<Selectable Layers>";
                case IdentifyMode.all:
                    return "<All Layers>";
                case IdentifyMode.topmost:
                    return "<Top Most Layers>";
                case IdentifyMode.layer:
                    if (_layer != null)
                    {
                        return _layer.Title;
                    }
                    break;
            }
            return "???";
        }
    }

    internal class TOCElementItem
    {
        ITocElement _element;

        public TOCElementItem(ITocElement element)
        {
            _element = element;
        }

        public ITocElement Element
        {
            get { return _element; }
        }

        public override string ToString()
        {
            return _element.Name;
        }
    }
}