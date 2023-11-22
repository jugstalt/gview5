using gView.Framework.Geometry;
using gView.Framework.Proj;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace gView.Interoperability.OGC.UI.Dataset.WMS
{
    public partial class EPSGSelector : UserControl
    {
        private IEpsgMetadata _metadata = null;

        public EPSGSelector()
        {
            InitializeComponent();
        }

        public IEpsgMetadata Metadata
        {
            get { return _metadata; }
            set { _metadata = value; }
        }

        private void EPSGSelector_Load(object sender, EventArgs e)
        {
            if (_metadata != null && _metadata.EpsgCodes != null)
            {
                foreach (string code in _metadata.EpsgCodes)
                {
                    lstCodes.Items.Add(code);
                }
            }
        }

        private void Commit()
        {
            if (_metadata != null)
            {
                List<string> codes = new List<string>();
                foreach (string item in lstCodes.Items)
                {
                    codes.Add(item);
                }

                _metadata.EpsgCodes = codes.ToArray();
            }
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            FormSpatialReferenceSystems dlg = new FormSpatialReferenceSystems(ProjDBTables.projs);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (dlg.SpatialRefererence != null)
                {
                    lstCodes.Items.Add(dlg.SpatialRefererence.Name);
                    Commit();
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstCodes.SelectedIndex != -1)
            {
                lstCodes.Items.RemoveAt(lstCodes.SelectedIndex);
                Commit();
            }
        }

        private void lstCodes_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtDescription.Text = String.Empty;
            if (lstCodes.SelectedItem == null)
            {
                return;
            }

            string sRefID = lstCodes.SelectedItem.ToString();
            ISpatialReference sRef = SpatialReference.FromID(sRefID);

            if (sRef != null)
            {
                txtDescription.Text = sRef.Description + "\r\n" + sRef.ToString();
            }
        }
    }
}
