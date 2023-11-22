using gView.Framework.Geometry;
using gView.Framework.Proj;
using System;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs
{
    /// <summary>
    /// Zusammenfassung für FormSpatialReferenceSystems.
    /// </summary>
    public class FormSpatialReferenceSystems : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtWhere;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Button btnOK;
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private ProjDBTables _table;
        private string _filter = "";

        public FormSpatialReferenceSystems(ProjDBTables table)
        {
            //
            // Erforderlich für die Windows Form-Designerunterstützung
            //
            InitializeComponent();
            _table = table;
        }
        public FormSpatialReferenceSystems(ProjDBTables table, string filter)
            : this(table)
        {
            _filter = filter;
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
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSpatialReferenceSystems));
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnQuery = new System.Windows.Forms.Button();
            this.txtWhere = new System.Windows.Forms.TextBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Controls.Add(this.btnQuery);
            this.panel1.Controls.Add(this.txtWhere);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnQuery
            // 
            resources.ApplyResources(this.btnQuery, "btnQuery");
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Click += new System.EventHandler(this.btnQuery_Click);
            // 
            // txtWhere
            // 
            this.txtWhere.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.txtWhere, "txtWhere");
            this.txtWhere.Name = "txtWhere";
            // 
            // listView1
            // 
            this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            resources.ApplyResources(this.listView1, "listView1");
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // FormSpatialReferenceSystems
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "FormSpatialReferenceSystems";
            this.Load += new System.EventHandler(this.FormSpatialReferenceSystems_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private void FormSpatialReferenceSystems_Load(object sender, System.EventArgs e)
        {
            ProjDB db = new ProjDB(_table);

            DataTable tab = (_table == ProjDBTables.projs) ?
                db.GetTable(_filter) :
                db.GetDatumTable(_filter);

            if (tab == null)
            {
                MessageBox.Show(db.errMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BuildList(tab);
            db.Dispose();
        }

        private void btnQuery_Click(object sender, System.EventArgs e)
        {
            ProjDB db = new ProjDB(_table);
            DataTable tab;
            if (_table == ProjDBTables.projs)
            {
                tab = db.GetTable("(PROJ_ID like '%" + txtWhere.Text + "%' OR PROJ_DESCRIPTION like '%" + txtWhere.Text + "%')" + ((_filter != String.Empty) ? " AND (" + _filter + ")" : ""));
            }
            else
            {
                tab = db.GetDatumTable("DATUM_Name like '%" + txtWhere.Text + "%'");
            }

            db.Dispose();

            if (tab == null)
            {
                MessageBox.Show(db.errMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BuildList(tab);
        }

        private void BuildList(DataTable table)
        {
            listView1.Items.Clear();
            foreach (DataRow row in table.Rows)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("|");
                    }

                    sb.Append(row[i].ToString());
                }
                listView1.Items.Add(new ListViewItem(sb.ToString().Split('|')));
            }
        }

        private ISpatialReference _spatialRef = null;

        public ISpatialReference SpatialRefererence
        {
            get
            {
                return _spatialRef;
            }
        }

        private IGeodeticDatum _datum = null;

        public IGeodeticDatum GeodeticDatum
        {
            get
            {
                return _datum;
            }
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }
            ListViewItem item = listView1.SelectedItems[0];

            switch (_table)
            {
                case ProjDBTables.projs:
                    _spatialRef = new SpatialReference(item.Text);
                    break;
                case ProjDBTables.datums:
                    _datum = new GeodeticDatum(item.Text);
                    break;
            }
        }
    }
}
