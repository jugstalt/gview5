using gView.GraphicsEngine.Abstraction;
using gView.GraphicsEngine.Extensions;
using gView.GraphicsEngine.Skia.Extensions;
using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace gView.GraphicsEngine.Skia
{
    internal class SkiaBitmap : IBitmap
    {
        private SKBitmap _bitmap;
        private float _dpiX = 96f, _dpyY = 96f;
        private int _bytesLockersCount = 0;

        private SkiaBitmap(SKBitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public SkiaBitmap(int width, int height)
        {
            _bitmap = new SKBitmap(width, height, true);
        }

        public SkiaBitmap(int width, int height, PixelFormat format)
        {
            _bitmap = new SKBitmap(width, height, colorType: format.ToSKColorType(), alphaType: SKAlphaType.Premul);
        }

        public SkiaBitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
        {
            _bitmap = new SKBitmap(width, height, colorType: format.ToSKColorType(), alphaType: SKAlphaType.Premul);
            _bitmap.SetPixels(scan0);
        }

        public SkiaBitmap(Stream stream)
        {
            _bitmap = SKBitmap.Decode(stream);
        }

        public SkiaBitmap(string filename)
        {
            _bitmap = SKBitmap.Decode(filename);
        }

        public int Width => _bitmap != null ? _bitmap.Width : 0;

        public int Height => _bitmap != null ? _bitmap.Height : 0;

        public float DpiX => _dpiX;

        public float DpiY => _dpyY;

        public PixelFormat PixelFormat
        {
            get
            {
                return _bitmap.ColorType.ToPixelFormat();
            }
        }

        public object EngineElement => _bitmap;

        public IBitmap Clone(PixelFormat format)
        {
            if (_bitmap != null)
            {
                return new SkiaBitmap(_bitmap.Copy(format.ToSKColorType()));
            }

            return null;
        }

        public ICanvas CreateCanvas()
        {
            if (_bitmap != null)
            {
                return new SkiaCanvas(new SKCanvas(_bitmap));
            }
            return null;
        }

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
                    if((DateTime.Now - startTime).TotalSeconds > 5)
                    {
                        return; // let make GC do the collection later
                    }
                }
            }

            currentBitmap.Dispose();
        }

        public ArgbColor GetPixel(int x, int y)
        {
            return _bitmap?.GetPixel(x, y).ToArgbColor() ?? ArgbColor.Empty;
        }

        public BitmapPixelData LockBitmapPixelData(BitmapLockMode lockMode, PixelFormat pixelFormat)
        {
            if (_bitmap != null)
            {
                var pixelData = new SkiaBitmapPixelData(lockMode)
                {
                    Width = _bitmap.Width,
                    Height = _bitmap.Height,
                    Stride = _bitmap.RowBytes,
                    PixelFormat = pixelFormat
                };

                if (lockMode == BitmapLockMode.Copy)
                {
                    pixelData.FreeMemory = true;
                    pixelData.Scan0 = Marshal.AllocHGlobal(_bitmap.ByteCount);
                    Marshal.Copy(_bitmap.Bytes, 0, pixelData.Scan0, _bitmap.ByteCount);
                }
                else if (pixelFormat == PixelFormat.Rgb24)
                {
                    pixelData.FreeMemory = true;
                    pixelData.Scan0 = Marshal.AllocHGlobal(_bitmap.RowBytes / 4 * _bitmap.Height * 3);
                    pixelData.Stride = _bitmap.RowBytes / 4 * 3;

                    if (lockMode == BitmapLockMode.ReadOnly || lockMode == BitmapLockMode.ReadWrite)
                    {
                        pixelData.ReadFromArgb(_bitmap.GetPixels());
                    }
                }
                else
                {
                    pixelData.Scan0 = _bitmap.GetPixels();
                }

                _bytesLockersCount++;
                return pixelData;
            }

            return null;
        }

        public void UnlockBitmapPixelData(BitmapPixelData bitmapPixelData)
        {
            try
            {
                if (bitmapPixelData.PixelFormat != PixelFormat.Rgb32 &&
                    bitmapPixelData.PixelFormat != PixelFormat.Rgba32 &&
                    bitmapPixelData.PixelFormat != PixelFormat.Gray8)
                {
                    if (bitmapPixelData.LockMode == BitmapLockMode.WriteOnly ||
                        bitmapPixelData.LockMode == BitmapLockMode.ReadWrite)
                    {
                        bitmapPixelData.CopyToArgb(_bitmap.GetPixels());
                    }
                }
            }
            finally
            {
                if (bitmapPixelData is SkiaBitmapPixelData &&
                    ((SkiaBitmapPixelData)bitmapPixelData).FreeMemory == true &&
                    bitmapPixelData.Scan0 != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(bitmapPixelData.Scan0);
                }

                _bytesLockersCount--;
            }
        }

        public void MakeTransparent()
        {
            using (var canvas = new SKCanvas(_bitmap))
            {
                canvas.Clear(SKColors.Transparent);
            }
        }

        public void MakeTransparent(ArgbColor color)
        {
        }

        public void Save(string filename, ImageFormat format, int quality = 0)
        {
            if (Current.Encoder != null && Current.Encoder.CanEncode(this))
            {
                Current.Encoder.Encode(this, filename, format, quality);
            }

            new SkiaBitmapEncoding().Encode(this, filename, format, quality);
        }

        public void Save(Stream stream, ImageFormat format, int quality = 0)
        {
            if (Current.Encoder != null && Current.Encoder.CanEncode(this))
            {
                Current.Encoder.Encode(this, stream, format, quality);
            }

            new SkiaBitmapEncoding().Encode(this, stream, format, quality);
        }

        public void SetResolution(float dpiX, float dpiY)
        {
            _dpiX = dpiX;
            _dpyY = dpiY;
        }

        #region Helper



        #endregion
    }
}