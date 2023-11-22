using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace gView.Framework.Db
{
    /// <summary>
    /// Zusammenfassung für DataProvider.
    /// </summary>
    public class DataProvider
    {
        private SqlConnection sqlConnection = null;
        private OracleConnection oracleConnection = null;
        private DbConnection dbConnection = null;
        private DbProviderFactory dbFactory = null;
        private string _errMsg = "";

        public DataProvider()
        {
        }

        public void Dispose()
        {
            Close();
        }

        public string lastErrorMessage
        {
            get { return _errMsg; }
        }
        public bool Open(string connectionString)
        {
            return Open(connectionString, false);
        }
        public bool Open(string connectionString, bool testIt)
        {
            int pos = connectionString.IndexOf(":");
            if (pos == -1)
            {
                return false;
            }

            try
            {
                Close();

                string type = connectionString.Substring(0, pos);
                string connStr = connectionString.Substring(pos + 1, connectionString.Length - pos - 1);

                if(type.Contains("@"))
                {
                    type = type.Split('@')[0]; // mssql@create
                }

                switch (type.ToLower())
                {
                    case "oracleclient":
                    case "oracle":
                        oracleConnection = new OracleConnection(connStr);
                        if (testIt)
                        {
                            oracleConnection.Open();
                            oracleConnection.Close();
                        }
                        break;
                    case "sqlclient":
                    case "sql":
                    case "mssql":
                        sqlConnection = new SqlConnection(connStr);
                        if (testIt)
                        {
                            sqlConnection.Open();
                            sqlConnection.Close();
                        }
                        break;
                    case "postgre":
                    case "postgis":
                    case "npgsql":
                        try
                        {
                            dbFactory = DataProvider.PostgresProvider;

                            dbConnection = dbFactory.CreateConnection();
                            dbConnection.ConnectionString = DbConnectionString.ParseNpgsqlConnectionString(connStr);
                            if (testIt)
                            {
                                dbConnection.Open();
                                dbConnection.Close();
                            }
                            break;
                        }
                        catch (Exception ex)
                        {
                            _errMsg = ex.Message;
                            return false;
                        }
                    default:
                        _errMsg = "Test Connection is not supported for '" + type + "'";
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return false;
            }
        }
        public void Close()
        {
            if (oracleConnection != null)
            {
                oracleConnection.Close();
                oracleConnection.Dispose();
            }
            if (sqlConnection != null)
            {
                sqlConnection.Close();
                sqlConnection.Dispose();
            }
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection.Dispose();
            }
            sqlConnection = null;
            sqlConnection = null;
            sqlConnection = null;
            sqlConnection = null;
            dbConnection = null;
            dbFactory = null;
        }

        public DataTable ExecuteQuery(string sql)
        {
            try
            {
                DataSet ds = new DataSet();

                if (oracleConnection != null)
                {
                    oracleConnection.Open();
                    OracleDataAdapter adapter = new OracleDataAdapter(sql, oracleConnection);
                    adapter.Fill(ds);
                    oracleConnection.Close();
                }
                else if (sqlConnection != null)
                {
                    sqlConnection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, sqlConnection);
                    adapter.Fill(ds);
                    sqlConnection.Close();
                }
                else if (dbConnection != null && dbFactory != null)
                {
                    DbCommand command = dbFactory.CreateCommand();
                    command.Connection = dbConnection;
                    command.CommandText = sql;
                    dbConnection.Open();
                    DbDataAdapter adapter = dbFactory.CreateDataAdapter();
                    adapter.SelectCommand = command;
                    adapter.Fill(ds);
                    dbConnection.Close();
                }
                if (ds.Tables.Count == 0)
                {
                    return null;
                }

                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                if (oracleConnection != null)
                {
                    oracleConnection.Close();
                }

                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                }

                if (dbConnection != null)
                {
                    dbConnection.Close();
                }

                _errMsg = ex.Message;
                return null;
            }
        }

        public static DbProviderFactory PostgresProvider
        {
            get
            {
                return Npgsql.NpgsqlFactory.Instance;
            }
        }

        public static DbProviderFactory SQLiteProviderFactory
        {
            get
            {
                return System.Data.SQLite.SQLiteFactory.Instance;
            }
        }

        #region Database Object Names

        public string ToTableName(string tableName)
        {
            tableName = tableName.Trim();

            if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
            {
                return ToDbTableName("npgsql", tableName);
            }

            return tableName;
        }

        public string ToFieldName(string fieldName)
        {
            fieldName = fieldName.Trim();

            if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
            {
                return ToDbTableName("npgsql", fieldName);
            }

            return fieldName;
        }

        public string ToFieldNames(string fieldNames)
        {
            if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
            {
                return ToDbFieldNames("npgsql", fieldNames);
            }
            return ToDbFieldNames(String.Empty, fieldNames);
        }

        public string ToWhereClause(string where)
        {
            where = where.Trim();

            if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
            {
                return ToDbWhereClause("npgsql", where);
            }

            return where;
        }

        public string FieldPrefix
        {
            get
            {
                if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
                {
                    return "\"";
                }
                return String.Empty;
            }
        }

        public string FieldPostfix
        {
            get
            {
                if (dbConnection != null && dbConnection.GetType().ToString().ToLower().Contains(".npgsqlconnection"))
                {
                    return "\"";
                }
                return String.Empty;
            }
        }

        #endregion

        #region Static Members

        public static string ToDbTableName(string type, string tableName)
        {
            tableName = tableName.Trim();

            switch (type.ToLower())
            {
                case "npgsql":
                    StringBuilder sb = new StringBuilder();
                    foreach (string t in tableName.Split('.'))
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(".");
                        }

                        sb.Append((t != t.ToLower()) ? "\"" + t + "\"" : t);
                    }
                    tableName = sb.ToString();

                    while (tableName.Contains("\"\""))
                    {
                        tableName = tableName.Replace("\"\"", "\"");
                    }
                    return tableName;
            }

            return tableName;
        }

        public static string ToDbFieldName(string type, string fieldName)
        {
            fieldName = fieldName.Trim();

            switch (type.ToLower())
            {
                case "npgsql":
                    fieldName = (fieldName != fieldName.ToLower()) ? "\"" + fieldName + "\"" : fieldName;

                    while (fieldName.Contains("\"\""))
                    {
                        fieldName = fieldName.Replace("\"\"", "\"");
                    }
                    return fieldName;
            }

            return fieldName;
        }

        public static string ToDbFieldNames(string type, string fieldNames)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string fieldName in fieldNames.Replace(",", " ").Split(' '))
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                sb.Append(ToDbFieldName(type, fieldName));
            }
            return sb.ToString();
        }

        public static string ToDbWhereClause(string type, string whereClause)
        {
            switch (type.ToLower())
            {
                case "npgsql":
                    return NpgsqlWhereClauseParser.Parse(whereClause);
            }
            return whereClause;
        }

        #endregion

        #region Helper Classes

        internal class NpgsqlWhereClauseParser
        {
            static public string Parse(string where)
            {
                if (String.IsNullOrEmpty(where))
                {
                    return String.Empty;
                }

                Parse(ref where, "=");
                Parse(ref where, ">");
                Parse(ref where, "<");
                Parse(ref where, "<=");
                Parse(ref where, ">=");
                Parse(ref where, "<>");
                Parse(ref where, " like ");
                Parse(ref where, " in ");
                Parse(ref where, " not ");

                return where;
            }

            static private void Parse(ref string where, string op)
            {
                op = op.ToLower();
                int pos = 0;
                while ((pos = where.ToLower().IndexOf(op, pos)) >= 0)
                {
                    if (op == "=" && (where[pos - 1] == '<' || where[pos - 1] == '>'))
                    {
                        pos++;
                        continue;
                    }
                    if (op == ">" && (where[pos - 1] == '<' || where[pos + 1] == '='))
                    {
                        pos++;
                        break;
                    }
                    if (op == "<" && (where[pos + 1] == '>' || where[pos + 1] == '='))
                    {
                        pos++;
                        break;
                    }

                    if (InsideQuotes(where, pos))
                    {
                        continue;
                    }

                    string fieldName = WordBefore(where, pos);
                    string w1 = where.Substring(0, pos - fieldName.Length);
                    string w2 = where.Substring(pos, where.Length - pos);
                    string fn = DbFieldName(fieldName.Trim());
                    where = w1 + fn + w2;
                    pos = w1.Length + fn.Length + 1;
                }
            }

            static private bool InsideQuotes(string str, int pos)
            {
                char act = (char)0;
                for (int i = 0; i <= pos; i++)
                {
                    if (str[i] == '\'' && act == (char)0)
                    {
                        act = '\'';
                    }
                    else if (str[i] == '\'' && act == '\'')
                    {
                        act = (char)0;
                    }
                    else if (str[i] == '"' && act == (char)0)
                    {
                        act = '"';
                    }
                    else if (str[i] == '"' && act == '"')
                    {
                        act = (char)0;
                    }
                }
                return act != (char)0;
            }

            static private string WordBefore(string str, int pos)
            {
                int i = pos - 1;
                for (; i >= 0; i--)
                {
                    if (str[i] == ' ' || str[i] == '(')
                    {
                        string wb = str.Substring(i + 1, pos - i - 1);
                        if (!String.IsNullOrEmpty(wb.Trim()))
                        {
                            return wb;
                        }
                    }
                }
                return str.Substring(0, pos);
            }

            static private string DbFieldName(string fn)
            {
                return DataProvider.ToDbFieldName("npgsql", fn);
            }
        }

        #endregion
    }
}
