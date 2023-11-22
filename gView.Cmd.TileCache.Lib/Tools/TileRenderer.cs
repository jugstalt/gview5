using gView.Cmd.Core.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.Metadata;
using gView.Framework.system;
using gView.Server.Connector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace gView.Cmd.TileCache.Lib.Tools
{
    public class TileRenderer
    {
        private readonly TileServiceMetadata _metadata;
        private readonly int _epsg;
        private readonly GridOrientation _orientation;
        private readonly string _cacheFormat;
        private readonly IEnumerable<double>? _preRenderScales;
        private readonly string _imageFormat;
        private readonly int _maxParallelRequests;
        private IEnvelope? _bbox = null;
        private ICancelTracker _cancelTracker;

        public TileRenderer(TileServiceMetadata metadata,
                            int epsg,
                            GridOrientation orientation = GridOrientation.UpperLeft,
                            string cacheFormat = "compact",
                            TileImageFormat imageFormat = TileImageFormat.png,
                            IEnvelope? bbox = null,
                            IEnumerable<double>? preRenderScales = null,
                            int maxParallelRequests = 1,
                            ICancelTracker? cancelTracker = null)
        {
            _metadata = metadata;
            _epsg = epsg;
            _orientation = orientation;
            _cacheFormat = cacheFormat;
            _imageFormat = imageFormat.ToString().ToLower();
            _bbox = bbox;
            _preRenderScales = preRenderScales;
            _maxParallelRequests = maxParallelRequests;
            _cancelTracker = cancelTracker ?? new CancelTracker();
        }

        public void Renderer(string server, string service, ICommandLogger? logger, string user = "", string pwd = "")
        {
            ISpatialReference sRef = SpatialReference.FromID($"epsg:{_epsg}");
            if (sRef == null)
            {
                throw new Exception($"Can't load SpatialReference EPSG:{_epsg}");
            }

            IEnvelope metadataBBox = _metadata.GetEPSGEnvelope(_epsg);
            if (metadataBBox == null)
            {
                throw new Exception($"Can't get extent for EPSG:{_epsg} from metadata");
            }
            if (_bbox == null)
            {
                _bbox = metadataBBox;
            }
            else if (metadataBBox.Contains(_bbox))
            {
                throw new Exception($"BBox must be completly contained in tile cache BBox: {metadataBBox.ToBBoxString()}");
            }

            double width = _bbox.Width;
            double height = _bbox.Height;

            double dpu = 1.0;
            if (sRef.SpatialParameters.IsGeographic)
            {
                GeoUnitConverter converter = new GeoUnitConverter();
                dpu = converter.Convert(1.0, GeoUnits.Meters, GeoUnits.DecimalDegrees);
            }

            Grid grid = new Grid(
                (_orientation == GridOrientation.UpperLeft ?
                    _metadata.GetOriginUpperLeft(_epsg) :
                    _metadata.GetOriginLowerLeft(_epsg)),
                _metadata.TileWidth, _metadata.TileHeight, 96.0,
                _orientation);

            int level = 0;
            foreach (double scale in _metadata.Scales.InnerList)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                grid.AddLevel(level++, res);
            }

            ServerConnection connector = new ServerConnection(server);

            int step = _cacheFormat == "compact" ? 128 : 1;

            #region Count Tiles

            int featureMax = 0;
            foreach (double scale in _preRenderScales ?? _metadata.Scales.InnerList)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                int col0 = grid.TileColumn(_bbox.minx, res), col1 = grid.TileColumn(_bbox.maxx, res);
                int row0 = grid.TileRow(_bbox.maxy, res), row1 = grid.TileRow(_bbox.miny, res);

                featureMax += Math.Max(1, (Math.Abs(col1 - col0) + 1) * (Math.Abs(row1 - row0) + 1) / step / step);
            }

            #endregion

            logger?.LogLine($"TilesCount: {featureMax}");

            RenderTileThreadPool threadPool = new RenderTileThreadPool(connector, service, user, pwd, _maxParallelRequests);

            var thread = threadPool.FreeThread!;
            if (_orientation == GridOrientation.UpperLeft)
            {
                thread.Start($"init/{_cacheFormat}/ul/{_epsg}/{_imageFormat}");
            }
            else
            {
                thread.Start($"init/{_cacheFormat}/ll/{_epsg}/{_imageFormat}");
            }

            foreach (double scale in _preRenderScales ?? _metadata.Scales.InnerList)
            {
                double res = scale / (96.0 / 0.0254) * dpu;
                int col0 = grid.TileColumn(_bbox.minx, res), col1 = grid.TileColumn(_bbox.maxx, res);
                int row0 = grid.TileRow(_bbox.maxy, res), row1 = grid.TileRow(_bbox.miny, res);
                int cols = Math.Abs(col1 - col0) + 1;
                int rows = Math.Abs(row1 - row0) + 1;
                col0 = Math.Min(col0, col1);
                row0 = Math.Min(row0, row1);

                int tilePos = 0;
                logger?.LogLine("");
                logger?.LogLine("Scale: " + scale.ToString() + " - " + Math.Max(1, (rows * cols) / step / step).ToString() + " tiles...");

                string boundingTiles = _cacheFormat == "compact" ? "/" + row0 + "|" + (row0 + rows) + "|" + col0 + "|" + (col0 + cols) : String.Empty;

                for (int row = row0; row < (row0 + rows) + (step - 1); row += step)
                {
                    for (int col = col0; col < (col0 + cols) + (step - 1); col += step)
                    {
                        while ((thread = threadPool.FreeThread) == null)
                        {
                            Thread.Sleep(50);
                            if (!_cancelTracker.Continue)
                                return;
                        }
                        if (_orientation == GridOrientation.UpperLeft)
                        {
                            thread.Start($"tile:render/{_cacheFormat}/ul/{_epsg}/{scale.ToDoubleString()}/{row}/{col}.{_imageFormat}{boundingTiles}");
                        }
                        else
                        {
                            thread.Start($"tile:render/{_cacheFormat}/ll/{_epsg}/{scale.ToDoubleString()}/{row}/{col}.{_imageFormat}{boundingTiles}");
                        }

                        tilePos++;
                        if (tilePos % 5 == 0 || _cacheFormat == "compact")
                        {
                            logger?.Log($" ..{tilePos}");
                        }

                        if (!_cancelTracker.Continue)
                            return;
                    }
                }
            }

            while (threadPool.IsFinished == false)
            {
                Thread.Sleep(50);
            }

            if (!String.IsNullOrEmpty(threadPool.Exceptions))
            {
                //MessageBox.Show(threadPool.Exceptions, "Exceptions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #region Helper Classes

        private class RenderTileThreadPool
        {
            private ServerConnection _connector;
            private string _service, _user, _pwd;
            private int _size;
            private StringBuilder _exceptions = new StringBuilder();

            private readonly Thread[] _threads;

            public RenderTileThreadPool(ServerConnection connector, string service, string user, string pwd, int size)
            {
                _connector = connector;
                _service = service;
                _user = user;
                _pwd = pwd;

                _size = size;
                _threads = new Thread[size];
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

            public Thread? FreeThread
            {
                get
                {
                    for (int i = 0; i < _threads.Length; i++)
                    {
                        if (_threads[i] == null || !_threads[i].IsAlive)
                        {
                            _threads[i] = new Thread(new ParameterizedThreadStart(this.Run));
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
                    for (int i = 0; i < _threads!.Length; i++)
                    {
                        if (_threads != null && _threads[i].IsAlive)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        #endregion
    }
}
