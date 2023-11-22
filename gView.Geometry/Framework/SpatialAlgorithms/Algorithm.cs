using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.LinAlg;
using gView.Framework.SpatialAlgorithms.Clipper;
using gView.Framework.system;
using gView.Geometry.Framework.Topology;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gView.Framework.SpatialAlgorithms
{
    public class Algorithm
    {
        #region Jordan
        public static bool Jordan(IPolygon polygon, double x, double y)
        {
            int inter = 0;
            try
            {
                for (int i = 0; i < polygon.RingCount; i++)
                {
                    inter += CalcIntersections(polygon[i], x, y);
                }
            }
            catch { return false; }

            return ((inter % 2) == 0) ? false : true;
        }

        public static bool Jordan(IRing ring, IRing hole)
        {
            if (hole == null || hole.PointCount < 3 || ring == null || ring.PointCount < 3)
            {
                return false;
            }

            Polygon polygon = new Polygon(ring);

            for (int i = 0; i < hole.PointCount; i++)
            {
                if (!Jordan(polygon, hole[i].X, hole[i].Y))
                {
                    return false;
                }
            }
            return true;
        }

        private static int CalcIntersections(IRing ring, double x, double y)
        {
            bool first = true;
            double x1 = 0.0, y1 = 0.0, x2 = 0, y2 = 0, x0 = 0, y0 = 0, k, d;
            int inter = 0;

            for (int i = 0; i < ring.PointCount; i++)
            {
                IPoint point = ring[i];
                x2 = point.X - x;
                y2 = point.Y - y;
                if (!first)
                {
                    if (isPositive(x1) != isPositive(x2))
                    {
                        if (getLineKD(x1, y1, x2, y2, out k, out d))
                        {
                            if (d > 0)
                            {
                                inter++;
                            }
                        }
                    }
                }
                x1 = x2;
                y1 = y2;
                if (first)
                {
                    first = false;
                    x0 = x1; y0 = y1;
                }
            }

            //Ring schliessen
            if (Math.Abs(x0 - x2) > 1e-7 || Math.Abs(y0 - y2) > 1e-7)
            {
                if (isPositive(x0) != isPositive(x2))
                {
                    if (getLineKD(x0, y0, x2, y2, out k, out d))
                    {
                        if (d > 0)
                        {
                            inter++;
                        }
                    }
                }
            }
            return inter;
        }
        #endregion

        #region Box
        public static bool IntersectBox(IGeometry geometry, IEnvelope envelope)
        {
            if (geometry == null)
            {
                return false;
            }

            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    return PointInBox((IPoint)geometry, envelope);
                case GeometryType.Multipoint:
                    IPointCollection points = (IPointCollection)geometry;
                    for (int i = 0; i < points.PointCount; i++)
                    {
                        if (PointInBox(points[i], envelope))
                        {
                            return true;
                        }
                    }
                    return false;
                case GeometryType.Polyline:
                    IPolyline polyline = (IPolyline)geometry;
                    for (int i = 0; i < polyline.PathCount; i++)
                    {
                        IPath path = polyline[i];
                        if (PathIntersectBox(path, envelope))
                        {
                            return true;
                        }

                        for (int j = 0; j < path.PointCount; j++)
                        {
                            if (PointInBox(path[i], envelope))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                case GeometryType.Polygon:
                    IPolygon polygon = (IPolygon)geometry;
                    for (int i = 0; i < polygon.RingCount; i++)
                    {
                        IRing ring = polygon[i];
                        if (PathIntersectBox(ring, envelope))
                        {
                            return true;
                        }

                        for (int j = 0; j < ring.PointCount; j++)
                        {
                            if (PointInBox(ring[j], envelope))
                            {
                                return true;
                            }
                        }
                    }

                    if (Jordan(polygon, envelope.minx, envelope.miny))
                    {
                        return true;
                    }

                    if (Jordan(polygon, envelope.maxx, envelope.maxy))
                    {
                        return true;
                    }

                    if (Jordan(polygon, envelope.minx, envelope.maxy))
                    {
                        return true;
                    }

                    if (Jordan(polygon, envelope.maxx, envelope.miny))
                    {
                        return true;
                    }

                    return false;
                case GeometryType.Envelope:
                    Envelope env = new Envelope((IEnvelope)geometry);
                    return env.Intersects(envelope);
                case GeometryType.Aggregate:
                    for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                    {
                        if (IntersectBox(((IAggregateGeometry)geometry)[i], envelope))
                        {
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }

        private static bool PointInBox(IPoint point, IEnvelope env)
        {
            if (point == null)
            {
                return false;
            }

            if (point.X >= env.minx && point.X <= env.maxx && point.Y >= env.miny && point.Y <= env.maxy)
            {
                return true;
            }

            return false;
        }
        private static bool PathIntersectBox(IPath path, IEnvelope envelope)
        {
            if (PathIntersectLine(path, envelope.minx, envelope.miny, envelope.minx, envelope.maxy))
            {
                return true;
            }

            if (PathIntersectLine(path, envelope.minx, envelope.maxy, envelope.maxx, envelope.maxy))
            {
                return true;
            }

            if (PathIntersectLine(path, envelope.maxx, envelope.maxy, envelope.maxx, envelope.miny))
            {
                return true;
            }

            if (PathIntersectLine(path, envelope.maxx, envelope.miny, envelope.minx, envelope.miny))
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Intersects

        public static bool Intersects(IGeometry geometry, IGeometry candidate)
        {
            if (geometry == null || candidate == null)
            {
                return false;
            }

            if (geometry is IEnvelope)
            {
                return IntersectBox(candidate, geometry as IEnvelope);
            }
            if (candidate is IEnvelope)
            {
                return IntersectBox(geometry, candidate as IEnvelope);
            }
            if (geometry is IPolygon)
            {
                IPointCollection coll = GeometryPoints(candidate, false);
                if (coll.PointCount == 0)
                {
                    return false;
                }

                // candidate is inside of geometry (1 Point)
                for (int i = 0; i < coll.PointCount; i++)
                {
                    IPoint point = coll[i];
                    if (point == null)
                    {
                        continue;
                    }

                    // If there is a point inside the polygon -> return true;
                    if (Jordan((IPolygon)geometry, point.X, point.Y))
                    {
                        return true;
                    }
                }

                // geometry is in candidate (1 Point)
                if (candidate is IPolygon)
                {
                    coll = GeometryPoints(geometry, false);
                    for (int i = 0; i < coll.PointCount; i++)
                    {
                        IPoint point = coll[i];
                        if (point == null)
                        {
                            continue;
                        }

                        // If there is a point inside the candidate -> return true;
                        if (Jordan((IPolygon)candidate, point.X, point.Y))
                        {
                            return true;
                        }
                    }
                }

                // if there is a path intersection -> return true;
                List<IPath> polyPaths = GeometryPaths(geometry);
                List<IPath> candPaths = GeometryPaths(candidate);
                if (polyPaths.Count > 0 && candPaths.Count > 0)
                {
                    foreach (IPath polyPath in polyPaths)
                    {
                        foreach (IPath candPath in candPaths)
                        {
                            if (PathIntersectPath(polyPath, candPath))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            if (geometry is IPolyline)
            {
                if (candidate is Polygon)
                {
                    return Intersects(candidate, geometry);
                }
                // if there is a path intersection -> return true;
                List<IPath> polyPaths = GeometryPaths(geometry);
                List<IPath> candPaths = GeometryPaths(candidate);
                if (polyPaths.Count > 0 && candPaths.Count > 0)
                {
                    foreach (IPath polyPath in polyPaths)
                    {
                        foreach (IPath candPath in candPaths)
                        {
                            if (PathIntersectPath(polyPath, candPath))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            if (geometry is IPoint)
            {
                if (candidate is IPoint)
                {
                    return (
                        Math.Abs(((IPoint)geometry).X - ((IPoint)candidate).X) < 1e-5 &&
                        Math.Abs(((IPoint)geometry).X - ((IPoint)candidate).X) < 1e-5);
                }

                return Intersects(candidate, geometry);
            }

            if (geometry is IMultiPoint)
            {
                if (!Intersects(candidate, geometry.Envelope))
                {
                    return false;
                }

                for (int i = 0; i < ((IMultiPoint)geometry).PointCount; i++)
                {
                    if (Intersects(candidate, ((IMultiPoint)geometry)[i]))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    if (Intersects(((IAggregateGeometry)geometry)[i], candidate))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
        public static IPoint SegmentIntersection(IPoint p11, IPoint p12, IPoint p21, IPoint p22, bool between)
        {
            if (p11 == null || p12 == null || p21 == null || p22 == null)
            {
                return null;
            }

            double lx = p21.X - p11.X;
            double ly = p21.Y - p11.Y;

            double r1x = p12.X - p11.X, r1y = p12.Y - p11.Y;
            double r2x = p22.X - p21.X, r2y = p22.Y - p21.Y;

            LinearEquation2 lineq = new LinearEquation2(
                lx, ly,
                r1x, -r2x,
                r1y, -r2y);
            if (lineq.Solve())
            {
                double t1 = lineq.Var1;
                double t2 = lineq.Var2;

                if (between &&
                    (t1 < 0.0 || t1 > 1.0 ||
                     t2 < 0.0 || t2 > 1.0))
                {
                    return null;
                }

                return new Point(p11.X + t1 * r1x, p11.Y + t1 * r1y);
            }

            return null;
        }

        public static bool IsSelfIntersecting(IPointCollection pColl, double tolerance, bool appendIntersectionVertices = false)
        {
            if (pColl == null || pColl.PointCount <= 2)
            {
                return false;
            }

            bool result = false;

            var env1 = new Envelope();
            var env2 = new Envelope();

            int to = pColl.PointCount - 1;
            for (int i = 0; i < to; i++)
            {
                IPoint p00 = pColl[i], p01 = pColl[i + 1];

                env1.Set(p00, p01);

                #region Check if next is parallel with opposite direction

                if (i < pColl.PointCount - 2)
                {
                    IPoint p02 = pColl[i + 2];

                    double x1 = p01.X - p00.X, y1 = p01.Y - p00.Y;
                    double x2 = p02.X - p01.X, y2 = p02.Y - p01.Y;

                    if (IsParallel(x1, y1, x2, y2, tolerance) == -1)
                    {
                        return true;
                    }
                }

                #endregion

                for (int j = i + 2; j < to; j++)  // i+2 -> direkt anschließende Line nicht checken, die kann maximal paralell zurück verlaufen und brauch einen anderen Test...
                {
                    IPoint p10 = pColl[j], p11 = pColl[j + 1];

                    env2.Set(p10, p11);
                    if (!env2.Intersects(env1))
                    {
                        continue;
                    }

                    IPoint intersectionPoint = SegmentIntersection(p00, p01, p10, p11, true);
                    if (intersectionPoint != null &&
                        (intersectionPoint.Distance(p00) > tolerance) &&
                        (intersectionPoint.Distance(p01) > tolerance) &&
                        (intersectionPoint.Distance(p10) > tolerance) &&
                        (intersectionPoint.Distance(p11) > tolerance))
                    {
                        if (appendIntersectionVertices)
                        {
                            pColl.InsertPoint(intersectionPoint, j + 1);   // j first !!
                            pColl.InsertPoint(intersectionPoint, i + 1);

                            return IsSelfIntersecting(pColl, tolerance, appendIntersectionVertices);
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return result;
        }

        public static bool HasInverseParallels(IPointCollection pColl, double tolerance, bool autoRepair = false)
        {
            if (pColl == null || pColl.PointCount <= 2)
            {
                return false;
            }

            bool retry = false;
            int to = pColl.PointCount - 1;

            for (int i = 0; i < to; i++)
            {
                IPoint p00 = pColl[i], p01 = pColl[i + 1];

                #region Check if next is parallel with opposite direction

                if (i < pColl.PointCount - 2)
                {
                    IPoint p02 = pColl[i + 2];

                    double x1 = p01.X - p00.X, y1 = p01.Y - p00.Y;
                    double x2 = p02.X - p01.X, y2 = p02.Y - p01.Y;

                    if (IsParallel(x1, y1, x2, y2, tolerance) == -1)
                    {
                        if (autoRepair)
                        {
                            pColl.RemovePoint(i + 1);
                            retry = true;
                            break;
                        }
                        return true;
                    }
                }

                #endregion
            }

            if (retry == true)
            {
                return HasInverseParallels(pColl, tolerance, autoRepair);
            }
            return false;
        }

        public static int IsParallel(double x1, double y1, double x2, double y2, double tolerance)
        {
            var len1 = Math.Sqrt(x1 * x1 + y1 * y1);
            var len2 = Math.Sqrt(x2 * x2 + y2 * y2);

            if (len1 == 0 || len2 == 0)  // ??
            {
                return 0;  // NaN ??
            }

            x1 /= len1;
            y1 /= len1;
            x2 /= len2;
            y2 /= len2;

            if (Math.Abs(x2 - x1) < tolerance && Math.Abs(y2 - y1) < tolerance)   // Parallel with same direction
            {
                return 1;
            }

            if (Math.Abs(x1 + x2) < tolerance && Math.Abs(y1 + y2) < tolerance)   // Paralell with opposite direction 
            {
                return -1;
            }

            return 0;
        }

        #endregion

        #region Paths

        private static int[] FindIdenticalPoints(IPointCollection pColl, double tolerance, bool ignoreStartAndEnd = false)
        {
            int from = ignoreStartAndEnd ? 1 : 0;
            int to = ignoreStartAndEnd ? pColl.PointCount - 1 : pColl.PointCount;
            var tolerance2 = tolerance * tolerance;

            List<int> idendenticalPoints = null;

            for (int i = from; i < to; i++)
            {
                if (idendenticalPoints?.Contains(i) == true)
                {
                    continue;
                }

                var duples = FindDuples(pColl, pColl[i], tolerance2, i + 1);

                if (duples.Length > 0)
                {
                    if (idendenticalPoints == null)
                    {
                        idendenticalPoints = new List<int>();
                    }

                    idendenticalPoints.Add(i);
                    idendenticalPoints.AddRange(duples);
                }
            }

            return idendenticalPoints?.ToArray() ?? new int[0];
        }

        private static int[] FindDuples(IPointCollection pColl, IPoint point, double tolerance2, int startAt = 0)
        {
            List<int> result = null;

            for (int i = startAt, to = pColl.PointCount; i < to; i++)
            {
                if (pColl[i].Distance2(point) <= tolerance2)
                {
                    if (result == null)
                    {
                        result = new List<int>();
                    }

                    result.Add(i);
                }
            }

            return result?.ToArray() ?? new int[0];
        }

        public static IEnumerable<IRing> SplitRing__(IRing ring, double tolerance)
        {
            ring.Close(tolerance);

            if (IsSelfIntersecting(ring, tolerance, true))
            {
                throw new Exception("can't resolve self intersecting for polygon ring");
            }

            var identicalPoints = FindIdenticalPoints(ring, tolerance, true);
            if (identicalPoints.Length == 0)
            {
                return new IRing[] { ring }.Where(r => r.Area > tolerance * tolerance);
            }

            List<IPath> paths = new List<IPath>();
            Path current = new Path();

            for (int i = 0; i < ring.PointCount; i++)
            {
                current.AddPoint(new Point(ring[i]));

                if (identicalPoints.Contains(i))
                {
                    paths.Add(current);
                    current = new Ring();
                    current.AddPoint(new Point(ring[i]));
                }
            }
            paths.Add(current);

            var result = new List<IRing>();

            #region Paths to Polygons

            foreach (var newPolygon in paths.Polygonize())
            {
                result.AddRange(newPolygon.Rings.Where(r => r.Area > tolerance * tolerance));
            }

            #endregion

            return result.Where(r => r.Area > tolerance * tolerance);
        }

        public static IEnumerable<IRing> RemoveLineArtifacts(IRing ring, double tolerance)
        {
            ring.Close(tolerance);

            if (HasInverseParallels(ring, tolerance, true))
            {
                throw new Exception("can't resolve self overlapping rings");
            }

            if (ring.Area < tolerance * tolerance)
            {
                return Array.Empty<IRing>();
            }

            return new[] { ring };

            //ring.Close(tolerance);

            //var identicalPoints = FindIdenticalPoints(ring, tolerance, true);
            //if (identicalPoints.Length == 0)
            //{
            //    return new IRing[] { ring }.Where(r => r.Area > 0);
            //}

            //var newRings = new List<IRing>();
            //var newRing = new Ring();

            //newRings.Add(newRing);

            //for (int p = 0, pointCount = ring.PointCount; p < pointCount; p++)
            //{
            //    if (p > 0 && p < pointCount - 1 && identicalPoints.Contains(p))
            //    {
            //        newRing.Close(tolerance);

            //        newRing = new Ring();
            //        newRings.Add(newRing);
            //    }
            //    newRing.AddPoint(ring[p]);
            //}
            //newRing.Close(tolerance);

            //return newRings.Where(r => r.Area > tolerance * tolerance);
        }

        #endregion

        #region Contains
        public static bool Contains(IGeometry geometry, IGeometry candidate)
        {
            if (geometry == null || candidate == null)
            {
                return false;
            }

            IPointCollection coll = GeometryPoints(candidate, false);
            if (coll.PointCount == 0)
            {
                return false;
            }

            if (geometry is IPolygon)
            {
                for (int i = 0; i < coll.PointCount; i++)
                {
                    IPoint point = coll[i];
                    if (point == null)
                    {
                        continue;
                    }

                    // If there is a Point outside -> return false;
                    if (!Jordan((IPolygon)geometry, point.X, point.Y))
                    {
                        return false;
                    }

                    // If there is a Path intersection -> return false;
                    List<IPath> polyPaths = GeometryPaths(geometry);
                    List<IPath> candPaths = GeometryPaths(candidate);
                    if (polyPaths.Count > 0 && candPaths.Count > 0)
                    {
                        foreach (IPath polyPath in polyPaths)
                        {
                            foreach (IPath candPath in candPaths)
                            {
                                if (PathIntersectPath(polyPath, candPath))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            else if (geometry is IEnvelope)
            {
                IEnvelope env = (IEnvelope)geometry;
                for (int i = 0; i < coll.PointCount; i++)
                {
                    IPoint point = coll[i];
                    if (point == null)
                    {
                        continue;
                    }

                    if (point.X < env.minx || point.X > env.maxx ||
                        point.Y < env.miny || point.Y > env.maxy)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    if (Contains(((IAggregateGeometry)geometry)[i], candidate))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
        #endregion

        #region generell
        private static bool PathIntersectPath(IPath path1, IPath path2)
        {
            if (path1 == null || path2 == null ||
                path1.PointCount < 2 || path2.PointCount < 2 || !path1.Envelope.Intersects(path2.Envelope))
            {
                return false;
            }

            double x1 = path1[0].X;
            double y1 = path1[0].Y;
            double x2, y2;
            for (int i = 1; i < path1.PointCount; i++)
            {
                x2 = path1[i].X;
                y2 = path1[i].Y;
                if (PathIntersectLine(path2, x1, y1, x2, y2))
                {
                    return true;
                }

                x1 = x2;
                y1 = y2;
            }
            if (path1 is IRing)
            {
                x2 = path1[0].X;
                y2 = path1[0].Y;
                if (PathIntersectLine(path2, x1, y1, x2, y2))
                {
                    return true;
                }
            }
            return false;
        }

        /*
        private static bool PathIntersectLine(IPath path, double x_1, double y_1, double x_2, double y_2)
        {
            if (path == null) return false;
            if (path.PointCount < 2) return false;

            double minx = Math.Min(x_1, x_2);
            double miny = Math.Min(y_1, y_2);
            double maxx = Math.Max(x_1, x_2);
            double maxy = Math.Max(y_1, y_2);

            double dx = Math.Abs(maxx - minx), dy = Math.Abs(maxy - miny);
            double al = Math.Atan2(dy, dx);
            double len = Math.Sqrt(dx * dx + dy * dy), c = Math.Cos(al), s = Math.Sin(al);

            double x1 = path[0].X - minx;
            double y1 = path[0].Y - miny;
            double x2, y2, xx1, yy1, xx2, yy2;

            xx1 = c * x1 + s * y1;
            yy1 = -s * x1 + c * y1;
            for (int j = 1; j < path.PointCount; j++)
            {
                IPoint point = path[j];

                x2 = point.X - minx;
                y2 = point.Y - miny;

                xx2 = c * x2 + s * y2;
                yy2 = -s * x2 + c * y2;

                if (isPositive(yy1) != isPositive(yy2))
                {
                    double k, d;
                    getLineKD(xx1, yy1, xx2, yy2, out k, out d);
                    if (Math.Abs(k) > 1e-5)
                    {
                        double xSec = -d / k;
                        if (xSec >= 0 && xSec <= len) return true;
                    }
                    else if (Math.Abs(k) < 1e-10 && Math.Abs(d) < 1e-10)
                    {
                        if (Math.Min(xx1, xx2) <= len &&
                            Math.Max(xx1, xx2) >= 0) return true;
                    }
                }
                xx1 = xx2;
                yy1 = yy2;
            }
            if (path is IRing)  // Letzten Punkt prüfen...
            {
                IPoint point = path[0];

                x2 = point.X - minx;
                y2 = point.Y - miny;

                xx2 = c * x2 + s * y2;
                yy2 = -s * x2 + c * y2;

                if (isPositive(yy1) != isPositive(yy2))
                {
                    double k, d;
                    getLineKD(xx1, yy1, xx2, yy2, out k, out d);
                    if (Math.Abs(k) > 1e-5)
                    {
                        double xSec = -d / k;
                        if (xSec >= 0 && xSec <= len) return true;
                    }
                    else if (Math.Abs(k) < 1e-10 && Math.Abs(d) < 1e-10)
                    {
                        if (Math.Min(xx1, xx2) <= len &&
                            Math.Max(xx1, xx2) >= 0) return true;
                    }
                }
            }
            return false;
        }
        */

        private static bool PathIntersectLine(IPath path, double x_1, double y_1, double x_2, double y_2)
        {
            if (path == null)
            {
                return false;
            }

            if (path.PointCount < 2)
            {
                return false;
            }

            double x1 = path[0].X;
            double y1 = path[0].Y;
            double x2, y2;

            for (int j = 1; j < path.PointCount; j++)
            {
                IPoint point = path[j];

                x2 = point.X;
                y2 = point.Y;
                if (LineIntersectLine(x1, y1, x2, y2, x_1, y_1, x_2, y_2))
                {
                    return true;
                }

                x1 = x2;
                y1 = y2;
            }
            if (path is IRing)  // Letzten Punkt prüfen...
            {
                IPoint point = path[0];

                x2 = point.X;
                y2 = point.Y;
                if (LineIntersectLine(x1, y1, x2, y2, x_1, y_1, x_2, y_2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LineIntersectLine(double x01, double y01, double x11, double y11, double x02, double y02, double x12, double y12)
        {
            double lx = x02 - x01;
            double ly = y02 - y01;

            double rx1 = x11 - x01, ry1 = y11 - y01;
            double rx2 = x12 - x02, ry2 = y12 - y02;

            double D = Det(rx1, -rx2, ry1, -ry2);
            if (Math.Abs(D) < 1e-10)
            {
                return false; // Paralell
            }
            // Algorithmus zum prüfen, ob sich Paralelle Linienstücke überlappen
            // Fehlt noch...

            double D1 = Det(lx, -rx2, ly, -ry2);
            double D2 = Det(rx1, lx, ry1, ly);

            double t1 = D1 / D;
            double t2 = D2 / D;

            if (t1 >= 0.0 && t1 <= 1.0 &&
                t2 >= 0.0 && t2 <= 1.0)
            {
                return true;
            }

            return false;
        }

        public static Point IntersectLine(IPoint p11, IPoint p12, IPoint p21, IPoint p22, bool between)
        {
            if (p11 == null || p12 == null || p21 == null || p22 == null)
            {
                return null;
            }

            double lx = p21.X - p11.X;
            double ly = p21.Y - p11.Y;

            double r1x = p12.X - p11.X, r1y = p12.Y - p11.Y;
            double r2x = p22.X - p21.X, r2y = p22.Y - p21.Y;

            LinearEquation2 lineq = new LinearEquation2(
                lx, ly,
                r1x, -r2x,
                r1y, -r2y);
            if (lineq.Solve())
            {
                double t1 = lineq.Var1;
                double t2 = lineq.Var2;

                if (between &&
                    (t1 < 0.0 || t1 > 1.0 ||
                     t2 < 0.0 || t2 > 1.0))
                {
                    return null;
                }

                return new Point(p11.X + t1 * r1x, p11.Y + t1 * r1y);
            }

            return null;
        }

        private static double Det(double a00, double a01, double a10, double a11)
        {
            // | a00  a01 |
            // | a01  a11 |

            return a00 * a11 - a01 * a10;
        }
        private static bool getLineKD(double x1, double y1, double x2, double y2, out double k, out double d)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            if (Math.Abs(dx) < 1e-10)
            {
                d = k = 0.0;
                return false;
            }
            k = dy / dx;
            d = y1 - k * x1;  // y=kx+d
            return true;
        }
        private static bool isPositive(double z)
        {
            if (Math.Sign(z) < 0)
            {
                return false;
            }

            return true;
        }

        private static List<IPoint> HorizontalIntersectionPoints(IPolygon polygon, double cx, double cy)
        {
            List<IPoint> points = new List<IPoint>();

            for (int i = 0; i < polygon.RingCount; i++)
            {
                HorizontalIntersectionPoints(polygon[i], cx, cy, ref points);
            }
            points.Sort(new PointComparerX());
            return points;
        }
        private static List<IPoint> VerticalIntersectionPoints(IPolygon polygon, double cx, double cy)
        {
            List<IPoint> points = new List<IPoint>();

            for (int i = 0; i < polygon.RingCount; i++)
            {
                VerticalIntersectionPoints(polygon[i], cx, cy, ref points);
            }
            points.Sort(new PointComparerX());
            return points;
        }
        private static void HorizontalIntersectionPoints(IPath path, double cx, double cy, ref List<IPoint> points)
        {
            if (path.PointCount == 0)
            {
                return;
            }

            double x1 = path[0].X - cx, y1 = path[0].Y - cy, x2, y2, k, d;

            int to = path.PointCount;
            if (path is IRing)
            {
                if (path[0].X != path[to - 1].X ||
                    path[0].Y != path[to - 1].Y)
                {
                    to += 1;
                }
            }

            for (int i = 1; i < to; i++)
            {
                IPoint point = (i < path.PointCount) ? path[i] : path[0];
                x2 = point.X - cx;
                y2 = point.Y - cy;

                if (isPositive(y1) != isPositive(y2))
                {
                    if (getLineKD(x1, y1, x2, y2, out k, out d))
                    {
                        if (k != 0)
                        {
                            points.Add(new Point(-d / k + cx, cy));
                        }
                    }
                }
                x1 = x2;
                y1 = y2;
            }
        }
        private static void VerticalIntersectionPoints(IPath path, double cx, double cy, ref List<IPoint> points)
        {
            if (path.PointCount == 0)
            {
                return;
            }

            double x1 = path[0].X - cx, y1 = path[0].Y - cy, x2, y2, k, d;

            int to = path.PointCount;
            if (path is IRing)
            {
                if (path[0].X != path[to - 1].X ||
                    path[0].Y != path[to - 1].Y)
                {
                    to += 1;
                }
            }

            for (int i = 1; i < to; i++)
            {
                IPoint point = (i < path.PointCount) ? path[i] : path[0];
                x2 = point.X - cx;
                y2 = point.Y - cy;

                if (isPositive(x1) != isPositive(x2))
                {
                    if (getLineKD(x1, y1, x2, y2, out k, out d))
                    {
                        points.Add(new Point(cx, d + cy));
                    }
                }
                x1 = x2;
                y1 = y2;
            }
        }

        private static List<SimpleLineSegment> CreateLineSegments(List<IPoint> points)
        {
            List<SimpleLineSegment> lines = new List<SimpleLineSegment>();
            CreateLineSegments(points, ref lines);
            return lines;
        }
        private static void CreateLineSegments(List<IPoint> points, ref List<SimpleLineSegment> lines)
        {
            if (lines == null || points == null)
            {
                return;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                lines.Add(new SimpleLineSegment(points[i], points[i + 1]));
            }
        }

        public static void Rotate(double angle, IPointCollection pColl)
        {
            if (pColl == null)
            {
                return;
            }

            double sin_a = Math.Sin(angle);
            double cos_a = Math.Cos(angle);

            for (int i = 0; i < pColl.PointCount; i++)
            {
                IPoint p = pColl[i];
                double x = p.X * cos_a - p.Y * sin_a;
                double y = p.X * sin_a + p.Y * cos_a;
                p.X = x;
                p.Y = y;
            }
        }
        public static void Translate(double tx, double ty, IPointCollection pColl)
        {
            if (pColl == null)
            {
                return;
            }

            for (int i = 0; i < pColl.PointCount; i++)
            {
                IPoint p = pColl[i];
                p.X += tx;
                p.Y += ty;
            }
        }

        public static IPointCollection GeometryPoints(IGeometry geometry, bool clonePoints)
        {
            IPointCollection coll = new PointCollection();
            AppendGeometryPoints(geometry, coll, clonePoints, false);
            return coll;
        }
        public static IPointCollection StartEndPoints(IGeometry geometry, bool clonePoints)
        {
            IPointCollection coll = new PointCollection();
            AppendGeometryPoints(geometry, coll, clonePoints, true);
            return coll;
        }
        private static void AppendGeometryPoints(IGeometry geometry, IPointCollection coll, bool clonePoints, bool startEndPointsOnly)
        {
            if (geometry == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                coll.AddPoint((clonePoints) ? geometry.Clone() as IPoint : (IPoint)geometry);
            }
            else if (geometry is IPointCollection)
            {
                IPointCollection pColl = (IPointCollection)geometry;
                if (startEndPointsOnly && pColl.PointCount > 1)
                {
                    AppendGeometryPoints(pColl[0], coll, clonePoints, false);
                    AppendGeometryPoints(pColl[pColl.PointCount - 1], coll, clonePoints, false);
                }
                else
                {
                    for (int i = 0; i < pColl.PointCount; i++)
                    {
                        AppendGeometryPoints(pColl[i], coll, clonePoints, startEndPointsOnly);
                    }
                }
            }
            else if (geometry is IPolyline)
            {
                for (int p = 0; p < ((IPolyline)geometry).PathCount; p++)
                {
                    IPath path = ((IPolyline)geometry)[p];
                    if (path == null)
                    {
                        continue;
                    }

                    if (startEndPointsOnly && path.PointCount > 1)
                    {
                        AppendGeometryPoints(path[0], coll, clonePoints, false);
                        AppendGeometryPoints(path[path.PointCount - 1], coll, clonePoints, false);
                    }
                    else
                    {
                        for (int i = 0; i < path.PointCount; i++)
                        {
                            AppendGeometryPoints(path[i], coll, clonePoints, false);
                        }
                    }
                }
            }
            else if (geometry is IPolygon)
            {
                for (int p = 0; p < ((IPolygon)geometry).RingCount; p++)
                {
                    IRing ring = ((IPolygon)geometry)[p];
                    if (ring == null)
                    {
                        continue;
                    }

                    if (startEndPointsOnly && ring.PointCount > 1)
                    {
                        AppendGeometryPoints(ring[0], coll, clonePoints, false);
                        AppendGeometryPoints(ring[ring.PointCount - 1], coll, clonePoints, false);
                    }
                    else
                    {
                        for (int i = 0; i < ring.PointCount; i++)
                        {
                            AppendGeometryPoints(ring[i], coll, clonePoints, false);
                        }
                    }
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int p = 0; p < ((IAggregateGeometry)geometry).GeometryCount; p++)
                {
                    IGeometry geom = ((IAggregateGeometry)geometry)[p];
                    if (geom == null)
                    {
                        continue;
                    }

                    AppendGeometryPoints(geom, coll, clonePoints, startEndPointsOnly);
                }
            }
            else if (geometry is IEnvelope)
            {
                IEnvelope env = (IEnvelope)geometry;
                if (startEndPointsOnly)  // was immer das beim Envelope sein soll!
                {
                    AppendGeometryPoints(env.LowerLeft, coll, clonePoints, false);
                    AppendGeometryPoints(env.UpperRight, coll, clonePoints, false);
                }
                else
                {
                    AppendGeometryPoints(env.LowerLeft, coll, clonePoints, false);
                    AppendGeometryPoints(env.LowerRight, coll, clonePoints, false);
                    AppendGeometryPoints(env.UpperRight, coll, clonePoints, false);
                    AppendGeometryPoints(env.UpperLeft, coll, clonePoints, false);
                }
            }
        }

        public static List<IPath> GeometryPaths(IGeometry geometry)
        {
            List<IPath> paths = new List<IPath>();
            AppendGeometryPaths(geometry, paths);
            return paths;
        }
        private static void AppendGeometryPaths(IGeometry geometry, List<IPath> paths)
        {
            if (geometry == null)
            {
                return;
            }

            if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    paths.Add(((IPolyline)geometry)[i]);
                }
            }
            else if (geometry is IPolygon)
            {
                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    paths.Add(((IPolygon)geometry)[i]);
                }
            }
            else if (geometry is IEnvelope)
            {
                Ring ring = new Ring();
                ring.AddPoint(new Point(((IEnvelope)geometry).minx, ((IEnvelope)geometry).miny));
                ring.AddPoint(new Point(((IEnvelope)geometry).minx, ((IEnvelope)geometry).maxy));
                ring.AddPoint(new Point(((IEnvelope)geometry).maxx, ((IEnvelope)geometry).maxy));
                ring.AddPoint(new Point(((IEnvelope)geometry).maxx, ((IEnvelope)geometry).miny));
                paths.Add(ring);
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int p = 0; p < ((IAggregateGeometry)geometry).GeometryCount; p++)
                {
                    IGeometry geom = ((IAggregateGeometry)geometry)[p];
                    AppendGeometryPaths(geom, paths);
                }
            }
        }

        public static List<IEnvelope> PartEnvelops(IGeometry geometry)
        {
            List<IEnvelope> envelops = new List<IEnvelope>();

            PartEnvelops(geometry, envelops);
            return envelops;
        }
        private static void PartEnvelops(IGeometry geometry, List<IEnvelope> envelops)
        {
            if (geometry == null)
            {
                return;
            }

            if (geometry is IPoint)
            {
                envelops.Add(geometry.Envelope);
            }
            else if (geometry is IMultiPoint)
            {
                for (int i = 0; i < ((IMultiPoint)geometry).PointCount; i++)
                {
                    if (((IMultiPoint)geometry)[i] != null)
                    {
                        envelops.Add(((IMultiPoint)geometry)[i].Envelope);
                    }
                }
            }
            else if (geometry is IPolyline)
            {
                for (int i = 0; i < ((IPolyline)geometry).PathCount; i++)
                {
                    if (((IPolyline)geometry)[i] != null)
                    {
                        envelops.Add(((IPolyline)geometry)[i].Envelope);
                    }
                }
            }
            else if (geometry is IPolygon)
            {
                if (geometry is Polygon)
                {
                    ((Polygon)geometry).VerifyHoles();
                }

                for (int i = 0; i < ((IPolygon)geometry).RingCount; i++)
                {
                    // Nur Ringe übernehmen, weil Envelope von Holes
                    // darin schon enthalten sind...
                    if (((IPolygon)geometry)[i] is IRing)
                    {
                        envelops.Add(((IPolygon)geometry)[i].Envelope);
                    }
                }
            }
            else if (geometry is IAggregateGeometry)
            {
                for (int i = 0; i < ((IAggregateGeometry)geometry).GeometryCount; i++)
                {
                    PartEnvelops(((IAggregateGeometry)geometry)[i], envelops);
                }
            }
        }

        public static IPoint NearestPointToPath(IGeometry geometry, IPoint point, out double distance)
        {
            return NearestPointToPath(geometry, point, out distance, false);
        }
        public static IPoint NearestPointToPath(IGeometry geometry, IPoint point, out double distance, bool addPoint)
        {
            int partNr = 0, pointNr = 0;
            return NearestPointToPath(geometry, point, out distance, addPoint, true, out partNr, out pointNr);
        }
        public static IPoint NearestPointToPath(IGeometry geometry, IPoint point, out double distance, bool addPoint, bool checkVertices)
        {
            int partNr = 0, pointNr = 0;
            return NearestPointToPath(geometry, point, out distance, addPoint, checkVertices, out partNr, out pointNr);
        }
        public static IPoint NearestPointToPath(IGeometry geometry, IPoint point, out double distance, bool addPoint, bool checkVertices, out int partNr, out int pointNr)
        {
            distance = double.MaxValue;
            partNr = pointNr = -1;

            try
            {
                IPoint nearest = new Point();

                IPath nearestPath = null;
                int nearestPathVertextIndex = -1;
                bool found = false;

                List<IPath> paths = GeometryPaths(geometry);
                if (paths != null)
                {
                    foreach (Path path in paths)
                    {
                        if (path == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < path.PointCount - 1; i++)
                        {
                            IPoint p0 = path[i], p1 = path[i + 1];

                            double l0 = point.X - p0.X;
                            double l1 = point.Y - p0.Y;
                            double a00 = p1.X - p0.X;
                            double a10 = p1.Y - p0.Y;
                            double len = Math.Sqrt(a00 * a00 + a10 * a10);
                            if (len == 0.0)
                            {
                                continue;
                            }

                            LinearEquation2 eq = new LinearEquation2(
                                l0, l1, a00, a10, a10, -a00);
                            if (eq.Solve())
                            {
                                double t1 = eq.Var1;
                                double t2 = eq.Var2;

                                if (t1 < 0.0 || t1 > 1.0)
                                {
                                    continue;
                                }

                                double dist = Math.Abs(len * t2);

                                if (dist < distance)
                                {
                                    distance = dist;
                                    nearest.X = p0.X + a00 * t1;
                                    nearest.Y = p0.Y + a10 * t1;

                                    nearestPath = path;
                                    nearestPathVertextIndex = i;

                                    found = true;

                                    partNr = paths.IndexOf(path);
                                    pointNr = i;
                                }
                            }
                        }
                    }
                }

                if (checkVertices)
                {
                    IPointCollection vertices = GeometryPoints(geometry, false);
                    if (vertices != null)
                    {
                        for (int i = 0; i < vertices.PointCount; i++)
                        {
                            IPoint p = vertices[i];
                            double dist = PointDistance(p, point);
                            if (dist < distance)
                            {
                                distance = dist;
                                nearest.X = p.X;
                                nearest.Y = p.Y;

                                nearestPath = null;
                                found = true;

                                partNr = -1;
                                pointNr = i;
                            }
                        }
                    }
                }
                if (found)
                {
                    if (addPoint && nearestPath != null)
                    {
                        nearestPath.InsertPoint(point, nearestPathVertextIndex + 1);
                    }
                    return nearest;
                }
            }
            catch
            {
            }
            return null;
        }

        static public double PointDistance(IPoint p1, IPoint p2)
        {
            if (p1 == null || p2 == null)
            {
                throw new ArgumentException();
            }

            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        static public IPoint PointDifference(IPoint p1, IPoint p2)
        {
            if (p1 == null || p2 == null)
            {
                throw new ArgumentException();
            }

            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        static public IPoint PointAddition(IPoint p1, IPoint p2)
        {
            if (p1 == null || p2 == null)
            {
                throw new ArgumentException();
            }

            return new Point(p2.X + p1.X, p2.Y + p1.Y);
        }

        static public List<IPolygon> SplitPolygonToDonutsAndPolygons(IPolygon canditate)
        {
            if (canditate == null || canditate.RingCount == 0)
            {
                return null;
            }

            List<IPolygon> polygons = new List<IPolygon>();
            Polygon polygon = new Polygon(canditate);
            if (polygon.RingCount == 1 ||
                polygon.OuterRingCount == 1)
            {
                polygons.Add(polygon);
                return polygons;
            }

            polygon.VerifyHoles();
            polygon.SortRingsInv();

            for (int i = 0; i < polygon.RingCount; i++)
            {
                if (polygon[i] == null || polygon[i] is IHole)
                {
                    continue;
                }

                Polygon p = new Polygon(polygon[i]);
                polygons.Add(p);
            }
            for (int i = 0; i < polygon.RingCount; i++)
            {
                if (!(polygon[i] is IHole))
                {
                    continue;
                }

                foreach (IPolygon p in polygons)
                {
                    if (Jordan(p[0], polygon[i]))
                    {
                        p.AddRing(polygon[i]);
                        break;
                    }
                }
            }

            return polygons;
        }

        static public void RemoveArcs(IPath path)
        {
            if (path == null || path.PointCount < 3)
            {
                return;
            }

            Point M = null;
            double R2 = double.NaN;
            List<int> removeIndices = new List<int>();
            for (int i = 2, pointCount = path.PointCount; i < pointCount - 1; i++)
            {
                LinearEquation3 leq = new LinearEquation3();

                IPoint p1 = path[i - 2], p2 = path[i - 1], p3 = path[i];
                double[] pX = new double[] { 0.0, p2.X - p1.X, p3.X - p1.X };  // p1 abziehen, sonst ist Determinate exterm schlecht konditioniert!!!!
                double[] pY = new double[] { 0.0, p2.Y - p1.Y, p3.Y - p1.Y };

                leq.SetRow(0, -(pX[0] * pX[0] + pY[0] * pY[0]), 1.0, -pX[0], -pY[0]);
                leq.SetRow(1, -(pX[1] * pX[1] + pY[1] * pY[1]), 1.0, -pX[1], -pY[1]);
                leq.SetRow(2, -(pX[2] * pX[2] + pY[2] * pY[2]), 1.0, -pX[2], -pY[2]);

                if (leq.Solve())
                {
                    double A = leq.Var1, B = leq.Var2, C = leq.Var3;

                    M = new Point(B / 2.0 + p1.X, C / 2.0 + p1.Y);
                    R2 = M.X * M.X + M.Y * M.Y - A;

                    for (int j = i + 1; j < pointCount; j++)
                    {
                        IPoint p = path[j];
                        leq.SetRow(2, -((p.X - p1.X) * (p.X - p1.X) + (p.Y - p1.Y) * (p.Y - p1.Y)), 1.0, -(p.X - p1.X), -(p.Y - p1.Y));

                        if (leq.Solve())
                        {
                            Point M_ = new Point(leq.Var2 / 2.0 + p1.X, leq.Var3 / 2.0 + p1.Y);
                            if (M.Distance2(M_) < 1e-2)
                            {
                                if (!removeIndices.Contains(j - 1))
                                {
                                    removeIndices.Add(j - 1);
                                }
                            }
                            else
                            {
                                i = j + 1;
                                break;
                            }
                        }
                        else
                        {
                            i = j + 1;
                            break;
                        }
                    }
                }
            }

            removeIndices.Sort();
            for (int i = removeIndices.Count - 1; i >= 0; i--)
            {
                path.RemovePoint(removeIndices[i]);
            }
        }

        static public IPointCollection RemoveDoubles(IPointCollection pColl, double epsi = 1e-7)
        {
            if (pColl == null)
            {
                return null;
            }

            PointCollection result = new PointCollection();
            if (pColl.PointCount > 0)
            {
                result.AddPoint(pColl[0]);
            }

            for (int i = 1, to = pColl.PointCount; i < to; i++)
            {
                IPoint p0 = pColl[i - 1], p1 = pColl[i];
                if (p0.Distance(p1) <= epsi)
                {
                    continue;
                }

                result.AddPoint(p1);
            }

            return result;
        }

        static public IGeometry Generalize(IGeometry geometry, double generalizationDistance, bool onlyValidResult = false)
        {
            if (geometry is IPolyline)
            {
                var polyline = (IPolyline)geometry;

                Polyline result = new Polyline();
                for (int i = 0; i < polyline.PathCount; i++)
                {
                    var path = new Path(GeneralizePointCollection(polyline[i], generalizationDistance));
                    if (onlyValidResult == false || path.Length > 0)
                    {
                        result.AddPath(path);
                    }
                }
                if (onlyValidResult == true && result.PathCount == 0)
                {
                    return null;
                }

                return result;
            }
            if (geometry is IPolygon)
            {
                var polygon = (IPolygon)geometry;

                Polygon result = new Polygon();
                for (int i = 0; i < polygon.RingCount; i++)
                {
                    var ring = new Ring(GeneralizePointCollection(polygon[i], generalizationDistance));
                    if (onlyValidResult == false || ring.Area > 0)
                    {
                        result.AddRing(ring);
                    }
                }

                if (onlyValidResult == true && result.RingCount == 0)
                {
                    return null;
                }

                return result;
            }

            return geometry;
        }

        static public IPointCollection GeneralizePointCollection(IPointCollection pColl, double generalizationDistance)
        {
            if (pColl == null)
            {
                return null;
            }

            PointCollection result = new PointCollection();
            if (pColl.PointCount > 0)
            {
                result.AddPoint(pColl[0]);
            }
            else
            {
                return result;
            }

            IPoint p0 = pColl[0];

            for (int i = 1, to = pColl.PointCount; i < to; i++)
            {
                IPoint p1 = pColl[i];
                if (p0.Distance(p1) <= generalizationDistance)
                {
                    continue;
                }

                result.AddPoint(p1);
                p0 = p1;
            }

            return result;
        }


        static public IGeometry InterpolatePoints(IGeometry geometry, int stepPoints, bool onlyValidResult = false)
        {
            if (geometry is IPolyline)
            {
                var polyline = (IPolyline)geometry;

                Polyline result = new Polyline();
                for (int i = 0; i < polyline.PathCount; i++)
                {
                    var path = new Path(InterpolatePoints(polyline[i], stepPoints));
                    if (onlyValidResult == false || path.Length > 0)
                    {
                        result.AddPath(path);
                    }
                }
                if (onlyValidResult == true && result.PathCount == 0)
                {
                    return null;
                }

                return result;
            }
            if (geometry is IPolygon)
            {
                var polygon = (IPolygon)geometry;

                Polygon result = new Polygon();
                for (int i = 0; i < polygon.RingCount; i++)
                {
                    var ring = new Ring(InterpolatePoints(polygon[i], stepPoints));
                    if (onlyValidResult == false || ring.Area > 0)
                    {
                        result.AddRing(ring);
                    }
                }

                if (onlyValidResult == true && result.RingCount == 0)
                {
                    return null;
                }

                return result;
            }

            return geometry;
        }
        static public IPointCollection InterpolatePoints(IPointCollection pColl, int stepPoints)
        {
            if (pColl == null || pColl.PointCount == 1)
            {
                return pColl;
            }

            PointCollection result = new PointCollection();

            for (int i = 1, to = pColl.PointCount - 1; i < to; i++)
            {
                IPoint p1 = pColl[i];
                IPoint p2 = pColl[i + 1];

                result.AddPoint(p1);

                var dist = p1.Distance2D(p2);
                if (dist > 0)
                {
                    double dx = (p2.X - p1.X) / (stepPoints + 1), dy = (p2.Y - p1.Y) / (stepPoints + 1);

                    for (int s = 0; s < stepPoints; s++)
                    {
                        result.AddPoint(new Point(p1.X + dx * s, p1.Y + dy * s));
                    }
                }

                result.AddPoint(p2);
            }

            return result;
        }


        static public IPolygon SnapOutsidePointsToEnvelope(IPolygon polygon, IEnvelope envelope)
        {
            // This Algormith is bullship -> try a liam bransky to solve this problem
            if (polygon == null)
            {
                return null;
            }

            if (envelope == null || envelope.Contains(polygon.Envelope))
            {
                return polygon;
            }

            Polygon result = new Polygon();
            int ringCount = polygon.RingCount;

            for (int r = 0; r < ringCount; r++)
            {
                var ring = polygon[r];
                var newRing = new Ring();  // Always create new Rings -> never change existing geometry!!!

                if (!envelope.Contains(ring.Envelope))
                {
                    for (int i = 0, to = ring.PointCount; i < to; i++)
                    {
                        IPoint point = new Point(ring[i]); // Always create new Point -> never change existing geometry!!!

                        point.X = Math.Max(point.X, envelope.minx);
                        point.Y = Math.Max(point.Y, envelope.miny);

                        point.X = Math.Min(point.X, envelope.maxx);
                        point.Y = Math.Min(point.Y, envelope.maxy);

                        newRing.AddPoint(point);
                    }
                }
                result.AddRing(newRing);
            }

            return result;
        }

        static public IPointCollection OrderPoints(IPointCollection points, IPoint center)
        {
            if (points == null || center == null)
            {
                return points;
            }

            List<IPoint> pointList = new List<IPoint>();
            for (int p = 0, to = points.PointCount; p < to; p++)
            {
                var point = points[p];
                if (point != null)
                {
                    pointList.Add(point);
                }
            }

            pointList.Sort(new PointSorter(center));

            return new PointCollection(pointList);
        }

        private class PointSorter : IComparer<IPoint>
        {
            private IPoint _center;

            public PointSorter(IPoint center)
            {
                _center = center;
            }

            public int Compare(IPoint x, IPoint y)
            {
                if (_center == null)
                {
                    return 0;
                }

                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return 1;
                }

                if (y == null)
                {
                    return -1;
                }

                return _center.Distance(x).CompareTo(_center.Distance(y));
            }
        }

        #endregion

        #region Labeling
        public static IMultiPoint PolygonLabelPoints(IPolygon polygon)
        {
            MultiPoint pColl = new MultiPoint();
            if (polygon == null)
            {
                return pColl;
            }

            if (polygon is Polygon)
            {
                ((Polygon)polygon).VerifyHoles();
            }

            for (int i = 0; i < polygon.RingCount; i++)
            {
                if (polygon[i] is IHole)
                {
                    continue;
                }

                double A = polygon[i].Area;
                IPoint centroid = polygon[i].Centroid;

                if (centroid == null)
                {
                    continue;
                }

                if (Jordan(polygon, centroid.X, centroid.Y))
                {
                    pColl.AddPoint(new SmartPolygonLabelPoint(centroid, polygon, polygon[i], centroid));
                    //AppendContentricLabelPoints(display, polygon, polygon[i], centroid, pColl);
                }
                else
                {

                    List<IPoint> hPoints = HorizontalIntersectionPoints(polygon, centroid.X, centroid.Y);
                    List<IPoint> vPoints = VerticalIntersectionPoints(polygon, centroid.X, centroid.Y);

                    List<SimpleLineSegment> Lines = new List<SimpleLineSegment>();
                    CreateLineSegments(hPoints, ref Lines);
                    CreateLineSegments(vPoints, ref Lines);

                    Lines.Sort(new SimpleLineSegmentComparerLengthInv());

                    foreach (SimpleLineSegment line in Lines)
                    {
                        IPoint center = line.Center;

                        if (Jordan(polygon, center.X, center.Y))
                        {
                            pColl.AddPoint(new SmartPolygonLabelPoint(center, polygon, polygon[i], center));
                            //AppendContentricLabelPoints(display, polygon, polygon[i], center, pColl);
                            break;
                        }
                    }
                }
            }
            return pColl;
        }

        private class SmartPolygonLabelPoint : Point, gView.Framework.Carto.ISmartLabelPoint
        {
            private IPolygon _polygon;
            private IRing _ring;
            private IPoint _centerPoint;

            public SmartPolygonLabelPoint(IPoint point, IPolygon polygon, IRing ring, IPoint centerPoint)
                : base(point)
            {
                _polygon = polygon;
                _ring = ring;
                _centerPoint = centerPoint;
            }

            #region ISmartLabelPoint Member

            public IMultiPoint AlernativeLabelPoints(gView.Framework.Carto.IDisplay display)
            {
                MultiPoint pColl = new MultiPoint();

                IEnvelope env = _ring.Envelope;
                double R =
                    Math.Max(
                    Math.Max(Math.Abs(env.minx - _centerPoint.X), Math.Abs(env.maxx - _centerPoint.X)),
                    Math.Max(Math.Abs(env.miny - _centerPoint.Y), Math.Abs(env.maxy - _centerPoint.Y)));
                double dr = 20.0 * display.MapScale / display.Dpm;

                for (double r = dr; r < R; r += dr)
                {
                    for (double w = 0.0; w < 2.0 * Math.PI; w += Math.PI / 8)
                    {
                        double x = _centerPoint.X + r * Math.Cos(w);
                        double y = _centerPoint.Y + r * Math.Sin(w);
                        if (Jordan(_polygon, x, y))
                        {
                            pColl.AddPoint(new Point(x, y));
                        }
                    }
                }

                return pColl;
            }

            #endregion
        }
        #endregion

        #region Buffer
        public static IPolygon MergePolygons(List<IPolygon> polygons)
        {
            return polygons.Merge();
        }

        public static IPolygon FastMergePolygon(List<IPolygon> polygons, ICancelTracker cancelTracker, gView.Framework.UI.ProgressReporterEvent reporter)
        {
            if (polygons == null || polygons.Count == 0)
            {
                return null;
            }

            if (polygons.Count == 1)
            {
                return polygons[0];
            }

            int count = polygons.Count;
            List<IPolygon> merged = new List<IPolygon>();
            gView.Framework.UI.ProgressReport report = new gView.Framework.UI.ProgressReport();
            report.featureMax = polygons.Count;
            report.Message = "Merge Polygons";

            for (int i = 0; i < polygons.Count; i += 2)
            {
                if (cancelTracker != null &&
                    cancelTracker.Continue == false)
                {
                    return null;
                }

                if (i + 1 < count)
                {
                    List<IPolygon> p = new List<IPolygon>();
                    p.Add(polygons[i]);
                    p.Add(polygons[i + 1]);
                    merged.Add(p.Merge());
                }
                else
                {
                    merged.Add(polygons[i]);
                }

                if (reporter != null)
                {
                    report.featurePos = i;
                    reporter(report);
                }
            }

            return FastMergePolygon(merged, cancelTracker, reporter);
        }

        internal static IPolygon PolylineBuffer(IPolyline polyline, double distance)
        {
            List<IPolygon> buffers = new List<IPolygon>();
            for (int i = 0; i < polyline.PathCount; i++)
            {
                PathBuffers(polyline[i], distance, ref buffers);
            }

            //return MergePolygons(buffers);
            return FastMergePolygon(buffers, null, null);
        }
        private static void PathBuffers(IPath path, double distance, ref List<IPolygon> polygons)
        {
            if (polygons == null)
            {
                return;
            }

            //IPolygon polygon=ClipWrapper.BufferPath(path,Math.Abs(distance));
            //if(polygon!=null) polygons.Add(polygon);

            List<IPoint> points = new List<IPoint>();
            int to = path.PointCount;
            if (to == 0)
            {
                return;
            }

            if (path is IRing)
            {
                if (path[0].X != path[to - 1].X || path[0].Y != path[to - 1].Y)
                {
                    to += 1;
                }
            }
            for (int i = 0; i < to - 1; i++)
            {
                IPoint p1 = ((i < path.PointCount) ? path[i] : path[0]);
                IPoint p2 = ((i + 1 < path.PointCount) ? path[i + 1] : path[0]);
                IPoint p3 = ((i + 2 < path.PointCount) ? path[i + 2] : path[0]);
                if (i == to - 2)
                {
                    p3 = null;
                }

                Vector2D v1 = new Vector2D(p2.X - p1.X, p2.Y - p1.Y);
                Vector2D v2 = (p3 != null) ? new Vector2D(p3.X - p2.X, p3.Y - p2.Y) : null;

                Vector2D pv1 = new Vector2D(v1);
                pv1.Rotate(Math.PI / 2.0);
                pv1.Length = distance;
                Vector2D pv2 = null;
                if (p3 != null)
                {
                    pv2 = new Vector2D(v2);
                    pv2.Rotate(Math.PI / 2.0);
                    pv2.Length = distance;
                }

                Ring ring = new Ring();
                if (i == 0)
                {
                    AppendCurve(ring, new Vector2D(p1.X, p1.Y), pv1, pv1 * -1.0, distance);
                }
                else
                {
                    ring.AddPoint(new Point(pv1.X + p1.X, pv1.Y + p1.Y));
                    ring.AddPoint(new Point(-pv1.X + p1.X, -pv1.Y + p1.Y));
                }

                if (p3 != null)
                {
                    v2.Rotate(-v1.Angle);
                    if (v2.Angle < Math.PI)
                    {
                        AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv1 * -1.0, pv2 * -1.0, distance);
                        //ring.AddPoint(new Point(p2.X, p2.Y));
                        ring.AddPoint(new Point(pv1.X + p2.X, pv1.Y + p2.Y));
                    }
                    else
                    {
                        ring.AddPoint(new Point(-pv1.X + p2.X, -pv1.Y + p2.Y));
                        //ring.AddPoint(new Point(p2.X, p2.Y));
                        AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv2, pv1, distance);
                    }
                }
                else
                {
                    AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv1 * -1.0, pv1, distance);
                }

                polygons.Add(new Polygon(ring));
            }
        }

        internal static IPolygon PolygonBuffer(IPolygon polygon, double distance)
        {
            if (polygon == null)
            {
                return null;
            }

            if (distance == 0.0)
            {
                return polygon;
            }

            //if (polygon.PointCount.PointCount > 50000)
            //    throw new Exception("To many vertices :" + polygon.PointCount);

            var clipperPolygon = polygon.ToClipperPolygons();
            var result = clipperPolygon.Buffer(distance);
            if (result == null)
            {
                throw new Exception("Can't calculate buffer!");
            }

            return result.ToPolygon();

            //List<IPolygon> buffers = new List<IPolygon>();

            //for (int i = 0; i < polygon.RingCount; i++)
            //{
            //    PathBuffers(polygon[i], Math.Abs(distance), ref buffers);
            //}

            //if (distance > 0.0)
            //{
            //    buffers.Add(polygon);
            //    //IPolygon buffer = MergePolygons(buffers);
            //    IPolygon buffer = FastMergePolygon(buffers, null, null);
            //    return buffer;
            //}
            //else
            //{
            //    //IPolygon buffer = MergePolygons(buffers);
            //    IPolygon buffer = FastMergePolygon(buffers, null, null);
            //    return ClipWrapper.Clip(ClipOperation.Difference, polygon, buffer);
            //}
        }

        internal static IPolygon PointBuffer(IPoint point, double distance)
        {
            Vector2D v1 = new Vector2D(0, distance);
            Vector2D v2 = new Vector2D(0, distance);
            v2.Rotate(-0.0001);

            Ring ring = new Ring();
            AppendCurve(ring, new Vector2D(point.X, point.Y), v1, v2, distance);

            return new Polygon(ring);
        }
        private static void AppendCurve(IPointCollection pColl, Vector2D m, Vector2D v1, Vector2D v2, double distance)
        {
            double from = v1.Angle;
            double to = v2.Angle;
            if (to < from)
            {
                to += 2.0 * Math.PI;
            }

            double step = Math.Min(Math.PI / 5, 1.0 / 10.0 /*/ distance*/);  // 1m toleranz!!!

            for (double a = from; a < to; a += step)
            {
                pColl.AddPoint(new Point(m.X + distance * Math.Cos(a), m.Y + distance * Math.Sin(a)));
            }
            pColl.AddPoint(new Point(m.X + distance * Math.Cos(to), m.Y + distance * Math.Sin(to)));
        }
        #endregion

        #region Merge
        public static IGeometry Merge(IGeometry geom1, IGeometry geom2, bool copy)
        {
            if (geom1 == null && geom2 == null)
            {
                return null;
            }

            if (geom1 == null)
            {
                if (copy)
                {
                    return (IGeometry)geom2.Clone();
                }

                return geom2;
            }
            if (geom2 == null)
            {
                if (copy)
                {
                    return (IGeometry)geom1.Clone();
                }

                return geom1;
            }

            #region IPoint
            if (geom2 is IPoint)
            {
                if (geom1 is IPoint)
                {
                    MultiPoint mp = new MultiPoint();
                    if (copy)
                    {
                        mp.AddPoint((IPoint)geom1.Clone());
                        mp.AddPoint((IPoint)geom1.Clone());
                    }
                    else
                    {
                        mp.AddPoint((IPoint)geom1);
                        mp.AddPoint((IPoint)geom2);
                    }
                    return mp;
                }
                if (geom1 is IMultiPoint)
                {
                    if (copy)
                    {
                        IMultiPoint mp = (IMultiPoint)geom1.Clone();
                        mp.AddPoint((IPoint)geom1.Clone());
                        return mp;
                    }
                    ((IMultiPoint)geom1).AddPoint((IPoint)geom2);
                    return geom1;
                }
            }
            #endregion

            #region IMultiPoint
            if (geom2 is IMultiPoint)
            {
                if (geom1 is IPoint)
                {
                    return Merge(geom2, geom1, copy);
                }

                if (geom1 is IMultiPoint)
                {
                    if (copy)
                    {
                        MultiPoint mp = new MultiPoint((IMultiPoint)geom1);
                        for (int i = 0; i < ((IMultiPoint)geom2).PointCount; i++)
                        {
                            mp.AddPoint(new Point(((IMultiPoint)geom2)[i]));
                        }

                        return mp;
                    }
                    else
                    {
                        for (int i = 0; i < ((IMultiPoint)geom2).PointCount; i++)
                        {
                            ((IMultiPoint)geom1).AddPoint(((IMultiPoint)geom2)[i]);
                        }

                        return geom1;
                    }
                }
            }
            #endregion

            #region IPolyline
            if (geom1 is IPolyline && geom2 is IPolyline)
            {
                if (copy)
                {
                    Polyline line = new Polyline();
                    for (int i = 0; i < ((IPolyline)geom1).PathCount; i++)
                    {
                        line.AddPath(new Path(((IPolyline)geom1)[i]));
                    }

                    for (int i = 0; i < ((IPolyline)geom2).PathCount; i++)
                    {
                        line.AddPath(new Path(((IPolyline)geom2)[i]));
                    }

                    return line;
                }
                else
                {
                    for (int i = 0; i < ((IPolyline)geom2).PathCount; i++)
                    {
                        ((IPolyline)geom1).AddPath(((IPolyline)geom2)[i]);
                    }

                    return geom1;
                }
            }
            #endregion

            #region Polygon
            if (geom1 is IPolygon && geom2 is IPolygon)
            {
                List<IPolygon> polygons = new List<IPolygon>();
                polygons.Add((IPolygon)geom1);
                polygons.Add((IPolygon)geom2);
                return MergePolygons(polygons);
            }
            #endregion

            #region Aggregate
            if (geom1 is IAggregateGeometry && geom2 is IAggregateGeometry)
            {
                if (copy)
                {
                    IAggregateGeometry ag = new AggregateGeometry();
                    for (int i = 0; i < ((IAggregateGeometry)geom1).GeometryCount; i++)
                    {
                        ag.AddGeometry((IGeometry)((IAggregateGeometry)geom1)[i].Clone());
                    }

                    for (int i = 0; i < ((IAggregateGeometry)geom2).GeometryCount; i++)
                    {
                        ag.AddGeometry((IGeometry)((IAggregateGeometry)geom2)[i].Clone());
                    }

                    return ag;
                }
                else
                {
                    for (int i = 0; i < ((IAggregateGeometry)geom2).GeometryCount; i++)
                    {
                        ((IAggregateGeometry)geom1).AddGeometry(((IAggregateGeometry)geom2)[i]);
                    }

                    return geom1;
                }
            }
            else if (geom1 is IAggregateGeometry)
            {
                if (copy)
                {
                    IAggregateGeometry ag = (IAggregateGeometry)geom1.Clone();
                    ag.AddGeometry(geom2);
                    return ag;
                }
                else
                {
                    ((IAggregateGeometry)geom1).AddGeometry(geom2);
                    return geom1;
                }
            }
            else if (geom2 is IAggregateGeometry)
            {
                return Merge(geom2, geom1, copy);
            }
            #endregion

            AggregateGeometry agg = new AggregateGeometry();
            agg.AddGeometry(geom1);
            agg.AddGeometry(geom2);
            return agg;
        }
        #endregion

        #region Distance
        public static Point Point2PolylineDistance(IPolyline polyline, IPoint point, out double dist, out double stat)
        {
            int sign;
            return Point2PolylineDistance(polyline, point, out dist, out stat, out sign);
        }
        public static Point Point2PolylineDistance(IPolyline polyline, IPoint point, out double dist, out double stat, out int sign)
        {
            dist = double.MaxValue;
            double Station = 0.0;
            stat = double.MaxValue;
            double X = 0.0, Y = 0.0;
            double x = point.X, y = point.Y;
            sign = 1;

            if (polyline == null)
            {
                return null;
            }

            try
            {
                for (int p = 0; p < polyline.PathCount; p++)
                {
                    IPath path = polyline[p];
                    if (path == null || path.PointCount == 0)
                    {
                        continue;
                    }

                    double x1, y1, x2, y2, X_, Y_;
                    x1 = path[0].X;
                    y1 = path[0].Y;
                    for (int i = 1; i < path.PointCount; i++)
                    {
                        x2 = path[i].X;
                        y2 = path[i].Y;
                        int si;
                        double d = Point2LineDistance2(x1, y1, x2, y2, x, y, out X_, out Y_, out si);
                        if (d < dist)
                        {
                            dist = d;
                            X = X_;
                            Y = Y_;
                            stat = Station + Math.Sqrt((X - x1) * (X - x1) + (Y - y1) * (Y - y1));
                            sign = si;
                        }
                        Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        x1 = x2; y1 = y2;
                    }
                }
            }
            catch
            {
                return null;
            }

            return (X != 0.0 || Y != 0.0) ? new Point(X, Y) : null;
        }

        public static Point Point2PathDistance(IPath path, IPoint point, out double dist, out double stat)
        {
            double tdx, tdy;
            return Point2PathDistance(path, point, out dist, out stat, out tdx, out tdy);
        }
        public static Point Point2PathDistance(IPath path, IPoint point, out double dist, out double stat, out double tangentDx, out double tangentDy)
        {
            dist = double.MaxValue;
            stat = double.MaxValue;
            tangentDx = tangentDy = 0.0;

            if (path == null || point == null)
            {
                return null;
            }

            double Station = 0.0;
            double X = 0.0, Y = 0.0;
            double x = point.X, y = point.Y;

            double x1, y1, x2, y2, X_, Y_;
            x1 = path[0].X;
            y1 = path[0].Y;
            int pointCount = path.PointCount;
            for (int i = 1; i < pointCount; i++)
            {
                x2 = path[i].X;
                y2 = path[i].Y;
                double d = Point2LineDistanceFast(x1, y1, x2, y2, x, y, out X_, out Y_);
                //double d = Point2LineDistance2(x1, y1, x2, y2, x, y, out X_, out Y_, out sign);
                if (d < dist)
                {
                    dist = d;
                    X = X_;
                    Y = Y_;
                    stat = Station + Math.Sqrt((X - x1) * (X - x1) + (Y - y1) * (Y - y1));

                    tangentDx = x2 - x1;
                    tangentDy = y2 - y1;
                }
                Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                x1 = x2; y1 = y2;
            }

            return (X != 0.0 || Y != 0.0) ? new Point(X, Y) : null;
        }

        public static Point Point2PathDistance2(IPath path, IPoint point, out double dist, out double stat, out double tangentDx, out double tangentDy)
        {
            dist = double.MaxValue;
            stat = double.MaxValue;

            tangentDx = tangentDy = 0.0;

            if (path == null || point == null)
            {
                return null;
            }

            double Station = 0.0;
            double X = 0.0, Y = 0.0;
            double x = point.X, y = point.Y;

            double x1, y1, x2, y2, X_, Y_;
            int sign;
            x1 = path[0].X;
            y1 = path[0].Y;
            int pointCount = path.PointCount;
            for (int i = 1; i < pointCount; i++)
            {
                x2 = path[i].X;
                y2 = path[i].Y;
                //double d = Point2LineDistanceFast(x1, y1, x2, y2, x, y, out X_, out Y_);
                double d = Point2LineDistance2_(x1, y1, x2, y2, x, y, out X_, out Y_, out sign);
                if (d < dist)
                {
                    dist = d;
                    X = X_;
                    Y = Y_;
                    stat = Station + Math.Sqrt((X - x1) * (X - x1) + (Y - y1) * (Y - y1));

                    tangentDx = x2 - x1;
                    tangentDy = y2 - y1;
                }
                Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                x1 = x2; y1 = y2;
            }

            if (tangentDx != 0.0 || tangentDy != 0.0)
            {
                double tangentS = Math.Sqrt(tangentDx * tangentDx + tangentDy * tangentDy);
                tangentDx /= tangentS;
                tangentDy /= tangentS;
            }
            return (X != 0.0 || Y != 0.0) ? new Point(X, Y) : null;
        }

        private static double Point2LineDistance2(double x1, double y1, double x2, double y2, double x, double y, out double X, out double Y, out int sign)
        {
            x -= x1;
            y -= y1;
            double dx = x2 - x1, dy = y2 - y1, x_, y_;
            double a = Math.Atan2(dy, dx), len = Math.Sqrt(dx * dx + dy * dy);
            double c = Math.Cos(a), s = Math.Sin(a);

            x_ = c * x + s * y;
            y_ = -s * x + c * y;

            double dist = 1e10;
            X = 0; Y = 0; sign = 1;
            if (x_ < 0)
            {
                dist = Math.Sqrt(x_ * x_ + y_ * y_);
                X = 0; Y = 0;
            }
            else if (x_ > len)
            {
                dist = Math.Sqrt((x_ - len) * (x_ - len) + y_ * y_);
                X = 0; Y = 0;
            }
            else
            {
                dist = Math.Abs(y_);
                X = x1 + x_ * c;
                Y = y1 + x_ * s;
            }
            sign = y_ < 0 ? -1 : 1;
            return dist;
        }

        private static double Point2LineDistance2_(double x1, double y1, double x2, double y2, double x, double y, out double X, out double Y, out int sign)
        {
            x -= x1;
            y -= y1;
            double dx = x2 - x1, dy = y2 - y1, x_, y_;
            double a = Math.Atan2(dy, dx), len = Math.Sqrt(dx * dx + dy * dy);
            double c = Math.Cos(a), s = Math.Sin(a);

            x_ = c * x + s * y;
            y_ = -s * x + c * y;

            double dist = 1e10;
            X = 0; Y = 0; sign = 1;
            if (x_ < 0)
            {
                dist = Math.Sqrt(x_ * x_ + y_ * y_);
                X = x1; Y = y1;
            }
            else if (x_ > len)
            {
                dist = Math.Sqrt((x_ - len) * (x_ - len) + y_ * y_);
                X = x2; Y = y2;
            }
            else
            {
                dist = Math.Abs(y_);
                X = x1 + x_ * c;
                Y = y1 + x_ * s;
            }
            sign = y_ < 0 ? -1 : 1;
            return dist;
        }

        private static double Point2LineDistanceFast(double x1, double y1, double x2, double y2, double x, double y, out double X, out double Y)
        {
            X = 0; Y = 0;

            // r... Richtungsvektor, r_ ... Normale auc Richtungsvektor
            //  x-x1 = v*rx - w*r_x
            //  y-y1 = v*ry - w*r_y

            // Richtungswinkel
            double rx = x2 - x1, ry = y2 - y1;
            // Normale auf Richtungswinkel
            double r_x, r_y;
            if (Math.Abs(ry) > Math.Abs(rx)) // Größern als negativen verwenden; eigentlich egal sollte nur nicht 0 sein!
            {
                r_x = -ry;
                r_y = rx;
            }
            else
            {
                r_x = ry;
                r_y = -rx;
            }

            LinearEquation2 eq = new LinearEquation2(x - x1, y - y1, rx, -r_x, ry, -r_y);

            if (!eq.Solve())
            {
                return double.MaxValue;
            }

            double v = eq.Var1;
            if (v > 1.0)
            {
                //return double.MaxValue;
                X = x2; Y = y2;
            }
            else if (v < 0.0)
            {
                X = x1; Y = y1;
            }
            else
            {
                X = x1 + rx * v;
                Y = y1 + ry * v;
            }
            double dist = (X - x) * (X - x) + (Y - y) * (Y - y);
            return Math.Sqrt(dist);
        }

        public static double Point2ShapeDistance(IGeometry shape, IPoint point)
        {
            int sign;
            return Point2ShapeDistance(shape, point, out sign);
        }
        public static double Point2ShapeDistance(IGeometry shape, IPoint point, out int sign)
        {
            sign = 1;
            if (shape == null || point == null)
            {
                return double.MaxValue;
            }

            if (shape is Point)
            {
                return ((Point)shape).Distance2D(point);
            }
            else if (shape is IPolyline)
            {
                double dist, stat;
                Point2PolylineDistance((Polyline)shape, point, out dist, out stat, out sign);
                return dist;
            }
            else if (shape is IPolygon)
            {
                var polygon = (Polygon)shape;
                if (Jordan(polygon, point.X, point.Y))
                {
                    return 0D;
                }

                double dist = double.MaxValue;
                foreach (var ring in polygon.Rings)
                {
                    ring.ClosePath();

                    dist = Math.Min(Point2ShapeDistance(ring.ToPolyline(), point, out sign), dist);
                }
                return dist;
            }
            else if (shape is IPointCollection)
            {
                double ret = double.MaxValue;
                for (int i = 0; i < ((PointCollection)shape).PointCount; i++)
                {
                    IPoint p = ((IPointCollection)shape)[i];
                    double dist = p.Distance2D(point);
                    if (dist < ret)
                    {
                        ret = dist;
                    }
                }
                return ret;
            }

            return double.MaxValue;
        }


        #endregion

        #region Stat
        public static Polyline PolylineSplit(IPolyline polyline, double from, double to)
        {
            if (polyline == null)
            {
                return null;
            }

            Polyline polylinePart = new Polyline();
            bool firstPointFound = false, lastPointFound = false;

            double Station = 0.0;
            for (int p = 0; p < polyline.PathCount; p++)
            {
                IPath path = polyline[p];
                if (path == null || path.PointCount == 0)
                {
                    continue;
                }

                PointCollection pColl = new PointCollection();

                double x1, y1, x2, y2;
                x1 = path[0].X;
                y1 = path[0].Y;
                for (int i = 0; i < path.PointCount; i++)
                {
                    x2 = path[i].X;
                    y2 = path[i].Y;

                    Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    if (Station >= from && Station <= to)
                    {
                        if (!firstPointFound)
                        {
                            pColl.AddPoint(PolylinePoint(polyline, from));
                            firstPointFound = true;
                        }
                        pColl.AddPoint(new Point(x2, y2));
                    }
                    else if (Station > to)
                    {
                        if (!lastPointFound)
                        {
                            pColl.AddPoint(PolylinePoint(polyline, to));
                            lastPointFound = true;
                            break;
                        }
                    }
                    x1 = x2; y1 = y2;
                }
                /*
                if (Station >= from && Station <= to)
                {
                    if (!firstPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, from));
                        firstPointFound = true;
                    }
                    pColl.AddPoint(new Point(x1, y1));
                }
                else if (Station > to)
                {
                    if (!lastPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, to));
                        lastPointFound = true;
                        break;
                    }
                }
                else*/
                if (Station <= to && !lastPointFound)
                {
                    pColl.AddPoint(new Point(x1, y1));
                }

                //if (Station >= from && Station <= to && lastPointFound == false)
                //    pColl.AddPoint(new Point(x1, y1));

                if (pColl.PointCount > 0)
                {
                    polylinePart.AddPath(new Path(RemoveDoubles(pColl)));
                }

                if (lastPointFound == true)
                {
                    break;
                }
            }

            return polylinePart.PathCount > 0 ? polylinePart : null;
        }
        public static Point PolylinePoint(IPolyline polyline, double stat)
        {
            if (polyline == null)
            {
                return null;
            }

            double Station = 0.0, Station0 = 0.0;
            for (int p = 0; p < polyline.PathCount; p++)
            {
                IPath path = polyline[p];
                if (path == null || path.PointCount == 0)
                {
                    continue;
                }

                Path newPath = new Path();

                double x1, y1, x2, y2;
                x1 = path[0].X;
                y1 = path[0].Y;
                for (int i = 1; i < path.PointCount; i++)
                {
                    x2 = path[i].X;
                    y2 = path[i].Y;

                    Station0 = Station;
                    Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    if (Station >= stat)
                    {
                        double t = stat - Station0;
                        double l = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        double dx = (x2 - x1) / l, dy = (y2 - y1) / l;
                        return new Point(x1 + dx * t, y1 + dy * t);
                    }

                    x1 = x2; y1 = y2;
                }
            }

            return null;
        }
        public static PointCollection PathPoints(IPath path, double fromStat, double toStat, double step, bool createPointM2 = false)
        {
            PointCollection pColl = new PointCollection();

            if (step > double.Epsilon)
            {
                for (double s = fromStat; s <= toStat; s += step)
                {
                    IPoint p = PathPoint(path, s, createPointM2);
                    if (p != null)
                    {
                        pColl.AddPoint(p);
                    }
                }
            }

            return pColl;
        }

        private static Point PathPoint(IPath path, double station, bool createPointM2 = false)
        {
            if (station < 0.0)
            {
                return null;
            }

            double x1, y1, x2, y2, stat = 0;
            x1 = path[0].X;
            y1 = path[0].Y;

            for (int i = 1, pointCount = path.PointCount; i < pointCount; i++)
            {
                x2 = path[i].X;
                y2 = path[i].Y;

                double stat0 = stat;
                stat += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                if (stat >= station)
                {
                    double t = station - stat0;
                    double l = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double dx = (x2 - x1) / l, dy = (y2 - y1) / l;

                    if (createPointM2 == true)
                    {
                        //
                        // -dy!! => this value is used for DisplyRotation (eg SymbolDotedLineSymbol)
                        // geographic Y and Display Y has different Directions!!
                        //
                        return new PointM2(x1 + dx * t, y1 + dy * t, stat0 + t, Math.Atan2(-dy, dx));
                    }

                    return new Point(x1 + dx * t, y1 + dy * t);
                }
                x1 = x2; y1 = y2;
            }

            return null;
        }

        private static PointCollection RemoveDoubles(IPointCollection pColl)
        {
            PointCollection newColl = new PointCollection();
            if (pColl.PointCount == 0)
            {
                return newColl;
            }

            newColl.AddPoint(new Point(pColl[0]));
            for (int i = 1; i < pColl.PointCount; i++)
            {
                if (newColl[newColl.PointCount - 1].Equals(pColl[i]) == false)
                {
                    newColl.AddPoint(pColl[i]);
                }
            }

            return newColl;
        }
        #endregion

        #region Offset
        static public IPath CalcOffsetPath(IPath path, double offset)
        {
            double ofac = (offset >= 0.0 ? -1.0 : 1.0);
            offset = Math.Abs(offset);
            double o = offset;
            Path offsetPath = null;

            int interation = 0;

            while (o > 0.0)
            {
                #region Doppelte Punkte suchen und löschen
                PointCollection shapePoints = new PointCollection();

                for (int i = 0, pCount = path.PointCount; i < pCount; i++)
                {
                    IPoint p = path[i];
                    if (shapePoints.PointCount == 0)
                    {
                        shapePoints.AddPoint(p);
                    }
                    else
                    {
                        if (p.Distance(shapePoints[shapePoints.PointCount - 1]) > double.Epsilon)
                        {
                            shapePoints.AddPoint(p);
                        }
                    }
                }
                #endregion
                offsetPath = new Path();

                #region Strecken
                double[] s = new double[shapePoints.PointCount - 1];
                for (int i = 1; i < shapePoints.PointCount; i++)
                {
                    Vector2d v = new Vector2d(shapePoints[i].X - shapePoints[i - 1].X,
                                              shapePoints[i].Y - shapePoints[i - 1].Y);
                    s[i - 1] = v.VectorNorm;
                }
                #endregion

                #region Winkel zwischen den Strecken
                double[] a = new double[shapePoints.PointCount - 2];
                for (int i = 1; i < shapePoints.PointCount - 1; i++)
                {
                    Vector2d v1 = new Vector2d(shapePoints[i].X - shapePoints[i - 1].X,
                                               shapePoints[i].Y - shapePoints[i - 1].Y);
                    Vector2d v2 = new Vector2d(shapePoints[i + 1].X - shapePoints[i].X,
                                               shapePoints[i + 1].Y - shapePoints[i].Y);
                    v1.Normalize(); v2.Normalize();
                    double cosa = v1 * v2;
                    double sina = v1 % v2;
                    double a_ = Math.Atan2(-sina, cosa); // -sina, weil Hochwert nach oben positiv
                    a[i - 1] = Math.PI + a_ * ofac;
                }
                #endregion

                #region h (=maximal berechenbarer Offset) ermitteln
                double h = double.MaxValue;
                for (int i = 1; i < s.Length; i++)
                {
                    if (a[i - 1] >= Math.PI ||
                        Math.Abs(Math.Abs(a[i - 1] / 2.0) - Math.PI / 2.0) <= 1e-8)
                    {
                        continue;
                    }

                    h = Math.Min(h, Math.Abs(Math.Max(s[i - 1], s[i]) * Math.Tan(a[i - 1] / 2.0)));
                    //h = Math.Min(h, Math.Abs(s[i - 1] * Math.Tan(a[i - 1] / 2.0)));
                    //h = Math.Min(h, Math.Abs(s[i] * Math.Tan(a[i - 1] / 2.0)));
                }
                if (h == 0.0)
                {
                    return null;
                }
                #endregion

                o = Math.Min(h, o);
                #region Offset-Punkte berechnen
                List<Point> offsetPoints = new List<Point>();
                for (int i = 0; i < shapePoints.PointCount - 1; i++)
                {
                    Vector2d v = new Vector2d(shapePoints[i + 1].Y - shapePoints[i].Y,
                                              -(shapePoints[i + 1].X - shapePoints[i].X));
                    v.Normalize();
                    offsetPoints.Add(new Point(shapePoints[i].X + v[0] * o * ofac * -1.0, shapePoints[i].Y + v[1] * o * ofac * -1.0));
                    offsetPoints.Add(new Point(shapePoints[i + 1].X + v[0] * o * ofac * -1.0, shapePoints[i + 1].Y + v[1] * o * ofac * -1.0));
                }
                #endregion

                #region Kontenpunkte verschneiden
                int add = -1;
                List<Point> nPoints = new List<Point>();
                nPoints.Add(offsetPoints[0]);
                for (int i = 1; i < shapePoints.PointCount - 1; i++)
                {
                    Point v = Algorithm.IntersectLine(offsetPoints[i + add],
                                                       offsetPoints[i + add + 1],
                                                       offsetPoints[i + add + 2],
                                                       offsetPoints[i + add + 3], false);
                    add++;
                    if (v == null)
                    {
                        continue;
                    }
                    //return null;
                    nPoints.Add(v);
                }
                nPoints.Add(offsetPoints[offsetPoints.Count - 1]);
                #endregion

                #region Self Intersection
                offsetPath.RemoveAllPoints();
                for (int i = 0; i < nPoints.Count - 1; i++)
                {
                    offsetPath.AddPoint(nPoints[i]);
                    //Point iPoint = null;
                    //int ni = -1;
                    //for (int j = i + 2; j < nPoints.Count - 1; j++)
                    //{
                    //    Point p = Algorithm.IntersectLine(nPoints[i], nPoints[i + 1], nPoints[j], nPoints[j + 1], true);
                    //    if (p != null)
                    //    {
                    //        iPoint = p;
                    //        ni = j;
                    //    }
                    //}
                    //if (ni != -1)
                    //{
                    //    nPoints[ni] = iPoint;
                    //    i = ni - 1;
                    //}
                }
                offsetPath.AddPoint(nPoints[nPoints.Count - 1]);

                #endregion
                offset = offset - o;
                o = offset;

                interation++;
                if (interation > 50)
                {
                    break;
                }

                path = offsetPath;
            }

            return offsetPath;
        }
        #endregion
    }

    internal class SimpleLineSegment
    {
        private IPoint _p1 = null, _p2 = null;

        public SimpleLineSegment(IPoint p1, IPoint p2)
        {
            _p1 = p1;
            _p2 = p2;
        }

        public double Length
        {
            get
            {
                if (_p1 == null || _p2 == null)
                {
                    return 0.0;
                }

                return Math.Sqrt((_p1.X - _p2.X) * (_p1.X - _p2.X) + (_p1.Y - _p2.Y) * (_p1.Y - _p2.Y));
            }
        }

        public IPoint Center
        {
            get
            {
                if (_p1 == null || _p2 == null)
                {
                    return null;
                }

                return new Point(
                    (_p1.X + _p2.X) * 0.5,
                    (_p1.Y + _p2.Y) * 0.5);
            }
        }

        public IPolygon Buffer(double distance)
        {
            if (_p1 == null || _p2 == null)
            {
                return null;
            }

            List<IPoint> points = new List<IPoint>();

            points.Add(new Point(0, -distance));
            BufferCurve(distance, Length, -Math.PI / 2, Math.PI / 2, ref points);
            //BufferCurve(distance, 0, Math.PI / 2.0, 1.5 * Math.PI, ref points);
            points.Add(new Point(0, distance));

            double angle = Math.Atan2(_p2.Y - _p1.Y, _p2.X - _p1.X);

            Ring ring = new Ring();
            foreach (IPoint point in points)
            {
                ring.AddPoint(point);
            }

            Algorithm.Rotate(angle, ring);
            Algorithm.Translate(_p1.X, _p1.Y, ring);

            Polygon polygon = new Polygon();
            polygon.AddRing(ring);

            return polygon;
        }

        private void BufferCurve(double distance, double mx, double from, double to, ref List<IPoint> points)
        {
            if (points == null)
            {
                return;
            }

            double step = Math.Min(Math.PI / 5, 1.0 / distance);  // 1m toleranz!!!
            step = Math.PI / 5.0;
            for (double a = from; a < to; a += step)
            {
                points.Add(new Point(
                    mx + distance * Math.Cos(a),
                    distance * Math.Sin(a)));
            }
            points.Add(new Point(
                    mx + distance * Math.Cos(to),
                    distance * Math.Sin(to)));
        }
    }

    internal class PointComparerX : IComparer<IPoint>
    {
        #region IComparer<IPoint> Member

        public int Compare(IPoint x, IPoint y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            if (x.X < y.X)
            {
                return -1;
            }

            if (x.X > y.X)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }

    internal class PointComparerY : IComparer<IPoint>
    {
        #region IComparer<IPoint> Member

        public int Compare(IPoint x, IPoint y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            if (x.Y < y.Y)
            {
                return -1;
            }

            if (x.Y > y.Y)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }

    internal class SimpleLineSegmentComparerLength : IComparer<SimpleLineSegment>
    {
        #region IComparer<SimpleLineSegment> Member

        public int Compare(SimpleLineSegment x, SimpleLineSegment y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            double len1 = x.Length;
            double len2 = y.Length;

            if (len1 < len2)
            {
                return -1;
            }

            if (len1 > len2)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }

    internal class SimpleLineSegmentComparerLengthInv : IComparer<SimpleLineSegment>
    {
        #region IComparer<SimpleLineSegment> Member

        public int Compare(SimpleLineSegment x, SimpleLineSegment y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            double len1 = x.Length;
            double len2 = y.Length;

            if (len1 < len2)
            {
                return 1;
            }

            if (len1 > len2)
            {
                return -1;
            }

            return 0;
        }

        #endregion
    }

    internal class RingComparerArea : IComparer<IRing>
    {
        #region IComparer<IRing> Member

        public int Compare(IRing x, IRing y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            double A1 = x.Area;
            double A2 = y.Area;

            if (A1 < A2)
            {
                return -1;
            }

            if (A1 > A2)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }

    internal class RingComparerAreaInv : IComparer<IRing>
    {
        #region IComparer<IRing> Member

        public int Compare(IRing x, IRing y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            double A1 = x.Area;
            double A2 = y.Area;

            if (A1 < A2)
            {
                return 1;
            }

            if (A1 > A2)
            {
                return -1;
            }

            return 0;
        }

        #endregion
    }
}

namespace gView.Framework.Geometry
{
    public class SpatialRelation
    {
        static public bool Check(ISpatialFilter filter, IGeometry geom)
        {
            if (filter == null || filter.Geometry == null || geom == null)
            {
                return false;
            }

            switch (filter.SpatialRelation)
            {
                case spatialRelation.SpatialRelationEnvelopeIntersects:
                    return filter.Geometry.Envelope.Intersects(geom.Envelope);
                case spatialRelation.SpatialRelationIntersects:
                    return SpatialAlgorithms.Algorithm.Intersects(filter.Geometry, geom);
                case spatialRelation.SpatialRelationContains:
                    return SpatialAlgorithms.Algorithm.Contains(filter.Geometry, geom);
                case spatialRelation.SpatialRelationWithin:
                    return SpatialAlgorithms.Algorithm.Contains(geom, filter.Geometry);
                case spatialRelation.SpatialRelationMapEnvelopeIntersects:
                    return SpatialAlgorithms.Algorithm.IntersectBox(geom, filter.Geometry.Envelope);
            }
            return false;
        }
    }
}
