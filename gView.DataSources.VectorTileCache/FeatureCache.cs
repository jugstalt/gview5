using GeoJSON.Net.Feature;
using gView.Data.Framework.Data.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Geometry;
using Mapbox.Vector.Tile;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataSources.VectorTileCache
{
    public class FeatureCache : IDatasetCache
    {
        private readonly Dataset _dataset;
        private readonly WebMercatorGrid _grid;

        private ConcurrentDictionary<string, ConcurrentBag<Feature>> _features = new ConcurrentDictionary<string, ConcurrentBag<Feature>>();

        public FeatureCache(Dataset dataset)
        {
            _dataset = dataset;
            _grid = new WebMercatorGrid();
        }

        internal Dataset Dataset => _dataset;

        async public Task LoadAsync(IDisplay display)
        {
            double displayResolution = display.MapScale / (/*display.dpi*/96.0 / 0.0254);

            int level = _grid.GetBestLevel(displayResolution, 90D);

            IEnvelope dispEnvelope = display.DisplayTransformation.TransformedBounds(display);
            if (display.GeometricTransformer != null)
            {
                dispEnvelope = (IEnvelope)((IGeometry)display.GeometricTransformer.InvTransform2D(dispEnvelope)).Envelope;
            }

            dispEnvelope = GeometricTransformerFactory.Transform2D(dispEnvelope, _dataset.SpatialReference, _dataset.WebMercatorSpatialReference)?.Envelope;

            double res = _grid.GetLevelResolution(level);
            int col0 = _grid.TileColumn(dispEnvelope.minx, res);
            int row0 = _grid.TileRow(dispEnvelope.maxy, res);

            int col1 = _grid.TileColumn(dispEnvelope.maxx, res);
            int row1 = _grid.TileRow(dispEnvelope.miny, res);

            int col_from = Math.Max(0, Math.Min(col0, col1)), col_to = Math.Min((int)Math.Round(_grid.Extent.Width / (_grid.TileSizeX * res), 0) - 1, Math.Max(col0, col1));
            int row_from = Math.Max(0, Math.Min(row0, row1)), row_to = Math.Min((int)Math.Round(_grid.Extent.Height / (_grid.TileSizeY * res), 0) - 1, Math.Max(row0, row1));

            await LoadAsync(level, col_from, col_to, row_from, row_to);
        }

        async public Task LoadAsync(int level, int col_from, int col_to, int row_from, int row_to)
        {
            if (_dataset.TileUrls == null || _dataset.TileUrls.Length == 0)
            {
                return;
            }

            _features.Clear();

            foreach (var dsElement in await _dataset.Elements())
            {
                _features.TryAdd(dsElement.Title, new ConcurrentBag<Feature>());
            }

            var url = _dataset.TileUrls[0];
            List<Task> task = new List<Task>();

            for (int r = row_from; r <= row_to; r++)
            {
                for (int c = col_from; c <= col_to; c++)
                {
                    var tileUrl = url
                        .Replace("{z}", level.ToString())
                        .Replace("{x}", c.ToString())
                        .Replace("{y}", r.ToString());

                    task.Add(Download(level, c, r, tileUrl));
                }
            }

            Task.WaitAll(task.ToArray());

            return;
        }

        async private Task Download(int level, int col, int row, string tileUrl)
        {
            try
            {
                var bytes = await DownloadTile(level, col, row, tileUrl);

                var layerInfos = VectorTileParser.Parse(new MemoryStream(bytes));

                foreach (var layerInfo in layerInfos)
                {
                    var fc = layerInfo.ToGeoJSON(col, row, level);
                    if (fc.Features != null &&
                        fc.Features.Count > 0 &&
                        _features.ContainsKey(layerInfo.Name))
                    {
                        foreach (var feature in fc.Features)
                        {
                            _features[layerInfo.Name].Add(feature);
                        }
                    }
                }

                StoreToLocalcache(level, col, row, bytes);
            }
            catch (Exception ex)
            {
                _dataset.LastErrorMessage = $"{ex.Message} at \n{ex.StackTrace}";
            }
        }

        public IEnumerable<string> LayersNames => _features.Keys;

        public int FeatureCount(string layername) => _features.ContainsKey(layername) ? _features[layername].Count : 0;

        public string ToGeoJson(string layername)
        {
            return JsonConvert.SerializeObject(
                new { type = "FeatureCollection", features = _features[layername] }
                );
        }

        public IEnumerable<Feature> this[string layername]
        {
            get
            {
                var features = _features.ContainsKey(layername) ? _features[layername].ToArray() : new Feature[0];

                return features;//.DistinctBy(f => f.Id);
            }
        }

        #region IDisposable

        public void Dispose()
        {
            _features.Clear();
        }

        #endregion

        #region Localcaching

        private const string LocalCachePath = @"C:\temp\vtc";

        private string LocalStorageTilePath(int level, int col, int row) => $"{LocalCachePath}/{level}/{col}/{row}.mvt";

        private void StoreToLocalcache(int level, int col, int row, byte[] data)
        {
            //FileInfo fi = new FileInfo(LocalStorageTilePath(level, col, row));
            //if (fi.Exists)
            //    return;

            //if (!fi.Directory.Exists)
            //    fi.Directory.Create();

            //File.WriteAllBytes(fi.FullName, data);
        }

        async private Task<byte[]> DownloadTile(int level, int col, int row, string tileUrl)
        {
            //FileInfo fi = new FileInfo(LocalStorageTilePath(level, col, row));
            //if (fi.Exists)
            //{
            //    using (var stream = new FileStream(fi.FullName, FileMode.Open))
            //    {
            //        var buffer = new byte[stream.Length];
            //        await stream.ReadAsync(buffer, 0, buffer.Length);
            //        return buffer;
            //    }
            //}
            //else
            {
                using (var responseMesssage = await Dataset._httpClient.GetAsync(tileUrl))
                {
                    var bytes = await responseMesssage.Content.ReadAsByteArrayAsync();
                    return bytes;
                }
            }
        }

        #endregion
    }
}

