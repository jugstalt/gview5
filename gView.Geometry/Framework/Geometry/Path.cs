﻿using gView.Framework.SpatialAlgorithms;
using gView.Framework.system;
using System;
using System.Collections.Generic;

//[assembly: InternalsVisibleTo("gView.OGC, PublicKey=0024000004800000940000000602000000240000525341310004000001000100916d0be3f662c2d3589fbe93479f3215e23fd195db9a20e77f42dc1d2942bd48cad3ea36b797f57880e6c31af0c238d2e445898c8ecce990aacbb70ae05a10aff73ab65c7db86366697f934b780238ed8fd1b2e28ba679a97e060b53fce66118e129b91d24f392d4dd3d482fa4173e61f18c74cda9f35721a97e77afbbc96dd2")]


namespace gView.Framework.Geometry
{
    /// <summary>
    /// A sequence of connected segments.
    /// </summary>
    public class Path : PointCollection, IPath, ICloneable
    {
        public Path()
            : base()
        {
        }
        public Path(List<IPoint> points)
            : base(points)
        {
        }
        public Path(IPointCollection pColl)
            : base(pColl)
        {
        }

        #region IPath Member

        /// <summary>
        /// The length of the curve.
        /// </summary>
        public double Length
        {
            get
            {
                if (m_points.Count < 2)
                {
                    return 0.0;
                }

                double len = 0.0;
                for (int i = 1; i < m_points.Count; i++)
                {
                    len += Math.Sqrt(
                        (this[i - 1].X - this[i].X) * (this[i - 1].X - this[i].X) +
                        (this[i - 1].Y - this[i].Y) * (this[i - 1].Y - this[i].Y));
                }
                return len;
            }
        }

        public void ClosePath()
        {
            if (this.PointCount < 3)
            {
                return;
            }

            IPoint ps = this[0];
            IPoint pe = this[this.PointCount - 1];

            if (ps == null || pe == null ||
                ps.Equals(pe, 1e-10))
            {
                return;
            }

            this.AddPoint(new Point(ps));
        }

        public void ChangeDirection()
        {
            m_points = ListOperations<IPoint>.Swap(m_points);
        }
        public IPath Trim(double length)
        {
            Path trim = new Path();
            if (m_points.Count == 0)
            {
                return trim;
            }

            IPoint prePoint = new Point(m_points[0]);
            trim.AddPoint(prePoint);

            double len = 0.0;
            for (int i = 1; i < m_points.Count; i++)
            {
                IPoint point = m_points[i];
                double l = prePoint.Distance(point);
                if (len + l > length)
                {
                    double dx = point.X - prePoint.X;
                    double dy = point.Y - prePoint.Y;
                    double dz = point.Z - prePoint.Z;
                    point.X = prePoint.X + (length - len) / l * dx;
                    point.Y = prePoint.Y + (length - len) / l * dy;
                    point.Z = prePoint.Z + (length - len) / l * dz;
                    trim.AddPoint(point);
                    break;
                }
                else
                {
                    trim.AddPoint(point);
                }
                len += l;
                prePoint = point;
            }
            return trim;
        }

        public IPoint MidPoint2D
        {
            get
            {
                if (this.PointCount == 0)
                {
                    return null;
                }

                double length = this.Length;
                if (length == 0)
                {
                    return this[0];
                }

                return Algorithm.PolylinePoint(new Polyline(this), length / 2D);
            }
        }

        public IPolyline ToPolyline()
        {
            var path = new Path(this);
            if (this is Ring)
            {
                path.ClosePath();
            }

            return new Polyline(path);
        }

        #endregion

        #region ICloneable Member

        public virtual object Clone()
        {
            return new Path(m_points);
        }

        #endregion

        public void Close()
        {
            if (m_points.Count < 3)
            {
                return;
            }

            if (!m_points[0].Equals(m_points[m_points.Count - 1]))
            {
                m_points.Add(new Point(m_points[0]));
            }
        }
    }
}
