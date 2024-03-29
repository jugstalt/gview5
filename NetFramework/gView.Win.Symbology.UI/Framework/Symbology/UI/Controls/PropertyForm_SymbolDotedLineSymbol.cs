using gView.Framework.UI;

namespace gView.Framework.Symbology.UI
{
    /// <summary>
    /// Zusammenfassung f�r PropertyForm_SimpleFillSymbol.
    /// </summary>
    internal class PropertyForm_SymbolDotedLineSymbol : System.Windows.Forms.Form, IPropertyPageUI, gView.Framework.Symbology.UI.IPropertyPanel
    {
        private System.Windows.Forms.ColorDialog colorDialog1;
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        public System.Windows.Forms.Panel panelLineSymbol;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        public System.Windows.Forms.Panel panelFillSymbol;
        private ILineSymbol _symbol = null;

        public PropertyForm_SymbolDotedLineSymbol()
        {
            InitializeComponent();
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

        #region Vom Windows Form-Designer generierter Code
        /// <summary>
        /// Erforderliche Methode f�r die Designerunterst�tzung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor ge�ndert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertyForm_SymbolDotedLineSymbol));
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.panelLineSymbol = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panelFillSymbol = new System.Windows.Forms.Panel();
            this.panelLineSymbol.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.panelFillSymbol.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelLineSymbol
            // 
            this.panelLineSymbol.Controls.Add(this.tabControl1);
            resources.ApplyResources(this.panelLineSymbol, "panelLineSymbol");
            this.panelLineSymbol.Name = "panelLineSymbol";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.propertyGrid);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // propertyGrid
            // 
            resources.ApplyResources(this.propertyGrid, "propertyGrid");
            this.propertyGrid.HelpForeColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.ToolbarVisible = false;
            this.propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid_PropertyValueChanged);
            // 
            // panelFillSymbol
            // 
            this.panelFillSymbol.Controls.Add(this.panelLineSymbol);
            resources.ApplyResources(this.panelFillSymbol, "panelFillSymbol");
            this.panelFillSymbol.Name = "panelFillSymbol";
            // 
            // PropertyForm_SymbolDotedLineSymbol
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.panelFillSymbol);
            this.Name = "PropertyForm_SymbolDotedLineSymbol";
            this.Load += new System.EventHandler(this.PropertyForm_SimpleFillSymbol_Load);
            this.panelLineSymbol.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.panelFillSymbol.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void PropertyForm_SimpleFillSymbol_Load(object sender, System.EventArgs e)
        {

        }

        private void propertyGrid_PropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(_symbol);
            }
        }

        #region IPropertryPageUI
        public event PropertyChangedEvent PropertyChanged = null;
        #endregion

        #region PropertyPanel Member

        public object PropertyPanel(ISymbol symbol)
        {
            if (symbol is SymbolDotedLineSymbol)
            {
                _symbol = (ILineSymbol)symbol;
            }

            if (_symbol == null)
            {
                return null;
            }

            propertyGrid.SelectedObject = new CustomClass(_symbol);

            return panelFillSymbol;
        }

        #endregion
    }
}
