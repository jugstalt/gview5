using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Symbology;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace gView.Framework.UI.Controls
{
    public enum TOCViewMode { Groups, Datasets }

    internal class GroupMenuItem : ToolStripMenuItem
    {
        ITocElement _element;

        public GroupMenuItem(ITocElement elem, System.EventHandler evHander)
        {
            _element = elem;

            string path = "<root>";
            if (_element != null)
            {
                if (_element.ElementType != TocElementType.ClosedGroup &&
                    _element.ElementType != TocElementType.OpenedGroup)
                {
                    return;
                }

                path = _element.Name;
                ITocElement parent = _element;
                while ((parent = parent.ParentGroup) != null)
                {
                    path = parent.Name + "/" + path;
                }
            }
            base.Text = path;

            if (evHander != null)
            {
                base.Click += evHander;
            }
        }
        public ITocElement TOCElement
        {
            get { return _element; }
        }
    }

    internal class MapItem : IRenamable, IContextType
    {
        IMap _map;

        public MapItem(IMap map)
        {
            _map = map;
        }
        public override string ToString()
        {
            if (_map == null)
            {
                return "null";
            }

            return _map.Name;
        }

        public IMap Map { get { return _map; } }

        #region IRenamable Members

        public void rename(string newName)
        {
            if (_map == null)
            {
                return;
            }

            _map.Name = newName;
        }

        #endregion

        #region IContextType Member

        public string ContextName
        {
            get { return _map.Name; }
        }

        public string ContextGroupName
        {
            get { return "Map"; }
        }

        public Type ContextType
        {
            get { return typeof(IMapContextMenuItem); }
        }

        public object ContextObject
        {
            get { return _map; }
        }

        #endregion
    }

    /*
    internal class GroupItem
    {
        string m_name;

        public GroupItem(string name) 
        {
            m_name=name;
        }
        public override string ToString()
        {
            return m_name;
        }
    }
    */

    internal interface IRenamable
    {
        void rename(string newName);
    }
    internal class DatasetItem
    {
        private IDataset m_dataset;
        private bool _isEncapsed = false;

        public DatasetItem(IDataset dataset)
        {
            m_dataset = dataset;
        }

        public bool isEncapsed
        {
            get
            {
                return _isEncapsed;
            }
            set
            {
                _isEncapsed = value;
            }
        }

        public IDataset Dataset
        {
            get { return m_dataset; }
        }
        public override string ToString()
        {
            //return m_dataset.DatasetGroupName+"/"+m_dataset.DatasetName;
            return m_dataset.DatasetName + " (" + m_dataset.DatasetGroupName + ")";
        }

    }

    internal class DatasetLayerItem
    {
        IDatasetElement m_layer;
        IDataset m_dataset;

        public DatasetLayerItem(IDatasetElement layer, IDataset dataset)
        {
            m_layer = layer;
            m_dataset = dataset;
        }
        public override string ToString()
        {
            return m_layer.Title;
        }
        public IDatasetElement Layer
        {
            get { return m_layer; }
        }
        public IDataset Dataset
        {
            get { return m_dataset; }
        }
    }

    internal class LayerItem : IRenamable, IContextType
    {
        ITocElement _element;

        public LayerItem(ITocElement element)
        {
            _element = element;
        }
        public int level
        {
            get
            {
                if (_element == null)
                {
                    return 0;
                }

                ITocElement parent = _element.ParentGroup;
                int l = 0;
                while (parent != null)
                {
                    parent = parent.ParentGroup;
                    l++;
                }
                return l;
            }
        }
        public bool Visible
        {
            get
            {
                if (_element == null)
                {
                    return false;
                }

                return _element.LayerVisible;
            }
            set
            {
                if (_element == null)
                {
                    return;
                }

                _element.LayerVisible = value;
            }
        }
        public ITocElement TOCElement
        {
            get { return _element; }
        }
        public override string ToString()
        {
            return _element.Name;
        }
        #region IRenameable Member

        public void rename(string newName)
        {
            if (_element == null)
            {
                return;
            }

            _element.Name = newName;
        }

        #endregion

        #region IContextType Member

        public string ContextName
        {
            get { return _element.Name; }
        }

        public string ContextGroupName
        {
            get { return "Layer"; }
        }

        public Type ContextType
        {
            get { return typeof(IDatasetElementContextMenuItem); }
        }

        public object ContextObject
        {
            get { return _element; }
        }

        #endregion
    }

    internal class LegendItem : IRenamable
    {
        private ILegendItem _legendItem;
        private LayerItem _layerItem;

        public LegendItem(ILegendItem legendItem, LayerItem layerItem)
        {
            _legendItem = legendItem;
            _layerItem = layerItem;
        }

        public ILegendItem legendItem
        {
            get { return _legendItem; }
        }
        public int level => _layerItem.level;

        static public gView.Framework.Symbology.ILegendGroup LegendGroup(LayerItem layerItem)
        {
            if (layerItem == null)
            {
                return null;
            }

            List<ILayer> layers = layerItem.TOCElement.Layers;
            if (layers.Count == 0)
            {
                return null;
            }

            ILayer elem = layers[0];

            if (!(elem is IFeatureLayer))
            {
                return null;
            }

            LegendGroupGroup legendGroup = new LegendGroupGroup();
            ILabelRenderer labelRenderer = ((IFeatureLayer)elem).LabelRenderer;
            if (labelRenderer is ILegendGroup)
            {
                legendGroup.Add((ILegendGroup)labelRenderer);
            }

            IFeatureRenderer renderer = ((IFeatureLayer)elem).FeatureRenderer;

            if (renderer is ILegendGroup)
            {
                legendGroup.Add((ILegendGroup)renderer);
            }

            return legendGroup.Count > 0 ? legendGroup : null;
        }

        public override string ToString()
        {
            if (_legendItem == null)
            {
                return "";
            }

            return _legendItem.LegendLabel;
        }

        public void SetSymbol(ISymbol symbol)
        {
            ILegendGroup legendGroup = LegendItem.LegendGroup(_layerItem);
            if (legendGroup == null)
            {
                return;
            }

            legendGroup.SetSymbol(_legendItem, symbol);
            if (_legendItem is ISymbol)
            {
                ((ISymbol)_legendItem).Release();
            }

            _legendItem = symbol;
        }

        #region IRenamable Members

        public void rename(string newName)
        {
            if (_legendItem != null)
            {
                _legendItem.LegendLabel = newName;
            }
        }

        #endregion

        private class LegendGroupGroup : ILegendGroup
        {
            private List<ILegendGroup> _legendGroups = new List<ILegendGroup>();

            public void Add(ILegendGroup legendGroup)
            {
                if (legendGroup != null && !_legendGroups.Contains(legendGroup))
                {
                    _legendGroups.Add(legendGroup);
                }
            }

            public int Count { get { return _legendGroups.Count; } }

            #region ILegendGroup Member

            public int LegendItemCount
            {
                get
                {
                    int count = 0;
                    foreach (ILegendGroup legendGroup in _legendGroups)
                    {
                        count += legendGroup.LegendItemCount;
                    }

                    return count;
                }
            }

            public ILegendItem LegendItem(int index)
            {
                int count = 0;
                foreach (ILegendGroup legendGroup in _legendGroups)
                {
                    if (index < legendGroup.LegendItemCount + count)
                    {
                        return legendGroup.LegendItem(index - count);
                    }

                    count += legendGroup.LegendItemCount;
                }
                return null;
            }

            public void SetSymbol(ILegendItem item, ISymbol symbol)
            {
                for (int i = 0; i < LegendItemCount; i++)
                {
                    if (item == LegendItem(i))
                    {
                        ILegendGroup legendGroup = ByIndex(i);
                        if (legendGroup != null)
                        {
                            legendGroup.SetSymbol(item, symbol);
                        }

                        break;
                    }
                }
            }

            #endregion

            private ILegendGroup ByIndex(int index)
            {
                int count = 0;
                foreach (ILegendGroup legendGroup in _legendGroups)
                {
                    if (index < legendGroup.LegendItemCount + count)
                    {
                        return legendGroup;
                    }

                    count += legendGroup.LegendItemCount;
                }
                return null;
            }
        }
    }
    internal class GroupItem : IRenamable
    {
        ITocElement _element;

        public GroupItem(ITocElement element)
        {
            _element = element;
        }
        public bool isEncapsed
        {
            get
            {
                if (_element == null)
                {
                    return false;
                }

                return _element.ElementType == TocElementType.OpenedGroup;
            }
        }
        public int level
        {
            get
            {
                if (_element == null)
                {
                    return 0;
                }

                ITocElement parent = _element.ParentGroup;
                int l = 0;
                while (parent != null)
                {
                    parent = parent.ParentGroup;
                    l++;
                }
                return l;
            }
        }
        public bool Visible
        {
            get
            {
                if (_element == null)
                {
                    return false;
                }

                return _element.LayerVisible;
            }
            set
            {
                if (_element == null)
                {
                    return;
                }

                _element.LayerVisible = value;
            }
        }
        public ITocElement TOCElement
        {
            get { return _element; }
        }
        public override string ToString()
        {
            return _element.Name;
        }
        #region IRenamable Member
        public void rename(string newName)
        {
            if (_element == null)
            {
                return;
            }

            _element.Name = newName;
        }
        #endregion
    }

    internal class MapContextMenuItem : ToolStripMenuItem
    {
        IMap _map;
        IMapContextMenuItem _item;

        public MapContextMenuItem(IMap map, IMapContextMenuItem item)
        {
            if (item == null)
            {
                return;
            }

            base.Text = item.Name;
            base.Enabled = item.Enable(map);

            _map = map;
            _item = item;
        }

        public IMap Map
        {
            get { return _map; }
        }
        public IMapContextMenuItem Item
        {
            get { return _item; }
        }
    }

    internal class LayerContextMenuItem : ToolStripMenuItem
    {
        IDatasetElement _layer = null;
        IDatasetElementContextMenuItem _tool = null;

        public LayerContextMenuItem(string name, IDatasetElement layer, IDatasetElementContextMenuItem tool)
        {
            base.Text = name;

            _layer = layer;
            _tool = tool;

            if (tool != null)
            {
                this.Image = tool.Image as Image;
            }
        }
        public LayerContextMenuItem(string name, Image image)
        {
            base.Text = name;
            this.Image = image;
        }

        public IDatasetElement Layer
        {
            get { return _layer; }
        }
        public IDatasetElementContextMenuItem Tool
        {
            get { return _tool; }
        }
    }

    internal class UnlockLayerMenuItem : ToolStripMenuItem
    {
        ITocElement _element;
        TOCControl _control;
        public UnlockLayerMenuItem(TOCControl control, ITocElement element, Image image)
        {
            _control = control;
            _element = element;

            this.Text = element.Name;
            this.Image = image;

            this.Click += new EventHandler(UnlockLayerMenuItem_Click);
        }

        async void UnlockLayerMenuItem_Click(object sender, EventArgs e)
        {
            _element.LayerLocked = false;
            await _control.BuildList(null);
        }
    }

    internal class DatasetContextMenuItemComparer : IComparer<IDatasetElementContextMenuItem>
    {
        #region IComparer<IDatasetElementContextMenuItem> Member

        public int Compare(IDatasetElementContextMenuItem x, IDatasetElementContextMenuItem y)
        {
            if (x.SortOrder < y.SortOrder)
            {
                return -1;
            }

            if (x.SortOrder > y.SortOrder)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }
}