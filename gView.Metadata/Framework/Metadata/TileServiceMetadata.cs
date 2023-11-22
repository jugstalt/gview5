using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace gView.Framework.Metadata
{
    [RegisterPlugInAttribute("D33D3DD2-DD63-4a47-9F84-F840FE0D01C0")]
    public class TileServiceMetadata : IMetadataProvider, IPropertyPage, IPropertyModel
    {
        private PropertyObject _properties = new PropertyObject();

        public TileServiceMetadata()
        {
            this.MakeTransparentPng = false;
        }

        #region Properties

        public bool Use
        {
            get { return _properties.Use; }
            set { _properties.Use = value; }
        }

        public List<int> EPSGCodes
        {
            get { return _properties.EpsgCodes; }
        }
        public IEnvelope GetEPSGEnvelope(int epsg)
        {
            if (!_properties.Extents.ContainsKey(epsg))
            {
                return null;
            }

            return _properties.Extents[epsg].ToEnvelope();
        }
        public void SetEPSGEnvelope(int epsg, IEnvelope envelope)
        {
            if (envelope == null)
            {
                envelope = new Envelope();
            }

            if (_properties.Extents.ContainsKey(epsg))
            {
                _properties.Extents[epsg] = new PropertyObject.EnvelopeModel(envelope);
            }
            else
            {
                _properties.Extents.Add(epsg, new PropertyObject.EnvelopeModel(envelope));
            }
        }

        public IPoint GetOriginUpperLeft(int epsg)
        {
            if (_properties.OriginUpperLeft.ContainsKey(epsg))
            {
                return _properties.OriginUpperLeft[epsg].ToPoint();
            }

            var envelope = GetEPSGEnvelope(epsg);
            if (envelope != null)
            {
                return new Point(envelope.minx, envelope.maxy);
            }

            return null;
        }
        public IPoint GetOriginLowerLeft(int epsg)
        {
            if (_properties.OriginLowerLeft.ContainsKey(epsg))
            {
                return _properties.OriginLowerLeft[epsg].ToPoint();
            }

            var envelope = GetEPSGEnvelope(epsg);
            if (envelope != null)
            {
                return new Point(envelope.minx, envelope.miny);
            }

            return null;
        }
        public void SetOriginUpperLeft(int epsg, IPoint upperLeft)
        {
            if (_properties.OriginUpperLeft.ContainsKey(epsg))
            {
                _properties.OriginUpperLeft[epsg] = new PropertyObject.PointModel(upperLeft);
            }
            else
            {
                _properties.OriginUpperLeft.Add(epsg, new PropertyObject.PointModel(upperLeft));
            }
        }
        public void SetOriginLowerLeft(int epsg, IPoint lowerLeft)
        {
            if (_properties.OriginLowerLeft.ContainsKey(epsg))
            {
                _properties.OriginLowerLeft[epsg] = new PropertyObject.PointModel(lowerLeft);
            }
            else
            {
                _properties.OriginLowerLeft.Add(epsg, new PropertyObject.PointModel(lowerLeft));
            }
        }

        public double Dpi
        {
            get
            {
                return _properties.Dpi;
            }
            set
            {
                _properties.Dpi = value;
            }
        }

        private DoubleScales _doubleScales = null;
        public DoubleScales Scales
        {
            get
            {
                if (_doubleScales == null)
                {
                    _doubleScales = new DoubleScales(_properties.Scales);
                }

                return _doubleScales;
            }
            set
            {
                _properties.Scales.Clear();

                if (value == null)
                {
                    _doubleScales = null;
                }
                else
                {
                    _properties.Scales.AddRange(_doubleScales.InnerList);
                    _doubleScales = new DoubleScales(_properties.Scales);
                    _doubleScales.Order();
                }
            }
        }
        public int TileWidth
        {
            get { return _properties.TileWidth; }
            set { _properties.TileWidth = value; }
        }
        public int TileHeight
        {
            get { return _properties.TileHeight; }
            set { _properties.TileHeight = value; }
        }

        public bool UpperLeft
        {
            get { return _properties.UseUpperLeft; }
            set { _properties.UseUpperLeft = value; }
        }
        public bool LowerLeft
        {
            get { return _properties.UseLowerLeft; }
            set { _properties.UseLowerLeft = value; }
        }

        public bool UpperLeftCacheTiles
        {
            get { return _properties.CacheUpperLeftTiles; }
            set { _properties.CacheUpperLeftTiles = value; }
        }
        public bool LowerLeftCacheTiles
        {
            get { return _properties.CacheLowerLeftTiles; }
            set { _properties.CacheLowerLeftTiles = value; }
        }

        public bool FormatPng
        {
            get { return _properties.SupportPng; }
            set { _properties.SupportPng = value; }
        }
        public bool FormatJpg
        {
            get { return _properties.SupportJpg; }
            set { _properties.SupportJpg = value; }
        }

        public bool MakeTransparentPng { get; set; }

        public bool RenderTilesOnTheFly { get; set; }

        #endregion

        #region IMetadataProvider Member

        public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            if (Object == null)
            {
                return Task.FromResult(false);
            }

            if (setDefaults)
            {
                if (_properties.Scales?.Count == 0)
                {
                    _properties.Scales.Add(0);
                }
                if (_properties.EpsgCodes?.Count == 0)
                {
                    _properties.EpsgCodes.Add(0);
                }
                if (_properties.Extents?.Count == 0)
                {
                    _properties.Extents.Add(0, new PropertyObject.EnvelopeModel());
                }
                if (_properties.OriginLowerLeft?.Count == 0)
                {
                    _properties.OriginLowerLeft.Add(0, new PropertyObject.PointModel());
                }
                if (_properties.OriginUpperLeft?.Count == 0)
                {
                    _properties.OriginUpperLeft.Add(0, new PropertyObject.PointModel());
                }
            }

            return Task.FromResult(Object is IMap);
            //return Task.FromResult(Object.GetType().ToString() == "gView.Server.AppCode.ServiceMap");
        }

        public string Name
        {
            get { return "Tile Service Properties"; }
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _properties.Use = (bool)stream.Load("use", false);

            _properties.TileWidth = (int)stream.Load("tile_width", 256);
            _properties.TileHeight = (int)stream.Load("tile_height", 256);

            _properties.UseUpperLeft = (bool)stream.Load("upperleft", true);
            _properties.UseLowerLeft = (bool)stream.Load("lowerleft", true);
            _properties.CacheUpperLeftTiles = (bool)stream.Load("upperleftcachetiles", true);
            _properties.CacheLowerLeftTiles = (bool)stream.Load("lowerleftcachetiles", true);

            _properties.SupportPng = (bool)stream.Load("formatpng", true);
            _properties.SupportJpg = (bool)stream.Load("formatjpg", true);
            this.MakeTransparentPng = (bool)stream.Load("maketransparentpng", false);

            _properties.Dpi = (double)stream.Load("dpi", Const.TilesDpi);

            this.RenderTilesOnTheFly = (bool)stream.Load("rendertilesonthefly", false);

            _properties.Scales.Clear();
            int scales_count = (int)stream.Load("scales_count", 0);
            for (int i = 0; i < scales_count; i++)
            {
                double scale = Convert.ToDouble(stream.Load("scale" + i, 0.0));
                if (scale <= 0.0)
                {
                    continue;
                }

                _properties.Scales.Add(scale);
            }

            _properties.Extents.Clear();
            _properties.EpsgCodes.Clear();
            _properties.OriginUpperLeft.Clear();
            _properties.OriginLowerLeft.Clear();
            int epsg_count = (int)stream.Load("epsg_count", 0);
            for (int i = 0; i < epsg_count; i++)
            {
                int epsg = (int)stream.Load("epsg" + i, 0);
                if (epsg == 0 || _properties.Extents.ContainsKey(epsg))
                {
                    continue;
                }

                _properties.EpsgCodes.Add((int)stream.Load("epsg" + i, epsg));
                _properties.Extents.Add(epsg,
                             new PropertyObject.EnvelopeModel(new Envelope(
                    (double)stream.Load("extent_minx" + i, 0.0),
                    (double)stream.Load("extent_miny" + i, 0.0),
                    (double)stream.Load("extent_maxx" + i, 0.0),
                    (double)stream.Load("extent_maxy" + i, 0.0))));

                _properties.OriginUpperLeft.Add(epsg, new PropertyObject.PointModel(new Point(
                    (double)stream.Load("origin_ul_x" + i, GetOriginUpperLeft(epsg)?.X ?? 0D),
                    (double)stream.Load("origin_ul_y" + i, GetOriginUpperLeft(epsg)?.Y ?? 0D)
                    )));

                _properties.OriginLowerLeft.Add(epsg, new PropertyObject.PointModel(new Point(
                    (double)stream.Load("origin_ll_x" + i, GetOriginLowerLeft(epsg)?.X ?? 0D),
                    (double)stream.Load("origin_ll_y" + i, GetOriginLowerLeft(epsg)?.Y ?? 0D)
                    )));
            }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("use", _properties.Use);

            stream.Save("tile_width", _properties.TileWidth);
            stream.Save("tile_height", _properties.TileHeight);

            stream.Save("upperleft", _properties.UseUpperLeft);
            stream.Save("lowerleft", _properties.UseLowerLeft);
            stream.Save("upperleftcachetiles", _properties.CacheUpperLeftTiles);
            stream.Save("lowerleftcachetiles", _properties.CacheLowerLeftTiles);

            stream.Save("formatpng", _properties.SupportPng);
            stream.Save("formatjpg", _properties.SupportJpg);
            stream.Save("maketransparentpng", this.MakeTransparentPng);

            stream.Save("rendertilesonthefly", this.RenderTilesOnTheFly);

            stream.Save("dpi", _properties.Dpi);

            this.Scales.Order();
            stream.Save("scales_count", this.Scales.InnerList.Count);
            int counter = 0;
            foreach (double scale in _properties.Scales)
            {
                if (scale <= 0.0)
                {
                    continue;
                }

                stream.Save("scale" + counter, scale);
                counter++;
            }

            stream.Save("epsg_count", _properties.EpsgCodes.Count);
            counter = 0;
            for (int i = 0; i < _properties.EpsgCodes.Count; i++)
            {
                int epsg = _properties.EpsgCodes[i];
                if (epsg <= 0)
                {
                    continue;
                }

                stream.Save("epsg" + counter, epsg);

                IEnvelope extent = _properties.Extents.ContainsKey(epsg) ? _properties.Extents[epsg].ToEnvelope() : new Envelope();
                stream.Save("extent_minx" + counter, extent.minx);
                stream.Save("extent_miny" + counter, extent.miny);
                stream.Save("extent_maxx" + counter, extent.maxx);
                stream.Save("extent_maxy" + counter, extent.maxy);

                IPoint ul = GetOriginUpperLeft(epsg) != null ? GetOriginUpperLeft(epsg) : new Point();
                stream.Save("origin_ul_x" + counter, ul.X);
                stream.Save("origin_ul_y" + counter, ul.Y);

                IPoint ll = GetOriginLowerLeft(epsg) != null ? GetOriginLowerLeft(epsg) : new Point();
                stream.Save("origin_ll_x" + counter, ll.X);
                stream.Save("origin_ll_y" + counter, ll.Y);

                counter++;
            }
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Metadata.UI.dll");

            IPlugInParameter p = uiAssembly.CreateInstance("gView.Framework.Metadata.UI.TileServiceMetadataControl") as IPlugInParameter;
            if (p != null)
            {
                p.Parameter = this;
            }

            return p;
        }

        public object PropertyPageObject()
        {
            return this;
        }

        #endregion

        #region Static Members

        public static string TilePath(GridOrientation orientation, int epsg, double scale, int row, int col)
        {
            return
                (orientation == GridOrientation.UpperLeft ? "ul" : "ll") + @"/" +
                epsg + @"/" + (int)Math.Round(scale) + @"/" + row + @"/" + col;
        }

        public static string ScalePath(GridOrientation orientation, int epsg, double scale)
        {
            return
                (orientation == GridOrientation.UpperLeft ? "ul" : "ll") + @"/" +
                epsg + @"/" + (int)Math.Round(scale) + @"/";
        }

        public static string EpsgPath(GridOrientation orientation, int epsg)
        {
            return
                (orientation == GridOrientation.UpperLeft ? "ul" : "ll") + @"/" +
                epsg + @"/";
        }

        #endregion

        #region IPropertyModel

        public Type PropertyModelType => typeof(PropertyObject);

        public object GetPropertyModel()
        {
            return _properties;
        }

        public void SetPropertyModel(object propertyObject)
        {
            if (propertyObject is PropertyObject)
            {
                _properties = (PropertyObject)propertyObject;
            }
        }

        #endregion

        #region Classes

        public class DoubleScales
        {
            private readonly List<double> _scales;
            public DoubleScales(List<double> scales)
            {
                _scales = scales;
            }

            public List<double> InnerList => _scales;

            public bool Contains(double scale)
            {
                double scaleR = Math.Round(scale);
                double scaleF = Math.Floor(scale);

                foreach (double s in _scales)
                {
                    if (Math.Round(s) == scaleR ||
                        Math.Floor(s) == scaleF)
                    {
                        return true;
                    }
                }
                return false;
            }

            public double GetScale(double scale)
            {
                double scaleR = Math.Round(scale);
                double scaleF = Math.Floor(scale);

                foreach (double s in _scales)
                {
                    if (Math.Round(s) == scaleR ||
                        Math.Floor(s) == scaleF)
                    {
                        return s;
                    }
                }
                return -1.0;
            }

            public void Order()
            {
                _scales.Sort();
                List<double> scales = ListOperations<double>.Swap(_scales);

                _scales.Clear();
                foreach (double s in scales)
                {
                    _scales.Add(s);
                }
            }
        }

        private class PropertyObject
        {
            public PropertyObject()
            {
                // default values
                this.Scales.Add(0);
                this.EpsgCodes.Add(0);
                this.Extents.Add(0, new EnvelopeModel());
                this.OriginUpperLeft.Add(0, new PointModel());
                this.OriginLowerLeft.Add(0, new PointModel());
            }

            public bool Use { get; set; } = false;
            public List<double> Scales { get; set; } = new List<double>();
            public List<int> EpsgCodes { get; set; } = new List<int>();
            public Dictionary<int, EnvelopeModel> Extents { get; set; } = new Dictionary<int, EnvelopeModel>();
            public Dictionary<int, PointModel> OriginUpperLeft { get; set; } = new Dictionary<int, PointModel>();
            public Dictionary<int, PointModel> OriginLowerLeft { get; set; } = new Dictionary<int, PointModel>();
            public int TileWidth { get; set; } = 256;
            public int TileHeight { get; set; } = 256;
            public bool UseUpperLeft { get; set; } = true;
            public bool UseLowerLeft { get; set; } = true;
            public bool CacheUpperLeftTiles { get; set; } = true;
            public bool CacheLowerLeftTiles { get; set; } = true;
            public bool SupportPng { get; set; } = true;
            public bool SupportJpg { get; set; } = true;
            public double Dpi { get; set; } = Const.TilesDpi;

            public class EnvelopeModel
            {
                public EnvelopeModel() { }
                public EnvelopeModel(IEnvelope envelope)
                {
                    this.MinX = envelope.minx;
                    this.MinY = envelope.miny;
                    this.MaxX = envelope.maxx;
                    this.MaxY = envelope.maxy;
                }
                public double MinX { get; set; }
                public double MinY { get; set; }
                public double MaxX { get; set; }
                public double MaxY { get; set; }

                public IEnvelope ToEnvelope() => new Envelope(MinX, MinY, MaxX, MaxY);
            }

            public class PointModel
            {
                public PointModel() { }
                public PointModel(IPoint point)
                {
                    this.X = point.X;
                    this.Y = point.Y;
                }

                public double X { get; set; }
                public double Y { get; set; }

                public IPoint ToPoint() => new Point(X, Y);
            }
        }

        #endregion
    }
}
