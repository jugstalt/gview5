using gView.Framework.Carto;
using gView.Framework.Carto.UI;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.Symbology;
using gView.Framework.Symbology.UI;
using gView.Framework.Sys.UI;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Framework.UI.Controls
{
    /// <summary>
    /// Zusammenfassung für TOCControl.
    /// </summary>
    public class TOCControl : System.Windows.Forms.UserControl, IDockableWindow
    {
        private System.ComponentModel.IContainer components;
        private object _contextItem;
        private System.Windows.Forms.TextBox _renameBox;

        private ToolStripMenuItem
            _menuItemInsertGroup,
            _menuItemMoveToGroup,
            _menuItemSplitMultiLayer,
            _menuItemRemoveDatasetElement,
            _menuItemLockLayer;
        private ImageList imageList1;
        private ContextMenuStrip menuStripMap;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMapActivate;
        private ToolStripMenuItem toolStripMapNewMap;
        private ToolStripMenuItem toolStripMapDeleteMap;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ImageList iLMenuItem;
        private ContextMenuStrip menuStripFeatureLayer;
        private ToolStripMenuItem toolStripMenuUnlock;
        private ToolStripSeparator toolStripSeparator4;
        private ContextMenuStrip menuStripGroupLayer;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem menuApply;
        private ToolStripMenuItem menuGroupApplyVisibility;
        private ToolStripMenuItem menuGroupApplyScales;
        private ScrollingListBox list;
        private bool _readonly = true, _fullversion = false;
        private ToolStripMenuItem debuggingToolStripMenuItem;
        private ToolStripMenuItem menuUnreferencedLayers;
        private ContextMenuStrip contextMenuStripDataset;
        private ToolStripMenuItem connectionPropertiesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        List<IDatasetElementContextMenuItem> _contextMenuItems;

        public event EventHandler SelectionChanged = null;

        public TOCControl()
        {
            // Dieser Aufruf ist für den Windows Form-Designer erforderlich.
            InitializeComponent();

            _menuItemInsertGroup = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.InsertGroup", "Insert Group"), iLMenuItem.Images[1]);
            _menuItemInsertGroup.Click += new EventHandler(this.clickInsertGroup);
            _menuItemMoveToGroup = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.MoveToGroup", "Move to Group"), iLMenuItem.Images[2]);
            _menuItemSplitMultiLayer = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.SpitLayerGroup", "Split LayerGroup"));
            _menuItemSplitMultiLayer.Click += new System.EventHandler(this.clickSplitMultiLayer);
            _menuItemRemoveDatasetElement = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.Remove", "Remove"), iLMenuItem.Images[0]);
            _menuItemRemoveDatasetElement.Click += new EventHandler(RemoveDatasetElement_Click);
            _menuItemLockLayer = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.LockLayer", "Lock Layer"), iLMenuItem.Images[3]);
            _menuItemLockLayer.Click += new EventHandler(LockLayer_Click);
            _renameBox = new TextBox();
            _renameBox.Parent = list;
            _renameBox.Visible = false;
            _renameBox.BorderStyle = BorderStyle.FixedSingle;

            _renameBox.LostFocus += new EventHandler(renameBoxLeave);
            _renameBox.KeyPress += new KeyPressEventHandler(renameBoxKeyPress);

            _contextMenuItems = new List<IDatasetElementContextMenuItem>();
            PlugInManager compMan = new PlugInManager();
            foreach (var contextTypes in compMan.GetPlugins(Plugins.Type.IDatasetElementContextMenuItem))
            {

                IDatasetElementContextMenuItem contextMenuItem = compMan.CreateInstance<IDatasetElementContextMenuItem>(contextTypes);
                if (contextMenuItem == null)
                {
                    continue;
                }

                _contextMenuItems.Add(contextMenuItem);
            }
            _contextMenuItems.Sort(new DatasetContextMenuItemComparer());

            LocalizedResources.GlobalizeMenuItem(menuStripGroupLayer);
            LocalizedResources.GlobalizeMenuItem(menuStripMap);
            LocalizedResources.GlobalizeMenuItem(connectionPropertiesToolStripMenuItem);

            this.list.CalcFontScaleFactor();
        }

        async void LockLayer_Click(object sender, EventArgs e)
        {
            if (!(_contextItem is LayerItem))
            {
                return;
            }
            ((LayerItem)_contextItem).TOCElement.LayerLocked = true;

            _iMapDocument.TemporaryRestore();

            await BuildList(list.Items[list.Items.IndexOf(_contextItem) - 1]);
        }

        private void RemoveDatasetElement_Click(object sender, EventArgs e)
        {
            if (list.SelectedItems.Contains(_contextItem))
            {
                List<object> items = new List<object>();
                foreach (object item in list.SelectedItems)
                {
                    items.Add(item);
                }

                foreach (object item in items)
                {
                    RemoveDatasetElement(item);
                }
            }
            else
            {
                RemoveDatasetElement(_contextItem);
            }

            if (_iMapDocument != null && _iMapDocument.Application is IMapApplication)
            {
                ((IMapApplication)_iMapDocument.Application).RefreshActiveMap(DrawPhase.All);
            }

            _iMapDocument.TemporaryRestore();
        }

        private void RemoveDatasetElement(object item)
        {
            if (item is LayerItem)
            {
                foreach (ILayer layer in ((LayerItem)item).TOCElement.Layers)
                {
                    _iMapDocument.FocusMap.RemoveLayer(layer);
                }
            }
            else if (item is GroupItem)
            {
                foreach (ILayer layer in ((GroupItem)item).TOCElement.Layers)
                {
                    _iMapDocument.FocusMap.RemoveLayer(layer);
                }
            }

            _iMapDocument.TemporaryRestore();
        }

        async private void clickInsertGroup(object sender, System.EventArgs e)
        {
            if (_mode != TOCViewMode.Groups || _iMapDocument == null)
            {
                return;
            }

            if (_iMapDocument.FocusMap == null)
            {
                return;
            }

            GroupLayer gLayer = new GroupLayer();
            gLayer.Title = "New Group";

            ITocElement parent = null;
            if (_contextItem != null)
            {
                if (_contextItem is GroupItem)
                {
                    parent = ((GroupItem)_contextItem).TOCElement;
                }
                if (_contextItem is LayerItem)
                {
                    parent = ((LayerItem)_contextItem).TOCElement.ParentGroup;
                }
            }
            if (parent != null && parent.Layers.Count == 1 && parent.Layers[0] is IGroupLayer)
            {
                gLayer.GroupLayer = parent.Layers[0] as IGroupLayer;
            }
            /*
			_iMapDocument.FocusMap.TOC.AddGroup(
				"New Group",parent);
            */

            _iMapDocument.FocusMap.AddLayer(gLayer);

            await this.BuildList(null);
        }

        async private void clickMoveToGroup(object sender, System.EventArgs e)
        {
            if (!(sender is GroupMenuItem))
            {
                return;
            }

            ITocElement Group = ((GroupMenuItem)sender).TOCElement;

            foreach (object item in list.SelectedItems)
            {
                if ((item is LayerItem))
                {
                    _iMapDocument.FocusMap.TOC.Add2Group(((LayerItem)item).TOCElement, Group);
                }
                if ((item is GroupItem))
                {
                    _iMapDocument.FocusMap.TOC.Add2Group(((GroupItem)item).TOCElement, Group);
                }
            }

            _iMapDocument.TemporaryRestore();
            await this.BuildList(null);
        }
        async private void clickSplitMultiLayer(object sender, System.EventArgs e)
        {
            if (!(_contextItem is LayerItem))
            {
                return;
            }

            _iMapDocument.FocusMap.TOC.SplitMultiLayer(((LayerItem)_contextItem).TOCElement);

            _iMapDocument.TemporaryRestore();
            await this.BuildList(null);
        }
        private void clickLayerContextItem(object sender, System.EventArgs e)
        {
            if (_iMapDocument == null)
            {
                return;
            }

            if (!(sender is LayerContextMenuItem))
            {
                return;
            }

            if (((LayerContextMenuItem)sender).Tool == null)
            {
                return;
            } ((LayerContextMenuItem)sender).Tool.OnCreate(_iMapDocument);

            IDatasetElement layer = ((LayerContextMenuItem)sender).Layer;
            if (layer == null)
            {
                return;
            }

            IMap map = _iMapDocument[layer];
            if (map == null)
            {
                return;
            }

            IDataset dataset = map[layer];
            //if(dataset==null) return;

            ((LayerContextMenuItem)sender).Tool.OnEvent(
                layer, dataset);
        }
        private void clickMenuContextItem(object sender, System.EventArgs e)
        {
            if (_iMapDocument == null)
            {
                return;
            }

            if (!(sender is MapContextMenuItem))
            {
                return;
            }

            if (((MapContextMenuItem)sender).Item == null)
            {
                return;
            } ((MapContextMenuItem)sender).Item.OnCreate(_iMapDocument);
            ((MapContextMenuItem)sender).Item.OnEvent(((MapContextMenuItem)sender).Map, _iMapDocument);
        }
        async private void connectionPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_iMapDocument != null &&
                _iMapDocument.FocusMap != null &&
                _contextItem is DatasetItem)
            {
                FormConnectionProperties dlg = new FormConnectionProperties(_iMapDocument.FocusMap, ((DatasetItem)_contextItem).Dataset);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _iMapDocument.TemporaryRestore();
                    await this.BuildList(null);
                }
            }
        }

        #region Eigenschaften

        private IMapDocument _iMapDocument;
        private System.Windows.Forms.ImageList iList;
        private TOCViewMode _mode = TOCViewMode.Groups;

        public IMapDocument GetMapDocument()
        {
            return _iMapDocument;
        }
        async public Task SetMapDocumentAsync(IMapDocument value)
        {
            if (_iMapDocument == value)
            {
                return;
            }

            _iMapDocument = value;
            if (value == null)
            {
                return;
            }

            if (_iMapDocument is IMapDocumentEvents)
            {
                ((IMapDocumentEvents)_iMapDocument).LayerAdded += new LayerAddedEvent(_iMapDocument_LayerAdded);
                ((IMapDocumentEvents)_iMapDocument).LayerRemoved += new LayerRemovedEvent(_iMapDocument_LayerRemoved);
                ((IMapDocumentEvents)_iMapDocument).MapAdded += new MapAddedEvent(_iMapDocument_MapAdded);
                ((IMapDocumentEvents)_iMapDocument).MapDeleted += new MapDeletedEvent(_iMapDocument_MapDeleted);
            }

            //LicenseTypes lt = _iMapDocument.Application.ComponentLicenseType("gview.desktop;gview.map");
            //if (lt == LicenseTypes.Express || lt == LicenseTypes.Licensed)
            //{
            //    _readonly = false;
            //    _fullversion = true;
            //}
            //else
            //{
            //    _readonly = true;
            //    _fullversion = false;
            //}

            if (_iMapDocument.Application is IMapApplication)
            {
                ((IMapApplication)_iMapDocument.Application).AfterLoadMapDocument += new AfterLoadMapDocumentEvent(_iMapDocument_AfterLoadMapDocument);
                _readonly = ((IMapApplication)_iMapDocument.Application).ReadOnly;
            }

            if (_iMapDocument is IMapDocumentEvents)
            {
                ((IMapDocumentEvents)_iMapDocument).AfterSetFocusMap += new AfterSetFocusMapEvent(_iMapDocument_AfterSetFocusMap);
            }

            await BuildList(null);
        }


        public TOCViewMode TocViewMode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                if (_mode == TOCViewMode.Groups && _readonly == false)
                {
                    if (!menuStripFeatureLayer.Items.Contains(_menuItemInsertGroup))
                    {
                        menuStripFeatureLayer.Items.Add(_menuItemInsertGroup);
                    }

                    if (!menuStripFeatureLayer.Items.Contains(_menuItemMoveToGroup))
                    {
                        menuStripFeatureLayer.Items.Add(_menuItemMoveToGroup);
                    }

                    if (!menuStripFeatureLayer.Items.Contains(_menuItemRemoveDatasetElement))
                    {
                        menuStripFeatureLayer.Items.Add(_menuItemRemoveDatasetElement);
                    }

                    if (!menuStripFeatureLayer.Items.Contains(_menuItemLockLayer))
                    {
                        menuStripFeatureLayer.Items.Add(_menuItemLockLayer);
                    }
                }
                else
                {
                    if (menuStripFeatureLayer.Items.Contains(_menuItemInsertGroup))
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemInsertGroup);
                    }

                    if (menuStripFeatureLayer.Items.Contains(_menuItemMoveToGroup))
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemMoveToGroup);
                    }

                    if (menuStripFeatureLayer.Items.Contains(_menuItemRemoveDatasetElement))
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemRemoveDatasetElement);
                    }

                    if (menuStripFeatureLayer.Items.Contains(_menuItemLockLayer))
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemLockLayer);
                    }
                }
            }
        }

        #endregion

        async private void _iMapDocument_LayerAdded(IMap sender, ILayer Layer)
        {
            await BuildList(null);
        }
        async void _iMapDocument_LayerRemoved(IMap sender, ILayer layer)
        {
            await BuildList(null);
        }
        async private void _iMapDocument_MapAdded(IMap map)
        {
            await BuildList(null);
        }
        async private void _iMapDocument_MapDeleted(IMap map)
        {
            await BuildList(null);
        }
        async private void _iMapDocument_AfterLoadMapDocument(IMapDocument mapDocument)
        {
            if (mapDocument != null && mapDocument.Application is IMapApplication &&
                _fullversion == true)
            {
                _readonly = ((IMapApplication)mapDocument.Application).ReadOnly;
                this.TocViewMode = _mode;
            }
            await BuildList(null);
        }
        async void _iMapDocument_AfterSetFocusMap(IMap map)
        {
            await BuildList(null);
            if (SelectionChanged != null)
            {
                SelectionChanged(this, new EventArgs());
            }
        }

        #region List
        private delegate void RefreshListCallback();
        async public Task RefreshList()
        {
            //if (list.InvokeRequired)
            //{
            //    RefreshListCallback d = new RefreshListCallback(RefreshList);
            //    list.Invoke(d);
            //}
            //else
            {
                await BuildList(null);
            }
        }
        public void RefreshTOCElement(IDatasetElement element)
        {
            List<LayerItem> items = new List<LayerItem>();
            foreach (object item in list.Items)
            {
                LayerItem layerItem = item as LayerItem;
                if (layerItem == null || layerItem.TOCElement == null || layerItem.TOCElement.Layers == null)
                {
                    continue;
                }

                foreach (ILayer layer in layerItem.TOCElement.Layers)
                {
                    if (layer == element)
                    {
                        items.Add(layerItem);
                        break;
                    }
                }
            }

            foreach (LayerItem item in items)
            {
                if (item.TOCElement.LegendVisible)
                {
                    ShowLegendGroup(item, false);
                    ShowLegendGroup(item, true);
                }
            }
        }
        async internal Task BuildList(object FromItem)
        {
            FromItem = null;
            if (FromItem == null)
            {
                list.Items.Clear();
            }
            else
            {
                int fromIndex = list.Items.IndexOf(FromItem);
                while (list.Items.Count > Math.Max(fromIndex, 0))
                {
                    list.Items.RemoveAt(fromIndex + 1);
                }
            }

            list.HorizontalExtent = 0;

            if (_iMapDocument == null)
            {
                return;
            }

            //IEnum Maps;
            //IMap map;

            IDataset dataset;
            int dsIndex = 0;

            switch (TocViewMode)
            {
                case TOCViewMode.Datasets:
                    foreach (IMap map in _iMapDocument.Maps)
                    {
                        list.Items.Add(new MapItem(map));
                        if (!(map == _iMapDocument.FocusMap))
                        {
                            continue;
                        }

                        dsIndex = 0;
                        while ((dataset = map[dsIndex]) != null)
                        {
                            DatasetItem dsItem = new DatasetItem(dataset);

                            list.Items.Add(dsItem);
                            if (!dsItem.isEncapsed)
                            {
                                dsIndex++;
                                continue;
                            }
                            await ShowDatasetLayers(dsItem);
                            //foreach (IDatasetElement layer in dataset.Elements)
                            //{
                            //    if(layer==null) continue;
                            //    foreach (IDatasetElement mapElement in map.MapElements)
                            //    {
                            //        if (mapElement == null) continue;

                            //        if (mapElement.DatasetID == dsIndex &&
                            //            mapElement.Title == layer.Title)
                            //        {
                            //            list.Items.Add(new DatasetLayerItem(mapElement, dataset));
                            //            break;
                            //        }
                            //    }
                            //}
                            dsIndex++;
                        }
                    }
                    break;
                case TOCViewMode.Groups:
                    foreach (IMap map in _iMapDocument.Maps)
                    {
                        list.Items.Add(new MapItem(map));
                        if (!(map == _iMapDocument.FocusMap))
                        {
                            continue;
                        }

                        if (map.TOC == null)
                        {
                            continue;
                        }

                        IToc toc = map.TOC;
                        toc.Reset();
                        ITocElement elem;
                        while ((elem = toc.NextVisibleElement) != null)
                        {
                            if (elem.LayerLocked)
                            {
                                continue;
                            }

                            switch (elem.ElementType)
                            {
                                case TocElementType.Layer:
                                    LayerItem layerItem = new LayerItem(elem);
                                    list.Items.Add(layerItem);

                                    if (elem.LegendVisible)
                                    {
                                        ShowLegendGroup(layerItem, true);
                                    }

                                    break;
                                case TocElementType.OpenedGroup:
                                case TocElementType.ClosedGroup:
                                    list.Items.Add(new GroupItem(elem));
                                    break;
                                case TocElementType.Legend:
                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        private int ShowLegendGroup(LayerItem layerItem, bool show)
        {
            int index = list.Items.IndexOf(layerItem);
            if (index == -1)
            {
                return 0;
            }

            int counter = 0;

            if (LegendItem.LegendGroup(layerItem) != null && show)
            {
                ILegendGroup lGroup = LegendItem.LegendGroup(layerItem);
                for (int i = 0; i < lGroup.LegendItemCount; i++)
                {
                    ILegendItem lItem = lGroup.LegendItem(i);
                    if (lItem == null)
                    {
                        continue;
                    }

                    if (lItem.ShowInTOC)
                    {
                        list.Items.Insert(++index, new LegendItem(lItem, layerItem));
                        counter++;
                    }
                }
            }
            else if (!show)
            {
                while (list.Items.Count > index + 1 && list.Items[index + 1] is LegendItem)
                {
                    list.Items.RemoveAt(index + 1);
                    counter--;
                }
            }
            return counter;
        }
        private void ShowGroupedLayers(GroupItem groupItem)
        {
            if (groupItem == null || groupItem.TOCElement == null)
            {
                return;
            }

            int index = list.Items.IndexOf(groupItem);
            if (index == -1)
            {
                return;
            }

            if (groupItem.TOCElement.ElementType == TocElementType.ClosedGroup)
            {
                while (list.Items.Count > index + 1)
                {
                    object item = list.Items[index + 1];
                    if ((item is LayerItem && ((LayerItem)item).level <= groupItem.level) ||
                        (item is GroupItem && ((GroupItem)item).level <= groupItem.level))
                    {
                        break;
                    }
                    list.Items.RemoveAt(index + 1);
                }
            }
            else if (groupItem.TOCElement.ElementType == TocElementType.OpenedGroup)
            {
                if (_iMapDocument == null || _iMapDocument.FocusMap == null || _iMapDocument.FocusMap.TOC == null)
                {
                    return;
                }

                IToc toc = _iMapDocument.FocusMap.TOC;

                foreach (ITocElement tocElement in toc.GroupedElements(groupItem.TOCElement))
                {
                    switch (tocElement.ElementType)
                    {
                        case TocElementType.Layer:
                            LayerItem layerItem = new LayerItem(tocElement);
                            list.Items.Insert(++index, layerItem);

                            if (tocElement.LegendVisible)
                            {
                                index += ShowLegendGroup(layerItem, true);
                            }

                            break;
                        case TocElementType.OpenedGroup:
                        case TocElementType.ClosedGroup:
                            list.Items.Insert(++index, new GroupItem(tocElement));
                            break;
                    }
                }
            }
        }
        async private Task ShowDatasetLayers(DatasetItem item)
        {
            if (item == null || _iMapDocument == null || _iMapDocument.FocusMap == null || _iMapDocument.FocusMap.TOC == null)
            {
                return;
            }

            int dsIndex = 0;
            IDataset dataset;
            while ((dataset = _iMapDocument.FocusMap[dsIndex]) != null)
            {
                if (dataset == item.Dataset)
                {
                    break;
                }

                dsIndex++;
            }
            if (dataset == null)
            {
                return;
            }

            int index = list.Items.IndexOf(item);
            if (index == -1)
            {
                return;
            }

            if (item.isEncapsed)
            {
                foreach (IDatasetElement layer in await dataset.Elements())
                {
                    if (layer == null)
                    {
                        continue;
                    }

                    foreach (IDatasetElement mapElement in _iMapDocument.FocusMap.MapElements)
                    {
                        if (!(mapElement is ILayer))
                        {
                            continue;
                        }

                        if (_iMapDocument.FocusMap.TOC.GetTOCElement((ILayer)mapElement) == null)
                        {
                            continue;
                        }

                        if (mapElement.DatasetID == dsIndex &&
                            mapElement.Title == layer.Title)
                        {
                            list.Items.Insert(++index, new DatasetLayerItem(mapElement, dataset));
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = index + 1; i < list.Items.Count;)
                {
                    if (!(list.Items[i] is DatasetLayerItem))
                    {
                        break;
                    }

                    list.Items.RemoveAt(i);
                }
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code
        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TOCControl));
            this.iList = new System.Windows.Forms.ImageList(this.components);
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.menuStripFeatureLayer = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuStripMap = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMapActivate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMapNewMap = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMapDeleteMap = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuUnlock = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.debuggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUnreferencedLayers = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.iLMenuItem = new System.Windows.Forms.ImageList(this.components);
            this.menuStripGroupLayer = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.menuApply = new System.Windows.Forms.ToolStripMenuItem();
            this.menuGroupApplyVisibility = new System.Windows.Forms.ToolStripMenuItem();
            this.menuGroupApplyScales = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripDataset = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.connectionPropertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.list = new gView.Framework.UI.Controls.ScrollingListBox();
            this.menuStripMap.SuspendLayout();
            this.menuStripGroupLayer.SuspendLayout();
            this.contextMenuStripDataset.SuspendLayout();
            this.SuspendLayout();
            // 
            // iList
            // 
            this.iList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("iList.ImageStream")));
            this.iList.TransparentColor = System.Drawing.Color.Transparent;
            this.iList.Images.SetKeyName(0, "minus.gif");
            this.iList.Images.SetKeyName(1, "plus.gif");
            this.iList.Images.SetKeyName(2, "");
            this.iList.Images.SetKeyName(3, "");
            this.iList.Images.SetKeyName(4, "");
            this.iList.Images.SetKeyName(5, "dataset.png");
            this.iList.Images.SetKeyName(6, "field_geom_point.png");
            this.iList.Images.SetKeyName(7, "field_geom_line.png");
            this.iList.Images.SetKeyName(8, "field_geom_polygon.png");
            this.iList.Images.SetKeyName(9, "");
            this.iList.Images.SetKeyName(10, "opengroup19.png");
            this.iList.Images.SetKeyName(11, "closegroup19.png");
            this.iList.Images.SetKeyName(12, "");
            this.iList.Images.SetKeyName(13, "");
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "toc.png");
            this.imageList1.Images.SetKeyName(1, "cat6.png");
            // 
            // menuStripFeatureLayer
            // 
            this.menuStripFeatureLayer.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStripFeatureLayer.Name = "menuStripFeatureLayer";
            this.menuStripFeatureLayer.Size = new System.Drawing.Size(61, 4);
            // 
            // menuStripMap
            // 
            this.menuStripMap.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStripMap.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMapActivate,
            this.toolStripSeparator2,
            this.toolStripMapNewMap,
            this.toolStripMapDeleteMap,
            this.toolStripSeparator3,
            this.toolStripMenuUnlock,
            this.toolStripSeparator4,
            this.debuggingToolStripMenuItem,
            this.toolStripMenuItem1,
            this.toolStripSeparator1});
            this.menuStripMap.Name = "menuStripMap";
            this.menuStripMap.Size = new System.Drawing.Size(193, 208);
            // 
            // toolStripMapActivate
            // 
            this.toolStripMapActivate.Name = "toolStripMapActivate";
            this.toolStripMapActivate.Size = new System.Drawing.Size(192, 30);
            this.toolStripMapActivate.Text = "Activate";
            this.toolStripMapActivate.Click += new System.EventHandler(this.toolStripMapActivate_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(189, 6);
            // 
            // toolStripMapNewMap
            // 
            this.toolStripMapNewMap.Name = "toolStripMapNewMap";
            this.toolStripMapNewMap.Size = new System.Drawing.Size(192, 30);
            this.toolStripMapNewMap.Text = "New Map";
            this.toolStripMapNewMap.Click += new System.EventHandler(this.toolStripMapNewMap_Click);
            // 
            // toolStripMapDeleteMap
            // 
            this.toolStripMapDeleteMap.Name = "toolStripMapDeleteMap";
            this.toolStripMapDeleteMap.Size = new System.Drawing.Size(192, 30);
            this.toolStripMapDeleteMap.Text = "Delete Map";
            this.toolStripMapDeleteMap.Click += new System.EventHandler(this.toolStripMapDeleteMap_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(189, 6);
            // 
            // toolStripMenuUnlock
            // 
            this.toolStripMenuUnlock.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuUnlock.Image")));
            this.toolStripMenuUnlock.Name = "toolStripMenuUnlock";
            this.toolStripMenuUnlock.Size = new System.Drawing.Size(192, 30);
            this.toolStripMenuUnlock.Text = "Unlock Layer";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(189, 6);
            // 
            // debuggingToolStripMenuItem
            // 
            this.debuggingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuUnreferencedLayers});
            this.debuggingToolStripMenuItem.Name = "debuggingToolStripMenuItem";
            this.debuggingToolStripMenuItem.Size = new System.Drawing.Size(192, 30);
            this.debuggingToolStripMenuItem.Text = "Debugging";
            // 
            // menuUnreferencedLayers
            // 
            this.menuUnreferencedLayers.Name = "menuUnreferencedLayers";
            this.menuUnreferencedLayers.Size = new System.Drawing.Size(267, 30);
            this.menuUnreferencedLayers.Text = "Unreferenced Layers...";
            this.menuUnreferencedLayers.Click += new System.EventHandler(this.menuUnreferencedLayers_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(192, 30);
            this.toolStripMenuItem1.Text = "Properties";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.MapProperties_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(189, 6);
            // 
            // iLMenuItem
            // 
            this.iLMenuItem.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("iLMenuItem.ImageStream")));
            this.iLMenuItem.TransparentColor = System.Drawing.Color.Transparent;
            this.iLMenuItem.Images.SetKeyName(0, "delete_16.png");
            this.iLMenuItem.Images.SetKeyName(1, "folder-open_16.png");
            this.iLMenuItem.Images.SetKeyName(2, "folder-moveTo_16.png");
            this.iLMenuItem.Images.SetKeyName(3, "locked.png");
            this.iLMenuItem.Images.SetKeyName(4, "unlocked.png");
            // 
            // menuStripGroupLayer
            // 
            this.menuStripGroupLayer.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStripGroupLayer.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator5,
            this.menuApply});
            this.menuStripGroupLayer.Name = "menuStripGroupLayer";
            this.menuStripGroupLayer.Size = new System.Drawing.Size(132, 40);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(128, 6);
            // 
            // menuApply
            // 
            this.menuApply.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuGroupApplyVisibility,
            this.menuGroupApplyScales});
            this.menuApply.Name = "menuApply";
            this.menuApply.Size = new System.Drawing.Size(131, 30);
            this.menuApply.Text = "Apply";
            // 
            // menuGroupApplyVisibility
            // 
            this.menuGroupApplyVisibility.Name = "menuGroupApplyVisibility";
            this.menuGroupApplyVisibility.Size = new System.Drawing.Size(161, 30);
            this.menuGroupApplyVisibility.Text = "Visibility";
            this.menuGroupApplyVisibility.Click += new System.EventHandler(this.menuGroupApplyVisibility_Click);
            // 
            // menuGroupApplyScales
            // 
            this.menuGroupApplyScales.Name = "menuGroupApplyScales";
            this.menuGroupApplyScales.Size = new System.Drawing.Size(161, 30);
            this.menuGroupApplyScales.Text = "Scales";
            this.menuGroupApplyScales.Click += new System.EventHandler(this.menuGroupApplyScales_Click);
            // 
            // contextMenuStripDataset
            // 
            this.contextMenuStripDataset.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStripDataset.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectionPropertiesToolStripMenuItem});
            this.contextMenuStripDataset.Name = "contextMenuStripDataset";
            this.contextMenuStripDataset.Size = new System.Drawing.Size(255, 34);
            // 
            // connectionPropertiesToolStripMenuItem
            // 
            this.connectionPropertiesToolStripMenuItem.Name = "connectionPropertiesToolStripMenuItem";
            this.connectionPropertiesToolStripMenuItem.Size = new System.Drawing.Size(254, 30);
            this.connectionPropertiesToolStripMenuItem.Text = "ConnectionProperties";
            this.connectionPropertiesToolStripMenuItem.Click += new System.EventHandler(this.connectionPropertiesToolStripMenuItem_Click);
            // 
            // list
            // 
            this.list.AllowDrop = true;
            this.list.ColumnWidth = 500;
            this.list.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.list.HorizontalScrollbar = true;
            this.list.HorizontalScrollPos = 0;
            this.list.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.list.ItemHeight = 30;
            this.list.Location = new System.Drawing.Point(0, 0);
            this.list.Name = "list";
            this.list.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.list.Size = new System.Drawing.Size(160, 392);
            this.list.TabIndex = 0;
            this.list.VerticalScrollPos = 0;
            this.list.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.list_DrawItem);
            this.list.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.list_MeasureItem);
            this.list.SelectedIndexChanged += new System.EventHandler(this.list_SelectedIndexChanged);
            this.list.SelectedValueChanged += new System.EventHandler(this.list_SelectedValueChanged);
            this.list.DragDrop += new System.Windows.Forms.DragEventHandler(this.list_DragDrop);
            this.list.DragEnter += new System.Windows.Forms.DragEventHandler(this.list_DragEnter);
            this.list.DoubleClick += new System.EventHandler(this.list_DoubleClick);
            this.list.MouseDown += new System.Windows.Forms.MouseEventHandler(this.list_MouseDown);
            this.list.MouseMove += new System.Windows.Forms.MouseEventHandler(this.list_MouseMove);
            this.list.MouseUp += new System.Windows.Forms.MouseEventHandler(this.list_MouseUp);
            // 
            // TOCControl
            // 
            this.Controls.Add(this.list);
            this.Name = "TOCControl";
            this.Size = new System.Drawing.Size(160, 392);
            this.menuStripMap.ResumeLayout(false);
            this.menuStripGroupLayer.ResumeLayout(false);
            this.contextMenuStripDataset.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void list_MeasureItem(object sender, System.Windows.Forms.MeasureItemEventArgs e)
        {
            object item = list.Items[e.Index];
            if (item is LegendItem)
            {
                if (((LegendItem)item).legendItem == null)
                {
                    e.ItemHeight = 1;
                }
                else
                {
                    //e.ItemHeight = ((LegendItem)item).LegendGroup.LegendItemCount * 20;
                    e.ItemHeight = 20;
                }
            }
            else
            {
                e.ItemHeight = 20;
            }

            e.ItemHeight = (int)(e.ItemHeight * list.FontScaleFactor);
        }

        private int _lastLayerLevel = 0;
        private void list_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                object item = list.Items[e.Index];
                if (list.SelectedIndices.Contains(e.Index) && !(item is LegendItem))
                {
                    brush.Color = Color.DarkBlue;
                    e.Graphics.FillRectangle(brush, e.Bounds);
                    brush.Color = Color.White;
                }

                bool bold = false, italic = false;
                if (item is MapItem)
                {
                    if (((MapItem)item).ToString() == _iMapDocument.FocusMap.Name)
                    {
                        bold = true;
                    }
                }
                if (item is LayerItem)
                {
                    ITocElement tocElement = ((LayerItem)item).TOCElement;
                    if (tocElement != null && tocElement.Layers != null)
                    {
                        foreach (ILayer layer in tocElement.Layers)
                        {
                            if (layer is IWebServiceTheme)
                            {
                                italic = true;
                            }
                        }
                    }
                }

                FontStyle style = FontStyle.Regular;
                if (bold)
                {
                    style |= FontStyle.Bold;
                }

                if (italic)
                {
                    style |= FontStyle.Italic;
                }

                using (Font font = new Font("Verdana", ((item is LegendItem) ? 8 : 10), style))
                {
                    {
                        SizeF stringSize = e.Graphics.MeasureString(item.ToString(), font);

                        if (item is MapItem)
                        {
                            e.Graphics.DrawImage(
                                global::gView.Win.Dialogs.Properties.Resources.layers,
                                2, e.Bounds.Top + 2, 16, 16);
                            e.Graphics.DrawString(item.ToString(), font, brush, 19, e.Bounds.Top + 2);
                            list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, 19 + stringSize.Width);
                        }
                        else if (item is DatasetItem)
                        {
                            e.Graphics.DrawImage(iList.Images[(((DatasetItem)item).isEncapsed ? 0 : 1)], 0, e.Bounds.Top, 19, 20);
                            //e.Graphics.DrawImage(iList.Images[2], 19, e.Bounds.Top, 19, 20);
                            e.Graphics.DrawImage(iList.Images[5], 19, e.Bounds.Top, 19, 20);
                            e.Graphics.DrawString(item.ToString(), font, brush, 39, e.Bounds.Top + 2);
                            list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, 39 + stringSize.Width);
                        }
                        else if (item is LayerItem)
                        {
                            if (((LayerItem)item).TOCElement.Layers.Count > 1)
                            {
                                brush.Color = Color.Blue;
                            }

                            foreach (ILayer layer in ((LayerItem)item).TOCElement.Layers)
                            {
                                if (layer is NullLayer)
                                {
                                    brush.Color = Color.FromArgb(255, 100, 100);
                                }
                            }
                            int l = ((LayerItem)item).level * 19;
                            e.Graphics.DrawImage(iList.Images[
                                (((LayerItem)item).Visible) ? 3 : 2
                                ], l, e.Bounds.Top, 19, 20);
                            if (LegendItem.LegendGroup((LayerItem)item) != null)
                            {
                                l += 19;
                                e.Graphics.DrawImage(iList.Images[
                                    (((LayerItem)item).TOCElement.LegendVisible) ? 0 : 1
                                    ], l, e.Bounds.Top, 19, 20);
                                l -= 4;
                            }
                            e.Graphics.DrawString(item.ToString(), font, brush, l + 19, e.Bounds.Top + 2);
                            list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, l + 19 + stringSize.Width);
                            _lastLayerLevel = ((LayerItem)item).level;
                        }
                        else if (item is LegendItem)
                        {
                            if (((LegendItem)item).legendItem != null)
                            {
                                //((LegendItem)item).level = _lastLayerLevel;
                                //int l = _lastLayerLevel * 19 + 19;
                                int l = ((LegendItem)item).level * 19 + 19;

                                ILegendItem legendItem = ((LegendItem)item).legendItem;
                                if (legendItem is ISymbol)
                                {
                                    using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(30, 20))
                                    using (var canvas = bitmap.CreateCanvas())
                                    {
                                        new SymbolPreview(null).Draw(canvas, new GraphicsEngine.CanvasRectangle(0, 0, 30, 20), (ISymbol)legendItem);

                                        e.Graphics.DrawImage(bitmap.CloneToGdiBitmap(), new System.Drawing.Point(l, e.Bounds.Top));
                                    }
                                }
                                if (legendItem.LegendLabel != "")
                                {
                                    e.Graphics.DrawString(legendItem.LegendLabel, font, brush, l + 32, e.Bounds.Top + e.Bounds.Height / 2 - font.Height / 2);
                                    stringSize = e.Graphics.MeasureString(legendItem.LegendLabel, font);
                                    list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, l + 32 + stringSize.Width);
                                }
                            }
                        }
                        else if (item is GroupItem)
                        {
                            int l = ((GroupItem)item).level * 19;

                            int closed = 10, opened = 11;
                            if (isWebServiceLayer(item))
                            {
                                opened = 0;
                                closed = 1;
                            }

                            e.Graphics.DrawImage(iList.Images[((GroupItem)item).Visible ? 3 : 2],
                                l, e.Bounds.Top, 19, 20);
                            e.Graphics.DrawImage(iList.Images[
                                (((GroupItem)item).isEncapsed) ? opened : closed
                                ], l + 19, e.Bounds.Top, 19, 20);

                            e.Graphics.DrawString(item.ToString(), font, brush, l + 38, e.Bounds.Top + 2);
                            list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, l + 38 + stringSize.Width);
                        }
                        else if (item is DatasetLayerItem)
                        {
                            //if (((DatasetLayerItem)item).Layer is ILayer)
                            //{
                            //    e.Graphics.DrawImage(iList.Images[
                            //        (((ILayer)((DatasetLayerItem)item).Layer).Visible ? 3 : 2)
                            //        ], 20, e.Bounds.Top, 19, 20);
                            //}
                            //else
                            //{
                            //    e.Graphics.DrawImage(iList.Images[0], 20, e.Bounds.Top, 19, 20);
                            //}
                            int imageIndex = -1;
                            if (((DatasetLayerItem)item).Layer is IRasterLayer)
                            {
                                imageIndex = 9;
                            }
                            else
                            {
                                if (((DatasetLayerItem)item).Layer is IFeatureLayer)
                                {
                                    if (((IFeatureLayer)((DatasetLayerItem)item).Layer).FeatureClass != null)
                                    {
                                        switch (((IFeatureLayer)((DatasetLayerItem)item).Layer).FeatureClass.GeometryType)
                                        {
                                            case GeometryType.Point:
                                            case GeometryType.Multipoint:
                                                imageIndex = 6;
                                                break;
                                            case GeometryType.Polyline:
                                                imageIndex = 7;
                                                break;
                                            case GeometryType.Polygon:
                                            case GeometryType.Envelope:
                                                imageIndex = 8;
                                                break;
                                        }
                                    }
                                }
                            }
                            if (imageIndex > 0)
                            {
                                e.Graphics.DrawImage(iList.Images[imageIndex], 38, e.Bounds.Top, 19, 20);
                            }
                            e.Graphics.DrawString(item.ToString(), font, brush, 57, e.Bounds.Top + 2);
                            list.HorizontalExtent = (int)Math.Max(list.HorizontalExtent, 57 + stringSize.Width);
                        }
                    }
                }
            }
        }

        private void list_SelectedValueChanged(object sender, System.EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, new EventArgs());
            }

            list.Refresh();
        }

        private void list_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (_iMapDocument == null)
            {
                return;
            }

            if (_iMapDocument.FocusMap == null)
            {
                return;
            }

            string layers = "";
            foreach (object item in list.SelectedItems)
            {
                if (item is DatasetLayerItem)
                {
                    if (layers != "")
                    {
                        layers += ";";
                    }

                    layers += item.ToString();
                }
            }
            _iMapDocument.FocusMap.ActiveLayerNames = layers;
        }

        #region MouseEvents
        async private void list_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_mouseOverItem != null &&
                _mouseDownItem != null &&
                _mouseOverItem != _mouseDownItem)
            {
                list.SelectedIndex = -1;
                ITocElement elem = null, insertBefore = null;

                if (_mouseOverItem is LayerItem)
                {
                    insertBefore = ((LayerItem)_mouseOverItem).TOCElement;
                }
                else if (_mouseOverItem is GroupItem)
                {
                    insertBefore = ((GroupItem)_mouseOverItem).TOCElement;
                }

                if (_mouseDownItem is LayerItem)
                {
                    elem = ((LayerItem)_mouseDownItem).TOCElement;
                }
                else if (_mouseDownItem is GroupItem)
                {
                    elem = ((GroupItem)_mouseDownItem).TOCElement;
                }

                if (elem == null || insertBefore == null || elem == insertBefore)
                {
                    return;
                }

                if (_moveElementAction == MoveElementAction.addToGroup)
                {
                    _iMapDocument.FocusMap.TOC.Add2Group(elem, insertBefore);
                    _iMapDocument.TemporaryRestore();
                }
                else if (_moveElementAction == MoveElementAction.insertBefore)
                {
                    _iMapDocument.FocusMap.TOC.MoveElement(elem, insertBefore, false);
                    _iMapDocument.TemporaryRestore();
                }
                else if (_moveElementAction == MoveElementAction.insertAfter)
                {
                    _iMapDocument.FocusMap.TOC.MoveElement(elem, insertBefore, true);
                    _iMapDocument.TemporaryRestore();
                }

                await this.BuildList(null);
                await RefreshMap();
            }
        }
        async private void list_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _rect = new Rectangle(-1, -1, -1, -1);
            _mouseOverItem = _mouseDownItem = null;

            object item = null;
            for (int i = 0; i < list.Items.Count; i++)
            {
                Rectangle rect = list.GetItemRectangle(i);

                if (rect.Y <= e.Y && (rect.Y + rect.Height) >= e.Y)
                {
                    item = list.Items[i];
                    break;
                }
            }
            if (item == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                _contextItem = item;

                if (item is MapItem)
                {
                    toolStripMenuUnlock.DropDownItems.Clear();
                    if (_iMapDocument != null && _iMapDocument.FocusMap != null && _iMapDocument.FocusMap.TOC != null && _iMapDocument.FocusMap.TOC.Elements != null)
                    {
                        foreach (ITocElement element in _iMapDocument.FocusMap.TOC.Elements)
                        {
                            if (!element.LayerLocked)
                            {
                                continue;
                            }

                            toolStripMenuUnlock.DropDownItems.Add(new UnlockLayerMenuItem(this, element, iLMenuItem.Images[4]));
                        }
                    }

                    toolStripMenuUnlock.Enabled = (toolStripMenuUnlock.DropDownItems.Count > 0) && (_readonly == false);
                    toolStripMapNewMap.Enabled =
                    toolStripMapDeleteMap.Enabled =
                    toolStripMenuItem1.Enabled = (_readonly == false);

                    //for (int i = menuStripMap.Items.Count - 1; i >= 0; i--)
                    //{
                    //    if (menuStripMap.Items[i] is MapContextMenuItem)
                    //        menuStripMap.Items.Remove(menuStripMap.Items[i]);
                    //}
                    menuStripMap.Items.Clear();
                    PlugInManager pman = new PlugInManager();
                    int order = 0;
                    foreach (IMapContextMenuItem contextMenuItem in OrderedPluginList<object>.Sort(pman.GetPluginInstances(typeof(IMapContextMenuItem))))
                    {
                        if (contextMenuItem.SortOrder / 10 - order / 10 >= 1 &&
                                menuStripMap.Items.Count > 0)
                        {
                            menuStripMap.Items.Add(new ToolStripSeparator());
                        }
                        if (contextMenuItem == null || contextMenuItem.Visible(((MapItem)item).Map) == false)
                        {
                            continue;
                        }

                        MapContextMenuItem mapItem = new MapContextMenuItem(((MapItem)item).Map, contextMenuItem);
                        mapItem.Image = contextMenuItem.Image as Image;
                        mapItem.Click += new EventHandler(clickMenuContextItem);
                        menuStripMap.Items.Add(mapItem);
                    }
                    menuStripMap.Show(this, new System.Drawing.Point(e.X, e.Y));
                    return;
                }
                if (item is DatasetItem)
                {
                    contextMenuStripDataset.Show(this, new System.Drawing.Point(e.X, e.Y));
                    return;
                }
                if (item is DatasetLayerItem || item is LayerItem || item is GroupItem)
                {
                    for (int i = 0; i < menuStripFeatureLayer.Items.Count; i++)
                    {
                        if (menuStripFeatureLayer.Items[i] is LayerContextMenuItem || menuStripFeatureLayer.Items[i] is ToolStripSeparator || menuStripFeatureLayer.Items[i] == menuApply)
                        {
                            menuStripFeatureLayer.Items.Remove(menuStripFeatureLayer.Items[i]);
                            i--;
                        }
                    }
                    if (menuStripFeatureLayer.Items.IndexOf(_menuItemSplitMultiLayer) != -1)
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemSplitMultiLayer);
                    }

                    if (item is GroupItem && _readonly == false)
                    {
                        menuStripFeatureLayer.Items.Add(menuApply);
                    }
                    if (item is DatasetLayerItem && _readonly == false)
                    {
                        int order = 0;
                        foreach (IDatasetElementContextMenuItem contextMenuItem in _contextMenuItems)
                        {
                            if ((contextMenuItem.SortOrder / 10 - order / 10) > 0 &&
                                menuStripFeatureLayer.Items.Count > 0 &&
                                !(menuStripFeatureLayer.Items[menuStripFeatureLayer.Items.Count - 1] is ToolStripSeparator))
                            {
                                menuStripFeatureLayer.Items.Add(new ToolStripSeparator());
                            }
                            LayerContextMenuItem layerItem = new LayerContextMenuItem(
                                contextMenuItem.Name,
                                ((DatasetLayerItem)item).Layer,
                                contextMenuItem);
                            layerItem.Click += new EventHandler(clickLayerContextItem);

                            menuStripFeatureLayer.Items.Add(layerItem);
                            order = contextMenuItem.SortOrder;
                        }
                    }
                    if ((item is LayerItem || item is GroupItem) && _readonly == false)
                    {
                        menuStripFeatureLayer.Items.Remove(_menuItemMoveToGroup);
                        _menuItemMoveToGroup.DropDownItems.Clear();

                        foreach (ITocElement Group in _iMapDocument.FocusMap.TOC.GroupElements)
                        {
                            _menuItemMoveToGroup.DropDownItems.Add(
                                new GroupMenuItem(Group, new System.EventHandler(this.clickMoveToGroup))
                                );
                        }
                        _menuItemMoveToGroup.Enabled =
                            (_menuItemMoveToGroup.DropDownItems.Count > 0 && list.SelectedIndices.Count > 0);
                        _menuItemMoveToGroup.DropDownItems.Insert(0,
                            new GroupMenuItem(null, new System.EventHandler(this.clickMoveToGroup)));

                        int index = menuStripFeatureLayer.Items.IndexOf(_menuItemInsertGroup);
                        menuStripFeatureLayer.Items.Insert(index + 1, _menuItemMoveToGroup);
                    }
                    if (item is LayerItem || (item is GroupItem && ((GroupItem)item).TOCElement.Layers.Count > 0))
                    {
                        ITocElement tocelement = (item is LayerItem) ? ((LayerItem)item).TOCElement : ((GroupItem)item).TOCElement;

                        if (tocelement.Layers.Count > 1)
                        {
                            int index = menuStripFeatureLayer.Items.IndexOf(_menuItemInsertGroup);
                            menuStripFeatureLayer.Items.Insert(index + 2, _menuItemSplitMultiLayer);
                        }

                        List<ILayer> layers = tocelement.Layers;
                        if (layers.Count > 0)
                        {
                            int order = 0;
                            foreach (IDatasetElementContextMenuItem contextMenuItem in _contextMenuItems)
                            {
                                if ((contextMenuItem.SortOrder / 10 - order / 10) > 0 &&
                                     menuStripFeatureLayer.Items.Count > 0 &&
                                    !(menuStripFeatureLayer.Items[menuStripFeatureLayer.Items.Count - 1] is ToolStripSeparator))
                                {
                                    menuStripFeatureLayer.Items.Add(new ToolStripSeparator());
                                }
                                order = contextMenuItem.SortOrder;
                                contextMenuItem.OnCreate(_iMapDocument);
                                ToolStripMenuItem layerItem;
                                if (layers.Count == 1)
                                {
                                    IDatasetElement element = layers[0];
                                    if (!contextMenuItem.Visible(element))
                                    {
                                        continue;
                                    }

                                    layerItem = new LayerContextMenuItem(
                                        contextMenuItem.Name,
                                        element,
                                        contextMenuItem);
                                    layerItem.Enabled = contextMenuItem.Enable(element);

                                    layerItem.Click += new EventHandler(clickLayerContextItem);
                                }
                                else
                                {
                                    layerItem = new LayerContextMenuItem(contextMenuItem.Name, contextMenuItem.Image as Image);

                                    if (layers.Count > 0)
                                    {
                                        foreach (IDatasetElement layer in layers)
                                        {
                                            if (!contextMenuItem.Visible(layer))
                                            {
                                                continue;
                                            }

                                            IMap map = _iMapDocument[layer];
                                            if (map == null)
                                            {
                                                continue;
                                            }

                                            IDataset ds = map[layer];
                                            if (ds == null)
                                            {
                                                continue;
                                            }

                                            LayerContextMenuItem lItem = new LayerContextMenuItem(
                                                ds.DatasetGroupName + "/" + ds.DatasetName + ": " + layer.Title,
                                                layer,
                                                contextMenuItem);
                                            lItem.Enabled = contextMenuItem.Enable(layer);
                                            lItem.Click += new EventHandler(clickLayerContextItem);
                                            layerItem.DropDownItems.Add(lItem);
                                        }
                                    }
                                }
                                menuStripFeatureLayer.Items.Add(layerItem);
                            }
                        }
                    }
                    if (menuStripFeatureLayer.Items.Count == 0)
                    {
                        return;
                    }

                    menuStripFeatureLayer.Show(this, new System.Drawing.Point(e.X, e.Y));

                    //for (int i = 0; i < menuStripFeatureLayer.Items.Count; i++) 
                    //{
                    //    if (menuStripFeatureLayer.Items[i] is LayerContextMenuItem)
                    //    {
                    //        menuStripFeatureLayer.Items.Remove(menuStripFeatureLayer.Items[i]);
                    //        i--;
                    //    }
                    //}
                    //if (menuStripFeatureLayer.Items.IndexOf(_menuItemSplitMultiLayer) != -1)
                    //    menuStripFeatureLayer.Items.Remove(_menuItemSplitMultiLayer);
                    return;
                }
                return;
            }

            int X = e.X + list.HorizontalScrollPos;
            if (item is DatasetItem)
            {
                if (X >= 1 && X <= 10)
                {
                    ((DatasetItem)item).isEncapsed = !((DatasetItem)item).isEncapsed;
                    await ShowDatasetLayers((DatasetItem)item);
                }
            }
            if (item is DatasetLayerItem)
            {
                if (((DatasetLayerItem)item).Layer is ILayer)
                {
                    //if (X >= 19 && X <= 38)
                    //{
                    //    ((ILayer)((DatasetLayerItem)item).Layer).Visible =
                    //        !((ILayer)((DatasetLayerItem)item).Layer).Visible;
                    //    list.Refresh();

                    //    RefreshMap();

                    //    return;
                    //}
                }
            }

            if (item is LayerItem)
            {
                int l = ((LayerItem)item).level * 19;
                if (X >= l && X <= l + 19)
                {
                    ((LayerItem)item).Visible = !((LayerItem)item).Visible;
                    list.Refresh();

                    await RefreshMap();

                    return;
                }
                if (X >= l + 19 && X <= l + 38)
                {
                    if (LegendItem.LegendGroup((LayerItem)item) != null)
                    {
                        ((LayerItem)item).TOCElement.LegendVisible = !((LayerItem)item).TOCElement.LegendVisible;
                        //this.buildList(item);
                        ShowLegendGroup((LayerItem)item, ((LayerItem)item).TOCElement.LegendVisible);
                        return;
                    }
                }
            }

            if (item is GroupItem)
            {
                int l = ((GroupItem)item).level * 19;
                if (X >= l && X <= l + 19)
                {
                    ((GroupItem)item).Visible = !((GroupItem)item).Visible;
                    list.Refresh();

                    await RefreshMap();

                    return;
                }
                else if (X >= l + 20 && X <= l + 38)
                {
                    ((GroupItem)item).TOCElement.OpenCloseGroup(
                        ((GroupItem)item).TOCElement.ElementType == TocElementType.ClosedGroup);
                    ShowGroupedLayers((GroupItem)item);
                    return;
                }

                // ansonsten: vorbereiten zu verschieben...

            }
            _mouseDownItem = item;
        }
        private int _mX = 0, _mY = 0;
        private Rectangle _rect = new Rectangle(-1, -1, -1, -1);
        private object _mouseOverItem = null;
        private object _mouseDownItem = null;
        private enum MoveElementAction { none, insertBefore, insertAfter, addToGroup }
        private MoveElementAction _moveElementAction = MoveElementAction.insertBefore;

        private void list_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _mX = e.X;
            _mY = e.Y;

            if (e.Button == MouseButtons.Left && list.SelectedIndices.Count > 0 && _mouseDownItem != null)
            {
                System.Drawing.Graphics gr = null;
                Pen pen = null;
                try
                {
                    object item = null;
                    Rectangle rect = new Rectangle(0, 0, 0, 0);
                    MoveElementAction lastAction = _moveElementAction;
                    _moveElementAction = MoveElementAction.none;

                    bool insertAfter = false;
                    if (list.Items.Count > 0 &&
                        list.GetItemRectangle(list.Items.Count - 1).Bottom < e.Y)
                    {
                        rect = list.GetItemRectangle(list.Items.Count - 1);
                        item = list.Items[list.Items.Count - 1];
                        insertAfter = true;
                    }
                    else
                    {
                        for (int i = 0; i < list.Items.Count; i++)
                        {
                            rect = list.GetItemRectangle(i);
                            //if(rect==null) continue;

                            if (rect.Y <= e.Y && (rect.Y + rect.Height) >= e.Y)
                            {
                                item = list.Items[i];
                                break;
                            }
                        }
                    }

                    gr = System.Drawing.Graphics.FromHwnd(list.Handle);
                    pen = new Pen(Color.Blue, 2);

                    if (_rect != rect || lastAction != _moveElementAction)
                    {
                        pen.Color = list.BackColor;
                        DrawActionHandle(gr, pen, _rect, lastAction, 0);
                        _rect = rect;
                    }

                    if (item == null ||
                        item is LegendItem ||
                        item is MapItem)
                    {
                        return;
                    }

                    ITocElement downTocElement = null;
                    if (_mouseDownItem is LayerItem)
                    {
                        downTocElement = ((LayerItem)_mouseDownItem).TOCElement;
                        if (downTocElement != null && downTocElement.Layers != null)
                        {
                            foreach (ILayer layer in downTocElement.Layers)
                            {
                                if (layer is IWebServiceTheme)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    ITocElement overTocElement = null;
                    if (item is LayerItem)
                    {
                        overTocElement = ((LayerItem)item).TOCElement;
                        if (overTocElement != null && overTocElement.Layers != null)
                        {
                            foreach (ILayer layer in overTocElement.Layers)
                            {
                                if (layer is IWebServiceTheme)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    _mouseOverItem = item;

                    if (insertAfter)
                    {
                        _moveElementAction = MoveElementAction.insertAfter;
                    }
                    else if (_mouseOverItem is GroupItem)
                    {
                        _moveElementAction = ((e.Y - rect.Y) < rect.Height / 3) ?
                            MoveElementAction.insertBefore :
                            MoveElementAction.addToGroup;
                    }
                    else
                    {
                        _moveElementAction = ((e.Y - rect.Y) < rect.Height / 2) ?
                            MoveElementAction.insertBefore :
                            MoveElementAction.insertAfter;
                        //_moveElementAction = MoveElementAction.insertBefore;
                    }

                    pen.Color = Color.Blue;
                    DrawActionHandle(gr, pen, rect, _moveElementAction, ItemLevel(item));

                }
                finally
                {
                    if (pen != null)
                    {
                        pen.Dispose();
                    }

                    pen = null;
                    if (gr != null)
                    {
                        gr.Dispose();
                    }

                    gr = null;
                }
            }
        }

        private void DrawActionHandle(System.Drawing.Graphics gr, Pen pen, Rectangle rect, MoveElementAction action, int level)
        {
            if (action == MoveElementAction.addToGroup)
            {
                gr.DrawRectangle(pen, rect);
            }
            else if (action == MoveElementAction.insertBefore)
            {
                gr.DrawLine(pen, rect.Left + level * 19, rect.Top, rect.Width, rect.Top);
            }
            else if (action == MoveElementAction.insertAfter)
            {
                gr.DrawLine(pen, rect.Left + level * 19, rect.Bottom, rect.Width, rect.Bottom);
            }
        }
        private int ItemLevel(object item)
        {
            if (item is LayerItem)
            {
                return ((LayerItem)item).level;
            }
            else if (item is GroupItem)
            {
                return ((GroupItem)item).level;
            }
            else if (item is LegendItem)
            {
                return ((LegendItem)item).level;
            }

            return 0;
        }
        private object _renameItem = null;
        private void list_DoubleClick(object sender, System.EventArgs e)
        {
            object item = null;
            Rectangle rect = new Rectangle(0, 0, 0, 0);

            for (int i = 0; i < list.Items.Count; i++)
            {
                rect = list.GetItemRectangle(i);
                //if(rect==null) continue;

                if (rect.Y <= _mY && (rect.Y + rect.Height) >= _mY)
                {
                    item = list.Items[i];
                    break;
                }
            }
            if (item == null)
            {
                return;
            }

            if (!(item is IRenamable))
            {
                return;
            }

            _renameItem = item;

            int xOffset = 0;
            if (item is GroupItem)
            {
                xOffset = ((GroupItem)item).level * 19 + 38;
            }
            else if (item is LayerItem)
            {
                xOffset = ((LayerItem)item).level * 19 + 19 + ((LegendItem.LegendGroup((LayerItem)item) == null) ? 0 : 15);
            }
            else if (item is LegendItem)
            {
                int l = ((LegendItem)item).level * 19 + 19;
                if (_mX < l)
                {
                    return;
                }

                if (_mX >= l && _mX <= l + 30)
                {
                    ILegendItem lItem = ((LegendItem)item).legendItem;
                    if (lItem is ISymbol)
                    {
                        FormSymbol dlg = new FormSymbol((ISymbol)lItem);
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            ((LegendItem)item).SetSymbol(dlg.Symbol);
                            list.Refresh();

                            if (_iMapDocument != null && _iMapDocument.Application is IMapApplication)
                            {
                                ((IMapApplication)_iMapDocument.Application).RefreshActiveMap(DrawPhase.All);
                            }

                            _iMapDocument.TemporaryRestore();
                        }
                    }
                    return;
                }
                xOffset = l + 32;
            }
            else
            {
                return;
            }

            if (_mX < xOffset)
            {
                return;
            }

            _renameBox.Left = xOffset;
            _renameBox.Top = rect.Top;
            _renameBox.Width = rect.Width - xOffset;
            _renameBox.Height = rect.Height;
            _renameBox.Text = item.ToString();

            _renameBox.Visible = true;
            _renameBox.Focus();
        }
        #endregion

        private string GetGroupNamesPath(ITocElement elem)
        {
            if (elem.ElementType != TocElementType.ClosedGroup &&
                elem.ElementType != TocElementType.OpenedGroup)
            {
                return "";
            }

            string path = elem.Name;
            ITocElement parent = elem;
            while ((parent = parent.ParentGroup) != null)
            {
                path = parent.Name + "/" + path;
            }
            return path;
        }

        async private void renameBoxLeave(object sender, System.EventArgs e)
        {
            _renameBox.Visible = false;
            if (!(_renameItem is IRenamable))
            {
                return;
            } ((IRenamable)_renameItem).rename(_renameBox.Text);
            _renameItem = null;
            await this.BuildList(null);
        }

        async private void renameBoxKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                try
                {
                    _renameBox.Visible = false;
                    if (!(_renameItem is IRenamable))
                    {
                        return;
                    } ((IRenamable)_renameItem).rename(_renameBox.Text);
                    _renameItem = null;

                    await this.BuildList(null);
                }
                finally
                {
                    _iMapDocument.TemporaryRestore();
                }
            }
        }

        async private Task RefreshMap()
        {
            if (_iMapDocument != null &&
                            _iMapDocument.Application is IMapApplication)
            {
                await ((IMapApplication)_iMapDocument.Application).RefreshActiveMap(DrawPhase.All);
            }
        }

        public object[] SelectedItems
        {
            get
            {
                List<object> items = new List<object>();
                foreach (var item in list.SelectedItems)
                {
                    items.Add(item);
                }
                return items.ToArray();
            }
        }

        public object[] Items
        {
            get
            {
                List<object> items = new List<object>();
                foreach (var item in list.Items)
                {
                    items.Add(item);
                }
                return items.ToArray();
            }
        }

        public void SelectItem(int index)
        {
            if (list.Items.Count >= index)
            {
                list.SelectedIndex = index;
            }
        }

        #region WebServiceLayer
        private bool isWebServiceLayer(object item)
        {
            if (item is GroupItem)
            {
                if (((GroupItem)item).TOCElement.Layers.Count > 0)
                {
                    foreach (ILayer layer in ((GroupItem)item).TOCElement.Layers)
                    {
                        if (layer is IWebServiceLayer)
                        {
                            return true;
                        }
                    }
                }
            }
            else if (item is LayerItem)
            {
                if (((LayerItem)item).TOCElement.Layers.Count > 0)
                {
                    foreach (ILayer layer in ((LayerItem)item).TOCElement.Layers)
                    {
                        if (layer is IWebServiceLayer)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private bool isWebServiceTheme(object item)
        {
            if (item is LayerItem)
            {
                if (((LayerItem)item).TOCElement.Layers.Count > 0)
                {
                    foreach (ILayer layer in ((LayerItem)item).TOCElement.Layers)
                    {
                        if (layer is IWebServiceTheme)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region ContextMenu Map
        private void SpatialReferenceSystem_Click(object sender, System.EventArgs e)
        {
            if (!(_contextItem is MapItem))
            {
                return;
            }

            IMap map = null;
            foreach (IMap m in _iMapDocument.Maps)
            {
                if (m.Name == ((MapItem)_contextItem).ToString())
                {
                    map = m;
                    break;
                }
            }
            if (map == null)
            {
                return;
            }

            if (map.Display != null && map is Map)
            {
                FormSpatialReference dlg = new FormSpatialReference(map.Display.SpatialReference);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ISpatialReference sRef1 = dlg.SpatialReference;
                    ISpatialReference sRef2 = map.Display.SpatialReference;

                    if (sRef1 != null && sRef2 != null)
                    {
                        IEnvelope limit = map.Display.Limit;
                        IEnvelope env = map.Display.Envelope;

                        using (var geoTrans = gView.Framework.Geometry.GeometricTransformerFactory.Create())
                        {
                            //geoTrans.FromSpatialReference = sRef2;
                            //geoTrans.ToSpatialReference = sRef1;
                            geoTrans.SetSpatialReferences(sRef2, sRef1);

                            IGeometry limit2 = (IGeometry)geoTrans.Transform2D(limit);
                            IGeometry env2 = (IGeometry)geoTrans.Transform2D(env);
                            map.Display.Limit = limit2.Envelope;
                            ((Map)_iMapDocument.FocusMap).ZoomTo(
                                env2.Envelope.minx,
                                env2.Envelope.miny,
                                env2.Envelope.maxx,
                                env2.Envelope.maxy);
                        }
                    }
                    map.Display.SpatialReference = dlg.SpatialReference;
                }
            }
        }

        async private void MapProperties_Click(object sender, EventArgs e)
        {
            if (!(_contextItem is MapItem))
            {
                return;
            }

            IMap map = null;
            foreach (IMap m in _iMapDocument.Maps)
            {
                if (m.Name == ((MapItem)_contextItem).ToString())
                {
                    map = m;
                    break;
                }
            }
            if (map == null)
            {
                return;
            }

            FormMapProperties dlg = new FormMapProperties(_iMapDocument.Application as IMapApplication, map, map.Display);

            ISpatialReference oldSRef = map.Display.SpatialReference;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                await this.BuildList(null);

                if (_iMapDocument != null && _iMapDocument.Application is IMapApplication)
                {
                    await ((IMapApplication)_iMapDocument.Application).RefreshActiveMap(DrawPhase.All);
                }
            }
        }

        async private void toolStripMapActivate_Click(object sender, EventArgs e)
        {
            if (_iMapDocument == null || !(_contextItem is MapItem))
            {
                return;
            }

            IMap map = null;
            foreach (IMap m in _iMapDocument.Maps)
            {
                if (m.Name == ((MapItem)_contextItem).ToString())
                {
                    map = m;
                    break;
                }
            }
            if (map == null)
            {
                return;
            }

            _iMapDocument.FocusMap = map;
            await this.BuildList(null);
        }

        private void toolStripMapNewMap_Click(object sender, EventArgs e)
        {
            if (_iMapDocument == null)
            {
                return;
            }

            IMap map = new Map();
            map.Name = "Map" + (_iMapDocument.Maps.Count() + 1).ToString();

            _iMapDocument.AddMap(map);
        }

        private void toolStripMapDeleteMap_Click(object sender, EventArgs e)
        {
            if (_iMapDocument == null || !(_contextItem is MapItem))
            {
                return;
            }

            IMap map = null;
            foreach (IMap m in _iMapDocument.Maps)
            {
                if (m.Name == ((MapItem)_contextItem).ToString())
                {
                    map = m;
                    break;
                }
            }
            if (map == null)
            {
                return;
            }

            _iMapDocument.RemoveMap(map);
        }
        #endregion

        #region IDockableWindow Members

        DockWindowState _dockState = DockWindowState.left;
        public DockWindowState DockableWindowState
        {
            get
            {
                return _dockState;
            }
            set
            {
                _dockState = value;
            }
        }
        public Image Image
        {
            get
            {
                switch (TocViewMode)
                {
                    case TOCViewMode.Groups:
                        return imageList1.Images[0];
                    case TOCViewMode.Datasets:
                        return imageList1.Images[1];
                }
                return null;
            }
        }
        public new string Name
        {
            get
            {
                switch (TocViewMode)
                {
                    case TOCViewMode.Groups:
                        return "TOC";
                    case TOCViewMode.Datasets:
                        return "Source";
                }
                return "";
            }
            set { }
        }
        #endregion

        #region DragDrop
        private void list_DragEnter(object sender, DragEventArgs e)
        {
            if (_iMapDocument == null || _iMapDocument.FocusMap == null)
            {
                return;
            }

            foreach (string format in e.Data.GetFormats())
            {
                object ob = e.Data.GetData(format);

                if (ob is List<IExplorerObjectSerialization>)
                {
                    //List<IExplorerObject> exObjects = ComponentManager.DeserializeExplorerObject((List<IExplorerObjectSerialization>)ob);
                    foreach (IExplorerObjectSerialization ser in (List<IExplorerObjectSerialization>)ob)
                    {
                        if (ser.ObjectTypes.Contains(typeof(IFeatureClass)) ||
                            ser.ObjectTypes.Contains(typeof(ITableClass)) ||
                            ser.ObjectTypes.Contains(typeof(IRasterClass)) ||
                            ser.ObjectTypes.Contains(typeof(IFeatureDataset)) ||
                            ser.ObjectTypes.Contains(typeof(ITocElement)) ||
                            ser.ObjectTypes.Contains(typeof(IMap)) ||
                            ser.ObjectTypes.Contains(typeof(IMapDocument)))
                        {
                            e.Effect = DragDropEffects.Copy;
                            return;
                        }
                    }
                }
            }
        }

        async private void list_DragDrop(object sender, DragEventArgs e)
        {
            if (_iMapDocument == null || _iMapDocument.FocusMap == null)
            {
                return;
            }

            foreach (string format in e.Data.GetFormats())
            {
                object ob = e.Data.GetData(format);

                if (ob is List<IExplorerObjectSerialization>)
                {
                    ExplorerObjectManager exObjectManager = new ExplorerObjectManager();

                    List<IExplorerObject> exObjects = await exObjectManager.DeserializeExplorerObject((List<IExplorerObjectSerialization>)ob);
                    if (exObjects == null)
                    {
                        return;
                    }

                    Envelope newMapEnvelope = null;

                    bool added = false;
                    foreach (IExplorerObject exObject in exObjects)
                    {
                        bool firstLayer = _iMapDocument.FocusMap.MapElements.Count == 0;

                        var instance = await exObject?.GetInstanceAsync();
                        if (instance == null)
                        {
                            continue;
                        }

                        if (instance is IClass)
                        {
                            ILayer layer = LayerFactory.Create((IClass)instance);
                            _iMapDocument.FocusMap.AddLayer(layer);
                            added = true;

                            if (firstLayer)
                            {
                                Append2Envelope(ref newMapEnvelope, (IClass)instance);
                            }
                        }
                        else if (instance is IDataset)
                        {
                            foreach (IDatasetElement element in await ((IDataset)instance).Elements())
                            {
                                if (element.Class is IFeatureClass || element.Class is ITableClass || element.Class is IRasterClass || element.Class is IWebServiceClass)
                                {
                                    ILayer layer = LayerFactory.Create(element.Class);
                                    _iMapDocument.FocusMap.AddLayer(layer);
                                    added = true;

                                    if (firstLayer)
                                    {
                                        Append2Envelope(ref newMapEnvelope, element.Class);
                                    }
                                }
                            }
                        }
                        else if (instance is ITocElement && ((ITocElement)instance).Layers != null)
                        {
                            foreach (ILayer layer in ((ITocElement)instance).Layers)
                            {
                                if (layer is IGroupLayer)
                                {
                                    AddGroupLayer(layer as IGroupLayer, ((ITocElement)instance).TOC);
                                }
                                else
                                {
                                    // Werte aus etwaigen Grouplayern übernehen 
                                    layer.MinimumScale = layer.MinimumScale;
                                    layer.MaximumScale = layer.MaximumScale;
                                    layer.MinimumLabelScale = layer.MinimumLabelScale;
                                    layer.MaximumLabelScale = layer.MaximumLabelScale;
                                    layer.MaximumZoomToFeatureScale = layer.MaximumZoomToFeatureScale;
                                    if (layer is Layer)
                                    {
                                        ((Layer)layer).GroupLayer = null;
                                    }

                                    _iMapDocument.FocusMap.AddLayer(layer);

                                    IToc toc = ((ITocElement)instance).TOC;
                                    if (_iMapDocument.FocusMap.TOC != null && toc != null)
                                    {
                                        ITocElement newTOCElement = _iMapDocument.FocusMap.TOC.GetTOCElement(layer);
                                        ITocElement oldTOCElement = toc.GetTOCElement(layer);

                                        if (newTOCElement != null && oldTOCElement != null)
                                        {
                                            _iMapDocument.FocusMap.TOC.RenameElement(newTOCElement, oldTOCElement.Name);
                                            newTOCElement.LegendVisible = oldTOCElement.LegendVisible;
                                        }
                                    }

                                    added = true;

                                    if (firstLayer)
                                    {
                                        Append2Envelope(ref newMapEnvelope, layer.Class);
                                    }
                                }
                            }
                        }
                        else if (instance is IMap)
                        {
                            _iMapDocument.AddMap(instance as IMap);
                        }
                        else if (instance is IMapDocument &&
                            _iMapDocument.Application is IMapApplication)
                        {
                            await ((IMapApplication)_iMapDocument.Application).LoadMapDocument(exObject.FullName);
                        }
                    }
                    if (added)
                    {
                        await BuildList(null);
                        if (newMapEnvelope != null && _iMapDocument.FocusMap.Display != null)
                        {
                            _iMapDocument.FocusMap.Display.Limit = newMapEnvelope;
                            _iMapDocument.FocusMap.Display.ZoomTo(newMapEnvelope);
                        }
                        if (_iMapDocument.Application is IMapApplication)
                        {
                            await ((IMapApplication)_iMapDocument.Application).RefreshActiveMap(DrawPhase.All);
                        }
                    }
                }
            }

            _iMapDocument.TemporaryRestore();
        }

        private void AddGroupLayer(IGroupLayer gLayer, IToc toc)
        {
            if (gLayer == null)
            {
                return;
            }

            _iMapDocument.FocusMap.AddLayer(gLayer);
            if (_iMapDocument.FocusMap.TOC != null && toc != null)
            {
                ITocElement newTOCElement = _iMapDocument.FocusMap.TOC.GetTOCElement(gLayer);
                ITocElement oldTOCElement = toc.GetTOCElement(gLayer);

                if (newTOCElement != null && oldTOCElement != null)
                {
                    _iMapDocument.FocusMap.TOC.RenameElement(newTOCElement, oldTOCElement.Name);
                    newTOCElement.LegendVisible = oldTOCElement.LegendVisible;
                    newTOCElement.OpenCloseGroup(oldTOCElement.ElementType == TocElementType.OpenedGroup);
                }
            }

            foreach (ILayer layer in gLayer.ChildLayer)
            {
                if (layer is IGroupLayer)
                {
                    AddGroupLayer(layer as IGroupLayer, toc);
                }
                else
                {
                    _iMapDocument.FocusMap.AddLayer(layer);
                    if (_iMapDocument.FocusMap.TOC != null && toc != null)
                    {
                        ITocElement newTOCElement = _iMapDocument.FocusMap.TOC.GetTOCElement(layer);
                        ITocElement oldTOCElement = toc.GetTOCElement(layer);

                        if (newTOCElement != null && oldTOCElement != null)
                        {
                            _iMapDocument.FocusMap.TOC.RenameElement(newTOCElement, oldTOCElement.Name);
                            newTOCElement.LegendVisible = oldTOCElement.LegendVisible;
                        }
                    }
                }
            }
        }
        private void Append2Envelope(ref Envelope env, IClass Class)
        {
            IEnvelope cEnv = null;
            if (Class is IFeatureClass)
            {
                cEnv = ((IFeatureClass)Class).Envelope;
            }
            else if (Class is IRasterClass && ((IRasterClass)Class).Polygon != null)
            {
                cEnv = ((IRasterClass)Class).Polygon.Envelope;
            }
            else if (Class is IWebServiceClass)
            {
                cEnv = ((IWebServiceClass)Class).Envelope;
            }

            if (cEnv == null)
            {
                return;
            }

            if (env == null)
            {
                env = new Envelope(cEnv);
            }
            else
            {
                env.Union(cEnv);
            }
        }
        #endregion

        #region Menu Grouplayer Apply
        private void menuGroupApplyVisibility_Click(object sender, EventArgs e)
        {
            if (!(_contextItem is GroupItem) || ((GroupItem)_contextItem).TOCElement == null)
            {
                return;
            }

            ITocElement tocElement = ((GroupItem)_contextItem).TOCElement;
            if (tocElement.Layers.Count != 1 || !(tocElement.Layers[0] is IGroupLayer))
            {
                return;
            }

            ApplyGroupVisibility(tocElement.Layers[0] as IGroupLayer);

            list.Refresh();
        }

        private void menuGroupApplyScales_Click(object sender, EventArgs e)
        {
            if (!(_contextItem is GroupItem) || ((GroupItem)_contextItem).TOCElement == null)
            {
                return;
            }

            ITocElement tocElement = ((GroupItem)_contextItem).TOCElement;
            if (tocElement.Layers.Count != 1 || !(tocElement.Layers[0] is IGroupLayer))
            {
                return;
            }

            ApplyGroupScales(tocElement.Layers[0] as IGroupLayer);

            list.Refresh();
        }

        private void ApplyGroupVisibility(IGroupLayer gLayer)
        {
            if (gLayer == null)
            {
                return;
            }

            foreach (ILayer layer in gLayer.ChildLayer)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.Visible = gLayer.Visible;
                if (layer is IGroupLayer)
                {
                    ApplyGroupVisibility(layer as IGroupLayer);
                }
            }
        }

        private void ApplyGroupScales(IGroupLayer gLayer)
        {
            if (gLayer == null)
            {
                return;
            }

            foreach (ILayer layer in gLayer.ChildLayer)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.MinimumScale = gLayer.MinimumScale;
                layer.MaximumScale = gLayer.MaximumScale;

                layer.MinimumLabelScale = gLayer.MinimumLabelScale;
                layer.MaximumLabelScale = gLayer.MaximumLabelScale;

                layer.MaximumZoomToFeatureScale = gLayer.MaximumZoomToFeatureScale;

                if (layer is IGroupLayer)
                {
                    ApplyGroupVisibility(layer as IGroupLayer);
                }
            }
        }
        #endregion

        private void menuUnreferencedLayers_Click(object sender, EventArgs e)
        {
            if (_iMapDocument == null || _iMapDocument.FocusMap == null || _iMapDocument.FocusMap.TOC == null)
            {
                return;
            }

            IMap map = _iMapDocument.FocusMap;
            IToc toc = map.TOC;

            List<IDatasetElement> unreferenced = new List<IDatasetElement>();
            foreach (IDatasetElement layer in map.MapElements)
            {
                ITocElement tocElement = null;
                if (layer is ILayer)
                {
                    tocElement = toc.GetTOCElement((ILayer)layer);
                }
                else
                {
                    tocElement = null; //toc.GetTOCElement(layer.Class);
                }

                if (tocElement == null)
                {
                    unreferenced.Add(layer);
                }
            }

            if (unreferenced.Count == 0)
            {
                MessageBox.Show("Keine unreferenzierten Layer vorhanden...");
                return;
            }
            FormDeleteDatasetElements dlg = new FormDeleteDatasetElements(unreferenced);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (IDatasetElement element in dlg.Selected)
                {
                    if (element is ILayer)
                    {
                        map.RemoveLayer((ILayer)element);
                    }
                    else
                    {
                        map.MapElements.Remove(element);
                    }
                }
            }
        }

    }
}
