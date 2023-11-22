using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Db;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.Offline;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.Fdb.MSSql
{
    /// <summary>
    /// Zusammenfassung für Class1.
    /// </summary>
    [RegisterPlugInAttribute("e6efb823-82ff-4682-8654-ffb099db7050")]
    public class SqlFDB : gView.DataSources.Fdb.MSAccess.AccessFDB
    {
        //public delegate void ProgressEvent(object progressEventReport);
        //override public event ProgressEvent reportProgress;
        private int _seVersion = 1;

        public SqlFDB()
        {
        }

        private string parseConnectionString(string connString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string p in connString.Split(';'))
            {
                if (p.ToLower().IndexOf("dsname=") == 0)
                {
                    _dsname = gView.Framework.IO.ConfigTextStream.ExtractValue(connString, "dsname");
                    continue;
                }
                if (p.ToLower().IndexOf("layers=") == 0 || p.ToLower().IndexOf("layer=") == 0)
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }

                sb.Append(p);
            }
            return sb.ToString();
        }

        #region IFDB Member

        override public void Dispose()
        {
            if (_conn != null)
            {
                _conn.Dispose();
            }

            _conn = null;

            base.Dispose();
            /*
            if(_connection!=null) 
            {
                if(_connection.State==ConnectionState.Open) _connection.Close();
                _connection.Dispose();
                _connection=null;
            }
            */
        }
        override public bool Create(string name)
        {
            return Create(name, new UserData());
        }

        public bool Create(string name, UserData parameters)
        {
            if (_conn == null)
            {
                _errMsg = "(Create) - No Connection: Use Open() before you call this methode...";
                return false;
            }

            StreamReader reader = new StreamReader(SystemVariables.StartupDirectory + @"/sql/sqlFDB/createdatabase.sql");
            string line = "";
            StringBuilder sql = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().ToLower() == "go")
                {
                    if (parameters != null &&
                        parameters.UserDataTypes.Length > 0 &&
                        sql.ToString().ToLower().StartsWith("create database"))
                    {
                        sql.Append(" on primary (");
                        bool hasName = false, first = true;
                        foreach (string type in parameters.UserDataTypes)
                        {
                            if (type.ToLower() == "name")
                            {
                                hasName = true;
                            }

                            if (!first)
                            {
                                sql.Append(",");
                            }

                            sql.Append(type + "=");
                            if (type.ToLower() == "filename")
                            {
                                sql.Append("'" + parameters.GetUserData(type) + "'");
                            }
                            else
                            {
                                sql.Append(parameters.GetUserData(type));
                            }

                            first = false;
                        }
                        if (!hasName)
                        {
                            if (!first)
                            {
                                sql.Append(",");
                            }

                            sql.Append("name=" + name);
                        }
                        sql.Append(")");
                    }

                    bool execute = true;

                    if (sql.ToString().ToLower().IndexOf("create database") == 0 && false.Equals(parameters.GetUserData("CreateDatabase")))
                    {
                        execute = false;
                    }

                    if (execute)
                    {
                        if (!_conn.ExecuteNoneQuery(sql.ToString().Replace("#FDB#", name)))
                        {
                            _errMsg = _conn.errorMessage;
                            _conn.Dispose();
                            reader.Close();
                            return false;
                        }
                    }
                    if (sql.ToString().ToLower().IndexOf("create database") == 0)
                    {
                        _conn.ConnectionString += ";database=" + name;
                    }

                    sql = new StringBuilder();
                }
                else
                {
                    sql.Append(line);
                }
            }
            reader.Close();

            return true;
        }

        public bool Create(string name, string filename)
        {
            if (_conn == null)
            {
                _errMsg = "(Create) - No Connection: Use Open() before you call this methode...";
                return false;
            }

            StreamReader reader = new StreamReader(SystemVariables.StartupDirectory + @"/sql/sqlFDB/createdatabase_from_mdf.sql");
            string line = "";
            StringBuilder sql = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().ToLower() == "go")
                {
                    if (!_conn.ExecuteNoneQuery(sql.ToString().Replace("#FDB#", name).Replace("#FILENAME#", filename)))
                    {
                        _errMsg = _conn.errorMessage;
                        _conn.Dispose();
                        reader.Close();
                        return false;
                    }
                    if (sql.ToString().ToLower().IndexOf("create database") == 0)
                    {
                        _conn.ConnectionString += ";database=" + name;
                    }

                    sql = new StringBuilder();
                }
                else
                {
                    sql.Append(line);
                }
            }
            reader.Close();

            return true;
        }

        //private SqlConnection _connection=null;
        async override public Task<bool> Open(string connString)
        {
            try
            {
                if (_conn != null)
                {
                    _conn.Dispose();
                }

                _conn = new CommonDbConnection();

                _conn.ConnectionString = parseConnectionString(connString);
                _conn.dbType = gView.Framework.Db.DBType.sql;

                await SetVersion();

                DataTable tab = await _conn.Select("*", "sys.Assemblies", "name='MSSqlSpatialEngine'");
                if (tab != null)
                {
                    _seVersion = (tab.Rows.Count == 0) ? 0 : 1;
                }
                else
                {
                    _seVersion = 0;
                }
                /*
                if(_connection!=null) 
                {
                    if(_connection.State==ConnectionState.Open) _connection.Close();
                    _connection.Dispose();
                    _connection=null;
                }
                _connection=new SqlConnection(parseConnectionString(connString));
                _connection.Open();
                */
                return true;
            }
            catch (Exception /*ex*/)
            {
                return false;
            }
        }

        public override Task<int> CreateDataset(string name, ISpatialReference sRef)
        {
            return this.CreateDataset(name, sRef, null, false, "");
        }

        async public override Task<IFeatureDataset> GetDataset(string dsname)
        {
            SqlFDBDataset dataset = await SqlFDBDataset.Create(this, dsname);
            if (dataset._dsID == -1)
            {
                _errMsg = "Dataset '" + dsname + "' does not exist!";
                return null;
            }
            return dataset;
        }
        async private Task<int> CreateDataset(string name, ISpatialReference sRef, bool imagedataset, string imageSpace)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return -1;
            }

            try
            {
                int sRefID = await CreateSpatialReference(sRef);

                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM FDB_Datasets WHERE Name='" + name + "'", "DS", true))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                if (ds.Tables[0].Rows.Count > 0)
                {
                    // already exists
                    _errMsg = "already exists !";
                    return -1;
                }
                DataRow row = ds.Tables[0].NewRow();
                row["Name"] = name;
                row["SpatialReferenceID"] = sRefID;
                row["ImageDataset"] = imagedataset;
                row["ImageSpace"] = imageSpace;

                ds.Tables[0].Rows.Add(row);

                if (!_conn.UpdateData(ref ds, "DS"))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                ds.Dispose();

                return await DatasetID(name);
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return -1;
            }
        }
        /*
        public SqlConnection sqlConnection 
        {
            get 
            {
                return _connection;
            }
        }
        */

        //private Hashtable _spatialSearchTrees=new Hashtable();

        async override public Task<IFeatureCursor> Query(IFeatureClass fc, IQueryFilter filter)
        {
            if (_conn == null || fc == null || !(fc.Dataset is IFDBDataset))
            {
                return null;
            }

            if (filter is IBufferQueryFilter)
            {
                ISpatialFilter sFilter = await BufferQueryFilter.ConvertToSpatialFilter(filter as IBufferQueryFilter);
                if (sFilter == null)
                {
                    return null;
                }

                return await Query(fc, sFilter);
            }
            if (filter is ISpatialFilter)
            {
                filter = SpatialFilter.Project(filter as ISpatialFilter, fc.SpatialReference);
            }

            string subfields = "";
            if (filter != null)
            {
                filter.fieldPrefix = "[";
                filter.fieldPostfix = "]";
                if (filter is ISpatialFilter)
                {
                    if (((ISpatialFilter)filter).SpatialRelation != spatialRelation.SpatialRelationEnvelopeIntersects)
                    {
                        filter.AddField("FDB_SHAPE");
                    }
                }
                //subfields=filter.SubFields.Replace(" ",",");
                subfields = filter.SubFieldsAndAlias;
            }
            if (subfields == "")
            {
                subfields = "*";
            }

            if (((IFDBDataset)fc.Dataset).SpatialIndexDef is MSSpatialIndex)
            {
                return await SqlFDBFeatureCursor2008.Create(_conn.ConnectionString, fc, filter, ((MSSpatialIndex)((IFDBDataset)fc.Dataset).SpatialIndexDef).GeometryType);
            }

            //if (_seVersion != 0)
            //{
            //    return new SqlFDBFeatureCursor2(_conn.connectionString, fc.Name, filter, fc);
            //}
            //else
            {
                List<long> NIDs = null;
                ISpatialFilter sFilter = null;

                if (filter is ISpatialFilter)
                {
                    sFilter = (ISpatialFilter)filter;

                    if (_seVersion != 0)
                    {
                        try
                        {
                            using (SqlConnection sqlConnection = new SqlConnection(_conn.ConnectionString))
                            {
                                SqlCommand command = new SqlCommand("select NID from CollectNIDs('" + fc.Name + "',@QGEOM)", sqlConnection);
                                SqlBytes bytes = GeometrySerialization.GeometryToSqlBytes(sFilter.Geometry);
                                SqlParameter gparam = new SqlParameter("@QGEOM", SqlDbType.Binary, (int)bytes.Length);
                                gparam.Value = bytes;
                                command.Parameters.Add(gparam);

                                DataTable nidTab = new DataTable("NIDTAB");
                                SqlDataAdapter adapter = new SqlDataAdapter(command);
                                adapter.Fill(nidTab);
                                adapter.Dispose();
                                command.Dispose();

                                NIDs = new List<long>();
                                foreach (DataRow row in nidTab.Rows)
                                {
                                    NIDs.Add((long)row[0]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _errMsg = ex.Message;
                            return null;
                        }
                    }
                    else
                    {
                        await CheckSpatialSearchTreeVersion(fc.Name);
                        if (_spatialSearchTrees[OriginFcName(fc.Name)] == null)
                        {
                            _spatialSearchTrees[OriginFcName(fc.Name)] = await this.SpatialSearchTree(fc.Name);
                        }
                        ISearchTree tree = (ISearchTree)_spatialSearchTrees[OriginFcName(fc.Name)];
                        if (tree != null && ((ISpatialFilter)filter).Geometry != null)
                        {
                            if (sFilter.SpatialRelation == spatialRelation.SpatialRelationMapEnvelopeIntersects &&
                                sFilter.Geometry is IEnvelope)
                            {
                                NIDs = tree.CollectNIDsPlus((IEnvelope)sFilter.Geometry);
                            }
                            else
                            {
                                NIDs = tree.CollectNIDs(sFilter.Geometry);
                            }
                        }
                    }
                    if (((ISpatialFilter)filter).SpatialRelation == spatialRelation.SpatialRelationMapEnvelopeIntersects)
                    {
                        sFilter = null;
                    }
                }

                string tabName = ((fc is SqlFDBFeatureClass) ? ((SqlFDBFeatureClass)fc).DbTableName : "FC_" + fc.Name);

                string sql = "SELECT " + subfields + " FROM " + tabName;
                string where = "", orederBy = "";
                if (filter != null)
                {
                    if (filter.WhereClause != "")
                    {
                        where = filter.WhereClause;
                    }
                    if (filter.OrderBy != String.Empty)
                    {
                        orederBy = filter.OrderBy;
                    }
                }

                return await SqlFDBFeatureCursor.Create(_conn.ConnectionString, sql, where, orederBy, filter.Limit, filter.BeginRecord, filter.NoLock, NIDs, sFilter, fc,
                    (filter != null) ? filter.FeatureSpatialReference : null);
            }
        }
        async override public Task<IFeatureCursor> QueryIDs(IFeatureClass fc, string subFields, List<int> IDs, ISpatialReference toSRef)
        {
            string tabName = ((fc is SqlFDBFeatureClass) ? ((SqlFDBFeatureClass)fc).DbTableName : "FC_" + fc.Name);
            string sql = "SELECT " + subFields + " FROM " + tabName;
            return await SqlFDBFeatureCursorIDs.Create(_conn.ConnectionString, sql, IDs, await this.GetGeometryDef(fc.Name), toSRef);
        }
        #endregion

        async override public Task<IDatasetElement> DatasetElement(/*SqlFDBDataset*/IDataset dataset, string elementName)
        {
            SqlFDBDataset sqlDataset = dataset as SqlFDBDataset;
            if (sqlDataset == null)
            {
                throw new Exception("datasset is null or not an SqlFDBDataset");
            }

            ISpatialReference sRef = await this.SpatialReference(sqlDataset.DatasetName);

            if (sqlDataset.DatasetName == elementName)
            {
                var isImageDatasetResult = await IsImageDataset(sqlDataset.DatasetName);
                string imageSpace = isImageDatasetResult.imageSpace;
                if (isImageDatasetResult.isImageDataset)
                {
                    IDatasetElement fLayer = (await DatasetElement(sqlDataset, elementName + "_IMAGE_POLYGONS")) as IDatasetElement;
                    if (fLayer != null && fLayer.Class is IFeatureClass)
                    {
                        SqlFDBImageCatalogClass iClass = new SqlFDBImageCatalogClass(sqlDataset, this, fLayer.Class as IFeatureClass, sRef, imageSpace);
                        iClass.SpatialReference = sRef;
                        return new DatasetElement(iClass);
                    }
                }
            }

            DataTable tab = await _conn.Select("*", "FDB_FeatureClasses", "DatasetID=" + sqlDataset._dsID + " AND Name='" + elementName + "'");
            if (tab == null || tab.Rows == null)
            {
                _errMsg = _conn.errorMessage;
                return null;
            }
            else if (tab.Rows.Count == 0)
            {
                _errMsg = "Can't find dataset element '" + elementName + "'...";
                return null;
            }

            //if (_seVersion != 0)
            //{
            //    _conn.ExecuteNoneQuery("execute LoadIndex '" + elementName + "'");
            //}
            DataRow row = tab.Rows[0];
            DataRow fcRow = row;

            if (IsLinkedFeatureClass(row))
            {
                IDataset linkedDs = await LinkedDataset(LinkedDatasetCacheInstance, LinkedDatasetId(row));
                if (linkedDs == null)
                {
                    return null;
                }

                IDatasetElement linkedElement = await linkedDs.Element((string)row["Name"]);

                LinkedFeatureClass fc = new LinkedFeatureClass(sqlDataset,
                    linkedElement != null && linkedElement.Class is IFeatureClass ? linkedElement.Class as IFeatureClass : null,
                    (string)row["Name"]);

                SqlFDBDatasetElement linkedLayer = new SqlFDBDatasetElement(fc);
                return linkedLayer;
            }

            if (row["Name"].ToString().Contains("@"))  // Geographic View
            {
                string[] viewNames = row["Name"].ToString().Split('@');
                if (viewNames.Length != 2)
                {
                    return null;
                }

                DataTable tab2 = await _conn.Select("*", "FDB_FeatureClasses", "DatasetID=" + sqlDataset._dsID + " AND Name='" + viewNames[0] + "'");
                if (tab2 == null || tab2.Rows.Count != 1)
                {
                    return null;
                }

                fcRow = tab2.Rows[0];
            }

            GeometryDef geomDef;
            if (fcRow.Table.Columns["HasZ"] != null)
            {
                geomDef = new GeometryDef((GeometryType)fcRow["GeometryType"], null, (bool)fcRow["HasZ"]);
            }
            else
            {  // alte Version war immer 3D
                geomDef = new GeometryDef((GeometryType)fcRow["GeometryType"], null, true);
            }

            SqlFDBDatasetElement layer = await SqlFDBDatasetElement.Create(this, sqlDataset, row["Name"].ToString(), geomDef);
            if (layer.Class is SqlFDBFeatureClass) // kann auch SqlFDBNetworkClass sein
            {
                ((SqlFDBFeatureClass)layer.Class).Envelope = await this.FeatureClassExtent(layer.Class.Name);
                ((SqlFDBFeatureClass)layer.Class).IDFieldName = "FDB_OID";
                ((SqlFDBFeatureClass)layer.Class).ShapeFieldName = "FDB_SHAPE";
                //((SqlFDBFeatureClass)layer.FeatureClass).SetSpatialTreeInfo(this.SpatialTreeInfo(row["Name"].ToString()));
                ((SqlFDBFeatureClass)layer.Class).SpatialReference = sRef;
            }
            var fields = await this.FeatureClassFields(sqlDataset._dsID, layer.Class.Name);
            if (fields != null && layer.Class is ITableClass)
            {
                foreach (IField field in fields)
                {
                    ((FieldCollection)((ITableClass)layer.Class).Fields).Add(field);
                }
            }

            return layer;
        }

        public override string EscapeQueryValue(string value)
        {
            return value;
        }

        async internal Task<DataTable> Select(string fields, string from, string where)
        {
            if (_conn == null)
            {
                return null;
            }

            return await _conn.Select(fields, from, where);
        }

        public override string ConnectionString
        {
            get
            {
                if (_conn == null)
                {
                    return "";
                }

                return _conn.ConnectionString;
            }
        }

        async override public Task<List<IDatasetElement>> DatasetLayers(IDataset dataset)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return null;
            }

            int dsID = await this.DatasetID(dataset.DatasetName);
            if (dsID == -1)
            {
                return null;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM FDB_FeatureClasses WHERE DatasetID=" + dsID, "FC"))
            {
                _errMsg = _conn.errorMessage;
                return null;
            }

            List<IDatasetElement> layers = new List<IDatasetElement>();
            ISpatialReference sRef = await SpatialReference(dataset.DatasetName);

            var isImageDatasetResult = await IsImageDataset(dataset.DatasetName);
            string imageSpace = isImageDatasetResult.imageSpace;
            if (isImageDatasetResult.isImageDataset)
            {
                if (await TableExists("FC_" + dataset.DatasetName + "_IMAGE_POLYGONS"))
                {
                    IFeatureClass fc = await SqlFDBFeatureClass.Create(this, dataset, new GeometryDef(GeometryType.Polygon, sRef, false));
                    ((SqlFDBFeatureClass)fc).Name = dataset.DatasetName + "_IMAGE_POLYGONS";
                    ((SqlFDBFeatureClass)fc).Envelope = await this.FeatureClassExtent(fc.Name);
                    ((SqlFDBFeatureClass)fc).IDFieldName = "FDB_OID";
                    ((SqlFDBFeatureClass)fc).ShapeFieldName = "FDB_SHAPE";

                    var fields = await this.FeatureClassFields(dataset.DatasetName, fc.Name);
                    if (fields != null)
                    {
                        foreach (IField field in fields)
                        {
                            ((FieldCollection)fc.Fields).Add(field);
                        }
                    }

                    IClass iClass = null;
                    if (imageSpace.ToLower() == "dataset") // gibts eigentlich noch gar nicht...
                    {
                        throw new NotImplementedException("Rasterdatasets are not implemented in this software version!");
                        //iClass = new SqlFDBImageDatasetClass(dataset as IRasterDataset, this, fc, sRef, imageSpace);
                        //((SqlFDBImageDatasetClass)iClass).SpatialReference = sRef;
                    }
                    else  // RasterCatalog
                    {
                        iClass = new SqlFDBImageCatalogClass(dataset as IRasterDataset, this, fc, sRef, imageSpace);
                        ((SqlFDBImageCatalogClass)iClass).SpatialReference = sRef;
                    }

                    layers.Add(new DatasetElement(iClass));
                }
            }

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                DataRow fcRow = row;

                if (IsLinkedFeatureClass(row))
                {
                    IDataset linkedDs = await LinkedDataset(LinkedDatasetCacheInstance, LinkedDatasetId(row));
                    if (linkedDs == null)
                    {
                        continue;
                    }

                    IDatasetElement linkedElement = await linkedDs.Element((string)row["Name"]);

                    LinkedFeatureClass fc = new LinkedFeatureClass(dataset,
                        linkedElement != null && linkedElement.Class is IFeatureClass ? linkedElement.Class as IFeatureClass : null,
                        (string)row["Name"]);

                    SqlFDBDatasetElement linkedLayer = new SqlFDBDatasetElement(fc);
                    layers.Add(linkedLayer);

                    continue;
                }

                if (row["Name"].ToString().Contains("@"))  // Geographic View
                {
                    string[] viewNames = row["Name"].ToString().Split('@');
                    if (viewNames.Length != 2)
                    {
                        continue;
                    }

                    DataRow[] fcRows = ds.Tables[0].Select("Name='" + viewNames[0] + "'");
                    if (fcRows == null || fcRows.Length != 1)
                    {
                        continue;
                    }

                    fcRow = fcRows[0];
                }

                if (!await TableExists("FC_" + fcRow["Name"].ToString()))
                {
                    continue;
                }
                //if (_seVersion != 0)
                //{
                //    _conn.ExecuteNoneQuery("execute LoadIndex '" + row["Name"].ToString() + "'");
                //}
                GeometryDef geomDef;
                if (fcRow.Table.Columns["HasZ"] != null)
                {
                    geomDef = new GeometryDef((GeometryType)fcRow["GeometryType"], null, (bool)fcRow["HasZ"]);
                }
                else
                {  // alte Version war immer 3D
                    geomDef = new GeometryDef((GeometryType)fcRow["GeometryType"], null, true);
                }

                SqlFDBDatasetElement layer = await SqlFDBDatasetElement.Create(this, dataset, row["Name"].ToString(), geomDef);
                if (layer.Class is SqlFDBFeatureClass)
                {
                    ((SqlFDBFeatureClass)layer.Class).Envelope = await this.FeatureClassExtent(layer.Class.Name);
                    ((SqlFDBFeatureClass)layer.Class).IDFieldName = "FDB_OID";
                    ((SqlFDBFeatureClass)layer.Class).ShapeFieldName = "FDB_SHAPE";
                    if (sRef != null)
                    {
                        ((SqlFDBFeatureClass)layer.Class).SpatialReference = (ISpatialReference)(new SpatialReference((SpatialReference)sRef));
                    }
                }
                var fields = await this.FeatureClassFields(dataset.DatasetName, layer.Class.Name);
                if (fields != null && layer.Class is ITableClass)
                {
                    foreach (IField field in fields)
                    {
                        ((FieldCollection)((ITableClass)layer.Class).Fields).Add(field);
                    }
                }
                layers.Add(layer);
            }
            return layers;
        }

        async override public Task<IFeatureClass> GetFeatureclass(string dsName, string fcName)
        {
            SqlFDBDataset dataset = new SqlFDBDataset();
            await dataset.SetConnectionString(_conn.ConnectionString + ";dsname=" + dsName);
            await dataset.Open();

            IDatasetElement element = await dataset.Element(fcName);
            if (element != null && element.Class is IFeatureClass)
            {
                return element.Class as IFeatureClass;
            }
            else
            {
                dataset.Dispose();
                return null;
            }
        }
        /*
        override protected bool UpdateSpatialIndexID(string FCName,List<SpatialIndexNode> nodes) 
        {
            ProgressReport report=new ProgressReport();
            report.featureMax=CountFeatures(FCName);
            report.featurePos=0;
			
            SqlConnection connection=null;
            SqlCommand command=null;
            try 
            {
                connection=new SqlConnection(_conn.connectionString);
                connection.Open();
                command=new SqlCommand("",connection);
				
                //this.DropIndex("FC_"+FCName+".FC_"+FCName+"_NID");

                foreach(ISpatialIndexNode node in nodes) 
                {
                    if(node.IDs==null) continue;
                    if(node.IDs.Count==0) continue;

                    StringBuilder sb=new StringBuilder();
                    int count=0;
                    foreach(object id in node.IDs) 
                    {
                        if(sb.Length>0) sb.Append(",");
                        sb.Append(id.ToString());
                        count++;
                        if(count>499) 
                        {
                            report.Message="Update Features...";
                            Report(report);
                            report.featurePos+=count;
							
                            command.CommandText="UPDATE FC_"+FCName+" SET FDB_NID="+node.NID+" WHERE FDB_OID IN ("+sb.ToString()+")";
                            command.ExecuteNonQuery();

                            sb=new StringBuilder();
                            count=0;
                        }
                    }
					
                    if(count>0) 
                    {
                        report.Message="Update Features...";
                        Report(report);
                        report.featurePos+=count;
						
                        command.CommandText="UPDATE FC_"+FCName+" SET FDB_NID="+node.NID+" WHERE FDB_OID IN ("+sb.ToString()+")";
                        command.ExecuteNonQuery();
                    }
                }
                command.Dispose(); 
                command=null;

                connection.Close();
                connection.Dispose();
                connection=null;

				
            } 
            catch(Exception ex) 
            {
                if(command!=null) 
                {
                    command.Dispose();
                    command=null;
                }
                if(connection!=null) 
                {
                    connection.Close();
                    connection.Dispose();
                    connection=null;
                }
                _errMsg+=ex.Message;
                return false;
            }
            return true;
        }
        */

        protected override string FieldDataType(IField field)
        {
            switch (field.type)
            {
                case FieldType.biginteger:
                    return "[bigint] NULL";
                case FieldType.integer:
                case FieldType.smallinteger:
                    return "[int] NULL";
                case FieldType.boolean:
                    return "[bit] NULL";
                case FieldType.Float:
                    return "[float] NULL";
                case FieldType.Double:
                    return "[float] NULL";
                case FieldType.Date:
                    return "[datetime] NULL";
                case FieldType.ID:
                    return "int";
                case FieldType.Shape:
                case FieldType.binary:
                    return "[image] NULL";
                case FieldType.character:
                    return "[nvarchar] (1) NULL";
                case FieldType.String:
                    if (field.size > 0)
                    {
                        return "[nvarchar](" + Math.Min(field.size, 4000) + ")";
                    }
                    else if (field.size <= 0)
                    {
                        return "[nvarchar] (255) NULL";
                    }

                    break;
                case FieldType.guid:
                    return "[uniqueidentifier] NULL";
                case FieldType.replicationID:
                    return "[uniqueidentifier] NULL";
                //return "[uniqueidentifier] ROWGUIDCOL DEFAULT (newid())";
                default:
                    return "[nvarchar] (255) NULL";
            }
            return "";
        }
        override protected bool CreateTable(string name, IFieldCollection Fields, bool msSpatial)
        {
            try
            {
                StringBuilder fields = new StringBuilder();
                StringBuilder types = new StringBuilder();

                bool first = true;
                bool hasID = false;

                string idField = "";

                foreach (IField field in Fields.ToEnumerable())
                {
                    //if( field.type==FieldType.ID ||
                    //if(	field.type==FieldType.Shape ) continue;

                    if (!first)
                    {
                        fields.Append(";");
                        types.Append(";");
                    }
                    first = false;

                    string fname = field.name.Replace("#", "_");

                    fields.Append(fname);

                    switch (field.type)
                    {
                        case FieldType.biginteger:
                            types.Append("[bigint] NULL");
                            break;
                        case FieldType.integer:
                        case FieldType.smallinteger:
                            /*
                            if(field.name=="FDB_OID" && name.IndexOf("FC__TMP_")==0) 
                            {
                                types.Append("[int] IDENTITY(1,1) NOT NULL");
                            }
                            */
                            types.Append("[int] NULL");
                            break;
                        case FieldType.boolean:
                            types.Append("[bit] NULL");
                            break;
                        case FieldType.Float:
                            types.Append("[float] NULL");
                            break;
                        case FieldType.Double:
                            types.Append("[float] NULL");
                            break;
                        case FieldType.Date:
                            types.Append("[datetime] NULL");
                            break;
                        case FieldType.ID:
                            if (!hasID)
                            {
                                types.Append("[int] IDENTITY(1,1) NOT NULL CONSTRAINT KEY_" + System.Guid.NewGuid().ToString("N") + "_" + field.name + " PRIMARY KEY");
                                if (msSpatial)
                                {
                                    types.Append(" CLUSTERED");
                                }
                                else
                                {
                                    types.Append(" NONCLUSTERED");
                                }

                                hasID = true;
                                idField = field.name;
                            }
                            else
                            {
                                types.Append("int");
                            }
                            break;
                        case FieldType.GEOGRAPHY:
                            types.Append("[GEOGRAPHY] NULL");
                            break;
                        case FieldType.GEOMETRY:
                            types.Append("[GEOMETRY] NULL");
                            break;
                        case FieldType.Shape:
                        case FieldType.binary:
                            types.Append("[image] NULL");
                            break;
                        case FieldType.character:
                            types.Append("[nvarchar] (1) NULL");
                            break;
                        case FieldType.String:
                            if (field.size > 0)
                            {
                                types.Append("[nvarchar](" + Math.Min(field.size, 4000) + ")");
                            }
                            else if (field.size <= 0)
                            {
                                types.Append("[nvarchar] (255) NULL");
                            }

                            break;
                        case FieldType.guid:
                            types.Append("[uniqueidentifier] NULL");
                            break;
                        case FieldType.replicationID:
                            types.Append("[uniqueidentifier] NULL");
                            //types.Append("[uniqueidentifier] ROWGUIDCOL DEFAULT (newid())");
                            break;
                        default:
                            types.Append("[nvarchar] (255) NULL");
                            break;

                    }
                }

                /*
                if(idField!="") 
                {
                    fields.Append(";Primary Key(["+idField+"])");
                    types.Append(";");
                }
                */
                if (!_conn.createTable(name, fields.ToString().Split(';'), types.ToString().Split(';')))
                {
                    _errMsg = _conn.errorMessage;
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

        async override protected Task<bool> UpdateSpatialIndexID(string FCName, List<SpatialIndexNode> nodes)
        {
            ProgressReport report = new ProgressReport();
            report.featureMax = await CountFeatures(FCName);

            try
            {
                string dsname = await DatasetNameFromFeatureClassName(FCName);
                if (dsname == "")
                {
                    return false;
                }

                FieldCollection fields = new FieldCollection();
                foreach (IField field in await FeatureClassFields(dsname, FCName))
                {
                    if (field.name == "FDB_OID" || field.name == "FDB_SHAPE")
                    {
                        continue;
                    }

                    fields.Add(field);
                }

                report.Message = "Create New FeatureClass...";
                Report(report);

                string newFCName = "_TMP_" + FCName;

                if (await CreateFeatureClass(dsname, newFCName, await GetGeometryDef(FCName), fields) == -1)
                {
                    return false;
                }

                SqlConnection connection = (SqlConnection)_conn.OpenConnection();

                if (connection == null)
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }

                if (!_conn.ExecuteNoneQuery("set identity_insert " + FcTableName(newFCName) + " on"))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }

                report.Message = "Update Features...";

                SqlCommand command = this.UpdateSpatialIndexID_CreateCommand(newFCName, connection);

                foreach (SpatialIndexNode node in nodes)
                {
                    if (node.IDs == null)
                    {
                        continue;
                    }

                    if (node.IDs.Count == 0)
                    {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    int counter = 0;
                    foreach (int oid in node.IDs)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        sb.Append(oid.ToString());

                        if (counter > 24)
                        {
                            Report(report);
                            report.featurePos += counter;

                            if (!await UpdateSpatialIndexID(FCName, newFCName, node.NID, sb.ToString(), command))
                            {
                                command.Dispose();
                                _conn.CloseConnection();
                                return false;
                            }
                            counter = 0;
                            sb = new StringBuilder();
                        }
                        counter++;
                    }
                    if (sb.Length > 0)
                    {
                        Report(report);
                        report.featurePos += counter;

                        if (!await UpdateSpatialIndexID(FCName, newFCName, node.NID, sb.ToString(), command))
                        {
                            command.Dispose();
                            _conn.CloseConnection();
                            return false;
                        }
                    }
                }
                command.Dispose();

                if (!_conn.ExecuteNoneQuery("set identity_insert " + FcTableName(newFCName) + " off"))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                _conn.CloseConnection();

                if (!_conn.dropTable(FcTableName(FCName)))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                // Beim Umbennenen Schema für Datenbank nicht zum Tabellennamen hinzufügen
                if (!_conn.RenameTable(FcTableName(newFCName), "FC_" + FCName))
                {
                    _errMsg = _conn.errorMessage;
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

        async private Task<bool> UpdateSpatialIndexID(string FCName, string tmpFCName, int NID, string ids, SqlCommand command)
        {
            try
            {
                string where = "FDB_OID IN (" + ids + ")";

                DataTable source = await _conn.Select("*", FcTableName(FCName), where, "FDB_OID");
                if (source == null)
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }

                int colCount = source.Columns.Count;
                foreach (DataRow sourceRow in source.Rows)
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        command.Parameters[i].Value = sourceRow[i];
                    }
                    command.Parameters["@FDB_NID"].Value = NID;
                    command.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return false;
            }
        }
        private SqlCommand UpdateSpatialIndexID_CreateCommand(string tmpFCName, SqlConnection connection)
        {
            try
            {
                string sql = "SELECT * FROM " + FcTableName(tmpFCName) + " WHERE FDB_NID=-1";
                _conn.GetSchema(FcTableName(tmpFCName));
                string fields = "", parameters = "";
                foreach (DataRow column in ((CommonDbConnection)_conn).schemaTable.Rows)
                {
                    if (fields != "")
                    {
                        fields += ",";
                        parameters += ",";
                    }
                    fields += "[" + column["ColumnName"] + "]";
                    parameters += "@" + column["ColumnName"];
                }

                SqlCommand command = new SqlCommand("INSERT INTO " + FcTableName(tmpFCName) + "(" + fields + ") VALUES (" + parameters + ")", connection);
                foreach (DataRow columns in ((CommonDbConnection)_conn).schemaTable.Rows)
                {
                    command.Parameters.Add("@" + columns["ColumnName"], (SqlDbType)columns["ProviderType"], (int)columns["ColumnSize"]);
                }
                return command;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return null;
            }
        }
        async override public Task<bool> ReorderRecords(IFeatureClass fClass)
        {
            ProgressReport report = new ProgressReport();
            report.featureMax = await CountFeatures(fClass.Name);

            /*
			try 
			{
				int dsID=DatasetIDFromFeatureClassName(fClass.Name);
				if(dsID==-1) return false;
				List<IField> fields=new List<IField>();
				foreach(IField field in FeatureClassFields(dsID,fClass.Name)) 
				{
					if(field.name=="FDB_OID" || field.name=="FDB_SHAPE") continue;
					fields.Add(field);
				}
				
				report.Message="Create New FeatureClass...";
				Report(report);

				string newFCName="_TMP_"+fClass.Name;

				if(CreateFeatureClass(dsID,newFCName,GetGeometryDef(fClass.Name).GeometryType,fields)==-1) 
				{
					return false;
				}

				QueryFilter filter=new QueryFilter();
				filter.SubFields="*";

				List<IFeature> features=new List<IFeature>();

				report.Message="Query Features...";
				Report(report);
				
				filter.WhereClause="";
				filter.OrderBy="OID_NID";
				IFeature feature;

				List<SpatialIndexNode> nodes=this.SpatialIndexNodes2(fClass.Name,true);
				foreach(SpatialIndexNode node in nodes) 
				{
					filter.WhereClause="FDB_NID="+node.NID;
					IFeatureCursor cursor=Query(fClass.Name,filter);
				
					while((feature=cursor.NextFeature)!=null) 
					{
						features.Add(feature);

						if(features.Count>499)  // in 500er Packeten...
						{	
							report.Message="Insert Features...";
							report.featurePos+=features.Count;
							Report(report);
						
							if(!Insert(newFCName,features)) 
							{
								return false;
							}

							features.Clear();
							features=new List<IFeature>();
						}
					}
					cursor.Release();
				}
				
				if(features.Count>0) 
				{
					report.Message="Insert Features...";
					report.featurePos+=features.Count;
					Report(report);
					
					//System.Windows.Forms.MessageBox.Show(features.Count.ToString());
					if(!Insert(FCName,features)) 
					{
						//System.Windows.Forms.MessageBox.Show(_conn.errorMessage);
						return false;
					}
				}

				//_conn.ExecuteNoneQuery("DELETE FROM FC_INDEX_"+newFCName);
				//InsertSpatialIndexNodes(newFCName,nodes);
				DropTable("FC_"+FCName);
				_conn.RenameTable("FC_"+newFCName,"FC_"+FCName);

				CalculateExtent(FCName);
			} 
			catch(Exception ex) 
			{
				_errMsg=ex.Message;
				return false;
			}
             * */
            return true;
        }

        #region IFeatureUpdater
        public override Task<bool> Insert(IFeatureClass fClass, IFeature feature)
        {
            List<IFeature> features = new List<IFeature>();
            features.Add(feature);
            return Insert(fClass, features);
        }
        async public override Task<bool> Insert(IFeatureClass fClass, List<IFeature> features)
        {
            if (fClass == null || features == null || !(fClass.Dataset is IFDBDataset))
            {
                return false;
            }

            if (features.Count == 0)
            {
                return true;
            }

            BinarySearchTree2 tree = null;
            ISpatialIndexDef si = ((IFDBDataset)fClass.Dataset).SpatialIndexDef;
            bool msSpatial = false;
            bool isNetwork = fClass.GeometryType == GeometryType.Network;

            if (si is MSSpatialIndex)
            {
                msSpatial = true;
            }
            else if (_seVersion == 0)
            {
                await CheckSpatialSearchTreeVersion(fClass.Name);
                if (_spatialSearchTrees[fClass.Name] == null)
                {
                    _spatialSearchTrees[fClass.Name] = await this.SpatialSearchTree(fClass.Name);
                }
                tree = _spatialSearchTrees[fClass.Name] as BinarySearchTree2;
            }

            var allowFcEditing = await Replication.AllowFeatureClassEditing(fClass);
            string replicationField = allowFcEditing.replFieldName;
            if (!allowFcEditing.allow)
            {
                _errMsg = "Replication Error: can't edit checked out and released featureclass...";
                return false;
            }
            try
            {
                //List<long> _nids = new List<long>();

                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    await connection.OpenAsync();

                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction = connection.BeginTransaction("InsertFeatureTransaction");

                    ReplicationTransaction replTrans = new ReplicationTransaction(connection, transaction);

                    command.Transaction = transaction;
                    SqlParameter p2 = null;

                    foreach (IFeature feature in features)
                    {
                        if (!await feature.BeforeInsert(fClass))
                        {
                            _errMsg = "Insert: Error in Feature.BeforeInsert (AutoFields,...)";
                            return false;
                        }
                        if (!String.IsNullOrEmpty(replicationField))
                        {
                            Replication.AllocateNewObjectGuid(feature, replicationField);
                            if (!await Replication.WriteDifferencesToTable(fClass, (System.Guid)feature[replicationField], Replication.SqlStatement.INSERT, replTrans))
                            {
                                _errMsg = "Replication Error: " + _errMsg;
                                return false;
                            }
                        }

                        StringBuilder fields = new StringBuilder(), parameters = new StringBuilder();
                        command.Parameters.Clear();
                        if (feature.Shape != null)
                        {
                            var shape = fClass.ConvertTo(feature.Shape);
                            GeometryDef.VerifyGeometryType(shape, fClass);

                            if (msSpatial)
                            {
                                string wkt;
                                if (si.GeometryType == GeometryFieldType.MsGeometry)
                                {
                                    if (fClass.GeometryType == GeometryType.Polygon)
                                    {
                                        wkt = "geometry::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(shape) + "',0).MakeValid()";
                                    }
                                    else
                                    {
                                        wkt = "geometry::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(shape) + "',0)";
                                    }
                                }
                                else
                                {
                                    if (fClass.GeometryType == GeometryType.Polygon)
                                    {
                                        wkt = "geography::STGeomFromText('" + GeographyMakeValid(connection, transaction, gView.Framework.OGC.WKT.ToWKT(shape)) + "',4326)";
                                    }
                                    else
                                    {
                                        wkt = "geography::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(shape) + "',4326)";
                                    }
                                }
                                //SqlParameter parameter = new SqlParameter("@FDB_SHAPE", wkt);
                                fields.Append("[FDB_SHAPE]");
                                parameters.Append(wkt);
                                //parameters.Append("@FDB_SHAPE");
                                //command.Parameters.Add(parameter);
                            }
                            else
                            {
                                BinaryWriter writer = new BinaryWriter(new MemoryStream());
                                feature.Shape.Serialize(writer, fClass);

                                byte[] geometry = new byte[writer.BaseStream.Length];
                                writer.BaseStream.Position = 0;
                                writer.BaseStream.Read(geometry, (int)0, (int)writer.BaseStream.Length);
                                writer.Close();

                                SqlParameter parameter = new SqlParameter("@FDB_SHAPE", geometry);
                                p2 = new SqlParameter("@FDB_SHAPE", geometry);
                                fields.Append("[FDB_SHAPE]");
                                parameters.Append("@FDB_SHAPE");
                                command.Parameters.Add(parameter);
                            }
                        }

                        bool hasNID = false;
                        if (fClass.Fields != null)
                        {
                            foreach (IFieldValue fv in feature.Fields)
                            {
                                if (fv.Name == "FDB_NID" || fv.Name == "FDB_SHAPE" || fv.Name == "FDB_OID")
                                {
                                    continue;
                                }

                                string name = fv.Name.Replace("$", "");
                                if (name == "FDB_NID")
                                {
                                    hasNID = true;
                                }
                                else if (fClass.FindField(name) == null)
                                {
                                    continue;
                                }

                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                if (parameters.Length != 0)
                                {
                                    parameters.Append(",");
                                }

                                SqlParameter parameter = new SqlParameter("@" + name,
                                    fv.Value != null ? fv.Value : System.DBNull.Value);
                                fields.Append("[" + name + "]");
                                parameters.Append("@" + name);
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (!hasNID && msSpatial == false && isNetwork == false)
                        {
                            long NID = 0;
                            if (_seVersion == 0)
                            {
                                if (tree != null && feature.Shape != null)
                                {
                                    NID = tree.InsertSINode(feature.Shape.Envelope);
                                }

                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                if (parameters.Length != 0)
                                {
                                    parameters.Append(",");
                                }

                                SqlParameter parameter = new SqlParameter("@FDB_NID", NID);
                                fields.Append("[FDB_NID]");
                                parameters.Append("@FDB_NID");
                                command.Parameters.Add(parameter);

                                //if (!_nids.Contains(NID)) _nids.Add(NID);
                            }
                            else
                            {
                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                if (parameters.Length != 0)
                                {
                                    parameters.Append(",");
                                }

                                IGeometryDef geomDef = fClass as IGeometryDef;
                                fields.Append("[FDB_NID]");
                                //parameters.Append("dbo.InsertSINode ('" + fClass.Name + "',@FDB_SHAPE)");
                                parameters.Append("@FDB_NID");
                            }
                        }

                        command.CommandText = "INSERT INTO " + FcTableName(fClass) + " (" + fields.ToString() + ") VALUES (" + parameters + ")";
                        if (_seVersion > 0 && !hasNID && msSpatial == false && isNetwork == false)
                        {
                            StringBuilder com = new StringBuilder();
                            com.Append("declare @FDB_NID bigint\n");
                            com.Append("set @FDB_NID=dbo.InsertSINode ('" + fClass.Name + "',@FDB_SHAPE)\n");
                            com.Append(command.CommandText + "\n");
                            com.Append("exec dbo.UpdateSIndex '" + fClass.Name + "',@FDB_NID");
                            command.CommandText = com.ToString();
                        }
                        await command.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();

                    transaction.Dispose();
                    command.Dispose();

                    if (_seVersion > 0)
                    {
                        return true;
                    }
                    else
                    {
                        //return SplitIndexNodes(fClass, connection, _nids);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _errMsg = "Insert: " + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }

        public override Task<bool> Update(IFeatureClass fClass, IFeature feature)
        {
            List<IFeature> features = new List<IFeature>();
            features.Add(feature);
            return Update(fClass, features);
        }
        async public override Task<bool> Update(IFeatureClass fClass, List<IFeature> features)
        {
            if (fClass == null || features == null || !(fClass.Dataset is IFDBDataset))
            {
                return false;
            }

            if (features.Count == 0)
            {
                return true;
            }

            BinarySearchTree2 tree = null;

            ISpatialIndexDef si = ((IFDBDataset)fClass.Dataset).SpatialIndexDef;
            bool msSpatial = false;

            if (si is MSSpatialIndex)
            {
                msSpatial = true;
            }
            else if (_seVersion == 0)
            {
                await CheckSpatialSearchTreeVersion(fClass.Name);
                if (_spatialSearchTrees[fClass.Name] == null)
                {
                    _spatialSearchTrees[fClass.Name] = await this.SpatialSearchTree(fClass.Name);
                }
                tree = _spatialSearchTrees[fClass.Name] as BinarySearchTree2;
            }

            var allowFcEditing = await Replication.AllowFeatureClassEditing(fClass);
            string replicationField = allowFcEditing.replFieldName;
            if (!allowFcEditing.allow)
            {
                _errMsg = "Replication Error: can't edit checked out and released featureclass...";
                return false;
            }
            try
            {
                //List<long> _nids = new List<long>();

                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    await connection.OpenAsync();

                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted, "UpdateFeatureTransaction");
                    ReplicationTransaction replTrans = new ReplicationTransaction(connection, transaction);
                    //replTrans = null;

                    command.Transaction = transaction;

                    foreach (IFeature feature in features)
                    {
                        if (!await feature.BeforeUpdate(fClass))
                        {
                            _errMsg = "Insert: Error in Feature.BeforeInsert (AutoFields,...)";
                            return false;
                        }
                        if (!String.IsNullOrEmpty(replicationField))
                        {
                            if (!await Replication.WriteDifferencesToTable(fClass,
                                await Replication.FeatureObjectGuid(fClass, feature, replicationField),
                                Replication.SqlStatement.UPDATE, replTrans))
                            {
                                _errMsg = "Replication Error: " + _errMsg;
                                return false;
                            }
                        }

                        long NID = 0;

                        command.Parameters.Clear();
                        StringBuilder commandText = new StringBuilder();

                        if (feature == null)
                        {
                            continue;
                        }

                        if (feature.OID < 0)
                        {
                            _errMsg = "Can't update feature with OID=" + feature.OID;
                            return false;
                        }

                        StringBuilder fields = new StringBuilder();
                        command.Parameters.Clear();
                        if (feature.Shape != null)
                        {
                            GeometryDef.VerifyGeometryType(feature.Shape, fClass);

                            if (msSpatial)
                            {
                                string wkt;
                                if (si.GeometryType == GeometryFieldType.MsGeometry)
                                {
                                    if (fClass.GeometryType == GeometryType.Polygon)
                                    {
                                        wkt = "geometry::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(feature.Shape) + "',0).MakeValid()";
                                    }
                                    else
                                    {
                                        wkt = "geometry::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(feature.Shape) + "',0)";
                                    }
                                }
                                else
                                {
                                    if (fClass.GeometryType == GeometryType.Polygon)
                                    {
                                        wkt = "geography::STGeomFromText('" + GeographyMakeValid(connection, transaction, gView.Framework.OGC.WKT.ToWKT(feature.Shape)) + "',4326)";
                                    }
                                    else
                                    {
                                        wkt = "geography::STGeomFromText('" + gView.Framework.OGC.WKT.ToWKT(feature.Shape) + "',4326)";
                                    }
                                }
                                //SqlParameter parameter = new SqlParameter("@FDB_SHAPE", wkt);
                                //fields.Append("[FDB_SHAPE]=@FDB_SHAPE");
                                //command.Parameters.Add(parameter);
                                fields.Append("[FDB_SHAPE]=" + wkt);
                            }
                            else
                            {
                                BinaryWriter writer = new BinaryWriter(new MemoryStream());
                                feature.Shape.Serialize(writer, fClass);

                                byte[] geometry = new byte[writer.BaseStream.Length];
                                writer.BaseStream.Position = 0;
                                writer.BaseStream.Read(geometry, (int)0, (int)writer.BaseStream.Length);
                                writer.Close();

                                SqlParameter parameter = new SqlParameter("@FDB_SHAPE", geometry);
                                fields.Append("[FDB_SHAPE]=@FDB_SHAPE");
                                command.Parameters.Add(parameter);
                            }
                        }

                        if (fClass.Fields != null)
                        {
                            foreach (IFieldValue fv in feature.Fields)
                            {
                                if (fv.Name == "FDB_OID" || fv.Name == "FDB_SHAPE")
                                {
                                    continue;
                                }

                                if (fv.Name == "$FDB_NID")
                                {
                                    long.TryParse(fv.Value.ToString(), out NID);
                                    continue;
                                }
                                string name = fv.Name;
                                if (fClass.FindField(name) == null)
                                {
                                    continue;
                                }

                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                SqlParameter parameter = new SqlParameter("@" + name,
                                    fv.Value != null ? fv.Value : System.DBNull.Value);
                                fields.Append("[" + name + "]=@" + name);
                                command.Parameters.Add(parameter);
                            }
                        }
                        // Wenn Shape upgedatet wird, auch neuen TreeNode berechnen
                        if (feature.Shape != null && msSpatial == false)
                        {
                            if (_seVersion > 0)
                            {
                                commandText.Append("declare @FDB_NID bigint\n");
                                commandText.Append("set @FDB_NID=dbo.UpdateSINode('" + fClass.Name + "'," + feature.OID + ",@FDB_SHAPE)\n");
                                commandText.Append("exec dbo.UpdateSIndex '" + fClass.Name + "',@FDB_NID\n");
                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                fields.Append("[FDB_NID]=@FDB_NID");
                            }
                            else if (tree != null)
                            {
                                if (feature.Shape != null)
                                {
                                    NID = (NID != 0) ? tree.UpdadeSINode(feature.Shape.Envelope, NID) : tree.InsertSINode(feature.Shape.Envelope);
                                }
                                else
                                {
                                    NID = 0;
                                }

                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                SqlParameter parameterNID = new SqlParameter("@FDB_NID", NID);
                                fields.Append("[FDB_NID]=@FDB_NID");
                                command.Parameters.Add(parameterNID);

                                //if (!_nids.Contains(NID)) _nids.Add(NID);
                            }
                        }

                        commandText.Append("UPDATE " + FcTableName(fClass) + " SET " + fields.ToString() + " WHERE FDB_OID=" + feature.OID);
                        command.CommandText = commandText.ToString();
                        await command.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();

                    transaction.Dispose();
                    command.Dispose();

                    if (_seVersion > 0)
                    {
                        return true;
                    }
                    else
                    {
                        //return SplitIndexNodes(fClass, connection, _nids);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _errMsg = "Update: " + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }

        public override Task<bool> Delete(IFeatureClass fClass, int oid)
        {
            return Delete(fClass, "FDB_OID=" + oid.ToString());
        }
        async public override Task<bool> Delete(IFeatureClass fClass, string where)
        {
            if (fClass == null)
            {
                return false;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    await connection.OpenAsync();

                    string sql = "DELETE FROM " + FcTableName(fClass) + ((where != String.Empty) ? " WHERE " + where : "");
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlTransaction transaction = connection.BeginTransaction("DeleteFeatureTransaction");
                    ReplicationTransaction replTrans = new ReplicationTransaction(connection, transaction);
                    command.Transaction = transaction;

                    var allowFcEditing = await Replication.AllowFeatureClassEditing(fClass);
                    string replicationField = allowFcEditing.replFieldName;
                    if (!allowFcEditing.allow)
                    {
                        _errMsg = "Replication Error: can't edit checked out and released featureclass...";
                        return false;
                    }
                    if (!String.IsNullOrEmpty(replicationField))
                    {
                        DataTable tab = await _conn.Select(replicationField, FcTableName(fClass), ((where != String.Empty) ? where : ""));
                        if (tab == null)
                        {
                            _errMsg = "Replication Error: " + _conn.errorMessage;
                            return false;
                        }

                        foreach (DataRow row in tab.Rows)
                        {
                            if (!await Replication.WriteDifferencesToTable(fClass,
                                (System.Guid)row[replicationField],
                                Replication.SqlStatement.DELETE,
                                replTrans))
                            {
                                _errMsg = "Replication Error: " + _errMsg;
                                return false;
                            }
                        }
                    }

                    await command.ExecuteNonQueryAsync();
                    transaction.Commit();
                    transaction.Dispose();

                    connection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return false;
            }
        }

        private string GeographyMakeValid(SqlConnection connection, SqlTransaction transaction, string wkt)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DECLARE @h GEOMETRY = '");
            sb.Append(wkt);
            sb.Append("'\n");

            sb.Append("DECLARE @geom GEOMETRY = @h.STUnion(@h.STStartPoint())\n");
            sb.Append("select @geom.ToString()");

            SqlCommand command = new SqlCommand(sb.ToString(), connection);
            command.Transaction = transaction;
            string valid_wkt = command.ExecuteScalar().ToString();
            return valid_wkt;
        }
        #endregion

        #region BinaryTree2
        async public override Task<bool> ShrinkSpatialIndex(string fcName)
        {
            if (_seVersion > 0)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("exec dbo.RebuildSIndex '" + fcName + "'", connection);
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.Message;
                    return false;
                }
            }
            else
            {
                return await base.ShrinkSpatialIndex(fcName);
            }
        }

        #region Rebuild Spatial Index
        async public override Task<bool> RebuildSpatialIndexDef(string fcName, BinaryTreeDef def, EventHandler callback)
        {
            if (_seVersion > 0)
            {
                _errMsg = "Operation is not valid, if database uses gView Spatial Engine.\nDeinstall first and try again...";
                return false;
            }
            else
            {
                return await base.RebuildSpatialIndexDef(fcName, def, callback);
            }
        }
        protected override bool UpdateFeatureSpatialNodeID(IFeatureClass fc, int oid, long nid)
        {
            if (fc == null || _conn == null)
            {
                return false;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    SqlCommand command = new SqlCommand("UPDATE " + FcTableName(fc) + " SET FDB_NID=" + nid.ToString() + " WHERE " + fc.IDFieldName + "=" + oid.ToString(), connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Fatal Exception:" + ex.Message;
                return false;
            }
        }
        #endregion
        #endregion

        #region MS Spatial Index
        override public bool SetMSSpatialIndex(MSSpatialIndex index, string fcName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand("", connection);
                    command.CommandText = "UPDATE [FDB_FeatureClasses] SET SI='" + index.GeometryType.ToString() + "'";
                    command.CommandText += ",SIMinX=" + ((index.SpatialIndexBounds != null) ? index.SpatialIndexBounds.minx.ToString(_nhi) : "0");
                    command.CommandText += ",SIMinY=" + ((index.SpatialIndexBounds != null) ? index.SpatialIndexBounds.miny.ToString(_nhi) : "0");
                    command.CommandText += ",SIMaxX=" + ((index.SpatialIndexBounds != null) ? index.SpatialIndexBounds.maxx.ToString(_nhi) : "0");
                    command.CommandText += ",SIMaxY=" + ((index.SpatialIndexBounds != null) ? index.SpatialIndexBounds.maxy.ToString(_nhi) : "0");
                    command.CommandText += ",MaxPerNode=" + index.CellsPerObject.ToString();
                    command.CommandText += ",MaxLevels=" + ((int)index.Level1 + ((int)index.Level2 << 4) + ((int)index.Level3 << 8) + ((int)index.Level4 << 12)).ToString();
                    command.CommandText += " WHERE Name='" + fcName + "'";
                    command.ExecuteNonQuery();

                    command = new SqlCommand(index.ToSql("SI_" + fcName, FcTableName(fcName), "FDB_SHAPE"), connection);
                    command.ExecuteNonQuery();

                    command.CommandText = "UPDATE [FDB_FeatureClasses] SET SI='" + index.GeometryType.ToString() + "' WHERE Name='" + fcName + "'";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception /*ex*/)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        #endregion

        async private Task<bool> SplitIndexNodes(IFeatureClass fClass, SqlConnection connection, List<long> nids)
        {
            //return true;
            if (nids == null || nids.Count == 0)
            {
                return true;
            }

            try
            {
                StringBuilder NIDs = new StringBuilder();
                foreach (long nid in nids)
                {
                    if (NIDs.Length > 0)
                    {
                        NIDs.Append(",");
                    }

                    NIDs.Append(nid.ToString());
                }

                DataTable tab = new DataTable();
                tab.TableName = "NIDS";
                using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT FDB_NID,COUNT(FDB_NID) as NID_COUNT FROM " + FcTableName(fClass) + " WHERE FDB_NID in (" + NIDs + ") GROUP BY(FDB_NID)", connection))
                {
                    while (true)
                    {
                        try
                        {
                            adapter.Fill(tab);
                            break;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                            {
                                System.Threading.Thread.Sleep(10);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                foreach (DataRow row in tab.Select("NID_COUNT>200"))
                {
                    if (!await SplitIndexNode(fClass, connection, (long)row["FDB_NID"]))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "SplitIndexNodes:" + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }

        async private Task<bool> SplitIndexNode(IFeatureClass fClass, SqlConnection connection, long nid)
        {
            if (_spatialSearchTrees[fClass.Name] == null)
            {
                _spatialSearchTrees[fClass.Name] = await this.SpatialSearchTree(fClass.Name);
            }

            BinarySearchTree2 tree = _spatialSearchTrees[fClass.Name] as BinarySearchTree2;
            if (tree == null)
            {
                return false;
            }

            if (!tree.SplitNode(nid))
            {
                return false;
            }

            long c1, c2;
            tree.ChildNodeNumbers(nid, out c1, out c2);
            IEnvelope env1 = tree[c1];
            IEnvelope env2 = tree[c2];
            bool addC1 = false, addC2 = false;

            SqlTransaction transaction = null;

            IGeometryDef geomDef = await this.GetGeometryDef(fClass.Name);
            try
            {
                SqlCommand command = new SqlCommand("SELECT [FDB_OID],[FDB_SHAPE],[FDB_NID] FROM " + FcTableName(fClass) + " WHERE FDB_NID=" + nid.ToString(), connection);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable tab = new DataTable();
                    while (true)
                    {
                        try
                        {
                            adapter.Fill(tab);
                            break;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                    foreach (DataRow row in tab.Rows)
                    {
                        if (row["FDB_SHAPE"] == System.DBNull.Value)
                        {
                            continue;
                        }

                        byte[] obj = (byte[])row["FDB_SHAPE"];
                        BinaryReader r = new BinaryReader(new MemoryStream());
                        r.BaseStream.Write((byte[])obj, 0, ((byte[])obj).Length);
                        r.BaseStream.Position = 0;

                        IGeometry p = null;
                        switch (geomDef.GeometryType)
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

                        if (p == null)
                        {
                            continue;
                        }

                        p.Deserialize(r, geomDef);
                        r.Close();

                        long NID = nid;
                        if (env1.Contains(p.Envelope))
                        {
                            addC1 = true;
                            NID = c1;
                        }
                        else if (env2.Contains(p.Envelope))
                        {
                            addC2 = true;
                            NID = c2;
                        }

                        row["FDB_NID"] = NID;
                    }

                    List<long> nids = new List<long>();
                    if (addC1)
                    {
                        nids.Add(c1);
                        if (!AddSpatatialIndexNode(fClass, connection, c1))
                        {
                            return false;
                        }
                    }
                    if (addC2)
                    {
                        nids.Add(c2);
                        if (!AddSpatatialIndexNode(fClass, connection, c2))
                        {
                            return false;
                        }
                    }

                    if (addC1 || addC2)
                    {
                        if (!IncSpatialIndexVersion(fClass, connection))
                        {
                            return false;
                        }
                    }

                    SqlCommand updateCommand = new SqlCommand(
                        "UPDATE " + FcTableName(fClass) + " SET FDB_NID=@FDB_NID WHERE FDB_OID=@FDB_OID",
                        connection);
                    updateCommand.Parameters.Add("@FDB_NID", SqlDbType.BigInt, 8, "FDB_NID");
                    updateCommand.Parameters.Add("@FDB_OID", SqlDbType.Int, 4, "FDB_OID");
                    transaction = connection.BeginTransaction();
                    updateCommand.Transaction = transaction;

                    adapter.UpdateCommand = updateCommand;
                    adapter.Update(tab);
                    while (true)
                    {
                        try
                        {
                            transaction.Commit();
                            break;
                        }
                        catch (SqlException ex)
                        {
                            Console.WriteLine(ex.ErrorCode);
                            if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }

                    return await SplitIndexNodes(fClass, connection, nids);
                }
            }
            catch (Exception ex)
            {
                _errMsg = "SplitIndexNode:" + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
            finally
            {

            }
        }

        private bool AddSpatatialIndexNode(IFeatureClass fClass, SqlConnection connection, long NID)
        {
            try
            {
                SqlCommand selectcommand = new SqlCommand(
                    "SELECT * FROM " + FcsiTableName(fClass) + " WHERE NID=" + NID.ToString(), connection);

                using (SqlDataAdapter adapter = new SqlDataAdapter(selectcommand))
                {
                    DataTable tab = new DataTable();
                    try
                    {
                        adapter.Fill(tab);
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        else
                        {
                            throw ex;
                        }
                    }

                    if (tab.Rows.Count != 0)
                    {
                        return true;
                    }

                    DataRow row = tab.NewRow();
                    row["NID"] = NID;
                    tab.Rows.Add(row);

                    SqlCommand command = new SqlCommand(
                        "INSERT INTO " + FcsiTableName(fClass) + " (NID) VALUES (@NID)", connection);
                    command.Parameters.Add("@NID", SqlDbType.BigInt, 8, "NID");
                    command.Transaction = connection.BeginTransaction();
                    adapter.InsertCommand = command;

                    adapter.Update(tab);
                    while (true)
                    {
                        try
                        {
                            command.Transaction.Commit();
                            command.Transaction = null;
                            break;
                            //return adapter.Update(tab) > 0;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errMsg = "AddSpatatialIndexNode:" + ex.GetType() + "\n" + ex.Message + "\n" + ex.Source + "\n";
                return false;
            }
        }

        private bool IncSpatialIndexVersion(IFeatureClass fClass, SqlConnection connection)
        {
            try
            {
                SqlCommand selectcommand = new SqlCommand(
                    "SELECT ID,SIVersion FROM FDB_FeatureClasses WHERE Name='" + fClass.Name + "'", connection);

                using (SqlDataAdapter adapter = new SqlDataAdapter(selectcommand))
                {
                    DataTable tab = new DataTable();
                    adapter.Fill(tab);

                    if (tab.Rows.Count != 1)
                    {
                        _errMsg = "Can't increment spatialindex version...\nTable FDB_FeatureClasses has " + tab.Rows.Count + " rows with Name='" + fClass.Name + "'";
                        return false;
                    }

                    DataRow row = tab.Rows[0];

                    if (row["SIVersion"] == System.DBNull.Value)
                    {
                        row["SIVersion"] = 1;
                    }
                    else
                    {
                        row["SIVersion"] = (long)row["SIVersion"] + 1;
                    }

                    SqlCommand command = new SqlCommand(
                        "UPDATE FDB_FeatureClasses SET SIVersion=@SIVersion WHERE ID=@ID", connection);
                    command.Parameters.Add("@SIVersion", SqlDbType.BigInt, 8, "SIVERSION");
                    command.Parameters.Add("@ID", SqlDbType.Int, 4, "ID");
                    command.Transaction = connection.BeginTransaction();
                    adapter.UpdateCommand = command;

                    adapter.Update(tab);
                    while (true)
                    {
                        try
                        {
                            command.Transaction.Commit();
                            command.Transaction = null;
                            break;
                            //return adapter.Update(tab) > 0;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 1205 && ex.ErrorCode == -2146232060)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errMsg = "IncSpatialIndexVersion:" + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }

        public override bool TruncateTable(string table)
        {
            if (_seVersion == 0)
            {
                if (_conn == null)
                {
                    return false;
                }

                if (!_conn.ExecuteNoneQuery("TRUNCATE TABLE " + FcTableName(table)))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                if (!_conn.ExecuteNoneQuery("TRUNCATE TABLE " + FcsiTableName(table)))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }

                try
                {
                    _spatialSearchTrees.Remove(table);
                }
                catch { }
                return true;
            }
            else if (_conn != null)
            {
                if (!_conn.ExecuteNoneQuery("TruncateTable '" + table + "'"))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                return true;
            }
            return false;
        }

        protected override Task<bool> RenameField(string table, IField oldField, IField newField)
        {
            if (oldField == null || newField == null || _conn == null)
            {
                return Task.FromResult(false);
            }

            string sql = "exec sp_rename '" + FcTableName(table) + "." + oldField.name + "','" + newField.name + "','COLUMN'";

            if (!_conn.ExecuteNoneQuery(sql))
            {
                _errMsg = _conn.errorMessage;
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        async protected override Task<bool> TableExists(string tableName)
        {
            if (_conn == null)
            {
                return false;
            }

            try
            {
                string sql = "IF EXISTS (SELECT 1 FROM sysobjects WHERE xtype='u' AND name='" + tableName + "') SELECT 1 as TAB_EXISTS ELSE SELECT 0 as TAB_EXISTS";
                int exists = (int)await _conn.QuerySingleField(sql, "TAB_EXISTS");

                return exists != 0;
            }
            catch
            {
                return false;
            }
        }

        async public override Task<List<string>> DatabaseTables()
        {
            if (_conn == null)
            {
                return new List<string>();
            }

            try
            {
                DataTable tab = await _conn.Select("name", "sysobjects", "xtype='u'");
                if (tab != null)
                {
                    List<string> tables = new List<string>();
                    foreach (DataRow row in tab.Rows)
                    {
                        tables.Add(row["name"].ToString());
                    }

                    return tables;
                }
            }
            catch
            {
            }
            return new List<string>();
        }

        async public override Task<List<string>> DatabaseViews()
        {
            if (_conn == null)
            {
                return new List<string>();
            }

            try
            {
                DataTable tab = await _conn.Select("name", "sysobjects", "xtype='v'");
                if (tab != null)
                {
                    List<string> views = new List<string>();
                    foreach (DataRow row in tab.Rows)
                    {
                        views.Add(row["name"].ToString());
                    }

                    return views;
                }
            }
            catch
            {
            }
            return new List<string>();
        }

        public Version SqlServerVersion
        {
            get
            {
                try
                {
                    if (_conn == null)
                    {
                        return new Version();
                    }

                    using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("SELECT  SERVERPROPERTY('productversion')", connection);
                        connection.Open();

                        return new Version(command.ExecuteScalar().ToString());
                    }
                }
                catch
                {
                    return new Version();
                }
            }
        }

        #region ICheckoutableDatabase Member

        async public override Task<bool> CreateIfNotExists(string tableName, IFieldCollection fields)
        {
            if (await TableExists(tableName))
            {
                return true;
            }

            return CreateTable(tableName, fields, false);
        }

        async public override Task<bool> CreateObjectGuidColumn(string fcName, string fieldname)
        {
            if (!await AlterTable(fcName, null, new Field(fieldname, FieldType.replicationID)))
            {
                return false;
            }

            //DataTable tab = _conn.Select("[FDB_OID],[" + fieldname + "]", "FC_" + fcName, fieldname + " is null", "", true);
            //if (tab == null)
            //{
            //    return false;
            //}
            //foreach (DataRow row in tab.Rows)
            //{
            //    row[fieldname] = System.Guid.NewGuid();
            //}
            //if (!_conn.Update(tab))
            //{
            //    return false;
            //}
            return true;
        }

        override public bool InsertRow(string table, IRow row, IReplicationTransaction replTrans)
        {
            try
            {
                SqlCommand command = new SqlCommand();

                StringBuilder fields = new StringBuilder(), parameters = new StringBuilder();
                command.Parameters.Clear();

                foreach (IFieldValue fv in row.Fields)
                {
                    string name = fv.Name;

                    if (fields.Length != 0)
                    {
                        fields.Append(",");
                    }

                    if (parameters.Length != 0)
                    {
                        parameters.Append(",");
                    }

                    SqlParameter parameter = new SqlParameter("@" + name, fv.Value);

                    fields.Append("[" + name + "]");
                    parameters.Append("@" + name);
                    command.Parameters.Add(parameter);
                }

                string tabname = table;
                if (!tabname.StartsWith("[") && !tabname.EndsWith("]"))
                {
                    tabname = "[" + tabname + "]";
                }

                command.CommandText = "INSERT INTO " + tabname + " (" + fields.ToString() + ") VALUES (" + parameters + ")";

                if (replTrans != null && replTrans.IsValid)
                {
                    replTrans.ExecuteNonQuery(command);
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.ExecuteNonQuery();
                        command.Dispose();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Insert: " + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }
        public override bool InsertRows(string table, List<IRow> rows, IReplicationTransaction replTrans)
        {
            if (rows == null || rows.Count == 0)
            {
                return true;
            }

            try
            {
                bool useTrans = replTrans != null && replTrans.IsValid;
                using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                {
                    if (useTrans)
                    {
                        SqlCommand command = new SqlCommand();
                        if (!useTrans)
                        {
                            command.Connection = connection;
                            connection.Open();
                        }
                        foreach (IRow row in rows)
                        {
                            StringBuilder fields = new StringBuilder(), parameters = new StringBuilder();
                            command.Parameters.Clear();

                            foreach (IFieldValue fv in row.Fields)
                            {
                                string name = fv.Name;

                                if (fields.Length != 0)
                                {
                                    fields.Append(",");
                                }

                                if (parameters.Length != 0)
                                {
                                    parameters.Append(",");
                                }

                                SqlParameter parameter = new SqlParameter("@" + name, fv.Value);

                                fields.Append("[" + name + "]");
                                parameters.Append("@" + name);
                                command.Parameters.Add(parameter);
                            }

                            string tabname = table;
                            if (!tabname.StartsWith("[") && !tabname.EndsWith("]"))
                            {
                                tabname = "[" + tabname + "]";
                            }

                            command.CommandText = "INSERT INTO " + tabname + " (" + fields.ToString() + ") VALUES (" + parameters + ")";

                            if (useTrans)
                            {
                                replTrans.ExecuteNonQuery(command);
                            }
                            else
                            {
                                command.ExecuteNonQuery();
                            }
                        }

                        command.Dispose();
                    }
                    else
                    {
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        using (SqlCommand command = new SqlCommand())
                        {
                            command.Transaction = transaction;
                            command.Connection = connection;
                            connection.Open();

                            foreach (IRow row in rows)
                            {
                                StringBuilder fields = new StringBuilder(), parameters = new StringBuilder();
                                command.Parameters.Clear();

                                foreach (IFieldValue fv in row.Fields)
                                {
                                    string name = fv.Name;

                                    if (fields.Length != 0)
                                    {
                                        fields.Append(",");
                                    }

                                    if (parameters.Length != 0)
                                    {
                                        parameters.Append(",");
                                    }

                                    SqlParameter parameter = new SqlParameter("@" + name, fv.Value);

                                    fields.Append("[" + name + "]");
                                    parameters.Append("@" + name);
                                    command.Parameters.Add(parameter);
                                }

                                string tabname = table;
                                if (!tabname.StartsWith("[") && !tabname.EndsWith("]"))
                                {
                                    tabname = "[" + tabname + "]";
                                }

                                command.CommandText = "INSERT INTO " + tabname + " (" + fields.ToString() + ") VALUES (" + parameters + ")";

                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Insert: " + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }
        override public bool UpdateRow(string table, IRow row, string IDField, IReplicationTransaction replTrans)
        {
            try
            {
                SqlCommand command = new SqlCommand();
                command.Parameters.Clear();
                StringBuilder commandText = new StringBuilder();

                if (row.OID < 0)
                {
                    _errMsg = "Can't update feature with OID=" + row.OID;
                    return false;
                }

                StringBuilder fields = new StringBuilder();
                foreach (IFieldValue fv in row.Fields)
                {
                    string name = fv.Name;
                    if (fields.Length != 0)
                    {
                        fields.Append(",");
                    }

                    SqlParameter parameter = new SqlParameter("@" + name, fv.Value);
                    fields.Append("[" + name + "]=@" + name);
                    command.Parameters.Add(parameter);
                }

                string tabname = table;
                if (!tabname.StartsWith("[") && !tabname.EndsWith("]"))
                {
                    tabname = "[" + tabname + "]";
                }

                commandText.Append("UPDATE " + tabname + " SET " + fields.ToString() + " WHERE " + IDField + "=" + row.OID);
                command.CommandText = commandText.ToString();

                if (replTrans != null && replTrans.IsValid)
                {
                    replTrans.ExecuteNonQuery(command);
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Update: " + ex.Message + "\n" + ex.Source + "\n" + ex.StackTrace;
                return false;
            }
        }
        override public bool DeleteRows(string table, string where, IReplicationTransaction replTrans)
        {
            try
            {
                string sql = "DELETE FROM " + table + ((where != String.Empty) ? " WHERE " + where : "");
                SqlCommand command = new SqlCommand();
                command.CommandText = sql;

                if (replTrans != null && replTrans.IsValid)
                {
                    replTrans.ExecuteNonQuery(command);
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_conn.ConnectionString))
                    {
                        connection.Open();
                        command.Connection = connection;

                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return false;
            }
        }

        DbProviderFactory _factory = null;
        override public DbProviderFactory ProviderFactory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = System.Data.SqlClient.SqlClientFactory.Instance;
                }

                return _factory;
            }
        }
        public override string GuidToSql(Guid guid)
        {
            return "'" + guid.ToString() + "'";
        }

        public override bool IsFilebaseDatabase
        {
            get
            {
                return false;
            }
        }
        #endregion

        internal string GetFeatureClassDbSchema(string fcName)
        {
            return GetTableDbSchema("FC_" + fcName);
        }

        internal string GetTableDbSchema(string tableName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_conn.ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("select TABLE_SCHEMA from INFORMATION_SCHEMA.Tables where TABLE_NAME='" + tableName + "' AND TABLE_TYPE ='BASE TABLE'", conn);
                    conn.Open();
                    object obj = cmd.ExecuteScalar();
                    if (obj != null)
                    {
                        return obj.ToString();
                    }

                    return String.Empty;
                }
            }
            catch { return String.Empty; }
        }
        override public string TableName(string tableName)
        {
            string dbSchema = GetTableDbSchema(tableName);

            return (String.IsNullOrEmpty(dbSchema) ? "[" + tableName + "]" : "[" + dbSchema + "].[" + tableName + "]");
        }

        override protected string FcTableName(string fcName)
        {
            string dbSchema = GetFeatureClassDbSchema(fcName);

            return (String.IsNullOrEmpty(dbSchema) ? "[FC_" + fcName + "]" : "[" + dbSchema + "].[FC_" + fcName + "]");
        }
        override protected string FcsiTableName(string fcName)
        {
            string dbSchema = GetFeatureClassDbSchema(fcName);

            return (String.IsNullOrEmpty(dbSchema) ? "[FCSI_" + fcName + "]" : "[" + dbSchema + "].[FCSI_" + fcName + "]");
        }
        override protected string FcTableName(IFeatureClass fc)
        {
            if (fc is SqlFDBFeatureClass)
            {
                return ((SqlFDBFeatureClass)fc).DbTableName;
            }

            return FcTableName(fc.Name);
        }
        override protected string FcsiTableName(IFeatureClass fc)
        {
            if (fc is SqlFDBFeatureClass)
            {
                return ((SqlFDBFeatureClass)fc).SiDbTableName;
            }

            return FcsiTableName(fc.Name);
        }
    }
}
