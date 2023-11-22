using gView.Framework.Data;
using gView.Framework.Data.Cursors;
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
    internal class SqlFDBFeatureCursorIDs : FeatureCursor
    {
        SqlConnection _connection;
        SqlDataReader _reader;
        SqlCommand _command;
        //DataTable _schemaTable;
        IGeometryDef _geomDef;
        List<int> _IDs;
        int _id_pos = 0;
        string _sql;

        private SqlFDBFeatureCursorIDs(IGeometryDef geomDef, ISpatialReference toSRef)
            : base((geomDef != null) ? geomDef.SpatialReference : null, toSRef)
        {

        }

        async static public Task<IFeatureCursor> Create(string connString, string sql, List<int> IDs, IGeometryDef geomDef, ISpatialReference toSRef)
        {
            var cursor = new SqlFDBFeatureCursorIDs(geomDef, toSRef);

            try
            {
                cursor._connection = new SqlConnection(connString);
                cursor._command = new SqlCommand(cursor._sql = sql, cursor._connection);
                await cursor._connection.OpenAsync();

                cursor._geomDef = geomDef;
                cursor._IDs = IDs;

                await cursor.ExecuteReaderAsync();
            }
            catch (Exception)
            {
                cursor.Dispose();
            }

            return cursor;
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
                _reader.Close();
                _reader = null;
            }
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }

                _connection = null;
            }
        }

        async private Task<bool> ExecuteReaderAsync()
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }
            if (_IDs == null)
            {
                return false;
            }

            if (_id_pos >= _IDs.Count)
            {
                return false;
            }

            int counter = 0;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                sb.Append(_IDs[_id_pos].ToString());
                counter++;
                _id_pos++;
                if (_id_pos >= _IDs.Count || counter > 49)
                {
                    break;
                }
            }
            if (sb.Length == 0)
            {
                return false;
            }

            string where = " WHERE [FDB_OID] IN (" + sb.ToString() + ")";

            _command = new SqlCommand(_sql + where, _connection);
            _command.SetCustomCursorTimeout();

            _reader = await _command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            return true;
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
                if (_reader == null)
                {
                    return null;
                }

                if (!await _reader.ReadAsync())
                {
                    await ExecuteReaderAsync();
                    return await NextFeature();
                }

                Feature feature = new Feature();
                for (int i = 0; i < _reader.FieldCount; i++)
                {
                    string name = _reader.GetName(i);
                    object obj = _reader.GetValue(i);
                    if (/*_schemaTable.Rows[i][0].ToString()*/name == "FDB_SHAPE")
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

                Transform(feature);
                return feature;
            }
            catch
            {
                Dispose();
                return null;
            }
        }

        #endregion
    }
}
