using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Db.Extensions;
using gView.Framework.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.Fdb.MSSql
{
    internal class SqlFDBFeatureCursor : FeatureCursor
    {
        SqlConnection _connection;
        SqlDataReader _reader;
        SqlCommand _command;

        //DataTable _schemaTable;
        IGeometryDef _geomDef;
        string _sql = "", _where = "", _orderBy = "";
        bool _nolock = false;
        int _nid_pos = 0;
        List<long> _nids;
        //IGeometry _queryGeometry;
        //Envelope _queryEnvelope;
        ISpatialFilter _spatialFilter;
        public int _limit = 0, _beginRecord = 0;

        private SqlFDBFeatureCursor(IGeometryDef geomDef, ISpatialReference toSRef) :
            base((geomDef != null) ? geomDef.SpatialReference : null,
                 /*(filter!=null) ? filter.FeatureSpatialReference : null*/
                 toSRef)
        {

        }

        async public static Task<IFeatureCursor> Create(string connString, string sql, string where, string orderBy, int limit, int beginRecord, bool nolock, List<long> nids, ISpatialFilter filter, IGeometryDef geomDef, ISpatialReference toSRef)
        {
            var cursor = new SqlFDBFeatureCursor(geomDef, toSRef);

            try
            {
                cursor._connection = new SqlConnection(connString);
                cursor._command = new SqlCommand(cursor._sql = sql, cursor._connection);
                await cursor._connection.OpenAsync();

                cursor._geomDef = geomDef;
                cursor._where = where;
                cursor._orderBy = orderBy;
                cursor._nolock = nolock;
                cursor._limit = limit;
                cursor._beginRecord = beginRecord;
                if (nids != null)
                {
                    /*if (nids.Count > 0)*/
                    cursor._nids = nids;
                }

                cursor._spatialFilter = filter;

                await cursor.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                cursor.Dispose();
                throw (ex);
            }

            return cursor;
        }

        async private Task<bool> ExecuteReaderAsync()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }

            string where = _where;

            StringBuilder sb = new StringBuilder();
            SqlParameter parameter = null, parameter2 = null;
            long pVal = 0, p2Val = 0;
            if (_nids != null)
            {
                if (_nid_pos >= _nids.Count)
                {
                    return false;
                }

                if (_nids[_nid_pos] < 0)
                {
                    where = "(FDB_NID BETWEEN @FDB_NID_FROM AND @FDB_NID_TO)" + (!String.IsNullOrEmpty(where) ? " AND (" + where + ")" : String.Empty);
                    parameter = new SqlParameter("@FDB_NID_FROM", SqlDbType.BigInt, 8);
                    parameter2 = new SqlParameter("@FDB_NID_TO", SqlDbType.BigInt, 8);

                    pVal = -_nids[_nid_pos];
                    p2Val = _nids[_nid_pos + 1];
                    _nid_pos++;
                }
                else
                {
                    where = "(FDB_NID=@FDB_NID)" + (!String.IsNullOrEmpty(where) ? " AND (" + where + ")" : String.Empty);
                    parameter = new SqlParameter("@FDB_NID", SqlDbType.BigInt, 8);

                    pVal = _nids[_nid_pos];
                }
            }
            else
            {
                if (_nid_pos > 0)
                {
                    return false;
                }
            }
            if (where != "")
            {
                where = " WHERE " + where;
            }

            string offset = String.Empty;
            if (this._limit > 0)
            {
                if (String.IsNullOrWhiteSpace(_orderBy))  // offset only works with order by
                {
                    _orderBy = "FDB_OID";
                }
                offset = " offset " + Math.Max(0, _beginRecord - 1) + " rows fetch next " + _limit + " rows only";
            }

            _nid_pos++;
            _command = new SqlCommand(_sql +
                where +
                (!String.IsNullOrWhiteSpace(_orderBy) ? " ORDER BY " + _orderBy : "") +
                (!String.IsNullOrWhiteSpace(offset) ? " " + offset : "") +
                (_nolock == true ? " WITH (NOLOCK)" : ""),
                _connection);
            if (parameter != null)
            {
                _command.Parameters.Add(parameter);
            }

            if (parameter2 != null)
            {
                _command.Parameters.Add(parameter2);
            }

            _command.Prepare();

            if (parameter != null)
            {
                parameter.Value = pVal;
            }

            if (parameter2 != null)
            {
                parameter2.Value = p2Val;
            }

            _command.SetCustomCursorTimeout();
            _reader = await _command.ExecuteReaderAsync(CommandBehavior.Default);

            return true;
        }

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
                try { _reader.Close(); }
                catch { }
                _reader = null;
            }
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    try { _connection.Close(); }
                    catch { }
                }

                _connection.Dispose();
                _connection = null;
            }
        }

        #region IFeatureCursor Member

        public void Reset()
        {
        }

        public void Release()
        {
            this.Dispose();
        }

        async public override Task<IFeature> NextFeature()
        {
            try
            {
                while (true)
                {
                    if (_reader == null)
                    {
                        this.Dispose();
                        return null;
                    }
                    if (!await _reader.ReadAsync())
                    {
                        await this.ExecuteReaderAsync();
                        return await this.NextFeature();
                    }

                    Feature feature = new Feature();
                    for (int i = 0; i < _reader.FieldCount; i++)
                    {
                        string name = _reader.GetName(i);
                        object obj = _reader.GetValue(i);
                        if (/*_schemaTable.Rows[i][0].ToString()*/name == "FDB_SHAPE" && obj != DBNull.Value)
                        {
                            BinaryReader r = new BinaryReader(new MemoryStream());
                            r.BaseStream.Write((byte[])obj, 0, ((byte[])obj).Length);
                            r.BaseStream.Position = 0;

                            IGeometry p = null;
                            switch (_geomDef.GeometryType)
                            {
                                case GeometryType.Point:
                                    p = new gView.Framework.Geometry.Point();
                                    break;
                                case GeometryType.Polyline:
                                    p = new gView.Framework.Geometry.Polyline();
                                    break;
                                case GeometryType.Polygon:
                                    p = new gView.Framework.Geometry.Polygon();
                                    break;
                            }
                            if (p != null)
                            {
                                p.Deserialize(r, _geomDef);

                                r.Close();

                                //if (_queryEnvelope != null)
                                //{
                                //    if (!_queryEnvelope.Intersects(p.Envelope))
                                //        return NextFeature;
                                //    if (_queryGeometry is IEnvelope)
                                //    {
                                //        if (!Algorithm.InBox(p, (IEnvelope)_queryGeometry))
                                //            return NextFeature;
                                //    }
                                //}
                                if (_spatialFilter != null)
                                {
                                    if (!gView.Framework.Geometry.SpatialRelation.Check(_spatialFilter, p))
                                    {
                                        feature = null;
                                        break;
                                    }
                                }
                                feature.Shape = p;
                            }
                        }
                        else
                        {
                            FieldValue fv = new FieldValue(/*_schemaTable.Rows[i][0].ToString()*/name, obj);
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
                    return feature;
                }
            }
            catch (Exception ex)
            {
                Dispose();
                throw (ex);
                //return null;
            }
        }

        #endregion
    }
}
