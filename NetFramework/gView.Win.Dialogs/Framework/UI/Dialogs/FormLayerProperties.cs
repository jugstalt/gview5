using gView.Framework.Carto;
using gView.Framework.Carto.Rendering;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs
{
    /// <summary>
    /// Zusammenfassung für FormLayerProperties.
    /// </summary>
    public class FormLayerProperties : System.Windows.Forms.Form
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private IDataset _dataset;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabPage tabRenderer;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panelRendererPropPage;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.GroupBox GroupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton radioAllScales;
        private System.Windows.Forms.RadioButton radioScales;
        private System.Windows.Forms.TabPage tabSR;
        private ILayer _layer;
        private TabPage tabLabelling;
        private GroupBox groupBox2;
        private CheckBox chkLabelLayer;
        private ComboBox cmbLabelRenderer;
        private Label label3;
        private IFeatureRenderer _renderer;
        private IFeatureRenderer _selRenderer;
        private Panel panelLabelRendererPage;
        private TabPage tabRaster;
        private ComboBox cmbInterpolationMode;
        private Label label4;
        private TrackBar tbTransparency;
        private Label label5;
        private Label lblTransPercent;
        private Button btnTranscolor;
        private TabPage tabWebServiceLayer;
        private DataGridView gvWebThemes;
        private DataGridViewCheckBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private TabPage tabFields;
        private DataGridView dgFields;
        private Panel panel3;
        private GroupBox groupBox3;
        private ComboBox cmbPrimaryField;
        private Label label6;
        private Panel panel4;
        private Button btnSelectAll;
        private Button btnFieldsMoveDown;
        private Button btnFieldsMoveUp;
        private Button btnClearAll;
        private GroupBox groupBox5;
        private GroupBox groupBox4;
        private RadioButton radioLabelScales;
        private RadioButton radioLabelAllScales;
        private Label label7;
        private Label label8;
        private Label label9;
        private NumericUpDown txtLabelMaxScale;
        private NumericUpDown txtLabelMinScale;
        private NumericUpDown txtMaxScale;
        private NumericUpDown txtMinScale;
        private NumericUpDown txtMaximunZoom2FeatureScale;
        private ILabelRenderer _labelRenderer;
        private TabPage tabSelRenderer;
        private TreeView tvRenderer;
        private Panel panelSelRendererPropPage;
        private Splitter splitter2;
        private TreeView tvSelRenderer;
        private Splitter splitter1;
        private Panel panel5;
        private CheckBox chkRenderLayer;
        private IGroupLayer _groupLayer = null;
        private Button btnCancelNoData;
        private Label label10;
        private Panel panelNoDataColor;
        private Panel panelNoDataValue;
        private NumericUpDown numNoDataValue;
        private CheckBox chkNoDataValue;
        private DataGridViewCheckBoxColumn fieldCol1;
        private DataGridViewTextBoxColumn fieldCol2;
        private DataGridViewTextBoxColumn fieldCol4;
        private DataGridViewTextBoxColumn fieldCol3;
        private DataGridViewTextBoxColumn colOrder;
        private GroupBox groupBox6;
        private Label label11;
        private ComboBox cmbGeometryType;
        private TabPage tabDescription;
        private TextBox txtCopyright;
        private Label label12;
        private TextBox txtDescription;
        private Label label13;
        private List<ILayerPropertyPage> propertyPages = new List<ILayerPropertyPage>();

        private IMapDocument _mapDocument;
        private ComboBox cmbApplyRefScale;
        private ComboBox cmbApplyLabelRefScale;
        private TabPage tabMapService;
        private ComboBox cmbMapServerGrouplayerStyle;
        private GroupBox groupBox7;
        private Label label16;
        private Label label15;
        private Label label14;
        private Label label18;
        private Label label17;
        private TabPage tabCompostionMode;
        private ComboBox cmbCompositionMode;
        private GroupBox groupBox8;
        private Panel panelCompModeCopy;
        private Label label20;
        private Label label19;
        private NumericUpDown numCompModeCopyTransparency;
        private Label label22;
        private Label label21;
        private Label label23;
        private PictureBox pictureBox2;
        private PictureBox pictureBox1;
        private Label label24;
        private ComboBox cmbFilter;
        private Label label25;
        private IMap _map;

        public FormLayerProperties(IMapDocument mapDocument, IMap map, IDataset dataset, ILayer layer)
        {
            _mapDocument = mapDocument;
            _dataset = dataset;
            _layer = layer;
            _map = map;

            if (_layer == null)
            {
                return;
            }

            if (_layer is Layer)
            {
                _groupLayer = _layer.GroupLayer;
                ((Layer)_layer).GroupLayer = null;
            }

            //this.Text = "Layer Properties";

            InitializeComponent();

            this.Text += ": " + layer.Title;

            cmbApplyRefScale.Items.Add(new ReferenceScaleItem(-1f, "Do not apply reference scale"));
            cmbApplyRefScale.Items.Add(new ReferenceScaleItem(0f, "Apply reference scale"));
            for (int i = 0; i <= 200; i += 10)
            {
                cmbApplyRefScale.Items.Add(new ReferenceScaleItem((float)(i + 100) / 100f, $"Apply reference scale (max scale up {i}%)"));
            }

            cmbApplyLabelRefScale.Items.Add(new ReferenceScaleItem(-1f, "Do not apply reference scale for labels"));
            cmbApplyLabelRefScale.Items.Add(new ReferenceScaleItem(0f, "Apply reference scale for labels"));
            for (int i = 0; i <= 200; i += 10)
            {
                cmbApplyLabelRefScale.Items.Add(new ReferenceScaleItem((float)(i + 100) / 100f, $"Apply reference scale for labels (max scale up {i}%)"));
            }

            foreach (MapServerGrouplayerStyle grouplayerStyle in Enum.GetValues(typeof(MapServerGrouplayerStyle)))
            {
                cmbMapServerGrouplayerStyle.Items.Add(grouplayerStyle);
            }

            tabControl1.TabPages.Remove(tabRaster);
            tabControl1.TabPages.Remove(tabRenderer);
            tabControl1.TabPages.Remove(tabSelRenderer);
            tabControl1.TabPages.Remove(tabLabelling);
            tabControl1.TabPages.Remove(tabWebServiceLayer);
            tabControl1.TabPages.Remove(tabFields);
            tabControl1.TabPages.Remove(tabSR);
            tabControl1.TabPages.Remove(tabDescription);
            tabControl1.TabPages.Remove(tabMapService);
            tabControl1.TabPages.Remove(tabCompostionMode);

            if (layer is IFeatureLayer)
            {
                if (layer.Class is IFeatureClass)
                {
                    if (!(layer is IRasterCatalogLayer))
                    {
                        tabControl1.TabPages.Add(tabRenderer);
                    }

                    tabControl1.TabPages.Add(tabSelRenderer);
                    tabControl1.TabPages.Add(tabLabelling);
                    tabControl1.TabPages.Add(tabFields);
                }
                else if (layer is IWebServiceTheme)
                {
                    tabControl1.TabPages.Add(tabRenderer);
                }

                tabControl1.TabPages.Add(tabSR);

                if (((IFeatureLayer)layer).FeatureRenderer != null)
                {
                    _renderer = (IFeatureRenderer)((IFeatureLayer)layer).FeatureRenderer.Clone();
                }
                if (((IFeatureLayer)layer).SelectionRenderer != null)
                {
                    _selRenderer = (IFeatureRenderer)((IFeatureLayer)layer).SelectionRenderer.Clone();
                }
                if (((IFeatureLayer)layer).LabelRenderer != null)
                {
                    _labelRenderer = (ILabelRenderer)((IFeatureLayer)layer).LabelRenderer.Clone();
                    chkLabelLayer.Checked = true;
                }
                else
                {
                    _labelRenderer = new SimpleLabelRenderer();
                }
            }
            if (layer is IGroupLayer)
            {
                if (!tabControl1.TabPages.Contains(tabMapService))
                {
                    tabControl1.TabPages.Add(tabMapService);
                }

                cmbMapServerGrouplayerStyle.SelectedItem = ((IGroupLayer)layer).MapServerStyle;
            }
            if (layer is IRasterLayer)
            {
                if (!tabControl1.TabPages.Contains(tabRaster))
                {
                    tabControl1.TabPages.Add(tabRaster);
                }

                if (!tabControl1.TabPages.Contains(tabSR))
                {
                    tabControl1.TabPages.Add(tabSR);
                }
            }
            if (layer is IWebServiceLayer)
            {
                if (!tabControl1.TabPages.Contains(tabWebServiceLayer))
                {
                    tabControl1.TabPages.Add(tabWebServiceLayer);
                }

                if (!tabControl1.TabPages.Contains(tabSR))
                {
                    tabControl1.TabPages.Add(tabSR);
                }
            }
            if (layer is IFeatureLayerComposition)
            {
                tabControl1.TabPages.Add(tabCompostionMode);

                foreach (FeatureLayerCompositionMode compostionMode in Enum.GetValues(typeof(FeatureLayerCompositionMode)))
                {
                    cmbCompositionMode.Items.Add(new CompositionModeItem(compostionMode));

                    if (((IFeatureLayerComposition)layer).CompositionMode == compostionMode)
                    {
                        cmbCompositionMode.SelectedIndex = cmbCompositionMode.Items.Count - 1;
                    }
                }

                numCompModeCopyTransparency.Value = (decimal)((IFeatureLayerComposition)layer).CompositionModeCopyTransparency;
                panelCompModeCopy.Visible = ((IFeatureLayerComposition)layer).CompositionMode == FeatureLayerCompositionMode.Copy;
            }
            if (_map != null)
            {
                tabControl1.TabPages.Add(tabDescription);
                txtDescription.Text = _map.GetLayerDescription(layer.ID);
                txtCopyright.Text = _map.GetLayerCopyrightText(layer.ID);
            }

            PlugInManager compMan = new PlugInManager();
            foreach (var compType in compMan.GetPlugins(Plugins.Type.ILayerPropertyPage))
            {
                ILayerPropertyPage page = compMan.CreateInstance<ILayerPropertyPage>(compType);
                if (page == null || !page.ShowWith(dataset, layer))
                {
                    continue;
                }

                Panel panel = page.PropertyPage(dataset, layer);
                if (panel == null)
                {
                    continue;
                }

                panel.Dock = DockStyle.Fill;
                propertyPages.Add(page);
                TabPage tpage = new TabPage();
                tpage.Text = page.Title;
                tpage.Controls.Add(panel);
                tabControl1.TabPages.Add(tpage);
            }


        }

        /// <summary>
        /// Die verwendeten Ressourcen bereinigen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _dataset = null;
            _layer = null;

            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code
        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLayerProperties));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.cmbGeometryType = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.txtMaximunZoom2FeatureScale = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.txtLabelMaxScale = new System.Windows.Forms.NumericUpDown();
            this.txtLabelMinScale = new System.Windows.Forms.NumericUpDown();
            this.radioLabelAllScales = new System.Windows.Forms.RadioButton();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.radioLabelScales = new System.Windows.Forms.RadioButton();
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.radioScales = new System.Windows.Forms.RadioButton();
            this.txtMaxScale = new System.Windows.Forms.NumericUpDown();
            this.txtMinScale = new System.Windows.Forms.NumericUpDown();
            this.radioAllScales = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tabRenderer = new System.Windows.Forms.TabPage();
            this.panelRendererPropPage = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.tvRenderer = new System.Windows.Forms.TreeView();
            this.panel5 = new System.Windows.Forms.Panel();
            this.cmbApplyRefScale = new System.Windows.Forms.ComboBox();
            this.chkRenderLayer = new System.Windows.Forms.CheckBox();
            this.tabSelRenderer = new System.Windows.Forms.TabPage();
            this.panelSelRendererPropPage = new System.Windows.Forms.Panel();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.tvSelRenderer = new System.Windows.Forms.TreeView();
            this.tabLabelling = new System.Windows.Forms.TabPage();
            this.panelLabelRendererPage = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cmbApplyLabelRefScale = new System.Windows.Forms.ComboBox();
            this.chkLabelLayer = new System.Windows.Forms.CheckBox();
            this.cmbLabelRenderer = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tabRaster = new System.Windows.Forms.TabPage();
            this.cmbFilter = new System.Windows.Forms.ComboBox();
            this.label25 = new System.Windows.Forms.Label();
            this.panelNoDataColor = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.btnTranscolor = new System.Windows.Forms.Button();
            this.btnCancelNoData = new System.Windows.Forms.Button();
            this.panelNoDataValue = new System.Windows.Forms.Panel();
            this.numNoDataValue = new System.Windows.Forms.NumericUpDown();
            this.chkNoDataValue = new System.Windows.Forms.CheckBox();
            this.lblTransPercent = new System.Windows.Forms.Label();
            this.tbTransparency = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbInterpolationMode = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tabWebServiceLayer = new System.Windows.Forms.TabPage();
            this.gvWebThemes = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabFields = new System.Windows.Forms.TabPage();
            this.dgFields = new System.Windows.Forms.DataGridView();
            this.fieldCol1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.fieldCol2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldCol4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fieldCol3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btnFieldsMoveDown = new System.Windows.Forms.Button();
            this.btnFieldsMoveUp = new System.Windows.Forms.Button();
            this.btnClearAll = new System.Windows.Forms.Button();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cmbPrimaryField = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabSR = new System.Windows.Forms.TabPage();
            this.tabDescription = new System.Windows.Forms.TabPage();
            this.txtCopyright = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tabMapService = new System.Windows.Forms.TabPage();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.cmbMapServerGrouplayerStyle = new System.Windows.Forms.ComboBox();
            this.tabCompostionMode = new System.Windows.Forms.TabPage();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.panelCompModeCopy = new System.Windows.Forms.Panel();
            this.label24 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.numCompModeCopyTransparency = new System.Windows.Forms.NumericUpDown();
            this.cmbCompositionMode = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtMaximunZoom2FeatureScale)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtLabelMaxScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtLabelMinScale)).BeginInit();
            this.GroupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtMaxScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinScale)).BeginInit();
            this.tabRenderer.SuspendLayout();
            this.panel5.SuspendLayout();
            this.tabSelRenderer.SuspendLayout();
            this.tabLabelling.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabRaster.SuspendLayout();
            this.panelNoDataColor.SuspendLayout();
            this.panelNoDataValue.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNoDataValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbTransparency)).BeginInit();
            this.tabWebServiceLayer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gvWebThemes)).BeginInit();
            this.tabFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).BeginInit();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabDescription.SuspendLayout();
            this.tabMapService.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.tabCompostionMode.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.panelCompModeCopy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCompModeCopyTransparency)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabGeneral);
            this.tabControl1.Controls.Add(this.tabRenderer);
            this.tabControl1.Controls.Add(this.tabSelRenderer);
            this.tabControl1.Controls.Add(this.tabLabelling);
            this.tabControl1.Controls.Add(this.tabRaster);
            this.tabControl1.Controls.Add(this.tabWebServiceLayer);
            this.tabControl1.Controls.Add(this.tabFields);
            this.tabControl1.Controls.Add(this.tabSR);
            this.tabControl1.Controls.Add(this.tabDescription);
            this.tabControl1.Controls.Add(this.tabMapService);
            this.tabControl1.Controls.Add(this.tabCompostionMode);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.groupBox6);
            this.tabGeneral.Controls.Add(this.groupBox5);
            this.tabGeneral.Controls.Add(this.groupBox4);
            this.tabGeneral.Controls.Add(this.GroupBox1);
            resources.ApplyResources(this.tabGeneral, "tabGeneral");
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.cmbGeometryType);
            this.groupBox6.Controls.Add(this.label11);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // cmbGeometryType
            // 
            this.cmbGeometryType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGeometryType.FormattingEnabled = true;
            resources.ApplyResources(this.cmbGeometryType, "cmbGeometryType");
            this.cmbGeometryType.Name = "cmbGeometryType";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.txtMaximunZoom2FeatureScale);
            this.groupBox5.Controls.Add(this.label9);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // txtMaximunZoom2FeatureScale
            // 
            resources.ApplyResources(this.txtMaximunZoom2FeatureScale, "txtMaximunZoom2FeatureScale");
            this.txtMaximunZoom2FeatureScale.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.txtMaximunZoom2FeatureScale.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.txtMaximunZoom2FeatureScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtMaximunZoom2FeatureScale.Name = "txtMaximunZoom2FeatureScale";
            this.txtMaximunZoom2FeatureScale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.txtLabelMaxScale);
            this.groupBox4.Controls.Add(this.txtLabelMinScale);
            this.groupBox4.Controls.Add(this.radioLabelAllScales);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.radioLabelScales);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // txtLabelMaxScale
            // 
            resources.ApplyResources(this.txtLabelMaxScale, "txtLabelMaxScale");
            this.txtLabelMaxScale.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtLabelMaxScale.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.txtLabelMaxScale.Name = "txtLabelMaxScale";
            // 
            // txtLabelMinScale
            // 
            resources.ApplyResources(this.txtLabelMinScale, "txtLabelMinScale");
            this.txtLabelMinScale.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtLabelMinScale.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.txtLabelMinScale.Name = "txtLabelMinScale";
            // 
            // radioLabelAllScales
            // 
            resources.ApplyResources(this.radioLabelAllScales, "radioLabelAllScales");
            this.radioLabelAllScales.Name = "radioLabelAllScales";
            this.radioLabelAllScales.CheckedChanged += new System.EventHandler(this.radioScales_CheckedChanged);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // radioLabelScales
            // 
            resources.ApplyResources(this.radioLabelScales, "radioLabelScales");
            this.radioLabelScales.Name = "radioLabelScales";
            this.radioLabelScales.CheckedChanged += new System.EventHandler(this.radioScales_CheckedChanged);
            // 
            // GroupBox1
            // 
            resources.ApplyResources(this.GroupBox1, "GroupBox1");
            this.GroupBox1.Controls.Add(this.radioScales);
            this.GroupBox1.Controls.Add(this.txtMaxScale);
            this.GroupBox1.Controls.Add(this.txtMinScale);
            this.GroupBox1.Controls.Add(this.radioAllScales);
            this.GroupBox1.Controls.Add(this.label2);
            this.GroupBox1.Controls.Add(this.label1);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.TabStop = false;
            // 
            // radioScales
            // 
            resources.ApplyResources(this.radioScales, "radioScales");
            this.radioScales.Name = "radioScales";
            this.radioScales.CheckedChanged += new System.EventHandler(this.radioScales_CheckedChanged);
            // 
            // txtMaxScale
            // 
            resources.ApplyResources(this.txtMaxScale, "txtMaxScale");
            this.txtMaxScale.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtMaxScale.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.txtMaxScale.Name = "txtMaxScale";
            // 
            // txtMinScale
            // 
            resources.ApplyResources(this.txtMinScale, "txtMinScale");
            this.txtMinScale.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtMinScale.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.txtMinScale.Name = "txtMinScale";
            // 
            // radioAllScales
            // 
            resources.ApplyResources(this.radioAllScales, "radioAllScales");
            this.radioAllScales.Name = "radioAllScales";
            this.radioAllScales.CheckedChanged += new System.EventHandler(this.radioScales_CheckedChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tabRenderer
            // 
            this.tabRenderer.Controls.Add(this.panelRendererPropPage);
            this.tabRenderer.Controls.Add(this.splitter1);
            this.tabRenderer.Controls.Add(this.tvRenderer);
            this.tabRenderer.Controls.Add(this.panel5);
            resources.ApplyResources(this.tabRenderer, "tabRenderer");
            this.tabRenderer.Name = "tabRenderer";
            this.tabRenderer.UseVisualStyleBackColor = true;
            // 
            // panelRendererPropPage
            // 
            resources.ApplyResources(this.panelRendererPropPage, "panelRendererPropPage");
            this.panelRendererPropPage.Name = "panelRendererPropPage";
            // 
            // splitter1
            // 
            resources.ApplyResources(this.splitter1, "splitter1");
            this.splitter1.Name = "splitter1";
            this.splitter1.TabStop = false;
            // 
            // tvRenderer
            // 
            this.tvRenderer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.tvRenderer, "tvRenderer");
            this.tvRenderer.FullRowSelect = true;
            this.tvRenderer.HideSelection = false;
            this.tvRenderer.Name = "tvRenderer";
            this.tvRenderer.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvRenderer_AfterSelect);
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.cmbApplyRefScale);
            this.panel5.Controls.Add(this.chkRenderLayer);
            resources.ApplyResources(this.panel5, "panel5");
            this.panel5.Name = "panel5";
            // 
            // cmbApplyRefScale
            // 
            resources.ApplyResources(this.cmbApplyRefScale, "cmbApplyRefScale");
            this.cmbApplyRefScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbApplyRefScale.FormattingEnabled = true;
            this.cmbApplyRefScale.Name = "cmbApplyRefScale";
            // 
            // chkRenderLayer
            // 
            resources.ApplyResources(this.chkRenderLayer, "chkRenderLayer");
            this.chkRenderLayer.Name = "chkRenderLayer";
            this.chkRenderLayer.UseVisualStyleBackColor = true;
            // 
            // tabSelRenderer
            // 
            this.tabSelRenderer.Controls.Add(this.panelSelRendererPropPage);
            this.tabSelRenderer.Controls.Add(this.splitter2);
            this.tabSelRenderer.Controls.Add(this.tvSelRenderer);
            resources.ApplyResources(this.tabSelRenderer, "tabSelRenderer");
            this.tabSelRenderer.Name = "tabSelRenderer";
            this.tabSelRenderer.UseVisualStyleBackColor = true;
            // 
            // panelSelRendererPropPage
            // 
            resources.ApplyResources(this.panelSelRendererPropPage, "panelSelRendererPropPage");
            this.panelSelRendererPropPage.Name = "panelSelRendererPropPage";
            // 
            // splitter2
            // 
            this.splitter2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.splitter2, "splitter2");
            this.splitter2.Name = "splitter2";
            this.splitter2.TabStop = false;
            // 
            // tvSelRenderer
            // 
            this.tvSelRenderer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.tvSelRenderer, "tvSelRenderer");
            this.tvSelRenderer.FullRowSelect = true;
            this.tvSelRenderer.HideSelection = false;
            this.tvSelRenderer.Name = "tvSelRenderer";
            this.tvSelRenderer.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvSelRenderer_AfterSelect);
            // 
            // tabLabelling
            // 
            this.tabLabelling.Controls.Add(this.panelLabelRendererPage);
            this.tabLabelling.Controls.Add(this.groupBox2);
            resources.ApplyResources(this.tabLabelling, "tabLabelling");
            this.tabLabelling.Name = "tabLabelling";
            this.tabLabelling.UseVisualStyleBackColor = true;
            // 
            // panelLabelRendererPage
            // 
            resources.ApplyResources(this.panelLabelRendererPage, "panelLabelRendererPage");
            this.panelLabelRendererPage.Name = "panelLabelRendererPage";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cmbApplyLabelRefScale);
            this.groupBox2.Controls.Add(this.chkLabelLayer);
            this.groupBox2.Controls.Add(this.cmbLabelRenderer);
            this.groupBox2.Controls.Add(this.label3);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // cmbApplyLabelRefScale
            // 
            resources.ApplyResources(this.cmbApplyLabelRefScale, "cmbApplyLabelRefScale");
            this.cmbApplyLabelRefScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbApplyLabelRefScale.FormattingEnabled = true;
            this.cmbApplyLabelRefScale.Name = "cmbApplyLabelRefScale";
            // 
            // chkLabelLayer
            // 
            resources.ApplyResources(this.chkLabelLayer, "chkLabelLayer");
            this.chkLabelLayer.Name = "chkLabelLayer";
            this.chkLabelLayer.UseVisualStyleBackColor = true;
            // 
            // cmbLabelRenderer
            // 
            this.cmbLabelRenderer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLabelRenderer.FormattingEnabled = true;
            resources.ApplyResources(this.cmbLabelRenderer, "cmbLabelRenderer");
            this.cmbLabelRenderer.Name = "cmbLabelRenderer";
            this.cmbLabelRenderer.SelectedIndexChanged += new System.EventHandler(this.cmbLabelRenderer_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // tabRaster
            // 
            this.tabRaster.Controls.Add(this.cmbFilter);
            this.tabRaster.Controls.Add(this.label25);
            this.tabRaster.Controls.Add(this.panelNoDataColor);
            this.tabRaster.Controls.Add(this.panelNoDataValue);
            this.tabRaster.Controls.Add(this.lblTransPercent);
            this.tabRaster.Controls.Add(this.tbTransparency);
            this.tabRaster.Controls.Add(this.label5);
            this.tabRaster.Controls.Add(this.cmbInterpolationMode);
            this.tabRaster.Controls.Add(this.label4);
            resources.ApplyResources(this.tabRaster, "tabRaster");
            this.tabRaster.Name = "tabRaster";
            this.tabRaster.UseVisualStyleBackColor = true;
            // 
            // cmbFilter
            // 
            this.cmbFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilter.FormattingEnabled = true;
            resources.ApplyResources(this.cmbFilter, "cmbFilter");
            this.cmbFilter.Name = "cmbFilter";
            // 
            // label25
            // 
            resources.ApplyResources(this.label25, "label25");
            this.label25.Name = "label25";
            // 
            // panelNoDataColor
            // 
            this.panelNoDataColor.Controls.Add(this.label10);
            this.panelNoDataColor.Controls.Add(this.btnTranscolor);
            this.panelNoDataColor.Controls.Add(this.btnCancelNoData);
            resources.ApplyResources(this.panelNoDataColor, "panelNoDataColor");
            this.panelNoDataColor.Name = "panelNoDataColor";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // btnTranscolor
            // 
            resources.ApplyResources(this.btnTranscolor, "btnTranscolor");
            this.btnTranscolor.Name = "btnTranscolor";
            this.btnTranscolor.UseVisualStyleBackColor = true;
            this.btnTranscolor.BackColorChanged += new System.EventHandler(this.btnTranscolor_BackColorChanged);
            this.btnTranscolor.Click += new System.EventHandler(this.btnTranscolor_Click);
            // 
            // btnCancelNoData
            // 
            resources.ApplyResources(this.btnCancelNoData, "btnCancelNoData");
            this.btnCancelNoData.Name = "btnCancelNoData";
            this.btnCancelNoData.UseVisualStyleBackColor = true;
            this.btnCancelNoData.Click += new System.EventHandler(this.btnCancelNoData_Click);
            // 
            // panelNoDataValue
            // 
            this.panelNoDataValue.Controls.Add(this.numNoDataValue);
            this.panelNoDataValue.Controls.Add(this.chkNoDataValue);
            resources.ApplyResources(this.panelNoDataValue, "panelNoDataValue");
            this.panelNoDataValue.Name = "panelNoDataValue";
            // 
            // numNoDataValue
            // 
            this.numNoDataValue.DecimalPlaces = 2;
            resources.ApplyResources(this.numNoDataValue, "numNoDataValue");
            this.numNoDataValue.Maximum = new decimal(new int[] {
            -1981284353,
            -1966660860,
            0,
            0});
            this.numNoDataValue.Minimum = new decimal(new int[] {
            -159383553,
            46653770,
            5421,
            -2147483648});
            this.numNoDataValue.Name = "numNoDataValue";
            // 
            // chkNoDataValue
            // 
            resources.ApplyResources(this.chkNoDataValue, "chkNoDataValue");
            this.chkNoDataValue.Name = "chkNoDataValue";
            this.chkNoDataValue.UseVisualStyleBackColor = true;
            // 
            // lblTransPercent
            // 
            resources.ApplyResources(this.lblTransPercent, "lblTransPercent");
            this.lblTransPercent.Name = "lblTransPercent";
            // 
            // tbTransparency
            // 
            resources.ApplyResources(this.tbTransparency, "tbTransparency");
            this.tbTransparency.Maximum = 100;
            this.tbTransparency.Name = "tbTransparency";
            this.tbTransparency.TickFrequency = 10;
            this.tbTransparency.Scroll += new System.EventHandler(this.tbTransparency_Scroll);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // cmbInterpolationMode
            // 
            this.cmbInterpolationMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInterpolationMode.FormattingEnabled = true;
            resources.ApplyResources(this.cmbInterpolationMode, "cmbInterpolationMode");
            this.cmbInterpolationMode.Name = "cmbInterpolationMode";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // tabWebServiceLayer
            // 
            this.tabWebServiceLayer.Controls.Add(this.gvWebThemes);
            resources.ApplyResources(this.tabWebServiceLayer, "tabWebServiceLayer");
            this.tabWebServiceLayer.Name = "tabWebServiceLayer";
            this.tabWebServiceLayer.UseVisualStyleBackColor = true;
            // 
            // gvWebThemes
            // 
            this.gvWebThemes.AllowUserToAddRows = false;
            this.gvWebThemes.AllowUserToDeleteRows = false;
            resources.ApplyResources(this.gvWebThemes, "gvWebThemes");
            this.gvWebThemes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3});
            this.gvWebThemes.Name = "gvWebThemes";
            this.gvWebThemes.RowHeadersVisible = false;
            // 
            // Column1
            // 
            this.Column1.FillWeight = 30F;
            resources.ApplyResources(this.Column1, "Column1");
            this.Column1.Name = "Column1";
            // 
            // Column2
            // 
            resources.ApplyResources(this.Column2, "Column2");
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            resources.ApplyResources(this.Column3, "Column3");
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // tabFields
            // 
            this.tabFields.Controls.Add(this.dgFields);
            this.tabFields.Controls.Add(this.panel4);
            this.tabFields.Controls.Add(this.panel3);
            resources.ApplyResources(this.tabFields, "tabFields");
            this.tabFields.Name = "tabFields";
            this.tabFields.UseVisualStyleBackColor = true;
            // 
            // dgFields
            // 
            this.dgFields.AllowUserToAddRows = false;
            this.dgFields.AllowUserToDeleteRows = false;
            this.dgFields.AllowUserToResizeRows = false;
            this.dgFields.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgFields.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.fieldCol1,
            this.fieldCol2,
            this.fieldCol4,
            this.fieldCol3,
            this.colOrder});
            resources.ApplyResources(this.dgFields, "dgFields");
            this.dgFields.GridColor = System.Drawing.Color.White;
            this.dgFields.MultiSelect = false;
            this.dgFields.Name = "dgFields";
            this.dgFields.RowHeadersVisible = false;
            // 
            // fieldCol1
            // 
            this.fieldCol1.Frozen = true;
            resources.ApplyResources(this.fieldCol1, "fieldCol1");
            this.fieldCol1.Name = "fieldCol1";
            // 
            // fieldCol2
            // 
            resources.ApplyResources(this.fieldCol2, "fieldCol2");
            this.fieldCol2.Name = "fieldCol2";
            this.fieldCol2.ReadOnly = true;
            this.fieldCol2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.fieldCol2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // fieldCol4
            // 
            resources.ApplyResources(this.fieldCol4, "fieldCol4");
            this.fieldCol4.Name = "fieldCol4";
            this.fieldCol4.ReadOnly = true;
            this.fieldCol4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // fieldCol3
            // 
            resources.ApplyResources(this.fieldCol3, "fieldCol3");
            this.fieldCol3.Name = "fieldCol3";
            this.fieldCol3.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.fieldCol3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colOrder
            // 
            resources.ApplyResources(this.colOrder, "colOrder");
            this.colOrder.Name = "colOrder";
            this.colOrder.ReadOnly = true;
            this.colOrder.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.btnFieldsMoveDown);
            this.panel4.Controls.Add(this.btnFieldsMoveUp);
            this.panel4.Controls.Add(this.btnClearAll);
            this.panel4.Controls.Add(this.btnSelectAll);
            resources.ApplyResources(this.panel4, "panel4");
            this.panel4.Name = "panel4";
            // 
            // btnFieldsMoveDown
            // 
            resources.ApplyResources(this.btnFieldsMoveDown, "btnFieldsMoveDown");
            this.btnFieldsMoveDown.Image = global::gView.Win.Dialogs.Properties.Resources.arrow_down_16;
            this.btnFieldsMoveDown.Name = "btnFieldsMoveDown";
            this.btnFieldsMoveDown.UseVisualStyleBackColor = true;
            this.btnFieldsMoveDown.Click += new System.EventHandler(this.btnFieldsMoveDown_Click);
            // 
            // btnFieldsMoveUp
            // 
            resources.ApplyResources(this.btnFieldsMoveUp, "btnFieldsMoveUp");
            this.btnFieldsMoveUp.Image = global::gView.Win.Dialogs.Properties.Resources.arrow_up_16;
            this.btnFieldsMoveUp.Name = "btnFieldsMoveUp";
            this.btnFieldsMoveUp.UseVisualStyleBackColor = true;
            this.btnFieldsMoveUp.Click += new System.EventHandler(this.btnFieldsMoveUp_Click);
            // 
            // btnClearAll
            // 
            resources.ApplyResources(this.btnClearAll, "btnClearAll");
            this.btnClearAll.Name = "btnClearAll";
            this.btnClearAll.UseVisualStyleBackColor = true;
            this.btnClearAll.Click += new System.EventHandler(this.btnClearAll_Click);
            // 
            // btnSelectAll
            // 
            resources.ApplyResources(this.btnSelectAll, "btnSelectAll");
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.groupBox3);
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Name = "panel3";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cmbPrimaryField);
            this.groupBox3.Controls.Add(this.label6);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // cmbPrimaryField
            // 
            this.cmbPrimaryField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cmbPrimaryField, "cmbPrimaryField");
            this.cmbPrimaryField.FormattingEnabled = true;
            this.cmbPrimaryField.Name = "cmbPrimaryField";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // tabSR
            // 
            resources.ApplyResources(this.tabSR, "tabSR");
            this.tabSR.Name = "tabSR";
            this.tabSR.UseVisualStyleBackColor = true;
            // 
            // tabDescription
            // 
            this.tabDescription.Controls.Add(this.txtCopyright);
            this.tabDescription.Controls.Add(this.label12);
            this.tabDescription.Controls.Add(this.txtDescription);
            this.tabDescription.Controls.Add(this.label13);
            resources.ApplyResources(this.tabDescription, "tabDescription");
            this.tabDescription.Name = "tabDescription";
            this.tabDescription.UseVisualStyleBackColor = true;
            // 
            // txtCopyright
            // 
            this.txtCopyright.AcceptsReturn = true;
            resources.ApplyResources(this.txtCopyright, "txtCopyright");
            this.txtCopyright.Name = "txtCopyright";
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // txtDescription
            // 
            this.txtDescription.AcceptsReturn = true;
            resources.ApplyResources(this.txtDescription, "txtDescription");
            this.txtDescription.Name = "txtDescription";
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            // 
            // tabMapService
            // 
            this.tabMapService.Controls.Add(this.groupBox7);
            resources.ApplyResources(this.tabMapService, "tabMapService");
            this.tabMapService.Name = "tabMapService";
            this.tabMapService.UseVisualStyleBackColor = true;
            // 
            // groupBox7
            // 
            resources.ApplyResources(this.groupBox7, "groupBox7");
            this.groupBox7.Controls.Add(this.label18);
            this.groupBox7.Controls.Add(this.label17);
            this.groupBox7.Controls.Add(this.label16);
            this.groupBox7.Controls.Add(this.label15);
            this.groupBox7.Controls.Add(this.label14);
            this.groupBox7.Controls.Add(this.cmbMapServerGrouplayerStyle);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.TabStop = false;
            // 
            // label18
            // 
            resources.ApplyResources(this.label18, "label18");
            this.label18.Name = "label18";
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            // 
            // cmbMapServerGrouplayerStyle
            // 
            this.cmbMapServerGrouplayerStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMapServerGrouplayerStyle.FormattingEnabled = true;
            resources.ApplyResources(this.cmbMapServerGrouplayerStyle, "cmbMapServerGrouplayerStyle");
            this.cmbMapServerGrouplayerStyle.Name = "cmbMapServerGrouplayerStyle";
            // 
            // tabCompostionMode
            // 
            this.tabCompostionMode.Controls.Add(this.groupBox8);
            resources.ApplyResources(this.tabCompostionMode, "tabCompostionMode");
            this.tabCompostionMode.Name = "tabCompostionMode";
            this.tabCompostionMode.UseVisualStyleBackColor = true;
            // 
            // groupBox8
            // 
            resources.ApplyResources(this.groupBox8, "groupBox8");
            this.groupBox8.Controls.Add(this.panelCompModeCopy);
            this.groupBox8.Controls.Add(this.cmbCompositionMode);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.TabStop = false;
            // 
            // panelCompModeCopy
            // 
            resources.ApplyResources(this.panelCompModeCopy, "panelCompModeCopy");
            this.panelCompModeCopy.Controls.Add(this.label24);
            this.panelCompModeCopy.Controls.Add(this.label23);
            this.panelCompModeCopy.Controls.Add(this.pictureBox2);
            this.panelCompModeCopy.Controls.Add(this.pictureBox1);
            this.panelCompModeCopy.Controls.Add(this.label22);
            this.panelCompModeCopy.Controls.Add(this.label21);
            this.panelCompModeCopy.Controls.Add(this.label20);
            this.panelCompModeCopy.Controls.Add(this.label19);
            this.panelCompModeCopy.Controls.Add(this.numCompModeCopyTransparency);
            this.panelCompModeCopy.Name = "panelCompModeCopy";
            // 
            // label24
            // 
            resources.ApplyResources(this.label24, "label24");
            this.label24.Name = "label24";
            // 
            // label23
            // 
            resources.ApplyResources(this.label23, "label23");
            this.label23.Name = "label23";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::gView.Win.Dialogs.Properties.Resources.trans_comp_copy;
            resources.ApplyResources(this.pictureBox2, "pictureBox2");
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::gView.Win.Dialogs.Properties.Resources.trans_symbol;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // label22
            // 
            resources.ApplyResources(this.label22, "label22");
            this.label22.Name = "label22";
            // 
            // label21
            // 
            resources.ApplyResources(this.label21, "label21");
            this.label21.Name = "label21";
            // 
            // label20
            // 
            resources.ApplyResources(this.label20, "label20");
            this.label20.Name = "label20";
            // 
            // label19
            // 
            resources.ApplyResources(this.label19, "label19");
            this.label19.Name = "label19";
            // 
            // numCompModeCopyTransparency
            // 
            this.numCompModeCopyTransparency.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            resources.ApplyResources(this.numCompModeCopyTransparency, "numCompModeCopyTransparency");
            this.numCompModeCopyTransparency.Name = "numCompModeCopyTransparency";
            // 
            // cmbCompositionMode
            // 
            resources.ApplyResources(this.cmbCompositionMode, "cmbCompositionMode");
            this.cmbCompositionMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCompositionMode.FormattingEnabled = true;
            this.cmbCompositionMode.Name = "cmbCompositionMode";
            this.cmbCompositionMode.SelectedIndexChanged += new System.EventHandler(this.cmbCompositionMode_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel2);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.button1);
            this.panel2.Controls.Add(this.btnOK);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // FormLayerProperties
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "FormLayerProperties";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormLayerProperties_FormClosed);
            this.Load += new System.EventHandler(this.FormLayerProperties_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtMaximunZoom2FeatureScale)).EndInit();
            this.groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtLabelMaxScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtLabelMinScale)).EndInit();
            this.GroupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtMaxScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinScale)).EndInit();
            this.tabRenderer.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.tabSelRenderer.ResumeLayout(false);
            this.tabLabelling.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabRaster.ResumeLayout(false);
            this.tabRaster.PerformLayout();
            this.panelNoDataColor.ResumeLayout(false);
            this.panelNoDataColor.PerformLayout();
            this.panelNoDataValue.ResumeLayout(false);
            this.panelNoDataValue.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNoDataValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbTransparency)).EndInit();
            this.tabWebServiceLayer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gvWebThemes)).EndInit();
            this.tabFields.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabDescription.ResumeLayout(false);
            this.tabDescription.PerformLayout();
            this.tabMapService.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.tabCompostionMode.ResumeLayout(false);
            this.groupBox8.ResumeLayout(false);
            this.panelCompModeCopy.ResumeLayout(false);
            this.panelCompModeCopy.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCompModeCopyTransparency)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void FormLayerProperties_Load(object sender, System.EventArgs e)
        {
            if (_layer == null)
            {
                return;
            }

            //radioAllScales.Enabled=radioScales.Enabled=txtMinScale.Enabled=txtMaxScale.Enabled=
            //	(_layer.FeatureRenderer!=null);

            //if(_layer.MinimumScale<0) _layer.MinimumScale=0;
            //if(_layer.MaximumScale<0) _layer.MaximumScale=0;
            //if(_layer.MinimumScale>0 || _layer.MaximumScale>0) 
            //{
            //    txtMinScale.Text=_layer.MaximumScale.ToString();
            //    txtMaxScale.Text=_layer.MinimumScale.ToString();
            //    radioScales.Checked=true;
            //} 
            //else 
            //{
            //    radioAllScales.Checked=true;
            //}

            txtMinScale.Value = (decimal)Math.Abs(_layer.MaximumScale);
            txtMaxScale.Value = (decimal)Math.Abs(_layer.MinimumScale);
            txtLabelMinScale.Value = (decimal)Math.Abs(_layer.MaximumLabelScale);
            txtLabelMaxScale.Value = (decimal)Math.Abs(_layer.MinimumLabelScale);
            txtMaximunZoom2FeatureScale.Value = (decimal)Math.Max(_layer.MaximumZoomToFeatureScale, 1);

            radioAllScales.Checked = _layer.MinimumScale <= 0 && _layer.MaximumScale <= 0;
            radioLabelAllScales.Checked = _layer.MinimumLabelScale <= 0 && _layer.MaximumLabelScale <= 0;
            radioScales.Checked = !radioAllScales.Checked;
            radioLabelScales.Checked = !radioLabelAllScales.Checked;

            if (_layer is IFeatureLayer)
            {
                foreach (var geomType in Enum.GetValues(typeof(GeometryType)))
                {
                    cmbGeometryType.Items.Add(geomType);
                }

                cmbGeometryType.SelectedItem = ((IFeatureLayer)_layer).LayerGeometryType;
                if (((IFeatureLayer)_layer).FeatureClass != null &&
                   ((IFeatureLayer)_layer).FeatureClass.GeometryType != GeometryType.Unknown)
                {
                    cmbGeometryType.Enabled = false;
                }
            }
            else
            {
                cmbGeometryType.Visible = false;
            }

            /*
			if(_dataset is IFeatureDataset) 
			{
				FormSpatialReference sr=new FormSpatialReference(((IFeatureDataset)_dataset).SpatialReference);
				sr.canModify=false;
				tabSR.Controls.Add(sr.panelReferenceSystem);
			}
			*/
            if (_layer is IFeatureLayer)
            {
                if (((IFeatureLayer)_layer).FeatureClass != null)
                {
                    FormSpatialReference sr = new FormSpatialReference(((IFeatureLayer)_layer).FeatureClass.SpatialReference);
                    sr.canModify = false;
                    tabSR.Controls.Add(sr.panelReferenceSystem);
                }
                MakeRendererTree();
                MakeSelectionRendererTree();
                MakeLabelRendererCombo();
                MakeFieldTab();
            }
            if (_layer is IRasterLayer)
            {
                foreach (GraphicsEngine.Filters.FilterImplementations filter in Enum.GetValues(typeof(GraphicsEngine.Filters.FilterImplementations)))
                {
                    cmbFilter.Items.Add(filter);
                    if (filter == ((IRasterLayer)_layer).FilterImplementation)
                    {
                        cmbFilter.SelectedItem = filter;
                    }
                }
                foreach (InterpolationMethod mode in Enum.GetValues(typeof(InterpolationMethod)))
                {
                    cmbInterpolationMode.Items.Add(mode);
                    if (mode == ((IRasterLayer)_layer).InterpolationMethod)
                    {
                        cmbInterpolationMode.SelectedItem = mode;
                    }
                }
                tbTransparency.Value = (int)Math.Max(Math.Min((int)(((IRasterLayer)_layer).Opacity * 100f), 100), 0);
                lblTransPercent.Text = tbTransparency.Value.ToString() + "%";

                btnTranscolor.BackColor = ((IRasterLayer)_layer).TransparentColor.ToGdiColor();
            }
            if (_layer.Class is IGridClass)
            {
                if (Bit.Has(((IGridClass)_layer.Class).ImplementsRenderMethods, GridRenderMethode.NullValue))
                {
                    panelNoDataColor.Visible = false;
                    panelNoDataValue.Visible = true;

                    chkNoDataValue.Checked = ((IGridClass)_layer.Class).UseIgnoreDataValue;
                    numNoDataValue.Value = (decimal)((IGridClass)_layer.Class).IgnoreDataValue;
                }
            }
            if (_layer is IWebServiceLayer)
            {
                gvWebThemes.Rows.Clear();
                foreach (IWebServiceTheme theme in ((IWebServiceClass)((IWebServiceLayer)_layer).Class).Themes)
                {
                    gvWebThemes.Rows.Add(new object[] { !theme.Locked, theme.Title, theme.LayerID });
                }
                if (((IWebServiceLayer)_layer).WebServiceClass != null)
                {
                    FormSpatialReference sr = new FormSpatialReference(((IWebServiceLayer)_layer).WebServiceClass.SpatialReference);
                    sr.canModify = false;
                    tabSR.Controls.Add(sr.panelReferenceSystem);
                }
            }
        }

        #region RendererPage

        private void MakeRendererTree()
        {
            if (_dataset == null || _layer == null)
            {
                return;
            }

            if (!(_dataset is IFeatureDataset))
            {
                return;
            }

            if (!(_layer is IFeatureLayer))
            {
                return;
            }

            tvRenderer.Nodes.Clear();
            if (_dataset is IImageDataset)
            {
                TreeNode node = new TreeNode(_dataset.DatasetGroupName);
                tvRenderer.Nodes.Add(node);
                node.Nodes.Add(new RendererNode(null));
                if (_renderer == null)
                {
                    tvRenderer.SelectedNode = node.Nodes[0];
                }
            }

            PlugInManager compManager = new PlugInManager();
            foreach (var pluginType in compManager.GetPlugins(Plugins.Type.IFeatureRenderer))
            {
                IFeatureRenderer renderer = compManager.CreateInstance<IFeatureRenderer>(pluginType);

                if (renderer == null)
                {
                    continue;
                }

                if (!renderer.CanRender(_layer as IFeatureLayer, null))
                {
                    continue;
                }

                TreeNode parent = null;
                foreach (TreeNode cat in tvRenderer.Nodes)
                {
                    if (cat.Text == renderer.Category)
                    {
                        parent = cat;
                        break;
                    }
                }
                if (parent == null)
                {
                    parent = new TreeNode(renderer.Category);
                    tvRenderer.Nodes.Add(parent);
                }

                if (_renderer != null)
                {
                    if (_renderer.GetType().Equals(renderer.GetType()))
                    {
                        renderer = _renderer;
                    }
                }

                TreeNode rNode = new RendererNode(renderer);
                parent.Nodes.Add(rNode);

                if (_renderer != null)
                {
                    if (_renderer.GetType().Equals(renderer.GetType()))
                    {
                        tvRenderer.SelectedNode = rNode;
                    }
                }
            }

            var featureLayer = (IFeatureLayer)_layer;
            chkRenderLayer.Checked = featureLayer.FeatureRenderer != null;

            #region RefScale Combos

            if (featureLayer.ApplyRefScale == false)
            {
                cmbApplyRefScale.SelectedIndex = 0;
            }
            else
            {
                if (featureLayer.MaxRefScaleFactor <= 1f)
                {
                    cmbApplyRefScale.SelectedIndex = 1;
                }
                else
                {
                    cmbApplyRefScale.SelectedIndex = Math.Min(cmbApplyRefScale.Items.Count, (int)((featureLayer.MaxRefScaleFactor - 1f) * 10f) + 2);
                }
            }

            if (featureLayer.ApplyLabelRefScale == false)
            {
                cmbApplyLabelRefScale.SelectedIndex = 0;
            }
            else
            {
                if (featureLayer.MaxLabelRefScaleFactor <= 1f)
                {
                    cmbApplyLabelRefScale.SelectedIndex = 1;
                }
                else
                {
                    cmbApplyLabelRefScale.SelectedIndex = Math.Min(cmbApplyLabelRefScale.Items.Count, (int)((featureLayer.MaxLabelRefScaleFactor - 1f) * 10f) + 2);
                }
            }

            #endregion
        }

        private void tvRenderer_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            TreeNode sel = tvRenderer.SelectedNode;
            if (!(sel is RendererNode))
            {
                tvRenderer.SelectedNode = ((TreeNode)sel).Nodes[0];
                return;
            }

            panelRendererPropPage.Controls.Clear(); ;

            IFeatureRenderer renderer = (IFeatureRenderer)((RendererNode)sel).FeatureRenderer;
            if (renderer != null)
            {
                if (renderer is IPropertyPage)
                {
                    Control control = ((IPropertyPage)renderer).PropertyPage(_layer) as Control;
                    if (control != null)
                    {
                        panelRendererPropPage.Controls.Add(control);
                    }
                }
            }
            _renderer = renderer;
            chkRenderLayer.Checked = true;
        }
        #endregion

        #region SelectionRendererPage
        private void MakeSelectionRendererTree()
        {
            if (_dataset == null || _layer == null)
            {
                return;
            }

            if (!(_dataset is IFeatureDataset))
            {
                return;
            }

            if (!(_layer is IFeatureLayer))
            {
                return;
            }

            tvSelRenderer.Nodes.Clear();
            if (_dataset is IImageDataset)
            {
                TreeNode node = new TreeNode(_dataset.DatasetGroupName);
                tvSelRenderer.Nodes.Add(node);
                node.Nodes.Add(new RendererNode(null));
                if (_renderer == null)
                {
                    tvSelRenderer.SelectedNode = node.Nodes[0];
                }
            }

            PlugInManager compManager = new PlugInManager();
            foreach (var pluginType in compManager.GetPlugins(Plugins.Type.IFeatureRenderer))
            {
                IFeatureRenderer selRenderer = compManager.CreateInstance<IFeatureRenderer>(pluginType);

                if (selRenderer == null)
                {
                    continue;
                }

                if (!selRenderer.CanRender(_layer as IFeatureLayer, null))
                {
                    continue;
                }

                TreeNode parent = null;
                foreach (TreeNode cat in tvSelRenderer.Nodes)
                {
                    if (cat.Text == selRenderer.Category)
                    {
                        parent = cat;
                        break;
                    }
                }
                if (parent == null)
                {
                    parent = new TreeNode(selRenderer.Category);
                    tvSelRenderer.Nodes.Add(parent);
                }

                if (_selRenderer != null)
                {
                    if (_selRenderer.GetType().Equals(selRenderer.GetType()))
                    {
                        selRenderer = _selRenderer;
                    }
                }

                TreeNode rNode = new RendererNode(selRenderer);
                parent.Nodes.Add(rNode);

                if (_selRenderer != null)
                {
                    if (_selRenderer.GetType().Equals(selRenderer.GetType()))
                    {
                        tvSelRenderer.SelectedNode = rNode;
                    }
                }
            }
        }

        private void tvSelRenderer_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            TreeNode sel = tvSelRenderer.SelectedNode;
            if (!(sel is RendererNode))
            {
                tvSelRenderer.SelectedNode = ((TreeNode)sel).Nodes[0];
                return;
            }

            panelSelRendererPropPage.Controls.Clear(); ;

            IFeatureRenderer selRenderer = (IFeatureRenderer)((RendererNode)sel).FeatureRenderer;
            if (selRenderer != null)
            {
                if (selRenderer is IPropertyPage)
                {
                    Control control = ((IPropertyPage)selRenderer).PropertyPage(_layer) as Control;

                    if (control != null)
                    {
                        panelSelRendererPropPage.Controls.Add(control);
                    }
                }
            }
            _selRenderer = selRenderer;
        }

        #endregion

        #region LabelRendererPage
        private void MakeLabelRendererCombo()
        {
            if (_dataset == null || _layer == null)
            {
                return;
            }

            if (!(_dataset is IFeatureDataset))
            {
                return;
            }

            if (!(_layer is IFeatureLayer))
            {
                return;
            }

            cmbLabelRenderer.Items.Clear();
            PlugInManager compManager = new PlugInManager();
            foreach (var pluginType in compManager.GetPlugins(Plugins.Type.ILabelRenderer))
            {
                ILabelRenderer renderer = compManager.CreateInstance<ILabelRenderer>(pluginType);
                if (renderer == null)
                {
                    continue;
                }

                LabelRendererItem item;
                if (_labelRenderer != null &&
                    renderer.GetType().Equals(_labelRenderer.GetType()))
                {
                    item = new LabelRendererItem(_labelRenderer);
                }
                else
                {
                    item = new LabelRendererItem(renderer);
                }

                cmbLabelRenderer.Items.Add(item);
                if (item.LabelRenderer == _labelRenderer)
                {
                    cmbLabelRenderer.SelectedItem = item;
                }
            }
        }

        private void cmbLabelRenderer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cmbLabelRenderer.SelectedItem is LabelRendererItem))
            {
                return;
            }

            panelLabelRendererPage.Controls.Clear();
            _labelRenderer = ((LabelRendererItem)cmbLabelRenderer.SelectedItem).LabelRenderer;
            if (_labelRenderer is IPropertyPage)
            {
                if (_labelRenderer is IPropertyPage)
                {
                    Control control = ((IPropertyPage)_labelRenderer).PropertyPage(_layer) as Control;
                    if (control != null)
                    {
                        panelLabelRendererPage.Controls.Add(control);
                    }
                }
            }
        }

        #endregion

        #region Fields
        private void MakeFieldTab()
        {
            if (!(_layer is IFeatureLayer))
            {
                return;
            }

            IFeatureLayer fLayer = _layer as IFeatureLayer;
            if (fLayer.FeatureClass == null || fLayer.FeatureClass.Fields == null)
            {
                return;
            }

            foreach (IField field in fLayer.Fields.ToEnumerable())
            {
                dgFields.Rows.Add(
                    new object[]{
                        field.visible,
                        field.name,
                        field.type.ToString(),
                        field.aliasname,
                        ((field is Field) ? ((Field)field).Priority : -1)});
            }
            dgFields.Sort(colOrder, ListSortDirection.Ascending);

            // Primary Display Field
            foreach (IField field in fLayer.Fields.ToEnumerable())
            {
                cmbPrimaryField.Items.Add(field.name);
                if (field == fLayer.Fields.PrimaryDisplayField)
                {
                    cmbPrimaryField.SelectedIndex = cmbPrimaryField.Items.Count - 1;
                }
            }
        }
        #endregion

        private void CommitSettings()
        {
            //if(radioScales.Checked && radioScales.Enabled) 
            //{
            //    _layer.MinimumScale=Convert.ToDouble(txtMaxScale.Text);
            //    _layer.MaximumScale=Convert.ToDouble(txtMinScale.Text);
            //}

            _layer.MinimumScale = (double)txtMaxScale.Value;
            _layer.MaximumScale = (double)txtMinScale.Value;
            if (radioAllScales.Checked)
            {
                _layer.MinimumScale *= -1.0;
                _layer.MaximumScale *= -1.0;
            }
            _layer.MinimumLabelScale = (double)txtLabelMaxScale.Value;
            _layer.MaximumLabelScale = (double)txtLabelMinScale.Value;
            if (radioLabelAllScales.Checked)
            {
                _layer.MaximumLabelScale *= -1.0;
                _layer.MinimumLabelScale *= -1.0;
            }
            _layer.MaximumZoomToFeatureScale = (double)txtMaximunZoom2FeatureScale.Value;

            if (_layer is Layer)
            {
                ((Layer)_layer).GroupLayer = _groupLayer;
            }
            if (_layer is IFeatureLayer)
            {
                if (((IFeatureLayer)_layer).FeatureClass != null &&
                   ((IFeatureLayer)_layer).FeatureClass.GeometryType == GeometryType.Unknown)
                {
                    ((IFeatureLayer)_layer).LayerGeometryType = (GeometryType)cmbGeometryType.SelectedItem;
                }


                if (chkRenderLayer.Checked)
                {
                    ((IFeatureLayer)_layer).FeatureRenderer = _renderer;
                }
                else
                {
                    ((IFeatureLayer)_layer).FeatureRenderer = null;
                }
                ((IFeatureLayer)_layer).SelectionRenderer = _selRenderer;
                if (chkLabelLayer.Checked)
                {
                    ((IFeatureLayer)_layer).LabelRenderer = _labelRenderer;
                }
                else
                {
                    ((IFeatureLayer)_layer).LabelRenderer = null;
                }
                IFeatureLayer fLayer = _layer as IFeatureLayer;
                if (fLayer.Fields != null)
                {
                    foreach (IField field in fLayer.Fields.ToEnumerable())
                    {
                        foreach (DataGridViewRow row in dgFields.Rows)
                        {
                            if (row.Cells[1].Value.ToString() == field.name)
                            {
                                field.visible = (bool)row.Cells[0].Value;

                                if (field is Field)
                                {
                                    ((Field)field).aliasname = row.Cells[3].Value.ToString();
                                    ((Field)field).Priority = (int)row.Cells[4].Value;
                                }
                                break;
                            }
                        }
                        if (cmbPrimaryField.SelectedItem != null && field.name == cmbPrimaryField.SelectedItem.ToString())
                        {
                            fLayer.Fields.PrimaryDisplayField = field;
                        }
                    }
                }
                fLayer.ApplyRefScale = cmbApplyRefScale.SelectedIndex > 0;
                fLayer.MaxRefScaleFactor = Math.Max(0f,
                    cmbApplyRefScale.SelectedItem is ReferenceScaleItem ? ((ReferenceScaleItem)cmbApplyRefScale.SelectedItem).Factor : 0f);

                fLayer.ApplyLabelRefScale = cmbApplyLabelRefScale.SelectedIndex > 0;
                fLayer.MaxLabelRefScaleFactor = Math.Max(0f,
                    cmbApplyLabelRefScale.SelectedItem is ReferenceScaleItem ? ((ReferenceScaleItem)cmbApplyLabelRefScale.SelectedItem).Factor : 0f);
            }
            if (_layer is IRasterLayer)
            {
                ((IRasterLayer)_layer).FilterImplementation = (GraphicsEngine.Filters.FilterImplementations)cmbFilter.SelectedItem;
                ((IRasterLayer)_layer).InterpolationMethod = (InterpolationMethod)cmbInterpolationMode.SelectedItem;
                ((IRasterLayer)_layer).Opacity = ((float)tbTransparency.Value) / 100f;
                ((IRasterLayer)_layer).TransparentColor = btnTranscolor.BackColor.ToArgbColor();
            }
            if (_layer.Class is IGridClass)
            {
                if (Bit.Has(((IGridClass)_layer.Class).ImplementsRenderMethods, GridRenderMethode.NullValue))
                {
                    ((IGridClass)_layer.Class).UseIgnoreDataValue = chkNoDataValue.Checked;
                    ((IGridClass)_layer.Class).IgnoreDataValue = (double)numNoDataValue.Value;
                }
            }
            if (_layer is IGroupLayer)
            {
                ((IGroupLayer)_layer).MapServerStyle = (MapServerGrouplayerStyle)cmbMapServerGrouplayerStyle.SelectedItem;
            }
            if (_layer is IWebServiceLayer)
            {
                foreach (IWebServiceTheme theme in ((IWebServiceClass)((IWebServiceLayer)_layer).Class).Themes)
                {
                    for (int i = 0; i < gvWebThemes.Rows.Count; i++)
                    {
                        if (gvWebThemes[2, i].Value.Equals(theme.LayerID))
                        {
                            theme.Locked = !(bool)gvWebThemes[0, i].Value;
                            break;
                        }
                    }
                }
            }
            if (_layer is IFeatureLayerComposition)
            {
                if (cmbCompositionMode.SelectedItem is CompositionModeItem)
                {
                    ((IFeatureLayerComposition)_layer).CompositionMode = ((CompositionModeItem)cmbCompositionMode.SelectedItem).CompositionMode;
                }

                ((IFeatureLayerComposition)_layer).CompositionModeCopyTransparency =
                    (float)numCompModeCopyTransparency.Value;
            }
            if (_map != null && _layer != null)
            {
                _map.SetLayerDescription(_layer.ID, txtDescription.Text);
                _map.SetLayerCopyrightText(_layer.ID, txtCopyright.Text);
            }

            _mapDocument.TemporaryRestore();
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            CommitSettings();

            foreach (ILayerPropertyPage page in propertyPages)
            {
                page.Commit();
            }

            if (_layer != null)
            {
                _layer.FirePropertyChanged();
            }
        }

        private void tbTransparency_Scroll(object sender, EventArgs e)
        {
            lblTransPercent.Text = tbTransparency.Value.ToString() + "%";
        }

        private void btnTranscolor_BackColorChanged(object sender, EventArgs e)
        {
            if (btnTranscolor.BackColor == System.Drawing.Color.Transparent)
            {
                btnTranscolor.Text = "None";
                btnCancelNoData.Visible = false;
            }
            else
            {
                btnTranscolor.Text = "";
                btnCancelNoData.Visible = true;
            }
        }

        private void btnTranscolor_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            dlg.Color = btnTranscolor.BackColor;

            if (dlg.Color.A == 0)
            {
                dlg.Color = Color.FromArgb(255, dlg.Color);
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                btnTranscolor.BackColor = dlg.Color;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgFields.Rows)
            {
                row.Cells[0].Value = true;
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgFields.Rows)
            {
                row.Cells[0].Value = false;
            }
        }

        private void btnFieldsMoveUp_Click(object sender, EventArgs e)
        {
            if (dgFields.CurrentCellAddress.Y >= 1)
            {
                DataGridViewRow row0 = dgFields.Rows[dgFields.CurrentCellAddress.Y - 1];
                DataGridViewRow row1 = dgFields.Rows[dgFields.CurrentCellAddress.Y];

                row0.Cells[4].Value = (int)row0.Cells[4].Value + 1;
                row1.Cells[4].Value = (int)row1.Cells[4].Value - 1;

                dgFields.Sort(colOrder, ListSortDirection.Ascending);
            }
        }

        private void btnFieldsMoveDown_Click(object sender, EventArgs e)
        {
            if (dgFields.CurrentCellAddress.Y >= 0 &&
                dgFields.CurrentCellAddress.Y < dgFields.Rows.Count - 1)
            {
                DataGridViewRow row0 = dgFields.Rows[dgFields.CurrentCellAddress.Y + 1];
                DataGridViewRow row1 = dgFields.Rows[dgFields.CurrentCellAddress.Y];

                row0.Cells[4].Value = (int)row0.Cells[4].Value - 1;
                row1.Cells[4].Value = (int)row1.Cells[4].Value + 1;

                dgFields.Sort(colOrder, ListSortDirection.Ascending);
            }
        }

        private void radioScales_CheckedChanged(object sender, EventArgs e)
        {
            txtMinScale.Enabled = txtMaxScale.Enabled = !radioAllScales.Checked;
            txtLabelMinScale.Enabled = txtLabelMaxScale.Enabled = !radioLabelAllScales.Checked;
        }

        private void FormLayerProperties_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_layer != null && _layer.GroupLayer == null && _layer is Layer && _groupLayer != null)
            {
                ((Layer)_layer).GroupLayer = _groupLayer;
            }
        }

        private void btnCancelNoData_Click(object sender, EventArgs e)
        {
            btnTranscolor.BackColor = Color.Transparent;
        }

        private void cmbCompositionMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCompositionMode.SelectedItem is CompositionModeItem)
            {
                panelCompModeCopy.Visible = ((CompositionModeItem)cmbCompositionMode.SelectedItem).CompositionMode == FeatureLayerCompositionMode.Copy;
            }
        }

        #region ItemClasses

        class ReferenceScaleItem
        {
            public ReferenceScaleItem(float factor, string text)
            {
                this.Factor = factor;
                this.Text = text;
            }

            public float Factor { get; private set; }

            public string Text { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }

        class CompositionModeItem
        {
            public CompositionModeItem(FeatureLayerCompositionMode compositionMode)
            {
                this.CompositionMode = compositionMode;
            }
            public FeatureLayerCompositionMode CompositionMode { get; private set; }

            public override string ToString()
            {
                return $"{this.CompositionMode.ToString()}{((int)this.CompositionMode == 0 ? " (default)" : "")}";
            }
        }

        #endregion
    }

    internal class RendererNode : TreeNode
    {
        IFeatureRenderer _renderer;

        public RendererNode(IFeatureRenderer renderer)
        {
            _renderer = renderer;

            if (_renderer != null)
            {
                base.Text = renderer.Name;
            }
            else
            {
                base.Text = "Default";
            }
        }
        public IFeatureRenderer FeatureRenderer
        {
            get { return _renderer; }
        }
    }

    internal class LabelRendererItem
    {
        ILabelRenderer _renderer;

        public LabelRendererItem(ILabelRenderer renderer)
        {
            _renderer = renderer;
        }

        public ILabelRenderer LabelRenderer
        {
            get
            {
                return _renderer;
            }
        }

        public override string ToString()
        {
            if (_renderer == null)
            {
                return "null";
            }

            return _renderer.Name;
        }
    }


}
