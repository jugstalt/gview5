using gView.DataSources.Fdb.PostgreSql;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Dialogs;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace gView.DataSources.Fdb.UI.PostgreSql
{
    public partial class FormCreatePostgreFeatureDatabase : Form
    {
        private IExplorerObject _resultExObject = null;
        private AdvancedSettings _advancedSettings = new AdvancedSettings();

        public FormCreatePostgreFeatureDatabase()
        {
            InitializeComponent();
        }

        private void chkCreateConnection_CheckedChanged(object sender, EventArgs e)
        {
            txtObject.Enabled = chkCreateConnection.Checked;
        }

        public string ConnectionString
        {
            get
            {
                return "Server=" + txtServer.Text + ";Port=" + txtPort.Text + ";Userid=" + txtUser.Text + ";Password=" + txtPassword.Text + ";Protocol=3;SSL=true; Pooling=true;MinPoolSize=3;MaxPoolSize=20;Encoding=UNICODE;Timeout=20;SslMode=Disable;";
            }
        }

        public string FullConnectionString
        {
            get
            {
                return "Server=" + txtServer.Text + ";Port=" + txtPort.Text + ";Userid=" + txtUser.Text + ";Password=" + txtPassword.Text + ";Database=" + txtDatabase.Text + ";Protocol=3;SSL=true; Pooling=true;MinPoolSize=3;MaxPoolSize=20;Encoding=UNICODE;Timeout=20;SslMode=Disable;";
            }
        }

        async private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                pgFDB fdb = new pgFDB();

                UserData parameters = _advancedSettings.ToUserData();
                if (btnOnlyRepository.Checked)
                {
                    parameters.SetUserData("CreateDatabase", false);
                }

                await fdb.Open(ConnectionString);

                if (!fdb.Create(txtDatabase.Text, parameters))
                {
                    MessageBox.Show(fdb.LastErrorMessage, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //if (chkCreateReplicationDatamodel.Checked == true)
                {
                    if (!await gView.Framework.Offline.Replication.CreateRelicationModel(fdb))
                    {
                        MessageBox.Show("RepliCreateRelicationModel failed:\n", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (chkCreateConnection.Checked)
                {
                    string connStr = FullConnectionString;
                    ConfigTextStream stream = new ConfigTextStream("postgrefdb_connections", true, true);
                    string id = txtObject.Text;
                    stream.Write(FullConnectionString, ref id);
                    stream.Close();

                    _resultExObject = new ExplorerObject(null, id, FullConnectionString);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("FATAL ERROR: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        public IExplorerObject ResultExplorerObject
        {
            get { return _resultExObject; }
        }

        private void txtDatabase_TextChanged(object sender, EventArgs e)
        {
            txtObject.Text = txtDatabase.Text;
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            FormPropertyGrid grid = new FormPropertyGrid(_advancedSettings);
            grid.ShowDialog();
        }

        #region Classes
        class AdvancedSettings
        {
            private string _filename = String.Empty;
            private string _name = String.Empty;
            int _size = 0, _maxsize = 0, _filegrowth = 0;

            [Category("File")]
            [Editor(typeof(SaveMdfFileEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public string FILENAME
            {
                get { return _filename; }
                set { _filename = value; }
            }
            [Category("Name")]
            public string NAME
            {
                get { return _name; }
                set { _name = value; }
            }
            [Category("Size")]
            public int SIZE
            {
                get { return _size; }
                set { _size = value; }
            }
            [Category("Size")]
            public int MAXSIZE
            {
                get { return _maxsize; }
                set { _maxsize = value; }
            }
            [Category("Size")]
            public int FILEGROWTH
            {
                get { return _filegrowth; }
                set { _filegrowth = value; }
            }

            public UserData ToUserData()
            {
                UserData ud = new UserData();

                if (!String.IsNullOrEmpty(_name))
                {
                    ud.SetUserData("NAME", _name);
                }

                if (!String.IsNullOrEmpty(_filename))
                {
                    ud.SetUserData("FILENAME", _filename);
                }

                if (_size > 0)
                {
                    ud.SetUserData("SIZE", _size.ToString());
                }

                if (_maxsize > 0)
                {
                    ud.SetUserData("MAXSIZE", _maxsize.ToString());
                }

                if (_filegrowth > 0)
                {
                    ud.SetUserData("FILEGROWTH", _filegrowth);
                }

                return ud;
            }
        }

        class SaveMdfFileEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.Modal;
            }

            public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "MDF File (*.mdf)|*.mdf";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
                return value;
            }
        }
        #endregion
    }
}