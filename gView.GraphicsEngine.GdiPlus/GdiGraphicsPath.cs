﻿using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.GraphicsEngine.GdiPlus
{
    class GdiGraphicsPath : IGraphicsPath
    {
        private System.Drawing.Drawing2D.GraphicsPath _path;

        public GdiGraphicsPath()
        {
            _path = new System.Drawing.Drawing2D.GraphicsPath();
        }

        #region IGraphicsPath

        public void StartFigure()
        {
            _path.StartFigure();
        }

        public void CloseFigure()
        {
            _path.CloseFigure();
        }

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            _path.AddLine(x1, y1, x2, y2);
        }

        public void AddLine(CanvasPointF p1, CanvasPointF p2)
        {
            _path.AddLine(p1.X, p1.Y, p2.X, p2.Y);
        }

        public CanvasRectangleF GetBounds()
        {
            var rectangleF = _path.GetBounds();
            return new CanvasRectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
        }

        public object EngineElement => _path;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if(_path!=null)
            {
                _path.Dispose();
                _path = null;
            }
        }

        #endregion
    }
}
