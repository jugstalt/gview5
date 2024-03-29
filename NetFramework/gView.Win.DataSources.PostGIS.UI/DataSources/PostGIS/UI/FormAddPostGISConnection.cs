using gView.Framework.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.DataSources.PostGIS.UI
{
    /// <summary>
    /// Zusammenfassung f�r FormAddSqlFDBDataset.
    /// </summary>
    internal class FormAddPostGISConnection : System.Windows.Forms.Form
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private TextBox txtPort;
        private Label label5;
        private string _connStr = "";

        public FormAddPostGISConnection()
        {
            //
            // Erforderlich f�r die Windows Form-Designerunterst�tzung
            //
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAddPostGISConnection));
            this.label1 = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.txtDataset = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPwd = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // txtServer
            // 
            resources.ApplyResources(this.txtServer, "txtServer");
            this.txtServer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtServer.Name = "txtServer";
            // 
            // txtDataset
            // 
            resources.ApplyResources(this.txtDataset, "txtDataset");
            this.txtDataset.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDataset.Name = "txtDataset";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // txtUser
            // 
            resources.ApplyResources(this.txtUser, "txtUser");
            this.txtUser.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtUser.Name = "txtUser";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // txtPwd
            // 
            resources.ApplyResources(this.txtPwd, "txtPwd");
            this.txtPwd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPwd.Name = "txtPwd";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Name = "button1";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Name = "button2";
            // 
            // txtPort
            // 
            resources.ApplyResources(this.txtPort, "txtPort");
            this.txtPort.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPort.Name = "txtPort";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // FormAddPostGISConnection
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtPwd);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.txtDataset);
            this.Controls.Add(this.txtServer);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FormAddPostGISConnection";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.TextBox txtDataset;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPwd;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;

        #region IDatasetEnum Member

        int _pos = 0;
        public void Reset()
        {
            _pos = 0;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            _connStr = "Server=" + txtServer.Text + ";Port=" + txtPort.Text + ";User Id=" + txtUser.Text + ";Password=" + txtPwd.Text + ";Database=" + txtDataset.Text;
        }


        private gView.Framework.Data.IDataset _dataset = null;
        async public Task<gView.Framework.Data.IDataset> Next()
        {
            if (_pos == 0)
            {
                _pos++;
                gView.DataSources.PostGIS.PostGISDataset dataset = new gView.DataSources.PostGIS.PostGISDataset();
                await dataset.SetConnectionString("Server=" + txtServer.Text + ";Port=" + txtPort.Text + ";User Id=" + txtUser.Text + ";Password=" + txtPwd.Text + ";Database=" + txtDataset.Text);
                await dataset.Open();

                // Dataset in _dataset zwischenspeichern, damit referenz bestehen bleibt
                // Dispose ist danach vom Benutzer auszuf�hren wenn das Objekt nicht mehr verwendet wird.
                // Ansonsten kann es vorkommen, dass der Garbagecolletor das Dispose ausf�hrt, wenn
                // ein Anwendung ein dataset abholt, Geoprocessing mit den Layern durchf�hrt und danach
                // das Dataset Objekt nicht mehr abholt.
                // Beim Dispose werden dann n�ml. auch die Datenbankverbingen (w�rend eines Geoprocessings)
                // abgeschnitten !!!
                return _dataset = dataset;
            }
            return null;
        }

        #endregion

        public string ConnectionString
        {
            get
            {
                return _connStr;
            }
            set
            {
                _connStr = value;
                txtServer.Text = ConfigTextStream.ExtractValue(value, "server");
                txtDataset.Text = ConfigTextStream.ExtractValue(value, "database");
                txtUser.Text = ConfigTextStream.ExtractValue(value, "uid");
                txtPwd.Text = ConfigTextStream.ExtractValue(value, "pwd");
            }
        }

    }
}
