using gView.DataSources.Fdb.MSSql;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.DataSources.Fdb.UI.MSSql
{
    public class CreateTileGridClass : IProgressReporter
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private string _name, _cacheDirectory;
        private IFeatureDataset _targetDataset;
        private ISpatialIndexDef _spatialIndexDef;
        private IRasterDataset _sourceDataset;
        private double _tileSizeX, _tileSizeY, _resX, _resY;
        private int _levels;
        private bool _createTiles = true;
        private gView.DataSources.Fdb.MSSql.SqlFDB _fdb = null;
        private TileGridType _gridType = TileGridType.image_jpg;
        private TileLevelType _levelType = TileLevelType.ConstantImagesize;
        private CancelTracker _cancelTracker = new CancelTracker();
        private List<int> _createLevels;

        public CreateTileGridClass(
            string name,
            IFeatureDataset targetDataset,
            ISpatialIndexDef spatialIndexDef,
            IRasterDataset sourceDataset,
            double tileSizeX, double tileSizeY,
            double resX, double resY,
            int levels,
            string cacheDirectory,
            TileGridType gridType)
        {
            _name = name;
            _cacheDirectory = cacheDirectory;
            _targetDataset = targetDataset;
            _spatialIndexDef = spatialIndexDef;
            _sourceDataset = sourceDataset;
            _tileSizeX = tileSizeX;
            _tileSizeY = tileSizeY;
            _resX = resX;
            _resY = resY;
            _levels = levels;
            _gridType = gridType;

            _fdb = (SqlFDB)_targetDataset.Database;

            _createLevels = new List<int>();
            for (int i = 0; i < _levels; i++)
            {
                _createLevels.Add(i);
            }
        }

        public bool CreateTiles
        {
            get { return _createTiles; }
            set { _createTiles = value; }
        }
        public TileLevelType TileLevelType
        {
            get { return _levelType; }
            set { _levelType = value; }
        }
        public List<int> CreateLevels
        {
            get { return _createLevels; }
            set { _createLevels = value; }
        }

        // Thread
        async private Task Run()
        {
            if (_targetDataset == null || _fdb == null || _sourceDataset == null)
            {
                return;
            }

            //if (_targetDataset[_name] != null)
            //{
            //    MessageBox.Show("Featureclass '" + _name + "' already exists!");
            //    return;
            //}
            bool succeeded = false;
            try
            {
                Envelope bounds = new Envelope(_spatialIndexDef.SpatialIndexBounds);
                Envelope iBounds = new Envelope(bounds.minx - _tileSizeX, bounds.miny - _tileSizeY,
                                                bounds.maxx + _tileSizeX, bounds.maxy + _tileSizeY);

                _cacheDirectory += @"/" + _name;
                if (!String.IsNullOrEmpty(_cacheDirectory))
                {
                    DirectoryInfo di = new DirectoryInfo(_cacheDirectory);
                    if (!di.Exists)
                    {
                        di.Create();
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append("<TileCacheDefinition>\r\n");
                    sb.Append(" <General levels='" + _levels + "' origin='lowerleft' />\r\n");
                    sb.Append(" <Envelope minx='" + bounds.minx.ToString(_nhi) + "' miny='" + bounds.miny.ToString(_nhi) + "' maxx='" + bounds.maxx.ToString(_nhi) + "' maxy='" + bounds.maxy.ToString(_nhi) + "' />\r\n");
                    sb.Append(" <TileSize x='" + _tileSizeX.ToString(_nhi) + "' y='" + _tileSizeY.ToString(_nhi) + "' />\r\n");
                    sb.Append(" <TileResolution x='" + _resX.ToString(_nhi) + "' y='" + _resY.ToString(_nhi) + "' />\r\n");
                    sb.Append("</TileCacheDefinition>");

                    StreamWriter sw = new StreamWriter(di.FullName + @"/tilecache.xml");
                    sw.WriteLine(sb.ToString());
                    sw.Close();
                }
                ProgressReport report = new ProgressReport();

                int datasetId = await _fdb.DatasetID(_targetDataset.DatasetName);
                if (datasetId == -1)
                {
                    return;
                }

                IClass cls = null;
                try
                {
                    cls = (await _sourceDataset.Elements())[0].Class;
                }
                catch { cls = null; }
                IMultiGridIdentify gridClass = cls as IMultiGridIdentify;
                if (_gridType == TileGridType.binary_float && gridClass == null)
                {
                    return;
                }

                IFeatureClass sourceFc = cls as IFeatureClass;

                Map map = null;
                if (_gridType == TileGridType.image_jpg || _gridType == TileGridType.image_png)
                {
                    map = new Map();
                    ILayer layer = LayerFactory.Create(cls);
                    map.AddLayer(layer);
                    //map.iWidth = (int)(_tileSizeX / _resX);
                    //map.iHeight = (int)(_tileSizeY / _resY);
                }


                #region Create Featureclass
                IFeatureClass fc = null;
                IDatasetElement element = await _targetDataset.Element(_name);
                if (element != null && element.Class is IFeatureClass)
                {
                    fc = (IFeatureClass)element.Class;
                    if (fc.GeometryType == GeometryType.Polygon &&
                        fc.FindField("GRID_LEVEL") != null &&
                        fc.FindField("GRID_ROW") != null &&
                        fc.FindField("GRID_COLUMN") != null &&
                        fc.FindField("FILE") != null)
                    {
                        if (MessageBox.Show("TileGridClass already exists. Do you wan't to append to this Grid?",
                            "Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                    else
                    {
                        await _fdb.DeleteFeatureClass(_name);
                        fc = null;
                    }
                }
                if (fc == null)
                {
                    FieldCollection fields = new FieldCollection();

                    fields.Add(new Field("GRID_LEVEL", FieldType.integer));
                    fields.Add(new Field("GRID_ROW", FieldType.integer));
                    fields.Add(new Field("GRID_COLUMN", FieldType.integer));
                    fields.Add(new Field("FILE", FieldType.String, 512));

                    await _fdb.CreateFeatureClass(_targetDataset.DatasetName, _name,
                        new GeometryDef(GeometryType.Polygon),
                        fields);
                    element = await _targetDataset.Element(_name);
                    if (element == null || !(element.Class is IFeatureClass))
                    {
                        return;
                    }

                    await _fdb.SetSpatialIndexBounds(_name, "BinaryTree2", iBounds, _spatialIndexDef.SplitRatio, _spatialIndexDef.MaxPerNode, _spatialIndexDef.Levels);
                    fc = (IFeatureClass)element.Class;
                }
                #endregion

                #region Create Tiles

                #region Report
                double tx = _tileSizeX, ty = _tileSizeY;
                if (ReportProgress != null)
                {
                    report.featureMax = 0;
                    for (int i = 0; i < _levels; i++)
                    {
                        if (_createLevels.Contains(i))
                        {
                            for (double y = bounds.miny; y < bounds.maxy; y += ty)
                            {
                                for (double x = bounds.minx; x < bounds.maxx; x += tx)
                                {
                                    report.featureMax++;
                                }
                            }
                        }
                        if (_levelType == TileLevelType.ConstantImagesize)
                        {
                            tx *= 2;
                            ty *= 2;
                        }
                    }
                    report.Message = "Create Tiles";
                    report.featurePos = 0;
                    ReportProgress(report);
                }
                int reportInterval = (_createTiles ? 10 : 1000);
                #endregion

                List<IFeature> features = new List<IFeature>();
                for (int level = 0; level < _levels; level++)
                {
                    if (map != null)
                    {
                        map.ImageWidth = (int)(_tileSizeX / _resX);
                        map.ImageHeight = (int)(_tileSizeY / _resY);
                    }
                    if (_createLevels.Contains(level))
                    {
                        int row = 0;
                        for (double y = bounds.miny; y < bounds.maxy; y += _tileSizeY)
                        {
                            DirectoryInfo di = new DirectoryInfo(_cacheDirectory + @"/" + level + @"/" + row);
                            if (!di.Exists)
                            {
                                di.Create();
                            }

                            int column = 0;
                            for (double x = bounds.minx; x < bounds.maxx; x += _tileSizeX)
                            {
                                #region Polygon
                                Polygon polygon = new Polygon();
                                Ring ring = new Ring();
                                ring.AddPoint(new Point(x, y));
                                ring.AddPoint(new Point(Math.Min(x + _tileSizeX, bounds.maxx), y));
                                ring.AddPoint(new Point(Math.Min(x + _tileSizeX, bounds.maxx), Math.Min(y + _tileSizeY, bounds.maxy)));
                                ring.AddPoint(new Point(x, Math.Min(y + _tileSizeY, bounds.maxy)));
                                ring.Close();
                                polygon.AddRing(ring);
                                #endregion

                                if (sourceFc != null)
                                {
                                    SpatialFilter filter = new SpatialFilter();
                                    filter.AddField(sourceFc.IDFieldName);
                                    filter.Geometry = polygon;
                                    filter.FilterSpatialReference = fc.SpatialReference;
                                    using (IFeatureCursor cursor = await sourceFc.GetFeatures(filter))
                                    {
                                        if (await cursor.NextFeature() == null)
                                        {
                                            column++;
                                            report.featurePos++;
                                            if (ReportProgress != null && report.featurePos % reportInterval == 0)
                                            {
                                                ReportProgress(report);
                                            }

                                            continue;
                                        }
                                    }
                                }

                                string relFilename = level + "/" + row + "/" + column + ".bin";

                                if (_createTiles)
                                {
                                    string filename = di.FullName + @"/" + column;
                                    if (_gridType == TileGridType.binary_float)
                                    {
                                        float[] vals = await gridClass.MultiGridQuery(
                                            null,
                                            new IPoint[] { ring[0], ring[1], ring[3] },
                                            _resX, _resY,
                                            fc.SpatialReference, null);
                                        if (!HasFloatArrayData(vals))
                                        {
                                            column++;
                                            report.featurePos++;
                                            if (ReportProgress != null && report.featurePos % reportInterval == 0)
                                            {
                                                ReportProgress(report);
                                            }

                                            continue;
                                        }
                                        StoreFloatArray(filename + ".bin", x, y, _resX, _resY, vals);
                                    }
                                    else if (map != null)
                                    {
                                        map.ZoomTo(new Envelope(x, y, x + _tileSizeX, y + _tileSizeY));
                                        await map.RefreshMap(DrawPhase.All, _cancelTracker);
                                        if (_gridType == TileGridType.image_png)
                                        {
                                            map.Bitmap.Save(filename + ".png", GraphicsEngine.ImageFormat.Png);
                                        }
                                        else if (_gridType == TileGridType.image_jpg)
                                        {
                                            map.Bitmap.Save(filename + ".jpg", GraphicsEngine.ImageFormat.Jpeg);
                                        }
                                    }
                                }

                                Feature feature = new Feature();
                                feature.Shape = polygon;
                                feature.Fields.Add(new FieldValue("GRID_LEVEL", level));
                                feature.Fields.Add(new FieldValue("GRID_ROW", row));
                                feature.Fields.Add(new FieldValue("GRID_COLUMN", column));
                                feature.Fields.Add(new FieldValue("FILE", relFilename));

                                features.Add(feature);
                                column++;
                                report.featurePos++;
                                if (features.Count >= reportInterval)
                                {
                                    if (ReportProgress != null)
                                    {
                                        ReportProgress(report);
                                    }

                                    if (!await _fdb.Insert(fc, features))
                                    {
                                        MessageBox.Show(_fdb.LastErrorMessage, "DB Insert Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    }
                                    features.Clear();

                                    if (!_cancelTracker.Continue)
                                    {
                                        succeeded = true;
                                        return;
                                    }
                                }
                            }
                            row++;
                        }
                    }
                    if (_levelType == TileLevelType.ConstantImagesize)
                    {
                        _tileSizeX *= 2;
                        _tileSizeY *= 2;
                    }
                    _resX *= 2;
                    _resY *= 2;
                }
                if (features.Count > 0)
                {
                    if (ReportProgress != null)
                    {
                        ReportProgress(report);
                    }

                    await _fdb.Insert(fc, features);
                }
                await _fdb.CalculateExtent(fc);
                #endregion

                succeeded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!succeeded)
                {
                    await _fdb.DeleteFeatureClass(_name);
                }
            }
        }

        public Task RunTask()
        {
            return this.Run();
        }

        #region IProgressReporter Member

        public event ProgressReporterEvent ReportProgress = null;

        public gView.Framework.system.ICancelTracker CancelTracker
        {
            get { return _cancelTracker; }
        }

        #endregion

        #region Helper
        private bool StoreFloatArray(string filename, double x, double y, double resX, double resY, float[] f)
        {
            StreamWriter sw = new StreamWriter(filename);
            BinaryWriter bw = new BinaryWriter(sw.BaseStream);

            bw.Write(x);
            bw.Write(y);
            bw.Write(resX);
            bw.Write(resY);
            foreach (float v in f)
            {
                bw.Write(v);
            }

            sw.Close();

            return true;
        }
        private bool HasFloatArrayData(float[] f)
        {
            float nodata = float.MinValue;

            for (int i = 2; i < f.Length; i++) // 0 und 1 sind die GridSize
            {
                if (f[i] != nodata)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
