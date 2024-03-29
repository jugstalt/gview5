using gView.DataSources.Fdb.MSAccess;
using gView.DataSources.Fdb.MSSql;
using gView.DataSources.Fdb.PostgreSql;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Db;
using gView.Framework.Db.UI;
using gView.Framework.IO;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.DataSources.Fdb.UI.MSSql
{
    public partial class FormChangeDataset : Form
    {
        private string _providerId, _connectionString, _dsname;

        public FormChangeDataset(string providerId, string connectionSring)
        {
            InitializeComponent();

            _providerId = providerId;

            _dsname = ConfigTextStream.ExtractValue(connectionSring, "dsname");
            _connectionString = ConfigTextStream.RemoveValue(connectionSring, "dsname");
        }

        async private Task BuildList()
        {
            lstDatasets.Items.Clear();
            AccessFDB fdb = null;

            switch (_providerId)
            {
                case "mssql":
                    fdb = new SqlFDB();
                    break;
                case "postgres":
                    fdb = new pgFDB();
                    break;
                case "sqlite":
                    fdb = new SQLiteFDB();
                    break;
            }

            if (fdb == null)
            {
                return;
            }

            if (!await fdb.Open(this.ConnectionString))
            {
                MessageBox.Show(fdb.LastErrorMessage, "Error");
                return;
            }

            string[] dsnames = await fdb.DatasetNames();
            if (dsnames != null)
            {
                foreach (string dsname in dsnames)
                {
                    var isImageDatasetResult = await fdb.IsImageDataset(dsname);

                    ListViewItem item = new ListViewItem(
                        dsname, isImageDatasetResult.isImageDataset ? 1 : 0);

                    lstDatasets.Items.Add(item);

                    if (item.Text == _dsname)
                    {
                        lstDatasets.SelectedIndices.Add(lstDatasets.Items.Count - 1);
                    }
                }
            }
        }

        public string ConnectionString
        {
            get
            {
                return _connectionString + ";dsname=" + _dsname;
            }
        }
        async private void FormChangeDataset_Load(object sender, EventArgs e)
        {
            await BuildList();
        }

        private void lstDatasets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDatasets.SelectedItems.Count == 1)
            {
                _dsname = lstDatasets.SelectedItems[0].Text;
                btnOK.Enabled = true;
            }
            else
            {
                btnOK.Enabled = false;
            }
        }

        async private void btnChangeConnectionString_Click(object sender, EventArgs e)
        {
            AccessFDB fdb = null;

            switch (_providerId)
            {
                case "mssql":
                    fdb = new SqlFDB();
                    break;
                case "postgres":
                    fdb = new pgFDB();
                    break;
                case "sqlite":
                    fdb = new SQLiteFDB();
                    break;
            }

            if (fdb == null)
            {
                return;
            }

            await fdb.Open(_connectionString);

            DbConnectionString dbConnStr = new DbConnectionString();
            dbConnStr.UseProviderInConnectionString = false;
            FormConnectionString dlg = (dbConnStr.TryFromConnectionString("mssql", fdb.ConnectionString) ?
                new FormConnectionString(dbConnStr) : new FormConnectionString());

            dlg.ProviderID = _providerId;
            dlg.UseProviderInConnectionString = false;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dbConnStr = dlg.DbConnectionString;
                _connectionString = dbConnStr.ConnectionString;

                await BuildList();
            }
        }
    }
}