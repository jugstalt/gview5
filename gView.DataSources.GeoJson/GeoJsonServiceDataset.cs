﻿using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.GeoJson
{
    [RegisterPlugIn("CD6E9CE5-6D67-49C4-8638-393503903011")]
    public class GeoJsonServiceDataset : DatasetMetadata, IFeatureDataset2
    {
        private List<IDatasetElement> _layers = new List<IDatasetElement>();
        private ISpatialReference _spatialReference = null;
        private GeoJsonSource _source = null;

        #region Properties

        internal GeoJsonSource Source => _source;

        #endregion

        #region IFeatureDataset 

        public string ConnectionString { get; private set; }

        public string DatasetGroupName => "GeoJson Service";

        public string DatasetName { get; private set; }

        public string ProviderName => "gViewGIS";

        public DatasetState State { get; private set; }

        public string Query_FieldPrefix => String.Empty;

        public string Query_FieldPostfix => String.Empty;

        public IDatabase Database => null;

        public string LastErrorMessage { get; set; }

        public Task AppendElement(string elementName)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            
        }

        async public Task<IDatasetElement> Element(string title)
        {
            if (_layers.Count == 0)
                await Elements();

            return _layers.Where(l => l.Class.Name == title).FirstOrDefault();
        }

        async public Task<List<IDatasetElement>> Elements()
        {
            if (_source != null && _layers.Count == 0)
            {
                _layers.Add(new DatasetElement(await GeoJsonServiceFeatureClass.CreateInstance(this, geometryType.Point)));
                _layers.Add(new DatasetElement(await GeoJsonServiceFeatureClass.CreateInstance(this, geometryType.Polyline)));
                _layers.Add(new DatasetElement(await GeoJsonServiceFeatureClass.CreateInstance(this, geometryType.Polygon)));
            }

            return _layers;
        }

        async public Task<IDataset2> EmptyCopy()
        {
            var dataset = new GeoJsonServiceDataset();
            await dataset.SetConnectionString(ConnectionString);
            await dataset.Open();

            return dataset;
        }

        public Task<IEnvelope> Envelope()
        {
            throw new NotImplementedException();
        }

        public Task<ISpatialReference> GetSpatialReference()
        {
            return Task.FromResult<ISpatialReference>(_spatialReference);
        }

        public void SetSpatialReference(ISpatialReference sRef)
        {
            _spatialReference = sRef;
        }

        async public Task<bool> Open()
        {
            try
            {
                var target = this.ConnectionString.ExtractConnectionStringParameter("target");
                _spatialReference = SpatialReference.FromID("epsg:4326");

                _source = new GeoJsonSource(target);
            }
            catch (Exception ex)
            {
                this.LastErrorMessage = ex.Message;
                return false;
            }
            return true;
        }

        public Task RefreshClasses()
        {
            return Task.CompletedTask;
        }

        public Task<bool> SetConnectionString(string connectionString)
        {
            this.ConnectionString = connectionString;
            return Task.FromResult(true);
        }

        #endregion

        #region IPersistable

        async public Task<bool> LoadAsync(IPersistStream stream)
        {
            if (_layers != null)
            {
                _layers.Clear();
            }

            await this.SetConnectionString((string)stream.Load("connectionstring", ""));
            return await this.Open();
        }

        public void Save(IPersistStream stream)
        {
            stream.SaveEncrypted("connectionstring", this.ConnectionString);
        }

        #endregion
    }
}
