using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.UI;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Controls
{
    internal enum selectionMothode
    {
        Rectangle,
        Multipoint,
        Polyline,
        Polygon
    }

    public class SelectionEnvironmentControl : System.Windows.Forms.UserControl, IDockableToolWindow
    {
        private IContainer components;
        private ImageList imageList1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem toolStripMenuItemSelectByAttributes;
        private ToolStrip toolStrip1;
        private ToolStripButton btnCategories;
        private ToolStripButton btnAlphabetic;
        private TreeView tree;

        private IMapDocument _doc = null;
        private selectionMothode _methode;
        private ToolStripSeparator toolStripSeparator1;

        private enum ViewState { categories, alphabetic }
        private ViewState viewState = ViewState.categories;
        private ToolStripDropDownButton btnSelectionMethode;
        private ToolStripMenuItem btnMethodeRectangle;
        private ToolStripMenuItem btnMethodeMultipoint;
        private ToolStripMenuItem btnMethodePolyline;
        private ToolStripMenuItem btnMethodePolygon;
        private ToolStripButton btnApplySelection;
        private ToolStripButton btnClearSelectionFigure;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripDropDownButton btnSpatialRelation;
        private ToolStripMenuItem btnIntersects;
        private ToolStripMenuItem btnContains;
        private ToolStripMenuItem btnWithin;
        private Panel panel1;
        private Panel panelSelByLoc;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private Button btnApplySelByLocation;
        private Button btnCloseSelByLocation;
        private CheckBox chkSelByLoc_AppendBuffer;
        private ComboBox cmbSelByLoc_BufferUnit;
        private NumericUpDown numSelByLoc_BufferDistance;
        private System.Windows.Forms.Label label4;
        private CheckBox chkSelByLoc_ApplyBuffer;
        private CheckBox chkSelByLoc_UseSelected;
        private ComboBox cmbSelByLocLayer;
        private ToolStripButton btnSelByLocation;
        private ToolStripButton btnSelByGraphic;
        private ToolStripMenuItem toolStripMenuItemClearSelection;
        private ToolStripMenuItem toolStripMenuItemZoomToSelection;
        private ToolStripDropDownButton btnCombination;
        private ToolStripMenuItem btnCombinationNew;
        private ToolStripMenuItem btnCombinationUnion;
        private ToolStripMenuItem btnCombinationIntersection;
        private ToolStripMenuItem btnCombinationDifference;
        private ToolStripMenuItem btnCombinationSymDifference;
        private Select _tool = null;
        private string _selectedString = String.Empty;

        public SelectionEnvironmentControl(Select tool)
        {
            // Dieser Aufruf ist für den Windows Form-Designer erforderlich.
            InitializeComponent();

            _tool = tool;
            SelectionMethode = selectionMothode.Rectangle;
            SpatialRelation = (_tool != null) ? _tool.SpatialRelation : spatialRelation.SpatialRelationIntersects;
            CombinationMethode = (_tool != null) ? _tool.CombinationMethode : CombinationMethod.New;

            _selectedString = chkSelByLoc_UseSelected.Text;
        }

        /// <summary> 
        /// Die verwendeten Ressourcen bereinigen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code
        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectionEnvironmentControl));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemSelectByAttributes = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemZoomToSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemClearSelection = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnCategories = new System.Windows.Forms.ToolStripButton();
            this.btnAlphabetic = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSelectionMethode = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnMethodeRectangle = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMethodeMultipoint = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMethodePolyline = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMethodePolygon = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSpatialRelation = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnIntersects = new System.Windows.Forms.ToolStripMenuItem();
            this.btnContains = new System.Windows.Forms.ToolStripMenuItem();
            this.btnWithin = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCombination = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnCombinationNew = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCombinationUnion = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCombinationDifference = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCombinationIntersection = new System.Windows.Forms.ToolStripMenuItem();
            this.btnCombinationSymDifference = new System.Windows.Forms.ToolStripMenuItem();
            this.btnApplySelection = new System.Windows.Forms.ToolStripButton();
            this.btnClearSelectionFigure = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSelByLocation = new System.Windows.Forms.ToolStripButton();
            this.btnSelByGraphic = new System.Windows.Forms.ToolStripButton();
            this.tree = new System.Windows.Forms.TreeView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panelSelByLoc = new System.Windows.Forms.Panel();
            this.btnApplySelByLocation = new System.Windows.Forms.Button();
            this.btnCloseSelByLocation = new System.Windows.Forms.Button();
            this.chkSelByLoc_AppendBuffer = new System.Windows.Forms.CheckBox();
            this.cmbSelByLoc_BufferUnit = new System.Windows.Forms.ComboBox();
            this.numSelByLoc_BufferDistance = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.chkSelByLoc_ApplyBuffer = new System.Windows.Forms.CheckBox();
            this.chkSelByLoc_UseSelected = new System.Windows.Forms.CheckBox();
            this.cmbSelByLocLayer = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panelSelByLoc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSelByLoc_BufferDistance)).BeginInit();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "select_1.png");
            // 
            // contextMenuStrip1
            // 
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemSelectByAttributes,
            this.toolStripMenuItemZoomToSelection,
            this.toolStripMenuItemClearSelection});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            // 
            // toolStripMenuItemSelectByAttributes
            // 
            resources.ApplyResources(this.toolStripMenuItemSelectByAttributes, "toolStripMenuItemSelectByAttributes");
            this.toolStripMenuItemSelectByAttributes.Name = "toolStripMenuItemSelectByAttributes";
            this.toolStripMenuItemSelectByAttributes.Click += new System.EventHandler(this.toolStripMenuItemSelectByAttributes_Click);
            // 
            // toolStripMenuItemZoomToSelection
            // 
            resources.ApplyResources(this.toolStripMenuItemZoomToSelection, "toolStripMenuItemZoomToSelection");
            this.toolStripMenuItemZoomToSelection.Name = "toolStripMenuItemZoomToSelection";
            this.toolStripMenuItemZoomToSelection.Click += new System.EventHandler(this.toolStripMenuItemZoomToSelection_Click);
            // 
            // toolStripMenuItemClearSelection
            // 
            resources.ApplyResources(this.toolStripMenuItemClearSelection, "toolStripMenuItemClearSelection");
            this.toolStripMenuItemClearSelection.Name = "toolStripMenuItemClearSelection";
            this.toolStripMenuItemClearSelection.Click += new System.EventHandler(this.toolStripMenuItemClearSelection_Click);
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnCategories,
            this.btnAlphabetic,
            this.toolStripSeparator1,
            this.btnSelectionMethode,
            this.btnSpatialRelation,
            this.btnCombination,
            this.btnApplySelection,
            this.btnClearSelectionFigure,
            this.toolStripSeparator2,
            this.btnSelByLocation,
            this.btnSelByGraphic});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // btnCategories
            // 
            resources.ApplyResources(this.btnCategories, "btnCategories");
            this.btnCategories.Checked = true;
            this.btnCategories.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnCategories.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnCategories.Name = "btnCategories";
            this.btnCategories.Click += new System.EventHandler(this.btnCategories_Click);
            // 
            // btnAlphabetic
            // 
            resources.ApplyResources(this.btnAlphabetic, "btnAlphabetic");
            this.btnAlphabetic.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAlphabetic.Name = "btnAlphabetic";
            this.btnAlphabetic.Click += new System.EventHandler(this.btnAlphabetic_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            // 
            // btnSelectionMethode
            // 
            resources.ApplyResources(this.btnSelectionMethode, "btnSelectionMethode");
            this.btnSelectionMethode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSelectionMethode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnMethodeRectangle,
            this.btnMethodeMultipoint,
            this.btnMethodePolyline,
            this.btnMethodePolygon});
            this.btnSelectionMethode.Name = "btnSelectionMethode";
            // 
            // btnMethodeRectangle
            // 
            resources.ApplyResources(this.btnMethodeRectangle, "btnMethodeRectangle");
            this.btnMethodeRectangle.Name = "btnMethodeRectangle";
            this.btnMethodeRectangle.Click += new System.EventHandler(this.btnMethodeRectangle_Click);
            // 
            // btnMethodeMultipoint
            // 
            resources.ApplyResources(this.btnMethodeMultipoint, "btnMethodeMultipoint");
            this.btnMethodeMultipoint.Name = "btnMethodeMultipoint";
            this.btnMethodeMultipoint.Click += new System.EventHandler(this.btnMethodeMultipoint_Click);
            // 
            // btnMethodePolyline
            // 
            resources.ApplyResources(this.btnMethodePolyline, "btnMethodePolyline");
            this.btnMethodePolyline.Name = "btnMethodePolyline";
            this.btnMethodePolyline.Click += new System.EventHandler(this.btnMethodePolyline_Click);
            // 
            // btnMethodePolygon
            // 
            resources.ApplyResources(this.btnMethodePolygon, "btnMethodePolygon");
            this.btnMethodePolygon.Name = "btnMethodePolygon";
            this.btnMethodePolygon.Click += new System.EventHandler(this.btnMethodePolygon_Click);
            // 
            // btnSpatialRelation
            // 
            resources.ApplyResources(this.btnSpatialRelation, "btnSpatialRelation");
            this.btnSpatialRelation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSpatialRelation.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnIntersects,
            this.btnContains,
            this.btnWithin});
            this.btnSpatialRelation.Name = "btnSpatialRelation";
            // 
            // btnIntersects
            // 
            resources.ApplyResources(this.btnIntersects, "btnIntersects");
            this.btnIntersects.Name = "btnIntersects";
            this.btnIntersects.Click += new System.EventHandler(this.btnIntersects_Click);
            // 
            // btnContains
            // 
            resources.ApplyResources(this.btnContains, "btnContains");
            this.btnContains.Name = "btnContains";
            this.btnContains.Click += new System.EventHandler(this.btnContains_Click);
            // 
            // btnWithin
            // 
            resources.ApplyResources(this.btnWithin, "btnWithin");
            this.btnWithin.Name = "btnWithin";
            this.btnWithin.Click += new System.EventHandler(this.btnWithin_Click);
            // 
            // btnCombination
            // 
            resources.ApplyResources(this.btnCombination, "btnCombination");
            this.btnCombination.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnCombination.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnCombinationNew,
            this.btnCombinationUnion,
            this.btnCombinationDifference,
            this.btnCombinationIntersection,
            this.btnCombinationSymDifference});
            this.btnCombination.Name = "btnCombination";
            // 
            // btnCombinationNew
            // 
            resources.ApplyResources(this.btnCombinationNew, "btnCombinationNew");
            this.btnCombinationNew.Name = "btnCombinationNew";
            this.btnCombinationNew.Click += new System.EventHandler(this.btnCombination_Click);
            // 
            // btnCombinationUnion
            // 
            resources.ApplyResources(this.btnCombinationUnion, "btnCombinationUnion");
            this.btnCombinationUnion.Name = "btnCombinationUnion";
            this.btnCombinationUnion.Click += new System.EventHandler(this.btnCombination_Click);
            // 
            // btnCombinationDifference
            // 
            resources.ApplyResources(this.btnCombinationDifference, "btnCombinationDifference");
            this.btnCombinationDifference.Name = "btnCombinationDifference";
            this.btnCombinationDifference.Click += new System.EventHandler(this.btnCombination_Click);
            // 
            // btnCombinationIntersection
            // 
            resources.ApplyResources(this.btnCombinationIntersection, "btnCombinationIntersection");
            this.btnCombinationIntersection.Name = "btnCombinationIntersection";
            this.btnCombinationIntersection.Click += new System.EventHandler(this.btnCombination_Click);
            // 
            // btnCombinationSymDifference
            // 
            resources.ApplyResources(this.btnCombinationSymDifference, "btnCombinationSymDifference");
            this.btnCombinationSymDifference.Name = "btnCombinationSymDifference";
            this.btnCombinationSymDifference.Click += new System.EventHandler(this.btnCombination_Click);
            // 
            // btnApplySelection
            // 
            resources.ApplyResources(this.btnApplySelection, "btnApplySelection");
            this.btnApplySelection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnApplySelection.Name = "btnApplySelection";
            this.btnApplySelection.Click += new System.EventHandler(this.btnApplySelection_Click);
            // 
            // btnClearSelectionFigure
            // 
            resources.ApplyResources(this.btnClearSelectionFigure, "btnClearSelectionFigure");
            this.btnClearSelectionFigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnClearSelectionFigure.Name = "btnClearSelectionFigure";
            this.btnClearSelectionFigure.Click += new System.EventHandler(this.btnClearSelectionFigure_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            // 
            // btnSelByLocation
            // 
            resources.ApplyResources(this.btnSelByLocation, "btnSelByLocation");
            this.btnSelByLocation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSelByLocation.Name = "btnSelByLocation";
            this.btnSelByLocation.Click += new System.EventHandler(this.btnSelByLocation_Click);
            // 
            // btnSelByGraphic
            // 
            resources.ApplyResources(this.btnSelByGraphic, "btnSelByGraphic");
            this.btnSelByGraphic.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSelByGraphic.Name = "btnSelByGraphic";
            this.btnSelByGraphic.Click += new System.EventHandler(this.btnSelByGraphic_Click);
            // 
            // tree
            // 
            resources.ApplyResources(this.tree, "tree");
            this.tree.CheckBoxes = true;
            this.tree.Name = "tree";
            this.tree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.tree_AfterCheck);
            this.tree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tree_MouseDown);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.label1);
            this.panel1.Name = "panel1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // panelSelByLoc
            // 
            resources.ApplyResources(this.panelSelByLoc, "panelSelByLoc");
            this.panelSelByLoc.Controls.Add(this.btnApplySelByLocation);
            this.panelSelByLoc.Controls.Add(this.btnCloseSelByLocation);
            this.panelSelByLoc.Controls.Add(this.chkSelByLoc_AppendBuffer);
            this.panelSelByLoc.Controls.Add(this.cmbSelByLoc_BufferUnit);
            this.panelSelByLoc.Controls.Add(this.numSelByLoc_BufferDistance);
            this.panelSelByLoc.Controls.Add(this.label4);
            this.panelSelByLoc.Controls.Add(this.chkSelByLoc_ApplyBuffer);
            this.panelSelByLoc.Controls.Add(this.chkSelByLoc_UseSelected);
            this.panelSelByLoc.Controls.Add(this.cmbSelByLocLayer);
            this.panelSelByLoc.Controls.Add(this.label3);
            this.panelSelByLoc.Name = "panelSelByLoc";
            // 
            // btnApplySelByLocation
            // 
            resources.ApplyResources(this.btnApplySelByLocation, "btnApplySelByLocation");
            this.btnApplySelByLocation.Name = "btnApplySelByLocation";
            this.btnApplySelByLocation.UseVisualStyleBackColor = true;
            this.btnApplySelByLocation.Click += new System.EventHandler(this.btnApplySelByLocation_Click);
            // 
            // btnCloseSelByLocation
            // 
            resources.ApplyResources(this.btnCloseSelByLocation, "btnCloseSelByLocation");
            this.btnCloseSelByLocation.Name = "btnCloseSelByLocation";
            this.btnCloseSelByLocation.UseVisualStyleBackColor = true;
            this.btnCloseSelByLocation.Click += new System.EventHandler(this.btnCloseSelByLocation_Click);
            // 
            // chkSelByLoc_AppendBuffer
            // 
            resources.ApplyResources(this.chkSelByLoc_AppendBuffer, "chkSelByLoc_AppendBuffer");
            this.chkSelByLoc_AppendBuffer.Name = "chkSelByLoc_AppendBuffer";
            this.chkSelByLoc_AppendBuffer.UseVisualStyleBackColor = true;
            // 
            // cmbSelByLoc_BufferUnit
            // 
            resources.ApplyResources(this.cmbSelByLoc_BufferUnit, "cmbSelByLoc_BufferUnit");
            this.cmbSelByLoc_BufferUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelByLoc_BufferUnit.FormattingEnabled = true;
            this.cmbSelByLoc_BufferUnit.Name = "cmbSelByLoc_BufferUnit";
            // 
            // numSelByLoc_BufferDistance
            // 
            resources.ApplyResources(this.numSelByLoc_BufferDistance, "numSelByLoc_BufferDistance");
            this.numSelByLoc_BufferDistance.DecimalPlaces = 5;
            this.numSelByLoc_BufferDistance.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numSelByLoc_BufferDistance.Minimum = new decimal(new int[] {
            100000000,
            0,
            0,
            -2147483648});
            this.numSelByLoc_BufferDistance.Name = "numSelByLoc_BufferDistance";
            this.numSelByLoc_BufferDistance.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // chkSelByLoc_ApplyBuffer
            // 
            resources.ApplyResources(this.chkSelByLoc_ApplyBuffer, "chkSelByLoc_ApplyBuffer");
            this.chkSelByLoc_ApplyBuffer.Name = "chkSelByLoc_ApplyBuffer";
            this.chkSelByLoc_ApplyBuffer.UseVisualStyleBackColor = true;
            this.chkSelByLoc_ApplyBuffer.CheckedChanged += new System.EventHandler(this.chkSelByLoc_ApplyBuffer_CheckedChanged);
            // 
            // chkSelByLoc_UseSelected
            // 
            resources.ApplyResources(this.chkSelByLoc_UseSelected, "chkSelByLoc_UseSelected");
            this.chkSelByLoc_UseSelected.Name = "chkSelByLoc_UseSelected";
            this.chkSelByLoc_UseSelected.UseVisualStyleBackColor = true;
            this.chkSelByLoc_UseSelected.CheckedChanged += new System.EventHandler(this.chkSelByLoc_UseSelected_CheckedChanged);
            // 
            // cmbSelByLocLayer
            // 
            resources.ApplyResources(this.cmbSelByLocLayer, "cmbSelByLocLayer");
            this.cmbSelByLocLayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelByLocLayer.FormattingEnabled = true;
            this.cmbSelByLocLayer.Name = "cmbSelByLocLayer";
            this.cmbSelByLocLayer.SelectedIndexChanged += new System.EventHandler(this.cmbSelByLocLayer_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // SelectionEnvironmentControl
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tree);
            this.Controls.Add(this.panelSelByLoc);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "SelectionEnvironmentControl";
            this.contextMenuStrip1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panelSelByLoc.ResumeLayout(false);
            this.panelSelByLoc.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSelByLoc_BufferDistance)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        public IMapDocument MapDocument
        {
            set
            {
                _doc = value;
                LinkEvents();
                buildTree();
            }
            get
            {
                return _doc;
            }
        }

        private void LinkEvents()
        {
            if (_doc == null)
            {
                return;
            }

            if (_doc is IMapDocumentEvents)
            {
                ((IMapDocumentEvents)_doc).MapAdded -= new MapAddedEvent(_iMapDocument_MapAdded);
                ((IMapDocumentEvents)_doc).MapDeleted -= new MapDeletedEvent(_iMapDocument_MapDeleted);
                ((IMapDocumentEvents)_doc).AfterSetFocusMap -= new AfterSetFocusMapEvent(_iMapDocument_AferSetFocusMap);

                ((IMapDocumentEvents)_doc).MapAdded += new MapAddedEvent(_iMapDocument_MapAdded);
                ((IMapDocumentEvents)_doc).MapDeleted += new MapDeletedEvent(_iMapDocument_MapDeleted);
                ((IMapDocumentEvents)_doc).AfterSetFocusMap += new AfterSetFocusMapEvent(_iMapDocument_AferSetFocusMap);
            }

            if (_doc.FocusMap != null && _doc.FocusMap.TOC != null)
            {
                _doc.FocusMap.TOC.TocChanged -= new EventHandler(TOC_TocChanged);
                _doc.FocusMap.TOC.TocChanged += new EventHandler(TOC_TocChanged);
                if (_doc.FocusMap.Display.GraphicsContainer != null)
                {
                    _doc.FocusMap.Display.GraphicsContainer.SelectionChanged -= new EventHandler(GraphicsContainer_SelectionChanged);
                    _doc.FocusMap.Display.GraphicsContainer.SelectionChanged += new EventHandler(GraphicsContainer_SelectionChanged);
                }
            }

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).AfterLoadMapDocument -= new AfterLoadMapDocumentEvent(Application_AfterLoadMapDocument);
                ((IMapApplication)_doc.Application).AfterLoadMapDocument += new AfterLoadMapDocumentEvent(Application_AfterLoadMapDocument);
            }
        }

        void GraphicsContainer_SelectionChanged(object sender, EventArgs e)
        {
            btnSelByGraphic.Enabled = false;
            IGraphicsContainer container = sender as IGraphicsContainer;
            if (container == null)
            {
                return;
            }

            btnSelByGraphic.Enabled = (container.SelectedElements.Count > 0);
        }

        void Application_AfterLoadMapDocument(IMapDocument mapDocument)
        {
            MapDocument = mapDocument;
        }

        internal selectionMothode SelectionMethode
        {
            get { return _methode; }
            set
            {
                _methode = value;

                if (_tool != null)
                {
                    _tool.SelectionMethode = value;
                }

                switch (_methode)
                {
                    case selectionMothode.Rectangle:
                        btnSelectionMethode.Image = btnMethodeRectangle.Image;
                        break;
                    case selectionMothode.Multipoint:
                        btnSelectionMethode.Image = btnMethodeMultipoint.Image;
                        break;
                    case selectionMothode.Polyline:
                        btnSelectionMethode.Image = btnMethodePolyline.Image;
                        break;
                    case selectionMothode.Polygon:
                        btnSelectionMethode.Image = btnMethodePolygon.Image;
                        break;
                }

                btnApplySelection.Enabled = btnClearSelectionFigure.Enabled =
                    _methode != selectionMothode.Rectangle;
            }
        }
        internal spatialRelation SpatialRelation
        {
            get { return (_tool != null) ? _tool.SpatialRelation : spatialRelation.SpatialRelationIntersects; }
            set
            {
                if (_tool != null)
                {
                    _tool.SpatialRelation = value;
                }

                btnIntersects.Checked = btnContains.Checked = btnWithin.Checked = false;

                switch (value)
                {
                    case spatialRelation.SpatialRelationIntersects:
                        btnSpatialRelation.Image = btnIntersects.Image;
                        btnIntersects.Checked = true;
                        break;
                    case spatialRelation.SpatialRelationContains:
                        btnSpatialRelation.Image = btnContains.Image;
                        btnContains.Checked = true;
                        break;
                    case spatialRelation.SpatialRelationWithin:
                        btnSpatialRelation.Image = btnWithin.Image;
                        btnWithin.Checked = true;
                        break;
                }
            }
        }
        internal CombinationMethod CombinationMethode
        {
            get { return (_tool != null) ? _tool.CombinationMethode : CombinationMethod.New; }
            set
            {
                if (_tool != null)
                {
                    _tool.CombinationMethode = value;
                }

                btnCombinationNew.Checked = btnCombinationSymDifference.Checked =
                    btnCombinationUnion.Checked = btnCombinationIntersection.Checked =
                    btnCombinationDifference.Checked = false;

                switch (value)
                {
                    case CombinationMethod.New:
                        btnCombination.Image = btnCombinationNew.Image;
                        btnCombinationNew.Checked = true;
                        break;
                    case CombinationMethod.Union:
                        btnCombination.Image = btnCombinationUnion.Image;
                        btnCombinationUnion.Checked = true;
                        break;
                    case CombinationMethod.Difference:
                        btnCombination.Image = btnCombinationDifference.Image;
                        btnCombinationDifference.Checked = true;
                        break;
                    case CombinationMethod.Intersection:
                        btnCombination.Image = btnCombinationIntersection.Image;
                        btnCombinationIntersection.Checked = true;
                        break;
                    case CombinationMethod.SymDifference:
                        btnCombination.Image = btnCombinationSymDifference.Image;
                        btnCombinationSymDifference.Checked = true;
                        break;
                }
            }
        }

        private void _iMapDocument_LayerAdded(IMap sender, ILayer layer)
        {
            buildTree();
        }
        private void _iMapDocument_MapAdded(IMap map)
        {
            buildTree();
        }
        private void _iMapDocument_MapDeleted(IMap map)
        {
            buildTree();
        }
        private void _iMapDocument_AferSetFocusMap(IMap map)
        {
            if (map.TOC != null)
            {
                map.TOC.TocChanged -= new EventHandler(TOC_TocChanged);
                map.TOC.TocChanged += new EventHandler(TOC_TocChanged);
                if (_doc.FocusMap.Display.GraphicsContainer != null)
                {
                    _doc.FocusMap.Display.GraphicsContainer.SelectionChanged -= new EventHandler(GraphicsContainer_SelectionChanged);
                    _doc.FocusMap.Display.GraphicsContainer.SelectionChanged += new EventHandler(GraphicsContainer_SelectionChanged);
                }
            }
            buildTree();
        }

        void TOC_TocChanged(object sender, EventArgs e)
        {
            buildTree();
        }

        private void buildTree()
        {
            if (_doc == null)
            {
                return;
            }

            IMap map = _doc.FocusMap;
            if (map == null)
            {
                return;
            }

            if (map.TOC == null)
            {
                return;
            }

            ISelectionEnvironment selEnv = map.SelectionEnvironment;

            tree.Nodes.Clear();
            List<IDatasetElement> selLayers = selEnv.SelectableElements;

            tree.AfterCheck -= new TreeViewEventHandler(tree_AfterCheck);
            if (viewState == ViewState.categories)
            {
                tree.ShowLines = tree.ShowPlusMinus = tree.ShowRootLines = true;

                if (map.TOC == null)
                {
                    return;
                }

                List<ITocElement> tocElements = map.TOC.Elements;

                foreach (ITocElement tocElement in tocElements)
                {
                    if (tocElement.ParentGroup != null)
                    {
                        continue;
                    }

                    insertTreeNode(tocElements, tocElement, null, selLayers);
                }
            }
            else
            {
                tree.ShowLines = tree.ShowPlusMinus = tree.ShowRootLines = false;

                List<IDatasetElement> layers = map.MapElements;
                layers.Sort(new DatasetElementComparer());

                List<ITocElement> tocElements = map.TOC.Elements;
                tocElements.Sort(new TOCElementComparer());

                foreach (ITocElement tocElement in tocElements)
                {
                    if (tocElement.ElementType != TocElementType.Layer)
                    {
                        continue;
                    }

                    insertTreeNode(tocElements, tocElement, null, selLayers);
                }
            }
            tree.AfterCheck += new TreeViewEventHandler(tree_AfterCheck);
        }

        private void insertTreeNode(List<ITocElement> elements, ITocElement tocElement, TreeNode parent, List<IDatasetElement> selLayers)
        {
            if (tocElement.ElementType == TocElementType.Layer)
            {
                bool check = false, found = false;
                foreach (IDatasetElement elem in tocElement.Layers)
                {
                    if (!(elem is IFeatureSelection))
                    {
                        continue;
                    } ((IFeatureSelection)elem).FeatureSelectionChanged -= new FeatureSelectionChangedEvent(SelectionEnvironmentControl_FeatureSelectionChanged);
                    ((IFeatureSelection)elem).FeatureSelectionChanged += new FeatureSelectionChangedEvent(SelectionEnvironmentControl_FeatureSelectionChanged);

                    found = true;
                    foreach (IDatasetElement selElem in selLayers)
                    {
                        if (selElem == elem)
                        {
                            check = true;
                            break;
                        }
                    }
                    if (check)
                    {
                        break;
                    }
                }
                if (found)
                {
                    if (parent == null)
                    {
                        tree.Nodes.Add(new SelectionTOCLayerNode(_doc, tocElement, check));
                    }
                    else
                    {
                        parent.Nodes.Add(new SelectionTOCLayerNode(_doc, tocElement, check));
                    }
                }
            }

            if (tocElement.ElementType == TocElementType.OpenedGroup ||
                tocElement.ElementType == TocElementType.ClosedGroup)
            {
                TreeNode p = new TreeNode(tocElement.Name);
                if (parent == null)
                {
                    tree.Nodes.Add(p);
                }
                else
                {
                    parent.Nodes.Add(p);
                }

                foreach (ITocElement elem in elements)
                {
                    if (elem.ParentGroup == tocElement)
                    {
                        insertTreeNode(elements, elem, p, selLayers);
                    }
                }

                if (p.Nodes.Count > 0)
                {
                    foreach (TreeNode node in p.Nodes)
                    {
                        if (node.Checked)
                        {
                            p.Checked = true;
                            break;
                        }
                    }
                    if (tocElement.ElementType == TocElementType.OpenedGroup)
                    {
                        p.Expand();
                    }
                }
                else
                {
                    if (parent == null)
                    {
                        tree.Nodes.Remove(p);
                    }
                    else
                    {
                        parent.Nodes.Remove(p);
                    }
                }
            }
        }

        void SelectionEnvironmentControl_FeatureSelectionChanged(IFeatureSelection sender)
        {
            if (sender == null || sender.SelectionSet == null)
            {
                return;
            }

            SelectionTOCLayerNode node = FindNode(tree.Nodes, sender as ILayer);
            if (node != null)
            {
                node.RefreshText();
            }

            tree.Refresh();
        }

        private SelectionTOCLayerNode FindNode(TreeNodeCollection nodes, ILayer layer)
        {
            if (layer == null)
            {
                return null;
            }

            foreach (TreeNode node in nodes)
            {
                if (!(node is SelectionTOCLayerNode))
                {
                    SelectionTOCLayerNode cnode = FindNode(node.Nodes, layer);
                    if (cnode != null)
                    {
                        return cnode;
                    }
                    else
                    {
                        continue;
                    }
                }

                ITocElement element = ((SelectionTOCLayerNode)node).Element;
                if (element == null || element.Layers == null)
                {
                    continue;
                }

                foreach (ILayer l in element.Layers)
                {
                    if (l == layer)
                    {
                        return node as SelectionTOCLayerNode;
                    }
                }
            }

            return null;
        }

        private void tree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_doc == null)
            {
                return;
            }

            IMap map = _doc.FocusMap;
            if (map == null)
            {
                return;
            }

            ISelectionEnvironment selEnv = map.SelectionEnvironment;
            if (selEnv == null)
            {
                return;
            }

            if (e.Node is SelectionTOCLayerNode)
            {
                foreach (IDatasetElement element in ((SelectionTOCLayerNode)e.Node).Element.Layers)
                {
                    if (!(element is IFeatureSelection))
                    {
                        continue;
                    }

                    if (e.Node.Checked)
                    {
                        selEnv.AddToSelectableElements(element);
                    }
                    else
                    {
                        selEnv.RemoveFromSelectableElements(element);
                    }
                }
                if (_clickNode == e.Node)
                {
                    CheckParentNodes(e.Node);
                }
            }
            else
            {
                CheckChildNodes(e.Node);
                CheckParentNodes(e.Node);
            }
        }

        private void CheckChildNodes(TreeNode parent)
        {
            if (parent == null)
            {
                return;
            }

            foreach (TreeNode node in parent.Nodes)
            {
                node.Checked = parent.Checked;
                //if (node.Nodes.Count > 0) CheckChildNodes(node);
            }
        }

        private void CheckParentNodes(TreeNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Parent == null)
            {
                return;
            }

            bool check = false;
            foreach (TreeNode childs in node.Parent.Nodes)
            {
                if (childs.Checked)
                {
                    check = true;
                    break;
                }
            }

            tree.AfterCheck -= new TreeViewEventHandler(tree_AfterCheck);
            node.Parent.Checked = check;
            tree.AfterCheck += new TreeViewEventHandler(tree_AfterCheck);

            CheckParentNodes(node.Parent);
        }
        #region IDockableWindow Members

        DockWindowState _dockstate = DockWindowState.left;
        public DockWindowState DockableWindowState
        {
            get
            {
                return _dockstate;
            }
            set
            {
                _dockstate = value;
            }
        }
        public Image Image
        {
            get { return imageList1.Images[0]; }
        }
        public new string Name
        {
            get { return LocalizedResources.GetResString("String.Selection", "Selection"); }
            set { }
        }
        #endregion

        SelectionTOCLayerNode _contextNode = null, _clickNode = null;
        private void tree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode node = tree.GetNodeAt(e.X, e.Y);
                if (node is SelectionTOCLayerNode)
                {
                    SelectionTOCLayerNode tocNode = (SelectionTOCLayerNode)node;
                    if (tocNode.Element.Layers.Count == 1)
                    {
                        _contextNode = tocNode;
                        contextMenuStrip1.Show(tree, e.X, e.Y);
                        return;
                    }
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                TreeNode node = tree.GetNodeAt(e.X, e.Y);
                if (node is SelectionTOCLayerNode)
                {
                    _clickNode = (SelectionTOCLayerNode)node;
                    return;
                }
            }
            _contextNode = null;
            _clickNode = null;
        }

        async private void toolStripMenuItemSelectByAttributes_Click(object sender, EventArgs e)
        {
            if (_contextNode == null)
            {
                return;
            }

            if (!(_contextNode.Element.Layers[0] is IFeatureLayer))
            {
                return;
            }

            FormQueryBuilder dlg = await FormQueryBuilder.CreateAsync((IFeatureLayer)_contextNode.Element.Layers[0]);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                QueryFilter filter = new QueryFilter();
                filter.WhereClause = dlg.whereClause;

                await ((IFeatureSelection)_contextNode.Element.Layers[0]).Select(filter, dlg.combinationMethod);
                ((IFeatureSelection)_contextNode.Element.Layers[0]).FireSelectionChangedEvent();

                if (_doc != null)
                {
                    await _doc.FocusMap.RefreshMap(DrawPhase.Selection, null);
                }
            }
        }

        private void toolStripMenuItemClearSelection_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.Application == null)
            {
                return;
            }

            if (!(_contextNode is SelectionTOCLayerNode))
            {
                return;
            }

            ITocElement element = ((SelectionTOCLayerNode)_contextNode).Element;
            if (element == null || element.Layers == null)
            {
                return;
            }

            foreach (ILayer layer in element.Layers)
            {
                if (!(layer is IFeatureSelection))
                {
                    continue;
                } ((IFeatureSelection)layer).ClearSelection();
                ((IFeatureSelection)layer).FireSelectionChangedEvent();
            }

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Selection | DrawPhase.Graphics);
            }
        }

        async private void toolStripMenuItemZoomToSelection_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.Application == null)
            {
                return;
            }

            if (!(_contextNode is SelectionTOCLayerNode))
            {
                return;
            }

            ITocElement element = ((SelectionTOCLayerNode)_contextNode).Element;
            if (element == null || element.Layers == null)
            {
                return;
            }

            gView.Framework.Geometry.Envelope env = null;

            foreach (ILayer layer in element.Layers)
            {
                if (!(layer is IFeatureSelection) ||
                    !(layer is IFeatureLayer) ||
                    ((IFeatureLayer)layer).FeatureClass == null ||
                    ((IFeatureSelection)layer).SelectionSet == null ||
                    ((IFeatureSelection)layer).SelectionSet.Count == 0)
                {
                    continue;
                }

                IFeatureClass fc = ((IFeatureLayer)layer).FeatureClass;
                if (fc == null)
                {
                    continue;
                }

                IQueryFilter filter = null;
                if (((IFeatureSelection)layer).SelectionSet is IIDSelectionSet)
                {
                    List<int> IDs = ((IIDSelectionSet)((IFeatureSelection)layer).SelectionSet).IDs;
                    filter = new RowIDFilter(fc.IDFieldName, IDs);
                }
                else if (((IFeatureSelection)layer).SelectionSet is IIDSelectionSet)
                {
                    filter = ((IQueryFilteredSelectionSet)((IFeatureSelection)layer).SelectionSet).QueryFilter;
                }
                if (filter == null)
                {
                    return;
                }

                bool project = false;
                if (fc.SpatialReference != null && !fc.SpatialReference.Equals(_doc.FocusMap.Display.SpatialReference))
                {
                    project = true;
                }

                filter.AddField(fc.ShapeFieldName);

                using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                {
                    if (cursor == null)
                    {
                        continue;
                    }

                    IFeature feat;
                    while ((feat = await cursor.NextFeature()) != null)
                    {
                        if (feat.Shape == null)
                        {
                            continue;
                        }

                        IEnvelope envelope = feat.Shape.Envelope;
                        if (project)
                        {
                            IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, fc.SpatialReference, _doc.FocusMap.Display.SpatialReference);
                            if (geom == null)
                            {
                                continue;
                            }

                            envelope = geom.Envelope;
                        }
                        if (env == null)
                        {
                            env = new gView.Framework.Geometry.Envelope(envelope);
                        }
                        else
                        {
                            env.Union(envelope);
                        }
                    }
                    cursor.Dispose();
                }
            }

            if (env != null)
            {
                env.Raise(110.0);
                _doc.FocusMap.Display.ZoomTo(env);
                if (_doc.Application is IMapApplication)
                {
                    await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
                }
            }
        }

        #region Buttons - Category
        private void btnCategories_Click(object sender, EventArgs e)
        {
            btnCategories.Checked = true;
            btnAlphabetic.Checked = false;
            viewState = ViewState.categories;

            buildTree();
        }

        private void btnAlphabetic_Click(object sender, EventArgs e)
        {
            btnAlphabetic.Checked = true;
            btnCategories.Checked = false;
            viewState = ViewState.alphabetic;

            buildTree();
        }
        #endregion

        #region Buttons - Methode
        private void btnMethodeRectangle_Click(object sender, EventArgs e)
        {
            this.SelectionMethode = selectionMothode.Rectangle;
        }

        private void btnMethodeMultipoint_Click(object sender, EventArgs e)
        {
            this.SelectionMethode = selectionMothode.Multipoint;
        }

        private void btnMethodePolyline_Click(object sender, EventArgs e)
        {
            this.SelectionMethode = selectionMothode.Polyline;
        }

        private void btnMethodePolygon_Click(object sender, EventArgs e)
        {
            this.SelectionMethode = selectionMothode.Polygon;
        }
        #endregion

        private void btnApplySelection_Click(object sender, EventArgs e)
        {
            if (_tool == null || _doc == null || _doc.Application == null)
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            if (_tool.SelectByGeometry(_doc.FocusMap, _tool.SelectionGeometry))
            {
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Selection);
                }
            }
            Cursor = Cursors.Default;
        }

        private void btnClearSelectionFigure_Click(object sender, EventArgs e)
        {
            if (_tool == null)
            {
                return;
            }

            _tool.ClearSelectionFigure();
        }

        #region Buttons - SpatialRelation
        private void btnIntersects_Click(object sender, EventArgs e)
        {
            SpatialRelation = spatialRelation.SpatialRelationIntersects;
        }

        private void btnContains_Click(object sender, EventArgs e)
        {
            SpatialRelation = spatialRelation.SpatialRelationContains;
        }

        private void btnWithin_Click(object sender, EventArgs e)
        {
            SpatialRelation = spatialRelation.SpatialRelationWithin;
        }
        #endregion

        #region SelectByLocation
        private void btnCloseSelByLocation_Click(object sender, EventArgs e)
        {
            panelSelByLoc.Visible = false;
        }
        private void btnSelByLocation_Click(object sender, EventArgs e)
        {
            panelSelByLoc.Visible = true;
            RefreshSelByLocationGUI();
        }

        private void RefreshSelByLocationGUI()
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.SelectionEnvironment == null)
            {
                return;
            }

            IFeatureLayer layer = (cmbSelByLocLayer.SelectedItem is FeatureLayerItem) ? (cmbSelByLocLayer.SelectedItem as FeatureLayerItem).FeatureLayer : null;
            cmbSelByLocLayer.Items.Clear();

            foreach (IDatasetElement element in _doc.FocusMap.MapElements)
            {
                ITocElement tocElement = null;
                if (element is IFeatureLayer && ((IFeatureLayer)element).FeatureClass != null)
                {
                    tocElement = _doc.FocusMap.TOC.GetTOCElement(element as ILayer);
                    if (tocElement == null)
                    {
                        return;
                    }

                    cmbSelByLocLayer.Items.Add(new FeatureLayerItem(element as IFeatureLayer, tocElement.Name));
                    if (element == layer)
                    {
                        cmbSelByLocLayer.SelectedIndex = cmbSelByLocLayer.Items.Count - 1;
                    }
                }
                else if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null)
                {
                    IWebServiceClass wsClass = ((IWebServiceLayer)element).WebServiceClass;

                    foreach (IWebServiceTheme theme in wsClass.Themes)
                    {
                        if (theme.FeatureClass == null)
                        {
                            continue;
                        }

                        tocElement = _doc.FocusMap.TOC.GetTOCElement(theme as ILayer);
                        if (tocElement == null)
                        {
                            return;
                        }

                        cmbSelByLocLayer.Items.Add(new FeatureLayerItem(theme as IFeatureLayer, tocElement.Name));
                        if (theme == layer)
                        {
                            cmbSelByLocLayer.SelectedIndex = cmbSelByLocLayer.Items.Count - 1;
                        }
                    }
                }
            }

            if (cmbSelByLocLayer.SelectedItem == null)
            {
                foreach (FeatureLayerItem item in cmbSelByLocLayer.Items)
                {
                    if (item.FeatureLayer is IFeatureSelection &&
                        ((IFeatureSelection)item.FeatureLayer).SelectionSet != null &&
                        ((IFeatureSelection)item.FeatureLayer).SelectionSet.Count > 0)
                    {
                        cmbSelByLocLayer.SelectedItem = item;
                        break;
                    }
                }
                if (cmbSelByLocLayer.SelectedIndex == -1 && cmbSelByLocLayer.Items.Count > 0)
                {
                    cmbSelByLocLayer.SelectedIndex = 0;
                }
            }

            if (cmbSelByLoc_BufferUnit.Items.Count == 0)
            {
                foreach (GeoUnits unit in Enum.GetValues(typeof(GeoUnits)))
                {
                    cmbSelByLoc_BufferUnit.Items.Add(unit);
                }
                cmbSelByLoc_BufferUnit.SelectedItem = _doc.FocusMap.Display.DisplayUnits;
            }
        }

        private void cmbSelByLocLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            FeatureLayerItem item = cmbSelByLocLayer.SelectedItem as FeatureLayerItem;
            if (item == null || item.FeatureLayer == null)
            {
                return;
            }

            if (item.FeatureLayer is IFeatureSelection &&
                ((IFeatureSelection)item.FeatureLayer).SelectionSet != null &&
                ((IFeatureSelection)item.FeatureLayer).SelectionSet.Count > 0)
            {
                chkSelByLoc_UseSelected.Enabled = true;
                chkSelByLoc_UseSelected.Text = _selectedString.Replace("0", ((IFeatureSelection)item.FeatureLayer).SelectionSet.Count.ToString());
            }
            else
            {
                chkSelByLoc_UseSelected.Enabled =
                    chkSelByLoc_AppendBuffer.Enabled =
                    chkSelByLoc_ApplyBuffer.Enabled = false;
                chkSelByLoc_UseSelected.Text = _selectedString;
            }
        }


        private void chkSelByLoc_UseSelected_CheckedChanged(object sender, EventArgs e)
        {
            chkSelByLoc_ApplyBuffer.Enabled =
                chkSelByLoc_AppendBuffer.Enabled = chkSelByLoc_UseSelected.Checked;
        }

        private void chkSelByLoc_ApplyBuffer_CheckedChanged(object sender, EventArgs e)
        {
            chkSelByLoc_AppendBuffer.Enabled = chkSelByLoc_ApplyBuffer.Checked;
        }

        async private void btnApplySelByLocation_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null)
            {
                return;
            }

            FeatureLayerItem item = cmbSelByLocLayer.SelectedItem as FeatureLayerItem;
            if (item == null || item.FeatureLayer == null)
            {
                return;
            }

            IFeatureLayer layer = item.FeatureLayer;
            if (layer.FeatureClass == null)
            {
                return;
            }

            IFeatureCursor cursor = null;
            try
            {
                ISelectionSet selSet = ((IFeatureSelection)item.FeatureLayer).SelectionSet;

                if (chkSelByLoc_UseSelected.Checked)
                {
                    IQueryFilter filter = null;
                    if (selSet is IIDSelectionSet)
                    {
                        filter = new RowIDFilter(layer.FeatureClass.IDFieldName, ((IIDSelectionSet)selSet).IDs);
                    }
                    else if (selSet is IQueryFilteredSelectionSet)
                    {
                        filter = ((IQueryFilteredSelectionSet)selSet).QueryFilter.Clone() as IQueryFilter;
                    }
                    if (filter == null)
                    {
                        MessageBox.Show("Can't perform selection !!!");
                        return;
                    }
                    filter.AddField(layer.FeatureClass.ShapeFieldName);
                    filter.FeatureSpatialReference = _doc.FocusMap.Display.SpatialReference;
                    cursor = await layer.FeatureClass.GetFeatures(filter);
                }
                else
                {
                    QueryFilter filter = new QueryFilter();
                    filter.AddField(layer.FeatureClass.ShapeFieldName);
                    filter.FeatureSpatialReference = _doc.FocusMap.Display.SpatialReference;
                    cursor = await layer.FeatureClass.GetFeatures(filter);
                }

                AggregateGeometry aGeometry = new AggregateGeometry();
                IFeature feature;
                while ((feature = await cursor.NextFeature()) != null)
                {
                    if (feature.Shape == null)
                    {
                        continue;
                    }

                    aGeometry.AddGeometry(feature.Shape);
                }

                IGeometry geometry = aGeometry;
                double distance = (double)numSelByLoc_BufferDistance.Value;
                GeoUnitConverter converter = new GeoUnitConverter();
                distance = converter.Convert(distance,
                    (GeoUnits)cmbSelByLoc_BufferUnit.SelectedItem,
                    _doc.FocusMap.Display.DisplayUnits);

                if (geometry is ITopologicalOperation &&
                    chkSelByLoc_ApplyBuffer.Enabled &&
                    chkSelByLoc_ApplyBuffer.Checked)
                {
                    geometry = ((ITopologicalOperation)geometry).Buffer(distance);

                    if (chkSelByLoc_AppendBuffer.Enabled &&
                        chkSelByLoc_AppendBuffer.Checked)
                    {
                        _doc.FocusMap.Display.GraphicsContainer.Elements.Add(new gView.Plugins.MapTools.Graphics.GraphicPolygon(geometry as IPolygon));
                    }
                }
                _tool.SelectByGeometry(_doc.FocusMap, geometry);

                if (_doc.Application is IMapApplication)
                {
                    await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics | DrawPhase.Selection);
                }
            }
            finally
            {
                if (cursor != null)
                {
                    cursor.Dispose();
                }
            }
        }

        internal class FeatureLayerItem
        {
            private IFeatureLayer _layer;
            private string _alias = "";

            public FeatureLayerItem(IFeatureLayer layer, string alias)
            {
                _layer = layer;
                _alias = alias;
            }
            public IFeatureLayer FeatureLayer
            {
                get { return _layer; }
            }
            public override string ToString()
            {
                return _alias;
            }
        }
        #endregion

        #region Aggregate
        private void AggregateGeometry(IGeometry geom, IGeometry aggregate)
        {
            if (aggregate == null || geom == null)
            {
                return;
            }

            if (aggregate is IPolygon && geom is IPolygon)
            {
                IPolygon a = aggregate as IPolygon;
                IPolygon g = geom as IPolygon;

                for (int i = 0; i < g.RingCount; i++)
                {
                }
            }
        }
        #endregion

        #region SelectByGraphics
        private void btnSelByGraphic_Click(object sender, EventArgs e)
        {
            if (_tool == null || _doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            AggregateGeometry aGeometry = new AggregateGeometry();
            foreach (IGraphicElement grElement in _doc.FocusMap.Display.GraphicsContainer.SelectedElements)
            {
                if (grElement is IGraphicElement2)
                {
                    IGeometry geom = ((IGraphicElement2)grElement).Geometry;
                    if (geom == null)
                    {
                        continue;
                    }

                    aGeometry.AddGeometry(geom);
                }
            }
            if (aGeometry.GeometryCount == 0)
            {
                MessageBox.Show("No selected graphic elements found!");
                return;
            }

            _tool.SelectByGeometry(_doc.FocusMap, aGeometry);
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics | DrawPhase.Selection);
            }
        }
        #endregion

        private void btnCombination_Click(object sender, EventArgs e)
        {
            if (_tool == null)
            {
                return;
            }

            if (sender == btnCombinationNew)
            {
                this.CombinationMethode = CombinationMethod.New;
            }
            else if (sender == btnCombinationUnion)
            {
                this.CombinationMethode = CombinationMethod.Union;
            }
            else if (sender == btnCombinationIntersection)
            {
                this.CombinationMethode = CombinationMethod.Intersection;
            }
            else if (sender == btnCombinationDifference)
            {
                this.CombinationMethode = CombinationMethod.Difference;
            }
            else if (sender == btnCombinationSymDifference)
            {
                this.CombinationMethode = CombinationMethod.SymDifference;
            }
        }


        #region Helper

        #endregion
    }

    internal class DatasetElementComparer : IComparer<IDatasetElement>
    {
        #region IComparer<IDatasetElement> Members

        public int Compare(IDatasetElement x, IDatasetElement y)
        {
            return x.Title.CompareTo(y.Title);
        }

        #endregion
    }

    internal class TOCElementComparer : IComparer<ITocElement>
    {

        #region IComparer<ITOCElement> Members

        public int Compare(ITocElement x, ITocElement y)
        {
            return x.Name.CompareTo(y.Name);
        }

        #endregion
    }

    //internal class BufferGraphics : gView.Framework.Symbology.GraphicShape
    //{
    //    public BufferGraphics(IPolygon polygon)
    //    {
    //        this.Template = polygon;

    //        SimpleLineSymbol _lineSymbol;
    //        HatchSymbol _fillSymbol;
    //        _lineSymbol = new SimpleLineSymbol();
    //        _lineSymbol.Color = System.Drawing.Color.Blue;
    //        _lineSymbol.Width = 2;
    //        _fillSymbol = new HatchSymbol();
    //        _fillSymbol.OutlineSymbol = _lineSymbol;

    //        this.Symbol = _fillSymbol;
    //    }
    //}
}
