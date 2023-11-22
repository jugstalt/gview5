using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Db;
using gView.Framework.Geometry;
using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.EventTable
{
    class FeatureCursor : gView.Framework.Data.FeatureCursor
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private EventTableConnection _etcon;
        //private DataTable _tab = null;
        private bool _addShape = false;
        private DbDataReader _dbReader = null;
        private DbConnection _dbConnection = null;
        private ISpatialFilter _spatialFilter = null;

        private FeatureCursor(EventTableConnection etconn, IQueryFilter filter)
            : base(
                    (etconn != null ? etconn.SpatialReference : null),
                    (filter != null ? filter.FeatureSpatialReference : null))
        {

        }

        async static public Task<FeatureCursor> Create(EventTableConnection etconn, IQueryFilter filter, IFeatureClass fc)
        {
            var cursor = new FeatureCursor(etconn, filter);

            cursor._etcon = etconn;
            if (etconn != null)
            {
                CommonDbConnection conn = new CommonDbConnection();
                conn.ConnectionString2 = etconn.DbConnectionString.ConnectionString;

                string appendWhere = String.Empty;
                if (filter is ISpatialFilter &&
                    ((ISpatialFilter)filter).Geometry != null)
                {
                    if (!(((ISpatialFilter)filter).Geometry is Envelope))
                    {
                        cursor._addShape = true;
                        filter.AddField("#SHAPE#");
                        cursor._spatialFilter = (ISpatialFilter)filter;
                    }
                    IEnvelope env = ((ISpatialFilter)filter).Geometry.Envelope;
                    appendWhere =
                        etconn.XFieldName + ">=" + env.minx.ToString(_nhi) + " AND " +
                        etconn.XFieldName + "<=" + env.maxx.ToString(_nhi) + " AND " +
                        etconn.YFieldName + ">=" + env.miny.ToString(_nhi) + " AND " +
                        etconn.YFieldName + "<=" + env.maxy.ToString(_nhi);
                }
                if (filter is IRowIDFilter)
                {
                    IRowIDFilter idFilter = (IRowIDFilter)filter;
                    appendWhere = idFilter.RowIDWhereClause;
                }

                string where = (filter != null) ? filter.WhereClause : String.Empty;
                if (!String.IsNullOrEmpty(where))
                {
                    where += (String.IsNullOrEmpty(appendWhere) ? String.Empty : " AND (" + appendWhere + ")");
                }
                else
                {
                    where = appendWhere;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(filter.SubFieldsAndAlias);
                foreach (string fieldname in filter.SubFields.Split(' '))
                {
                    if (fieldname == "#SHAPE#")
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        sb.Append(cursor._etcon.XFieldName + "," + cursor._etcon.YFieldName);
                        cursor._addShape = true;
                    }
                    if (fieldname == "*")
                    {
                        cursor._addShape = true;
                    }
                }

                string fields = sb.ToString().Replace(",#SHAPE#", "").Replace("#SHAPE#,", "").Replace("#SHAPE#", "");

                //_tab = conn.Select(sb.ToString(), etconn.TableName, where);
                //_addShape = _tab.Columns.Contains(_etcon.XFieldName) &&
                //            _tab.Columns.Contains(_etcon.YFieldName);

                var limits = conn.LimitResults(filter, fc);
                string commandText = $"select {limits.top} {fields} from {etconn.TableName} {(String.IsNullOrEmpty(where) ? String.Empty : " WHERE " + where)} {limits.limit}";
                
                var dataReaderResult = await conn.DataReaderAsync(commandText);

                cursor._dbReader = dataReaderResult.reader;
                cursor._dbConnection = dataReaderResult.connection;
            }

            return cursor;
        }

        #region IFeatureCursor Member

        async public override Task<IFeature> NextFeature()
        {
            if (_dbReader == null)
            {
                return null;
            }

            try
            {
                while (true)
                {
                    if (!await _dbReader.ReadAsync())
                    {
                        return null;
                    }

                    Feature feature = new Feature();
                    double x = 0.0, y = 0.0;
                    for (int i = 0; i < _dbReader.FieldCount; i++)
                    {
                        string name = _dbReader.GetName(i);
                        object obj = _dbReader.GetValue(i);
                        if (name == _etcon.XFieldName && obj != DBNull.Value)
                        {
                            x = Convert.ToDouble(obj);
                        }
                        else if (name == _etcon.YFieldName && obj != DBNull.Value)
                        {
                            y = Convert.ToDouble(obj);
                        }
                        else if (name == _etcon.IdFieldName && obj != DBNull.Value)
                        {
                            try
                            {
                                feature.OID = Convert.ToInt32(obj);
                            }
                            catch { }
                        }

                        FieldValue fv = new FieldValue(name, obj);
                        feature.Fields.Add(fv);
                    }

                    if (_addShape)
                    {
                        feature.Shape = new Point(x, y);

                        if (_spatialFilter != null)
                        {
                            if (!gView.Framework.Geometry.SpatialRelation.Check(_spatialFilter, feature.Shape))
                            {
                                continue;
                            }
                        }
                    }
                    Transform(feature);
                    return feature;
                }
            }
            catch (Exception ex)
            {
                Dispose();
                throw (ex);
            }
        }

        #endregion

        #region IDisposable Member

        override public void Dispose()
        {
            base.Dispose();

            if (_dbReader != null)
            {
                _dbReader.Close();
                _dbReader = null;
            }
            if (_dbConnection != null)
            {
                if (_dbConnection.State == ConnectionState.Open)
                {
                    _dbConnection.Close();
                }

                _dbConnection.Dispose();
                _dbConnection = null;
            }
        }

        #endregion
    }
}
