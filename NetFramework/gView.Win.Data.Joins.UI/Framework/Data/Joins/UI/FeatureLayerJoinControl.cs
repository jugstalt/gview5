using gView.Framework.UI;
using System;
using System.Windows.Forms;

namespace gView.Framework.Data.Joins.UI
{
    public partial class FeatureLayerJoinControl : UserControl, IJoinPropertyPanel, IPage
    {
        private FeatureLayerJoin _join;

        public FeatureLayerJoinControl()
        {
            InitializeComponent();
        }

        #region Events

        private void cmbJoinedLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbJoinedFeatureLayerJoinField.Items.Clear();

            IDatasetElement element = ((DatasetElementItem)cmbJoinedLayer.SelectedItem).DatasetElement;
            if (element != null && element.Class is ITableClass)
            {
                foreach (IField field in ((ITableClass)element.Class).Fields.ToEnumerable())
                {
                    cmbJoinedFeatureLayerJoinField.Items.Add(field.name);
                }
            }
        }

        #endregion

        #region Item Classes

        private class DatasetElementItem
        {
            private string _alias;

            public DatasetElementItem(IDatasetElement element, string alias)
            {
                DatasetElement = element;
                _alias = alias;
            }

            public IDatasetElement DatasetElement { get; private set; }

            public override string ToString()
            {
                return _alias;
            }
        }

        #endregion

        #region IJoinPropertyPanel Member

        public object PropertyPanel(IFeatureLayerJoin join, Framework.UI.IMapDocument mapDocument)
        {
            if (join is FeatureLayerJoin)
            {
                _join = (FeatureLayerJoin)join;

                foreach (IDatasetElement element in mapDocument.FocusMap.MapElements)
                {
                    if (/*element == layer ||*/ !(element.Class is ITableClass))
                    {
                        continue;
                    }

                    ITocElement tocElement = mapDocument.FocusMap.TOC.GetTOCElement(element as ILayer);
                    string alias = tocElement != null ? tocElement.Name : element.Title;
                    cmbJoinedLayer.Items.Add(new DatasetElementItem(element, alias));

                    if (_join.FeatureLayer != null && element.ID == _join.FeatureLayer.ID)
                    {
                        cmbJoinedLayer.SelectedIndex = cmbJoinedLayer.Items.Count - 1;
                    }
                }

                cmbJoinedFeatureLayerJoinField.SelectedItem = _join.JoinField;
            }
            return this;
        }

        #endregion

        #region IPage Member

        public void Commit()
        {
            if (_join != null)
            {
                _join.JoinField = (string)cmbJoinedFeatureLayerJoinField.SelectedItem;
                if (cmbJoinedLayer.SelectedItem is DatasetElementItem)
                {
                    _join.FeatureLayer = ((DatasetElementItem)cmbJoinedLayer.SelectedItem).DatasetElement as IFeatureLayer;
                }
            }
        }

        #endregion
    }
}
