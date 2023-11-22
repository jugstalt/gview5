using gView.Desktop.Wpf.Carto;
using gView.Framework.Carto;
using gView.Framework.Carto.Rendering;
using gView.Framework.Carto.UI;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Sys.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Controls;
using gView.Framework.XML;
using gView.GraphicsEngine.Abstraction;
using gView.system.UI.Framework.system.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml;
using Xceed.Wpf.AvalonDock.Layout;

namespace gView.Win.Carto
{
    internal delegate void DataViewRemovedEvent(DataView dataView);

    internal class MapApplication : License, IMapApplication
    {
        private gView.Win.Carto.MainWindow _appWindow = null;
        private List<DataView> _dataViews;
        private DataView _activeDataView = null;
        private LayoutDocumentPane _dataViewContainer;
        private MapDocument _doc;
        private bool _dirty = false, _readonly = false;
        private string _docFilename = "";
        private Dictionary<IMap, IEnvelope> _publicExtents = new Dictionary<IMap, IEnvelope>();
        private List<ITool> _tools;
        private const string _cryptoKey = "{D8A60AC4-14E7-4972-BBDD-28F8EA5762EB}";

        public event EventHandler OnApplicationStart;

        public MapApplication(gView.Win.Carto.MainWindow appWindow, LayoutDocumentPane dataViewContainer)
            : base()
        {
            _appWindow = appWindow;
            Title = "gView.Carto";

            _dataViews = new List<DataView>();
            _dataViewContainer = dataViewContainer;
            _tools = new List<ITool>();

            _dataViewContainer.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_dataViewContainer_PropertyChanged);
        }

        async void _dataViewContainer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedContent" && _doc != null)
            {
                LayoutDocument document = _dataViewContainer.SelectedContent as LayoutDocument;
                if (document != null)
                {
                    DataView dv = this.GetDataView(document.Title);
                    if (dv != null)
                    {
                        _doc.FocusMap = dv.Map;
                        await this.RefreshActiveMap(DrawPhase.All);
                    }
                }
            }
        }

        public void Start()
        {
            OnApplicationStart?.Invoke(this, new EventArgs());
        }

        public MapDocument mapDocument
        {
            get { return _doc; }
            set
            {
                _doc = value;
                _doc.AfterSetFocusMap += new AfterSetFocusMapEvent(mapDocument_AfterSetFocusMap);
                _doc.MapAdded += new MapAddedEvent(mapDocument_MapAdded);
                _doc.MapDeleted += new MapDeletedEvent(mapDocument_MapDeleted);
                _doc.LayerRemoved += new LayerRemovedEvent(mapDocument_LayerRemoved);
                _doc.LayerAdded += new LayerAddedEvent(mapDocument_LayerAdded);
            }
        }

        public void MapAddedToDocument(IMap map)
        {
            if (map == null)
            {
                return;
            }

            map.NewBitmap += new NewBitmapEvent(NewBitmapCreated);
            map.DoRefreshMapView += new DoRefreshMapViewEvent(MakeMapViewRefresh);
            map.DrawingLayer += new gView.Framework.Carto.DrawingLayerEvent(OnDrawingLayer);
            map.Display.MapScaleChanged += new MapScaleChangedEvent(Display_MapScaleChanged);
            map.Display.RenderOverlayImage += new RenderOverlayImageEvent(Display_RenderOverlayImage);

            _appWindow.AddDataView(map);
        }

        void Display_RenderOverlayImage(IBitmap bitmap, bool clearOld)
        {
            if (_activeDataView == null || _activeDataView.MapView == null)
            {
                return;
            }

            _activeDataView.MapView.RenderOverlayImage(bitmap, clearOld);
        }

        private void NewBitmapCreated(IBitmap bitmap)
        {
            if (_activeDataView == null)
            {
                return;
            }

            if (bitmap == null)
            {
                // Before Disposing Image
                foreach (DataView dv in _dataViews)
                {
                    dv.MapView.NewBitmapCreated(null);
                }
            }
            else
            {
                _activeDataView.MapView.NewBitmapCreated(bitmap);
            }
        }

        private void MakeMapViewRefresh()
        {
            if (_activeDataView == null)
            {
                return;
            }

            _activeDataView.MapView.MakeMapViewRefresh();
        }

        private void OnDrawingLayer(string layerName)
        {
            if (_activeDataView == null)
            {
                return;
            }

            _activeDataView.MapView.OnDrawingLayer(layerName);
        }

        private void Display_MapScaleChanged(IDisplay display)
        {
            if (_activeDataView == null)
            {
                return;
            }

            if (_activeDataView.Envelope != null)
            {
                _activeDataView.Envelope.minx = display.Envelope.minx;
                _activeDataView.Envelope.miny = display.Envelope.miny;
                _activeDataView.Envelope.maxx = display.Envelope.maxx;
                _activeDataView.Envelope.maxy = display.Envelope.maxy;
            }

            IsDirty = true;
        }

        private void mapDocument_AfterSetFocusMap(IMap map)
        {
            foreach (DataView dv in _dataViews)
            {
                if (dv.Map == map)
                {
                    dv.MapView.Tool = _activeDataView.MapView.Tool;
                    _activeDataView = dv;
                    break;
                }
            }

            IsDirty = true;

            if (!_dataViewContainer.Children.Contains(_activeDataView.LayoutDocument))
            {
                _dataViewContainer.Children.Add(_activeDataView.LayoutDocument);
            }

            _dataViewContainer.SelectedContentIndex = _dataViewContainer.Children.IndexOf(_activeDataView.LayoutDocument);
        }

        private void mapDocument_MapAdded(IMap map)
        {
            map.TOCChanged += new TOCChangedEvent(Map_TOCChanged);

            IsDirty = true;
        }

        void mapDocument_MapDeleted(IMap map)
        {
            DataView dv = this.GetDataView(map);
            if (dv != null)
            {
                _dataViews.Remove(dv);
                _dataViewContainer.Children.Remove(dv.LayoutDocument);
            }
        }

        void mapDocument_LayerRemoved(IMap sender, gView.Framework.Data.ILayer layer)
        {
            // private TOC verständigen
            foreach (DataView dv in _dataViews)
            {
                if (dv.TOC is TOC && dv.Map == sender)
                {
                    ((TOC)dv.TOC).RemoveLayer(layer);
                }
            }
        }

        void mapDocument_LayerAdded(IMap sender, gView.Framework.Data.ILayer layer)
        {
            // private TOC verständigen
            foreach (DataView dv in _dataViews)
            {
                if (dv.TOC is TOC && dv.Map == sender)
                {
                    ((TOC)dv.TOC).AddLayer(layer, null);
                }
            }
        }

        async private void Map_TOCChanged(IMap map)
        {
            foreach (IDockableWindow win in _dockWindows)
            {
                if (win is TOCControl)
                {
                    await ((TOCControl)win).RefreshList();
                }
            }
        }

        public bool InvokeRequired
        {
            get { return _appWindow.InvokeRequired; }
        }

        public object Invoke(Delegate method)
        {
            return _appWindow.Invoke(method);
        }

        public bool IsDirty
        {
            get { return _dirty; }
            set
            {
                _dirty = value;
                ValidateUI();
            }
        }

        public void ValidateUI()
        {
            if (_appWindow != null)
            {
                _appWindow.ValidateButtons();
                _appWindow.ValidateToolbars();
            }
        }

        public IStatusBar StatusBar
        {
            get { return new MapStatusBar(_appWindow); }
        }

        public void AppendContextMenuItems(ContextMenuStrip strip, object context)
        {

        }

        public bool SaveDirtyDocument()
        {
            if (!IsDirty)
            {
                return true;
            }

            switch (MessageBox.Show("Save changes in document?", "gView.Carto", MessageBoxButtons.YesNoCancel))
            {
                case DialogResult.No:
                    _doc.RemoveTemporeryRestore();
                    return true;
                case DialogResult.Yes:
                    this.SaveMapDocument(_docFilename, true);
                    return true;
            }
            return false;
        }

        public void SetCursor(object cursor)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.SetCursor(cursor);
                });
            }
            else
            {
                _appWindow.Cursor = gView.Desktop.Wpf.CursorFactory.ToWpfCursor(cursor);
                var focused = System.Windows.Input.FocusManager.GetFocusedElement(_appWindow);
                if (focused is WindowsFormsHost && ((WindowsFormsHost)focused).Child != null)
                {
                    ((WindowsFormsHost)focused).Child.Cursor = gView.Desktop.Wpf.CursorFactory.ToFormsCursor(cursor);
                }
            }
        }

        #region IApplication

        public string Title
        {
            get
            {
                if (_appWindow != null)
                {
                    return _appWindow.Title;
                }
                else
                {
                    return "";
                }
            }
            set
            {
                if (_appWindow != null)
                {
                    _appWindow.Title = value;
                }
            }
        }

        public void Exit()
        {
            if (_appWindow != null)
            {
                _appWindow.Close();
            }
        }

        #endregion

        #region IMapAppliction Members

        ArrayList _dockWindows = new ArrayList();

        public event DockWindowAddedEvent DockWindowAdded = null;
        public event OnShowDockableWindowEvent OnShowDockableWindow = null;
        public event AfterLoadMapDocumentEvent AfterLoadMapDocument = null;
        public event ActiveMapToolChangedEvent ActiveMapToolChanged;
        public event OnCursorPosChangedEvent OnCursorPosChanged;

        public List<IDockableWindow> DockableWindows
        {
            get
            {
                List<IDockableWindow> e = new List<IDockableWindow>();

                foreach (IDockableWindow window in _dockWindows)
                {
                    e.Add(window);
                }
                return e;
            }
        }

        public void AddDockableWindow(IDockableWindow window, DockWindowState state)
        {
            if (window != null)
            {
                foreach (IDockableWindow win in _dockWindows)
                {
                    if (win == window)
                    {
                        return;
                    }
                }

                window.DockableWindowState = state;
                _dockWindows.Add(window);
            }
            if (DockWindowAdded != null)
            {
                DockWindowAdded(window, "");
            }
        }

        public void AddDockableWindow(IDockableWindow window, string parentDockableWindowName)
        {
            if (window != null)
            {
                foreach (IDockableWindow win in _dockWindows)
                {
                    if (win == window)
                    {
                        return;
                    }
                }
                /*
                if (parentDockableWindowName != "")
                {
                    window.DockableWindowState = DockWindowState.child;
                }
                else */
                if (window is IDockableToolWindow)
                {
                    window.DockableWindowState = DockWindowState.right;
                    foreach (IDockableWindow win in _dockWindows)
                    {
                        if (win is IDockableToolWindow)
                        {
                            parentDockableWindowName = win.Name;
                            window.DockableWindowState = DockWindowState.child;
                            break;
                        }
                    }
                }
                _dockWindows.Add(window);
            }
            if (DockWindowAdded != null)
            {
                DockWindowAdded(window, parentDockableWindowName);
            }
        }

        public void ShowDockableWindow(IDockableWindow window)
        {
            if (window is Form)
            {
                ((Form)window).Visible = true;
            }
            if (OnShowDockableWindow != null)
            {
                OnShowDockableWindow(window);
            }
        }

        public ITool Tool(Guid guid)
        {
            foreach (ITool tool in _tools)
            {
                if (PlugInManager.PlugInID(tool) == guid)
                {
                    return tool;
                }
            }
            return null;
        }
        public List<ITool> Tools
        {
            get
            {
                if (_tools == null)
                {
                    return new List<ITool>();
                }

                return gView.Framework.system.ListOperations<ITool>.Clone(_tools);
            }
        }

        public IToolbar Toolbar(Guid guid)
        {
            if (_appWindow == null)
            {
                return null;
            }

            foreach (ToolbarStrip strip in _appWindow.ToolbarStrips)
            {
                if (strip == null)
                {
                    continue;
                }

                if (PlugInManager.PlugInID(strip) == guid)
                {
                    return strip;
                }
            }
            return null;
        }
        public List<IToolbar> Toolbars
        {
            get
            {
                List<IToolbar> toolbars = new List<IToolbar>();
                if (_appWindow == null)
                {
                    return toolbars;
                }

                foreach (ToolbarStrip strip in _appWindow.ToolbarStrips)
                {
                    if (strip == null)
                    {
                        continue;
                    }

                    toolbars.Add(strip);
                }

                return toolbars;
            }
        }
        internal void AddTool(ITool tool)
        {
            if (tool == null)
            {
                return;
            }

            if (this.Tool(PlugInManager.PlugInID(tool)) != null)
            {
                return; // Schon vorhanden
            }

            _tools.Add(tool);
        }
        internal void SendOnCreate2Tools(IMapDocument doc)
        {
            foreach (ITool tool in _tools)
            {
                try
                {
                    tool.OnCreate(doc);
                }
                catch (Exception ex)
                {
                    throw new Exception("Can't create tool '" + tool.Name + "'", ex);
                }
            }
        }

        public IGUIAppWindow ApplicationWindow
        {
            get
            {
                return _appWindow;
            }
        }

        async public Task LoadMapDocument(string filename)
        {
            if (_doc == null || filename == "")
            {
                return;
            }

            _docFilename = filename;

            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            if (!fi.Exists)
            {
                return;
            }

            if (fi.Extension.ToLower() != ".axl")
            {
                RemoveAllDataViews();
                if (_appWindow != null)
                {
                    _appWindow.RemoveAllToolbars();
                }

                _activeDataView = null;
            }

            XmlStream stream = new XmlStream("");

            if (fi.Extension.ToLower() == ".rdm")
            {
                StreamReader sr = new StreamReader(filename);
                byte[] bytes = new byte[fi.Length];
                BinaryReader br = new BinaryReader(sr.BaseStream);
                br.Read(bytes, 0, bytes.Length);
                sr.Close();

                bytes = Crypto.Decrypt(bytes, _cryptoKey);

                MemoryStream ms = new MemoryStream(bytes);
                stream.ReadStream(ms);
                ms.Close();
            }
            else
            {
                stream.ReadStream(filename);
            }

            while (_doc.Maps.Count() > 0)
            {
                _doc.RemoveMap(_doc.Maps.First());
            }

            _dataViews.Clear();

            await stream.LoadAsync("MapDocument", _doc);

            // Load DataViews...
            DataView dv;
            while ((dv = (DataView)stream.Load("DataView", null, new DataView(_doc.Maps))) != null)
            {
                if (!(dv.Map is Map))
                {
                    continue;
                }

                DataView dataView = _appWindow.AddDataView((Map)dv.Map);
                if (dataView == null)
                {
                    continue;
                }

                dataView.Envelope = dv.Envelope;
                dataView.TOC = dv.TOC;
                dataView.DisplayRotation = dv.DisplayRotation;

                if (_activeDataView == null)
                {
                    _activeDataView = dataView;
                }
            }

            if (_dataViews.Count == 0 && _doc.Maps.Count() > 0)
            {
                //_appWindow.AddDataView((Map)_doc.Maps[0]);
                _activeDataView = _dataViews[0];
            }

            if (_activeDataView != null && _activeDataView.MapView != null)
            {
                _activeDataView.MapView.Tool = this.ActiveTool;
            }

            ValidateUI();

            IsDirty = false;

            if (fi.Extension.ToLower() == ".restore")
            {
                IsDirty = true;
                fi = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - ".restore".Length));
                _docFilename = fi.FullName;
            }

            _appWindow.Title = "gView.Carto " + fi.Name;
            _readonly = (fi.Extension.ToLower() == ".rdm");

            foreach (var map in _doc.Maps)
            {
                if (map.HasErrorMessages)
                {
                    FormToolException.Show($"Loading Map '{map.Name}'", String.Join(Environment.NewLine, map.ErrorMessages));
                }
            }

            if (AfterLoadMapDocument != null)
            {
                AfterLoadMapDocument(_doc);
            }
        }

        public void SaveMapDocument()
        {
            SaveMapDocument(_docFilename, true);
        }
        public void SaveMapDocument(string filename, bool performEncryption)
        {
            if (filename == "")
            {
                return;
            }

            if (filename.ToLower() == SystemVariables.ApplicationDirectory.ToLower() + @"/normal.mxl" ||
                filename == "normal.mxl")
            {
                switch (MessageBox.Show("Override normal.mxl?", "gView.Carto", MessageBoxButtons.YesNo))
                {
                    case DialogResult.No:
                        return;
                }
            }

            foreach (var map in _doc.Maps)
            {
                if (map is Map && ((Map)map).HasErrorMessages)
                {
                    MessageBox.Show("A map with errors can't be saved. Solve errors in MXL File (e.g. with a Texteditor) and reopen?", "gView.Carto", MessageBoxButtons.OK);
                    return;
                }
            }

            XmlStream stream = new XmlStream("MapApplication", performEncryption);
            stream.Save("MapDocument", _doc);

            foreach (DataView dv in _dataViews)
            {
                stream.Save("DataView", dv);
            }

            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            if (fi.Extension.ToLower() == ".rdm")
            {
                StringBuilder sb = new StringBuilder();
                StringWriter strwr = new StringWriter(sb);
                stream.WriteStream(strwr, Formatting.None);
                strwr.Close();

                byte[] bytes = Encoding.Unicode.GetBytes(sb.ToString());
                bytes = Crypto.Encrypt(bytes, _cryptoKey);
                StreamWriter sw = new StreamWriter(fi.FullName);
                BinaryWriter bw = new BinaryWriter(sw.BaseStream);
                bw.Write(bytes);
                sw.Close();
            }
            else if (fi.Extension.ToLower() == ".axl")
            {
                MessageBox.Show("Can't save AXL Documents...", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                stream.WriteStream(filename);
            }

            if (fi.Extension.ToLower() != ".restore")
            {
                IsDirty = false;
                _docFilename = filename;


                _appWindow.Title = "gView.Carto " + fi.Name;
            }
        }

        public string DocumentFilename
        {
            get { return _docFilename; }
            set
            {
                if (value.Trim() == String.Empty)
                {
                    if (_docFilename.Trim() == String.Empty)
                    {
                        _docFilename = SystemVariables.ApplicationDirectory + @"/NewDocument.mxl";
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(_docFilename);
                        _docFilename = fi.DirectoryName + @"/NewDocument.mxl";
                    }
                }
                else
                {
                    _docFilename = value;
                }

                try
                {
                    FileInfo fi = new FileInfo(_docFilename);
                    _appWindow.Title = "gView.Carto " + fi.Name;
                }
                catch { }
            }
        }

        DateTime _lastRefresh = DateTime.Now;
        static private object refreshLock = new object();
        public Task RefreshActiveMap(DrawPhase drawPhase)
        {
            if (_activeDataView == null)
            {
                return Task.CompletedTask;
            }

            if (_activeDataView.MapView == null)
            {
                return Task.CompletedTask;
            }

            lock (refreshLock)
            {
                TimeSpan ts = DateTime.Now - _lastRefresh;
                if (ts.TotalMilliseconds < 300)
                {
                    _appWindow.Cursor = System.Windows.Input.Cursors.Wait;
                    //MessageBox.Show("Slower... !!!");
                    Thread.Sleep((int)(300 - ts.TotalMilliseconds));
                    _appWindow.Cursor = System.Windows.Input.Cursors.Arrow;
                }

                _activeDataView.MapView.RefreshCopyrightVisibility();
                _lastRefresh = DateTime.Now;

                Task.Run(async () =>
                {
                    await this.RefreshActiveMapThread(drawPhase);
                });
            }

            return Task.CompletedTask;
        }

        async public Task RefreshTOC()
        {
            if (_appWindow != null)
            {
                await _appWindow.RefreshTOC();
            }
        }

        public void RefreshTOCElement(IDatasetElement element)
        {
            if (_appWindow != null)
            {
                _appWindow.RefreshTOCElement(element);
            }
        }

        public IMapApplicationModule IMapApplicationModule(Guid guid)
        {
            if (_appWindow == null)
            {
                return null;
            }

            return _appWindow.MapApplicationModule(guid);
        }

        public bool ReadOnly
        {
            get { return _readonly; }
        }

        public void DrawReversibleGeometry(IGeometry geometry, System.Drawing.Color color)
        {
            DataView actDataView = ActiveDataView;
            if (actDataView == null || actDataView.MapView == null || actDataView.Map == null)
            {
                return;
            }

            actDataView.MapView.DrawReversibleGeometry(
                actDataView.Map.Display,
                geometry,
                color);
        }
        #endregion

        //private void RefreshActiveMapThread(object drawPhase)
        private Task RefreshActiveMapThread(object drawPhase)
        {
            if (_activeDataView == null)
            {
                return Task.CompletedTask;
            }

            if (_activeDataView.MapView == null)
            {
                return Task.CompletedTask;
            }

            _activeDataView.MapView.RefreshMap((DrawPhase)drawPhase);

            return Task.CompletedTask;
        }

        public IDocumentWindow DocumentWindow
        {
            get { return _appWindow; }
        }

        #region DataView
        internal event DataViewRemovedEvent DataViewRemoved = null;

        public bool AddDataView(DataView dataView)
        {
            foreach (DataView dv in _dataViews)
            {
                if (dv.Name == dataView.Name)
                {
                    return false;
                }
            }

            WindowsFormsHost winHost = new WindowsFormsHost();
            winHost.Child = dataView.MapView;
            LayoutDocument layoutDoc = new LayoutDocument();
            layoutDoc.Title = dataView.Name;
            layoutDoc.Content = winHost;
            dataView.TabPage = layoutDoc;
            ((LayoutDocumentPane)_dataViewContainer).Children.Add(layoutDoc);

            _dataViews.Add(dataView);
            if (_activeDataView == null)
            {
                _activeDataView = dataView;
            }

            IsDirty = true;

            dataView.DataViewRenamed += new EventHandler(dataView_DataViewRenamed);

            layoutDoc.CanClose = layoutDoc.CanFloat = false;

            dataView.LayoutDocument = layoutDoc;
            return true;
        }

        void dataView_DataViewRenamed(object sender, EventArgs e)
        {
            try
            {
                DataView dv = (DataView)sender;
                foreach (LayoutDocument document in ((ILayoutDocumentPane)_dataViewContainer).Children)
                {
                    if (document.Content is WindowsFormsHost &&
                        ((WindowsFormsHost)document.Content).Child is MapView)
                    {
                        if (dv.MapView == (MapView)((WindowsFormsHost)document.Content).Child)
                        {
                            document.Title = dv.Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataView ByLayoutDocument(LayoutDocument layoutDocument)
        {
            foreach (DataView dv in _dataViews)
            {
                if (dv.TabPage == layoutDocument)
                {
                    return dv;
                }
            }
            return null;
        }

        public bool RemoveDataView(string name)
        {
            DataView dataView = null;
            foreach (DataView dv in _dataViews)
            {
                if (dv.Name == name)
                {
                    dataView = dv;
                    dataView.MapView.Dispose();
                    break;
                }
            }
            if (dataView == null)
            {
                return false;
            }

            _dataViews.Remove(dataView);
            if (DataViewRemoved != null)
            {
                DataViewRemoved(dataView);
            }

            LayoutDocument layoutDocument = dataView.TabPage;
            ((LayoutDocumentPane)_dataViewContainer).RemoveChild(layoutDocument);

            return true;
        }

        public void RemoveAllDataViews()
        {
            List<string> names = new List<string>();
            foreach (DataView dv in _dataViews)
            {
                names.Add(dv.Name);
            }

            foreach (string name in names)
            {
                this.RemoveDataView(name);
            }
        }

        public DataView ActiveDataView
        {
            get
            {
                return _activeDataView;
            }
            set
            {
                foreach (DataView dv in _dataViews)
                {
                    if (dv == value)
                    {
                        if (_activeDataView != null && _activeDataView.Envelope == null && _activeDataView.Map != null && _activeDataView.Map.Display != null)
                        {
                            SetPublicEnvelope(_activeDataView.Map, _activeDataView.Map.Display.Envelope);
                        }
                        _activeDataView = dv;
                        if (_activeDataView.Map != null && _activeDataView.Map.Display != null)
                        {
                            if (_activeDataView.Envelope != null)
                            {
                                _activeDataView.Map.Display.ZoomTo(_activeDataView.Envelope);
                            }
                            else
                            {
                                IEnvelope envelope = GetPublicEnvelope(_activeDataView.Map);
                                if (envelope != null)
                                {
                                    _activeDataView.Map.Display.ZoomTo(envelope);
                                }
                            }
                        }
                        if (_activeDataView.Map is Map)
                        {
                            ((Map)_activeDataView.Map).DataViewTOC = _activeDataView.TOC as TOC;
                            _activeDataView.Map.Display.DisplayTransformation.DisplayRotation = _activeDataView.DisplayRotation;
                        }
                        return;
                    }
                }
            }
        }
        public string ActiveDataViewName
        {
            get
            {
                if (_activeDataView != null)
                {
                    return _activeDataView.Name;
                }

                return "";
            }
            set
            {
                foreach (DataView dv in _dataViews)
                {
                    if (dv.Name == value)
                    {
                        ActiveDataView = dv;
                        IsDirty = true;
                        return;
                    }
                }
            }
        }

        public DataView GetDataView(string name)
        {
            foreach (DataView dv in _dataViews)
            {
                if (dv.Name == name)
                {
                    return dv;
                }
            }
            return null;
        }

        public DataView GetDataView(IMap map)
        {
            foreach (DataView dv in _dataViews)
            {
                if (dv.Map == map)
                {
                    return dv;
                }
            }
            return null;
        }
        /*
        public void ChangeDataViewMap(DataView dataView, IMap map)
        {
            if (!(map is Map) || dataView == null) return;
            if (map != dataView.Map)
            {
                dataView.MapView.CancelDrawing(DrawPhase.All);
                dataView.Map = (Map)map;
                dataView.MapView.RefreshMap(DrawPhase.All);
            }

            IsDirty = true;
        }
         * */
        #endregion

        public ITool ActiveTool
        {
            get
            {
                if (_appWindow == null)
                {
                    return null;
                }

                return _appWindow.ActiveTool;
            }
            set
            {
                if (_appWindow == null)
                {
                    return;
                }

                _appWindow.ActiveTool = value;
            }
        }

        internal void MapToolChanged(ITool oldTool, ITool newTool)
        {
            if (newTool != null)
            {
                if (_appWindow != null)
                {
                    _appWindow.SetPanelImage(0, newTool.Image as System.Drawing.Image);
                    _appWindow.SetPanelText(0, (!String.IsNullOrEmpty(newTool.ToolTip)) ? newTool.ToolTip : newTool.Name);
                }
            }
            if (ActiveMapToolChanged != null)
            {
                ActiveMapToolChanged(oldTool, newTool);
            }
        }

        internal void CursorPos(double X, double Y)
        {
            if (OnCursorPosChanged != null)
            {
                OnCursorPosChanged(X, Y);
            }
        }
        private IEnvelope GetPublicEnvelope(IMap map)
        {
            if (map != null && map.Display != null && map.Display.Envelope != null)
            {
                IEnvelope publicEnv;
                if (!_publicExtents.TryGetValue(map, out publicEnv))
                {
                    _publicExtents.Add(map, map.Display.Envelope);
                }

                return _publicExtents[map];
            }
            return null;
        }

        private void SetPublicEnvelope(IMap map, IEnvelope envelope)
        {
            if (map == null || envelope == null)
            {
                return;
            }

            IEnvelope ext;
            if (_publicExtents.TryGetValue(map, out ext))
            {
                _publicExtents[map] = envelope;
            }
            else
            {
                _publicExtents.Add(map, envelope);
            }
        }

        #region Helpers
        #region Axl Import
        private void SetLayernameAndScales(XmlNode layerNode, ILayer fLayer)
        {
            if (_doc != null && _doc.FocusMap != null && _doc.FocusMap.TOC != null)
            {
                ITocElement tocElement = _doc.FocusMap.TOC.GetTOCElement(fLayer);
                tocElement.Name = layerNode.Attributes["name"].Value;
                if (layerNode.Attributes["visible"] != null)
                {
                    tocElement.LayerVisible = Convert.ToBoolean(layerNode.Attributes["visible"].Value);
                }
            }

            if (layerNode.Attributes["minscale"] != null)
            {
                string[] s = layerNode.Attributes["minscale"].Value.Split(':');
                double o;
                if (s.Length == 2)
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MinimumScale = o;
                    }
                }
                else
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MinimumScale = o * (96.0 / 0.254);
                    }
                }
            }
            if (layerNode.Attributes["maxscale"] != null)
            {
                string[] s = layerNode.Attributes["maxscale"].Value.Split(':');
                double o;
                if (s.Length == 2)
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MaximumScale = o;
                    }
                }
                else
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MaximumScale = o * (96.0 / 0.254);
                    }
                }
            }
        }

        private void SetRenderers(XmlNode layerNode, IFeatureLayer fLayer)
        {
            fLayer.FeatureRenderer = null;
            fLayer.LabelRenderer = null;
            foreach (XmlNode child in layerNode.ChildNodes)
            {
                if (child.Name == "SIMPLERENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.SimpleRenderer(child);
                }
                else if (child.Name == "VALUEMAPRENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.ValueMapRenderer(child);
                }
                else if (child.Name == "SIMPLELABELRENDERER")
                {
                    fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child, fLayer.FeatureClass);
                }
                else if (child.Name == "SCALEDEPENDENTRENDERER")
                {
                    if (child.SelectSingleNode("SIMPLELABELRENDERER") != null)
                    {
                        fLayer.LabelRenderer = ObjectFromAXLFactory.ScaleDependentLabelRenderer(child, fLayer.FeatureClass);
                    }
                    if (child.SelectSingleNode("SIMPLERENDERER") != null ||
                        child.SelectSingleNode("VALUEMAPRENDERER") != null)
                    {
                        fLayer.FeatureRenderer = ObjectFromAXLFactory.ScaleDependentRenderer(child);
                    }
                }
                else if (child.Name == "GROUPRENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.GroupRenderer(child);

                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name == "SIMPLELABELRENDERER")
                        {
                            fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child2, fLayer.FeatureClass);
                        }
                        else if (child2.Name == "SCALEDEPENDENTRENDERER")
                        {
                            if (child2.SelectSingleNode("SIMPLELABELRENDERER") != null)
                            {
                                fLayer.LabelRenderer = ObjectFromAXLFactory.ScaleDependentLabelRenderer(child2, fLayer.FeatureClass);
                            }
                            //if (child2.SelectSingleNode("SIMPLERENDERER") != null ||
                            //    child2.SelectSingleNode("VALUEMAPRENDERER") != null)
                            //{
                            //    fLayer.FeatureRenderer = ObjectFromAXLFactory.ScaleDependentRenderer(child2);
                            //}
                        }
                    }
                }
            }

            #region Renderer vereinfachen
            fLayer.FeatureRenderer = SimplifyRenderer(fLayer.FeatureRenderer) as IFeatureRenderer;
            #endregion
        }

        private IRenderer SimplifyRenderer(IRenderer renderer)
        {
            if (renderer is ISimplify)
            {
                ((ISimplify)renderer).Simplify();
            }

            if (renderer is IGroupRenderer)
            {
                IGroupRenderer gRenderer = (IGroupRenderer)renderer;
                if (gRenderer.Renderers.Count == 1)
                {
                    if (gRenderer.Renderers[0] is IScaledependent)
                    {
                    }
                    else
                    {
                        renderer = gRenderer.Renderers[0];
                    }
                }
            }
            return renderer;
            //if (renderer is IGroupRenderer)
            //{
            //    IGroupRenderer gRenderer = (IGroupRenderer)renderer;
            //    if (gRenderer.Renderers.Count == 0)
            //        return renderer;

            //    bool allSimpleRenderers = true;
            //    foreach (IRenderer cRenderer in gRenderer.Renderers)
            //    {
            //        if (!(cRenderer is SimpleRenderer))
            //        {
            //            allSimpleRenderers = false;
            //            break;
            //        }
            //    }
            //    if (allSimpleRenderers)
            //    {
            //        renderer = gRenderer.Renderers[0];
            //        if (gRenderer.Renderers.Count > 1)
            //        {
            //            ISymbolCollection symCol=PlugInManager.Create(new Guid("062AD1EA-A93C-4c3c-8690-830E65DC6D91")) as ISymbolCollection;
            //            foreach (IRenderer cRenderer in gRenderer.Renderers)
            //            {
            //                if (((SimpleRenderer)cRenderer).Symbol != null)
            //                    symCol.AddSymbol(((SimpleRenderer)cRenderer).Symbol);
            //            }
            //        }
            //        return renderer;
            //    }

            //    for(int i=0;i<gRenderer.Renderers.Count;i++)
            //    {
            //        if (gRenderer.Renderers[i] is IGroupRenderer)
            //        {
            //            IFeatureRenderer s = SimplifyRenderer(gRenderer.Renderers[i]) as IFeatureRenderer;
            //            if (s != gRenderer.Renderers[i])
            //            {
            //                gRenderer.Renderers.RemoveAt(i);
            //                gRenderer.Renderers.Insert(i, s);
            //            }
            //        }
            //    }
            //}

            //return renderer;
        }
        #endregion
        #endregion

        #region IGUIApplication Member

        public void ShowBackstageMenu()
        {
            _appWindow.ShowBackstageMenu();
        }

        public void HideBackstageMenu()
        {
            _appWindow.HideBackstageMenu();
        }

        #endregion
    }

    internal class MapStatusBar : IStatusBar
    {
        private gView.Win.Carto.MainWindow _appWin;

        public MapStatusBar(gView.Win.Carto.MainWindow app)
        {
            _appWin = app;
        }

        #region IStatusBar Member

        public string Text
        {
            set
            {
                if (_appWin == null)
                {
                    return;
                }

                _appWin.SetPanelText(0, value);
            }
        }
        public System.Drawing.Image Image
        {
            set
            {
                if (_appWin == null)
                {
                    return;
                }

                _appWin.SetPanelImage(0, value);
            }
        }
        #endregion

        #region IStatusBar Member

        public bool ProgressVisible
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public int ProgressValue
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public void Refresh()
        {
        }
        #endregion
    }
}
