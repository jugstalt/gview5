using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Db;
using gView.Framework.Geometry;
using System;
using System.Data;
using System.Threading.Tasks;

namespace gView.DataSources.EventTable
{
    class FeatureClass : IFeatureClass
    {
        private FieldCollection _fields = null;
        private EventTableConnection _etcon;
        private Dataset _dataset;
        private IEnvelope _env = null;

        private FeatureClass(Dataset dataset, EventTableConnection etconn)
        {
            _etcon = etconn;
            _dataset = dataset;
        }

        async static public Task<IFeatureClass> Create(Dataset dataset, EventTableConnection etconn)
        {
            var fc = new FeatureClass(dataset, etconn);

            if (fc._etcon != null)
            {
                CommonDbConnection conn = new CommonDbConnection();
                conn.ConnectionString2 = fc._etcon.DbConnectionString.ConnectionString;
                if (conn.GetSchema(fc._etcon.TableName))
                {
                    fc._fields = new FieldCollection(conn.schemaTable);
                    IField idfield = fc._fields.FindField(fc._etcon.IdFieldName);
                    if (idfield is Field)
                    {
                        ((Field)idfield).type = FieldType.ID;
                    }
                }
                DataTable tab = await conn.Select("MIN(" + fc._etcon.XFieldName + ") as minx,MAX(" + fc._etcon.XFieldName + ") as maxx,MIN(" + fc._etcon.YFieldName + ") as miny,MAX(" + fc._etcon.YFieldName + ") as maxy", fc._etcon.TableName);

                if (conn.LastException != null)
                {
                    throw conn.LastException;
                }
                
                if (tab != null && tab.Rows.Count == 1)
                {
                    try
                    {
                        fc._env = new Envelope(
                            Convert.ToDouble(tab.Rows[0]["minx"]),
                            Convert.ToDouble(tab.Rows[0]["miny"]),
                            Convert.ToDouble(tab.Rows[0]["maxx"]),
                            Convert.ToDouble(tab.Rows[0]["maxy"]));
                    }
                    catch
                    {
                        fc._env = new Envelope();
                    }
                }
            }

            return fc;
        }

        #region IFeatureClass Member

        public string ShapeFieldName
        {
            get { return "#SHAPE#"; }
        }

        public gView.Framework.Geometry.IEnvelope Envelope
        {
            get { return _env; }
        }

        public Task<int> CountFeatures()
        {
            return Task.FromResult<int>(0);
        }

        async public Task<IFeatureCursor> GetFeatures(IQueryFilter filter)
        {
            if (filter is ISpatialFilter)
            {
                filter = SpatialFilter.Project(filter as ISpatialFilter, this.SpatialReference);
            }

            return await FeatureCursor.Create(_etcon, filter, this);
        }

        #endregion

        #region ITableClass Member

        async public Task<ICursor> Search(IQueryFilter filter)
        {
            return await GetFeatures(filter);
        }

        async public Task<ISelectionSet> Select(IQueryFilter filter)
        {
            if (String.IsNullOrEmpty(this.IDFieldName) || filter == null)
            {
                return null;
            }

            filter.SubFields = this.IDFieldName;

            using (IFeatureCursor cursor = await this.GetFeatures(filter))
            {
                IFeature feat;

                SpatialIndexedIDSelectionSet selSet = new SpatialIndexedIDSelectionSet(this.Envelope);
                while ((feat = await cursor.NextFeature()) != null)
                {
                    selSet.AddID(feat.OID, feat.Shape);
                }

                return selSet;
            }
        }

        public IFieldCollection Fields
        {
            get { return _fields; }
        }

        public IField FindField(string name)
        {
            if (_fields != null)
            {
                return _fields.FindField(name);
            }

            return null;
        }

        public string IDFieldName
        {
            get
            {
                if (_etcon != null)
                {
                    return _etcon.IdFieldName;
                }

                return String.Empty;
            }
        }

        #endregion

        #region IClass Member

        public string Name
        {
            get
            {
                if (_etcon != null)
                {
                    return _etcon.TableName;
                }

                return String.Empty;
            }
        }

        public string Aliasname
        {
            get
            {
                if (_etcon != null)
                {
                    return _etcon.TableName;
                }

                return String.Empty;
            }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion

        #region IGeometryDef Member

        public bool HasZ
        {
            get { return false; }
        }

        public bool HasM
        {
            get { return false; }
        }

        public gView.Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                if (_etcon != null)
                {
                    return _etcon.SpatialReference;
                }

                return null;
            }
            set
            {
                if (_etcon != null)
                {
                    _etcon.SpatialReference = value;
                }
            }
        }

        public gView.Framework.Geometry.GeometryType GeometryType
        {
            get { return GeometryType.Point; }
        }

        #endregion
    }
}
