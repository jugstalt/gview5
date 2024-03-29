using gView.Framework.Data;
using gView.Framework.Data.Filters;
using System;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs
{
    [gView.Framework.system.RegisterPlugIn("F3C6A9D8-8354-495f-981B-1F3C295BD8BD")]
    public partial class FormLayerFilter : Form, ILayerPropertyPage
    {
        private IFeatureLayer _layer = null;

        public FormLayerFilter()
        {
            InitializeComponent();
        }

        #region ILayerPropertyPage Member

        public Panel PropertyPage(IDataset dataset, gView.Framework.Data.ILayer layer)
        {
            _layer = layer as IFeatureLayer;

            if (_layer != null && _layer.FilterQuery != null)
            {
                if (!String.IsNullOrEmpty(_layer.FilterQuery.JsonWhereClause))
                {
                    txtExpression.Text = _layer.FilterQuery.JsonWhereClause;
                }
                else
                {
                    txtExpression.Text = _layer.FilterQuery.WhereClause;
                }
            }
            return panelPage;
        }

        public bool ShowWith(IDataset dataset, gView.Framework.Data.ILayer layer)
        {
            return (layer is IFeatureLayer &&
                layer.Class is IFeatureClass);
        }

        public string Title
        {
            get { return "Filter"; }
        }

        public void Commit()
        {
            if (_layer == null)
            {
                return;
            }

            QueryFilter filter = new QueryFilter();
            filter.WhereClause = txtExpression.Text;

            _layer.FilterQuery = filter;
        }

        #endregion

        async private void btnQueryBuilder_Click(object sender, EventArgs e)
        {
            if (_layer == null)
            {
                return;
            }

            FormQueryBuilder dlg = await FormQueryBuilder.CreateAsync(_layer);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtExpression.Text = dlg.whereClause;
            }
        }
    }
}