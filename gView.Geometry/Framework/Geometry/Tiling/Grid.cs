using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Framework.Geometry.Tiling
{
    public enum GridOrientation
    {
        LowerLeft = 0,
        UpperLeft = 1
    }

    public enum TileImageFormat { png, jpg };

    public class Grid
    {
        private List<GridLevel> _levels = new List<GridLevel>();

        public Grid(IPoint origin, int tileSizeX, int tileSizeY, double dpi, GridOrientation orientation)
        {
            _origin = origin;
            _tileSizeX = tileSizeX;
            _tileSizeY = tileSizeY;
            _dpi = dpi;
            _orientation = orientation;
        }

        #region Properties

        private IPoint _origin;
        public IPoint Origin
        {
            get { return _origin; }
        }

        private double _dpi;
        public double Dpi
        {
            get { return _dpi; }
        }

        private GridOrientation _orientation;
        public GridOrientation Orientation
        {
            get { return _orientation; }
        }

        private int _tileSizeX;
        public int TileSizeX
        {
            get { return _tileSizeX; }
        }

        private int _tileSizeY;
        public int TileSizeY
        {
            get { return _tileSizeY; }
        }

        public int MinResolutionLevel
        {
            get
            {
                if (_levels.Count == 0)
                {
                    return -1;
                }

                double min = _levels[0].Resolution;
                int l = _levels[0].Level;

                foreach (GridLevel level in _levels)
                {
                    if (level.Resolution < min)
                    {
                        l = level.Level;
                        min = level.Resolution;
                    }
                }

                return l;
            }
        }

        public int MaxResolutionLevel
        {
            get
            {
                if (_levels.Count == 0)
                {
                    return -1;
                }

                double max = _levels[0].Resolution;
                int l = _levels[0].Level;

                foreach (GridLevel level in _levels)
                {
                    if (level.Resolution > max)
                    {
                        l = level.Level;
                        max = level.Resolution;
                    }
                }

                return l;
            }
        }

        public string Quadkey(IEnvelope bounds, int level, int row, int col, double res)
        {
            StringBuilder sb = new StringBuilder();

            IPoint tilePoint = TileUpperLeft(row, col, res);
            IPoint tileCPoint = new Point(tilePoint.X + TileWidth(res) / 2.0, tilePoint.Y - TileHeight(res) / 2.0);

            for (int l = 0; l <= level; l++)
            {
                int key = Quadkey(bounds, tileCPoint);
                sb.Append(key.ToString());
                bounds = QuadPart(bounds, key);
            }

            return sb.ToString();
        }

        private int Quadkey(IEnvelope quad, IPoint tileCenterPoint)
        {
            int key = 0;

            IPoint center = quad.Center;

            if (tileCenterPoint.X > center.X)
            {
                key += 1;
            }

            if (tileCenterPoint.Y < center.Y)
            {
                key += 2;
            }

            return key;
        }

        private IEnvelope QuadPart(IEnvelope quad, int key)
        {
            switch (key)
            {
                case 0:
                    return new Envelope(quad.minx, quad.miny + quad.Height / 2.0,
                                        quad.minx + quad.Width / 2.0, quad.maxy);
                case 1:
                    return new Envelope(quad.minx + quad.Width / 2.0, quad.miny + quad.Height / 2.0,
                                        quad.maxx, quad.maxy);
                case 2:
                    return new Envelope(quad.minx, quad.miny,
                                        quad.minx + quad.Width / 2.0, quad.miny + quad.Height / 2.0);
                case 3:
                    return new Envelope(quad.minx + quad.Width / 2.0, quad.miny,
                                        quad.maxx, quad.miny + quad.Height / 2.0);
            }

            throw new ArgumentException("Quadkey must be [0..3]");
        }
        #endregion

        #region Members

        #region Level Members
        public void AddLevel(int level, double resolution)
        {
            _levels.Add(new GridLevel(level, resolution));
        }
        public void AddLevel(Point origin, int level, double resolution)
        {
            _levels.Add(new GridLevel(origin, level, resolution));
        }

        public int GetBestLevel(double mapResolution)
        {
            if (_levels.Count == 0)
            {
                return -1;
            }

            double res = double.MinValue;
            int l = -1;

            foreach (GridLevel level in _levels)
            {
                if (level.Resolution < mapResolution &&
                    level.Resolution > res)
                {
                    res = level.Resolution;
                    l = level.Level;
                }
            }

            return (l >= 0) ? l : this.MinResolutionLevel;
        }

        public int GetBestLevel(double mapResolution, double percent)
        {
            if (_levels.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < _levels.Count - 1; i++)
            {
                GridLevel level = _levels[i];
                if (mapResolution >= (level.Resolution - (level.Resolution - _levels[i + 1].Resolution) * percent / 100D))
                {
                    return level.Level;
                }
            }

            return this.MinResolutionLevel;
        }

        public int GetNextLowerLevel(int level)
        {
            if (level < 0)
            {
                return -1;
            }

            int ret = -1;
            double levelRes = GetLevelResolution(level), res = double.MaxValue;
            foreach (GridLevel l in _levels)
            {
                if (l.Resolution > levelRes && levelRes < res)
                {
                    ret = l.Level;
                    res = l.Resolution;
                }
            }

            return ret;
        }

        public double GetLevelResolution(int level)
        {
            foreach (GridLevel l in _levels)
            {
                if (l.Level == level)
                {
                    return l.Resolution;
                }
            }

            return double.MinValue;
        }
        #endregion

        #region Tile Members
        public double TileWidth(double res)
        {
            return _tileSizeX * res;
        }
        public double TileHeight(double res)
        {
            return _tileSizeY * res;
        }

        public int TileColumn(double x, double res)
        {
            return (int)Math.Floor((x - _origin.X) / TileWidth(res));
        }
        public int TileRow(double y, double res)
        {

            return (int)Math.Floor((y - _origin.Y) * (_orientation == GridOrientation.UpperLeft ? -1.0 : 1.0) / TileWidth(res));
        }

        public Point TileUpperLeft(int row, int col, double res)
        {
            double x = _origin.X + col * TileWidth(res);
            double y = 0.0;
            if (_orientation == GridOrientation.UpperLeft)
            {
                y = _origin.Y - row * TileHeight(res);
            }
            else
            {
                y = _origin.Y + (row + 1) * TileHeight(res);
            }

            return new Point(x, y);
        }
        #endregion

        #endregion

        #region Helperclasses
        private class GridLevel
        {
            public GridLevel(int level, double resolution)
            {
                _level = level;
                _resolution = resolution;
            }
            public GridLevel(Point origin, int level, double resolution)
                : this(level, resolution)
            {
                _origin = origin;
            }

            #region Properties
            private int _level;
            public int Level
            {
                get { return _level; }
                //set { _level = value; }
            }

            private double _resolution;
            public double Resolution
            {
                get { return _resolution; }
                //set { _resolution = value; }
            }

            private Point _origin = null;
            public Point Origin
            {
                get { return _origin; }
            }
            #endregion
        }
        #endregion
    }
}
