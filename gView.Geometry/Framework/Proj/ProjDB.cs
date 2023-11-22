using gView.Framework.Db;
using gView.Framework.system;
using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace gView.Framework.Proj
{
    public enum ProjDBTables { projs, datums }
    /// <summary>
    /// Zusammenfassung für ProjDB.
    /// </summary>
    public class ProjDB
    {
        private IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private DbProviderFactory _dbFactory = null;
        private string _connectionString = String.Empty;
        private DataSet _ds = null;
        private string _table;

        public ProjDB() : this("projs")
        {
        }
        public ProjDB(ProjDBTables table) : this(table.ToString())
        {
        }
        public ProjDB(string table)
        {
            _table = table;

            _dbFactory = DataProvider.SQLiteProviderFactory;
            _connectionString = $"Data Source={Path.Combine(SystemVariables.StartupDirectory, "proj.db")}";
        }

        /*
		public ProjDB(string dbname)
		{
			_conn=new DBConnection();
			_conn.OleDbConnectionMDB=dbname;
		}
		*/

        ~ProjDB()
        {
            this.Dispose();
        }

        public void Dispose()
        {

        }
        public string errMessage
        {
            get { return this.LastDbError; }
        }
        public bool Refresh()
        {
            _ds = new DataSet();
            DataTable table = DbSelect("*", _table, String.Empty);
            if (table == null)
            {
                _ds = null;
                return false;
            }
            _ds.Tables.Add(table);
            return true;
        }

        public bool createProjection(string id, string param, string description, string category)
        {
            //_err="";
            //DataSet ds=new DataSet();
            //if(_conn.SQLQuery(ref ds,"SELECT * FROM "+_table+" WHERE ID=-1","PROJS",true)) 
            //{
            //    DataRow row=ds.Tables[0].NewRow();
            //    row["PROJ_ID"]=id;
            //    row["PROJ_P4"]=param;
            //    row["PROJ_DESCRIPTION"]=description;
            //    row["PROJ_CATEGORY"]=category;
            //    ds.Tables[0].Rows.Add(row);

            //    if(!_conn.UpdateData(ref ds,"PROJS")) 
            //    {
            //        _err=_conn.errorMessage;
            //        return false;
            //    }
            //} 
            //else 
            //{
            //    _err=_conn.errorMessage;
            //    return false;
            //}
            //return true;

            throw new NotImplementedException();
        }

        public string GetParameters(string ID)
        {
            DataTable tab = DbSelect("PROJ_P4", _table, "PROJ_ID like '" + ID + "'");
            if (tab == null || tab.Rows.Count == 0)
            {
                return "";
            }

            DataRow row = tab.Rows[0];
            string param = row["PROJ_P4"].ToString().Trim();
            if (param == "")
            {
                return "";
            }

            if (param[0] != '+')
            {
                param = "+" + param.Replace(" ", " +");
            }

            return param;
        }

        public string GetDatumParameters(string ID)
        {
            DataTable tab = DbSelect("*", _table, "DATUM_Name='" + ID + "'");
            if (tab == null || tab.Rows.Count == 0)
            {
                return "";
            }

            DataRow row = tab.Rows[0];
            string param = String.Format("+towgs84={0},{1},{2},{3},{4},{5},{6}",
                ((double)row["DATUM_Dx"]).ToString(_nhi),
                ((double)row["DATUM_Dy"]).ToString(_nhi),
                ((double)row["DATUM_Dz"]).ToString(_nhi),
                ((double)row["DATUM_Rx"]).ToString(_nhi),
                ((double)row["DATUM_Ry"]).ToString(_nhi),
                ((double)row["DATUM_Rz"]).ToString(_nhi),
                ((double)row["DATUM_Ppm"]).ToString(_nhi));

            return param;
        }

        public string GetDescription(string ID)
        {
            DataTable tab = DbSelect("PROJ_DESCRIPTION", _table, "PROJ_ID like '" + ID + "'");
            if (tab == null || tab.Rows.Count == 0)
            {
                return "";
            }

            DataRow row = tab.Rows[0];
            string descr = row["PROJ_DESCRIPTION"].ToString().Trim();
            return descr;
        }

        public string GetDatumName(string ID)
        {
            DataTable tab = DbSelect("PROJ_DATUM", _table, "PROJ_ID like '" + ID + "'");
            if (tab == null || tab.Rows.Count == 0)
            {
                return "Unknown";
            }

            DataRow row = tab.Rows[0];

            string datum = row["PROJ_DATUM"].ToString().Trim();
            if (datum == "")
            {
                return "Unknown";
            }

            return datum;
        }

        public DataTable GetTable(string where = "")
        {
            DataTable tab = DbSelect("PROJ_ID,PROJ_DESCRIPTION", _table, where);
            return tab;
        }

        public DataTable GetDatumTable(string where = "")
        {
            DataTable tab = DbSelect("DATUM_NAME", _table, where);
            return tab;
        }

        public static string SpheroidByP4(string ID, out double majorAxis, out double minorAxis, out double invFlattening)
        {
            ProjDB projDb = new ProjDB();
            DataTable tab = projDb.DbSelect("*", "spheroid", "SPHEROID_P4='" + ID + "'");
            if (tab != null && tab.Rows.Count > 0)
            {
                DataRow row = tab.Rows[0];

                majorAxis = ((row["SPHEROID_SemiMajorAxis"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_SemiMajorAxis"]) : 0.0);
                minorAxis = ((row["SPHEROID_SemiMinorAxis"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_SemiMinorAxis"]) : 0.0);
                invFlattening = ((row["SPHEROID_InverseFlattening"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_InverseFlattening"]) : 0.0);

                return row["SPHEROID_Name"].ToString();
            }

            majorAxis = minorAxis = invFlattening = 0.0;
            return "";
        }

        public static string SpheroidByName(string Name, out double majorAxis, out double minorAxis, out double invFlattening)
        {
            ProjDB projDb = new ProjDB();
            DataTable tab = projDb.DbSelect("*", "spheroid", "SPHEROID_Name='" + Name + "'");
            if (tab != null && tab.Rows.Count > 0)
            {
                DataRow row = tab.Rows[0];

                majorAxis = ((row["SPHEROID_SemiMajorAxis"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_SemiMajorAxis"]) : 0.0);
                minorAxis = ((row["SPHEROID_SemiMinorAxis"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_SemiMinorAxis"]) : 0.0);
                invFlattening = ((row["SPHEROID_InverseFlattening"] != DBNull.Value) ? Convert.ToDouble(row["SPHEROID_InverseFlattening"]) : 0.0);

                return row["SPHEROID_P4"].ToString();
            }

            majorAxis = minorAxis = invFlattening = 0.0;
            return "";
        }

        public static string PrimeMeridianByP4(string p4, out double longitude)
        {
            ProjDB projDb = new ProjDB();
            DataTable tab = projDb.DbSelect("*", "pm", "PM_P4='" + p4 + "'");
            if (tab != null && tab.Rows.Count > 0)
            {
                DataRow row = tab.Rows[0];

                longitude = (double)row["PM_Meridian"];
                return row["PM_Name"].ToString();
            }
            longitude = 0.0;
            return "Unknown";
        }

        public static string ProjectionNameByP4(string p4)
        {
            ProjDB projDb = new ProjDB();
            DataTable tab = projDb.DbSelect("*", "projections", "PROJECTION_P4='" + p4 + "'");
            if (tab != null)
            {
                DataRow row = tab.Rows[0];

                return row["PROJECTION_Name"].ToString();
            }

            return "";
        }

        public static string ProjectionP4ByName(string name)
        {
            ProjDB projDb = new ProjDB();
            DataTable tab = projDb.DbSelect("*", "projections", "PROJECTION_Name='" + name + "'");
            if (tab != null)
            {
                DataRow row = tab.Rows[0];

                return row["PROJECTION_P4"].ToString();
            }

            return "";
        }

        #region WKT
        public string GetPgWkt(string ID)
        {
            try
            {
                DataTable tab = DbSelect("WKT", "wkt", "PROJ_ID like '" + ID + "'");
                if (tab == null || tab.Rows.Count == 0)
                {
                    return String.Empty;
                }

                return tab.Rows[0]["WKT"].ToString();
            }
            catch
            {
                return String.Empty;
            }
        }

        public string GetQoutedWKTParameter(string wkt, string pre, string quote1, string quote2)
        {
            try
            {
                int pos = wkt.IndexOf(pre);
                if (pos != -1)
                {
                    int posS = wkt.IndexOf(quote1, pos + pre.Length);
                    int posE = wkt.IndexOf(quote2, posS + 1);

                    return wkt.Substring(posS + 1, posE - posS - 1);
                }
            }
            catch { }
            return String.Empty;
        }
        #endregion

        #region DB

        internal DataTable DbSelect(string fields, string from, string where, string orderBy = "")
        {
            DataTable table = new DataTable(from);
            string sql = "SELECT " + ((fields == "") ? "*" : fields)
                               + " FROM " + from
                               + ((where == "") ? "" : " WHERE " + where)
                               + ((orderBy == "") ? "" : " ORDER BY " + orderBy);

            try
            {
                using (DbConnection connection = _dbFactory.CreateConnection())
                using (DbCommand command = _dbFactory.CreateCommand())
                {
                    connection.ConnectionString = _connectionString;

                    command.Connection = connection;
                    command.CommandText = sql;

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        int fieldCount = reader.FieldCount;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            table.Columns.Add(new DataColumn(reader.GetName(i), typeof(object)));
                        }

                        while (reader.Read())
                        {
                            DataRow row = table.NewRow();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                row[i] = reader.GetValue(i);
                            }
                            table.Rows.Add(row);
                        }
                    }

                    return table;
                }
            }
            catch (Exception ex)
            {
                this.LastDbError = ex.Message;
            }
            return null;
        }

        internal string LastDbError { get; set; }

        #endregion
    }
}
