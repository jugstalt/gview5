using gView.Framework.system;
using NpgsqlTypes;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace gView.Framework.Geometry
{
    /// <summary>
    /// A Rectange with side paralell to the coordinate system axes.
    /// </summary>
    public sealed class Envelope : IEnvelope, IGeometry
    {
        private double m_minx = 0.0, m_miny = 0.0, m_maxx = 0.0, m_maxy = 0.0;

        public Envelope()
        {
            m_minx = m_miny = m_maxx = m_maxy = 0.0;
        }

        public Envelope(double minX, double minY, double maxX, double maxY)
        {
            m_minx = Math.Min(minX, maxX);
            m_miny = Math.Min(minY, maxY);
            m_maxx = Math.Max(maxX, minX);
            m_maxy = Math.Max(maxY, minY);
        }

        public Envelope(IEnvelope env)
        {
            if (env == null)
            {
                return;
            }

            m_minx = env.minx;
            m_miny = env.miny;
            m_maxx = env.maxx;
            m_maxy = env.maxy;
        }

        public Envelope(XmlNode env)
        {
            if (env == null)
            {
                return;
            }

            minx = Convert.ToDouble(env.Attributes["minx"].Value.Replace(".", ","));
            miny = Convert.ToDouble(env.Attributes["miny"].Value.Replace(".", ","));
            maxx = Convert.ToDouble(env.Attributes["maxx"].Value.Replace(".", ","));
            maxy = Convert.ToDouble(env.Attributes["maxy"].Value.Replace(".", ","));
        }

        public Envelope(IPoint lowerLeft, IPoint upperRight)
        {
            m_minx = Math.Min(lowerLeft.X, upperRight.X);
            m_miny = Math.Min(lowerLeft.Y, upperRight.Y);

            m_maxx = Math.Max(lowerLeft.X, upperRight.X);
            m_maxy = Math.Max(lowerLeft.Y, upperRight.Y);
        }



        /// <summary>
        /// The position of the left side
        /// </summary>
        public double minx
        {
            get { return m_minx; }
            set { m_minx = value; }
        }

        /// <summary>
        /// The position of the bottom side
        /// </summary>
        public double miny
        {
            get { return m_miny; }
            set { m_miny = value; }
        }

        /// <summary>
        /// The position of the right side
        /// </summary>
        public double maxx
        {
            get { return m_maxx; }
            set { m_maxx = value; }
        }

        /// <summary>
        /// The position of the top side
        /// </summary>
        public double maxy
        {
            get { return m_maxy; }
            set { m_maxy = value; }
        }

        public IPoint LowerLeft
        {
            get { return new Point(minx, miny); }
            set
            {
                if (value == null)
                {
                    return;
                }

                m_minx = value.X;
                m_miny = value.Y;
            }
        }
        public IPoint LowerRight
        {
            get { return new Point(maxx, miny); }
            set
            {
                if (value == null)
                {
                    return;
                }

                m_maxx = value.X;
                m_miny = value.Y;
            }
        }
        public IPoint UpperRight
        {
            get { return new Point(maxx, maxy); }
            set
            {
                if (value == null)
                {
                    return;
                }

                m_maxx = value.X;
                m_maxy = value.Y;
            }
        }
        public IPoint UpperLeft
        {
            get { return new Point(minx, maxy); }
            set
            {
                if (value == null)
                {
                    return;
                }

                m_minx = value.X;
                m_maxy = value.Y;
            }
        }
        public IPoint Center
        {
            get
            {
                return new Point(minx * 0.5 + maxx * 0.5, miny * 0.5 + maxy * 0.5);
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                IPoint c = this.Center;
                double dx = value.X - c.X;
                double dy = value.Y - c.Y;

                m_minx += dx; m_maxx += dx;
                m_miny += dy; m_maxy += dy;
            }
        }
        private void CheckMinMax()
        {
            double minx = m_minx, miny = m_miny;
            double maxx = m_maxx, maxy = m_maxy;

            m_minx = Math.Min(minx, maxx);
            m_miny = Math.Min(miny, maxy);
            m_maxx = Math.Max(minx, maxx);
            m_maxy = Math.Max(miny, maxy);
        }

        public Envelope MakeValid()
        {
            if (!double.IsNaN(m_minx) &&
               !double.IsNaN(m_miny) &&
               !double.IsNaN(m_maxx) &&
               !double.IsNaN(m_maxy) &&
               !double.IsInfinity(m_minx) &&
               !double.IsInfinity(m_miny) &&
               !double.IsInfinity(m_maxx) &&
               !double.IsInfinity(m_maxy))
            {
                return this;
            }

            double minx = m_minx, miny = m_miny, maxx = m_maxx, maxy = m_maxy;

            if ((double.IsNaN(minx) || double.IsInfinity(minx)) &&
                !(double.IsNaN(maxx) && double.IsInfinity(maxx)))
            {
                minx = maxx - 0.1;
            }

            if ((double.IsNaN(miny) || double.IsInfinity(miny)) &&
                !(double.IsNaN(maxy) && double.IsInfinity(maxy)))
            {
                miny = maxy - 0.1;
            }

            if ((double.IsNaN(maxx) || double.IsInfinity(maxx)) &&
                !(double.IsNaN(minx) && double.IsInfinity(minx)))
            {
                maxx = minx + 0.1;
            }

            if ((double.IsNaN(maxy) || double.IsInfinity(maxy)) &&
                !(double.IsNaN(miny) && double.IsInfinity(miny)))
            {
                maxy = miny + 0.1;
            }

            if (!double.IsNaN(minx) &&
               !double.IsNaN(miny) &&
               !double.IsNaN(maxx) &&
               !double.IsNaN(maxy) &&
               !double.IsInfinity(minx) &&
               !double.IsInfinity(miny) &&
               !double.IsInfinity(maxx) &&
               !double.IsInfinity(maxy))
            {
                return new Envelope(minx, miny, maxx, maxy);
            }

            return null;
        }

        public void Clean(CleanGemetryMethods methods, double tolerance = 1e-8)
        {
            // Nothing to do for envelops?
        }

        public bool IsEmpty() => false;

        /// <summary>
        /// Sets the Envelope equal to the union of the base Envelope and the input Envelope
        /// </summary>
        /// <param name="envelope">An object that implements <c>IEnvelope</c></param>
        public void Union(IEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            m_minx = Math.Min(m_minx, envelope.minx);
            m_miny = Math.Min(m_miny, envelope.miny);
            m_maxx = Math.Max(m_maxx, envelope.maxx);
            m_maxy = Math.Max(m_maxy, envelope.maxy);
        }

        public void Raise(double percent)
        {
            percent /= 100.0;
            double w = Math.Abs(m_maxx - m_minx);
            double h = Math.Abs(m_maxy - m_miny);

            w = (w * percent - w) / 2;
            h = (h * percent - h) / 2;

            m_minx -= w;
            m_miny -= h;
            m_maxx += w;
            m_maxy += h;
        }

        public void Raise(IPoint point, double percent)
        {
            if (point == null)
            {
                return;
            }

            percent /= 100;
            double w1 = point.X - minx, w2 = maxx - point.X;
            double h1 = point.Y - miny, h2 = maxy - point.Y;

            w1 = w1 * percent; w2 = w2 * percent;
            h1 = h1 * percent; h2 = h2 * percent;

            m_minx = point.X - w1;
            m_miny = point.Y - h1;
            m_maxx = point.X + w2;
            m_maxy = point.Y + h2;
        }

        public bool Intersects(IEnvelope envelope)
        {
            if (envelope.maxx >= m_minx &&
                envelope.minx <= m_maxx &&
                envelope.maxy >= m_miny &&
                envelope.miny <= m_maxy)
            {
                return true;
            }

            return false;
        }

        public bool Contains(IEnvelope envelope)
        {
            if (envelope.minx < m_minx ||
                envelope.maxx > m_maxx)
            {
                return false;
            }

            if (envelope.miny < m_miny ||
                envelope.maxy > m_maxy)
            {
                return false;
            }

            return true;
        }

        public bool Contains(IPoint point)
        {
            return point.X >= m_minx &&
                   point.X <= m_maxx &&
                   point.Y >= m_miny &&
                   point.Y <= m_maxy;
        }

        #region IGeometry Member

        /// <summary>
        /// The type of the geometry (Envelope)
        /// </summary>
        public gView.Framework.Geometry.GeometryType GeometryType
        {
            get
            {
                return GeometryType.Envelope;
            }
        }

        /// <summary>
        /// Creates a copy of this geometry's envelope and returns it.
        /// </summary>
        IEnvelope gView.Framework.Geometry.IGeometry.Envelope
        {
            get
            {
                return new Envelope(this);
            }
        }

        public int? Srs { get; set; }

        public int VertexCount => 4;

        #endregion

        public IPolygon ToPolygon(int accuracy = 0)
        {
            if (accuracy < 0)
            {
                accuracy = 0;
            }

            double stepx = this.Width / (accuracy + 1);
            double stepy = this.Height / (accuracy + 1);

            IPolygon polygon = new Polygon();
            polygon.AddRing(new Ring());

            polygon[0].AddPoint(new Point(m_minx, m_miny));
            for (int i = 0; i < accuracy; i++)
            {
                polygon[0].AddPoint(new Point(m_minx, m_miny + stepy * (i + 1)));
            }

            polygon[0].AddPoint(new Point(m_minx, m_maxy));
            for (int i = 0; i < accuracy; i++)
            {
                polygon[0].AddPoint(new Point(m_minx + stepx * (i + 1), m_maxy));
            }

            polygon[0].AddPoint(new Point(m_maxx, m_maxy));
            for (int i = 0; i < accuracy; i++)
            {
                polygon[0].AddPoint(new Point(m_maxx, m_maxy - stepy * (i + 1)));
            }

            polygon[0].AddPoint(new Point(m_maxx, m_miny));
            for (int i = 0; i < accuracy; i++)
            {
                polygon[0].AddPoint(new Point(m_maxx - stepx * (i + 1), m_miny));
            }

            polygon[0].AddPoint(new Point(m_minx, m_miny));

            return polygon;
        }

        public IPointCollection ToPointCollection(int accuracy)
        {
            if (accuracy < 0)
            {
                accuracy = 0;
            }

            double stepx = this.Width / (accuracy + 1);
            double stepy = this.Height / (accuracy + 1);

            PointCollection pColl = new PointCollection();
            for (int y = 0; y <= accuracy + 1; y++)
            {
                for (int x = 0; x <= accuracy + 1; x++)
                {
                    pColl.AddPoint(new Point(this.minx + stepx * x, this.miny + stepy * y));
                }
            }
            return pColl;
        }
        public static double[] minBounds(IEnvelope e)
        {

            double[] min = new double[4];
            min[0] = e.minx;
            min[1] = e.miny;
            min[2] = min[3] = 0;
            return min;

        }
        public static double[] maxBounds(IEnvelope e)
        {

            double[] max = new double[4];
            max[0] = e.maxx;
            max[1] = e.maxy;
            max[2] = max[3] = 0;
            return max;

        }

        #region IEnvelope Member


        public double Width
        {
            get { return Math.Abs(m_maxx - m_minx); }
        }

        public double Height
        {
            get { return Math.Abs(m_maxy - m_miny); }
        }

        public void Translate(double mx, double my)
        {
            m_minx += mx;
            m_miny += my;
            m_maxx += mx;
            m_maxy += my;
        }

        public void TranslateTo(double mx, double my)
        {
            double cx = m_minx * 0.5 + m_maxx * 0.5;
            double cy = m_miny * 0.5 + m_maxy * 0.5;

            Translate(mx - cx, my - cy);
        }

        public string ToBBoxString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_minx.ToDoubleString());
            sb.Append(",");
            sb.Append(m_miny.ToDoubleString());
            sb.Append(",");
            sb.Append(m_maxx.ToDoubleString());
            sb.Append(",");
            sb.Append(m_maxy.ToDoubleString());

            return sb.ToString();
        }
        #endregion

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj, 0.0);
        }
        public bool Equals(object obj, double epsi)
        {
            if (!(obj is IEnvelope))
            {
                return false;
            }

            IEnvelope env2 = (IEnvelope)obj;
            return
                Math.Abs(minx - env2.minx) <= epsi &&
                Math.Abs(miny - env2.miny) <= epsi &&
                Math.Abs(maxx - env2.maxx) <= epsi &&
                Math.Abs(maxy - env2.maxy) <= epsi;
        }

        #region ICloneable Member

        public object Clone()
        {
            return new Envelope(this);
        }

        #endregion

        /// <summary>
        /// For the internal use of the framework
        /// </summary>
        /// <param name="w"></param>
        public void Serialize(BinaryWriter w, IGeometryDef geomDef)
        {
            w.Write(m_minx);
            w.Write(m_miny);
            w.Write(m_maxx);
            w.Write(m_maxy);
            if (geomDef.HasZ)
            {
                w.Write(0);
            }
        }
        /// <summary>
        /// For the internal use of the framework
        /// </summary>
        /// <param name="w"></param>
        public void Deserialize(BinaryReader r, IGeometryDef geomDef)
        {
            m_minx = r.ReadDouble();
            m_miny = r.ReadDouble();
            m_maxx = r.ReadDouble();
            m_maxy = r.ReadDouble();
            if (geomDef.HasZ)
            {
                r.ReadDouble();
            }
        }

        #region Static Members
        public static bool IsNull(IEnvelope envelope)
        {
            if (envelope == null)
            {
                return true;
            }

            if (Math.Abs(envelope.minx) < double.Epsilon &&
                Math.Abs(envelope.miny) < double.Epsilon &&
                Math.Abs(envelope.maxx) < double.Epsilon &&
                Math.Abs(envelope.maxy) < double.Epsilon)
            {
                return true;
            }

            if (double.IsNaN(envelope.minx) ||
                double.IsNaN(envelope.miny) ||
                double.IsNaN(envelope.maxx) ||
                double.IsNaN(envelope.maxy))
            {
                return true;
            }

            return false;
        }

        public static IEnvelope Null() => new Envelope(0D, 0D, 0D, 0D);

        public static IEnvelope FromBBox(string bboxString)
        {
            string[] bbox = bboxString.Split(',');
            if (bbox.Length != 4)
            {
                throw new Exception("Invalid BBOX parameter. Must consist of 4 elements of type double or integer");
            }

            double MinX = bbox[0].ToDouble();
            double MinY = bbox[1].ToDouble();
            double MaxX = bbox[2].ToDouble();
            double MaxY = bbox[3].ToDouble();

            return new Envelope(MinX, MinY, MaxX, MaxY);
        }

        #endregion

        public void Set(IPoint ll, IPoint ur)
        {
            m_minx = Math.Min(ll.X, ur.X);
            m_minx = Math.Min(ll.Y, ur.Y);

            m_maxx = Math.Max(ll.X, ur.X);
            m_maxx = Math.Max(ll.Y, ur.Y);
        }
    }
}
