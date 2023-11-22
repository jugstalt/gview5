using gView.GraphicsEngine.Abstraction;
using gView.GraphicsEngine.GdiPlus.Extensions;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace gView.GraphicsEngine.GdiPlus
{
    internal class GdiBitmap : IBitmap
    {
        private Bitmap _bitmap;
        private int _bytesLockersCount = 0;

        #region Constructors

        private GdiBitmap(Bitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public GdiBitmap(int width, int height)
        {
            _bitmap = new Bitmap(width, height);
        }

        public GdiBitmap(int width, int height, PixelFormat format)
        {
            _bitmap = new Bitmap(width, height, format.ToGdiPixelFormat());
            if (format == PixelFormat.Gray8)
            {
                _bitmap.SetGrayscalePalette();
            }
        }

        public GdiBitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
        {
            _bitmap = new Bitmap(width, height, stride, format.ToGdiPixelFormat(), scan0);
        }

        public GdiBitmap(Stream stream)
        {
            _bitmap = (Bitmap)Bitmap.FromStream(stream);
        }

        public GdiBitmap(string filename)
        {
            _bitmap = (Bitmap)Bitmap.FromFile(filename);
        }

        #endregion Constructors

        #region IBitmap

        public int Width => _bitmap != null ? _bitmap.Width : 0;
        public int Height => _bitmap != null ? _bitmap.Height : 0;

        public float DpiX => _bitmap != null ? _bitmap.HorizontalResolution : 0f;
        public float DpiY => _bitmap != null ? _bitmap.VerticalResolution : 0f;

        public void SetResolution(float dpiX, float dpiY)
        {
            if (_bitmap != null)
            {
                _bitmap.SetResolution(dpiX, DpiY);
            }
        }

        public PixelFormat PixelFormat => _bitmap != null ? _bitmap.PixelFormat.ToPixelFormat() : PixelFormat.Rgba32;

        public object EngineElement => _bitmap;

        public ICanvas CreateCanvas()
        {
            return new GdiCanvas(_bitmap);
        }

        public void MakeTransparent()
        {
            if (_bitmap != null)
            {
                _bitmap.MakeTransparent();
            }
        }

        public void MakeTransparent(ArgbColor color)
        {
            if (_bitmap != null)
            {
                _bitmap.MakeTransparent(color.ToGdiColor());
            }
        }

        public IBitmap Clone(PixelFormat format) =>
            _bitmap == null ?
                null :
                new GdiBitmap(_bitmap.Clone(new System.Drawing.Rectangle(0, 0, _bitmap.Width, _bitmap.Height), format.ToGdiPixelFormat()));

        public void Save(string filename, ImageFormat format, int quality = 0)
        {
            if (Current.Encoder != null && Current.Encoder.CanEncode(this))
            {
                Current.Encoder.Encode(this, filename, format, quality);
            }

            new GdiBitmapEncoding().Encode(this, filename, format, quality);
        }

        public void Save(Stream stream, ImageFormat format, int quality = 0)
        {
            if (Current.Encoder != null && Current.Encoder.CanEncode(this))
            {
                Current.Encoder.Encode(this, stream, format, quality);
            }

            new GdiBitmapEncoding().Encode(this, stream, format, quality);
        }
        public BitmapPixelData LockBitmapPixelData(BitmapLockMode lockMode, PixelFormat pixelFormat)
        {
            _bytesLockersCount++;
            try
            {
                var bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                 lockMode.ToGidImageLockMode(),
                                                 pixelFormat.ToGdiPixelFormat());



                return new GdiBitmapPixelData(bitmapData, lockMode);
            }
            catch
            {
                _bytesLockersCount--;
                throw;
            }
        }

        public void UnlockBitmapPixelData(BitmapPixelData bitmapPixelData)
        {
            try
            {
                if (bitmapPixelData is GdiBitmapPixelData)
                {
                    _bitmap.UnlockBits(((GdiBitmapPixelData)bitmapPixelData).BitmapData);
                }
            }
            finally
            {
                _bytesLockersCount--;
            }
        }

        public ArgbColor GetPixel(int x, int y)
        {
            return _bitmap.GetPixel(x, y).ToArgbColor();
        }

        #endregion IBitmap

        #region IDisposable

        public void Dispose()
        {
            if (_bitmap != null)
            {
                if (Current.UseSecureDisposingOnUserInteractiveUIs)
                {
                    //Task.Run(async () =>
                    //{
                    //    await DisposeSecure();
                    //});
                    DisposeSecure().Wait(5000);
                }
                else
                {
                    _bitmap.Dispose();
                    _bitmap = null;
                }
            }
        }

        async private Task DisposeSecure()
        {
            var currentBitmap = _bitmap;
            _bitmap = null;

            if (currentBitmap is null)
            {
                return;
            }

            if (_bytesLockersCount > 0)
            {
                var startTime = DateTime.Now;
                while (_bytesLockersCount > 0)
                {
                    await Task.Delay(10);
                    if ((DateTime.Now - startTime).TotalSeconds > 5)
                    {
                        return; // let make GC do the collection later
                    }
                }
            }

            currentBitmap.Dispose();
        }

        #endregion IDisposable

        #region Classes

        public class GdiBitmapPixelData : BitmapPixelData
        {
            private readonly System.Drawing.Imaging.BitmapData _bitmapData;
            public GdiBitmapPixelData(System.Drawing.Imaging.BitmapData bitmapData, BitmapLockMode lockMode)
               : base(lockMode)
            {
                _bitmapData = bitmapData;

                this.PixelFormat = bitmapData.PixelFormat.ToPixelFormat();
                this.Reserved = bitmapData.Reserved;
                this.Stride = bitmapData.Stride;
                this.Scan0 = bitmapData.Scan0;
                this.Width = bitmapData.Width;
                this.Height = bitmapData.Height;
            }

            internal System.Drawing.Imaging.BitmapData BitmapData => _bitmapData;
        }

        #endregion
    }
}