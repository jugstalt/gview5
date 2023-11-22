using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataSources.TileCache
{
    class ParentRasterClass : IRasterClass, IParentRasterLayer
    {
        private Dataset _dataset;

        public ParentRasterClass(Dataset dataset)
        {
            _dataset = dataset;
        }

        #region IRasterClass Member

        public Framework.Geometry.IPolygon Polygon
        {
            get
            {
                if (_dataset != null)
                {
                    return _dataset.Extent.ToPolygon(0);
                }
                return null;
            }
        }

        public IBitmap Bitmap
        {
            get { return null; }
        }

        public double oX
        {
            get { return 0D; }
        }

        public double oY
        {
            get { return 0D; }
        }

        public double dx1
        {
            get { return 0D; }
        }

        public double dx2
        {
            get { return 0D; }
        }

        public double dy1
        {
            get { return 0D; }
        }

        public double dy2
        {
            get { return 0D; }
        }

        public Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                return ((gView.DataSources.TileCache.Dataset)this.Dataset).SpatialReference;
            }
            set
            {

            }
        }

        public Task<IRasterPaintContext> BeginPaint(Framework.Carto.IDisplay display, Framework.system.ICancelTracker cancelTracker)
        {
            return Task.FromResult<IRasterPaintContext>(new RasterPaintContext(null));
        }

        #endregion

        #region IClass Member

        public string Name
        {
            get { return this.Dataset.DatasetName; }
        }

        public string Aliasname
        {
            get { return this.Name; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
            set { _dataset = value as Dataset; }
        }

        #endregion

        #region IParentRasterLayer Member

        public Task<IRasterLayerCursor> ChildLayers(gView.Framework.Carto.IDisplay display, string filterClause)
        {
            if (_dataset == null || _dataset.Extent == null || _dataset.Scales == null)
            {
                return null;
            }

            double dpi = 96.0;  // gView Default... //25.4D / 0.28D;   // wmts 0.28mm -> 1 Pixel in WebMercator;

            // !!!! Only correct, if diplay unit is meter !!!! 
            double displayResolution = display.MapScale / (display.Dpi / 0.0254);

            Grid grid = new Grid(
                _dataset.OriginPoint == null ? new Point(_dataset.Extent.minx, _dataset.Extent.maxy) : new Point(_dataset.OriginPoint),
                _dataset.TileWidth,
                _dataset.TileHeight,
                dpi,
                _dataset.OriginOrientation);

            for (int i = 0, to = _dataset.Scales.Length; i < to; i++)
            {
                grid.AddLevel(i, _dataset.Scales[i] / (dpi / 0.0254));
            }

            IEnvelope dispEnvelope = display.DisplayTransformation.TransformedBounds(display); //display.Envelope;
            if (display.GeometricTransformer != null)
            {
                dispEnvelope = ((IGeometry)display.GeometricTransformer.InvTransform2D(dispEnvelope)).Envelope;
            }

            int level = grid.GetBestLevel(displayResolution, 90D);
            double res = grid.GetLevelResolution(level);
            int col0 = grid.TileColumn(dispEnvelope.minx, res);
            int row0 = grid.TileRow(dispEnvelope.maxy, res);

            int col1 = grid.TileColumn(dispEnvelope.maxx, res);
            int row1 = grid.TileRow(dispEnvelope.miny, res);

            int col_from = Math.Max(grid.TileColumn(_dataset.Extent.minx, res) , Math.Min(col0, col1)),
                col_to = Math.Min(grid.TileColumn(_dataset.Extent.maxx, res), Math.Max(col0, col1));
            int row_from = Math.Max(grid.TileRow(_dataset.Extent.maxy, res), Math.Min(row0, row1)),
                row_to = Math.Min(grid.TileRow(_dataset.Extent.miny, res), Math.Max(row0, row1));

            LayerCursor cursor = new LayerCursor();
            for (int r = row_from; r <= row_to; r++)
            {
                for (int c = col_from; c <= col_to; c++)
                {
                    cursor.Layers.Add(
                        new RasterTile(_dataset, grid, level, r, c, res));
                }
            }
            cursor.Layers.Sort(new TileSorter(dispEnvelope.Center));

            return Task.FromResult<IRasterLayerCursor>(cursor);
        }

        #endregion

        #region Classes

        private class LayerCursor : IRasterLayerCursor
        {
            private int _pos = 0;
            internal List<IRasterLayer> Layers = new List<IRasterLayer>();

            #region IRasterLayerCursor Member

            public Task<IRasterLayer> NextRasterLayer()
            {
                if (_pos >= Layers.Count)
                {
                    return Task.FromResult<IRasterLayer>(null);
                }

                return Task.FromResult<IRasterLayer>(Layers[_pos++]);
            }

            #endregion

            #region IDisposable Member

            public void Dispose()
            {

            }

            #endregion
        }

        public class TileSorter : IComparer<IRasterLayer>
        {
            private IPoint _center;

            public TileSorter(IPoint centerPoint)
            {
                _center = centerPoint;
            }

            #region IComparer<IRasterClass> Member

            public int Compare(IRasterLayer x, IRasterLayer y)
            {
                IPolygon p1 = ((IRasterClass)x).Polygon;
                IPolygon p2 = ((IRasterClass)y).Polygon;

                if (p1 == null)
                {
                    return 1;
                }

                if (p2 == null)
                {
                    return -1;
                }

                double d1 = _center.Distance2(p1[0][0]);
                double d2 = _center.Distance2(p2[0][0]);

                if (d1 < d2)
                {
                    return -1;
                }

                if (d1 > d2)
                {
                    return 1;
                }

                return 0;
            }

            #endregion
        }


        #endregion
    }
}
