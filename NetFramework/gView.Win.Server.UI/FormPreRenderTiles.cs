using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.IO;
using gView.Framework.Metadata;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using gView.Framework.UI.Dialogs;
using gView.Interoperability.Server;
using gView.Server.Connector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.MapServer.Lib.UI
{
    public partial class FormPreRenderTiles : Form, IProgressReporter
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private TileServiceMetadata _metadata;
        private MapServerClass _mapServerClass;
        private List<double> _preRenderScales = new List<double>();
        private int _maxParallelRequests = 2;
        private string _imgExt = ".png";
        private int _selectedEpsg = 0;
        private IEnvelope _bounds = null;
        private GridOrientation _orientation = GridOrientation.UpperLeft;
        private string _cacheFormat = "normal";

        public FormPreRenderTiles(TileServiceMetadata metadata, MapServerClass mapServerClass)
        {
            _metadata = metadata;
            _mapServerClass = mapServerClass;

            InitializeComponent();

            if (_metadata != null)
            {
                foreach (int epsg in _metadata.EPSGCodes)
                {
                    cmbEpsg.Items.Add(epsg);
                }
                if (cmbEpsg.Items.Count > 0)
                {
                    cmbEpsg.SelectedIndex = 0;
                }
            }

            if (_mapServerClass != null && _mapServerClass.Dataset != null)
            {
                txtServer.Text = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "server");
                txtService.Text = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "service");
            }

            numMaxParallelRequest.Value = _maxParallelRequests;

            if (metadata.FormatPng)
            {
                cmbImageFormat.Items.Add("image/png");
            }

            if (metadata.FormatJpg)
            {
                cmbImageFormat.Items.Add("image/jpeg");
            }

            if (metadata.UpperLeft && metadata.UpperLeftCacheTiles)
            {
                cmbOrigin.Items.Add("Upper Left");
            }

            if (metadata.LowerLeft && metadata.LowerLeftCacheTiles)
            {
                cmbOrigin.Items.Add("Lower Left");
            }

            if (cmbImageFormat.Items.Count > 0)
            {
                cmbImageFormat.SelectedIndex = 0;
            }

            if (cmbOrigin.Items.Count > 0)
            {
                cmbOrigin.SelectedIndex = 0;
            }

            if (cmbCacheFormat.Items.Count > 0)
            {
                cmbCacheFormat.SelectedIndex = 0;
            }
        }

        private void cmbEpsg_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstScales.Items.Clear();

            if (_metadata == null)
            {
                return;
            }

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + cmbEpsg.SelectedItem.ToString());
            if (sRef == null)
            {
                return;
            }

            IEnvelope extent = _metadata.GetEPSGEnvelope((int)cmbEpsg.SelectedItem);
            if (extent == null)
            {
                return;
            }

            double width = extent.Width;
            double height = extent.Height;

            double dpu = 1.0;
            if (sRef.SpatialParameters.IsGeographic)
            {
                GeoUnitConverter converter = new GeoUnitConverter();
                dpu = converter.Convert(1.0, GeoUnits.Meters, GeoUnits.DecimalDegrees);
            }

            foreach (double scale in _metadata.Scales.InnerList)
            {
                double tileWidth = (double)_metadata.TileWidth * (double)scale / (96.0 / 0.0254);
                double tileHeight = (double)_metadata.TileHeight * (double)scale / (96.0 / 0.0254);

                tileWidth *= dpu;
                tileHeight *= dpu;

                int tx = (int)Math.Floor(width / tileWidth) + 1;
                int ty = (int)Math.Floor(height / tileHeight) + 1;

                lstScales.Items.Add(new ListViewItem(new string[] { scale.ToString(_nhi), ty.ToString(), tx.ToString(), (tx * ty).ToString() }));
                lstScales.Items[lstScales.Items.Count - 1].Checked = true;
            }

            if (!Envelope.IsNull(this.CurrentExtent) &&
                cmbEpsg.SelectedItem != null)
            {
                ISpatialReference oldSRef = SpatialReference.FromID(lblEpsg.Text.Replace("(", "").Replace(")", ""));
                ISpatialReference newSRef = SpatialReference.FromID("epsg:" + cmbEpsg.SelectedItem.ToString());

                using (var geometricTransformer = GeometricTransformerFactory.Create())
                {
                    geometricTransformer.SetSpatialReferences(oldSRef, newSRef);
                    IGeometry geom = geometricTransformer.Transform2D(this.CurrentExtent) as IGeometry;
                    if (geom != null)
                    {
                        this.CurrentExtent = geom.Envelope;
                    }
                }
            }
            lblEpsg.Text = "(EPSG:" + (cmbEpsg.SelectedItem != null ? cmbEpsg.SelectedItem.ToString() : "0") + ")";
        }

        private void btnPreRender_Click(object sender, EventArgs e)
        {
            _preRenderScales.Clear();
            foreach (ListViewItem item in lstScales.Items)
            {
                if (item.Checked)
                {
                    _preRenderScales.Add(double.Parse(item.Text.Replace(",", "."), _nhi));
                }
            }

            _maxParallelRequests = (int)numMaxParallelRequest.Value;
            switch (cmbImageFormat.SelectedItem.ToString().ToLower())
            {
                case "image/png":
                    _imgExt = ".png";
                    break;
                case "image/jpeg":
                    _imgExt = ".jpg";
                    break;
            }
            switch (cmbOrigin.SelectedItem.ToString().ToLower())
            {
                case "upper left":
                    _orientation = GridOrientation.UpperLeft;
                    break;
                case "lower left":
                    _orientation = GridOrientation.LowerLeft;
                    break;
            }
            _selectedEpsg = (int)cmbEpsg.SelectedItem;
            if (chkUseExtent.Checked == true)
            {
                _bounds = this.CurrentExtent;
            }
            else
            {
                _bounds = null;
            }

            _cacheFormat = cmbCacheFormat.SelectedItem.ToString().ToLower();

            FormTaskProgress dlg = new FormTaskProgress();
            dlg.ShowProgressDialog(this, this.RunAndWait());
        }

        async private Task RunAndWait()
        {
            Thread thread = new Thread(new ThreadStart(RunThread));
            thread.Start();

            while (thread.IsAlive)
            {
                await Task.Delay(100);
            }
        }
        private void RunThread()
        {
            if (_metadata == null || _mapServerClass == null || _mapServerClass.Dataset == null || _preRenderScales.Count == 0)
            {
                return;
            }

            //await Task.Delay(1000);

            string server = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "service");
            string user = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "user");
            string pwd = ConfigTextStream.ExtractValue(_mapServerClass.Dataset.ConnectionString, "pwd");

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + _selectedEpsg);
            if (sRef == null)
            {
                return;
            }

            IEnvelope extent = _metadata.GetEPSGEnvelope(_selectedEpsg);
            if (extent == null)
            {
                return;
            }

            if (_bounds == null)
            {
                _bounds = extent;
            }

            double width = extent.Width;
            double height = extent.Height;

            double dpu = 1.0;
            if (sRef.SpatialParameters.IsGeographic)
            {
                GeoUnitConverter converter = new GeoUnitConverter();
                dpu = converter.Convert(1.0, GeoUnits.Meters, GeoUnits.DecimalDegrees);
            }

            Grid grid = new Grid(
                (_orientation == GridOrientation.UpperLeft ?
                    _metadata.GetOriginUpperLeft(_selectedEpsg) :
                    _metadata.GetOriginLowerLeft(_selectedEpsg)),
                /*new Point(extent.minx, extent.maxy) :
                new Point(extent.minx, extent.miny))*/
                _metadata.TileWidth, _metadata.TileHeight, 96.0,
                _orientation);

            int level = 0;
            foreach (double scale in _metadata.Scales.InnerList)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                grid.AddLevel(level++, res);
            }

            ServerConnection connector = new ServerConnection(server);
            ProgressReport report = new ProgressReport();
            _cancelTracker.Reset();

            int step = _cacheFormat == "compact" ? 128 : 1;

            #region Count Tiles

            report.featureMax = 0;
            foreach (double scale in _preRenderScales)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                int col0 = grid.TileColumn(_bounds.minx, res), col1 = grid.TileColumn(_bounds.maxx, res);
                int row0 = grid.TileRow(_bounds.maxy, res), row1 = grid.TileRow(_bounds.miny, res);

                report.featureMax += Math.Max(1, (Math.Abs(col1 - col0) + 1) * (Math.Abs(row1 - row0) + 1) / step / step);
            }

            #endregion

            RenderTileThreadPool threadPool = new RenderTileThreadPool(connector, service, user, pwd, _maxParallelRequests);

            var thread = threadPool.FreeThread;
            if (_orientation == GridOrientation.UpperLeft)
            {
                thread.Start("init/" + _cacheFormat + "/ul/" + cmbEpsg.SelectedItem.ToString() + "/" + _imgExt.Replace(".", ""));
            }
            else
            {
                thread.Start("init/" + _cacheFormat + "/ll/" + cmbEpsg.SelectedItem.ToString() + "/" + _imgExt.Replace(".", ""));
            }

            foreach (double scale in _preRenderScales)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                int col0 = grid.TileColumn(_bounds.minx, res), col1 = grid.TileColumn(_bounds.maxx, res);
                int row0 = grid.TileRow(_bounds.maxy, res), row1 = grid.TileRow(_bounds.miny, res);
                int cols = Math.Abs(col1 - col0) + 1;
                int rows = Math.Abs(row1 - row0) + 1;
                col0 = Math.Min(col0, col1);
                row0 = Math.Min(row0, row1);

                if (ReportProgress != null)
                {
                    report.Message = "Scale: " + scale.ToString() + " - " + Math.Max(1, (rows * cols) / step / step).ToString() + " tiles...";
                    ReportProgress(report);
                }

                string boundingTiles = _cacheFormat == "compact" ? "/" + row0 + "|" + (row0 + rows) + "|" + col0 + "|" + (col0 + cols) : String.Empty;

                for (int row = row0; row < (row0 + rows) + (step - 1); row += step)
                {
                    for (int col = col0; col < (col0 + cols) + (step - 1); col += step)
                    {
                        while ((thread = threadPool.FreeThread) == null)
                        {
                            Thread.Sleep(50);
                            if (!_cancelTracker.Continue)
                            {
                                return;
                            }
                        }
                        if (_orientation == GridOrientation.UpperLeft)
                        {
                            thread.Start("tile:render/" + _cacheFormat + "/ul/" + cmbEpsg.SelectedItem.ToString() + "/" + scale.ToString(_nhi) + "/" + row + "/" + col + _imgExt + boundingTiles);
                        }
                        else
                        {
                            thread.Start("tile:render/" + _cacheFormat + "/ll/" + cmbEpsg.SelectedItem.ToString() + "/" + scale.ToString(_nhi) + "/" + row + "/" + col + _imgExt + boundingTiles);
                        }

                        if (ReportProgress != null)
                        {
                            report.featurePos++;
                            if (report.featurePos % 5 == 0 || _cacheFormat == "compact")
                            {
                                ReportProgress(report);
                            }
                        }
                        if (!_cancelTracker.Continue)
                        {
                            return;
                        }
                    }
                }
            }

            while (threadPool.IsFinished == false)
            {
                Thread.Sleep(50);
            }

            if (!String.IsNullOrEmpty(threadPool.Exceptions))
            {
                MessageBox.Show(threadPool.Exceptions, "Exceptions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #region IProgressReporter Member

        public event ProgressReporterEvent ReportProgress;

        private CancelTracker _cancelTracker = new CancelTracker();
        public ICancelTracker CancelTracker
        {
            get { return _cancelTracker; }
        }

        #endregion

        #region Properties
        public IEnvelope CurrentExtent
        {
            get
            {
                return new Envelope(
                    (double)numLeft.Value, (double)numBottom.Value,
                    (double)numRight.Value, (double)numTop.Value);
            }
            set
            {
                if (value == null)
                {
                    numLeft.Value = numBottom.Value = numRight.Value = numTop.Value = (decimal)0.0;
                }
                else
                {
                    try
                    {
                        numLeft.Value = (decimal)value.minx;
                        numBottom.Value = (decimal)value.miny;
                        numRight.Value = (decimal)value.maxx;
                        numTop.Value = (decimal)value.maxy;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
        #endregion

        #region Helper Classes

        private class RenderTileThreadPool
        {
            ServerConnection _connector;
            string _service, _user, _pwd;
            int _size;
            StringBuilder _exceptions = new StringBuilder();

            List<Thread> _threads = new List<Thread>();

            public RenderTileThreadPool(ServerConnection connector, string service, string user, string pwd, int size)
            {
                _connector = connector;
                _service = service;
                _user = user;
                _pwd = pwd;

                _size = size;

                _connector.Timeout = 3600; // 1h
            }

            public string Exceptions
            {
                get { return _exceptions.ToString(); }
            }

            private void Run(object args)
            {
                try
                {
                    _connector.Send(_service, args.ToString(), "ED770739-12FA-40d7-8EF9-38FE9299564A", _user, _pwd);
                }
                catch (Exception ex)
                {
                    _exceptions.Append(ex.Message + "\n");
                }
            }

            public Thread FreeThread
            {
                get
                {
                    if (_threads.Count < _size)
                    {
                        Thread thread = new Thread(new ParameterizedThreadStart(this.Run), 1024);
                        _threads.Add(thread);
                        return thread;
                    }
                    for (int i = 0; i < _threads.Count; i++)
                    {
                        if (!_threads[i].IsAlive)
                        {
                            _threads[i] = new Thread(new ParameterizedThreadStart(this.Run), 1024);
                            return _threads[i];
                        }
                    }

                    return null;
                }
            }
            public bool IsFinished
            {
                get
                {
                    for (int i = 0; i < _threads.Count; i++)
                    {
                        if (_threads[i].IsAlive)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        #endregion

        #region Helper
        private IEnvelope ClassEnvelope(IClass Class, ISpatialReference sRef)
        {
            IEnvelope envelope = null;
            ISpatialReference classSRef = null;

            if (Class is IFeatureClass)
            {
                envelope = ((IFeatureClass)Class).Envelope;
                classSRef = ((IFeatureClass)Class).SpatialReference;
            }
            else if (Class is IRasterClass && ((IRasterClass)Class).Polygon != null)
            {
                envelope = ((IRasterClass)Class).Polygon.Envelope;
                classSRef = ((IRasterClass)Class).SpatialReference;
            }

            if (envelope != null && classSRef != null && sRef != null && !sRef.Equals(classSRef))
            {
                using (var geometricTransformer = GeometricTransformerFactory.Create())
                {
                    geometricTransformer.SetSpatialReferences(classSRef, sRef);
                    IGeometry geom = geometricTransformer.Transform2D(envelope) as IGeometry;
                    if (geom == null)
                    {
                        return null;
                    }

                    envelope = geom.Envelope;
                }
            }
            return envelope;
        }
        #endregion

        async private void btnImport_Click(object sender, EventArgs e)
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
            filters.Add(new OpenDataFilter());

            ExplorerDialog dlg = new ExplorerDialog("Import Extent", filters, true);
            dlg.MulitSelection = true;

            ISpatialReference sRef = SpatialReference.FromID("epsg:" + cmbEpsg.SelectedItem.ToString());

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                IEnvelope bounds = null;
                foreach (IExplorerObject exObject in dlg.ExplorerObjects)
                {
                    var objectInstance = await exObject?.GetInstanceAsync();
                    if (objectInstance == null)
                    {
                        continue;
                    }

                    IEnvelope objEnvelope = null;

                    if (objectInstance is IDataset)
                    {
                        foreach (IDatasetElement element in await ((IDataset)objectInstance).Elements())
                        {
                            if (element == null)
                            {
                                continue;
                            }

                            objEnvelope = ClassEnvelope(element.Class, sRef);
                        }
                    }
                    else
                    {
                        objEnvelope = ClassEnvelope(objectInstance as IClass, sRef);
                    }

                    if (objEnvelope != null)
                    {
                        if (bounds == null)
                        {
                            bounds = new Envelope(objEnvelope);
                        }
                        else
                        {
                            bounds.Union(objEnvelope);
                        }
                    }
                }

                if (bounds != null)
                {
                    this.CurrentExtent = bounds;
                }
            }
        }
    }
}
