﻿using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.UI.Controls.Wizard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs.Network
{
    public partial class ComplexEdgesControl : UserControl, IWizardPageNotification
    {
        private IFeatureDatabase3 _database = null;
        private SelectFeatureclassesControl _selected;

        public ComplexEdgesControl(IFeatureDataset dataset, SelectFeatureclassesControl selected)
        {
            InitializeComponent();

            if (dataset != null)
            {
                _database = dataset.Database as IFeatureDatabase3;
            }

            //if (_database == null)
            //    throw new ArgumentException();

            _selected = selected;
        }

        private void chkCreateComplexEdges_CheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Enabled = chkCreateComplexEdges.Checked;
        }

        #region IWizardPageNotification Member

        async public Task OnShowWizardPage()
        {
            if (_selected != null)
            {
                List<int> fcIds = await this.ComplexEdgeFcIds();
                lstEdges.Items.Clear();

                foreach (IFeatureClass fc in _selected.EdgeFeatureclasses)
                {
                    SelectFeatureclassesControl.FcListViewItem item = new SelectFeatureclassesControl.FcListViewItem(fc);
                    int fcId = await _database.GetFeatureClassID(fc.Name);
                    if (fcIds.Contains(fcId))
                    {
                        item.Checked = true;
                    }

                    lstEdges.Items.Add(item);
                }
            }
        }

        #endregion

        #region Properties

        async public Task<List<int>> ComplexEdgeFcIds()
        {
            List<int> fcIds = new List<int>();

            if (chkCreateComplexEdges.Checked == true)
            {
                foreach (SelectFeatureclassesControl.FcListViewItem item in lstEdges.Items)
                {
                    if (item.Checked == true)
                    {
                        fcIds.Add(await _database.GetFeatureClassID(item.Featureclass.Name));
                    }
                }
            }

            return fcIds;
        }

        public Serialized Serialize
        {
            get
            {
                List<string> names = new List<string>();

                foreach (SelectFeatureclassesControl.FcListViewItem item in lstEdges.Items)
                {
                    if (item.Checked == true)
                    {
                        names.Add(item.Featureclass.Name);
                    }
                }

                return new Serialized()
                {
                    UserComplexEdges = chkCreateComplexEdges.Checked,
                    ComplexEdgeNames = names.ToArray()
                };
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                chkCreateComplexEdges.Checked = value.UserComplexEdges;

                foreach (SelectFeatureclassesControl.FcListViewItem item in lstEdges.Items)
                {
                    item.Checked = false;
                }

                foreach (var name in value.ComplexEdgeNames)
                {
                    foreach (SelectFeatureclassesControl.FcListViewItem item in lstEdges.Items)
                    {
                        if (item.Featureclass.Name == name)
                        {
                            item.Checked = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Serializer Class

        public class Serialized
        {
            [JsonProperty(PropertyName = "user_complexedges")]
            public bool UserComplexEdges { get; set; }

            [JsonProperty(PropertyName = "complexedges")]
            public string[] ComplexEdgeNames { get; set; }
        }

        #endregion
    }
}
