using System;
using System.Windows.Forms;

namespace gView.Interoperability.ArcXML.Dataset
{
    public partial class FormNewConnection : Form
    {
        public FormNewConnection()
        {
            InitializeComponent();
        }

        public string ConnectionString
        {
            get
            {
                return "server=" + txtServer.Text + ";user=" + txtUser.Text + ";pwd=" + txtPwd.Text;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {

        }
    }
}