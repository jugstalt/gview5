using gView.Framework.Db;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.DataSources.Fdb.Sqlite
{
    class SqliteConn : ICommonDbConnection
    {
        private string _connectionString;
        private string _errMsg = String.Empty;
        private Exception _lastException = null;
        private SQLiteDataAdapter _updateAdapter = null;
        private DataTable _schemaTable = null;

        public SqliteConn(string connectionString)
        {
            _connectionString = connectionString;
        }


        #region ICommonDbConnection Members

        public void CloseConnection()
        {
            throw new NotImplementedException();
        }

        public System.Data.Common.DbConnection OpenConnection()
        {
            throw new NotImplementedException();
        }

        public DbConnectionString DbConnectionString
        {
            set { throw new NotImplementedException(); }
        }

        public string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        public DBType dbType
        {
            get
            {
                return DBType.unkonwn;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string dbTypeString
        {
            get
            {
                return this.dbType.ToString();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string errorMessage
        {
            get { return _errMsg; }
        }

        public Exception LastException
        {
            get { return _lastException; }
        }

        async public Task<bool> SQLQuery(System.Data.DataSet ds, string sql, string table, bool writeable)
        {
            if (!writeable)
            {
                return await SQLQuery(ds, sql, table);
            }

            //throw new NotImplementedException();
            if (_updateAdapter != null)
            {
                _updateAdapter.Dispose();
            }

            try
            {
                _updateAdapter = new SQLiteDataAdapter(sql, _connectionString);
                SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(_updateAdapter);
                _updateAdapter.Fill(ds, table);
            }
            catch (Exception e)
            {
                if (_updateAdapter != null)
                {
                    _updateAdapter.Dispose();
                    _updateAdapter = null;
                }
                _errMsg = "QUERY WRITEABLE (" + sql + "): " + e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool Update(System.Data.DataTable table)
        {
            //throw new NotImplementedException();
            try
            {
                _updateAdapter.Update(table);

                _updateAdapter.Dispose();
                _updateAdapter = null;
            }
            catch (Exception e)
            {
                if (_updateAdapter != null)
                {
                    _updateAdapter.Dispose();
                    _updateAdapter = null;
                }
                _errMsg = "UPDATE TABLE " + table + ": " + e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool UpdateData(ref System.Data.DataSet ds, string table)
        {
            return Update(ds.Tables[table]);
        }

        public void ReleaseUpdateAdapter()
        {
            if (_updateAdapter != null)
            {
                _updateAdapter.Dispose();
                _updateAdapter = null;
            }
        }

        public Task<DataTable> Select(string fields, string from)
        {
            return Select(fields, from, "", "", false);
        }
        public Task<DataTable> Select(string fields, string from, string where)
        {
            return Select(fields, from, where, "", false);
        }
        public Task<DataTable> Select(string fields, string from, string where, string orderBy)
        {
            return Select(fields, from, where, orderBy, false);
        }
        async public Task<DataTable> Select(string fields, string from, string where, string orderBy, bool writeable)
        {
            DataSet ds = new DataSet();
            string sql = "SELECT " + ((fields == "") ? "*" : fields)
                               + " FROM " + from
                               + ((where == "") ? "" : " WHERE " + where)
                               + ((orderBy == "") ? "" : " ORDER BY " + orderBy);
            if (!await SQLQuery(ds, sql, "TAB1", writeable))
            {
                return null;
            }
            return ds.Tables[0];
        }

        async public Task<bool> SQLQuery(DataSet ds, string sql, string table)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    var command = new SQLiteCommand(sql, connection);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        DataTable tab = new DataTable(table);
                        ds.Tables.Add(tab);

                        while (await reader.ReadAsync())
                        {
                            int fieldCount = reader.FieldCount;

                            if (tab.Columns.Count == 0)
                            {
                                for (int i = 0; i < fieldCount; i++)
                                {
                                    tab.Columns.Add(new DataColumn(reader.GetName(i), reader.GetFieldType(i)));
                                }
                            }

                            DataRow row = tab.NewRow();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                row[i] = reader.GetValue(i);
                            }
                            tab.Rows.Add(row);
                        }
                    }
                }
                //using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql, _connectionString))
                //{
                //    adapter.Fill(ds, table);
                //}
            }
            catch (Exception e)
            {
                _errMsg = "QUERY (" + sql + "): " + e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }
        async public Task<bool> SQLQuery(DataSet ds, string sql, string table, DataRow refRow)
        {
            string field = getFieldPlacehoder(sql);
            while (field != "")
            {
                if (refRow[field] == null)
                {
                    _errMsg = "Field " + field + " not found in DataRow !!";
                    return false;
                }
                sql = sql.Replace("[" + field + "]", refRow[field].ToString());
                field = getFieldPlacehoder(sql);
            }
            return await SQLQuery(ds, sql, table);
        }
        async public Task<bool> SQLQuery(string sql, XmlNode feature)
        {
            return await SQLQuery(sql, feature, true);
        }
        async public Task<bool> SQLQuery(string sql, XmlNode feature, bool one2n)
        {
            string field = getFieldPlacehoder(sql);
            while (field != "")
            {
                string val = getFieldValue(feature, field);
                if (val == "")
                {
                    return false;
                }

                sql = sql.Replace("[" + field + "]", val);
                field = getFieldPlacehoder(sql);
            }

            DataSet ds = new DataSet();
            if (!await SQLQuery(ds, sql, "JOIN"))
            {
                return false;
            }

            if (ds.Tables["JOIN"].Rows.Count == 0)
            {
                return false;
            }

            DataRow row = ds.Tables["JOIN"].Rows[0];

            XmlNodeList fields = feature.SelectNodes("FIELDS");
            if (fields.Count == 0)
            {
                return false;
            }

            int rowCount = ds.Tables["JOIN"].Rows.Count;
            XmlAttribute attr;
            foreach (DataColumn col in ds.Tables["JOIN"].Columns)
            {
                XmlNode fieldNode = feature.OwnerDocument.CreateNode(XmlNodeType.Element, "FIELD", "");

                attr = feature.OwnerDocument.CreateAttribute("name");
                attr.Value = col.ColumnName;
                fieldNode.Attributes.Append(attr);
                attr = feature.OwnerDocument.CreateAttribute("value");
                if (!one2n)
                {
                    attr.Value = row[col.ColumnName].ToString();
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table width='100%' cellpadding='0' cellspacing='0' border='0'>");

                    int count = 1;
                    foreach (DataRow row2 in ds.Tables["JOIN"].Rows)
                    {
                        if (count < rowCount)
                        {
                            sb.Append("<tr><td nowrap style='border-bottom: gray 1px solid'>");
                        }
                        else
                        {
                            sb.Append("<tr><td nowrap>");
                        }

                        string val = row2[col.ColumnName].ToString().Trim();
                        if (val == "")
                        {
                            val = "&nbsp;";
                        }

                        sb.Append(val + "</td></tr>");
                        count++;
                    }
                    sb.Append("</table>");
                    attr.Value = sb.ToString();
                }
                fieldNode.Attributes.Append(attr);
                attr = feature.OwnerDocument.CreateAttribute("type");
                attr.Value = "12";
                fieldNode.Attributes.Append(attr);
                fields[0].AppendChild(fieldNode);
            }
            return true;
        }

        public Task<(System.Data.Common.DbDataReader reader, System.Data.Common.DbConnection connection)> DataReaderAsync(string sql)
        {
            throw new NotImplementedException();
        }

        async public Task<object> QuerySingleField(string sql, string FieldName)
        {
            try
            {
                DataSet ds = new DataSet();
                if (await SQLQuery(ds, sql, "FIELD"))
                {
                    if (ds.Tables["FIELD"].Rows.Count > 0)
                    {
                        return ds.Tables["FIELD"].Rows[0][FieldName];
                    }
                }
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                _lastException = ex;
            }
            return null;
        }

        public bool insertInto(string tab, string[] fields, string[] values)
        {
            throw new NotImplementedException();
        }

        public bool createIndex(string name, string table, string field, bool unique)
        {
            return createIndex(name, table, field, unique, false);
        }

        public bool createIndex(string name, string table, string field, bool unique, bool grouped)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"CREATE " +
                                 (unique ? " UNIQUE " : String.Empty) +
                                 @" INDEX [" + name + "] ON [" + table + "]("; //[" + field + "] DESC)";

                    int fieldIndex = 0;
                    foreach (string f in field.Replace(" ", "").Split(','))
                    {
                        if (String.IsNullOrWhiteSpace(f))
                        {
                            continue;
                        }

                        if (fieldIndex > 0)
                        {
                            sql += ",";
                        }

                        sql += "[" + f + "] DESC";
                        fieldIndex++;
                    }
                    sql += ")";

                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool createTable(string name, string[] fields, string[] dataTypes)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "CREATE TABLE " + name + " (";
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string field = fields[i];
                        //if (field == "KEY") field = "KEY_";
                        //if (field == "USER") field = "USER_";
                        //if (field == "TEXT") field = "TEXT_";
                        //if (field == "COUNT") field = "COUNT_";

                        sql += field + " " + dataTypes[i];
                        if (i < (fields.Length - 1))
                        {
                            sql += ",";
                        }
                    }
                    sql += ");";

                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool ExecuteNoneQuery(string sql)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool RenameTable(string name, string newName)
        {
            return ExecuteNoneQuery("ALTER TABLE " + name + " RENAME TO " + newName);
        }

        public bool dropIndex(string name)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "DROP INDEX " + name + ";";

                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool dropTable(string name)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "DROP TABLE " + name + ";";

                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }

        public bool GetSchema(string name)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    if (connection.State == ConnectionState.Open)
                    {
                        using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM " + name, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                        {
                            _schemaTable = reader.GetSchemaTable();
                        }
                        connection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                _schemaTable = null;
                _errMsg = e.Message;
                _lastException = e;
                return false;
            }
            return true;
        }
        public DataTable GetSchema2(string name)
        {
            DataTable schema = null;

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    if (connection.State == ConnectionState.Open)
                    {
                        using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM " + name, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                        {
                            schema = reader.GetSchemaTable();
                        }
                        connection.Close();
                    }
                }

                return schema;
            }
            catch (Exception e)
            {
                _errMsg = e.Message;
                _lastException = e;
                return null;
            }
        }

        async public Task<bool> createTable(System.Data.DataTable tab, bool data)
        {
            try
            {
                if (this.GetSchema(tab.TableName))
                {
                    dropTable(tab.TableName);
                }
                string fields = "", dataTypes = "";
                foreach (DataColumn col in tab.Columns)
                {
                    if (fields != "")
                    {
                        fields += ";";
                        dataTypes += ";";
                    }
                    fields += col.ColumnName;
                    if (col.DataType == typeof(int))
                    {
                        dataTypes += "INTEGER";
                    }
                    else
                    {
                        dataTypes += "TEXT(50) WITH COMPRESSION";
                    }
                }

                if (createTable(tab.TableName, fields.Split(';'), dataTypes.Split(';')))
                {
                    if (data)
                    {
                        DataSet ds = new DataSet();
                        string[] fields_ = fields.Split(';');
                        if (await this.SQLQuery(ds, "SELECT * FROM " + tab.TableName, tab.TableName, true))
                        {
                            foreach (DataRow row in tab.Rows)
                            {
                                DataRow newRow = ds.Tables[0].NewRow();
                                foreach (string field in fields_)
                                {
                                    newRow[field] = row[field];
                                }
                                ds.Tables[0].Rows.Add(newRow);
                            }
                            if (!this.UpdateData(ref ds, tab.TableName))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _errMsg = e.Message + "\n" + e.StackTrace;
                _lastException = e;
                return false;
            }
            //return true;
        }

        public dataType getFieldType(string fieldname)
        {
            throw new NotImplementedException();
        }

        public int getFieldSize(string fieldname)
        {
            throw new NotImplementedException();
        }

        public string[] TableNames(bool appendViews = false)
        {
            List<string> tableNames = new List<string>();
            List<string> querySchemes = new List<string>() { "Tables" };
            if (appendViews)
            {
                querySchemes.Add("Views");
            }

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(this.ConnectionString))
                {
                    connection.Open();
                    foreach (var queryScheme in querySchemes)
                    {
                        DataTable tables = connection.GetSchema(queryScheme);
                        foreach (DataRow row in tables.Rows)
                        {
                            string scheme = row["TABLE_SCHEMA"].ToString();
                            string table = string.IsNullOrEmpty(scheme) ? row["TABLE_NAME"].ToString() : scheme + "." + row["TABLE_NAME"].ToString();
                            tableNames.Add(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                _lastException = ex;
            }
            return tableNames.ToArray();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {

        }

        #endregion

        #region Helper

        private string getFieldPlacehoder(string str)
        {
            int pos = str.IndexOf("[");
            if (pos == -1)
            {
                return "";
            }

            int pos2 = str.IndexOf("]");
            if (pos2 == -1)
            {
                return "";
            }

            return str.Substring(pos + 1, pos2 - pos - 1);
        }

        private string getFieldValue(XmlNode feature, string name)
        {
            XmlNodeList fields = feature.SelectNodes("FIELDS/FIELD");
            name = name.ToUpper();
            foreach (XmlNode field in fields)
            {
                string fieldname = field.Attributes["name"].Value.ToString().ToUpper();

                if (fieldname == name || shortName(fieldname).ToUpper() == name)
                {
                    string val = field.Attributes["value"].Value.ToString();
                    return val;
                }
            }
            return "";
        }

        private string shortName(string name)
        {
            name.Trim();
            int index = name.IndexOf(".");
            while (index != -1)
            {
                name = name.Substring(index + 1, name.Length - index - 1);
                index = name.IndexOf(".");
            }
            return name;
        }

        #endregion
    }
}
