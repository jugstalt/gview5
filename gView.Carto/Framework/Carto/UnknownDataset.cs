﻿using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Carto.Framework.Carto
{
    //[gView.Framework.system.RegisterPlugIn("B9D72B66-B716-4375-A01D-9386AC6235B8")]
    public class UnknownDataset : IDataset
    {
        public Task<IDatasetElement> Element(string title)
        {
            return Task.FromResult<IDatasetElement>(null);
        }

        public string ConnectionString
        {
            get
            {
                return String.Empty;
            }
        }
        public Task<bool> SetConnectionString(string value)
        {
            return Task.FromResult(true);
        }


        public IDatabase Database
        {
            get
            {
                return null;
            }
        }

        public string DatasetGroupName
        {
            get
            {
                return "";
            }
        }

        public string DatasetName
        {
            get
            {
                return "Unkown Dataset";
            }
        }

        public Task<List<IDatasetElement>> Elements()
        {
            return Task.FromResult(new List<IDatasetElement>());
        }

        public string LastErrorMessage
        {
            get; set;
        }

        public string ProviderName
        {
            get
            {
                return String.Empty;
            }
        }

        public Task<IEnumerable<IMetadataProvider>> GetMetadataProviders()
        {
            return Task.FromResult<IEnumerable<IMetadataProvider>>(new IMetadataProvider[0]);
        }

        public string Query_FieldPostfix
        {
            get
            {
                return String.Empty;
            }
        }

        public string Query_FieldPrefix
        {
            get
            {
                return String.Empty;
            }
        }

        public DatasetState State
        {
            get
            {
                return DatasetState.unknown;
            }
        }

        public void Dispose()
        {

        }

        public IMetadataProvider MetadataProvider(Guid guid)
        {
            return null;
        }

        public Task<bool> Open()
        {
            return Task.FromResult(true);
        }

        public void ReadMetadata(IPersistStream stream)
        {

        }

        public Task UpdateMetadataProviders()
        {
            return Task.CompletedTask;
        }

        public Task RefreshClasses()
        {
            return Task.CompletedTask;
        }

        public Task WriteMetadata(IPersistStream stream)
        {
            return Task.CompletedTask;
        }

        public Task<bool> LoadAsync(IPersistStream stream)
        {
            return Task.FromResult(true);
        }

        public void Save(IPersistStream stream)
        {

        }
    }
}
