﻿using gView.GraphicsEngine.Abstraction;
using gView.GraphicsEngine.Skia.Extensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.GraphicsEngine.Skia
{
    class SkiaCanvas : ICanvas
    {
        private SKCanvas _canvas;

        internal SkiaCanvas(SKCanvas canvas)
        {
            _canvas = canvas;
        }

        public float DpiX => 96f;

        public float DpiY => 96f;

        public CompositingMode CompositingMode { set { } }
        public InterpolationMode InterpolationMode { get; set; }
        public SmoothingMode SmoothingMode { get; set; }
        public TextRenderingHint TextRenderingHint { get; set; }

        public IDisplayCharacterRanges DisplayCharacterRanges(IFont font, IDrawTextFormat format, string text)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _canvas.Dispose();
        }

        public void DrawBitmap(IBitmap bitmap, CanvasPoint point)
        {
            //_canvas.DrawBitmap((SKBitmap)bitmap.EngineElement, point.ToSKPoint(), new SKPaint()
            //{
            //    FilterQuality = this.InterpolationMode.ToSKFilterQuality()
            //});
        }

        public void DrawBitmap(IBitmap bitmap, CanvasPointF pointF)
        {
            _canvas.DrawBitmap((SKBitmap)bitmap.EngineElement, pointF.ToSKPoint(), new SKPaint()
            {
                FilterQuality = this.InterpolationMode.ToSKFilterQuality()
            });
        }

        public void DrawBitmap(IBitmap bitmap, CanvasRectangle dest, CanvasRectangle source, float opacity = 1)
        {
            _canvas.DrawBitmap((SKBitmap)bitmap.EngineElement, source.ToSKRect(), dest.ToSKRect(), new SKPaint()
            {
                FilterQuality = this.InterpolationMode.ToSKFilterQuality()
            });
        }

        public void DrawBitmap(IBitmap bitmap, CanvasRectangleF dest, CanvasRectangleF source)
        {
            _canvas.DrawBitmap((SKBitmap)bitmap.EngineElement, source.ToSKRect(), dest.ToSKRect(), new SKPaint()
            {
                FilterQuality = this.InterpolationMode.ToSKFilterQuality()
            });
        }

        public void DrawBitmap(IBitmap bitmap, CanvasPointF[] points, CanvasRectangleF source, float opacity = 1)
        {
            // ToDo: Nicht korrekt!!
            var dest = new CanvasRectangleF(points[0].X, points[0].Y, points[1].X - points[0].X, points[1].Y - points[0].Y);

            _canvas.DrawBitmap((SKBitmap)bitmap.EngineElement, source.ToSKRect(), dest.ToSKRect());
        }

        public void DrawEllipse(IPen pen, float x1, float y1, float width, float height)
        {
            _canvas.DrawOval(new SKRect(x1, y1, x1 + width, y1 + width), GetSKPaint(pen));
        }

        public void FillEllipse(IBrush brush, float x1, float y1, float width, float height)
        {
            _canvas.DrawOval(new SKRect(x1, y1, x1 + width, y1 + width), GetSKPaint(brush));
        }

        public void DrawRectangle(IPen pen, CanvasRectangle rectangle)
        {
            _canvas.DrawRect(rectangle.ToSKRect(), GetSKPaint(pen));
        }

        public void DrawRectangle(IPen pen, CanvasRectangleF rectangleF)
        {
            _canvas.DrawRect(rectangleF.ToSKRect(), GetSKPaint(pen));
        }

        public void FillRectangle(IBrush brush, CanvasRectangle rectangle)
        {
            _canvas.DrawRect(rectangle.ToSKRect(), GetSKPaint(brush));
        }

        public void FillRectangle(IBrush brush, CanvasRectangleF rectangleF)
        {
            _canvas.DrawRect(rectangleF.ToSKRect(), GetSKPaint(brush));
        }

        public void DrawLine(IPen pen, CanvasPoint p1, CanvasPoint p2)
        {
            _canvas.DrawLine(p1.ToSKPoint(), p2.ToSKPoint(), GetSKPaint(pen));
        }

        public void DrawLine(IPen pen, CanvasPointF p1, CanvasPointF p2)
        {
            _canvas.DrawLine(p1.ToSKPoint(), p2.ToSKPoint(), GetSKPaint(pen));
        }

        public void DrawLine(IPen pen, int x1, int y1, int x2, int y2)
        {
            _canvas.DrawLine(x1, y1, x2, y2, GetSKPaint(pen));
        }

        public void DrawLine(IPen pen, float x1, float y1, float x2, float y2)
        {
            _canvas.DrawLine(x1, y1, x2, y2, GetSKPaint(pen));
        }

        public void DrawPath(IPen pen, IGraphicsPath path)
        {
            _canvas.DrawPath((SKPath)path.EngineElement, GetSKPaint(pen));
        }

        public void FillPath(IBrush brush, IGraphicsPath path)
        {
            _canvas.DrawPath((SKPath)path.EngineElement, GetSKPaint(brush));
        }

        public void DrawText(string text, IFont font, IBrush brush, CanvasPoint point)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, int x, int y)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, CanvasPointF pointF)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, float x, float y)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, CanvasRectangleF rectangleF)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, CanvasPoint point, IDrawTextFormat format)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, int x, int y, IDrawTextFormat format)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, CanvasPointF pointF, IDrawTextFormat format)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, IFont font, IBrush brush, float x, float y, IDrawTextFormat format)
        {
            //throw new NotImplementedException();
        }

        public CanvasSizeF MeasureText(string text, IFont font)
        {
            return new CanvasSizeF(50, 10);
            //throw new NotImplementedException();
        }

        public void ResetTransform()
        {
            _canvas.ResetMatrix();
        }

        public void RotateTransform(float angle)
        {
            _canvas.RotateDegrees(angle);
        }

        public void TranslateTransform(CanvasPointF point)
        {
            _canvas.Translate(point.ToSKPoint());
        }

        #region Helper

        private SKPaint GetSKPaint(IPen pen)
        {
            var skPaint = (SKPaint)pen.EngineElement;

            return skPaint;
        }

        private SKPaint GetSKPaint(IBrush brush)
        {
            var skPaint = (SKPaint)brush.EngineElement;

            return skPaint;
        }

        private SKPaint GetSKPaint(IFont font)
        {
            var skPaint = (SKPaint)font.EngineElement;

            return skPaint;
        }

        #endregion
    }
}
 