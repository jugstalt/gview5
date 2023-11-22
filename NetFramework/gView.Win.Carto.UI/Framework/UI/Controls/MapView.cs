using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Snapping.Core;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using gView.Framework.system.UI;
using gView.Framework.UI.Dialogs;
using gView.Framework.UI.Events;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Framework.UI.Controls
{
    public enum NavigationType { Standard, Tablet }

    public class MapView : System.Windows.Forms.UserControl
    {
        public enum ResizeMode { SameScale = 0, SameExtent = 1 }
        private IContainer components;
        private Map _map;
        private MapRenderInstance _mapRendererInstance = null;
        private bool _mouseDown = false;
        private int _mx, _my, _ox, _oy, _sx, _sy, _minMx, _minMy, _maxMx, _maxMy, _wsx = -1, _wsy = -1;
        private double _diff_y, _sX, _sY, _wsX, _wsY;
        private Rectangle m_rubberband;
        private Envelope _limit = null, _wheelImageStartEnv = null;
        private ITool _actTool = null, _actMouseDownTool = null, _mapTool = null, _shiftTool = null;
        private NavigationType _navType = NavigationType.Standard;
        private CancelTracker _cancelTracker;
        private IMapDocument _mapDoc = null;
        private bool _canceled = false;
        public delegate void CursorMoveEvent(int x, int y, double X, double Y);
        public event CursorMoveEvent CursorMove = null;
        public delegate void StartRequestEvent();
        public event StartRequestEvent StartRequest = null;
        public delegate void AfterRefreshMapEvent();
        public event AfterRefreshMapEvent AfterRefreshMap = null;
        public delegate void BeforeRefreshMapEvent();
        public event BeforeRefreshMapEvent BeforeRefreshMap = null;
        public delegate void DrawingLayerEvent(string layerName);
        public event DrawingLayerEvent DrawingLayer;
        private IBitmap _iBitmap = null;
        private Bitmap _bitmapSnapShot = null;
        private object lockThis = new object();
        private System.Windows.Forms.Timer timerResize;
        private bool _mouseWheel = true, _uiLocked = false;
        private ContextMenuStrip _contextMenu = null;
        private System.Windows.Forms.Timer timerWheel;
        private Panel panelCopyright;
        private LinkLabel lnkCopyright;

        delegate void MakeMapViewRefreshCallback();
        delegate void NewBitmapCreatedCallbark(System.Drawing.Image image);

        private TaskScheduler _uiTaskSchedular;

        public MapView()
        {
            InitializeComponent();

            _uiTaskSchedular = TaskScheduler.FromCurrentSynchronizationContext();
            _cancelTracker = new CancelTracker();

            this.DoubleBuffered = true;

            this.MouseWheel += new MouseEventHandler(MapView_MouseWheel);
        }

        //public Task RunSyncronously(Action action)
        //{
        //    if (_uiTaskSchedular != null)
        //    {
        //        var task = new Task(action);
        //        task.RunSynchronously(_uiTaskSchedular);

        //        //await Task.Factory.StartNew(action,
        //        //    CancellationToken.None,
        //        //    TaskCreationOptions.None,
        //        //    _uiTaskSchedular);


        //    }

        //    return Task.CompletedTask;
        //}

        new public void Dispose()
        {
            if (_mapDoc != null && _mapDoc.Application is IMapApplication)
            {
                ((IMapApplication)_mapDoc.Application).AfterLoadMapDocument -= new AfterLoadMapDocumentEvent(this.AfterLoadMapDocument);
                _mapDoc = null;
            }
            timerResize.Enabled = false;
            base.Dispose();
        }

        private bool HasMap
        {
            get
            {
                if (_map != null)
                {
                    return true;
                }

                return false;
            }
        }

        public Envelope Limit
        {
            get
            {
                return _limit;
            }
            set
            {
                _limit = value;
                if (!HasMap)
                {
                    return;
                }
            }
        }

        public bool AllowMouseWheel
        {
            get
            {
                return _mouseWheel;
            }
            set
            {
                _mouseWheel = value;
            }
        }

        public new ContextMenuStrip ContextMenu
        {
            get { return _contextMenu; }
            set { _contextMenu = value; }
        }

        [Browsable(false)]
        public gView.Framework.Carto.IMap Map
        {
            get
            {
                return _map;
            }

            set
            {
                _map = (Map)value;
                if (_map != null)
                {
                    _map.Display.ImageWidth = this.Width;
                    _map.Display.ImageHeight = this.Height;
                    this.BackColor = _map.Display.BackgroundColor.ToGdiColor();
                    _map.NewBitmap += NewBitmapCreated;
                    _map.DoRefreshMapView += MakeMapViewRefresh;
                    //_map.DrawingLayer+=new gView.Framework.Carto.DrawingLayerEvent(OnDrawingLayer);

                    _map.OnUserInterface -= _map_OnUserInterface;
                    _map.OnUserInterface += _map_OnUserInterface;
                }
            }
        }

        private delegate void OnUserInterfaceCallback(IMap sender, bool lockUI);
        private void _map_OnUserInterface(IMap sender, bool lockUI)
        {
            if (this.InvokeRequired)
            {
                var d = new OnUserInterfaceCallback(_map_OnUserInterface);
                this.Invoke(d, new object[] { sender, lockUI });
            }
            else
            {
                //_uiLocked = lockUI;
                try
                {
                    if (lockUI)
                    {
                        this.Cursor = Cursors.WaitCursor;
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
                catch { }
            }
        }

        public NavigationType MouseNavigationType
        {
            get { return _navType; }
            set { _navType = value; }
        }

        [Browsable(false)]
        public gView.Framework.UI.IMapDocument MapDocument
        {
            set
            {
                if (value == null)
                {
                    return;
                }

                if (value.Application is IMapApplication)
                {
                    ((IMapApplication)value.Application).AfterLoadMapDocument += new AfterLoadMapDocumentEvent(this.AfterLoadMapDocument);
                }

                _mapDoc = value;
            }
        }

        private ResizeMode _resizeMode = ResizeMode.SameExtent;
        public ResizeMode resizeMode
        {
            get { return _resizeMode; }
            set { _resizeMode = value; }
        }
        async private void AfterLoadMapDocument(IMapDocument mapDocument)
        {
            if (mapDocument == null)
            {
                return;
            }

            if (!(mapDocument.FocusMap is Map))
            {
                return;
            }

            if (mapDocument.FocusMap != this.Map)
            {
                return;
            }

            if (mapDocument.FocusMap.Display == null)
            {
                return;
            }

            gView.Framework.Geometry.IEnvelope env = mapDocument.FocusMap.Display.Envelope;
            Map.Display.ZoomTo(env);

            if (this.Visible)
            {
                if (mapDocument.Application is IMapApplication)
                {
                    await ((IMapApplication)mapDocument.Application).RefreshActiveMap(DrawPhase.All);
                }
                else
                {
                    RefreshMap(DrawPhase.All);
                }
            }
        }

        public void OnDrawingLayer(string layerName)
        {
            DrawingLayer?.Invoke(layerName);
        }

        public void NewBitmapCreated(IBitmap bitmap)
        {
            _iBitmap = bitmap;
            CreateBitmapSnapshot();
        }

        public void MakeMapViewRefresh()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    MakeMapViewRefreshCallback d = new MakeMapViewRefreshCallback(MakeMapViewRefresh);
                    this.Invoke(d);
                }
                else
                {
                    if (_iBitmap != null && _handle != IntPtr.Zero)
                    {
                        try
                        {
                            if (CreateBitmapSnapshot())
                            {
                                using (var gr = System.Drawing.Graphics.FromHwnd(_handle))
                                {
                                    gr.DrawImage(_bitmapSnapShot, new PointF(0, 0));
                                }
                                _iSnapX = _iSnapY = int.MinValue;
                            }
                        }
                        catch (Exception)
                        {
                            //MessageBox.Show("MakeMapViewRefresh: " + ex.Message);
                        }
                    }
                }
            }
            catch { }
        }

        private delegate void RenderOverlayImageCallback(IBitmap bitmap, bool clearOld);
        public void RenderOverlayImage(IBitmap bitmap, bool clearOld)
        {
            if (this.InvokeRequired)
            {
                RenderOverlayImageCallback d = new RenderOverlayImageCallback(RenderOverlayImage);
                this.Invoke(d, new object[] { bitmap, clearOld });
            }
            else
            {
                if (bitmap == null)
                {
                    MakeMapViewRefresh();
                    return;
                }
                if (clearOld)
                {
                    try
                    {
                        using (var targetBitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        using (var gr = System.Drawing.Graphics.FromImage(targetBitmap))
                        {
                            if (_iBitmap != null)
                            {
                                using (var sourceBitmap = _iBitmap.CloneToGdiBitmap())
                                {
                                    gr.DrawImage(sourceBitmap, new PointF(0, 0));
                                }
                            }

                            if (bitmap != null)
                            {
                                using (var sourceBitmap = bitmap.CloneToGdiBitmap())
                                {
                                    gr.DrawImage(sourceBitmap, new PointF(0, 0));
                                }
                            }

                            using (var controlGraphics = System.Drawing.Graphics.FromHwnd(this.Handle))
                            {
                                controlGraphics.DrawImage(targetBitmap, new PointF(0, 0));
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                else
                {
                    if (bitmap != null)
                    {
                        try
                        {
                            using (var controlGraphics = System.Drawing.Graphics.FromHwnd(this.Handle))
                            using (var sourceBitmap = bitmap.CloneToGdiBitmap())
                            {
                                controlGraphics.DrawImage(sourceBitmap, new PointF(0, 0));
                            }
                        }
                        catch
                        {
                        }

                    }
                }
            }
        }

        [Browsable(false)]
        public gView.Framework.UI.ITool Tool
        {
            get
            {
                return _actTool;
            }
            set
            {
                _actTool = _mapTool = value;
            }
        }

        [Browsable(false)]
        public gView.Framework.UI.ITool ControlKeyTool
        {
            get
            {
                return _shiftTool;
            }
            set
            {
                _shiftTool = value;
            }
        }

        private DrawPhase _phase;

        private bool _cancelling = false;

        public void CancelDrawing(DrawPhase phase = DrawPhase.All)
        {
            _canceled = _cancelling = true;
            try
            {
                if (!HasMap)
                {
                    return;
                }

                // Create a Snapshot for mouse events (panning/zooming)
                CreateBitmapSnapshot();
                // Release events & cancel
                ReleaseCurrentMapRendererInstance();
                _cancelTracker.Cancel();
            }
            finally
            {
                _cancelling = false;
            }
        }

        private Thread _refreshMapThread = null;
        public void RefreshMap(DrawPhase phase)
        {
            if (!HasMap)
            {
                return;
            }

            if (DesignMode)
            {
                return;
            }

            try
            {
                if (_cancelTracker.Continue)
                {
                    CancelDrawing(phase);
                }

                BeforeRefreshMap?.Invoke();

                _map.Display.ImageWidth = this.Width;
                _map.Display.ImageHeight = this.Height;

                _phase = phase;
                _canceled = false;

                StartRequest?.Invoke();

                _cancelTracker = new CancelTracker();  // a new CancelTracker for every request!?
                _cancelTracker.Reset();
                _refreshMapThread = new Thread(async () =>
                  {
                      BeforeRefreshMap?.Invoke();

                      try
                      {
                          var myCancelTracker = _cancelTracker;
                          var myMapRendererInstance = _mapRendererInstance = CreateMapRendererInstance();

                          await myMapRendererInstance.RefreshMap(_phase, _cancelTracker);
                          MakeMapViewRefresh();

                          AfterRefreshMap?.Invoke();

                          if (myCancelTracker.Continue)
                          {
                              CreateBitmapSnapshot();
                              _iBitmap = null;
                              myMapRendererInstance.Dispose();
                          }
                          else
                          {
                              if (_iBitmap != null)
                              {
                                  lock (_iBitmap)
                                  {
                                      myMapRendererInstance.Dispose();
                                  }
                              }
                          }
                      }
                      catch (Exception ex)
                      {
                          MessageBox.Show("Exception: " + ex.Message + "\n" + ex.StackTrace);
                      }
                      finally
                      {
                          ReleaseCurrentMapRendererInstance();
                      }
                  });
                _refreshMapThread.Start();
            }
            catch (Exception)
            {
                return;
            }
            return;
        }

        private delegate void RefreshCopyrightVisibilityDelegate();
        public void RefreshCopyrightVisibility()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new RefreshCopyrightVisibilityDelegate(RefreshCopyrightVisibility));
            }
            else
            {
                panelCopyright.Visible = _map.HasDataCopyright;
            }
        }

        /// <summary> 
        /// Die verwendeten Ressourcen bereinigen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
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
            catch { }
        }

        #region Vom Komponenten-Designer generierter Code
        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timerResize = new System.Windows.Forms.Timer(this.components);
            this.timerWheel = new System.Windows.Forms.Timer(this.components);
            this.panelCopyright = new System.Windows.Forms.Panel();
            this.lnkCopyright = new System.Windows.Forms.LinkLabel();
            this.panelCopyright.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerResize
            // 
            this.timerResize.Enabled = true;
            this.timerResize.Interval = 1000;
            this.timerResize.Tick += new System.EventHandler(this.timerResize_Tick);
            // 
            // timerWheel
            // 
            this.timerWheel.Interval = 1000;
            this.timerWheel.Tick += new System.EventHandler(this.timerWheel_Tick);
            // 
            // panelCopyright
            // 
            this.panelCopyright.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.panelCopyright.BackColor = System.Drawing.Color.White;
            this.panelCopyright.Controls.Add(this.lnkCopyright);
            this.panelCopyright.Location = new System.Drawing.Point(185, 270);
            this.panelCopyright.Name = "panelCopyright";
            this.panelCopyright.Size = new System.Drawing.Size(137, 19);
            this.panelCopyright.TabIndex = 0;
            // 
            // lnkCopyright
            // 
            this.lnkCopyright.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.lnkCopyright.AutoSize = true;
            this.lnkCopyright.Location = new System.Drawing.Point(3, 3);
            this.lnkCopyright.Name = "lnkCopyright";
            this.lnkCopyright.Size = new System.Drawing.Size(176, 20);
            this.lnkCopyright.TabIndex = 0;
            this.lnkCopyright.TabStop = true;
            this.lnkCopyright.Text = "© Copyright Information";
            this.lnkCopyright.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCopyright_LinkClicked);
            // 
            // MapView
            // 
            this.Controls.Add(this.panelCopyright);
            this.Name = "MapView";
            this.Size = new System.Drawing.Size(322, 289);
            this.Load += new System.EventHandler(this.MapView_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MapView_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MapView_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MapView_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MapView_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MapView_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.this_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.this_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.this_MouseUp);
            this.Resize += new System.EventHandler(this.MapView_Resize);
            this.panelCopyright.ResumeLayout(false);
            this.panelCopyright.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private void Image2World(ref double x, ref double y)
        {
            if (_map == null || _map.Display == null)
            {
                return;
            }

            _map.Display.Image2World(ref x, ref y);
        }
        private IEnvelope Image2World(IEnvelope envelope)
        {
            double minx = envelope.minx, miny = envelope.miny;
            double maxx = envelope.maxx, maxy = envelope.maxy;

            Image2World(ref minx, ref miny);
            Image2World(ref maxx, ref maxy);

            return new Envelope(
                Math.Min(minx, maxx), Math.Min(miny, maxy),
                Math.Max(minx, maxx), Math.Max(miny, maxy)
                );
        }
        private void World2Image(ref double x, ref double y)
        {
            if (_map == null || _map.Display == null)
            {
                return;
            }

            _map.Display.World2Image(ref x, ref y);
        }

        private IEnvelope World2Image(IEnvelope envelope)
        {
            double minx = envelope.minx, miny = envelope.miny;
            double maxx = envelope.maxx, maxy = envelope.maxy;
            double rot = 0.0;

            bool useDisplayTrans = _map.Display.DisplayTransformation.UseTransformation;
            if (useDisplayTrans)
            {
                rot = _map.Display.DisplayTransformation.DisplayRotation;
                _map.Display.DisplayTransformation.DisplayRotation = 0.0;
            }
            World2Image(ref minx, ref miny);
            World2Image(ref maxx, ref maxy);
            if (useDisplayTrans)
            {
                _map.Display.DisplayTransformation.DisplayRotation = rot;
            }

            return new Envelope(
                Math.Min(minx, maxx), Math.Min(miny, maxy),
                Math.Max(minx, maxx), Math.Max(miny, maxy)
                );
        }

        private MouseButtons _button = MouseButtons.None;

        private void DrawReversibleSnapPoint(int x, int y)
        {
            if (x == int.MinValue || y == int.MinValue)
            {
                return;
            }

            x -= this.Left;
            y -= this.Top;
            ROP2 rop2 = new ROP2();

            rop2.Color = Color.Red;
            rop2.Width = 2;

            System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(this.Handle);
            rop2.DrawXORRectangle(gr, x - 1, y - 1, x + 5, y + 5);
        }

        #region MouseEvents

        void MapView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_uiLocked)
            {
                return;
            }

            timerWheel.Stop();
            if (this.Width == 0 || this.Height == 0)
            {
                return;
            }

            if (_actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseWheel(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }
            if (!_mouseWheel)
            {
                return;
            }

            if (_map == null || _map.Display == null || e.Delta == 0)
            {
                return;
            }

            CancelDrawing();

            bool first = (_wheelImageStartEnv == null);

            if (first)
            {
                _wheelImageStartEnv = World2Image(new Envelope(_map.Display.Envelope)) as Envelope;
                _wsx = e.X;
                _wsy = e.Y;
            }

            double x = e.X, y = e.Y;
            Image2World(ref x, ref y);
            if (first)
            {
                _wsX = x;
                _wsY = y;
            }

            double diff_w = (e.Delta < 0 ? -10 : 10);

            _wheelImageStartEnv.Raise(new Geometry.Point(_wsx, _wsy), 100.0 + diff_w);

            //Envelope env = World2Image(_wheelWorldStartEnv) as Envelope;

            int rw = (int)(_wheelImageStartEnv.maxx - _wheelImageStartEnv.minx);
            int rh = (int)(_wheelImageStartEnv.maxy - _wheelImageStartEnv.miny);
            int dx = (int)(_wsx - ((double)_wsx / this.Width * rw));
            int dy = (int)(_wsy - ((double)_wsy / this.Height * rh));

            Rectangle rect = new Rectangle(dx, dy, rw, rh);

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(this.Handle))
            {
                using (SolidBrush brush = new SolidBrush((_map != null) ? _map.Display.BackgroundColor.ToGdiColor() : Color.White))
                {
                    gr.FillRectangle(brush, 0, 0, this.Width, this.Height);
                }

                if (_bitmapSnapShot != null)
                {
                    try
                    {
                        // ToDO: Snapshot...
                        gr.DrawImage(_bitmapSnapShot,
                            rect,
                            new Rectangle(0, 0, this.Width, this.Height),
                            System.Drawing.GraphicsUnit.Pixel);
                    }
                    catch { }
                }
            }

            Envelope extent = new Envelope(_map.Display.Envelope);
            extent.Raise(new Geometry.Point(_wsX, _wsY), 100.0 - diff_w);
            _map.Display.ZoomTo(extent);

            timerWheel.Enabled = true;
            timerWheel.Start();
            //if (_mapDoc != null && _mapDoc.Application is IMapApplication)
            //    ((IMapApplication)_mapDoc.Application).RefreshActiveMap(DrawPhase.All);
            //else
            //    RefreshMap(DrawPhase.All);
        }

        public void Wheel(double X, double Y, double diff_w)
        {
            if (_uiLocked) { return; }
            timerWheel.Stop();

            bool first = (_wheelImageStartEnv == null);

            if (first)
            {
                _wheelImageStartEnv = World2Image(new Envelope(_map.Display.Envelope)) as Envelope;
                double x = X, y = Y;
                _map.Display.World2Image(ref x, ref y);
                _wsx = (int)x;
                _wsy = (int)y;
            }

            if (first)
            {
                _wsX = X;
                _wsY = Y;
            }

            _wheelImageStartEnv.Raise(new Geometry.Point(_wsx, _wsy), 100.0 + diff_w);

            //Envelope env = World2Image(_wheelWorldStartEnv) as Envelope;

            int rw = (int)(_wheelImageStartEnv.maxx - _wheelImageStartEnv.minx);
            int rh = (int)(_wheelImageStartEnv.maxy - _wheelImageStartEnv.miny);
            int dx = (int)(_wsx - ((double)_wsx / this.Width * rw));
            int dy = (int)(_wsy - ((double)_wsy / this.Height * rh));

            Rectangle rect = new Rectangle(dx, dy, rw, rh);

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(this.Handle))
            {
                using (SolidBrush brush = new SolidBrush((_map != null) ? _map.Display.BackgroundColor.ToGdiColor() : Color.White))
                {
                    gr.FillRectangle(brush, 0, 0, this.Width, this.Height);
                }

                if (_bitmapSnapShot != null)
                {
                    try
                    {
                        gr.DrawImage(_bitmapSnapShot,
                            rect,
                            new Rectangle(0, 0, this.Width, this.Height),
                            System.Drawing.GraphicsUnit.Pixel);
                    }
                    catch { }
                }
            }

            Envelope extent = new Envelope(_map.Display.Envelope);
            extent.Raise(new Geometry.Point(_wsX, _wsY), 100.0 - diff_w);
            _map.Display.ZoomTo(extent);

            timerWheel.Enabled = true;
            timerWheel.Start();
        }

        private double _X = 0.0, _Y = 0.0;
        int _iSnapX = int.MinValue, _iSnapY = int.MinValue;
        private void this_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_uiLocked)
            {
                return;
            }

            if (e.Button == MouseButtons.Right &&
                (_actTool != null && _actTool.toolType != ToolType.smartnavigation))
            {
                if (_actTool is IToolContextMenu)
                {
                    ContextMenuStrip strip = ((IToolContextMenu)_actTool).ContextMenu;
                    if (strip != null && strip.Items.Count > 0)
                    {
                        if (_actTool is IToolMouseActions2)
                        {
                            ((IToolMouseActions2)_actTool).BeforeShowContext(_map.Display, e, new gView.Framework.Geometry.Point(_X, _Y));
                        }

                        strip.Show(this, new System.Drawing.Point(e.X, e.Y));
                    }
                    else if (_contextMenu != null)
                    {
                        if (_actTool is IToolMouseActions2)
                        {
                            ((IToolMouseActions2)_actTool).BeforeShowContext(_map.Display, e, new gView.Framework.Geometry.Point(_X, _Y));
                        }

                        _contextMenu.Show(this, new System.Drawing.Point(e.X, e.Y));
                    }
                    return;
                }
                else if (_contextMenu != null)
                {
                    if (_actTool is IToolMouseActions2)
                    {
                        ((IToolMouseActions2)_actTool).BeforeShowContext(_map.Display, e, new gView.Framework.Geometry.Point(_X, _Y));
                    }

                    _contextMenu.Show(this, new System.Drawing.Point(e.X, e.Y));

                    return;
                }
            }

            if (_actTool == null)
            {
                return;
            }

            switch (_actTool.toolType)
            {
                case ToolType.rubberband:
                    m_rubberband = new Rectangle(Control.MousePosition.X, Control.MousePosition.Y, 0, 0);
                    break;
                case ToolType.smartnavigation:
                    //CancelDrawing(DrawPhase.All);
                    CancelDrawing();
                    break;
                case ToolType.pan:
                    if (_navType == NavigationType.Standard)
                    {
                        CancelDrawing();
                    }

                    break;
            }

            _mouseDown = true;
            _button = e.Button;
            _actMouseDownTool = _actTool;

            if (_navType == NavigationType.Standard)
            {
                _sx = _ox = _minMx = _maxMx = _mx = Control.MousePosition.X;
                _sy = _oy = _minMy = _maxMy = _my = Control.MousePosition.Y;
            }
            else if (_navType == NavigationType.Tablet)
            {
                _sx = _ox = _minMx = _maxMx = _mx = e.X;
                _sx = _oy = _minMy = _maxMy = _my = e.Y;
            }

            _sX = _sx = e.X;
            _sY = _sy = e.Y;
            Image2World(ref _sX, ref _sY);
            //this.CancelDrawing();
            _cancelTracker.Cancel();

            if (!_cancelling && _actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseDown(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }
        }

        private void this_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_uiLocked)
            {
                return;
            }

            if (_actTool == null || _map == null || _map.Display == null)
            {
                return;
            }

            if (!HasMap)
            {
                return;
            }

            _X = e.X; _Y = e.Y;
            _map.Display.Image2World(ref _X, ref _Y);

            #region Snapping

            DrawReversibleSnapPoint(_iSnapX, _iSnapY);
            if (_actTool is ISnapTool)
            {
                ((ISnapTool)_actTool).Snap(ref _X, ref _Y);
                if (((ISnapTool)_actTool).ShowSnapMarker)
                {
                    double snapX = _X, snapY = _Y;
                    World2Image(ref snapX, ref snapY);

                    _iSnapX = (int)snapX /*+ Control.MousePosition.X - e.X*/;
                    _iSnapY = (int)snapY /*+ Control.MousePosition.Y - e.Y*/;
                    DrawReversibleSnapPoint(_iSnapX, _iSnapY);
                }
                else
                {
                    _iSnapX = _iSnapY = int.MinValue;
                }
            }
            else
            {
                _iSnapX = _iSnapY = int.MinValue;
            }

            #endregion

            if (CursorMove != null)
            {
                CursorMove(e.X, this.Height - e.Y, _X, _Y);
            }

            if (_actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseMove(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }

            if (!_mouseDown)
            {
                return;
            }

            switch (_actTool.toolType)
            {
                case ToolType.rubberband:
                    if (_navType == NavigationType.Standard)
                    {
                        ControlPaint.DrawReversibleFrame(
                            m_rubberband,
                            Color.Black,
                            FrameStyle.Thick);

                        m_rubberband.X = Math.Min(Control.MousePosition.X, _mx);
                        m_rubberband.Y = Math.Min(Control.MousePosition.Y, _my);
                        m_rubberband.Width = Math.Abs(Control.MousePosition.X - _mx);
                        m_rubberband.Height = Math.Abs(Control.MousePosition.Y - _my);

                        ControlPaint.DrawReversibleFrame(
                            m_rubberband,
                            Color.Black,
                            FrameStyle.Thick);
                    }
                    else if (_navType == NavigationType.Tablet)
                    {
                        _minMx = Math.Min(e.X, _minMx);
                        _minMy = Math.Min(e.Y, _minMy);
                        _maxMx = Math.Max(e.X, _maxMx);
                        _maxMy = Math.Max(e.Y, _maxMy);

                        System.Drawing.Graphics gr2 = System.Drawing.Graphics.FromHwnd(this.Handle);
                        Pen pen = new Pen(Color.Red, 5);
                        gr2.DrawLine(pen,
                            new System.Drawing.Point(e.X, e.Y),
                            new System.Drawing.Point(_ox, _oy));
                        pen.Color = Color.Yellow;
                        pen.Width -= 2;
                        gr2.DrawLine(pen,
                            new System.Drawing.Point(e.X, e.Y),
                            new System.Drawing.Point(_ox, _oy));
                        pen.Dispose(); pen = null;
                        gr2.Dispose(); gr2 = null;
                    }
                    break;
                case ToolType.smartnavigation:
                    if (_button == MouseButtons.Right)
                    {
                        double diff_y = 0;
                        if (_navType == NavigationType.Standard)
                        {
                            diff_y = (MousePosition.Y - _my);
                        }
                        else if (_navType == NavigationType.Tablet)
                        {
                            diff_y = (e.Y - _my);
                        }

                        _diff_y = 100.0 + ((diff_y > 0) ? Math.Pow(diff_y, 1.5) : diff_y);
                        if (_diff_y < 5)
                        {
                            _diff_y = 5;
                        }

                        Envelope env = new Envelope(_map.Display.Envelope);
                        env.Raise(_diff_y);
                        env.TranslateTo(_sX, _sY);
                        env = World2Image(env) as Envelope;

                        int dx = _sx - this.Width / 2;
                        int dy = _sy - this.Height / 2;

                        Rectangle rect = new Rectangle(
                            (int)env.minx, (int)env.miny,
                            (int)(env.maxx - env.minx),
                            (int)(env.maxy - env.miny));

                        using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(this.Handle))
                        {
                            using (SolidBrush brush = new SolidBrush((_map != null) ? _map.Display.BackgroundColor.ToGdiColor() : Color.White))
                            {
                                gr.FillRectangle(brush, 0, 0, this.Width, this.Height);
                            }

                            if (_bitmapSnapShot != null)
                            {
                                try
                                {
                                    gr.DrawImage(_bitmapSnapShot,
                                        rect,
                                        new Rectangle(dx, dy, this.Width, this.Height),
                                        System.Drawing.GraphicsUnit.Pixel);
                                }
                                catch { }
                            }
                        }
                    }
                    else if (_button == MouseButtons.Left)
                    {
                        Pan_MouseMove(e);
                    }
                    break;
                case ToolType.pan:
                    Pan_MouseMove(e);
                    break;
            }

            if (_navType == NavigationType.Standard)
            {
                _ox = Control.MousePosition.X;
                _oy = Control.MousePosition.Y;
            }
            else if (_navType == NavigationType.Tablet)
            {
                _ox = e.X;
                _oy = e.Y;
            }
        }

        private void Pan_MouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            if (_bitmapSnapShot == null || _uiLocked)
            {
                return;
            }

            if (_navType == NavigationType.Standard)
            {
                System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(this.Handle);

                int x1 = Control.MousePosition.X - _mx, y1 = Control.MousePosition.Y - _my;

                SolidBrush brush = new SolidBrush(this.BackColor);
                if (x1 < 0)
                {
                    gr.FillRectangle(brush, x1 + this.Width, 0, this.Width - x1 - this.Width, this.Height);
                }
                else
                {
                    gr.FillRectangle(brush, 0, 0, x1, this.Height);
                }

                if (y1 < 0)
                {
                    gr.FillRectangle(brush, 0, y1 + this.Height, this.Width, this.Height - y1 - this.Height);
                }
                else
                {
                    gr.FillRectangle(brush, 0, 0, this.Width, y1);
                }

                try
                {
                    gr.DrawImage(_bitmapSnapShot,
                        new Rectangle(
                        Control.MousePosition.X - _mx,
                        Control.MousePosition.Y - _my,
                        this.Width,
                        this.Height),
                        0, 0, this.Width, this.Height,
                        System.Drawing.GraphicsUnit.Pixel);
                }
                catch { }
                brush.Dispose(); brush = null;
                gr.Dispose(); gr = null;
            }
            else if (_navType == NavigationType.Tablet)
            {
                _minMx = Math.Min(e.X, _minMx);
                _minMy = Math.Min(e.Y, _minMy);
                _maxMx = Math.Max(e.X, _maxMx);
                _maxMy = Math.Max(e.Y, _maxMy);

                System.Drawing.Graphics gr2 = System.Drawing.Graphics.FromHwnd(this.Handle);
                Pen pen = new Pen(Color.Red, 5);
                gr2.DrawLine(pen,
                    new System.Drawing.Point(e.X, e.Y),
                    new System.Drawing.Point(_ox, _oy));
                pen.Color = Color.Yellow;
                pen.Width -= 2;
                gr2.DrawLine(pen,
                    new System.Drawing.Point(e.X, e.Y),
                    new System.Drawing.Point(_ox, _oy));
                pen.Dispose(); pen = null;
                gr2.Dispose(); gr2 = null;
            }
        }



        async private void this_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!_mouseDown)
            {
                return;
            }

            _mouseDown = false;
            if (_actTool == null ||
                _actTool != _actMouseDownTool)      // Aktives Tool kann während DoubleClick,... geändert geworden sein;
            // dann kein MouseUp senden...) 
            {
                return;
            }

            IEnvelope bounds = _map.Display.Envelope;
            double minx = bounds.minx,
                miny = bounds.miny,
                maxx = bounds.maxx,
                maxy = bounds.maxy,
                x1 = 0.0, y1 = 0.0, x2 = 0.0, y2 = 0.0;

            bool refresh = false;
            DrawPhase phase = DrawPhase.All;

            Cursor currentCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                switch (_actTool.toolType)
                {
                    case ToolType.click:
                        MapEventClick ev0 = new MapEventClick(
                            _map,
                            _X,
                            _Y);
                        await _actTool.OnEvent(ev0);
                        refresh = ev0.refreshMap;
                        phase = ev0.drawPhase;
                        break;
                    case ToolType.rubberband:
                        System.Drawing.Point p1 = new System.Drawing.Point(0, 0), p2 = new System.Drawing.Point(0, 0);
                        if (_navType == NavigationType.Standard)
                        {
                            ControlPaint.DrawReversibleFrame(
                                m_rubberband,
                                Color.Black,
                                FrameStyle.Thick);

                            p1 = this.PointToClient(new System.Drawing.Point(m_rubberband.X, m_rubberband.Y));
                            p2 = this.PointToClient(new System.Drawing.Point(m_rubberband.X + m_rubberband.Width,
                                m_rubberband.Y + m_rubberband.Height));
                        }
                        else if (_navType == NavigationType.Tablet)
                        {
                            p1 = new System.Drawing.Point(_minMx, _minMy);
                            p2 = new System.Drawing.Point(_maxMx, _maxMy);
                        }

                        //x1 = minx + Math.Abs(maxx - minx) * (p1.X) / this.Width;
                        //x2 = minx + Math.Abs(maxx - minx) * (p2.X) / this.Width;
                        //y1 = miny + Math.Abs(maxy - miny) * (this.Height - p1.Y) / this.Height;
                        //y2 = miny + Math.Abs(maxy - miny) * (this.Height - p2.Y) / this.Height;
                        x1 = p1.X; y1 = p1.Y; _map.Display.Image2World(ref x1, ref y1);
                        x2 = p2.X; y2 = p2.Y; _map.Display.Image2World(ref x2, ref y2);

                        MapEventRubberband ev1 = new MapEventRubberband(
                            _map,
                            Math.Min(x1, x2),
                            Math.Min(y1, y2),
                            Math.Max(x2, x1),
                            Math.Max(y2, y1));

                        await _actTool.OnEvent(ev1);
                        refresh = ev1.refreshMap;
                        phase = ev1.drawPhase;
                        break;
                    case ToolType.pan:
                    case ToolType.smartnavigation:
                        if (_map != null)
                        {
                            if (_button == MouseButtons.Right)
                            {
                                Envelope extent = new Envelope(_map.Display.Envelope);
                                extent.Raise(new gView.Framework.Geometry.Point(_sX, _sY), 100.0 / (_diff_y / 100));

                                MapEventRubberband ev3 = new MapEventRubberband(
                                    _map,
                                    extent.minx, extent.miny, extent.maxx, extent.maxy);

                                await _actTool.OnEvent(ev3);
                                refresh = ev3.refreshMap;
                                phase = ev3.drawPhase;
                            }
                            else if (_button == MouseButtons.Left)
                            {
                                if (_navType == NavigationType.Standard)
                                {
                                    //x1 = minx + Math.Abs(maxx - minx) * (_mx) / this.Width;
                                    //x2 = minx + Math.Abs(maxx - minx) * (Control.MousePosition.X) / this.Width;
                                    //y1 = miny + Math.Abs(maxy - miny) * (this.Height - _my) / this.Height;
                                    //y2 = miny + Math.Abs(maxy - miny) * (this.Height - Control.MousePosition.Y) / this.Height;
                                    x1 = _mx; y1 = _my; _map.Display.Image2World(ref x1, ref y1);
                                    x2 = Control.MousePosition.X; y2 = Control.MousePosition.Y; _map.Display.Image2World(ref x2, ref y2);
                                }
                                else if (_navType == NavigationType.Tablet)
                                {
                                    //x1 = minx + Math.Abs(maxx - minx) * (_mx) / this.Width;
                                    //x2 = minx + Math.Abs(maxx - minx) * (e.X) / this.Width;
                                    //y1 = miny + Math.Abs(maxy - miny) * (this.Height - _my) / this.Height;
                                    //y2 = miny + Math.Abs(maxy - miny) * (this.Height - e.Y) / this.Height;
                                    x1 = _mx; y1 = _my; _map.Display.Image2World(ref x1, ref y1);
                                    x2 = e.X; y2 = e.Y; _map.Display.Image2World(ref x2, ref y2);
                                }
                                MapEventPan ev2 = new MapEventPan(
                                    _map,
                                    x1 - x2, y1 - y2);

                                await _actTool.OnEvent(ev2);
                                refresh = ev2.refreshMap;
                                phase = ev2.drawPhase;
                            }
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex is UIException)
                {
                    UIExceptionBox.Show((UIException)ex);
                }
            }
            this.Cursor = currentCursor;

            if (refresh)
            {
                if (_mapDoc != null && _mapDoc.Application is IMapApplication)
                {
                    // Avoid Deadlocking -> run this inside of Task  (or Thread?);
                    var refreshTask = Task.Run(async () =>
                    {
                        await ((IMapApplication)_mapDoc.Application).RefreshActiveMap(phase);
                    });
                }
                else
                {
                    RefreshMap(phase);
                }
            }
            else
            {
                _cancelTracker.Reset();
            }

            _button = MouseButtons.None;

            if (_actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseUp(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }
        }

        private void MapView_MouseClick(object sender, MouseEventArgs e)
        {
            if (_actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseClick(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }
        }

        private void MapView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_actTool is IToolMouseActions)
            {
                ((IToolMouseActions)_actTool).MouseDoubleClick(_map != null ? _map.Display : null, e, new gView.Framework.Geometry.Point(_X, _Y));
            }
        }
        private void MapView_Resize(object sender, EventArgs e)
        {

        }

        #endregion

        private IGeometry _lastReversibleGeometry = null;
        public void DrawReversibleGeometry(IDisplay display, IGeometry geometry, Color color)
        {
            DrawReversibleGeometry(display, geometry, color, true);
        }
        private void DrawReversibleGeometry(IDisplay display, IGeometry geometry, Color color, bool removeold)
        {
            if (removeold && _lastReversibleGeometry != null)
            {
                DrawReversibleGeometry(display, _lastReversibleGeometry, color, false);
            }

            lock (lockThis)
            {
                if (geometry == null || display == null)
                {
                    return;
                }

                List<IPath> paths = SpatialAlgorithms.Algorithm.GeometryPaths(geometry);
                foreach (IPath path in paths)
                {
                    for (int i = 1; i < path.PointCount; i++)
                    {
                        IPoint p1 = path[i - 1];
                        IPoint p2 = path[i];

                        double x1 = p1.X, y1 = p1.Y;
                        double x2 = p2.X, y2 = p2.Y;

                        display.World2Image(ref x1, ref y1);
                        display.World2Image(ref x2, ref y2);

                        ControlPaint.DrawReversibleLine(
                            new System.Drawing.Point((int)x1 + this.Left, (int)y1 + this.Top),
                            new System.Drawing.Point((int)x2 + this.Left, (int)y2 + this.Top), color);
                    }
                }
                _lastReversibleGeometry = geometry;
            }
        }

        private delegate void PaintCallback(object sender, PaintEventArgs e);

        //private void MapView_Paint(object sender, PaintEventArgs e)
        //{
        //    MakeMapViewRefresh();
        //}
        protected override void OnPaint(PaintEventArgs e)
        {
            //MakeMapViewRefresh();
            //base.OnPaint(e);
        }

        #region Key
        private void MapView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                if (_shiftTool != null)
                {
                    _actTool = _shiftTool;
                }
            }
            if (_actTool is IToolKeyActions)
            {
                ((IToolKeyActions)_actTool).KeyDown(_map != null ? _map.Display : null, e);
            }
        }

        private void MapView_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_actTool is IToolKeyActions)
            {
                ((IToolKeyActions)_actTool).KeyPress(_map != null ? _map.Display : null, e);
            }
        }

        private void MapView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                _actTool = _mapTool;
            }
            if (_actTool is IToolKeyActions)
            {
                ((IToolKeyActions)_actTool).KeyUp(_map != null ? _map.Display : null, e);
            }
        }
        #endregion

        #region Resize

        private IntPtr _handle = IntPtr.Zero;

        private void MapView_Load(object sender, EventArgs e)
        {
            Form parentForm = GetParentForm(this);
            if (parentForm != null)
            {
                parentForm.Resize += new EventHandler(MapView_SizeChanged);
                parentForm.ResizeEnd += new EventHandler(MapView_ResizeEnd);
                parentForm.ResizeBegin += new EventHandler(MapView_ResizeBegin);
            }

            _handle = this.Handle;
        }

        private Form GetParentForm(Control control)
        {
            if (control is Form)
            {
                return control as Form;
            }

            if (control == null || control.Parent == null)
            {
                return null;
            }

            if (!(control.Parent is Form))
            {
                return GetParentForm(control.Parent);
            }
            else
            {
                return control.Parent as Form;
            }
        }

        void MapView_ResizeBegin(object sender, EventArgs e)
        {
            _inSizing = true;
        }

        private void MapView_SizeChanged(object sender, System.EventArgs e)
        {
            //_inSizing = true;
        }

        void MapView_ResizeEnd(object sender, EventArgs e)
        {
            this._inSizing = false;
        }

        private bool _inSizing = false;
        async private void timerResize_Tick(object sender, EventArgs e)
        {
            if (_inSizing)
            {
                return;
            }

            if (_map == null || _map.Display == null)
            {
                return;
            }

            if (this.Width == _map.Display.ImageWidth &&
                this.Height == _map.Display.ImageHeight)
            {
                // Is eh gleich
                return;
            }

            _map.Display.ImageWidth = this.Width;
            _map.Display.ImageHeight = this.Height;

            if (_resizeMode == ResizeMode.SameScale)
            {
                _map.Display.MapScale = _map.Display.MapScale;
            }
            else
            {
                _map.Display.ZoomTo(_map.Display.Envelope);
            }

            CancelDrawing();

            if (_mapDoc != null && _mapDoc.Application is IMapApplication)
            {
                await ((IMapApplication)_mapDoc.Application).RefreshActiveMap(DrawPhase.All);
            }
            else
            {
                RefreshMap(DrawPhase.All);
            }
        }
        #endregion

        async private void timerWheel_Tick(object sender, EventArgs e)
        {
            timerWheel.Stop();

            _wheelImageStartEnv = null;

            if (_mapDoc != null && _mapDoc.Application is IMapApplication)
            {
                await ((IMapApplication)_mapDoc.Application).RefreshActiveMap(DrawPhase.All);
            }
            else
            {
                RefreshMap(DrawPhase.All);
            }
        }

        private void lnkCopyright_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            (new FormCopyrightInformation(_map.DataCopyrightText)).ShowDialog();
        }

        #region Helper

        private MapRenderInstance CreateMapRendererInstance()
        {
            var mapRendererInstance = MapRenderInstance.CreateAsync(_map).Result;

            mapRendererInstance.NewBitmap += NewBitmapCreated;
            mapRendererInstance.DoRefreshMapView += MakeMapViewRefresh;

            return mapRendererInstance;
        }

        private void ReleaseCurrentMapRendererInstance()
        {
            if (_mapRendererInstance != null)
            {
                _mapRendererInstance.NewBitmap -= NewBitmapCreated;
                _mapRendererInstance.DoRefreshMapView -= MakeMapViewRefresh;

                _mapRendererInstance = null;
            }
        }

        private static object lockCreateBitmapSnapshot = new object();
        private bool CreateBitmapSnapshot()
        {
            lock (lockCreateBitmapSnapshot)
            {
                if (_iBitmap != null)
                {
                    lock (_iBitmap)
                    {
                        try
                        {
                            var oldBitmapSnapShot = _bitmapSnapShot;
                            _bitmapSnapShot = _iBitmap.CloneToGdiBitmap();

                            ReleaseBitmapSnapshot(oldBitmapSnapShot);
                        }
                        catch { }
                    }
                }

                return _bitmapSnapShot != null;
            }
        }

        private static object lockReleaseSnapshot = new object();
        private void ReleaseBitmapSnapshot(Bitmap snapShot)
        {
            lock (lockReleaseSnapshot)
            {
                if (snapShot != null)
                {
                    snapShot.Dispose();
                }
            }
        }

        #endregion
    }

    internal class DisplayOperations
    {
        public static System.Drawing.Drawing2D.GraphicsPath Geometry2GraphicsPath(IDisplay display, IGeometry geometry)
        {
            try
            {
                if (geometry is IPolygon)
                {
                    return ConvertPolygon(display, (IPolygon)geometry);
                }
                else if (geometry is IPolyline)
                {
                    return ConvertPolyline(display, (IPolyline)geometry);
                }
            }
            catch
            {
            }
            return null;
        }

        private static System.Drawing.Drawing2D.GraphicsPath ConvertPolygon(IDisplay display, IPolygon polygon)
        {
            GraphicsPath gp = new GraphicsPath();
            double o_x = -1e10, o_y = -1e10;

            for (int r = 0; r < polygon.RingCount; r++)
            {
                bool first = true;
                int count = 0;
                IRing ring = polygon[r];
                int pCount = ring.PointCount;
                gp.StartFigure();
                for (int p = 0; p < pCount; p++)
                {
                    IPoint point = ring[p];
                    double x = point.X, y = point.Y;
                    display.World2Image(ref x, ref y);

                    if (!((int)o_x == (int)x &&
                        (int)o_y == (int)y))
                    {
                        if (!first)
                        {
                            gp.AddLine((int)o_x, (int)o_y, (int)x, (int)y);
                            count++;
                        }
                        else
                        {
                            first = false;
                        }
                    }
                    o_x = x;
                    o_y = y;
                }
                if (count > 0)
                {
                    gp.CloseFigure();
                }
            }

            return gp;
        }

        private static System.Drawing.Drawing2D.GraphicsPath ConvertPolyline(IDisplay display, IPolyline polyline)
        {
            GraphicsPath gp = new GraphicsPath();
            double o_x = -1e10, o_y = -1e10;

            for (int r = 0; r < polyline.PathCount; r++)
            {
                bool first = true;
                int count = 0;
                IPath path = polyline[r];
                int pCount = path.PointCount;
                gp.StartFigure();
                for (int p = 0; p < pCount; p++)
                {
                    IPoint point = path[p];
                    double x = point.X, y = point.Y;
                    display.World2Image(ref x, ref y);

                    if (!((int)o_x == (int)x &&
                        (int)o_y == (int)y))
                    {
                        if (!first)
                        {
                            gp.AddLine((int)o_x, (int)o_y, (int)x, (int)y);
                            count++;
                        }
                        else
                        {
                            first = false;
                        }
                    }
                    o_x = x;
                    o_y = y;
                }
                /*
                if(count>0) 
                { 
                    gp.CloseFigure();
                }
                */
            }

            return gp;
        }
    }
}
