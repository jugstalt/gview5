using gView.Framework.Carto.Graphics;
using gView.Framework.Data;
using System;
using System.Collections;
using System.Collections.Generic;

namespace gView.Framework.Carto
{
    /// <summary>
    /// Zusammenfassung für ImageMerger.
    /// </summary>
    public class ImageMerger
    {
        protected ArrayList m_picList, m_orderList;
        protected int m_max = 0;
        protected string m_outputUrl = "";
        protected string m_outputPath = "";
        protected double m_scale = -1.0, m_dpi = 96.0;

        public ImageMerger()
        {
            m_picList = new ArrayList();
            m_orderList = new ArrayList();
        }

        public void clear()
        {
            m_picList.Clear();
            m_orderList.Clear();
            m_max = 0;
            m_scale = -1.0;
        }

        public int Count
        {
            get
            {
                return m_picList.Count;
            }
        }

        public double mapScale
        {
            set { m_scale = value; }
        }

        public double dpi
        {
            set { m_dpi = value; }
        }

        public string outputPath
        {
            set { m_outputPath = value; }
            get { return m_outputPath; }
        }

        public string outputUrl
        {
            set { m_outputUrl = value; }
            get { return m_outputPath; }
        }

        public void Add(string path, int order)
        {
            for (int i = 0; i < m_picList.Count; i++)
            {
                int o = Convert.ToInt32(m_orderList[i]);
                if (order < o)
                {
                    m_picList.Insert(i, path);
                    m_orderList.Insert(i, order);
                    return;
                }
            }

            m_picList.Add(path);
            m_orderList.Add(order);
        }

        public int max
        {
            get { return m_max; }
            set { m_max = value; }
        }

        public string Merge(int iWidth, int iHeight, out string imageUrl)
        {
            imageUrl = "";

            try
            {
                DateTime time = DateTime.Now;
                string filename = "merged_" +
                    time.Day.ToString() + time.Hour.ToString() +
                    time.Second.ToString() + time.Millisecond.ToString() + ".png";

                using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(iWidth, iHeight, GraphicsEngine.PixelFormat.Rgba32))
                using (var canvas = bitmap.CreateCanvas())
                using (var brush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.White))
                {
                    canvas.FillRectangle(brush, new GraphicsEngine.CanvasRectangle(0, 0, iWidth, iHeight));

                    foreach (string pic in m_picList)
                    {
                        using (var img = GraphicsEngine.Current.Engine.CreateBitmap(pic))
                        {
                            canvas.DrawBitmap(img,
                                new GraphicsEngine.CanvasRectangle(0, 0, iWidth, iHeight),
                                new GraphicsEngine.CanvasRectangle(0, 0, img.Width, img.Height));
                        }
                    }

                    if (m_scale > 0)
                    {
                        Scalebar bar = new Scalebar(m_scale, m_dpi);
                        bar.Create(bitmap, bitmap.Width - (int)(50 * m_dpi / 96.0) - bar.ScaleBarWidth, bitmap.Height - (int)(32 * m_dpi / 96.0));
                    }
                    imageUrl = m_outputUrl + "/" + filename;
                    filename = m_outputPath + @"/" + filename;

                    canvas.Flush();
                    bitmap.Save(filename, GraphicsEngine.ImageFormat.Png);
                    return filename;
                }
            }
            catch { return ""; }
        }
    }

    public class ImageMerger2 : IDisposable
    {
        protected List<GeorefBitmap> _picList;
        protected List<int> _orderList;
        protected int _max = 0;
        protected double _scale = -1.0, m_dpi = 96.0;

        private string _errMsg = String.Empty;
        private object lockThis = new object();

        public ImageMerger2()
        {
            _picList = new List<GeorefBitmap>();
            _orderList = new List<int>();
        }

        public void Dispose()
        {
            this.Clear();
        }

        public void Clear()
        {
            foreach (var georefBitmap in _picList)
            {
                georefBitmap?.Dispose();
            }

            _picList.Clear();
            _orderList.Clear();
            _max = 0;
            _scale = -1.0;
        }

        public int Count
        {
            get
            {
                return _picList.Count;
            }
        }

        public double mapScale
        {
            set { _scale = value; }
        }

        public double dpi
        {
            set { m_dpi = value; }
        }

        public void Add(GeorefBitmap image, int order)
        {
            lock (lockThis)
            {
                for (int i = 0; i < _picList.Count; i++)
                {
                    int o = Convert.ToInt32(_orderList[i]);
                    if (order < o)
                    {
                        _picList.Insert(i, image);
                        _orderList.Insert(i, order);
                        return;
                    }
                }

                _picList.Add(image);
                _orderList.Add(order);
            }
        }

        public int max
        {
            get { return _max; }
            set { _max = value; }
        }

        public string LastErrorMessage
        {
            get { return _errMsg; }
        }

        public bool Merge(GraphicsEngine.Abstraction.IBitmap bitmap, IDisplay display)
        {
            try
            {
                int iWidth = bitmap.Width;
                int iHeight = bitmap.Height;

                using (var canvas = bitmap.CreateCanvas())
                {
                    canvas.CompositingMode = GraphicsEngine.CompositingMode.SourceOver;

                    foreach (GeorefBitmap geoBmp in _picList)
                    {
                        if (geoBmp == null || geoBmp.Bitmap == null)
                        {
                            continue;
                        }

                        if (bitmap != geoBmp.Bitmap)
                        {
                            if (geoBmp.Envelope != null)
                            {
                                double x0, y0, x1, y1, x2, y2;
                                gView.Framework.Geometry.IGeometry geom = gView.Framework.Geometry.GeometricTransformerFactory.Transform2D(geoBmp.Envelope, geoBmp.SpatialReference, display.SpatialReference);
                                if (geom is gView.Framework.Geometry.IPolygon)
                                {
                                    gView.Framework.Geometry.IRing ring = ((gView.Framework.Geometry.IPolygon)geom)[0];

                                    x0 = ring[1].X; y0 = ring[1].Y;
                                    x1 = ring[2].X; y1 = ring[2].Y;
                                    x2 = ring[0].X; y2 = ring[0].Y;

                                    /////////////////////////////////////////////////////////
                                    Display d = new Display(display.Map, false);
                                    d.Limit = d.Envelope = geoBmp.Envelope;
                                    d.ImageWidth = geoBmp.Bitmap.Width;
                                    d.ImageHeight = geoBmp.Bitmap.Height;
                                    d.SpatialReference = geoBmp.SpatialReference;
                                    Resample(bitmap, display, geoBmp.Bitmap, d);
                                    continue;
                                }
                                else
                                {
                                    x0 = geoBmp.Envelope.minx; y0 = geoBmp.Envelope.maxy;
                                    x1 = geoBmp.Envelope.maxx; y1 = geoBmp.Envelope.maxy;
                                    x2 = geoBmp.Envelope.minx; y2 = geoBmp.Envelope.miny;
                                }

                                display.World2Image(ref x0, ref y0);
                                display.World2Image(ref x1, ref y1);
                                display.World2Image(ref x2, ref y2);

                                GraphicsEngine.CanvasPointF[] points ={
                                    new GraphicsEngine.CanvasPointF((float)x0,(float)y0),
                                    new GraphicsEngine.CanvasPointF((float)x1,(float)y1),
                                    new GraphicsEngine.CanvasPointF((float)x2,(float)y2)
                                };

                                canvas.DrawBitmap(geoBmp.Bitmap,
                                    points,
                                    new GraphicsEngine.CanvasRectangleF(0, 0, geoBmp.Bitmap.Width, geoBmp.Bitmap.Height),
                                    opacity: geoBmp.Opacity);
                            }
                            else
                            {
                                canvas.DrawBitmap(geoBmp.Bitmap,
                                    new GraphicsEngine.CanvasRectangle(0, 0, iWidth, iHeight),
                                    new GraphicsEngine.CanvasRectangle(0, 0, geoBmp.Bitmap.Width, geoBmp.Bitmap.Height),
                                    opacity: geoBmp.Opacity);
                            }
                        }
                    }
                }

                if (_scale > 0)
                {
                    Scalebar bar = new Scalebar(_scale, m_dpi);
                    bar.Create(bitmap, bitmap.Width - (int)(50 * m_dpi / 96.0) - bar.ScaleBarWidth, bitmap.Height - (int)(32 * m_dpi / 96.0));
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }

        private GraphicsEngine.PixelFormat NonIndedexedPixelFormat(GeorefBitmap bm)
        {
            if (bm == null || bm.Bitmap == null)
            {
                return GraphicsEngine.PixelFormat.Rgba32;
            }

            switch (bm.Bitmap.PixelFormat)
            {
                case GraphicsEngine.PixelFormat.Gray8:
                    return GraphicsEngine.PixelFormat.Rgba32;
            }

            return bm.Bitmap.PixelFormat;
        }

        public GraphicsEngine.Abstraction.IBitmap Merge()
        {
            GraphicsEngine.Abstraction.ICanvas gr = null;
            GraphicsEngine.Abstraction.IBitmap ret = null;
            try
            {
                foreach (GeorefBitmap geoBmp in _picList)
                {
                    if (geoBmp == null || geoBmp.Bitmap == null)
                    {
                        continue;
                    }

                    if (gr == null)
                    {
                        ret = geoBmp.Bitmap;
                        gr = geoBmp.Bitmap.CreateCanvas();
                    }
                    else
                    {
                        gr.DrawBitmap(geoBmp.Bitmap,
                            new GraphicsEngine.CanvasRectangle(0, 0, ret.Width, ret.Height),
                            new GraphicsEngine.CanvasRectangle(0, 0, geoBmp.Bitmap.Width, geoBmp.Bitmap.Height));
                        geoBmp.Dispose();
                    }
                }

                this.Clear();
                return ret;
            }
            catch
            {
                return ret;
            }
            finally
            {
                if (gr != null)
                {
                    gr.Dispose();
                }
            }
        }

        public static void Resample(GraphicsEngine.Abstraction.IBitmap dest,
                                    IDisplay destDisplay,
                                    GraphicsEngine.Abstraction.IBitmap source,
                                    IDisplay sourceDisplay)
        {
            GraphicsEngine.BitmapPixelData destData = null, sourceData = null;
            using (var transformer = gView.Framework.Geometry.GeometricTransformerFactory.Create())
            {
                try
                {
                    transformer.SetSpatialReferences(sourceDisplay.SpatialReference, destDisplay.SpatialReference);

                    destData = dest.LockBitmapPixelData(GraphicsEngine.BitmapLockMode.WriteOnly, GraphicsEngine.PixelFormat.Rgba32);
                    sourceData = source.LockBitmapPixelData(GraphicsEngine.BitmapLockMode.ReadOnly, GraphicsEngine.PixelFormat.Rgba32);

                    int sWidth = source.Width, sHeight = source.Height;
                    int dWidth = dest.Width, dHeight = dest.Height;

                    unsafe
                    {
                        byte* ptr = (byte*)destData.Scan0;

                        for (int y = 0; y < dHeight; y++)
                        {
                            for (int x = 0; x < dWidth; x++)
                            {
                                double xx = x, yy = y;
                                destDisplay.Image2World(ref xx, ref yy);
                                gView.Framework.Geometry.IPoint point = (gView.Framework.Geometry.IPoint)transformer.InvTransform2D(new gView.Framework.Geometry.Point(xx, yy));
                                xx = point.X; yy = point.Y;
                                sourceDisplay.World2Image(ref xx, ref yy);

                                int x_ = (int)xx, y_ = (int)yy;
                                if (x_ >= 0 && x_ < sWidth &&
                                    y_ >= 0 && y_ < sHeight)
                                {
                                    byte* p = (byte*)sourceData.Scan0;
                                    p += (y_ * destData.Stride + x_ * 4);

                                    if (p[3] != 0) // Transparenz!!!
                                    {
                                        ptr[0] = p[0];
                                        ptr[1] = p[1];
                                        ptr[2] = p[2];
                                        ptr[3] = p[3];
                                    }
                                }

                                ptr += 4;
                            }
                            ptr += destData.Stride - destData.Width * 4;
                        }
                    }
                }
                catch { }
                finally
                {
                    if (destData != null)
                    {
                        dest.UnlockBitmapPixelData(destData);
                    }

                    if (sourceData != null)
                    {
                        source.UnlockBitmapPixelData(sourceData);
                    }
                }
            }
        }
    }
}