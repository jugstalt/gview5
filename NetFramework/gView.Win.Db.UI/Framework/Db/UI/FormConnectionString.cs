using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace gView.Framework.Db.UI
{
    /// <summary>
    /// Zusammenfassung für FormConnectionString.
    /// </summary>
    public class FormConnectionString : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label label1;
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.ComboBox cmbConnections;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbTypes;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panelParameters;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button button1;
        private Panel panel3;
        private PictureBox pictureBox1;
        private Button btnCancel;
        private Label label3;
        private XmlDocument _config;
        private Dictionary<string, string> _values = new Dictionary<string, string>();
        private DbConnectionString _initialConnString = null;

        public FormConnectionString()
        {
            InitializeComponent();

            using (Graphics graphics = this.CreateGraphics())
            {
                this.FontScaleFactor = graphics.DpiX / 96f;
            }
        }
        public FormConnectionString(DbConnectionString initialConnString)
            : this()
        {
            _initialConnString = initialConnString;
            if (_initialConnString != null)
            {
                _useProvider = _initialConnString.UseProviderInConnectionString;
            }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConnectionString));
            this.cmbConnections = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbTypes = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.panelParameters = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbConnections
            // 
            resources.ApplyResources(this.cmbConnections, "cmbConnections");
            this.cmbConnections.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConnections.Name = "cmbConnections";
            this.cmbConnections.SelectedIndexChanged += new System.EventHandler(this.cmbConnections_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // cmbTypes
            // 
            resources.ApplyResources(this.cmbTypes, "cmbTypes");
            this.cmbTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTypes.Name = "cmbTypes";
            this.cmbTypes.SelectedIndexChanged += new System.EventHandler(this.cmbTypes_SelectedIndexChanged);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.cmbConnections);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.cmbTypes);
            this.panel1.Name = "panel1";
            // 
            // panel2
            // 
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Controls.Add(this.button1);
            this.panel2.Controls.Add(this.btnOK);
            this.panel2.Name = "panel2";
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Name = "btnOK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // panelParameters
            // 
            resources.ApplyResources(this.panelParameters, "panelParameters");
            this.panelParameters.Name = "panelParameters";
            // 
            // panel3
            // 
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.BackColor = System.Drawing.Color.White;
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.pictureBox1);
            this.panel3.Name = "panel3";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Image = global::gView.Win.Db.UI.Properties.Resources.db_connect1;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // FormConnectionString
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.panelParameters);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FormConnectionString";
            this.Load += new System.EventHandler(this.FormConnectionString_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private void FormConnectionString_Load(object sender, System.EventArgs e)
        {
            LoadXml();
        }

        private float FontScaleFactor { get; set; }

        private string _providerID = String.Empty;
        public string ProviderID
        {
            get { return _providerID; }
            set
            {
                _providerID = value;
                LoadXml();
            }
        }
        private bool _useProvider = true;
        public bool UseProviderInConnectionString
        {
            get { return _useProvider; }
            set { _useProvider = value; }
        }

        private bool _xmlLoaded = false;
        private void LoadXml()
        {
            if (_xmlLoaded)
            {
                return;
            }

            try
            {
                Type obj = typeof(CommonDbConnection);
                string configName = SystemVariables.ApplicationDirectory + @"\gView.DB.UI.xml";

                _config = new XmlDocument();
                _config.Load(configName);

                cmbConnections.Items.Clear();

                foreach (XmlNode provider in _config.SelectNodes("//provider[@id]"))
                {
                    if (!String.IsNullOrEmpty(_providerID))
                    {
                        bool found = false;
                        foreach (string providerID in _providerID.Replace(",", ";").Split(';'))
                        {
                            if (providerID.ToLower() == provider.Attributes["id"].Value.ToLower())
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            continue;
                        }
                    }

                    if (provider.Attributes["name"] == null)
                    {
                        continue;
                    }

                    cmbConnections.Items.Add(new ConnectionItem(provider));

                    if (_initialConnString != null &&
                        provider.Attributes["id"].Value == _initialConnString.ProviderId)
                    {
                        cmbConnections.SelectedIndex = cmbConnections.Items.Count - 1;
                    }
                }
                if (cmbConnections.Items.Count > 0 &&
                    cmbConnections.SelectedIndex == -1)
                {
                    cmbConnections.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }

            if (_initialConnString != null)
            {
                _xmlLoaded = true;
                _initialConnString = null;
            }
        }

        private void cmbConnections_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (cmbConnections.SelectedItem is ConnectionItem)
            {
                ConnectionItem item = (ConnectionItem)cmbConnections.SelectedItem;
                cmbTypes.Items.Clear();
                foreach (string name in item.ConnectionSchemas)
                {
                    cmbTypes.Items.Add(name);
                    if (_initialConnString != null &&
                        name == _initialConnString.SchemaName)
                    {
                        cmbTypes.SelectedIndex = cmbTypes.Items.Count - 1;
                    }
                }
                if (cmbTypes.Items.Count > 0 &&
                    cmbTypes.SelectedIndex == -1)
                {
                    cmbTypes.SelectedIndex = 0;
                }
            }
        }

        private string _connectionString = "";
        public string ConnectionString
        {
            get { return _connectionString; }
        }
        public DbConnectionString DbConnectionString
        {
            get
            {
                if (!(cmbConnections.SelectedItem is ConnectionItem) ||
                    cmbTypes.SelectedItem == null)
                {
                    return null;
                }

                DbConnectionString dbConnString = new DbConnectionString();

                dbConnString.ProviderId = ((ConnectionItem)cmbConnections.SelectedItem).ID;
                dbConnString.SchemaName = cmbTypes.SelectedItem.ToString();
                dbConnString.UseProviderInConnectionString = _useProvider;

                string[] parameters = Parameters();
                if (parameters == null)
                {
                    return null;
                }

                foreach (string param in parameters)
                {
                    foreach (Control control in panelParameters.Controls)
                    {
                        if (control is TextBox && control.Name == "box_" + param.Replace(" ", "_"))
                        {
                            dbConnString.SetUserData(param, ((TextBox)control).Text);
                        }
                    }
                }

                return dbConnString;
            }
        }
        private string[] Parameters()
        {
            if (cmbTypes.SelectedItem == null)
            {
                return null;
            }

            if (cmbConnections.SelectedItem == null)
            {
                return null;
            }

            return Parameters(((ConnectionItem)cmbConnections.SelectedItem).ConnectionSchema(cmbTypes.SelectedItem.ToString()));

        }

        private string[] Parameters(XmlNode ConnectionType)
        {
            if (ConnectionType == null)
            {
                return null;
            }

            string commandLine = _connectionString = ConnectionType.InnerText.Trim();
            int pos1 = 0, pos2;
            pos1 = commandLine.IndexOf("[");
            string parameters = "";

            while (pos1 != -1)
            {
                pos2 = commandLine.IndexOf("]", pos1);
                if (pos2 == -1)
                {
                    break;
                }

                if (parameters != "")
                {
                    parameters += ";";
                }

                parameters += commandLine.Substring(pos1 + 1, pos2 - pos1 - 1);
                pos1 = commandLine.IndexOf("[", pos2);
            }
            if (parameters != "")
            {
                return parameters.Split(';');
            }
            else
            {
                return null;
            }
        }

        private void cmbTypes_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (cmbTypes.SelectedItem == null)
            {
                return;
            }

            panelParameters.Controls.Clear();
            string[] parameters = Parameters();
            if (parameters == null)
            {
                return;
            }

            int i = 0;
            foreach (string param in parameters)
            {
                Label label = new Label();
                label.Text = param + ": ";
                label.Left = 10;
                label.Top = 10 + (int)(i * 24 * this.FontScaleFactor);
                label.TextAlign = ContentAlignment.MiddleRight;
                panelParameters.Controls.Add(label);

                TextBox box = new TextBox();
                box.Left = 112;
                box.Top = label.Top;
                box.Width = 290;
                box.Name = "box_" + param.Replace(" ", "_");

                string v;
                if (_values.TryGetValue(box.Name, out v))
                {
                    box.Text = v;
                }

                box.TextChanged += new EventHandler(box_TextChanged);
                if (param.ToLower().IndexOf("password") == 0)
                {
                    box.PasswordChar = '*';
                }
                panelParameters.Controls.Add(box);

                if (param.ToLower() == "file" || param.ToLower() == "folder")
                {
                    GetPathButton button = new GetPathButton(
                        ((param.ToLower() == "file") ?
                        GetPathButton.GetPathButtonType.file :
                        GetPathButton.GetPathButtonType.folder),
                        box
                        );
                    button.Filter = ((ConnectionItem)cmbConnections.SelectedItem).FileFilter(cmbTypes.Text);

                    panelParameters.Controls.Add(button);
                }

                if (_initialConnString != null)
                {
                    if (_initialConnString.GetUserData(param) != null)
                    {
                        box.Text = _initialConnString.GetUserData(param).ToString();
                    }
                }
                i++;
            }


            this.Height = panel1.Height + panel2.Height + panel3.Height + 60 + (int)((i * 24 + 40) * this.FontScaleFactor);
        }

        void box_TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox))
            {
                return;
            }

            TextBox tb = sender as TextBox;
            _values.Remove(tb.Name);
            _values.Add(tb.Name, tb.Text);
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            buildConnectionString(_useProvider);
        }

        private void buildConnectionString(bool useProvider)
        {
            string[] parameters = Parameters();
            if (parameters == null)
            {
                return;
            }

            _connectionString = ((useProvider) ? ((ConnectionItem)cmbConnections.SelectedItem).Provider + ":" : String.Empty)
                + _connectionString;

            foreach (string param in parameters)
            {
                foreach (Control control in panelParameters.Controls)
                {
                    if (control is TextBox && control.Name == "box_" + param.Replace(" ", "_"))
                    {
                        _connectionString = _connectionString.Replace("[" + param + "]", ((TextBox)control).Text);
                    }
                }
            }
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            buildConnectionString(true);

            DataProvider provider = new DataProvider();
            try
            {
                if (provider.Open(this.ConnectionString, true))
                {
                    MessageBox.Show("Connection succeeded...");
                    provider.Close();

                    this.Cursor = Cursors.Default;
                }
                else
                {
                    this.Cursor = Cursors.Default;
                    MessageBox.Show(provider.lastErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class ConnectionItem
    {
        XmlNode _connection;

        public ConnectionItem(XmlNode connection)
        {
            _connection = connection;
        }

        public string[] ConnectionSchemas
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (XmlNode type in _connection.SelectNodes("schema"))
                {
                    if (type.Attributes["name"] == null)
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append("|");
                    }

                    sb.Append(type.Attributes["name"].Value);
                }
                return sb.ToString().Split('|');
            }
        }

        public XmlNode ConnectionSchema(string name)
        {
            return _connection.SelectSingleNode("schema[@name='" + name + "']");
        }

        public string FileFilter(string schema)
        {
            XmlNode schemaNode = _connection.SelectSingleNode("schema[@name='" + schema + "' and @filefilter]");
            if (schemaNode == null || schemaNode.Attributes["filefilter"] == null)
            {
                return String.Empty;
            }

            return schemaNode.Attributes["filefilter"].Value;
        }

        public string Provider
        {
            get
            {
                if (_connection.Attributes["provider"] == null)
                {
                    return "";
                }

                return _connection.Attributes["provider"].Value;
            }
        }

        public string ID
        {
            get
            {
                if (_connection.Attributes["id"] == null)
                {
                    return "";
                }

                return _connection.Attributes["id"].Value;
            }
        }

        public override string ToString()
        {
            if (_connection == null)
            {
                return "";
            }

            if (_connection.Attributes["name"] == null)
            {
                return "";
            }

            return _connection.Attributes["name"].Value;
        }

    }

    internal class GetPathButton : Button
    {
        public enum GetPathButtonType { file, folder }

        private GetPathButtonType _type;
        private string _filter;
        private TextBox _box;

        public GetPathButton(GetPathButtonType type, TextBox box) : base()
        {
            _type = type;
            _box = box;

            this.Top = _box.Top;
            this.Left = _box.Left + _box.Width;
            this.Height = _box.Height;
            this.Width = 30;
            this.Text = "...";
            this.FlatStyle = FlatStyle.Popup;

            this.Click += new EventHandler(button_Click);
        }

        public string Filter
        {
            set { _filter = value; }
            get { return _filter; }
        }

        private void button_Click(object sender, System.EventArgs e)
        {
            switch (_type)
            {
                case GetPathButtonType.file:
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = _filter;
                    if (_box != null)
                    {
                        dlg.FileName = _box.Text;
                    }

                    if (dlg.ShowDialog() == DialogResult.OK && _box != null)
                    {
                        _box.Text = dlg.FileName;
                    }
                    break;
                case GetPathButtonType.folder:
                    FolderBrowserDialog fDlg = new FolderBrowserDialog();
                    if (_box != null)
                    {
                        fDlg.SelectedPath = _box.Text;
                    }

                    if (fDlg.ShowDialog() == DialogResult.OK && _box != null)
                    {
                        _box.Text = fDlg.SelectedPath;
                    }
                    break;
            }
        }
    }
}
