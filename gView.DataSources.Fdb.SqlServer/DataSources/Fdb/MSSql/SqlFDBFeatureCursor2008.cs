using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.Fdb.MSSql
{
    internal class SqlFDBFeatureCursor2008 : FeatureCursor
    {
        private IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        private SqlConnection _connection;
        private SqlDataReader _reader;
        private SqlCommand _command;

        private SqlFDBFeatureCursor2008(IFeatureClass fc, IQueryFilter filter)
            : base((fc != null) ? fc.SpatialReference : null,
            (filter != null) ? filter.FeatureSpatialReference : null)
        {

        }

        async public static Task<IFeatureCursor> Create(string connectionString, IFeatureClass fc, IQueryFilter filter, GeometryFieldType geometryType)
        {
            var cursor = new SqlFDBFeatureCursor2008(fc, filter);

            StringBuilder where = new StringBuilder();

            if (filter.SubFields == "*")
            {
                filter = (IQueryFilter)filter.Clone();
                filter.SubFields = "";
                foreach (IField field in fc.Fields.ToEnumerable())
                {
                    filter.AddField(field.name);
                }
            }
            if (filter is ISpatialFilter && ((ISpatialFilter)filter).Geometry != null)
            {
                ISpatialFilter sFilter = filter as ISpatialFilter;

                if (sFilter.SpatialRelation == spatialRelation.SpatialRelationMapEnvelopeIntersects &&
                    sFilter.Geometry is IEnvelope)
                {
                    where.Append(fc.ShapeFieldName + ".Filter(");
                    if (geometryType == GeometryFieldType.MsGeometry)
                    {
                        where.Append("geometry::STGeomFromText('");
                    }
                    else
                    {
                        where.Append("geography::STGeomFromText('");
                    }

                    where.Append(gView.Framework.OGC.WKT.ToWKT(sFilter.Geometry));
                    where.Append("',");
                    if (geometryType == GeometryFieldType.MsGeometry)
                    {
                        where.Append("0");
                    }
                    else
                    {
                        where.Append("4326");
                    }

                    where.Append("))=1");
                }
                else if (sFilter.Geometry != null)
                {
                    where.Append(fc.ShapeFieldName + ".STIntersects(");
                    if (geometryType == GeometryFieldType.MsGeometry)
                    {
                        where.Append("geometry::STGeomFromText('");
                    }
                    else
                    {
                        where.Append("geography::STGeomFromText('");
                    }

                    where.Append(gView.Framework.OGC.WKT.ToWKT(sFilter.Geometry));
                    where.Append("',");
                    if (geometryType == GeometryFieldType.MsGeometry)
                    {
                        where.Append("0");
                    }
                    else
                    {
                        where.Append("4326");
                    }

                    where.Append("))=1");
                }
                filter.AddField(fc.ShapeFieldName);
            }

            string filterWhereClause = (filter is IRowIDFilter) ? ((IRowIDFilter)filter).RowIDWhereClause : filter.WhereClause;

            StringBuilder fieldNames = new StringBuilder();
            foreach (string fieldName in filter.SubFields.Split(' '))
            {
                if (fieldNames.Length > 0)
                {
                    fieldNames.Append(",");
                }

                if (fieldName == "[" + fc.ShapeFieldName + "]")
                {
                    fieldNames.Append(fc.ShapeFieldName + ".STAsBinary() as temp_geometry");
                    //ShapeFieldName = "temp_geometry";
                }
                else
                {
                    fieldNames.Append(fieldName);
                }
            }

            string tabName = ((fc is SqlFDBFeatureClass) ? ((SqlFDBFeatureClass)fc).DbTableName : "FC_" + fc.Name);

            cursor._command = new SqlCommand();
            cursor._command.CommandText = "SELECT " + fieldNames + " FROM " + tabName;
            if (!String.IsNullOrEmpty(where.ToString()))
            {
                cursor._command.CommandText += " WHERE " + where.ToString() + ((filterWhereClause != "") ? " AND (" + filterWhereClause + ")" : "");
            }
            else if (!String.IsNullOrEmpty(filterWhereClause))
            {
                cursor._command.CommandText += " WHERE " + filterWhereClause;
            }

            try
            {
                cursor._connection = new SqlConnection(connectionString);
                await cursor._connection.OpenAsync();
                cursor._command.Connection = cursor._connection;

                cursor._reader = await cursor._command.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                cursor._reader = null;
            }

            return cursor;
        }

        #region IFeatureCursor Member

        public override Task<IFeature> NextFeature()
        {
            while (true)
            {
                if (_reader == null || !_reader.Read())
                {
                    this.Dispose();
                    return Task.FromResult<IFeature>(null);
                }
                //return null;

                Feature feature = new Feature();
                for (int i = 0; i < _reader.FieldCount; i++)
                {
                    string name = _reader.GetName(i);
                    object obj = _reader.GetValue(i);
                    if (name == "temp_geometry" && obj != DBNull.Value)
                    {
                        feature.Shape = gView.Framework.OGC.OGC.WKBToGeometry((byte[])obj);
                    }
                    else
                    {
                        FieldValue fv = new FieldValue(name, obj);
                        feature.Fields.Add(fv);
                        if (fv.Name == "FDB_OID")
                        {
                            feature.OID = Convert.ToInt32(obj);
                        }
                    }
                }
                if (feature == null)
                {
                    continue;
                }

                Transform(feature);
                return Task.FromResult<IFeature>(feature);
            }
        }

        #endregion

        #region IDisposable Member

        public override void Dispose()
        {
            base.Dispose();
            if (_connection != null && _command != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    if (_reader != null)
                    {
                        try
                        {
                            while (_reader.Read())
                            {
                                _command.Cancel();
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    if (_command != null)
                    {
                        _command.Dispose();
                        _command = null;
                    }
                }
            }

            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }

                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion
    }
}
