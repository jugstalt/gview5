using gView.Framework.Geometry;
using System;

namespace gView.Framework.Carto
{
    public class DisplayTransformation : IDisplayTransformation
    {
        private bool _useTransformation = false;
        private double _cos = 1.0, _sin = 0.0, _rotation = 0.0;

        public DisplayTransformation()
        {
        }

        public bool UseTransformation
        {
            get { return _useTransformation; }
        }

        public double DisplayRotation
        {
            set
            {
                _rotation = value;
                _cos = Math.Cos(_rotation * Math.PI / 180.0);
                _sin = Math.Sin(_rotation * Math.PI / 180.0);

                _useTransformation = (_rotation != 0.0);
            }
            get { return _rotation; }
        }

        public void Transform(IDisplay display, ref double x, ref double y)
        {
            if (display == null || _useTransformation == false)
            {
                return;
            }

            x -= display.ImageWidth / 2.0;
            y -= display.ImageHeight / 2.0;

            double x_ = x, y_ = y;

            x = x_ * _cos + y_ * _sin;
            y = -x_ * _sin + y_ * _cos;

            x += display.ImageWidth / 2.0;
            y += display.ImageHeight / 2.0;
        }

        public void InvTransform(IDisplay display, ref double x, ref double y)
        {
            if (display == null || _useTransformation == false)
            {
                return;
            }

            x -= display.ImageWidth / 2.0;
            y -= display.ImageHeight / 2.0;

            double x_ = x, y_ = y;

            x = x_ * _cos - y_ * _sin;
            y = x_ * _sin + y_ * _cos;

            x += display.ImageWidth / 2.0;
            y += display.ImageHeight / 2.0;
        }

        public IEnvelope TransformedBounds(IDisplay display)
        {
            if (display == null)
            {
                return new Envelope();
            }

            if (_useTransformation == false)
            {
                return display.Envelope;
            }

            IEnvelope oBounds = display.Envelope;
            Envelope bounds = new Envelope(display.Envelope);
            bounds.TranslateTo(0.0, 0.0);

            IPointCollection pColl = bounds.ToPointCollection(0);
            for (int i = 0; i < pColl.PointCount; i++)
            {
                IPoint point = pColl[i];
                double x = point.X * _cos + point.Y * _sin;
                double y = -point.X * _sin + point.Y * _cos;
                point.X = x;
                point.Y = y;
            }
            bounds = new Envelope(pColl.Envelope);
            bounds.TranslateTo(oBounds.Center.X, oBounds.Center.Y);
            return bounds;
        }
    }
}
