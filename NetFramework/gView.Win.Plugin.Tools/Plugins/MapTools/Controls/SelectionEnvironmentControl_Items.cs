using gView.Framework.Data;
using gView.Framework.UI;
using System.Windows.Forms;

namespace gView.Plugins.MapTools.Controls
{
    internal class SelectionTOCLayerNode : TreeNode
    {
        private ITocElement _element = null;

        public SelectionTOCLayerNode(IMapDocument mapDocument, ITocElement element, bool check)
        {
            if (element == null)
            {
                return;
            }

            _element = element;

            base.Checked = check;
            RefreshText();

            //if (mapDocument != null && mapDocument.TableRelations != null)
            //{
            //    foreach (ILayer layer in element.Layers)
            //    {
            //        foreach (ITableRelation tableRelation in mapDocument.TableRelations.GetRelations(layer))
            //        {
            //            base.Nodes.Add(new SelectionRelationTreeNode(tableRelation, layer));
            //        }
            //    }
            //}
        }

        public ITocElement Element
        {
            get { return _element; }
        }

        public void RefreshText()
        {
            if (_element == null)
            {
                return;
            }

            base.Text = _element.Name;

            int count = 0;
            foreach (ILayer layer in _element.Layers)
            {
                if (layer is IFeatureSelection && ((IFeatureSelection)layer).SelectionSet != null)
                {
                    count += ((IFeatureSelection)layer).SelectionSet.Count;
                }
            }

            if (count != 0)
            {
                base.Text += " (" + count + " selected)";
            }
        }
    }

    internal class SelectionRelationTreeNode : TreeNode
    {
        public SelectionRelationTreeNode(ITableRelation tableRelation, IDatasetElement element)
        {
            this.TableRelation = tableRelation;
            this.DatasetElement = element;

            IDatasetElement target = (tableRelation.LeftTable == element) ? tableRelation.RightTable : tableRelation.LeftTable;
            base.Text = tableRelation.RelationName + " (" + target.Title + ")";
            base.ImageIndex = 0;
        }

        public ITableRelation TableRelation { get; set; }
        public IDatasetElement DatasetElement { get; set; }
    }
}
