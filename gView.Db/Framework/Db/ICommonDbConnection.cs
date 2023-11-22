using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Framework.Db
{
    public interface ICommonDbConnection : IDisposable
    {
        void CloseConnection();
        System.Data.Common.DbConnection OpenConnection();
        DbConnectionString DbConnectionString { set; }

        string ConnectionString { get; set; }
        DBType dbType { get; set; }
        string dbTypeString { get; set; }

        string errorMessage { get; }
        Exception LastException { get; }

        Task<bool> SQLQuery(DataSet ds, string sql, string table, bool writeable);
        bool Update(DataTable table);
        bool UpdateData(ref DataSet ds, string table);
        void ReleaseUpdateAdapter();

        Task<DataTable> Select(string fields, string from);
        Task<DataTable> Select(string fields, string from, string where);
        Task<DataTable> Select(string fields, string from, string where, string orderBy);
        Task<DataTable> Select(string fields, string from, string where, string orderBy, bool writeable);
        Task<bool> SQLQuery(DataSet ds, string sql, string table);
        Task<bool> SQLQuery(DataSet ds, string sql, string table, DataRow refRow);
        Task<bool> SQLQuery(string sql, XmlNode feature);
        Task<bool> SQLQuery(string sql, XmlNode feature, bool one2n);

        Task<(DbDataReader reader, DbConnection connection)> DataReaderAsync(string sql);

        Task<object> QuerySingleField(string sql, string FieldName);

        //bool insertInto(string tab, string[] fields, string[] values);
        bool createIndex(string name, string table, string field, bool unique);
        bool createIndex(string name, string table, string field, bool unique, bool grouped);

        bool createTable(string name, string[] fields, string[] dataTypes);

        bool ExecuteNoneQuery(string sql);

        bool RenameTable(string name, string newName);
        bool dropIndex(string name);

        bool dropTable(string name);
        bool GetSchema(string name);
        DataTable GetSchema2(string name);

        Task<bool> createTable(DataTable tab, bool data);

        dataType getFieldType(string fieldname);
        int getFieldSize(string fieldname);

        string[] TableNames(bool appendViews = false);
    }
}
