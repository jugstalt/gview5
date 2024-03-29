﻿using gView.Framework.Data;
using gView.Framework.IO;
using System.Windows.Forms;

namespace gView.Framework.UI.Controls
{
    public partial class DatasetInfoControl : UserControl
    {
        private IDataset _dataset;

        public DatasetInfoControl()
        {
            InitializeComponent();
        }

        public IDataset Dataset
        {
            get
            {
                return _dataset;
            }
            set
            {
                _dataset = value;
                MakeGui();
            }
        }

        private void MakeGui()
        {
            if (_dataset != null)
            {
                txtName.Text = _dataset.DatasetName;
                txtType.Text = _dataset.GetType().ToString();
                txtGroupName.Text = _dataset.DatasetGroupName;
                txtProvider.Text = _dataset.ProviderName;

                txtConnectionString.Text = ConfigTextStream.SecureConfig(_dataset.ConnectionString);
            }
        }
    }
}
