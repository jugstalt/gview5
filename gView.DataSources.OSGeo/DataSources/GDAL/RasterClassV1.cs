using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.LinAlg;
using gView.Framework.system;
using gView.Geometry.Framework.Geometry.Extesnsions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.GDAL
{
    public class RasterClassV1 : IRasterClass, IRasterFile, IRasterFileBitmap, IPointIdentify, IGridIdentify, IGridClass, IPersistable, IDisposable
    {
        internal enum RasterType { image = 0, grid = 1, wavelet = 2 }

        private string _filename, _title;
        private bool _valid = true;
        private int _iWidth = 0, _iHeight = 0;
        internal TFWFile _tfw;
        private IPolygon _polygon;
        private ISpatialReference _sRef = null;
        private IRasterDataset _dataset;
        private OSGeo_v1.GDAL.Dataset _gDS = null;
        private RasterType _type = RasterType.image;
        private double _min = 0, _max = 0;
        private double _nodata = 0;
        private int _hasNoDataVal = 0;
        private ArgbColor _minColor = ArgbColor.Black;
        private ArgbColor _maxColor = ArgbColor.White;
        private double[] _hillShade = { 0.0, 1.0, 1.0 };
        private bool _useHillShade = true;
        private GridColorClass[] _colorClasses = null;

        public RasterClassV1()
        {
        }

        public RasterClassV1(IRasterDataset dataset, string filename)
            : this(dataset, filename, null)
        {
        }
        public RasterClassV1(IRasterDataset dataset, string filename, IPolygon polygon)
        {
            try
            {
                FileInfo fi = new FileInfo(filename);
                _title = fi.Name;
                _filename = filename;
                _dataset = dataset;

                if (!OSGeo.Initializer.RegisterAll())
                {
                    throw new Exception("Can't register GDAL");
                }

                _gDS = OSGeo_v1.GDAL.Gdal.Open(fi.FullName, 0);
                if (_gDS == null && _gDS.RasterCount == 0)
                {
                    _valid = false;
                    return;
                }

                _iWidth = _gDS.RasterXSize;
                _iHeight = _gDS.RasterYSize;

                switch (fi.Extension.ToLower())
                {
                    case ".adf":
                    case ".gsd":
                        _type = RasterType.grid;
                        break;
                        //case ".jp2":
                        //    _type = RasterType.wavelet;
                        //    break;
                }

                using (OSGeo_v1.GDAL.Band band = _gDS.GetRasterBand(1))
                {
                    if (_gDS.RasterCount == 1)
                    {
                        if (band.DataType != OSGeo_v1.GDAL.DataType.GDT_Byte)
                        {
                            _type = RasterType.grid;
                        }
                    }
                    band.GetMinimum(out _min, out _hasNoDataVal);
                    band.GetMaximum(out _max, out _hasNoDataVal);
                    band.GetNoDataValue(out _nodata, out _hasNoDataVal);
                }
                OSGeo_v1.GDAL.Driver driver = _gDS.GetDriver();

                double[] tfw = new double[6];
                _gDS.GetGeoTransform(tfw);

                string tfwFilename = fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length);
                switch (fi.Extension.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        tfwFilename += ".jgw";
                        break;
                    case ".jp2":
                        tfwFilename += ".j2w";
                        break;
                    case ".tif":
                    case ".tiff":
                        tfwFilename += ".tfw";
                        break;
                    case ".ecw":
                        tfwFilename += ".eww";
                        break;
                    default:
                        break;
                }

                FileInfo tfwInfo = new FileInfo(tfwFilename);

                _tfw = new TFWFile(tfw[0], tfw[3], tfw[1], tfw[2], tfw[4], tfw[5]);
                if (tfwInfo.Exists)
                {
                    _tfw.Filename = tfwFilename;
                }

                if (_tfw.X == 0.0 && _tfw.Y == 0.0 &&
                    Math.Abs(_tfw.dx_X) == 1.0 && _tfw.dx_Y == 0.0 &&
                    Math.Abs(_tfw.dy_Y) == 1.0 && _tfw.dy_X == 0.0 && driver != null)
                {
                    if (tfwInfo.Exists)
                    {
                        _tfw = new TFWFile(tfwFilename);
                    }
                    else
                    {
                        _tfw.isValid = false;
                    }
                }
                else
                {
                    // Bei dem Driver schein es nicht Pixelmitte sein, oder ist das bei GDAL generell
                    //if (driver.ShortName.ToLower() == "jp2openjpeg")  
                    {
                        _tfw.X += (_tfw.dx_X / 2.0D + _tfw.dx_Y / 2.0D);
                        _tfw.Y += (_tfw.dy_X / 2.0D + _tfw.dy_Y / 2.0D);
                    }
                }

                _sRef = fi.FileSpatialReference();

                if (polygon != null)
                {
                    _polygon = polygon;
                }
                else
                {
                    CalcPolygon();
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;

                Console.WriteLine($"GDAL Excepiton: {ex.Message}");
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    Console.WriteLine($"  Inner Exception: {ex.Message}");
                }

                _dataset.LastErrorMessage = ex.Message;
                _valid = false;
            }
        }

        public bool isValid { get { return _valid; } }
        internal RasterType Type
        {
            get { return _type; }
        }

        private void CalcPolygon()
        {
            _polygon = new Polygon();
            Ring ring = new Ring();
            gView.Framework.Geometry.Point p1 = new gView.Framework.Geometry.Point(
                _tfw.X - _tfw.dx_X / 2.0 - _tfw.dy_X / 2.0,
                _tfw.Y - _tfw.dx_Y / 2.0 - _tfw.dy_Y / 2.0);

            ring.AddPoint(p1);
            ring.AddPoint(new gView.Framework.Geometry.Point(p1.X + _tfw.dx_X * _iWidth, p1.Y + _tfw.dx_Y * _iWidth));
            ring.AddPoint(new gView.Framework.Geometry.Point(p1.X + _tfw.dx_X * _iWidth + _tfw.dy_X * _iHeight, p1.Y + _tfw.dx_Y * _iWidth + _tfw.dy_Y * _iHeight));
            ring.AddPoint(new gView.Framework.Geometry.Point(p1.X + _tfw.dy_X * _iHeight, p1.Y + _tfw.dy_Y * _iHeight));
            _polygon.AddRing(ring);
        }

        #region IRasterClass Member

        public gView.Framework.Geometry.IPolygon Polygon
        {
            get { return _polygon; }
        }

        public double oX { get { return _tfw.X; } }
        public double oY { get { return _tfw.Y; } }
        public double dx1 { get { return _tfw.dx_X; } }
        public double dx2 { get { return _tfw.dx_Y; } }
        public double dy1 { get { return _tfw.dy_X; } }
        public double dy2 { get { return _tfw.dy_Y; } }

        public gView.Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                return _sRef;
            }
            set
            {
                _sRef = value;
            }
        }

        public Task<IRasterPaintContext> BeginPaint(gView.Framework.Carto.IDisplay display, ICancelTracker cancelTracker)
        {
            try
            {
                if (!(_polygon is ITopologicalOperation))
                {
                    return Task.FromResult<IRasterPaintContext>(null);
                }

                TFWFile tfw = this.WorldFile as TFWFile;
                if (tfw == null)
                {
                    return Task.FromResult<IRasterPaintContext>(null);
                }

                IEnvelope dispEnvelope = display.DisplayTransformation.TransformedBounds(display); //display.Envelope;
                if (display.GeometricTransformer != null)
                {
                    dispEnvelope = ((IGeometry)display.GeometricTransformer.InvTransform2D(dispEnvelope)).Envelope;
                }

                IGeometry clipped;
                ((ITopologicalOperation)_polygon).Clip(dispEnvelope, out clipped);
                if (!(clipped is IPolygon))
                {
                    return Task.FromResult<IRasterPaintContext>(null);
                }

                IPolygon cPolygon = (IPolygon)clipped;

                // geclipptes Polygon transformieren -> Bild
                vector2[] vecs = new vector2[cPolygon[0].PointCount];
                for (int i = 0; i < cPolygon[0].PointCount; i++)
                {
                    vecs[i] = new vector2(cPolygon[0][i].X, cPolygon[0][i].Y);
                }
                if (!tfw.ProjectInv(vecs))
                {
                    return Task.FromResult<IRasterPaintContext>(null);
                }

                IEnvelope picEnv = vector2.IntegerEnvelope(vecs);
                picEnv.minx = Math.Max(0, picEnv.minx);
                picEnv.miny = Math.Max(0, picEnv.miny);
                picEnv.maxx = Math.Min(picEnv.maxx, _iWidth);
                picEnv.maxy = Math.Min(picEnv.maxy, _iHeight);

                // Ecken zurücktransformieren -> Welt
                vecs = new vector2[3];
                vecs[0] = new vector2(picEnv.minx, picEnv.miny);
                vecs[1] = new vector2(picEnv.maxx, picEnv.miny);
                vecs[2] = new vector2(picEnv.minx, picEnv.maxy);
                tfw.Project(vecs);
                var p1 = new gView.Framework.Geometry.Point(vecs[0].x, vecs[0].y);
                var p2 = new gView.Framework.Geometry.Point(vecs[1].x, vecs[1].y);
                var p3 = new gView.Framework.Geometry.Point(vecs[2].x, vecs[2].y);

                double pix = display.MapScale / (display.Dpi / 0.0254);  // [m]
                double c1 = Math.Sqrt(_tfw.dx_X * _tfw.dx_X + _tfw.dx_Y * _tfw.dx_Y);
                double c2 = Math.Sqrt(_tfw.dy_Y * _tfw.dy_Y + _tfw.dy_X * _tfw.dy_X);
                double mag = Math.Min(c1, c2) / pix;

                if (mag > 1.0)
                {
                    mag = 1.0;
                }

                int x = (int)(picEnv.minx);
                int y = (int)(picEnv.miny);
                int wWidth = (int)(picEnv.Width);
                int wHeight = (int)(picEnv.Height);

                //if (wWidth + x > _iWidth) wWidth = _iWidth - x;
                //if (wHeight + y > _iHeight) wHeight = _iHeight - y;

                int iWidth = (int)(wWidth * mag);
                int iHeight = (int)(wHeight * mag);

                RasterPaintContext2 rasterPaintContext = null;

                switch (_type)
                {
                    case RasterType.image:
                        rasterPaintContext = PaintImage(x, y, wWidth, wHeight, iWidth, iHeight, cancelTracker);
                        break;
                    case RasterType.wavelet:
                        rasterPaintContext = PaintWavelet(x, y, wWidth, wHeight, iWidth, iHeight, cancelTracker);
                        break;
                    case RasterType.grid:
                        if (_renderRawGridValues)
                        {
                            rasterPaintContext = PaintGrid(x, y, wWidth, wHeight, iWidth, iHeight);
                        }
                        else
                        {
                            rasterPaintContext = PaintHillShade(x, y, wWidth, wHeight, iWidth, iHeight, mag, cancelTracker);
                        }
                        break;
                }

                if (rasterPaintContext != null)
                {
                    rasterPaintContext.PicPoint1 = p1;
                    rasterPaintContext.PicPoint2 = p2;
                    rasterPaintContext.PicPoint3 = p3;
                }

                return Task.FromResult<IRasterPaintContext>(rasterPaintContext);
            }
            finally
            {

            }
        }

        #endregion

        private RasterPaintContext2 PaintImage(int x, int y, int wWidth, int wHeight, int iWidth, int iHeight, ICancelTracker cancelTracker)
        {
            if (CancelTracker.Canceled(cancelTracker) || _gDS == null)
            {
                return null;
            }

            int pixelSpace = 3;
            var bitmap = Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgb24);
            var bitmapData = bitmap.LockBitmapPixelData(BitmapLockMode.WriteOnly, GraphicsEngine.PixelFormat.Rgb24);

            try
            {
                int stride = bitmapData.Stride;
                IntPtr buf = bitmapData.Scan0;

                List<ArgbColor> colors = new List<ArgbColor>();
                for (int i = 1; i <= (_gDS.RasterCount > 3 ? 3 : _gDS.RasterCount); ++i)
                {
                    using (OSGeo_v1.GDAL.Band band = _gDS.GetRasterBand(i))
                    {

                        int ch = 0;
                        switch ((ColorInterp)band.GetRasterColorInterpretation())
                        {
                            case ColorInterp.BlueBand:
                                ch = 0;
                                break;
                            case ColorInterp.GreenBand:
                                ch = 1;
                                break;
                            case ColorInterp.RedBand:
                                ch = 2;
                                break;
                            case ColorInterp.GrayIndex:
                                for (int iColor = 0; iColor < 256; iColor++)
                                {
                                    colors.Add(ArgbColor.FromArgb(255, iColor, iColor, iColor));
                                }
                                break;
                            case ColorInterp.PaletteIndex:
                                OSGeo_v1.GDAL.ColorTable colTable = band.GetRasterColorTable();
                                if (colTable == null)
                                {
                                    break;
                                }

                                int colCount = colTable.GetCount();
                                for (int iColor = 0; iColor < colCount; iColor++)
                                {
                                    OSGeo_v1.GDAL.ColorEntry colEntry = colTable.GetColorEntry(iColor);
                                    colors.Add(ArgbColor.FromArgb(
                                        colEntry.c4, colEntry.c1, colEntry.c2, colEntry.c3));
                                }

                                break;
                        }
                        band.ReadRaster(x, y, wWidth, wHeight,
                            new IntPtr(buf.ToInt64() + ch),
                            iWidth, iHeight, OSGeo_v1.GDAL.DataType.GDT_Byte, pixelSpace, stride);
                    }
                }
                if (colors.Count > 0)
                {
                    unsafe
                    {
                        byte* ptr = (byte*)(bitmapData.Scan0);
                        for (int i = 0; i < bitmapData.Height; i++)
                        {
                            if (CancelTracker.Canceled(cancelTracker))
                            {
                                return null;
                            }

                            for (int j = 0; j < bitmapData.Width; j++)
                            {
                                // write the logic implementation here
                                byte c = ptr[0];
                                ArgbColor col = colors[c];
                                ptr[0] = col.B;
                                ptr[1] = col.G;
                                ptr[2] = col.R;
                                ptr += pixelSpace;
                            }
                            ptr += bitmapData.Stride - bitmapData.Width * pixelSpace;
                        }
                    }
                }

                return new RasterPaintContext2(bitmap);
            }
            catch (Exception ex)
            {
                if (bitmap != null && bitmapData != null)
                {
                    bitmap.UnlockBitmapPixelData(bitmapData);
                    bitmapData = null;
                    bitmap.Dispose();
                    bitmap = null;
                }

                throw ex;
            }
            finally
            {
                if (bitmap != null && bitmapData != null)
                {
                    bitmap.UnlockBitmapPixelData(bitmapData);
                }
            }
        }

        private RasterPaintContext2 PaintWavelet(int x, int y, int wWidth, int wHeight, int iWidth, int iHeight, ICancelTracker cancelTracker)
        {
            if (CancelTracker.Canceled(cancelTracker) || _gDS == null)
            {
                return null;
            }

            int pixelSpace = 4;
            var bitmap = Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgba32);
            var bitmapData = bitmap.LockBitmapPixelData(BitmapLockMode.ReadWrite, GraphicsEngine.PixelFormat.Rgba32);

            try
            {
                int stride = bitmapData.Stride;
                IntPtr buf = bitmapData.Scan0;

                for (int i = 1; i <= (_gDS.RasterCount > 3 ? 3 : _gDS.RasterCount); ++i)
                {
                    using (OSGeo_v1.GDAL.Band band = _gDS.GetRasterBand(i))
                    {

                        int ch = 0;
                        switch ((ColorInterp)band.GetRasterColorInterpretation())
                        {
                            case ColorInterp.BlueBand:
                                ch = 0;
                                break;
                            case ColorInterp.GreenBand:
                                ch = 1;
                                break;
                            case ColorInterp.RedBand:
                                ch = 2;
                                break;
                        }
                        band.ReadRaster(x, y, wWidth, wHeight,
                                new IntPtr(buf.ToInt64() + ch),
                                iWidth, iHeight, OSGeo_v1.GDAL.DataType.GDT_Byte, pixelSpace, stride);
                    }
                }

                return new RasterPaintContext2(bitmap);
            }
            catch (Exception ex)
            {
                if (bitmap != null && bitmapData != null)
                {
                    bitmap.UnlockBitmapPixelData(bitmapData);
                    bitmapData = null;
                    bitmap.Dispose();
                    bitmap = null;
                }

                throw ex;
            }
            finally
            {
                if (bitmap != null && bitmapData != null)
                {
                    bitmap.UnlockBitmapPixelData(bitmapData);
                }
            }
        }

        private RasterPaintContext2 PaintGrid(int x, int y, int wWidth, int wHeight, int iWidth, int iHeight)
        {
            if (_gDS == null)
            {
                return null;
            }

            int pixelSpace = 4;
            using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight + 100, GraphicsEngine.PixelFormat.Rgba32))
            {
                var bitmapData = bitmap.LockBitmapPixelData(GraphicsEngine.BitmapLockMode.ReadWrite, GraphicsEngine.PixelFormat.Rgba32);
                OSGeo_v1.GDAL.Band band = null;

                try
                {
                    int stride = bitmapData.Stride;
                    IntPtr buf = bitmapData.Scan0;

                    List<ArgbColor> colors = new List<ArgbColor>();
                    using (band = _gDS.GetRasterBand(1))
                    {
                        band.ReadRaster(x, y, wWidth, wHeight,
                            buf,
                            iWidth, iHeight, OSGeo_v1.GDAL.DataType.GDT_CFloat32, pixelSpace, stride);

                        band.Dispose();
                    }

                    unsafe
                    {
                        byte* ptr = (byte*)(bitmapData.Scan0);
                        float* v = (float*)(bitmapData.Scan0);

                        var floatNodata = (float)_nodata;
                        for (int i = 0; i < iHeight; i++)
                        {
                            for (int j = 0; j < iWidth; j++)
                            {
                                if (_renderRawGridValues)
                                {
                                    if (_hasNoDataVal == 1 && *v == floatNodata)
                                    {
                                        ptr[0] = ptr[1] = ptr[2] = ptr[3] = 0;
                                    }
                                    else
                                    {
                                        var int24Bytes = new Int24(*v * 100f).GetBytes();
                                        ptr[0] = int24Bytes[0];
                                        ptr[1] = int24Bytes[1];
                                        ptr[2] = int24Bytes[2];
                                        ptr[3] = 255;
                                    }
                                    //byte* vb = (byte*)v;
                                    //ptr[0] = *vb; vb++;
                                    //ptr[1] = *vb; vb++;
                                    //ptr[2] = *vb; vb++;
                                    //ptr[3] = *vb;  
                                }
                                else
                                {
                                    if (_hasNoDataVal == 1 && *v == floatNodata)
                                    {
                                        ptr[0] = ptr[1] = ptr[2] = ptr[3] = 0;
                                    }
                                    else
                                    {
                                        double c = (*v - _min) / (_max - _min);
                                        double a = _minColor.A + c * (_maxColor.A - _minColor.A);
                                        double r = _minColor.R + c * (_maxColor.R - _minColor.R);
                                        double g = _minColor.G + c * (_maxColor.G - _minColor.G);
                                        double b = _minColor.B + c * (_maxColor.B - _minColor.B);
                                        ptr[0] = (byte)b; ptr[1] = (byte)g; ptr[2] = (byte)r;
                                        ptr[3] = (byte)a; // alpha
                                    }
                                }
                                ptr += pixelSpace;
                                v++;
                            }
                            ptr += bitmapData.Stride - bitmapData.Width * pixelSpace;
                            v = (float*)ptr;
                        }
                    }

                    if (bitmap != null)
                    {
                        bitmap.UnlockBitmapPixelData(bitmapData);
                        bitmapData = null;
                    }

                    var contextBitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgba32);
                    using (var canvas = contextBitmap.CreateCanvas())
                    {
                        canvas.DrawBitmap(bitmap, new GraphicsEngine.CanvasPoint(0, 0));
                    }

                    return new RasterPaintContext2(contextBitmap);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (bitmap != null && bitmapData != null)
                    {
                        bitmap.UnlockBitmapPixelData(bitmapData);
                    }
                }
            }
        }

        private RasterPaintContext2 PaintHillShade(int x, int y, int wWidth, int wHeight, int iWidth, int iHeight, double mag, ICancelTracker cancelTracker)
        {
            if (CancelTracker.Canceled(cancelTracker) || _gDS == null)
            {
                return null;
            }

            int pixelSpace = 4;
            using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight + 100, GraphicsEngine.PixelFormat.Rgba32))
            {
                var bitmapData = bitmap.LockBitmapPixelData(GraphicsEngine.BitmapLockMode.ReadWrite, GraphicsEngine.PixelFormat.Rgba32);
                OSGeo_v1.GDAL.Band band = null;

                try
                {
                    int stride = bitmapData.Stride;
                    IntPtr buf = bitmapData.Scan0;

                    List<ArgbColor> colors = new List<ArgbColor>();
                    using (band = _gDS.GetRasterBand(1))
                    {

                        band.ReadRaster(x, y, wWidth, wHeight,
                            buf,
                            iWidth, iHeight, OSGeo_v1.GDAL.DataType.GDT_CFloat32, pixelSpace, stride);

                        band.Dispose();
                    }

                    double cx = _tfw.cellX / mag;
                    double cy = _tfw.cellY / mag;

                    Vector3d sun = new Vector3d(_hillShade);
                    sun.Normalize();
                    int rowStride = stride / pixelSpace;

                    ArgbColor col = ArgbColor.White;
                    unsafe
                    {
                        byte* ptr = (byte*)(bitmapData.Scan0);
                        float* v = (float*)(bitmapData.Scan0);

                        float floatNodata = (float)_nodata;

                        for (int i = 0; i < iHeight; i++)
                        {
                            if (CancelTracker.Canceled(cancelTracker))
                            {
                                return null;
                            }

                            for (int j = 0; j < iWidth; j++)
                            {
                                if ((_hasNoDataVal == 1 && *v == floatNodata) ||
                                    (_useIgnoreValue && *v == _ignoreValue))
                                {
                                    ptr[0] = ptr[1] = ptr[2] = ptr[3] = 0;
                                }
                                else
                                {
                                    double c = *v;

                                    col = GridColorClass.FindColor(c, _colorClasses);
                                    if (!_useHillShade)
                                    {
                                        ptr[0] = col.B; ptr[1] = col.G; ptr[2] = col.R;
                                        ptr[3] = col.A; // alpha
                                    }
                                    else
                                    {
                                        double c1 = (j < iWidth - 1) ? (*(v + 1)) : c;
                                        double c2 = (i < iHeight - 1) ? (*(v + rowStride)) : c;
                                        c1 = ((_hasNoDataVal != 0 && c1 == floatNodata) ||
                                              (_useIgnoreValue && c1 == _ignoreValue)) ? c : c1;
                                        c2 = ((_hasNoDataVal != 0 && c2 == floatNodata) ||
                                              (_useIgnoreValue && c2 == _ignoreValue)) ? c : c2;

                                        Vector3d v1 = new Vector3d(cx, 0.0, c1 - c); v1.Normalize();
                                        Vector3d v2 = new Vector3d(0.0, -cy, c2 - c); v2.Normalize();
                                        Vector3d vs = v2 % v1; vs.Normalize();
                                        double h = Math.Min(Math.Max(0.0, sun * vs), 1.0);
                                        //double h = Math.Abs(sun * vs);

                                        double a = col.A;
                                        double r = col.R * h;
                                        double g = col.G * h;
                                        double b = col.B * h;
                                        ptr[0] = (byte)b; ptr[1] = (byte)g; ptr[2] = (byte)r;
                                        ptr[3] = (byte)a; // alpha
                                    }
                                }
                                ptr += pixelSpace;
                                v++;
                            }
                            ptr += bitmapData.Stride - (bitmapData.Width) * pixelSpace;
                            v = (float*)ptr;
                        }
                    }
                    if (bitmap != null)
                    {
                        bitmap.UnlockBitmapPixelData(bitmapData);
                        bitmapData = null;
                    }

                    var contextBitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgba32);
                    using (var gr = contextBitmap.CreateCanvas())
                    {
                        gr.DrawBitmap(bitmap, new GraphicsEngine.CanvasPoint(0, 0));
                    }

                    return new RasterPaintContext2(contextBitmap);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (bitmap != null && bitmapData != null)
                    {
                        bitmap.UnlockBitmapPixelData(bitmapData);
                    }
                }
            }
        }

        #region IClass Member

        public string Name
        {
            get { return _title; }
        }

        public string Aliasname
        {
            get { return _title; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion

        #region IRasterFile Member

        public string Filename
        {
            get { return _filename; }
        }

        public IRasterWorldFile WorldFile
        {
            get { return _tfw; }
        }

        #endregion

        #region IBitmap Member

        public GraphicsEngine.Abstraction.IBitmap LoadBitmap()
        {
            return null;
        }

        #endregion

        #region IPointIdentify Member

        public IPointIdentifyContext CreatePointIdentifyContext() => new DummyPointIdentifyContext();

        public Task<ICursor> PointQuery(gView.Framework.Carto.IDisplay display, IPoint point, ISpatialReference sRef, IUserData userdata, IPointIdentifyContext context)
        {
            TFWFile tfw = this.WorldFile as TFWFile;
            if (tfw == null)
            {
                return Task.FromResult<ICursor>(null);
            }

            if (this.SpatialReference != null && sRef != null &&
                !sRef.Equals(this.SpatialReference))
            {
                point = GeometricTransformerFactory.Transform2D(point, sRef, this.SpatialReference) as IPoint;
            }
            if (point == null)
            {
                return Task.FromResult<ICursor>(null); ;
            }

            // Punkt transformieren -> Bild
            vector2[] vecs = new vector2[1];
            vecs[0] = new vector2(point.X, point.Y);

            if (!tfw.ProjectInv(vecs))
            {
                return Task.FromResult<ICursor>(null); ;
            }

            if (vecs[0].x < 0 || vecs[0].x >= _iWidth ||
                vecs[0].y < 0 || vecs[0].y >= _iHeight)
            {
                return Task.FromResult<ICursor>(null);
            }

            switch (_type)
            {
                case RasterType.image:
                    return Task.FromResult<ICursor>(QueryImage((int)Math.Floor(vecs[0].x), (int)Math.Floor(vecs[0].y)));
                case RasterType.grid:
                    return Task.FromResult<ICursor>(QueryGrid((int)Math.Floor(vecs[0].x), (int)Math.Floor(vecs[0].y)));
            }

            return Task.FromResult<ICursor>(null); ;
        }

        #endregion

        #region IGridIdentify Member

        private OSGeo_v1.GDAL.Band _gridQueryBand = null;
        public void InitGridQuery()
        {
            if (_gridQueryBand == null)
            {
                _gridQueryBand = _gDS.GetRasterBand(1);
            }
        }

        public float GridQuery(gView.Framework.Carto.IDisplay display, IPoint point, ISpatialReference sRef)
        {
            float floatNodata = (float)_nodata;

            TFWFile tfw = this.WorldFile as TFWFile;
            if (tfw == null)
            {
                return floatNodata;
            }

            if (this.SpatialReference != null && sRef != null &&
                !sRef.Equals(this.SpatialReference))
            {
                point = GeometricTransformerFactory.Transform2D(point, sRef, this.SpatialReference) as IPoint;
            }
            if (point == null)
            {
                return floatNodata;
            }

            // Punkt transformieren -> Bild
            vector2[] vecs = new vector2[1];
            vecs[0] = new vector2(point.X, point.Y);

            if (!tfw.ProjectInv(vecs))
            {
                return floatNodata;
            }

            if (vecs[0].x < 0 || vecs[0].x >= _iWidth ||
                vecs[0].y < 0 || vecs[0].y >= _iHeight)
            {
                return floatNodata;
            }

            unsafe
            {

                fixed (float* buf = new float[2])
                {
                    _gridQueryBand.ReadRaster((int)vecs[0].x, (int)vecs[0].y, 1, 1,
                        (IntPtr)buf,
                        1, 1, OSGeo_v1.GDAL.DataType.GDT_CFloat32, 4, 0);

                    if ((_hasNoDataVal != 0 && buf[0] == floatNodata) ||
                        (_useIgnoreValue && buf[0] == _ignoreValue))
                    {
                        return floatNodata;
                    }

                    return buf[0];
                }
            }
        }

        public void ReleaseGridQuery()
        {
            if (_gridQueryBand != null)
            {
                _gridQueryBand.Dispose();
                _gridQueryBand = null;
            }
        }

        #endregion

        private ICursor QueryImage(int x, int y)
        {
            unsafe
            {
                int bandCount = _gDS.RasterCount;
                string[] tags = new string[bandCount + 2];
                object[] values = new object[bandCount + 2];
                List<ArgbColor> colors = new List<ArgbColor>();

                for (int i = 1; i <= bandCount; ++i)
                {
                    OSGeo_v1.GDAL.Band band = _gDS.GetRasterBand(i);

                    string bandName = "";
                    switch ((ColorInterp)band.GetRasterColorInterpretation())
                    {
                        case ColorInterp.BlueBand:
                            bandName = "(blue)";
                            break;
                        case ColorInterp.GreenBand:
                            bandName = "(green)";
                            break;
                        case ColorInterp.RedBand:
                            bandName = "(red)";
                            break;
                        case ColorInterp.GrayIndex:
                            for (int iColor = 0; iColor < 256; iColor++)
                            {
                                colors.Add(ArgbColor.FromArgb(255, iColor, iColor, iColor));
                            }
                            break;
                        case ColorInterp.PaletteIndex:
                            tags = new string[tags.Length + 4];
                            values = new object[values.Length + 4];

                            OSGeo_v1.GDAL.ColorTable colTable = band.GetRasterColorTable();
                            if (colTable == null)
                            {
                                break;
                            }

                            int colCount = colTable.GetCount();
                            for (int iColor = 0; iColor < colCount; iColor++)
                            {
                                OSGeo_v1.GDAL.ColorEntry colEntry = colTable.GetColorEntry(iColor);
                                colors.Add(ArgbColor.FromArgb(
                                    colEntry.c4, colEntry.c1, colEntry.c2, colEntry.c3));
                            }

                            break;
                    }

                    int c = 0;

                    int* buf = &c;

                    band.ReadRaster(x, y, 1, 1,
                        (IntPtr)buf,
                        1, 1, OSGeo_v1.GDAL.DataType.GDT_Int32, 4, 0);

                    band.Dispose();

                    tags[i + 1] = "Band " + i.ToString() + " " + bandName;
                    values[i + 1] = c;

                    if (colors.Count > 0 && c >= 0 && c < colors.Count)
                    {
                        ArgbColor col = colors[c];
                        tags[i + 2] = "Alpha";
                        values[i + 2] = col.A;
                        tags[i + 3] = "Red";
                        values[i + 3] = col.R;
                        tags[i + 4] = "Green";
                        values[i + 4] = col.G;
                        tags[i + 5] = "Blue";
                        values[i + 5] = col.B;
                    }
                }

                tags[0] = "ImageX"; values[0] = x;
                tags[1] = "ImageY"; values[1] = y;

                return new QueryCursor(tags, values);
            }
        }

        private ICursor QueryGrid(int x, int y)
        {
            try
            {
                unsafe
                {
                    var floatNodata = (float)_nodata;

                    fixed (float* buf = new float[2])
                    {
                        OSGeo_v1.GDAL.Band band = _gDS.GetRasterBand(1);

                        band.ReadRaster(x, y, 1, 1,
                            (IntPtr)buf,
                            1, 1, OSGeo_v1.GDAL.DataType.GDT_CFloat32, 4, 0);

                        if ((_hasNoDataVal != 0 && buf[0] == floatNodata) ||
                            (_useIgnoreValue && buf[0] == _ignoreValue))
                        {
                            return null;
                        }

                        string[] tags = { "ImageX", "ImageY", "band1" };
                        object[] values = { x, y, buf[0] };

                        band.Dispose();

                        return new QueryCursor(tags, values);
                    }
                }
            }
            finally
            {
            }
        }

        private class QueryCursor : IRowCursor
        {
            private int _pos = 0;
            private string[] _tag;
            private object[] _values;

            public QueryCursor(string[] tag, object[] values)
            {
                _tag = tag;
                _values = values;
            }

            #region IDisposable Member

            public void Dispose()
            {

            }

            #endregion

            #region IRowCursor Member

            public Task<IRow> NextRow()
            {
                if (_pos > 0)
                {
                    return Task.FromResult<IRow>(null);
                }

                Row row = new Row();
                row.OID = 1;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _tag.Length; i++)
                {
                    if (_tag[i] == "ImageX" || _tag[i] == "ImageY")
                    {
                        continue;
                    }

                    if (_values[_pos] == null)
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(_values[i].ToString());
                }
                row.Fields.Add(new FieldValue("Bands", "(" + sb.ToString() + ")"));

                for (int i = 0; i < _tag.Length; i++)
                {
                    if (_tag[_pos] == null)
                    {
                        continue;
                    }

                    row.Fields.Add(new FieldValue(_tag[i], _values[i]));
                }
                _pos++;

                return Task.FromResult<IRow>(row);
            }

            #endregion
        }

        #region IGridClass Member
        private double _ignoreValue = 0.0;
        private bool _useIgnoreValue = false;

        public GridRenderMethode ImplementsRenderMethods
        {
            get
            {
                if (_type == RasterType.image)
                {
                    return GridRenderMethode.None;
                }
                else
                {
                    return GridRenderMethode.Colors | GridRenderMethode.HillShade | GridRenderMethode.NullValue;
                }
            }
        }

        public GridColorClass[] ColorClasses
        {
            get
            {
                if (_colorClasses == null)
                {
                    return null;
                }

                GridColorClass[] copy = new GridColorClass[_colorClasses.Length];
                _colorClasses.CopyTo(copy, 0);

                return copy;
            }
            set
            {
                if (value == null)
                {
                    _colorClasses = null;
                }
                else
                {
                    _colorClasses = new GridColorClass[value.Length];
                    value.CopyTo(_colorClasses, 0);
                }
            }
        }

        public bool UseHillShade
        {
            get { return _useHillShade; }
            set { _useHillShade = value; }
        }
        public double[] HillShadeVector
        {
            get
            {
                return new Vector3d(_hillShade).ToArray();
            }
            set
            {
                if (value == null || value.Length != 3)
                {
                    return;
                }

                _hillShade[0] = value[0];
                _hillShade[1] = value[1];
                _hillShade[2] = value[2];
            }
        }

        public double MinValue
        {
            get { return _min; }
        }
        public double MaxValue
        {
            get { return _max; }
        }

        public double IgnoreDataValue
        {
            get
            {
                return _ignoreValue;
            }
            set
            {
                _ignoreValue = value;
            }
        }
        public bool UseIgnoreDataValue
        {
            get
            {
                return _useIgnoreValue;
            }
            set
            {
                _useIgnoreValue = value;
            }
        }

        bool _renderRawGridValues = false;
        public bool RenderRawGridValues
        {
            get { return _renderRawGridValues; }
            set { _renderRawGridValues = value; }
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            if (_type == RasterType.grid)
            {
                _colorClasses = null;
                List<GridColorClass> classes = new List<GridColorClass>();
                GridColorClass cc;
                while ((cc = (GridColorClass)stream.Load("GridClass", null, new GridColorClass(0, 0, ArgbColor.White))) != null)
                {
                    classes.Add(cc);
                }
                if (classes.Count > 0)
                {
                    _colorClasses = classes.ToArray();
                }

                _useHillShade = (bool)stream.Load("UseHillShade", true);
                _hillShade[0] = (double)stream.Load("HillShadeDx", 0.0);
                _hillShade[1] = (double)stream.Load("HillShadeDy", 0.0);
                _hillShade[2] = (double)stream.Load("HillShadeDz", 0.0);
                _useIgnoreValue = (bool)stream.Load("UseIgnoreData", 0);
                _ignoreValue = (double)stream.Load("IgnoreData", 0.0);
                _renderRawGridValues = (bool)stream.Load("RenderRawGridValues", false);
            }
        }

        public void Save(IPersistStream stream)
        {
            if (_type == RasterType.grid)
            {
                if (_colorClasses != null)
                {
                    foreach (GridColorClass cc in _colorClasses)
                    {
                        stream.Save("GridClass", cc);
                    }
                }

                stream.Save("UseHillShade", _useHillShade);
                stream.Save("HillShadeDx", _hillShade[0]);
                stream.Save("HillShadeDy", _hillShade[1]);
                stream.Save("HillShadeDz", _hillShade[2]);
                stream.Save("UseIgnoreData", _useIgnoreValue);
                stream.Save("IgnoreData", _ignoreValue);
                stream.Save("RenderRawGridValues", _renderRawGridValues);
            }
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (_gDS != null)
            {
                _gDS.Dispose();
                _gDS = null;
            }
        }

        #endregion
    }
}
