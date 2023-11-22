using gView.Desktop.Wpf;
using gView.Desktop.Wpf.Carto;
using gView.Desktop.Wpf.Controls;
using gView.Desktop.Wpf.Items;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Controls;
using gView.Framework.UI.Events;
using gView.Win.Carto.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Xceed.Wpf.AvalonDock.Layout;

namespace gView.Win.Carto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow, IGUIAppWindow, IDocumentWindow, System.Windows.Forms.IWin32Window
    {
        private gView.Framework.UI.MapDocument _mapDocument;
        private MapApplication _mapApplication;
        private ICheckAbleButton _activeTool = null;
        private List<ToolToggleButton> _toolButtons = new List<ToolToggleButton>();
        private List<ToolbarStrip> _toolBars = new List<ToolbarStrip>();
        private System.Windows.Forms.ContextMenuStrip _contextMenuStripMapView;
        private List<IMapApplicationModule> _modules = new List<IMapApplicationModule>();
        private gView.Framework.UI.Controls.TOCControl _toc = null;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    float displayDpi = g.DpiX;

                    Engines.RegisterGraphcisEngine(new GraphicsEngine.GdiPlus.GdiGraphicsEngine(displayDpi));
                    Engines.RegisterGraphcisEngine(GraphicsEngine.Current.Engine = new GraphicsEngine.Skia.SkiaGraphicsEngine(displayDpi));

                    GraphicsEngine.Current.Encoder = new gView.GraphicsEngine.GdiPlus.GdiBitmapEncoding();

                    if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                    {
                        // Method don't work on RemoteSessions
                        //SystemVariables.SystemFontsScaleFactor = 1f;

                        var systemFontScaleFactor = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / (float)SystemParameters.PrimaryScreenWidth;
                        SystemVariables.SystemFontsScaleFactor = systemFontScaleFactor;
                    }
                    else
                    {
                        var systemFontScaleFactor = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / (float)SystemParameters.PrimaryScreenWidth;
                        SystemVariables.SystemFontsScaleFactor = systemFontScaleFactor;
                    }
                }

                #region Create Application

                _mapApplication = new MapApplication(this, cartoDocPane);
                _mapApplication.DockWindowAdded += new DockWindowAddedEvent(mapApplication_DockWindowAdded);
                _mapApplication.OnShowDockableWindow += new OnShowDockableWindowEvent(mapApplication_OnShowDockableWindow);

                #endregion

                #region Create Document

                _mapDocument = new gView.Framework.UI.MapDocument(_mapApplication);
                _mapDocument.DocumentWindow = this;
                _mapDocument.MapAdded += new MapAddedEvent(_mapApplication.MapAddedToDocument);
                _mapDocument.MapScaleChanged += new MapScaleChangedEvent(_mapDocument_MapScaleChanged);

                #endregion

                _mapApplication.mapDocument = _mapDocument;
                _mapApplication.DocumentFilename = String.Empty;

                // Erst alle Tools erzeugen
                PlugInManager pm = new PlugInManager();

                foreach (var toolType in pm.GetPlugins(Framework.system.Plugins.Type.ITool))
                {
                    try
                    {
                        _mapApplication.AddTool(pm.CreateInstance<ITool>(toolType));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error creating Instance: " + toolType.ToString() + "\n" + ex.Message);
                    }
                }

                //AddDataView("DataView1", new Map());
                //_mapDocument.FocusMap = _mapApplication.ActiveDataView.Map;
                _mapDocument.AddMap(new Map());
                _mapDocument.FocusMap = _mapDocument.Maps.First();

                #region Create Modules

                PlugInManager compMan = new PlugInManager();
                foreach (var moduleType in compMan.GetPlugins(Framework.system.Plugins.Type.IMapApplicationModule))
                {
                    IMapApplicationModule module = compMan.CreateInstance<IMapApplicationModule>(moduleType);
                    if (module != null)
                    {
                        _modules.Add(module);
                        module.OnCreate(_mapDocument);
                    }
                }

                #endregion

                _mapApplication.SendOnCreate2Tools(_mapDocument);
                backstageTabControl.SelectionChanged += new SelectionChangedEventHandler(backstageTabControl_SelectionChanged);
                ribbon.SelectedTabChanged += new SelectionChangedEventHandler(ribbon_SelectedTabChanged);

                _contextMenuStripMapView = new System.Windows.Forms.ContextMenuStrip();
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(SystemVariables.ApplicationDirectory + @"/menu.carto.xml");

                    MakeMainMenuBar(doc.SelectSingleNode("//Menubar"));
                }
                catch (Exception)
                {
                    //MessageBox.Show(ex.Message);
                }
                MakeRibbon();
                ValidateButtons();

                #region Create Toc
                FormTOC toc = FormTOC.CreateAsync(_mapDocument).Result;
                _mapApplication.AddDockableWindow(_toc = toc.TOC, DockWindowState.left);
                //_mapApplication.AddDockableWindow(toc.Source, DockWindowState.left);

                _toc.SelectionChanged += new EventHandler(_toc_SelectionChanged);
                #endregion

                ShowBackstageMenu();

                this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

                this.Activated += delegate
                {
                    foreach (var item in ribbon.QuickAccessItems)
                    {
                        if (item is Fluent.QuickAccessMenuItem)
                        {
                            item.IsChecked = true;
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        #region DockWindow Events

        private Dictionary<IDockableWindow, LayoutAnchorable> _anchorables = new Dictionary<IDockableWindow, LayoutAnchorable>();
        void mapApplication_OnShowDockableWindow(IDockableWindow window)
        {
            if (!_anchorables.ContainsKey(window))
            {
                return;
            }

            LayoutAnchorable anchorable = _anchorables[window];
            if (anchorable != null)
            {
                if (anchorable.IsAutoHidden)
                {
                    anchorable.ToggleAutoHide();
                }
                else
                {
                    LayoutContent content = anchorable;
                    while (content != null && content.Parent is ILayoutContentSelector)
                    {
                        ILayoutContentSelector selector = (ILayoutContentSelector)content.Parent;
                        int index = selector.IndexOf(content);
                        if (index != selector.SelectedContentIndex)
                        {
                            selector.SelectedContentIndex = index;
                        }

                        content = content.Parent as LayoutContent;
                    }

                    anchorable.Show();
                    if (anchorable.IsVisible == false)
                    {
                        //if (anchorable.CanFloat)
                        //    anchorable.Float();
                        //else
                        anchorable.Dock();
                    }
                }
            }
        }

        void mapApplication_DockWindowAdded(IDockableWindow window, string parentDockableWindowName)
        {
            LayoutAnchorable anchorable = new LayoutAnchorable();
            //anchorable.Title = String.IsNullOrEmpty(parentDockableWindowName) ? window.Name : parentDockableWindowName;

            anchorable.Title = window.Name;

            if (window is System.Windows.Forms.Control)
            {
                WindowsFormsHost winHost = new WindowsFormsHost();
                winHost.Child = window as System.Windows.Forms.Control;
                anchorable.Content = winHost;
            }
            else
            {
                anchorable.Content = window;
            }

            anchorable.ContentId = parentDockableWindowName;
            anchorable.IsActiveChanged += new EventHandler(anchorable_IsActiveChanged);

            if (!String.IsNullOrEmpty(parentDockableWindowName))
            {
                LayoutAnchorablePane parent = null;
                foreach (LayoutAnchorable a in _anchorables.Values)
                {
                    if (a.ContentId == parentDockableWindowName && a.Parent is LayoutAnchorablePane)
                    {
                        parent = (LayoutAnchorablePane)a.Parent;
                    }
                }
                if (parent != null)
                {
                    parent.Children.Add(anchorable);
                }
                else
                {
                    parentDockableWindowName = String.Empty;
                }
            }
            if (String.IsNullOrEmpty(parentDockableWindowName))
            {
                switch (window.DockableWindowState)
                {
                    case DockWindowState.child:
                    case DockWindowState.right:
                        anchorPaneRight.Children.Add(anchorable);
                        break;
                    case DockWindowState.top:
                        anchorPaneTop.Children.Add(anchorable);
                        break;
                    case DockWindowState.bottom:
                        anchorPaneBottom.Children.Add(anchorable);
                        break;
                    case DockWindowState.left:
                        anchorPaneLeft.Children.Add(anchorable);
                        break;
                    default:
                        anchorPaneBottom.Children.Add(anchorable);
                        anchorable.FloatingHeight = 200;
                        anchorable.Float();
                        break;
                }
            }

            _anchorables.Add(window, anchorable);

            WpfViewToolStripItem winItem = new WpfViewToolStripItem(window.Name, window.Image, window);
            winItem.Click += new RoutedEventHandler(ViewToolStripItem_Click);

            anchorable_IsActiveChanged(anchorable, new EventArgs());
        }

        void anchorable_IsActiveChanged(object sender, EventArgs e)
        {
            LayoutAnchorable anchorable = sender as LayoutAnchorable;
            if (anchorable == null || anchorable.IsActive == false)
            {
                return;
            }

            IDockableWindow window = (anchorable.Content is WindowsFormsHost ? ((WindowsFormsHost)anchorable.Content).Child : anchorable.Content) as IDockableWindow;
            if (window == null)
            {
                return;
            }

            //if (window is IContextTools)
            {
                MakeRibbonDockableWindowContextGroups();
            }
        }

        #endregion

        public bool InvokeRequired
        {
            get
            {
                return _contextMenuStripMapView.InvokeRequired;
            }
        }
        public object Invoke(Delegate method)
        {
            return _contextMenuStripMapView.Invoke(method);
        }

        #region Events UI
        private void ViewToolStripItem_Click(object sender, System.EventArgs e)
        {
            if (!(sender is WpfViewToolStripItem))
            {
                return;
            }

            mapApplication_OnShowDockableWindow(((WpfViewToolStripItem)sender).DockableToolWindow);
        }

        async private void ToolButton_Click(object sender, EventArgs e)
        {
            DataView dataView = _mapApplication.ActiveDataView;
            if (dataView == null)
            {
                return;
            }

            ITool tool = null;
            if (sender is ToolButton)
            {
                tool = ((ToolButton)sender).Tool;
            }
            else if (sender is ToolToggleButton)
            {
                tool = ((ToolToggleButton)sender).Tool;
            }
            else if (sender is ToolMenuItem)
            {
                tool = ((ToolMenuItem)sender).Tool;
            }
            else if (sender is DropDownToolButton)
            {
                tool = ((DropDownToolButton)sender).Tool;
            }

            if (tool == null)
            {
                return;
            }

            if (tool is IToolWindow)
            {
                _mapApplication.AddDockableWindow(((IToolWindow)tool).ToolWindow, "");
                _mapApplication.ShowDockableWindow(((IToolWindow)tool).ToolWindow);
            }
            switch (tool.toolType)
            {
                case ToolType.command:
                    MapEvent ev = new MapEvent((Map)_mapDocument.FocusMap);
                    await tool.OnEvent(ev);
                    if (ev.refreshMap && _mapDocument.FocusMap != null)
                    {
                        //dataView.MapView.RefreshMap(ev.drawPhase);
                        //_mapDocument.FocusMap.RefreshMap(ev.drawPhase, null);
                        await _mapApplication.RefreshActiveMap(ev.drawPhase);
                    }
                    ValidateButtons();
                    break;
                case ToolType.sketch:
                case ToolType.click:
                case ToolType.rubberband:
                case ToolType.smartnavigation:
                case ToolType.pan:
                case ToolType.userdefined:
                    if (sender is ICheckAbleButton)
                    {
                        SetActiveTool(tool);
                        if (_activeTool != null)
                        {
                            _activeTool.Checked = false;
                        }

                        _activeTool = sender as ICheckAbleButton;
                        _activeTool.Checked = true;
                    }
                    if (tool.toolType == ToolType.userdefined)
                    {
                        MapEvent ev2 = new MapEvent((Map)_mapDocument.FocusMap);
                        await tool.OnEvent(ev2);
                        if (ev2.refreshMap && _mapDocument.FocusMap != null)
                        {
                            //dataView.MapView.RefreshMap(ev2.drawPhase);
                            await _mapApplication.RefreshActiveMap(ev2.drawPhase);
                        }
                    }
                    break;
            }

            ValidateButtons();
        }

        private void DataViewMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is DataViewMenuItem)
            {
                DataViewMenuItem item = (DataViewMenuItem)sender;
                //if (item.TabPage == null || item.DataView == null) return;

                //if (!_filler.TabPages.Contains(item.TabPage))
                //{
                //    _filler.TabPages.Add(item.TabPage);
                //}
                //_filler.SelectedTab = item.TabPage;
            }
        }

        private void MenuItem_Plugins_Click(object sender, RoutedEventArgs e)
        {
            gView.Framework.UI.Dialogs.FormComponents dlg = new gView.Framework.UI.Dialogs.FormComponents();
            dlg.ShowDialog();
        }

        private void MenuItem_Options_Click(object sender, RoutedEventArgs e)
        {
            gView.Framework.UI.Dialogs.FormExplorerOptions dlg = new gView.Framework.UI.Dialogs.FormExplorerOptions(null);
            dlg.ShowDialog();
        }

        private IControl _activeBackstageControl = null;
        private void backstageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (backstageTabControl.SelectedItem is Fluent.BackstageTabItem)
            {
                Fluent.BackstageTabItem item = (Fluent.BackstageTabItem)backstageTabControl.SelectedItem;

                if (_activeBackstageControl != null)
                {
                    _activeBackstageControl.UnloadControl();
                }

                if (item.Content is IControl)
                {
                    ((IControl)item.Content).OnShowControl(_mapDocument);
                    _activeBackstageControl = (IControl)item.Content;
                }
                else if (item.Content is WindowsFormsHost &&
                    ((WindowsFormsHost)item.Content).Child is IControl)
                {
                    ((IControl)((WindowsFormsHost)item.Content).Child).OnShowControl(_mapDocument);
                    _activeBackstageControl = (IControl)((WindowsFormsHost)item.Content).Child;
                }
                else
                {
                    _activeBackstageControl = null;
                }
            }
        }

        private int _ribbonIndex = -999;
        void ribbon_SelectedTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ribbonIndex == -1 && _activeBackstageControl != null)
            {
                _activeBackstageControl.UnloadControl();
                _activeBackstageControl = null;
            }
            _ribbonIndex = ribbon.SelectedTabIndex;
        }

        internal void ShowBackstageMenu()
        {
            ribbon.SelectedTabIndex = -1;
        }
        internal void HideBackstageMenu()
        {
            //ribbon.SelectedTabIndex = 0;
            var backstage = ribbon.Menu as Fluent.Backstage;
            if (backstage != null)
            {
                backstage.IsOpen = false;
            }
        }

        void _toc_SelectionChanged(object sender, EventArgs e)
        {
            if (_toc.SelectedItems.Length == 1 &&
                _toc.SelectedItems[0] is IContextType &&
                ((IContextType)_toc.SelectedItems[0]).ContextObject is IMap &&
                (IMap)((IContextType)_toc.SelectedItems[0]).ContextObject != _mapDocument.FocusMap)
            {
                _mapDocument.FocusMap = (IMap)((IContextType)_toc.SelectedItems[0]).ContextObject;
                this.SelectTocElementByContextObject(_mapDocument.FocusMap);
                return;
            }

            MakeRibbonTocContextGroups();
        }

        void _mapDocument_MapScaleChanged(IDisplay sender)
        {
            if (!cmbScale.Dispatcher.CheckAccess())
            {
                cmbScale.Dispatcher.Invoke(new MapScaleChangedEvent(_mapDocument_MapScaleChanged),
                    System.Windows.Threading.DispatcherPriority.Normal,
                     new object[] { sender });
            }
            else
            {
                cmbScale.Text = String.Format("{0:0,0}", sender.MapScale);
                sliderScale.Value = _lastSliderValue = 0D;
            }
        }

        #region Scale Combo/Slider

        async private void cmbScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem)
                {
                    if (int.TryParse(((ComboBoxItem)e.AddedItems[0]).Content.ToString()
                        .Replace(".", "")
                        .Replace(",", "")
                        .Replace(" ", ""), out int scale))
                    {
                        await SetMapScale(scale);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _mapDocument_MapScaleChanged(_mapDocument.FocusMap.Display);
            }
        }

        async private void cmbScale_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(cmbScale.Text
                    .Replace(" ", "")
                    .Replace(".", ""), out int scale))
                {
                    if (scale <= 0)
                    {
                        return;
                    }

                    //int index = 0;
                    //foreach (ComboBoxItem item in cmbScale.Items)
                    //{
                    //    int s = int.Parse(item.Content.ToString().Replace(".", "").Replace(",", ""));
                    //    if (s > scale)
                    //    {
                    //        ComboBoxItem newItem = new ComboBoxItem();
                    //        newItem.Content = String.Format("{0:0,0}", scale);
                    //        cmbScale.Items.Insert(index, newItem);
                    //        cmbScale.Text = newItem.Content.ToString();
                    //        break;
                    //    }
                    //    index++;
                    //}

                    cmbScale.Text = String.Format("{0:0,0}", scale);

                    await SetMapScale(scale);
                }
            }
        }

        #endregion

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mapApplication.ActiveDataView.MapView.CancelDrawing(DrawPhase.All);
            if (_mapApplication.IsDirty)
            {
                e.Cancel = !_mapApplication.SaveDirtyDocument();
            }
        }

        #endregion

        #region Private Members

        private ToolToggleButton GetToolButton(ITool tool)
        {
            foreach (ToolToggleButton button in _toolButtons)
            {
                if (button.Tool == tool)
                {
                    return button;
                }
            }
            return null;
        }

        private void SetActiveTool(ITool newTool)
        {
            if (_mapApplication == null)
            {
                return;
            }

            DataView dataView = _mapApplication.ActiveDataView;

            if (dataView != null && dataView.MapView != null)
            {
                ITool oldTool = null;
                oldTool = dataView.MapView.Tool;
                if (oldTool == newTool)
                {
                    return;
                }

                dataView.MapView.Tool = newTool;
                _mapApplication.MapToolChanged(oldTool, newTool);
            }
        }

        async private Task SetMapScale(int scale)
        {
            if (_mapDocument == null || _mapDocument.FocusMap == null || _mapApplication == null)
            {
                return;
            }

            if (scale <= 0 || scale == (int)((IDisplay)_mapDocument.FocusMap).MapScale)
            {
                return;
            }

            _mapDocument.FocusMap.Display.MapScale = scale;
            await _mapApplication.RefreshActiveMap(DrawPhase.All);
        }

        private double _lastSliderValue;
        private void sliderScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mapDocument != null && sliderScale.Value != _lastSliderValue)
            {
                gView.Framework.Geometry.IPoint cp = _mapDocument.FocusMap.Display.Envelope.Center;

                DataView dv = _mapApplication.ActiveDataView;
                _mapDocument.MapScaleChanged -= new MapScaleChangedEvent(_mapDocument_MapScaleChanged);
                dv.MapView.Wheel(cp.X, cp.Y, sliderScale.Value - _lastSliderValue);
                _lastSliderValue = sliderScale.Value;
                _mapDocument.MapScaleChanged += new MapScaleChangedEvent(_mapDocument_MapScaleChanged);
            }
        }

        #endregion

        #region Members

        internal ITool ActiveTool
        {
            get
            {
                if (_activeTool == null)
                {
                    return null;
                }

                return _activeTool.Tool;
            }

            set
            {
                ToolToggleButton button = GetToolButton(value);
                if (button == null)
                {
                    return;
                }

                ToolButton_Click(button, new EventArgs());
            }
        }

        private void MakeMainMenuBar(XmlNode node)
        {
            if (node == null)
            {
                return;
            }

            backstageTabControl.Items.Clear();

            foreach (XmlNode menu in node.SelectNodes("Menu[@name]"))
            {
                if (menu.Attributes["name"].Value.ToLower() == "file")
                {
                    foreach (XmlNode menuItem in menu.SelectNodes("MenuItem"))
                    {
                        if (menuItem.Attributes["guid"] == null)
                        {
                            //backstageTabControl.Items.Add(new Separator());
                        }
                        else
                        {
                            ITool tool = _mapApplication.Tool(new Guid(menuItem.Attributes["guid"].Value));
                            if (tool == null)
                            {
                                continue;
                            }

                            tool.OnCreate(_mapDocument);

                            if (tool is IToolControl)
                            {
                                Fluent.BackstageTabItem backItem = new Fluent.BackstageTabItem();
                                backItem.Header = tool.Name;
                                backItem.Icon =
                                    ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);

                                object control = ((IToolControl)tool).Control;
                                if (control is System.Windows.Forms.Control)
                                {
                                    WindowsFormsHost host = WinHostFactory.ToWindowsHost((System.Windows.Forms.Control)control);
                                    backItem.Content = host;
                                }
                                else if (control is FrameworkElement)
                                {
                                    backItem.Content = control;
                                }

                                backstageTabControl.Items.Add(backItem);
                            }
                            else
                            {
                                ToolButton button = new ToolButton(tool);
                                button.Click += new RoutedEventHandler(ToolButton_Click);

                                backstageTabControl.Items.Add(button);
                            }
                        }
                    }
                }
                if (menu.Attributes["name"].Value.ToLower() == "quickaccess")
                {
                    foreach (XmlNode menuItem in menu.SelectNodes("MenuItem"))
                    {
                        if (menuItem.Attributes["guid"] == null)
                        {
                            ribbon.QuickAccessItems.Add(new Fluent.QuickAccessMenuItem() { Target = new Separator() });
                            continue;
                        }
                        ITool tool = _mapApplication.Tool(new Guid(menuItem.Attributes["guid"].Value));
                        if (tool == null)
                        {
                            continue;
                        }

                        tool.OnCreate(_mapDocument);

                        if (tool.toolType == ToolType.command)
                        {
                            ToolButton button = new ToolButton(tool);
                            button.Click += new RoutedEventHandler(ToolButton_Click);

                            ribbon.QuickAccessItems.Add(new Fluent.QuickAccessMenuItem() { Target = button, IsChecked = true, IsEnabled = true, Visibility = Visibility.Visible });
                        }
                        else
                        {
                            ToolToggleButton button = new ToolToggleButton(tool);
                            button.Click += new RoutedEventHandler(ToolButton_Click);

                            ribbon.QuickAccessItems.Add(new Fluent.QuickAccessMenuItem() { Target = button, IsChecked = true, IsEnabled = true, Visibility = Visibility.Visible });
                        }
                    }
                }
            }
            /*
            mainMenu.Items.Clear();

            PlugInManager compManager = new PlugInManager();
            foreach (XmlNode menu in node.SelectNodes("Menu[@name]"))
            {
                MenuItem item;
                if (menu.Attributes["type"] == null)
                {
                    item = new MenuItem();
                    item.Header=LocalizedResources.GetResString("MenuHeader." + menu.Attributes["name"].Value, menu.Attributes["name"].Value);
                }
                else
                {
                    switch (menu.Attributes["type"].Value.ToLower())
                    {
                        case "view":
                            item = viewToolStripMenuItem;
                            LocalizedResources.GlobalizeWpfMenuItem(item);
                            break;
                        case "options":
                            item = optionsToolStripMenuItem;
                            LocalizedResources.GlobalizeWpfMenuItem(item);
                            break;
                        default:
                            item = new MenuItem();
                            item.Header=LocalizedResources.GetResString("MenuHeader." + menu.Attributes["name"].Value, menu.Attributes["name"].Value);
                            break;
                    }
                }

                foreach (XmlNode menuItem in menu.SelectNodes("MenuItem"))
                {
                    if (menuItem.Attributes.Count == 0)
                    {
                        item.Items.Add(new System.Windows.Controls.Separator());
                    }
                    else if (menuItem.Attributes["guid"] != null)
                    {
                        try
                        {
                            ITool tool = (ITool)compManager.CreateInstance(new Guid(menuItem.Attributes["guid"].Value));
                            if (tool == null) continue;

                            tool.OnCreate(_mapDocument);

                            if (tool is IToolMenu)
                            {
                                foreach (ITool childTool in ((IToolMenu)tool).DropDownTools)
                                {
                                    ToolMenuItem menuitem = new ToolMenuItem(childTool);
                                    //menuitem.ImageScaling = ToolStripItemImageScaling.None;

                                    menuitem.Click += new RoutedEventHandler(ToolButton_Click);
                                    item.Items.Add(menuitem);
                                }
                            }
                            else
                            {
                                ToolMenuItem menuitem = new ToolMenuItem(tool);
                                //menuitem.ImageScaling = ToolStripItemImageScaling.None;

                                menuitem.Click += new RoutedEventHandler(ToolButton_Click);
                                item.Items.Add(menuitem);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                mainMenu.Items.Add(item);
            }
             * */
        }

        void QuickAccessItem_Click(object sender, RoutedEventArgs e)
        {
            Fluent.QuickAccessMenuItem item = (Fluent.QuickAccessMenuItem)sender;

            if (item.Target is ToolButton)
            {
                ToolButton_Click(item.Target, new EventArgs());
            }
        }

        internal DataView AddDataView(IMap map)
        {
            if (map == null)
            {
                return null;
            }

            DataView dataView = _mapApplication.GetDataView(map);
            if (dataView == null)
            {
                gView.Framework.UI.Controls.MapView mapView = new gView.Framework.UI.Controls.MapView();
                if (map != null)
                {
                    mapView.Map = map;
                }

                mapView.resizeMode = gView.Framework.UI.Controls.MapView.ResizeMode.SameScale;
                mapView.ContextMenu = _contextMenuStripMapView;
                mapView.ControlKeyTool = _mapApplication.Tool(new Guid("3E2E9F8C-24FB-48f6-B80E-1B0A54E8C309")); // SmartNavigation

                dataView = new DataView(map, mapView);

                if (!_mapApplication.AddDataView(dataView))
                {
                    mapView.Dispose();
                    MessageBox.Show("Can't add Data View " + dataView.Name + "!");
                    return null;
                }

                //_mapView = mapView;
                mapView.MapDocument = _mapDocument;  // Um Event AfterLoadMapDocument hinzuzufügen
                mapView.CursorMove += new gView.Framework.UI.Controls.MapView.CursorMoveEvent(mapView1_CursorMove);
                mapView.DrawingLayer += new gView.Framework.UI.Controls.MapView.DrawingLayerEvent(mapView1_DrawingLayer);

                mapView.BeforeRefreshMap += new MapView.BeforeRefreshMapEvent(mapView1_BeforeRefreshMap);
                mapView.AfterRefreshMap += new MapView.AfterRefreshMapEvent(mapView1_AfterRefreshMap);
            }
            return dataView;
        }

        internal System.Windows.Forms.ContextMenuStrip ContextMenuStripDataView
        {
            get { return _contextMenuStripMapView; }
        }

        internal IMapApplicationModule MapApplicationModule(Guid guid)
        {
            foreach (IMapApplicationModule module in _modules)
            {
                if (PlugInManager.PlugInID(module).Equals(guid))
                {
                    return module;
                }
            }
            return null;
        }

        async internal Task RefreshTOC()
        {
            if (_toc != null)
            {
                await _toc.RefreshList();
            }
        }

        internal void RefreshTOCElement(IDatasetElement element)
        {
            if (_toc != null)
            {
                _toc.RefreshTOCElement(element);
            }
        }

        internal void SelectTocElementByContextObject(object contextObject)
        {
            object[] items = _toc.Items;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is IContextType)
                {
                    IContextType ctype = (IContextType)items[i];
                    if (ctype.ContextObject == contextObject)
                    {
                        _toc.SelectItem(i);
                        break;
                    }
                }
            }
        }

        #endregion

        #region Ribbon
        public void MakeRibbon()
        {
            DataView dataView = _mapApplication.ActiveDataView;
            if (dataView == null)
            {
                return;
            }

            PlugInManager pm = new PlugInManager();

            foreach (ICartoRibbonTab cartoRibbonTab in OrderedPluginList<ICartoRibbonTab>.Sort(pm.GetPlugins(Framework.system.Plugins.Type.ICartoRibbonTab)))
            {
                CartoRibbonTab tabItem = new CartoRibbonTab(cartoRibbonTab) { Header = cartoRibbonTab.Header };
                ribbon.Tabs.Add(tabItem);
                tabItem.DataContext = cartoRibbonTab;

                foreach (RibbonGroupBox cartoGroupBox in cartoRibbonTab.Groups)
                {
                    CartoRibbonGroup groupBox = new CartoRibbonGroup(cartoGroupBox) { Header = cartoGroupBox.Header };
                    tabItem.Groups.Add(groupBox);

                    if (cartoGroupBox.OnLauncherClick != null)
                    {
                        groupBox.LauncherClick += new RoutedEventHandler(groupBox_LauncherClick);
                        groupBox.IsLauncherVisible = true;
                    }

                    foreach (RibbonItem cartoRibbonItem in cartoGroupBox.Items)
                    {
                        Guid toolGUID = cartoRibbonItem.Guid;
                        if (toolGUID == new Guid("00000000-0000-0000-0000-000000000000"))
                        {
                            groupBox.Items.Add(new Separator());
                            continue;
                        }

                        object tool = _mapApplication.Tool(toolGUID);
                        if (tool == null)
                        {
                            continue;
                        }

                        #region IToolItem
                        if (tool is IToolItem)
                        {
                            if (((IToolItem)tool).ToolItem != null)
                            {
                                if (((IToolItem)tool).ToolItem is System.Windows.Forms.ToolStripItem)
                                {
                                    StackPanel panel = new StackPanel();
                                    panel.Margin = new Thickness(0, 32, 0, 0);

                                    System.Windows.Forms.ToolStripItem stripItem = ((IToolItem)tool).ToolItem;

                                    System.Windows.Forms.MenuStrip bar = new System.Windows.Forms.MenuStrip();
                                    bar.BackColor = System.Drawing.Color.Transparent; //.FromArgb(223, 234, 246);

                                    bar.Items.Add(stripItem);

                                    WindowsFormsHost host = new WindowsFormsHost();
                                    host.Background = new SolidColorBrush(Color.FromRgb(223, 234, 246));
                                    host.Child = bar;

                                    if (tool is IToolItemLabel)
                                    {
                                        IToolItemLabel label = (IToolItemLabel)tool;
                                        panel.Orientation = label.LabelPosition == ToolItemLabelPosition.top ||
                                                            label.LabelPosition == ToolItemLabelPosition.bottom ? Orientation.Vertical : Orientation.Horizontal;

                                        if (panel.Orientation == Orientation.Vertical)
                                        {
                                            panel.Margin = new Thickness(0, 13, 0, 0);
                                        }

                                        TextBlock text = new TextBlock();
                                        text.Text = label.Label;
                                        text.Padding = new Thickness(3);

                                        if (label.LabelPosition == ToolItemLabelPosition.top || label.LabelPosition == ToolItemLabelPosition.left)
                                        {
                                            panel.Children.Insert(0, text);
                                        }
                                        else
                                        {
                                            panel.Children.Add(text);
                                        }
                                    }

                                    panel.Children.Add(host);

                                    groupBox.Items.Add(panel);
                                }
                            }
                        }
                        #endregion

                        #region IToolMenu
                        else if (tool is IToolMenu)
                        {
                            DropDownToolButton button = new DropDownToolButton(tool as IToolMenu);
                            button.Click += new RoutedEventHandler(ToolButton_Click);
                            groupBox.Items.Add(button);
                        }
                        #endregion

                        #region ITool
                        else if (tool is ITool)
                        {
                            if (((ITool)tool).toolType == ToolType.command)
                            {
                                ToolButton button = new ToolButton(tool as ITool);
                                button.SizeDefinition = cartoRibbonItem.SizeDefinition;

                                groupBox.Items.Add(button);
                                button.Click += new RoutedEventHandler(ToolButton_Click);
                            }
                            else
                            {
                                ToolToggleButton button = new ToolToggleButton(tool as ITool);
                                button.SizeDefinition = cartoRibbonItem.SizeDefinition;

                                if ((((ITool)tool).toolType == ToolType.pan ||
                                     ((ITool)tool).toolType == ToolType.rubberband ||
                                     ((ITool)tool).toolType == ToolType.smartnavigation) && _activeTool == null)
                                {
                                    _activeTool = button;
                                    SetActiveTool(tool as ITool);
                                    button.Checked = true;
                                }

                                groupBox.Items.Add(button);
                                button.Click += new RoutedEventHandler(ToolButton_Click);

                                _toolButtons.Add(button);
                                //SetToolTipText(tool, button);
                            }
                        }
                        #endregion
                    }
                }
            }

            #region Options
            Fluent.RibbonTabItem optionsTab = new Fluent.RibbonTabItem() { Header = "Options" };
            Fluent.RibbonGroupBox optionsBox = new Fluent.RibbonGroupBox() { Header = String.Empty };
            optionsTab.Groups.Add(optionsBox);

            foreach (var pageType in pm.GetPlugins(Framework.system.Plugins.Type.IMapOptionPage))
            {
                IMapOptionPage page = pm.CreateInstance<IMapOptionPage>(pageType);
                if (page == null)
                {
                    continue;
                }

                OptionsButton button = new OptionsButton(page);
                button.Click += new RoutedEventHandler(OptionButton_Click);
                optionsBox.Items.Add(button);
            }
            ribbon.Tabs.Add(optionsTab);
            #endregion

            #region Docs (?)
            Fluent.RibbonTabItem docsTab = new Fluent.RibbonTabItem() { Header = "?" };
            Fluent.RibbonGroupBox docsBox = new Fluent.RibbonGroupBox() { Header = String.Empty };
            docsTab.Groups.Add(docsBox);

            try
            {
                foreach (System.IO.FileInfo fi in (new System.IO.DirectoryInfo(SystemVariables.ApplicationDirectory + @"/doc").GetFiles("*.pdf")))
                {
                    PdfLinkButton button = new PdfLinkButton(fi);
                    docsBox.Items.Add(button);
                }

            }
            catch { }
            ribbon.Tabs.Add(docsTab);
            #endregion
        }

        private void MakeRibbonTocContextGroups()
        {
            #region Remove Tabs & Groups
            for (int i = 1; i < ribbon.Tabs.Count; i++)
            {
                if (ribbon.Tabs[i].Group is RibbonTocContextualTabGroup)
                {
                    try
                    {
                        if (ribbon.SelectedTabIndex == i)
                        {
                            ribbon.SelectedTabIndex = 0;
                        }

                        ribbon.ContextualGroups.Remove(ribbon.Tabs[i].Group);
                        ribbon.Tabs.RemoveAt(i);
                    }
                    catch { }
                    i--;
                }
            }
            #endregion

            bool first = true;
            foreach (object item in _toc.SelectedItems)
            {
                if (item is IContextType)
                {
                    string contextGroupName = ((IContextType)item).ContextGroupName;
                    Fluent.RibbonContextualTabGroup group = null;
                    foreach (Fluent.RibbonContextualTabGroup ctg in ribbon.ContextualGroups)
                    {
                        if (ctg is RibbonTocContextualTabGroup && ctg.Header == contextGroupName)
                        {
                            group = ctg;
                            break;
                        }
                    }
                    if (group == null)
                    {
                        group = new RibbonTocContextualTabGroup();
                        group.Header = contextGroupName;
                        group.Background = new SolidColorBrush(Colors.Green);
                        group.BorderBrush = new SolidColorBrush(Colors.Green);
                        group.Visibility = Visibility.Visible;
                        ribbon.ContextualGroups.Add(group);
                    }
                    Fluent.RibbonTabItem tabItem = new Fluent.RibbonTabItem();
                    tabItem.Header = ((IContextType)item).ContextName;
                    tabItem.Group = group;

                    Fluent.RibbonGroupBox groupBox = new Fluent.RibbonGroupBox();
                    groupBox.Header = String.Empty;

                    PlugInManager pm = new PlugInManager();
                    int order = 0;
                    foreach (object contextItem in OrderedPluginList<object>.Sort(pm.GetPluginInstances(((IContextType)item).ContextType)))
                    {
                        if (contextItem is IContextMenuTool)
                        {
                            if (((IContextMenuTool)contextItem).SortOrder / 10 - order / 10 >= 1 &&
                                groupBox.Items.Count > 0)
                            {
                                tabItem.Groups.Add(groupBox);
                                groupBox = new Fluent.RibbonGroupBox();
                                groupBox.Header = String.Empty;
                            }
                            order = ((IContextMenuTool)contextItem).SortOrder;
                            ((IContextMenuTool)contextItem).OnCreate(_mapDocument);
                            ContextMenuButton button = new ContextMenuButton((IContextMenuTool)contextItem, (IContextType)item, _mapDocument);
                            if (button.Visibility == Visibility.Visible)
                            {
                                groupBox.Items.Add(button);
                            }
                        }
                    }

                    tabItem.Groups.Add(groupBox);
                    ribbon.Tabs.Add(tabItem);

                    if (first)
                    {
                        first = false;
                        ribbon.SelectedTabIndex = ribbon.Tabs.Count - 1;
                    }
                }
            }
        }

        private void MakeRibbonDockableWindowContextGroups()
        {
            #region Remove Tabs & Groups
            for (int i = 1; i < ribbon.Tabs.Count; i++)
            {
                if (ribbon.Tabs[i].Group is RibbonDockableWindowContextualTabGroup)
                {
                    try
                    {
                        if (ribbon.SelectedTabIndex == i)
                        {
                            ribbon.SelectedTabIndex = 0;
                        }

                        ribbon.ContextualGroups.Remove(ribbon.Tabs[i].Group);
                        ribbon.Tabs.RemoveAt(i);
                    }
                    catch { }
                    i--;
                }
            }
            #endregion

            foreach (LayoutAnchorable anchorable in _anchorables.Values)
            {
                if (!anchorable.IsActive)
                {
                    continue;
                }

                IDockableWindow window = (anchorable.Content is WindowsFormsHost ? ((WindowsFormsHost)anchorable.Content).Child : anchorable.Content) as IDockableWindow;
                if (window is IContextTools && ((IContextTools)window).ContextTools != null)
                {
                    RibbonDockableWindowContextualTabGroup group = new RibbonDockableWindowContextualTabGroup();
                    group.Header = window.Name;
                    group.Background = new SolidColorBrush(Colors.Violet);
                    group.BorderBrush = new SolidColorBrush(Colors.Violet);
                    group.Visibility = Visibility.Visible;
                    ribbon.ContextualGroups.Add(group);

                    Fluent.RibbonTabItem tabItem = new Fluent.RibbonTabItem();
                    tabItem.Header = window.Name;
                    tabItem.Group = group;

                    Fluent.RibbonGroupBox groupBox = new Fluent.RibbonGroupBox() { Header = String.Empty };

                    foreach (ITool tool in ((IContextTools)window).ContextTools)
                    {
                        if (tool is IToolMenu)
                        {
                            DropDownToolButton button = new DropDownToolButton(tool as IToolMenu, window);
                            groupBox.Items.Add(button);
                        }
                        else
                        {
                            ContextToolButton button = new ContextToolButton(tool, window);
                            groupBox.Items.Add(button);
                        }
                    }

                    tabItem.Groups.Add(groupBox);
                    ribbon.Tabs.Add(tabItem);

                    ribbon.SelectedTabIndex = ribbon.Tabs.Count - 1;
                }
            }
        }

        void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsButton button = (OptionsButton)sender;
            IMapOptionPage page = button.MapOptionPage;

            if (!page.IsAvailable(_mapDocument))
            {
                gView.Framework.system.UI.AppUIGlobals.IsAppReadOnly(_mapDocument.Application);  // Show Dialog
                return;
            }

            if (page != null)
            {
                FormOptions dlg = new FormOptions(_mapDocument, page);
                dlg.ShowDialog();
            }
        }

        void groupBox_LauncherClick(object sender, RoutedEventArgs e)
        {
            if (sender is CartoRibbonGroup)
            {
                CartoRibbonGroup cartoRibbonGroup = (CartoRibbonGroup)sender;

                if (cartoRibbonGroup.GroupBox != null && cartoRibbonGroup.GroupBox.OnLauncherClick != null)
                {
                    cartoRibbonGroup.GroupBox.OnLauncherClick(cartoRibbonGroup.GroupBox, new RibbonGroupBox.LauncherClickEventArgs(_mapDocument));
                }
            }
        }

        private delegate void ValidateButtonsCallback();
        internal void ValidateButtons()
        {
            if (!ribbon.Dispatcher.CheckAccess())
            {
                ribbon.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new ValidateButtonsCallback(ValidateButtons));
            }
            else
            {
                foreach (Fluent.RibbonTabItem tabItem in ribbon.Tabs)
                {
                    foreach (Fluent.RibbonGroupBox groupBox in tabItem.Groups)
                    {
                        foreach (object button in groupBox.Items)
                        {
                            ValidateButton(button);
                        }
                    }
                }
                foreach (var item in ribbon.QuickAccessItems)
                {
                    ValidateButton(item.Target);
                }
            }
        }

        private void ValidateButton(object button)
        {
            if (button is ToolButton && ((ToolButton)button).Tool != null)
            {
                ITool tool = ((ToolButton)button).Tool;
                ((ToolButton)button).IsEnabled = tool.Enabled;
            }
            else if (button is ToolToggleButton && ((ToolToggleButton)button).Tool != null)
            {
                ITool tool = ((ToolToggleButton)button).Tool;
                ((ToolToggleButton)button).IsEnabled = tool.Enabled;
                ((ToolToggleButton)button).Checked = ((ToolToggleButton)button).Tool == this.ActiveTool;
            }
            else if (button is DropDownToolButton && ((DropDownToolButton)button).Tool != null)
            {
                ITool tool = ((DropDownToolButton)button).Tool;
                ((DropDownToolButton)button).IsEnabled = tool.Enabled;
            }
        }

        internal void ValidateToolbars()
        {
            if (!ribbon.Dispatcher.CheckAccess())
            {
                ribbon.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new ValidateButtonsCallback(ValidateToolbars));
            }
            else
            {
                foreach (Fluent.RibbonTabItem tabItem in ribbon.Tabs)
                {
                    if (tabItem is CartoRibbonTab)
                    {
                        ICartoRibbonTab cartoTab = ((CartoRibbonTab)tabItem).CartoTab;
                        tabItem.Visibility = cartoTab.IsVisible(_mapDocument) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        internal List<ToolbarStrip> ToolbarStrips
        {
            get { return _toolBars; }
        }

        internal void RemoveAllToolbars()
        {
            _toolBars.Clear();
        }
        #endregion

        #region MapView Events
        private delegate void CursorMoveCallback(int x, int y, double X, double Y);
        public void mapView1_CursorMove(int x, int y, double X, double Y)
        {
            if (_mapDocument == null || _mapDocument.FocusMap == null || _mapDocument.FocusMap.Display == null)
            {
                return;
            }

            if (!statusLabelX.Dispatcher.CheckAccess())
            {
                statusBarLabel1.Dispatcher.Invoke(
                    new CursorMoveCallback(mapView1_CursorMove),
                    System.Windows.Threading.DispatcherPriority.Normal,
                     new object[] { x, y, X, Y });
            }
            else
            {
                IMap map = _mapDocument.FocusMap;

                GeoUnitConverter converter = new GeoUnitConverter();

                string[] c = converter.Convert(
                    new string[] { X.ToString(), Y.ToString() },
                    map.Display.MapUnits,
                    map.Display.DisplayUnits,
                    map.Display.SpatialReference != null ? map.Display.SpatialReference.SpatialParameters : null);
                if (c == null || c.Length != 2)
                {
                    return;
                }

                statusLabelX.Text = "X=" + String.Format("{0:#.##}", c[0]);
                statusLabelY.Text = "Y=" + String.Format("{0:#.##}", c[1]);
                statusLabelUnit.Text = "[" + map.Display.DisplayUnits.ToString() + "]";
                if (map.Display.SpatialReference != null &&
                    statusLabelRefSystem.Text != map.Display.SpatialReference.Description)
                {
                    statusLabelRefSystem.Text = map.Display.SpatialReference.Description;
                }

                if (_mapApplication != null)
                {
                    _mapApplication.CursorPos(X, Y);
                }
            }
        }

        private void mapView1_DrawingLayer(string layerName)
        {
            SetPanelText(1, "Drawing Layer " + layerName + "...");
        }

        public void mapView1_AfterRefreshMap()
        {
            ITool tool = this.ActiveTool;
            SetPanelText(1, (int)(DateTime.UtcNow - _mapRefreshStartTime).TotalMilliseconds + "ms");
        }

        private DateTime _mapRefreshStartTime;
        public void mapView1_BeforeRefreshMap()
        {
            _mapRefreshStartTime = DateTime.UtcNow;
        }

        #endregion

        #region Item Wrapper

        private class WpfToolStripCombo : ComboBox
        {
            private System.Windows.Forms.ToolStripComboBox _combo;

            public WpfToolStripCombo(System.Windows.Forms.ToolStripComboBox combo)
            {
                _combo = combo;
                _combo.SelectedIndexChanged += new EventHandler(_combo_SelectedIndexChanged);
                Fill();

                base.DropDownOpened += new EventHandler(WpfToolStripCombo_DropDownOpened);
                base.SelectionChanged += new SelectionChangedEventHandler(WpfToolStripCombo_SelectionChanged);


                base.Style = new Style(GetType(), FindResource(ToolBar.ComboBoxStyleKey) as Style);
            }

            void WpfToolStripCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                _combo.SelectedItem = base.SelectedItem;
            }

            void _combo_SelectedIndexChanged(object sender, EventArgs e)
            {
                base.SelectedItem = _combo.SelectedItem;
            }

            void WpfToolStripCombo_DropDownOpened(object sender, EventArgs e)
            {
                Fill();
            }

            private void Fill()
            {
                foreach (object item in _combo.Items)
                {
                    if (base.Items.Contains(item))
                    {
                        continue;
                    }

                    base.Items.Add(item);
                }
                base.SelectedItem = _combo.SelectedItem;
            }
        }

        private class WpfToolStripMenu : Menu
        {
            private System.Windows.Forms.ToolStripMenuItem _menu;

            public WpfToolStripMenu(System.Windows.Forms.ToolStripMenuItem menu)
            {
                _menu = menu;

                MenuItem item = new MenuItem();
                item.Header = menu.Text;
                base.Items.Add(item);

                #region Child Items
                foreach (System.Windows.Forms.ToolStripItem ddItem in menu.DropDownItems)
                {
                    object itemObj = WpfToolStripMenu.WpfItem(ddItem);
                    item.Items.Add(itemObj);
                }
                #endregion

                base.Background = base.BorderBrush = new SolidColorBrush(Colors.Transparent);

                base.Style = new Style(GetType(), FindResource(System.Windows.Controls.ToolBar.MenuStyleKey) as Style);
            }

            static public object WpfItem(System.Windows.Forms.ToolStripItem ddItem)
            {
                if (ddItem is System.Windows.Forms.ToolStripLabel)
                {
                    Label label = new Label();
                    label.Content = ddItem.Text;
                    label.Height = 23;
                    label.Padding = new Thickness(1);
                    label.VerticalAlignment = VerticalAlignment.Top;
                    label.VerticalContentAlignment = VerticalAlignment.Center;
                    return label;
                }
                else if (ddItem is System.Windows.Forms.ToolStripComboBox)
                {
                    WpfToolStripCombo combobox = new WpfToolStripCombo((System.Windows.Forms.ToolStripComboBox)ddItem);
                    combobox.Width = Math.Min(ddItem.Size.Width, 150);
                    return combobox;
                }
                else if (ddItem is System.Windows.Forms.ToolStripMenuItem)
                {
                    WpfToolStripMenu menu = new WpfToolStripMenu((System.Windows.Forms.ToolStripMenuItem)ddItem);
                    return menu;
                }
                else if (ddItem is System.Windows.Forms.ToolStripDropDownButton)
                {
                    WpfToolStripDownDownButton ddButton = new WpfToolStripDownDownButton((System.Windows.Forms.ToolStripDropDownButton)ddItem);
                    return ddButton;
                }

                return null;
            }
        }

        private class WpfToolStripDownDownButton : Menu
        {
            System.Windows.Forms.ToolStripDropDownButton _button;

            public WpfToolStripDownDownButton(System.Windows.Forms.ToolStripDropDownButton button)
            {
                _button = button;

                MenuItem item = new MenuItem();
                if (button.Image != null)
                {
                    item.Icon = ImageFactory.FromBitmap(button.Image);
                    if (item.Icon is Image)
                    {
                        item.Width = Math.Max(24, ((Image)item.Icon).Width);
                        item.Height = Math.Max(20, ((Image)item.Icon).Height);
                    }
                }
                else
                {
                    item.Header = button.Text;
                }
                base.Items.Add(item);

                #region Child Items
                foreach (System.Windows.Forms.ToolStripItem ddItem in button.DropDownItems)
                {
                    object itemObj = WpfToolStripMenu.WpfItem(ddItem);
                    item.Items.Add(itemObj);
                }
                #endregion

                base.Background = base.BorderBrush = new SolidColorBrush(Colors.Transparent);
                base.Style = new Style(GetType(), FindResource(System.Windows.Controls.ToolBar.MenuStyleKey) as Style);
            }
        }

        private class CartoRibbonTab : Fluent.RibbonTabItem
        {
            public CartoRibbonTab(ICartoRibbonTab cartoRibbonTab)
            {
                CartoTab = cartoRibbonTab;
            }

            public ICartoRibbonTab CartoTab
            {
                get;
                private set;
            }
        }

        private class CartoRibbonGroup : Fluent.RibbonGroupBox
        {
            public CartoRibbonGroup(RibbonGroupBox groupBox)
            {
                GroupBox = groupBox;
            }

            public RibbonGroupBox GroupBox
            {
                get;
                private set;
            }
        }

        public class RibbonTocContextualTabGroup : Fluent.RibbonContextualTabGroup
        {
        }

        public class RibbonDockableWindowContextualTabGroup : Fluent.RibbonContextualTabGroup
        {
        }

        #endregion

        #region IDocumentWindow

        private void dockManager_Loaded(object sender, RoutedEventArgs e)
        {
            anchorPaneRight.Children[0].Hide();
            anchorPaneBottom.Children[0].Hide();
            anchorPaneTop.Children[0].Hide();

            //App.CloseSplash();

            foreach (var item in ribbon.QuickAccessItems)
            {
                ribbon.AddToQuickAccessToolBar(item.Target);
            }
            _toc.SelectItem(0);


        }

        public void RefreshControls(DrawPhase phase)
        {
            throw new NotImplementedException();
        }

        public void AddChildWindow(System.Windows.Forms.Form child)
        {
            throw new NotImplementedException();
        }

        public System.Windows.Forms.Form GetChildWindow(string Title)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Statusbar
        private delegate void SetPanelTextCallback(int panelNr, string text);
        internal void SetPanelText(int panelNr, string text)
        {
            if (!statusBarLabel1.Dispatcher.CheckAccess())
            {
                statusBarLabel1.Dispatcher.Invoke(
                    new SetPanelTextCallback(SetPanelText),
                    System.Windows.Threading.DispatcherPriority.Normal,
                     new object[] { panelNr, text });
            }
            else
            {

                if (panelNr == 0)
                {
                    statusBarLabel1.Visibility = Visibility.Visible;
                    statusBarLabel2.Visibility = Visibility.Collapsed;
                    statusBarLabel1.Text = text;
                }
                else if (panelNr == 1)
                {
                    if (text == null)
                    {
                        statusBarLabel1.Visibility = Visibility.Visible;
                        statusBarLabel2.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        //statusBarLabel1.Visibility = Visibility.Collapsed;
                        statusBarLabel2.Visibility = Visibility.Visible;
                        statusBarLabel2.Text = text;
                    }


                }

                statusBarLabel1.Refresh();
                statusBarLabel2.Refresh();
            }
        }

        private delegate void SetPanelImageCallback(int panelNr, System.Drawing.Image image);
        internal void SetPanelImage(int panelNr, System.Drawing.Image image)
        {
            if (!statusBarLabel1.Dispatcher.CheckAccess())
            {
                statusBarLabel1.Dispatcher.Invoke(
                    new SetPanelImageCallback(SetPanelImage),
                    System.Windows.Threading.DispatcherPriority.Normal,
                     new object[] { panelNr, image });
            }
            else
            {
                if (panelNr == 0)
                {
                    statusBarImage1.Visibility = Visibility.Visible;
                    statusBarImage2.Visibility = Visibility.Collapsed;
                    statusBarImage1.Children.Clear();
                    statusBarImage1.Children.Add(ImageFactory.FromBitmap(image));
                }
                else if (panelNr == 1)
                {
                    statusBarImage1.Visibility = Visibility.Collapsed;
                    statusBarImage2.Visibility = Visibility.Visible;
                    statusBarImage2.Children.Clear();
                    statusBarImage2.Children.Add(ImageFactory.FromBitmap(image));
                }
            }
        }
        #endregion

        private void RibbonWindow_Activated(object sender, EventArgs e)
        {
            this.BringIntoView();
            this.Activated -= new EventHandler(RibbonWindow_Activated);
            _mapApplication.Start();
        }

        #region IWin32Window Member

        public IntPtr Handle
        {
            get
            {
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(this);
                return interopHelper.Handle;
            }
        }

        #endregion
    }
}
