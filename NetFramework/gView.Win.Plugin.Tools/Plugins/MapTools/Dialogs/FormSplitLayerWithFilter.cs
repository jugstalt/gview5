using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Dialogs
{
    internal partial class FormSplitLayerWithFilter : Form
    {
        private ILayer _layer = null;

        public FormSplitLayerWithFilter(IMapDocument doc, ILayer layer)
        {
            InitializeComponent();

            if (!(layer.Class is IFeatureClass))
            {
                return;
            }

            _layer = layer;

            cmbOperator.SelectedIndex = 0;

            foreach (IField field in ((IFeatureClass)layer.Class).Fields.ToEnumerable())
            {
                if (field == null)
                {
                    continue;
                }

                cmbFilterField.Items.Add(new FieldItem(field));
            }
            if (cmbFilterField.Items.Count > 0)
            {
                cmbFilterField.SelectedIndex = 0;
            }

            string title = _layer.Title;
            if (doc != null && doc.FocusMap != null && doc.FocusMap.TOC != null)
            {
                ITocElement tocElement = doc.FocusMap.TOC.GetTOCElement(_layer);
                if (tocElement != null)
                {
                    title = tocElement.Name;
                }
            }
            txtTemplate.Text = title + " [VALUE]";
        }

        #region Properties
        public List<FilterExpessionItem> FilterExpressions
        {
            get
            {
                List<FilterExpessionItem> expressions = new List<FilterExpessionItem>();

                foreach (FilterExpessionItem item in lstLayerNames.CheckedItems)
                {
                    expressions.Add(item);
                }

                return expressions;
            }
        }
        #endregion

        #region ItemClasses
        private class FieldItem
        {
            IField _field;
            public FieldItem(IField field)
            {
                _field = field;
            }

            public IField Field
            {
                get { return _field; }
            }

            public override string ToString()
            {
                return gView.Framework.Data.Field.WhereClauseFieldName(Field.name);
            }
        }

        public class FilterExpessionItem
        {
            string _text, _filter;
            public FilterExpessionItem(string text, string filter)
            {
                _text = text;
                _filter = filter;
            }

            public string Text
            {
                get { return _text; }
            }
            public string Filter
            {
                get { return _filter; }
            }

            public override string ToString()
            {
                return _text;
            }
        }
        #endregion

        #region Events
        private void cmbFilterField_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbOperator.Visible = numOperator.Visible = false;

            if (cmbFilterField.SelectedItem is FieldItem)
            {
                switch (((FieldItem)cmbFilterField.SelectedItem).Field.type)
                {
                    case FieldType.integer:
                    case FieldType.smallinteger:
                    case FieldType.biginteger:
                    case FieldType.Double:
                    case FieldType.Float:
                    case FieldType.ID:
                        cmbOperator.Visible = numOperator.Visible = true;
                        break;
                }
            }
        }

        async private void btnApply_Click(object sender, EventArgs e)
        {
            if (!txtTemplate.Text.Contains("[VALUE]"))
            {
                txtTemplate.Text += "[VALUE]";
            }

            lstLayerNames.Items.Clear();
            string fieldName = cmbFilterField.Text.Replace("[", String.Empty).Replace("]", String.Empty); // Joined fields [...]. Dont need brackets in Distinctfilter...
            DistinctFilter filter = new DistinctFilter(fieldName);
            filter.OrderBy = fieldName;
            using (IFeatureCursor cursor = await ((IFeatureClass)_layer.Class).Search(filter) as IFeatureCursor)
            {
                if (cursor == null)
                {
                    return;
                }

                IFeature feature;
                while ((feature = await cursor.NextFeature()) != null)
                {
                    object oobj;
                    object obj = oobj = feature[fieldName];
                    if (obj == null && fieldName.Contains(":"))
                    {
                        obj = oobj = feature[fieldName.Split(':')[1]];
                    }

                    if (obj == null || obj == DBNull.Value)
                    {
                        continue;
                    }

                    if (numOperator.Visible)
                    {
                        try
                        {
                            double d = Convert.ToDouble(obj);
                            switch (cmbOperator.Text)
                            {
                                case "+":
                                    d += (double)numOperator.Value;
                                    break;
                                case "-":
                                    d -= (double)numOperator.Value;
                                    break;
                                case "*":
                                    d *= (double)numOperator.Value;
                                    break;
                                case "/":
                                    d /= (double)numOperator.Value;
                                    break;
                            }

                            obj = d;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("ERROR: " + ex.Message);
                            return;
                        }
                    }

                    lstLayerNames.Items.Add(
                        new FilterExpessionItem(
                            txtTemplate.Text.Replace("[VALUE]", obj.ToString()),
                            cmbFilterField.Text + "=" + Quote(oobj.ToString())), true);
                }
            }
        }

        private void btnCheckAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstLayerNames.Items.Count; i++)
            {
                lstLayerNames.SetItemChecked(i, true);
            }
        }

        private void btnUncheckAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstLayerNames.Items.Count; i++)
            {
                lstLayerNames.SetItemChecked(i, false);
            }
        }
        #endregion

        #region Helper
        private string Quote(string str)
        {
            if (cmbFilterField.SelectedItem is FieldItem)
            {
                switch (((FieldItem)cmbFilterField.SelectedItem).Field.type)
                {
                    case FieldType.character:
                    case FieldType.String:
                    case FieldType.Date:
                        return "'" + str + "'";
                }
            }

            return str;
        }
        #endregion
    }
}