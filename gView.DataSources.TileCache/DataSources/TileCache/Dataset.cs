using gView.Framework.Data;
using gView.Framework.Data.Metadata;
using gView.Framework.Geometry;
using gView.Framework.Geometry.Tiling;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace gView.DataSources.TileCache
{
    [gView.Framework.system.RegisterPlugIn("fce6b9a8-c0b1-4600-bdd5-3eb10fe6b29d")]
    public class Dataset : DatasetMetadata, IDataset, IDataCopyright
    {
        private string _dsName = String.Empty;
        private DatasetState _state = DatasetState.unknown;
        private string _connectionString, _lastErrMsg = String.Empty, _copyright;
        private ParentRasterClass _class = null;
        private IDatasetElement _dsElement = null;

        internal static HttpClient _httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        public Dataset()
        {

        }

        #region IDataset Member

        public string ConnectionString
        {
            get
            {
                return _connectionString;
            }
        }

        public Task<bool> SetConnectionString(string value)
        {
            _connectionString = value;

            _dsName = ConfigTextStream.ExtractValue(value, "name");
            string extent = ConfigTextStream.ExtractValue(value, "extent");
            if (!String.IsNullOrEmpty(extent))
            {
                string[] bbox = extent.Split(',');
                this.Extent = new Envelope(double.Parse(bbox[0], Numbers.Nhi),
                                           double.Parse(bbox[1], Numbers.Nhi),
                                           double.Parse(bbox[2], Numbers.Nhi),
                                           double.Parse(bbox[3], Numbers.Nhi));
            }
            string origin = ConfigTextStream.ExtractValue(value, "origin");
            if (!String.IsNullOrEmpty(origin))
            {
                this.OriginOrientation = (GridOrientation)int.Parse(origin);
            }

            if (!String.IsNullOrEmpty(ConfigTextStream.ExtractValue(value, "origin_x")) &&
                !String.IsNullOrEmpty(ConfigTextStream.ExtractValue(value, "origin_y")))
            {
                this.OriginPoint = new Point(
                    double.Parse(ConfigTextStream.ExtractValue(value, "origin_x"), Numbers.Nhi),
                    double.Parse(ConfigTextStream.ExtractValue(value, "origin_y"), Numbers.Nhi));
            } 
            else
            {
                this.OriginPoint = new Point(
                    this.Extent.minx, this.OriginOrientation == GridOrientation.LowerLeft ? this.Extent.miny : this.Extent.maxy);
            }

            string sref64 = ConfigTextStream.ExtractValue(value, "sref64");
            if (!String.IsNullOrEmpty(sref64))
            {
                this.SpatialReference = new SpatialReference();
                this.SpatialReference.FromBase64String(sref64);
            }
            string scales = ConfigTextStream.ExtractValue(value, "scales");
            List<double> listScales = new List<double>();
            foreach (string scale in scales.Split(','))
            {
                if (String.IsNullOrEmpty(scale))
                {
                    continue;
                }

                listScales.Add(double.Parse(scale, Numbers.Nhi));
            }
            this.Scales = listScales.ToArray();

            string tileWidth = ConfigTextStream.ExtractValue(value, "tilewidth");
            if (!String.IsNullOrEmpty(tileWidth))
            {
                TileWidth = int.Parse(tileWidth);
            }

            string tileHeight = ConfigTextStream.ExtractValue(value, "tileheight");
            if (!String.IsNullOrEmpty(tileHeight))
            {
                TileHeight = int.Parse(tileHeight);
            }

            TileUrl = ConfigTextStream.ExtractValue(value, "tileurl");

            _copyright = ConfigTextStream.ExtractValue(value, "copyright");

            return Task.FromResult(true);
        }

        public string DatasetGroupName
        {
            get { return "Tile Cache"; }
        }

        public string DatasetName
        {
            get { return _dsName; }
        }

        public string ProviderName
        {
            get { return "gView.GIS"; }
        }

        public DatasetState State
        {
            get { return _state; }
        }

        public Task<bool> Open()
        {
            _class = new ParentRasterClass(this);
            _dsElement = new DatasetElement(_class);
            _dsElement.Title = "Cache";

            _state = DatasetState.opened;
            return Task.FromResult(true);
        }

        public string LastErrorMessage
        {
            get { return _lastErrMsg; }
            set { _lastErrMsg = value; }
        }

        public Task<List<IDatasetElement>> Elements()
        {
            if (_dsElement == null)
            {
                return null;
            }

            return Task.FromResult(new List<IDatasetElement>() { _dsElement });
        }

        public string Query_FieldPrefix
        {
            get { return String.Empty; }
        }

        public string Query_FieldPostfix
        {
            get { return String.Empty; }
        }

        public Framework.FDB.IDatabase Database
        {
            get { return null; }
        }

        public Task<IDatasetElement> Element(string title)
        {
            return Task.FromResult(_dsElement);
        }

        public Task RefreshClasses()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {

        }

        #endregion

        #region Properties

        public IEnvelope Extent { get; private set; }

        public GridOrientation OriginOrientation { get; private set; }
        public IPoint OriginPoint { get; private set; }

        public ISpatialReference SpatialReference { get; private set; }

        public double[] Scales { get; private set; }

        public int TileWidth { get; private set; }

        public int TileHeight { get; private set; }

        public string TileUrl { get; private set; }

        #endregion

        #region IPersistable Member

        async public Task<bool> LoadAsync(IPersistStream stream)
        {
            return await this.SetConnectionString((string)stream.Load("connectionstring", String.Empty));
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("connectionstring", _connectionString);
        }

        #endregion

        #region IDataCopyright Member

        public bool HasDataCopyright
        {
            get { return !String.IsNullOrEmpty(_copyright); }
        }

        public string DataCopyrightText
        {
            get { return _copyright; }
        }

        #endregion
    }
}
