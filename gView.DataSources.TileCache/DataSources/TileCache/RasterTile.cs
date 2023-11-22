using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.IO;
using gView.Framework.system;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataSources.TileCache
{
    class RasterTile : IRasterClass, IRasterLayer
    {
        private Dataset _dataset;
        private Grid _grid;
        private int _level, _row, _col;
        private double _resolution;
        private IPolygon _poly;
        private double _oX, _oY, _dx1, _dy2;
        private static int index = 0;

        public RasterTile(Dataset dataset, Grid grid, int level, int row, int col, double resolution)
        {
            _dataset = dataset;
            _grid = grid;
            _level = level;
            _row = row;
            _col = col;
            _resolution = resolution;

            double tileWidth = grid.TileWidth(resolution), tileHeight = grid.TileHeight(resolution);
            _poly = new Polygon(new Ring());
            IPoint tilePoint = _grid.TileUpperLeft(row, col, resolution);
            _poly[0].AddPoint(tilePoint);
            _poly[0].AddPoint(new Point(tilePoint.X + tileWidth, tilePoint.Y));
            _poly[0].AddPoint(new Point(tilePoint.X + tileWidth, tilePoint.Y - tileHeight));
            _poly[0].AddPoint(new Point(tilePoint.X, tilePoint.Y - tileHeight));
            _poly[0].AddPoint(tilePoint);

            _oX = tilePoint.X;
            _oY = tilePoint.Y;
            _dx1 = resolution;
            _dy2 = -resolution;

            //_thread = new Thread(new ThreadStart(this.GetImage));
            //_thread.Start();
            //_thread.Join();
        }

        #region IRasterClass Member

        public Framework.Geometry.IPolygon Polygon
        {
            get { return _poly; }
        }

        public double oX
        {
            get { return _oX; }
        }

        public double oY
        {
            get { return _oY; }
        }

        public double dx1
        {
            get { return _dx1; }
        }

        public double dx2
        {
            get { return 0.0; }
        }

        public double dy1
        {
            get { return 0.0; }
        }

        public double dy2
        {
            get { return _dy2; }
        }

        public Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                return _dataset.SpatialReference;
            }
            set
            {

            }
        }

        async public Task<IRasterPaintContext> BeginPaint(IDisplay display, ICancelTracker cancelTracker)
        {
            if (!cancelTracker.Continue)
            {
                return null;
            }

            return await GetImage();
        }

        #endregion

        #region IClass Member

        public string Name
        {
            get { return "Tile"; }
        }

        public string Aliasname
        {
            get { return this.Name; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion

        #region IRasterLayer Member

        public InterpolationMethod InterpolationMethod
        {
            get
            {
                return InterpolationMethod.Fast;
            }
            set
            {

            }
        }

        public float Opacity
        {
            get
            {
                return 1f;
            }
            set
            {

            }
        }

        public ArgbColor TransparentColor
        {
            get
            {
                return ArgbColor.White;
            }
            set
            {

            }
        }

        public GraphicsEngine.Filters.FilterImplementations FilterImplementation
        {
            get { return GraphicsEngine.Filters.FilterImplementations.Default; }
            set { }
        }

        public IRasterClass RasterClass
        {
            get { return this; }
        }

        #endregion

        #region ILayer Member

        public bool Visible
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double MinimumScale
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double MaximumScale
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double MinimumLabelScale
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double MaximumLabelScale
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public double MaximumZoomToFeatureScale
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IGroupLayer GroupLayer
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IDatasetElement Member

        public string Title
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IClass Class
        {
            get { return this; }
        }

        public int DatasetID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event PropertyChangedHandler PropertyChanged;

        public void FirePropertyChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged();
            }
        }

        #endregion

        #region IID Member

        public int ID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IStringID Member

        public string SID
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasSID
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region IMetadata Member

        public void ReadMetadata(Framework.IO.IPersistStream stream)
        {
            throw new NotImplementedException();
        }

        public Task UpdateMetadataProviders() => Task.CompletedTask;

        public Task WriteMetadata(IPersistStream stream) => Task.CompletedTask;

        public Framework.IO.IMetadataProvider MetadataProvider(Guid guid)
        {
            return null;
        }

        public Task<IEnumerable<IMetadataProvider>> GetMetadataProviders()
        {
            return Task.FromResult<IEnumerable<IMetadataProvider>>(new IMetadataProvider[0]);
        }

        #endregion

        #region INamespace Member

        public string Namespace
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public object ArgbDrawing { get; private set; }

        #endregion

        async private Task<IRasterPaintContext> GetImage()
        {
            FileInfo fi = null;
            if (LocalCachingSettings.UseLocalCaching)
            {
                string fn = $"{LocalCachingSettings.LocalCachingFolder}/{_dataset.DatasetName}/{_level}/{_row}/{_col}.jpg";
                fi = new FileInfo(fn);
                try
                {

                    if (fi.Exists)
                    {
                        // ToDo: Read Async
                        new RasterPaintContext(Current.Engine.CreateBitmap(fn));
                    }
                }
                catch { }
            }
            try
            {
                string url = _dataset.TileUrl, quadkey = String.Empty;
                if (url.Contains("\n"))
                {
                    url = url.Replace("\r", String.Empty);
                    string[] urls = url.Split('\n');

                    url = urls[RasterTile.index % urls.Length];
                    RasterTile.index++;
                    if (RasterTile.index > 1000)
                    {
                        RasterTile.index = 0;
                    }
                }
                if (url.Contains("{3}"))
                {
                    quadkey = _grid.Quadkey(_dataset.Extent, _level, _row, _col, _resolution);
                }

                url = String.Format(url, _col, _row, _level, quadkey);

                using (var responseMessage = await gView.DataSources.TileCache.Dataset._httpClient.GetAsync(url))
                {
                    var bytes = await responseMessage.Content.ReadAsByteArrayAsync();
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bitmap = Current.Engine.CreateBitmap(ms);
                        try
                        {
                            if (fi != null)
                            {
                                fi.Refresh();
                                if (!fi.Exists)
                                {
                                    if (!fi.Directory.Exists)
                                    {
                                        fi.Directory.Create();
                                    }

                                    File.WriteAllBytes(fi.FullName, bytes);
                                }
                            }

                            return new RasterPaintContext(bitmap);
                        }
                        catch (Exception ex)
                        {
                            bitmap?.Dispose();
                            throw ex;
                        }
                    }
                }
            }
            catch { return null; }
        }
    }
}
