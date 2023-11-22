namespace gView.Framework.UI.Dialogs.Network
{
    partial class NetworkEdgeWeightsControl
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lstWeights = new System.Windows.Forms.ListView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.propsWeight = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnAddWeight = new System.Windows.Forms.ToolStripButton();
            this.btnRemoveWeight = new System.Windows.Forms.ToolStripButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.gridFcs = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewButtonColumn();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.groupBox1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridFcs)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lstWeights);
            this.groupBox1.Controls.Add(this.splitter1);
            this.groupBox1.Controls.Add(this.propsWeight);
            this.groupBox1.Controls.Add(this.toolStrip1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 80);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Size = new System.Drawing.Size(246, 442);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edge Weight";
            // 
            // lstWeights
            // 
            this.lstWeights.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstWeights.HideSelection = false;
            this.lstWeights.Location = new System.Drawing.Point(4, 62);
            this.lstWeights.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lstWeights.MultiSelect = false;
            this.lstWeights.Name = "lstWeights";
            this.lstWeights.Size = new System.Drawing.Size(238, 170);
            this.lstWeights.TabIndex = 1;
            this.lstWeights.UseCompatibleStateImageBehavior = false;
            this.lstWeights.View = System.Windows.Forms.View.List;
            this.lstWeights.SelectedIndexChanged += new System.EventHandler(this.lstWeights_SelectedIndexChanged);
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Location = new System.Drawing.Point(4, 232);
            this.splitter1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(238, 5);
            this.splitter1.TabIndex = 3;
            this.splitter1.TabStop = false;
            // 
            // propsWeight
            // 
            this.propsWeight.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.propsWeight.Location = new System.Drawing.Point(4, 237);
            this.propsWeight.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.propsWeight.Name = "propsWeight";
            this.propsWeight.Size = new System.Drawing.Size(238, 200);
            this.propsWeight.TabIndex = 2;
            this.propsWeight.ToolbarVisible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnAddWeight,
            this.btnRemoveWeight});
            this.toolStrip1.Location = new System.Drawing.Point(4, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.toolStrip1.Size = new System.Drawing.Size(238, 38);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnAddWeight
            // 
            this.btnAddWeight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAddWeight.Image = global::gView.Win.Dialogs.Properties.Resources.add_16;
            this.btnAddWeight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAddWeight.Name = "btnAddWeight";
            this.btnAddWeight.Size = new System.Drawing.Size(34, 33);
            this.btnAddWeight.Text = "Add Weight";
            this.btnAddWeight.Click += new System.EventHandler(this.btnAddWeight_Click);
            // 
            // btnRemoveWeight
            // 
            this.btnRemoveWeight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnRemoveWeight.Enabled = false;
            this.btnRemoveWeight.Image = global::gView.Win.Dialogs.Properties.Resources.delete_16;
            this.btnRemoveWeight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRemoveWeight.Name = "btnRemoveWeight";
            this.btnRemoveWeight.Size = new System.Drawing.Size(34, 33);
            this.btnRemoveWeight.Text = "Remove Weight";
            this.btnRemoveWeight.Click += new System.EventHandler(this.btnRemoveWeight_Click);
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(627, 80);
            this.panel1.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.gridFcs);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(250, 80);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Size = new System.Drawing.Size(377, 442);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Featureclasses";
            // 
            // gridFcs
            // 
            this.gridFcs.AllowUserToAddRows = false;
            this.gridFcs.AllowUserToDeleteRows = false;
            this.gridFcs.AllowUserToResizeRows = false;
            this.gridFcs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridFcs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridFcs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5});
            this.gridFcs.Location = new System.Drawing.Point(8, 25);
            this.gridFcs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.gridFcs.MultiSelect = false;
            this.gridFcs.Name = "gridFcs";
            this.gridFcs.RowHeadersVisible = false;
            this.gridFcs.RowHeadersWidth = 62;
            this.gridFcs.Size = new System.Drawing.Size(361, 403);
            this.gridFcs.TabIndex = 0;
            this.gridFcs.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridFcs_CellClick);
            this.gridFcs.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridFcs_CellValueChanged);
            // 
            // Column1
            // 
            this.Column1.HeaderText = "FCID";
            this.Column1.MinimumWidth = 8;
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.Visible = false;
            this.Column1.Width = 150;
            // 
            // Column2
            // 
            this.Column2.HeaderText = "FeatureClass";
            this.Column2.MinimumWidth = 8;
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            this.Column2.Width = 180;
            // 
            // Column3
            // 
            this.Column3.HeaderText = "FieldName";
            this.Column3.MinimumWidth = 8;
            this.Column3.Name = "Column3";
            this.Column3.Width = 120;
            // 
            // Column4
            // 
            this.Column4.HeaderText = "Calculations";
            this.Column4.MinimumWidth = 8;
            this.Column4.Name = "Column4";
            this.Column4.Width = 120;
            // 
            // Column5
            // 
            this.Column5.HeaderText = "...";
            this.Column5.MinimumWidth = 8;
            this.Column5.Name = "Column5";
            this.Column5.Width = 25;
            // 
            // splitter2
            // 
            this.splitter2.Location = new System.Drawing.Point(246, 80);
            this.splitter2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(4, 442);
            this.splitter2.TabIndex = 3;
            this.splitter2.TabStop = false;
            // 
            // NetworkEdgeWeightsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.splitter2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "NetworkEdgeWeightsControl";
            this.Size = new System.Drawing.Size(627, 522);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridFcs)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnAddWeight;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ToolStripButton btnRemoveWeight;
        private System.Windows.Forms.ListView lstWeights;
        private System.Windows.Forms.PropertyGrid propsWeight;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.DataGridView gridFcs;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewComboBoxColumn Column3;
        private System.Windows.Forms.DataGridViewComboBoxColumn Column4;
        private System.Windows.Forms.DataGridViewButtonColumn Column5;
    }
}
