using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.system;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Framework.UI.Controls
{
    [gView.Framework.system.RegisterPlugIn("3143FA90-587B-4cd0-B508-1BE88882E6B3")]
    public partial class PreviewControl : UserControl, IExplorerTabPage
    {
        private IExplorerObject _exObject = null;
        private ToolButton _activeToolButton;
        private MapDocument _mapDocument;
        private IMap _map;
        private List<ITool> _tools = new List<ITool>();
        private bool _exObjectInvokeRequired = false;

        public PreviewControl()
        {
            InitializeComponent();
        }

        #region IExplorerTabPage Members

        public Control Control
        {
            get { return this; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IGUIApplication)
            {
                _mapDocument = new MapDocument(new ExplorerMapApplication((IGUIApplication)hook, this, mapView1));
                _map = new Map();
                _map.Display.ReferenceScale = 0;
                _map.DrawingLayer += new DrawingLayerEvent(DrawingLayer);

                _mapDocument.AddMap(_map);
                _mapDocument.FocusMap = _map;

                PlugInManager compMan = new PlugInManager();

                ITool zoomin = compMan.CreateInstance(KnownObjects.Tools_DynamicZoomIn) as ITool;
                ITool zoomout = compMan.CreateInstance(KnownObjects.Tools_DynamicZoomOut) as ITool;
                ITool smartNav = compMan.CreateInstance(KnownObjects.Tools_SmartNavigation) as ITool;
                ITool pan = compMan.CreateInstance(KnownObjects.Tools_Pan) as ITool;
                //ITool zoomextent = compMan.CreateInstance(KnownObjects.Tools_Zoom2Extent) as ITool;
                //ITool toc = compMan.CreateInstance(KnownObjects.Tools_TOC) as ITool;

                ITool identify = compMan.CreateInstance(KnownObjects.Tools_Identify) as ITool;
                ITool queryCombo = compMan.CreateInstance(KnownObjects.Tools_QueryThemeCombo) as ITool;

                if (zoomin != null)
                {
                    zoomin.OnCreate(_mapDocument);
                }

                if (zoomout != null)
                {
                    zoomout.OnCreate(_mapDocument);
                }

                if (smartNav != null)
                {
                    smartNav.OnCreate(_mapDocument);
                }

                if (pan != null)
                {
                    pan.OnCreate(_mapDocument);
                }

                if (identify != null)
                {
                    identify.OnCreate(_mapDocument);
                    identify.OnCreate(this);
                }
                if (queryCombo != null)
                {
                    queryCombo.OnCreate(_mapDocument);
                }
                //if (zoomextent != null) zoomextent.OnCreate(_mapDocument);
                //if (toc != null) toc.OnCreate(_mapDocument);

                if (zoomin != null)
                {
                    toolStrip.Items.Add(new ToolButton(zoomin));
                }

                if (zoomout != null)
                {
                    toolStrip.Items.Add(new ToolButton(zoomout));
                }

                if (smartNav != null)
                {
                    toolStrip.Items.Add(_activeToolButton = new ToolButton(smartNav));
                }

                if (pan != null)
                {
                    toolStrip.Items.Add(new ToolButton(pan));
                }
                //if(zoomextent!=null) toolStrip.Items.Add(new ToolButton(zoomextent));
                //if(toc!=null) toolStrip.Items.Add(new ToolButton(toc));
                if (identify != null)
                {
                    toolStrip.Items.Add(new ToolButton(identify));
                }

                if (queryCombo is IToolItem)
                {
                    toolStrip.Items.Add(((IToolItem)queryCombo).ToolItem);
                }

                if (zoomin != null)
                {
                    _tools.Add(zoomin);
                }

                if (zoomout != null)
                {
                    _tools.Add(zoomout);
                }

                if (smartNav != null)
                {
                    _tools.Add(smartNav);
                }

                if (pan != null)
                {
                    _tools.Add(pan);
                }
                //if (zoomextent != null) _tools.Add(zoomextent);
                //if (toc != null) _tools.Add(toc);
                if (identify != null)
                {
                    _tools.Add(identify);
                }

                if (queryCombo != null)
                {
                    _tools.Add(queryCombo);
                }

                _activeToolButton.Checked = true;
                mapView1.Map = _map;

                _map.NewBitmap += InvokeNewBitmapCreated;
                _map.DoRefreshMapView += InvokeDoRefreshMapView;

                mapView1.Tool = _activeToolButton.Tool;
            }
        }

        private void InvokeDoRefreshMapView()
        {
            mapView1.MakeMapViewRefresh();

        }

        private void InvokeNewBitmapCreated(IBitmap image)
        {
            mapView1.NewBitmapCreated(image);
        }

        public Task<bool> OnShow()
        {
            // Starts Refresh in the background, do NOT await!!!! Otherwise it blocks UI
            Task.Run(async () =>
            {
                if (_exObjectInvokeRequired)
                {
                    await InvokeSetExplorerObject();
                }

                await Task.Delay(100);
                mapView1.RefreshMap(DrawPhase.Geography);
            });
            return Task.FromResult(true);
        }

        public void OnHide()
        {
            mapView1.CancelDrawing(DrawPhase.All);
        }

        async private Task InvokeSetExplorerObject()
        {
            _exObjectInvokeRequired = false;

            mapView1.CancelDrawing(DrawPhase.All);
            foreach (IDatasetElement element in _map.MapElements)
            {
                _map.RemoveLayer(element as ILayer);
            }

            if (_exObject != null)
            {
                var instance = await _exObject.GetInstanceAsync();
                if (instance is IFeatureClass && ((IFeatureClass)instance).Envelope != null)
                {
                    mapView1.Map = _map;
                    _map.AddLayer(LayerFactory.Create(instance as IClass));
                    _map.Display.Limit = ((IFeatureClass)instance).Envelope;
                    _map.Display.ZoomTo(((IFeatureClass)instance).Envelope);
                }
                else if (instance is IRasterClass && ((IRasterClass)instance).Polygon != null)
                {
                    mapView1.Map = _map;
                    _map.AddLayer(LayerFactory.Create(instance as IClass));
                    _map.Display.Limit = ((IRasterClass)instance).Polygon.Envelope;
                    _map.Display.ZoomTo(((IRasterClass)instance).Polygon.Envelope);
                }
                else if (instance is IWebServiceClass && ((IWebServiceClass)instance).Envelope != null)
                {
                    mapView1.Map = _map;
                    _map.AddLayer(LayerFactory.Create(instance as IClass));
                    _map.Display.Limit = ((IWebServiceClass)instance).Envelope;
                    _map.Display.ZoomTo(((IWebServiceClass)instance).Envelope);
                }
                else if (instance is IFeatureDataset)
                {
                    mapView1.Map = _map;
                    IFeatureDataset dataset = (IFeatureDataset)instance;
                    foreach (IDatasetElement element in await dataset.Elements())
                    {
                        ILayer layer = LayerFactory.Create(element.Class) as ILayer;
                        if (layer == null)
                        {
                            continue;
                        }

                        _map.AddLayer(layer);
                    }
                    _map.Display.Limit = await dataset.Envelope();
                    _map.Display.ZoomTo(await dataset.Envelope());
                }
                else if (instance is Map)
                {
                    Map map = (Map)instance;

                    map.NewBitmap -= InvokeNewBitmapCreated;
                    map.DoRefreshMapView -= InvokeDoRefreshMapView;

                    map.NewBitmap += InvokeNewBitmapCreated;
                    map.DoRefreshMapView += InvokeDoRefreshMapView;

                    mapView1.Map = (Map)instance;
                }
            }
        }

        public IExplorerObject GetExplorerObject()
        {
            return _exObject;
        }
        public Task SetExplorerObjectAsync(IExplorerObject value)
        {
            if (_exObject == value || _map == null)
            {
                return Task.CompletedTask;
            }

            _exObject = value;
            _exObjectInvokeRequired = true;

            return Task.CompletedTask;
        }

        public Task<bool> ShowWith(IExplorerObject exObject)
        {
            if (exObject == null)
            {
                return Task.FromResult(false);
            }

            if (TypeHelper.Match(exObject.ObjectType, typeof(IFeatureClass)) ||
                TypeHelper.Match(exObject.ObjectType, typeof(IRasterClass)) ||
                TypeHelper.Match(exObject.ObjectType, typeof(IWebServiceClass)) ||
                TypeHelper.Match(exObject.ObjectType, typeof(IFeatureDataset)) ||
                TypeHelper.Match(exObject.ObjectType, typeof(Map)))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public string Title
        {
            get { return "Preview"; }
        }

        async public Task<bool> RefreshContents()
        {
            if (_exObjectInvokeRequired)
            {
                await InvokeSetExplorerObject();
            }

            mapView1.CancelDrawing(DrawPhase.All);
            mapView1.RefreshMap(DrawPhase.All);

            return true;
        }

        #endregion

        private void toolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!(e.ClickedItem is ToolButton) || _activeToolButton == e.ClickedItem)
            {
                return;
            }

            _activeToolButton.Checked = false;
            _activeToolButton = (ToolButton)e.ClickedItem;
            _activeToolButton.Checked = true;

            mapView1.Tool = _activeToolButton.Tool;
        }

        internal ITool Tool(Guid guid)
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

        public Task RefreshMap()
        {
            mapView1.RefreshMap(DrawPhase.All);

            return Task.CompletedTask;
        }

        #region IOrder Members

        public int SortOrder
        {
            get { return 10; }
        }

        #endregion

        #region mapView

        private delegate void DrawingLayerCallback(string layerName);
        private void DrawingLayer(string layerName)
        {
            if (statusStrip1.InvokeRequired)
            {
                DrawingLayerCallback d = new DrawingLayerCallback(DrawingLayer);
                statusStrip1.Invoke(d, new object[] { layerName });
            }
            else
            {
                if (layerName == "")
                {
                    toolStripStatusLabel1.Text = "";
                }
                else
                {
                    toolStripStatusLabel1.Text = "Drawing Layer " + layerName + "...";
                }
            }
        }

        private void mapView1_AfterRefreshMap()
        {
            DrawingLayer("");
        }

        private delegate void CursorMoveCallback(int x, int y, double X, double Y);
        private void mapView1_CursorMove(int x, int y, double X, double Y)
        {
            if (statusStrip1.InvokeRequired)
            {
                CursorMoveCallback d = new CursorMoveCallback(mapView1_CursorMove);
                statusStrip1.Invoke(d, new object[] { x, y, X, Y });
            }
            else
            {
                toolStripStatusLabel2.Text = "X: " + Math.Round(X, 2);
                toolStripStatusLabel3.Text = "Y: " + Math.Round(Y, 2);
            }
        }
        #endregion
    }

    internal class ToolButton : ToolStripButton
    {
        private ITool _tool;

        public ToolButton(ITool tool)
            : base((Image)tool.Image)
        {
            _tool = tool;
        }

        public ITool Tool
        {
            get { return _tool; }
        }
    }


}
