using gView.Framework.Carto.UI;
using gView.Framework.Symbology;
using gView.GraphicsEngine;
using System;
using System.IO;

namespace gView.Blazor.Core.Extensions;

static public class SymbolExtensions
{
    static public byte[] ToBytes(this ISymbol? symbol, int width = 30, int height = 20, bool addCrossHair = false)
    {
        var imageBytes = Array.Empty<byte>();

        if (symbol is not null)
        {

            using (var bitmap = Current.Engine.CreateBitmap(width, height, PixelFormat.Rgba32))
            using (var canvas = bitmap.CreateCanvas())
            using (var memStream = new MemoryStream())
            {
                bitmap.MakeTransparent();

                var rect = new CanvasRectangle(0, 0, width, height);

                if (addCrossHair)
                {
                    using (var pen = Current.Engine.CreatePen(ArgbColor.Gray, 1f))
                    {
                        pen.DashStyle = LineDashStyle.DashDotDot;
                        canvas.DrawLine(pen, 0f, rect.Height / 2f, rect.Width, rect.Height / 2f);
                        canvas.DrawLine(pen, rect.Width / 2f, 0f, rect.Width / 2f, rect.Height);
                    }
                }

                new SymbolPreview(null).Draw(canvas, rect, symbol, false);

                bitmap.Save(memStream, ImageFormat.Png);
                imageBytes = memStream.ToArray();

            }
        }

        return imageBytes;
    }

    static public string ToBase64ImageSource(this ISymbol? symbol, int width = 30, int height = 20, bool addCrossHair = false)
        => $"data:image/png;base64, {Convert.ToBase64String(symbol.ToBytes(width, height, addCrossHair))}";
}
