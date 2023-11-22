
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Db;
using gView.Framework.Editor.Core;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.Network;
using gView.Framework.Offline;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataSources.Fdb.MSAccess
{
    /// <summary>
    /// Zusammenfassung für AccessFDB.
    /// </summary>
    public abstract class AccessFDB : gView.Framework.FDB.IFeatureDatabase3, gView.Framework.FDB.IImageDB, gView.Framework.FDB.IFeatureUpdater, gView.Framework.FDB.IAltertable, gView.Framework.Offline.IFeatureDatabaseReplication, IEditableDatabase, IImplementsBinarayTreeDef, IAlterDatabase, IDatabaseNames, IDisposable, IFDBDatabase
    {
        protected IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        protected enum IndexType { BinaryTree, BinaryTree2 }
        protected IndexType _indexType = IndexType.BinaryTree2;

        public ICommonDbConnection _conn;
        private string _filename;
        protected string _errMsg;
        protected Hashtable _spatialSearchTrees = new Hashtable();
        protected string _dsname = "";
        protected Version _version = new Version(1, 0);

        public delegate void ProgressEvent(object progressEventReport);
        virtual public event ProgressEvent reportProgress;

        private string parseConnectionString(string connString)
        {
            string mdb = connString;
            foreach (string p in connString.Split(';'))
            {
                if (p.IndexOf("dsname=") == 0)
                {
                    _dsname = gView.Framework.IO.ConfigTextStream.ExtractValue(connString, "dsname");
                    continue;
                }
                if (p.IndexOf("layers=") == 0)
                {
                    continue;
                }
                if (p.IndexOf("mdb=") == 0)
                {
                    mdb = gView.Framework.IO.ConfigTextStream.ExtractValue(connString, "mdb");
                }
            }
            return mdb;
        }
        public AccessFDB()
        {
            _conn = null;
        }

        public Version FdbVersion
        {
            get { return _version; }
        }

        async virtual public Task<int> FeatureClassID(int DatasetID, string name)
        {
            if (_conn == null)
            {
                return -1;
            }

            try
            {
                _errMsg = "";

                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + name + "' AND " + DbColName("DatasetID") + "=" + DatasetID, "FC", false))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                if (ds.Tables[0].Rows.Count == 0)
                {
                    _errMsg = "not found !";
                    return -1;
                }

                int ID = Convert.ToInt32(ds.Tables[0].Rows[0]["ID"]);
                ds.Dispose();

                return ID;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message;
                return -1;
            }
        }
        async virtual public Task<int> DatasetID(string name)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return -1;
            }

            try
            {
                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("Name") + "='" + name + "'", "DS"))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                if (ds.Tables[0].Rows.Count == 0)
                {
                    _errMsg = "Dataset not found !";
                    return -1;
                }
                int ID = Convert.ToInt32(ds.Tables[0].Rows[0]["ID"]);
                ds.Dispose();

                return ID;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace;
                return -1;
            }
        }
        async virtual public Task<string> DatasetName(int DatasetID)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return String.Empty;
            }

            try
            {
                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("ID") + "=" + DatasetID, "DS"))
                {
                    _errMsg = _conn.errorMessage;
                    return String.Empty;
                }
                if (ds.Tables[0].Rows.Count == 0)
                {
                    _errMsg = "Dataset not found !";
                    return String.Empty;
                }
                string name = (string)ds.Tables[0].Rows[0]["Name"];
                ds.Dispose();

                return name;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return String.Empty;
            }
        }

        async virtual public Task<int> DatasetIDByFeatureClass(string fcName)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return -1;
            }

            try
            {
                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcName + "'", "FC"))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                if (ds.Tables[0].Rows.Count == 0)
                {
                    _errMsg = "Featureclass not found !";
                    return -1;
                }
                int ID = Convert.ToInt32(ds.Tables[0].Rows[0]["DatasetID"]);
                ds.Dispose();

                return ID;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return -1;
            }
        }
        async virtual public Task<string> DatasetNameByFeatureClass(string fcName)
        {
            int DatasetID = await DatasetIDByFeatureClass(fcName);
            if (DatasetID == -1)
            {
                return String.Empty;
            }

            return await DatasetName(DatasetID);
        }

        #region IFDB
        virtual public bool Create(string name)
        {
            FileInfo tpl = new FileInfo(gView.Framework.system.SystemVariables.StartupDirectory + @"/sql/accessFDB/fdb_1_2_0.tpl");
            if (!tpl.Exists)
            {
                _errMsg = "Can't find template file: " + tpl.FullName;
                return false;
            }

            FileInfo fi = new FileInfo(_filename = name);
            if (fi.Extension.ToLower() != ".mdb")
            {
                fi = new FileInfo(_filename = name + ".mdb");
            }

            if (fi.Exists)
            {
                fi.Delete();
            }

            tpl.CopyTo(fi.FullName);
            /*
			ADOX.CatalogClass cat=new ADOX.CatalogClass();

			cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source="+name+";Jet OLEDB:Engine Type=5");
			//System.Runtime.InteropServices.Marshal.ReleaseComObject(cat);
			cat=null;

            
			DBConnection _conn=new DBConnection();
			_conn.OleDbConnectionMDB=name;

			string [] fields_ds = { "ID",		                      "Name",      "SpatialReferenceID", "ImageDataset", "ImageSpace", "Primary Key([ID])" };
            string[] types_ds =   { "INTEGER NOT NULL IDENTITY(1,1)", "TEXT(255)", "INTEGER",            "YESNO",        "TEXT(255)",  "" };

			string [] fields_fc = { "ID",                            "Name",     "DatasetID","GeometryType","ShapeField","HasZ", "HasM", "MinX",  "MinY",  "MaxX",  "MaxY",  "SI",      "SIMinX","SIMinY","SIMaxX","SIMaxY","SIRATIO","MaxPerNode","Primary Key([ID])"  };
			string [] types_fc  = { "INTEGER NOT NULL IDENTITY(1,1)","TEXT(255)","INTEGER",  "INTEGER",     "TEXT(255)", "YESNO","YESNO","DOUBLE","DOUBLE","DOUBLE","DOUBLE","TEXT(50)","DOUBLE","DOUBLE","DOUBLE","DOUBLE","DOUBLE", "INTEGER",    ""};

			string [] fields_fcf= { "ID",                            "FClassID","FieldName","Aliasname","FieldType","DefaultValue","IsRequired","IsEditable" };
			string [] types_fcf = { "INTEGER NOT NULL IDENTITY(1,1)","INTEGER","TEXT",     "TEXT",     "INTEGER",  "TEXT(255)",   "YESNO",      "YESNO" };

			string [] fields_sr = { "ID",                            "Name",     "Description", "Params","DatumName","DatumParam","Primary Key([ID])" };
			string [] types_sr  = { "INTEGER NOT NULL IDENTITY(1,1)","TEXT(255)","TEXT(255)",   "MEMO",  "TEXT(255)","TEXT(255)", "" };

			string [] fields_ri = { "Major",  "Minor",  "Bugfix" };
			string [] types_ri  = { "INTEGER","INTEGER","INTEGER" };

			_conn.createTable("FDB_Datasets",fields_ds,types_ds);
			_conn.createTable("FDB_FeatureClasses",fields_fc,types_fc);
			_conn.createTable("FDB_FeatureClassFields",fields_fcf,types_fcf);
			_conn.createTable("FDB_SpatialReference",fields_sr,types_sr);
			_conn.createTable("FDB_ReleaseInfo",fields_ri,types_ri);

			DataSet ds=new DataSet();
			_conn.SQLQuery(ref ds,"SELECT * FROM FDB_ReleaseInfo","rel",true);
			DataRow row=ds.Tables[0].NewRow();
			row["Major"]=1;
			row["Minor"]=0;
			row["Bugfix"]=0;
			ds.Tables[0].Rows.Add(row);
			_conn.UpdateData(ref ds,"rel");
			ds.Dispose();
            */
            return true;
        }
        async virtual public Task<bool> Open(string connectionString)
        {
            _conn = new CommonDbConnection();
            ((CommonDbConnection)_conn).ConnectionString2 = _filename = parseConnectionString(connectionString);

            await SetVersion();
            return true;
        }
        virtual public void Dispose()
        {
            //System.Windows.Forms.MessageBox.Show("Dispose "+_conn.connectionString);
            if (_conn != null)
            {
                _conn.Dispose();
                _conn = null;
            }

            if (LinkedDatasetCacheInstance != null)
            {
                LinkedDatasetCacheInstance.Dispose();
            }
        }

        public string LastErrorMessage { get { return _errMsg; } set { _errMsg = value; } }
        public Exception LastException { get { return (_conn != null ? _conn.LastException : null); } }

        virtual public Task<int> CreateDataset(string name, ISpatialReference sRef)
        {
            return this.CreateDataset(name, sRef, null);
        }

        virtual public Task<int> CreateDataset(string name, ISpatialReference sRef, ISpatialIndexDef sIndexDef)
        {
            return CreateDataset(name, sRef, sIndexDef, false, String.Empty);
        }

        async virtual protected Task<int> CreateDataset(string name, ISpatialReference sRef, ISpatialIndexDef sIndexDef, bool imagedataset, string imageSpace)
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
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("Name") + "='" + name + "'", "DS", true))
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
                DataRow row = ds.Tables["DS"].NewRow();
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

                int dsID = await DatasetID(name);

                #region DatasetGeometryType
                if (sIndexDef != null && FdbVersion >= new Version(1, 2, 0))
                {
                    ds = new DataSet();
                    if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_DatasetGeometryType") + " WHERE " + DbColName("DatasetID") + "=" + dsID, "DSGT", true))
                    {
                        await DeleteDataset(name);
                        _errMsg = _conn.errorMessage;
                        return -1;
                    }

                    row = ds.Tables["DSGT"].NewRow();
                    if (sIndexDef is gViewSpatialIndexDef)
                    {
                        #region gView BinaryIndex
                        gViewSpatialIndexDef gvIndex = (gViewSpatialIndexDef)sIndexDef;

                        row["DatasetID"] = dsID;
                        row["GeometryType"] = (int)FieldType.Shape;
                        row["SIMinX"] = gvIndex.SpatialIndexBounds != null ? gvIndex.SpatialIndexBounds.minx : 0.0;
                        row["SIMinY"] = gvIndex.SpatialIndexBounds != null ? gvIndex.SpatialIndexBounds.miny : 0.0;
                        row["SIMaxX"] = gvIndex.SpatialIndexBounds != null ? gvIndex.SpatialIndexBounds.maxx : 0.0;
                        row["SIMaxY"] = gvIndex.SpatialIndexBounds != null ? gvIndex.SpatialIndexBounds.maxy : 0.0;
                        row["SIRATIO"] = gvIndex.SplitRatio;
                        row["MaxPerNode"] = gvIndex.MaxPerNode;
                        row["MaxLevels"] = gvIndex.Levels;
                        #endregion
                    }
                    else if (sIndexDef is MSSpatialIndex)
                    {
                        #region MS Spatial Index
                        MSSpatialIndex msIndex = (MSSpatialIndex)sIndexDef;
                        if (msIndex.GeometryType == GeometryFieldType.MsGeometry)
                        {
                            row["DatasetID"] = dsID;
                            row["GeometryType"] = (int)FieldType.GEOMETRY;
                            row["SIMinX"] = msIndex.SpatialIndexBounds != null ? msIndex.SpatialIndexBounds.minx : 0.0;
                            row["SIMinY"] = msIndex.SpatialIndexBounds != null ? msIndex.SpatialIndexBounds.miny : 0.0;
                            row["SIMaxX"] = msIndex.SpatialIndexBounds != null ? msIndex.SpatialIndexBounds.maxx : 0.0;
                            row["SIMaxY"] = msIndex.SpatialIndexBounds != null ? msIndex.SpatialIndexBounds.maxy : 0.0;
                            row["SIRATIO"] = 0.0;
                            row["MaxPerNode"] = msIndex.CellsPerObject;
                            row["MaxLevels"] = msIndex.Levels;
                        }
                        else if (msIndex.GeometryType == GeometryFieldType.MsGeography)
                        {
                            row["DatasetID"] = dsID;
                            row["GeometryType"] = (int)FieldType.GEOGRAPHY;
                            row["SIMinX"] = 0.0;
                            row["SIMinY"] = 0.0;
                            row["SIMaxX"] = 0.0;
                            row["SIMaxY"] = 0.0;
                            row["SIRATIO"] = 0.0;
                            row["MaxPerNode"] = msIndex.CellsPerObject;
                            row["MaxLevels"] = msIndex.Levels;
                        }
                        #endregion
                    }
                    ds.Tables["DSGT"].Rows.Add(row);
                    if (!_conn.UpdateData(ref ds, "DSGT"))
                    {
                        await DeleteDataset(name);
                        _errMsg = _conn.errorMessage;
                        return -1;
                    }
                }
                #endregion
                return dsID;
            }
            catch (Exception ex)
            {
                await DeleteDataset(name);
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return -1;
            }
        }

        async virtual public Task<string[]> DatasetNames()
        {
            _errMsg = "";
            if (_conn == null)
            {
                return null;
            }

            try
            {
                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_Datasets"), "DS"))
                {
                    _errMsg = _conn.errorMessage;
                    return null;
                }
                StringBuilder sb = new StringBuilder();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append("@");
                    }

                    sb.Append(row["Name"].ToString());
                }
                ds.Dispose();
                return sb.ToString().Split('@');
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return null;
            }
        }

        async virtual public Task<int> CreateFeatureClass(
            string dsname,
            string fcname,
            IGeometryDef geomDef,
            IFieldCollection Fields)
        {
            return await CreateFeatureClass(dsname, fcname, geomDef, Fields, false);
        }

        async virtual protected Task<int> CreateFeatureClass(
            string dsname,
            string fcname,
            IGeometryDef geomDef,
            IFieldCollection Fields,
            bool recreate)
        {
            FieldCollection fields = new FieldCollection(Fields);

            if (_conn == null)
            {
                return -1;
            }

            int dsID = await DatasetID(dsname);
            if (dsID == -1)
            {
                _errMsg = "Dataset '" + dsname + "' does not exist!";
                return -1;
            }
            ISpatialIndexDef sIndexDef = await SpatialIndexDef(dsID);
            if (sIndexDef == null)
            {
                _errMsg = "Dataset '" + dsname + "' has no spatial index definition...";
                return -1;
            }
            bool tempFC = false;
            if (fcname.ToUpper().IndexOf("_TMP_") == 0)
            {
                tempFC = true;
            }

            if (geomDef == null)
            {
                _errMsg = "no GeometryDef...";
                return -1;
            }
            bool msSpatial = false;
            if (_conn.dbType == DBType.sql)
            {
                if (sIndexDef.GeometryType == GeometryFieldType.MsGeography ||
                    sIndexDef.GeometryType == GeometryFieldType.MsGeometry)
                {
                    msSpatial = true;
                }
            }
            try
            {
                DataSet ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcname + "'", "FC", !tempFC))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
                DataRow row = ds.Tables[0].NewRow();

                if (ds.Tables[0].Rows.Count > 0)
                {
                    if (recreate == true)
                    {
                        row = ds.Tables[0].Rows[0];
                    }
                    else
                    {
                        _errMsg = "already exists!";
                        return -1;
                    }
                }
                else
                {
                    row = ds.Tables[0].NewRow();
                }

                row["Name"] = fcname;
                row["DatasetID"] = dsID;
                row["GeometryType"] = (int)geomDef.GeometryType;
                row["HasZ"] = geomDef.HasZ;
                row["HasM"] = geomDef.HasM;
                row["ShapeField"] = "SHAPE";
                if (!recreate)
                {
                    ds.Tables[0].Rows.Add(row);
                }

                if (!tempFC)
                {
                    if (!_conn.UpdateData(ref ds, "FC"))
                    {
                        _errMsg = _conn.errorMessage;
                        return -1;
                    }
                }
                ds.Dispose();
                ds = null;

                if (fields == null)
                {
                    fields = new FieldCollection();
                }

                FieldCollection FieldsCopy = new FieldCollection();
                foreach (IField f in fields.ToEnumerable())
                {
                    FieldsCopy.Add(f);
                }

                foreach (IField f in FieldsCopy.ToEnumerable())
                {
                    if (f.type == FieldType.ID || ColumnName(f.name) == ColumnName("FDB_OID") || 
                        f.name.Contains("(")  // Function like Shape.STArea(), ...
                        )
                    {
                        fields.Remove(f);
                    }
                    else if ((f.type == FieldType.binary || f.type == FieldType.Shape) &&
                        ColumnName(f.name) == ColumnName("FDB_SHAPE"))
                    {
                        fields.Remove(f);
                    }
                    else if (ColumnName(f.name) == ColumnName("FDB_NID"))
                    {
                        fields.Remove(f);
                    }
                    if (_conn.dbType == DBType.npgsql && f is Field)
                    {
                        ((Field)f).name = ColumnName(f.name);
                    }
                }
                // OID
                Field field = new Field();
                field.name = ColumnName("FDB_OID"); field.aliasname = ColumnName("OID");
                //field.type=((tempFC) ? FieldType.integer : FieldType.ID);
                field.type = FieldType.ID;
                fields.Insert(0, field);
                // SHAPE
                if (geomDef.GeometryType == GeometryType.Point ||
                    geomDef.GeometryType == GeometryType.Polyline ||
                    geomDef.GeometryType == GeometryType.Polygon ||
                    geomDef.GeometryType == GeometryType.Multipoint ||
                    geomDef.GeometryType == GeometryType.Envelope ||
                    geomDef.GeometryType == GeometryType.Aggregate ||
                    geomDef.GeometryType == GeometryType.Unknown)
                {
                    field = new Field();
                    field.name = field.aliasname = ColumnName("FDB_SHAPE");
                    if (msSpatial)
                    {
                        switch (sIndexDef.GeometryType)
                        {
                            case GeometryFieldType.Default:
                                field.type = FieldType.Shape;
                                break;
                            case GeometryFieldType.MsGeography:
                                field.type = FieldType.GEOGRAPHY;
                                break;
                            case GeometryFieldType.MsGeometry:
                                field.type = FieldType.GEOMETRY;
                                break;
                        }
                    }
                    else
                    {
                        field.type = FieldType.binary;
                    }

                    fields.Insert(1, field);
                    // FDB_NID
                    if (!msSpatial)
                    {
                        field = new Field();
                        field.name = field.aliasname = ColumnName("FDB_NID");
                        field.type = FieldType.biginteger;
                        fields.Insert(2, field);
                    }
                }
                if (!CreateTable("FC_" + fcname, fields, msSpatial))
                {
                    return -1;
                }

                if (geomDef.GeometryType == GeometryType.Point ||
                    geomDef.GeometryType == GeometryType.Polyline ||
                    geomDef.GeometryType == GeometryType.Polygon ||
                    geomDef.GeometryType == GeometryType.Multipoint ||
                    geomDef.GeometryType == GeometryType.Envelope ||
                    geomDef.GeometryType == GeometryType.Aggregate ||
                    geomDef.GeometryType == GeometryType.Unknown)
                {
                    if (_conn.dbType == DBType.oledb)
                    {
                        if (!_conn.createIndex("FC_" + fcname + "_NID", "FC_" + fcname, "FDB_NID", false))
                        {
                            _errMsg = _conn.errorMessage;
                            return -1;
                        }
                    }
                    else if (_conn.dbType == DBType.sql)
                    {
                        if (msSpatial)
                        {
                            // keinen Index für dieses Feld
                        }
                        else
                        {
                            if (!_conn.createIndex("FC_" + System.Guid.NewGuid().ToString("N") + "_NID", "FC_" + fcname, "FDB_NID", false, false))
                            {
                                _errMsg = _conn.errorMessage;
                                return -1;
                            }
                        }
                    }
                    else if (_conn.dbType == DBType.npgsql)
                    {
                        if (!_conn.createIndex("fc_" + System.Guid.NewGuid().ToString("N").ToLower() + "_nid", "FC_" + fcname, "FDB_NID", false, true))
                        {
                            _errMsg = _conn.errorMessage;
                            return -1;
                        }
                    }
                    else
                    {
                        if (!_conn.createIndex("fc_" + System.Guid.NewGuid().ToString("N").ToLower() + "_nid", "FC_" + fcname, "FDB_NID", false, true))
                        {
                            _errMsg = _conn.errorMessage;
                            return -1;
                        }
                    }
                }
                if (tempFC)
                {
                    return -99;
                }

                int FC_ID = await FeatureClassID(dsID, fcname);
                if (FC_ID == -1)
                {
                    _errMsg = "Unknown !!!";
                    return -1;
                }

                ds = new DataSet();
                if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClassFields") + " WHERE " + DbColName("FClassID") + "=" + FC_ID, "FCF", true))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }

                foreach (DataRow row_ in ds.Tables[0].Select(""))
                {
                    ds.Tables[0].Rows.Remove(row_);
                }
                foreach (IField field_ in fields.ToEnumerable())
                {
                    if (field_.name.ToLower().IndexOf("fdb_") == 0)
                    {
                        continue;
                    }

                    row = ds.Tables[0].NewRow();
                    row["FClassID"] = FC_ID;
                    row["FieldName"] = field_.name;
                    row["Aliasname"] = field_.aliasname;
                    row["FieldType"] = (int)field_.type;
                    row["IsRequired"] = false;

                    if (ds.Tables[0].Columns["AutoFieldGUID"] != null &&
                        field_ is IAutoField &&
                        PlugInManager.IsPlugin(field_))
                    {
                        row["AutoFieldGUID"] = PlugInManager.PlugInID(field_).ToString();
                        row["IsEditable"] = false;
                    }
                    else
                    {
                        row["IsEditable"] = true;
                    }

                    ds.Tables[0].Rows.Add(row);
                }
                if (!_conn.UpdateData(ref ds, "FCF"))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }

                //this.InitSpatialIndex(name);
                if (geomDef.GeometryType == GeometryType.Point ||
                    geomDef.GeometryType == GeometryType.Polyline ||
                    geomDef.GeometryType == GeometryType.Polygon ||
                    geomDef.GeometryType == GeometryType.Multipoint ||
                    geomDef.GeometryType == GeometryType.Envelope ||
                    geomDef.GeometryType == GeometryType.Aggregate)
                {
                    if (msSpatial == false)
                    {
                        this.InitSpatialIndex2(fcname);
                    }
                }
                // Index für Netzwerk Graphen
                if (geomDef.GeometryType == GeometryType.Network)
                {
                    //_conn.createIndex("GRAPH_" + Guid.NewGuid().ToString("N"),
                    //    "FC_" + fcname,
                    //    "[N1] ASC,[N2] ASC", false, false);
                }
                return FC_ID;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return -1;
            }
        }

        async virtual public Task<int> CreateSpatialView(
            string dsname,
            string spatialViewName)
        {
            string[] spatialViewNames = SpatialViewNames(spatialViewName);
            int dsID = await DatasetID(dsname);
            if (dsID == -1)
            {
                _errMsg = "Dataset '" + dsname + "' does not exist!";
                return -1;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + spatialViewName + "'", "FC", true))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }
            DataRow row = ds.Tables[0].NewRow();

            if (ds.Tables[0].Rows.Count > 0)
            {
                _errMsg = "already exists!";
                return -1;
            }
            else
            {
                row = ds.Tables[0].NewRow();
            }

            row["Name"] = spatialViewName;
            row["DatasetID"] = dsID;
            row["GeometryType"] = GeometryType.Unknown;
            row["HasZ"] = false;
            row["HasM"] = false;
            row["ShapeField"] = "SHAPE";
            ds.Tables[0].Rows.Add(row);

            if (!_conn.UpdateData(ref ds, "FC"))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }

            int FC_ID = await FeatureClassID(dsID, spatialViewName);
            if (FC_ID == -1)
            {
                _errMsg = "Unknown !!!";
                return -1;
            }

            return FC_ID;
        }

        async virtual public Task<int> CreateLinkedFeatureClass(string dsname, IFeatureClass linkedFc)
        {
            int dsID = await DatasetID(dsname);
            if (dsID == -1)
            {
                _errMsg = "Dataset '" + dsname + "' does not exist!";
                return -1;
            }

            await FdbDataModel.UpdateLinkedFcDatatables(this);

            IDataset linkedDs = linkedFc.Dataset;

            Guid linkedDsGuid = PlugInManager.PlugInID(linkedDs);
            string linkedConnectionString = linkedDs.ConnectionString;

            linkedConnectionString = Crypto.Encrypt(linkedConnectionString, "gView.Linked.Dataset");

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_LinkedConnections") + " WHERE " + DbColName("Plugin") + "=" + GuidToSql(linkedDsGuid) + " AND " + DbColName("Connection") + "='" + linkedConnectionString + "'", "Linked", true))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }

            int linkedId = 0;
            if (ds.Tables["Linked"].Rows.Count == 0)
            {
                DataRow newLinked = ds.Tables["Linked"].NewRow();
                newLinked["Plugin"] = linkedDsGuid;
                newLinked["Connection"] = linkedConnectionString;
                ds.Tables["Linked"].Rows.Add(newLinked);

                if (!_conn.UpdateData(ref ds, "Linked"))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }

                await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_LinkedConnections") + " WHERE " + DbColName("Plugin") + "=" + GuidToSql(linkedDsGuid) + " AND " + DbColName("Connection") + "='" + linkedConnectionString + "'", "Linked2");
                linkedId = Convert.ToInt32(ds.Tables["Linked2"].Rows[0]["ID"]);
            }
            else
            {
                linkedId = Convert.ToInt32(ds.Tables["Linked"].Rows[0]["ID"]);
            }

            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + linkedFc.Name + "'", "FC", true))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }
            if (ds.Tables["FC"].Rows.Count > 0)
            {
                _errMsg = "Featureclass " + linkedFc.Name + " already exists! ";
                foreach (DataColumn col in ds.Tables["FC"].Columns)
                {
                    _errMsg += col.ColumnName + "=" + (ds.Tables["FC"].Rows[0][col.ColumnName] != null ? ds.Tables["FC"].Rows[0][col.ColumnName].ToString() : "") + ", ";
                }
                return -1;
            }

            DataRow row = ds.Tables["FC"].NewRow();

            row["Name"] = linkedFc.Name;
            row["DatasetID"] = dsID;
            row["GeometryType"] = (int)linkedFc.GeometryType;
            row["HasZ"] = linkedFc.HasZ;
            row["HasM"] = linkedFc.HasM;
            row["ShapeField"] = linkedFc.ShapeFieldName;
            row["SI"] = "Linked";
            row["SIVersion"] = linkedId;
            ds.Tables["FC"].Rows.Add(row);

            if (!_conn.UpdateData(ref ds, "FC"))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }

            int FC_ID = await FeatureClassID(dsID, linkedFc.Name);
            if (FC_ID == -1)
            {
                _errMsg = "Unknown !!!";
                return -1;
            }

            return FC_ID;
        }

        async virtual public Task<bool> DeleteFeatureClass(string fcName)
        {
            return await DeleteFeatureClass(fcName, true);
        }
        async virtual protected Task<bool> DeleteFeatureClass(string fcName, bool deleteFeatureClassesRow)
        {
            if (_conn == null)
            {
                return false;
            }

            if (await IsNetworkSubFeatureclass(fcName))
            {
                _errMsg = "Can't delete featureclass: delete network first!";
                return false;
            }
            if (!_conn.dropTable(FcsiTableName(fcName)))
            {
                if (await TableExists(TableName("FCSI_" + fcName))) // event. war Tab. schon gelöscht!!! Hier nicht DBSchema vor Tabelle angeben
                {
                    _errMsg = _conn.errorMessage;
                }
            }
            if (!_conn.dropTable(FcTableName(fcName)))
            {
                if (await TableExists(TableName("FC_" + fcName))) // event. war Tab. schon gelöscht!!! Hier nicht DBSchema vor Tabelle angeben
                {
                    _errMsg += "\n" + _conn.errorMessage;
                    return false;
                }
            }
            DataTable FCs = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + fcName + "'", "", deleteFeatureClassesRow);
            if (FCs == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            bool isNetwork = false;
            List<int> fcIDs = new List<int>();
            foreach (DataRow row in FCs.Rows)
            {
                fcIDs.Add(Convert.ToInt32(row["ID"]));
                if ((GeometryType)row["GeometryType"] == GeometryType.Network)
                {
                    isNetwork = true;
                }

                if (deleteFeatureClassesRow)
                {
                    row.Delete();
                }
            }
            if (deleteFeatureClassesRow)
            {
                if (!_conn.Update(FCs))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
            }
            foreach (int fcID in fcIDs)
            {
                _conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_FeatureClassFields") + " WHERE " + DbColName("FClassID") + "=" + fcID.ToString());

                if (isNetwork)
                {
                    _conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_Networks") + " WHERE ID=" + fcID.ToString());
                    _conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_NetworkClasses") + " WHERE NetworkId=" + fcID.ToString());
                    DataTable tab = await _conn.Select(DbColName("WeightGuid"), TableName("FDB_NetworkWeights"), DbColName("NetworkId") + "=" + fcID.ToString());
                    if (tab != null)
                    {
                        foreach (DataRow row in tab.Rows)
                        {
                            DropTable(TableName(fcName + "_Weights_" + ((Guid)row["WeightGuid"]).ToString("N").ToLower()));
                        }
                    }
                    _conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_NetworkWeights") + " WHERE " + DbColName("NetworkId") + "=" + fcID.ToString());
                }
            }

            if (isNetwork)
            {
                await DeleteFeatureClass(fcName + "_Nodes");
                await DeleteFeatureClass(fcName + "_ComplexEdges");
                DropTable(TableName(fcName + "_Edges"));
                DropTable(TableName(fcName + "_EdgeIndex"));
            }
            return true;
        }

        async virtual public Task<bool> DeleteDataset(string dsname)
        {
            if (_conn == null)
            {
                return false;
            }

            int dsID = await DatasetID(dsname);
            if (dsID == -1)
            {
                _errMsg = "Dataset doesn't exist!";
                return false;
            }

            var isImageDatasetResult = await IsImageDataset(dsname);
            string imageSpace = isImageDatasetResult.imageSpace;
            if (isImageDatasetResult.isImageDataset)
            {
                _conn.dropTable(TableName(dsname + "_IMAGE_DATA"));
                try
                {
                    DirectoryInfo di = new DirectoryInfo(dsname);
                    if (di.Exists)
                    {
                        IFeatureClass fc = await this.GetFeatureclass(dsname, dsname + "_IMAGE_POLYGONS");
                        QueryFilter filter = new QueryFilter();
                        filter.AddField(DbColName("MANAGED_FILE"));
                        IFeatureCursor cursor = await fc.Search(filter) as IFeatureCursor;

                        if (cursor != null)
                        {
                            IFeature feature;
                            while ((feature = await cursor.NextFeature()) != null)
                            {
                                if (feature[ColumnName("MANAGED_FILE")] == System.DBNull.Value)
                                {
                                    continue;
                                }

                                string file = feature[ColumnName("MANAGED_FILE")].ToString();
                                try
                                {
                                    FileInfo fi = new FileInfo(file);
                                    if (fi.Exists)
                                    {
                                        fi.Delete();
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            DataTable FCs = await _conn.Select(DbColName("Name"), TableName("FDB_FeatureClasses"), DbColName("DatasetID") + "=" + dsID);
            if (FCs == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            foreach (DataRow fcRow in FCs.Rows)
            {
                if (!await DeleteFeatureClass(fcRow["Name"].ToString()))
                {
                    return false;
                }
            }

            if (!_conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("ID") + "=" + dsID.ToString()))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            _conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_DatasetGeometryType") + " WHERE " + DbColName("DatasetID") + "=" + dsID.ToString());

            return true;
        }

        async virtual public Task<int> ReplaceFeatureClass(
            string dsname,
            string fcname,
            IGeometryDef geomDef,
            IFieldCollection Fields)
        {
            int fcId = await FeatureClassID(await DatasetID(dsname), fcname);
            if (fcId == -1)
            {
                return await CreateFeatureClass(dsname, fcname, geomDef, Fields);
            }
            if (!await DeleteFeatureClass(fcname, false))
            {
                return -1;
            }

            return await CreateFeatureClass(dsname, fcname, geomDef, Fields, true);
        }

        abstract public Task<IFeatureDataset> GetDataset(string dsname);

        virtual protected string FieldDataType(IField field)
        {
            if (field == null)
            {
                return "";
            }

            switch (field.type)
            {
                case FieldType.biginteger:
                case FieldType.integer:
                case FieldType.smallinteger:
                    return "INTEGER";
                case FieldType.boolean:
                    return "YESNO";
                case FieldType.Float:
                    return "FLOAT";
                case FieldType.Double:
                    return "DOUBLE";
                case FieldType.Date:
                    return "DATE";
                case FieldType.ID:
                    return "INTEGER";
                case FieldType.binary:
                    return "OLEOBJECT";
                case FieldType.character:
                    return "TEXT(1)";
                case FieldType.String:
                    if (field.size > 0 && field.size < 256)
                    {
                        return "TEXT(" + field.size + ")";
                    }
                    else if (field.size > 255)
                    {
                        return "MEMO";
                    }
                    else if (field.size <= 0)
                    {
                        return "TEXT(255)";
                    }

                    break;
                case FieldType.guid:
                    return "GUID";
                case FieldType.replicationID:
                    return "GUID NOT NULL";
                default:
                    return "TEXT(255)";
            }
            return "";
        }
        virtual protected bool CreateTable(string name, IFieldCollection Fields, bool msSpatial)
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
                    if (field.type == FieldType.Shape)
                    {
                        continue;
                    }

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
                        case FieldType.integer:
                        case FieldType.smallinteger:
                            types.Append("INTEGER");
                            break;
                        case FieldType.boolean:
                            types.Append("YESNO");
                            break;
                        case FieldType.Float:
                            types.Append("FLOAT");
                            break;
                        case FieldType.Double:
                            types.Append("DOUBLE");
                            break;
                        case FieldType.Date:
                            types.Append("DATE");
                            break;
                        case FieldType.ID:
                            if (!hasID)
                            {
                                types.Append("INTEGER NOT NULL IDENTITY(1,1)");
                                hasID = true;
                                idField = field.name;
                            }
                            else
                            {
                                types.Append("INTEGER");
                            }
                            break;
                        case FieldType.GEOMETRY:  // gibts nicht im Access
                        case FieldType.GEOGRAPHY:
                        case FieldType.Shape:
                        case FieldType.binary:
                            types.Append("OLEOBJECT");
                            break;
                        case FieldType.character:
                            types.Append("TEXT(1)");
                            break;
                        case FieldType.String:
                            if (field.size > 0 && field.size < 256)
                            {
                                types.Append("TEXT(" + field.size + ")");
                            }
                            else if (field.size > 255)
                            {
                                types.Append("MEMO");
                            }
                            else if (field.size <= 0)
                            {
                                types.Append("TEXT(255)");
                            }

                            break;
                        case FieldType.guid:
                            types.Append("GUID");
                            break;
                        case FieldType.replicationID:
                            types.Append("GUID NOT NULL");
                            break;
                        default:
                            types.Append("TEXT(255)");
                            break;

                    }
                }

                if (idField != "")
                {
                    fields.Append(";Primary Key([" + idField + "])");
                    types.Append(";");
                }
                if (!_conn.createTable(name, fields.ToString().Split(';'), types.ToString().Split(';')))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }

        /*
        virtual public IFeatureCursor Query(string FCName,IQueryFilter filter) 
        {
            if(_conn==null) return null;
			
            string subfields="";
            if(filter!=null) 
            {
                filter.fieldPrefix="[";
                filter.fieldPostfix="]";
                if (filter is ISpatialFilter)
                {
                    if (((ISpatialFilter)filter).FuzzyQuery == false) filter.AddField("FDB_SHAPE");
                }
                subfields=filter.SubFields.Replace(" ",",");
            }
            if(subfields=="") subfields="*";

            List<int> NIDs=null;
            IGeometry queryGeometry=null;

            if(filter is ISpatialFilter) 
            {
                if(_spatialSearchTrees[FCName]==null) 
                {
                    _spatialSearchTrees[FCName]=this.SpatialSearchTree(FCName);
                }
                BinarySearchTree tree=(BinarySearchTree)_spatialSearchTrees[FCName];
                if(tree!=null && ((ISpatialFilter)filter).Geometry!=null) 
                {
                    NIDs=tree.CollectNIDs(((ISpatialFilter)filter).Geometry.Envelope);
                }

                if (((ISpatialFilter)filter).FuzzyQuery == false)
                {

                    queryGeometry = ((ISpatialFilter)filter).Geometry;
                }
            } 
			
            string sql="SELECT "+subfields+" FROM FC_"+FCName;
            string where="";
            if(filter!=null) 
            {
                if(filter.WhereClause!="") 
                {
                    where=filter.WhereClause;
                }
            }
            return new AccessFDBFeatureCursor(_conn.connectionString,sql,where,filter.OrderBy,NIDs,queryGeometry,this.GetGeometryDef(FCName));
        }
        */
        abstract public Task<IFeatureCursor> Query(IFeatureClass fc, IQueryFilter filter);

        //virtual public IFeatureCursor QueryIDs(string FCName,string subFields,List<int> IDs, ISpatialReference toSRef) 
        //{
        //    string sql="SELECT "+subFields+" FROM FC_"+FCName;
        //    return new AccessFDBFeatureCursorIDs(_conn.connectionString,sql,IDs,this.GetGeometryDef(FCName),toSRef);
        //}
        abstract public Task<IFeatureCursor> QueryIDs(IFeatureClass fc, string subFields, List<int> IDs, ISpatialReference toSRef);

        public delegate void FeatureClassRenamedEventHandler(string oldName, string newName);
        public event FeatureClassRenamedEventHandler FeatureClassRenamed = null;

        async public virtual Task<bool> RenameFeatureClass(string FCName, string newFCName)
        {
            if (FCName.Contains("@"))
            {
                return false;
            }

            if (_conn == null)
            {
                return false;
            }

            if (await IsNetworkSubFeatureclass(FCName))
            {
                _errMsg = "rename network featureclass!";
                return false;
            }

            DataTable tab = await _conn.Select(DbColName("ID") + "," + DbColName("Name"), TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + newFCName + "'", "", false);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count != 0)
            {
                _errMsg = "Featureclass '" + newFCName + "' already exits...";
                return false;
            }

            tab = await _conn.Select(DbColName("ID") + "," + DbColName("Name") + "," + DbColName("GeometryType"), TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'", "", true);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count != 1)
            {
                _errMsg = "Can't find Featureclass...";
                return false;
            }
            bool isNetwork = ((GeometryType)tab.Rows[0]["GeometryType"] == GeometryType.Network);
            int fcId = Convert.ToInt32(tab.Rows[0]["ID"]);
            // Beim Umbennenen Schema für Datenbank nicht zum Tabellennamen hinzufügen
            if (await TableExists("FCSI_" + FCName))  // Gibts zB nicht bei Sql2008 GEOMETRY; TableExists...DbSchema nicht angeben
            {
                if (!_conn.RenameTable(FcsiTableName(FCName), "FCSI_" + newFCName))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
            }
            if (!_conn.RenameTable(FcTableName(FCName), "FC_" + newFCName))
            {
                _errMsg = _conn.errorMessage;
                _conn.RenameTable(FcsiTableName(newFCName), "FCSI_" + FCName);
                return false;
            }
            tab.Rows[0]["Name"] = newFCName;
            if (!_conn.Update(tab))
            {
                _errMsg = _conn.errorMessage;
                _conn.RenameTable(FcTableName(newFCName), "FC_" + FCName);
                _conn.RenameTable(FcsiTableName(newFCName), "FCSI_" + FCName);
                return false;
            }

            if (isNetwork)
            {
                await RenameFeatureClass(FCName + "_Nodes", newFCName + "_Nodes");
                await RenameFeatureClass(FCName + "_ComplexEdges", newFCName + "_ComplexEdges");
                _conn.RenameTable(FCName + "_Edges", newFCName + "_Edges");
                _conn.RenameTable(FCName + "_EdgeIndex", newFCName + "_EdgeIndex");

                DataTable tab2 = await _conn.Select("WeightGuid", "FDB_NetworkWeights", "NetworkId=" + fcId.ToString());
                if (tab2 != null)
                {
                    foreach (DataRow row in tab2.Rows)
                    {
                        _conn.RenameTable(FCName + "_Weights_" + ((Guid)row["WeightGuid"]).ToString("N").ToLower(),
                                       newFCName + "_Weights_" + ((Guid)row["WeightGuid"]).ToString("N").ToLower());
                    }
                }
            }

            if (FeatureClassRenamed != null)
            {
                FeatureClassRenamed(FCName, newFCName);
            }

            return true;
        }

        public delegate void DatasetRenamedEventHandler(string oldName, string newName);
        public event DatasetRenamedEventHandler DatasetRenamed = null;

        async public virtual Task<bool> RenameDataset(string DSName, string newDSName)
        {
            if (_conn == null)
            {
                return false;
            }

            DataTable tab = await _conn.Select(DbColName("ID") + "," + DbColName("Name"), TableName("FDB_Datasets"), DbColName("Name") + "='" + newDSName + "'", "", false);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count != 0)
            {
                _errMsg = "Dataset '" + newDSName + "' already exists...";
                return false;
            }

            var isImageDatasetResult = await IsImageDataset(DSName);
            string imageSpace = isImageDatasetResult.imageSpace;
            if (isImageDatasetResult.isImageDataset)
            {
                if (!_conn.RenameTable(DSName + "_IMAGE_DATA", newDSName + "_IMAGE_DATA"))
                {
                    _errMsg = "Rename IMAGE_DATA: " + _conn.errorMessage;
                    return false;
                }
                if (!await RenameFeatureClass(DSName + "_IMAGE_POLYGONS", newDSName + "_IMAGE_POLYGONS"))
                {
                    _conn.RenameTable(newDSName + "_IMAGE_DATA", DSName + "_IMAGE_DATA");
                    return false;
                }
            }

            tab = await _conn.Select(DbColName("ID") + "," + DbColName("Name"), TableName("FDB_Datasets"), DbColName("Name") + "='" + DSName + "'", "", true);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count != 1)
            {
                _errMsg = "Can't find Dataset...";
                return false;
            }

            tab.Rows[0]["Name"] = newDSName;
            if (!_conn.Update(tab))
            {
                _errMsg = _conn.errorMessage;

                if ((await IsImageDataset(DSName)).isImageDataset)
                {
                    _conn.RenameTable(newDSName + "_IMAGE_DATA", DSName + "_IMAGE_DATA");
                    await RenameFeatureClass(newDSName + "_IMAGE_POLYGONS", DSName + "_IMAGE_POLYGONS");
                }
                return false;
            }

            if (DatasetRenamed != null)
            {
                DatasetRenamed(DSName, newDSName);
            }

            return true;
        }
        #endregion

        #region IFeatureUpdater Member

        public virtual Task<bool> Insert(IFeatureClass fClass, IFeature feature)
        {
            List<IFeature> features = new List<IFeature>();
            features.Add(feature);
            return Insert(fClass, features);
        }
        public abstract Task<bool> Insert(IFeatureClass fClass, List<IFeature> features);

        public abstract Task<bool> Update(IFeatureClass fClass, IFeature feature);
        public abstract Task<bool> Update(IFeatureClass fClass, List<IFeature> features);

        public abstract Task<bool> Delete(IFeatureClass fClass, int oid);
        public abstract Task<bool> Delete(IFeatureClass fClass, string where);

        public virtual int SuggestedInsertFeatureCountPerTransaction
        {
            get { return 1000; }
        }

        #endregion

        #region DB

        public bool DropTable(string name)
        {
            if (_conn == null)
            {
                return false;
            }

            if (!_conn.dropTable(name))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        protected bool DeleteAllFeatures(string fcname)
        {
            if (_conn == null)
            {
                return false;
            }

            return _conn.ExecuteNoneQuery("DELETE FROM " + FcTableName(fcname));
        }
        protected bool CompressDB()
        {
            if (_conn is CommonDbConnection)
            {
                return false;
            }

            return ((CommonDbConnection)_conn).CompactAccessDB();
        }
        public bool DropIndex(string name)
        {
            if (_conn == null)
            {
                return false;
            }

            if (!_conn.dropIndex(name))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        public bool CreateIndex(string name, string table, string field, bool unique)
        {
            if (_conn == null)
            {
                return false;
            }

            if (!_conn.createIndex(name, table, field, unique))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        /*
        private DataRow [] SpatialIndexNIDs(string fcname) 
        {
            if(_conn==null) return null;

            DataSet ds=new DataSet();
            if(!_conn.SQLQuery(ref ds,"SELECT NID FROM FC_INDEX_"+fcname,"NIDs")) 
            {
                _errMsg=_conn.errorMessage;
                return null;
            }

            return ds.Tables[0].Select("","NID");
        }
        */
        async public Task<int> CountFeatures(string fcname)
        {
            if (_conn == null)
            {
                return 0;
            }

            try
            {
                int count = Convert.ToInt32(await _conn.QuerySingleField("SELECT COUNT(" + DbColName("FDB_OID") + ") AS " + ColumnName("COUNT_FEATURES") + " FROM " + FcTableName(fcname), ColumnName("COUNT_FEATURES")));
                return count;
            }
            catch (Exception ex)
            {
                _errMsg = _conn.errorMessage + " " + ex.Message;
                return 0;
            }
        }

        virtual public IFieldCollection TableFields(string name)
        {
            if (_conn == null)
            {
                return null;
            }

            DataTable schema = _conn.GetSchema2(name);
            if (schema == null)
            {
                return null;
            }

            FieldCollection fields = new FieldCollection();
            foreach (DataRow row in schema.Rows)
            {
                Field field = new Field(row);
                fields.Add(field);
            }
            return fields;
        }

        #endregion

        #region SpatialIndex

        async virtual public Task<ISpatialIndexDef> SpatialIndexDef(string dsName)
        {
            return await SpatialIndexDef(await this.DatasetID(dsName));
        }
        async public Task<ISpatialIndexDef> SpatialIndexDef(int dsID)
        {
            if (FdbVersion >= new Version(1, 2, 0))
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_DatasetGeometryType"), DbColName("DatasetID") + "=" + dsID);
                if (tab != null && tab.Rows.Count == 1)
                {
                    DataRow row = tab.Rows[0];
                    if ((GeometryFieldType)row["GeometryType"] == GeometryFieldType.Default)
                    {
                        gViewSpatialIndexDef gvIndex = new gViewSpatialIndexDef(
                            new Envelope((double)row["SIMinX"], (double)row["SIMinY"], (double)row["SIMaxX"], (double)row["SIMaxY"]),
                            Convert.ToInt32(row["MaxLevels"]),
                            Convert.ToInt32(row["MaxPerNode"]),
                            (double)row["SIRATIO"]);
                        gvIndex.SpatialReference = await this.SpatialReference(dsID);
                        return gvIndex;
                    }
                    else if ((GeometryFieldType)row["GeometryType"] == GeometryFieldType.MsGeometry)
                    {
                        MSSpatialIndex msIndex = new MSSpatialIndex();
                        msIndex.GeometryType = GeometryFieldType.MsGeometry;
                        msIndex.SpatialIndexBounds = new Envelope((double)row["SIMinX"], (double)row["SIMinY"], (double)row["SIMaxX"], (double)row["SIMaxY"]);
                        msIndex.CellsPerObject = Convert.ToInt32(row["MaxPerNode"]);
                        msIndex.Levels = Convert.ToInt32(row["MaxLevels"]);
                        msIndex.SpatialReference = await this.SpatialReference(dsID);
                        return msIndex;
                    }
                    else if ((GeometryFieldType)row["GeometryType"] == GeometryFieldType.MsGeography)
                    {
                        MSSpatialIndex msIndex = new MSSpatialIndex();
                        msIndex.GeometryType = GeometryFieldType.MsGeography;
                        msIndex.SpatialIndexBounds = new Envelope();
                        msIndex.CellsPerObject = Convert.ToInt32(row["MaxPerNode"]);
                        msIndex.Levels = Convert.ToInt32(row["MaxLevels"]);
                        msIndex.SpatialReference = await this.SpatialReference(dsID);
                        return msIndex;
                    }
                }
            }
            gViewSpatialIndexDef sDef = new gViewSpatialIndexDef();
            sDef.SpatialReference = await this.SpatialReference(dsID);
            return sDef;
        }
        async public Task<ISpatialIndexDef> FcSpatialIndexDef(string fcName)
        {
            fcName = OriginFcName(fcName);
            try
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + fcName + "'");

                if (tab != null && tab.Rows.Count == 1)
                {
                    DataRow row = tab.Rows[0];
                    string si = row["SI"].ToString().ToLower();
                    if (si == "binarytree" || si == "binarytree2")
                    {
                        return new gViewSpatialIndexDef(
                            new Envelope((double)row["SIMinX"], (double)row["SIMinY"], (double)row["SIMaxX"], (double)row["SIMaxY"]),
                            Convert.ToInt32(row["MaxLevels"]),
                            Convert.ToInt32(row["MaxPerNode"]),
                            (double)row["SIRATIO"]);
                    }
                    else if (si == "msgeometry")
                    {
                        MSSpatialIndex msIndex = new MSSpatialIndex();
                        msIndex.GeometryType = GeometryFieldType.MsGeometry;
                        msIndex.SpatialIndexBounds = new Envelope((double)row["SIMinX"], (double)row["SIMinY"], (double)row["SIMaxX"], (double)row["SIMaxY"]);
                        msIndex.CellsPerObject = Convert.ToInt32(row["MaxPerNode"]);
                        msIndex.Levels = Convert.ToInt32(row["MaxLevels"]);
                        return msIndex;
                    }
                    else if (si == "msgeography")
                    {
                        MSSpatialIndex msIndex = new MSSpatialIndex();
                        msIndex.GeometryType = GeometryFieldType.MsGeometry;
                        msIndex.SpatialIndexBounds = new Envelope();
                        msIndex.CellsPerObject = Convert.ToInt32(row["MaxPerNode"]);
                        msIndex.Levels = Convert.ToInt32(row["MaxLevels"]);
                        return msIndex;
                    }
                }
            }
            catch
            {

            }
            return new gViewSpatialIndexDef();
        }

        #endregion

        #region SpatialIndex2
        async virtual public Task<bool> SetSpatialIndexBounds(string FCName, string TreeType, IEnvelope Bounds, double SpatialRatio, int maxPerNode, int maxLevels)
        {
            if (_conn == null || Bounds == null)
            {
                return false;
            }

            FCName = OriginFcName(FCName);
            DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'", "", true);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count == 0)
            {
                _errMsg = "FeatureClass not found...";
                return false;
            }
            tab.Rows[0]["SI"] = TreeType;
            tab.Rows[0]["SIMinX"] = Bounds.minx;
            tab.Rows[0]["SIMinY"] = Bounds.miny;
            tab.Rows[0]["SIMaxX"] = Bounds.maxx;
            tab.Rows[0]["SIMaxY"] = Bounds.maxy;
            tab.Rows[0]["SIRATIO"] = SpatialRatio;
            tab.Rows[0]["MaxPerNode"] = maxPerNode;
            if (tab.Columns["MaxLevels"] != null)
            {
                tab.Rows[0]["MaxLevels"] = maxLevels;
            }

            if (!_conn.Update(tab))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        protected bool InitSpatialIndex2(string FCName)
        {
            if (_conn == null)
            {
                return false;
            }

            FCName = OriginFcName(FCName);

            string name_nodes = "FCSI_" + FCName;

            FieldCollection Fields = new FieldCollection();
            if (_indexType == IndexType.BinaryTree)
            {
                // NID
                Field field = new Field();
                field.name = ColumnName("NID"); field.aliasname = ColumnName("NID");
                field.type = FieldType.integer;
                Fields.Insert(0, field);
                // PID
                field = new Field();
                field.name = ColumnName("PID"); field.aliasname = ColumnName("PID");
                field.type = FieldType.integer;
                Fields.Insert(1, field);
                // Page
                field = new Field();
                field.name = ColumnName("Page"); field.aliasname = ColumnName("Page");
                field.type = FieldType.smallinteger;
                Fields.Insert(2, field);
            }
            else if (_indexType == IndexType.BinaryTree2)
            {
                // ID ... damit auch Update/Remove in der Tabelle möglich ist...
                Field field = new Field();
                field.name = ColumnName("ID"); field.aliasname = ColumnName("ID");
                field.type = FieldType.ID;
                Fields.Insert(0, field);
                // NID
                field = new Field();
                field.name = ColumnName("NID"); field.aliasname = ColumnName("NID");
                field.type = FieldType.biginteger;
                Fields.Insert(0, field);
            }

            if (!CreateTable(name_nodes, Fields, false))
            {
                return false;
            }
            return true;
        }
        public Task<bool> __intInsertSpatialIndexNodes2(string FCName, List<SpatialIndexNode> nodes)
        {
            return InsertSpatialIndexNodes2(FCName, nodes);
        }
        async protected Task<bool> InsertSpatialIndexNodes2(string FCName, List<SpatialIndexNode> nodes)
        {
            if (_conn == null)
            {
                return false;
            }

            FCName = OriginFcName(FCName);

            string name_nodes = FcsiTableName(FCName);

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + name_nodes + " WHERE NID=-1", "NODES", true))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            foreach (ISpatialIndexNode node in nodes)
            {
                DataRow row = ds.Tables["NODES"].NewRow();
                row["NID"] = node.NID;
                row["PID"] = node.PID;
                row["Page"] = node.Page;
                ds.Tables["NODES"].Rows.Add(row);
            }

            if (!_conn.UpdateData(ref ds, "NODES"))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }

        async public Task<string> SpatialIndexType(string FCName)
        {
            if (_conn == null)
            {
                return String.Empty;
            }

            FCName = OriginFcName(FCName);
            DataTable tab = await _conn.Select(DbColName("SI"), TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'", "");
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return String.Empty;
            }
            if (tab.Rows.Count == 0)
            {
                _errMsg = "FeatureClass not found...";
                return String.Empty;
            }

            return Convert.ToString(tab.Rows[0]["SI"]);  // kann auch DbNull sein, darum indirektes Casting!
        }
        async public Task<ISearchTree> SpatialSearchTree(string FCName)
        {
            if (_conn == null)
            {
                return null;
            }

            FCName = OriginFcName(FCName);
            DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'", "");
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return null;
            }
            if (tab.Rows.Count == 0)
            {
                _errMsg = "FeatureClass not found...";
                return null;
            }
            Envelope Bounds = new Envelope(0, 0, 0, 0);
            try
            {
                Bounds = new Envelope((double)tab.Rows[0]["SIMinX"], (double)tab.Rows[0]["SIMinY"], (double)tab.Rows[0]["SIMaxX"], (double)tab.Rows[0]["SIMaxY"]);
            }
            catch { }
            if (tab.Rows[0]["SI"].ToString() == "BinaryTree")
            {
                BinarySearchTree tree = new BinarySearchTree(Bounds, (double)tab.Rows[0]["SIRATIO"]);

                tab = await _conn.Select("*", FcsiTableName(FCName), "", "PID");
                if (tab == null)
                {
                    _errMsg = _conn.errorMessage;
                    return null;
                }
                if (tab.Rows.Count == 0)
                {
                    _errMsg = "No Nodes...";
                    return null;
                }
                BinarySearchTreeNode root = new BinarySearchTreeNode();
                root.NID = 0;
                InsertBinarySearchTreeNodes(root, tab);

                tree.Root = root;

                return tree;
            }
            else if (tab.Rows[0]["SI"].ToString() == "BinaryTree2")
            {
                //System.Windows.Forms.MessageBox.Show("Before Distinct");
                DataTable distinct = await _conn.Select("DISTINCT " + DbColName("NID"), FcsiTableName(FCName), "", DbColName("NID"));
                if (distinct == null)
                {
                    return null;
                }
                //System.Windows.Forms.MessageBox.Show("After Distinct");

                List<long> nodeNumbers = new List<long>();
                foreach (DataRow row in distinct.Rows)
                {
                    nodeNumbers.Add(Convert.ToInt64(row[0]));
                }

                BinarySearchTree2 tree2 = new BinarySearchTree2(
                    Bounds,
                    Convert.ToInt32(tab.Rows[0]["MaxLevels"]),
                    Convert.ToInt32(tab.Rows[0]["MaxPerNode"]),
                    (double)tab.Rows[0]["SIRATIO"],
                    nodeNumbers);

                tree2.IndexVersion = (tab.Rows[0]["SIVersion"] != null && tab.Rows[0]["SIVersion"] != System.DBNull.Value) ? Convert.ToInt32(tab.Rows[0]["SIVersion"]) : tree2.IndexVersion;
                tree2.TreeNodeAdded += new BinarySearchTree2.TreeNodeAddedEventHander(SpatialIndexTree_TreeNodeAdded);
                return tree2;
            }
            //else if (tab.Rows[0]["SI"].ToString().ToUpper() == "GEOMETRY")
            //{
            //    MSSpatialIndex index = new MSSpatialIndex();
            //    index.BoundingBox = Bounds;
            //    index.Type = MSSpatialIndexType.Geometry;
            //    index.CellsPerObject = (int)tab.Rows[0]["MaxPerNode"];
            //    int levels = (int)tab.Rows[0]["MaxLevels"];
            //    index.Level1 = (MSSpatialIndexLevelSize)(levels & 0xff);
            //    index.Level2 = (MSSpatialIndexLevelSize)((levels >> 4) & 0xff);
            //    index.Level3 = (MSSpatialIndexLevelSize)((levels >> 8) & 0xff);
            //    index.Level4 = (MSSpatialIndexLevelSize)((levels >> 12) & 0xff);
            //    return index;
            //}
            //else if (tab.Rows[0]["SI"].ToString().ToUpper() == "GEOGRAPHY")
            //{
            //    MSSpatialIndex index = new MSSpatialIndex();
            //    index.BoundingBox = Bounds;
            //    index.Type = MSSpatialIndexType.Geometry;
            //    index.CellsPerObject = (int)tab.Rows[0]["MaxPerNode"];
            //    int levels = (int)tab.Rows[0]["MaxLevels"];
            //    index.Level1 = (MSSpatialIndexLevelSize)(levels & 0xff);
            //    index.Level2 = (MSSpatialIndexLevelSize)((levels >> 4) & 0xff);
            //    index.Level3 = (MSSpatialIndexLevelSize)((levels >> 8) & 0xff);
            //    index.Level4 = (MSSpatialIndexLevelSize)((levels >> 12) & 0xff);
            //    return index;
            //}
            return null;
        }

        async private void SpatialIndexTree_TreeNodeAdded(object sender, long nid)
        {
            if (_conn == null)
            {
                return;
            }

            try
            {
                string fcName = "";
                foreach (string key in _spatialSearchTrees.Keys)
                {
                    if (_spatialSearchTrees[key] == sender)
                    {
                        fcName = key;
                        break;
                    }
                }
                if (fcName == "")
                {
                    return;
                }

                await AddTreeNode(fcName, nid);
            }
            catch
            {
            }
        }

        async virtual protected Task AddTreeNode(string fcName, long nid)
        {
            if (_conn.dbType == DBType.sql)
            {
                _conn.ExecuteNoneQuery("IF NOT EXISTS (SELECT NID FROM " + FcsiTableName(fcName) + " WHERE NID=" + nid.ToString() + ") INSERT INTO " + FcsiTableName(fcName) + " (NID) VALUES (" + nid.ToString() + ")");
            }
            else
            {
                _conn.ExecuteNoneQuery("INSERT INTO " + FcsiTableName(fcName) + " (" + DbColName("NID") + ") VALUES (" + nid.ToString() + ")");
            }

            await IncSpatialIndexVersion(fcName);
        }

        async public Task CheckSpatialSearchTreeVersions()
        {
            await CheckSpatialSearchTreeVersion(String.Empty);
        }
        async public Task CheckSpatialSearchTreeVersion(string FCName)
        {
            if (_spatialSearchTrees == null)
            {
                return;
            }

            FCName = OriginFcName(FCName);
            if (String.IsNullOrEmpty(FCName))
            {
                DataTable tab = await _conn.Select(DbColName("Name") + "," + DbColName("SIVersion"), TableName("FDB_FeatureClasses"));
                if (tab == null)
                {
                    return;
                }

                foreach (DataRow row in tab.Rows)
                {
                    BinarySearchTree2 tree = CachedSpatialSearchTree(row["Name"].ToString()) as BinarySearchTree2;
                    if (tree == null)
                    {
                        continue;
                    }

                    int indexVersion = (row["SIVersion"] != null && row["SIVersion"] != System.DBNull.Value) ? Convert.ToInt32(row["SIVersion"]) : 0;
                    if (tree.IndexVersion < indexVersion)
                    {
                        _spatialSearchTrees.Remove(row["Name"].ToString());
                    }
                }
            }
            else
            {
                DateTime td = DateTime.Now;
                BinarySearchTree2 tree = CachedSpatialSearchTree(FCName) as BinarySearchTree2;
                if (tree == null)
                {
                    return;
                }

                int siVersion = 0;
                object siVersionObject = await _conn.QuerySingleField("SELECT " + DbColName("SIVersion") + " FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + FCName + "'", ColumnName("SIVersion"));
                if (siVersionObject != System.DBNull.Value && siVersionObject != null)
                {
                    siVersion = Convert.ToInt32(siVersionObject);
                }

                if (tree.IndexVersion < siVersion)
                {
                    _spatialSearchTrees.Remove(FCName);
                }

                TimeSpan ts = DateTime.Now - td;
                double ms = ts.TotalSeconds;
            }
        }
        async private Task IncSpatialIndexVersion(string fcName)
        {
            if (_conn == null)
            {
                return;
            }

            fcName = OriginFcName(fcName);
            try
            {
                int siVersion = 1;
                object siVersionObject = await _conn.QuerySingleField("SELECT " + DbColName("SIVersion") + " FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcName + "'", ColumnName("SIVersion"));
                if (siVersionObject != System.DBNull.Value && siVersionObject != null)
                {
                    siVersion = Convert.ToInt32(siVersionObject) + 1;
                }
                if (!_conn.ExecuteNoneQuery("UPDATE " + TableName("FDB_FeatureClasses") + " SET " + DbColName("SIVersion") + "=" + siVersion + " WHERE " + DbColName("Name") + "='" + fcName + "'"))
                {

                }
            }
            catch { }
        }

        public ISearchTree CachedSpatialSearchTree(string FCName)
        {
            FCName = OriginFcName(FCName);
            return (ISearchTree)_spatialSearchTrees[FCName];
        }

        private void InsertBinarySearchTreeNodes(BinarySearchTreeNode parent, DataTable tab)
        {
            DataRow[] rows = tab.Select("PID=" + parent.NID);
            foreach (DataRow row in rows)
            {
                BinarySearchTreeNode node = new BinarySearchTreeNode();
                node.NID = Convert.ToInt32(row["NID"]);
                node.Page = (short)(Convert.ToInt32(row["Page"]));
                InsertBinarySearchTreeNodes(node, tab);

                if (parent.ChildNodes == null)
                {
                    parent.ChildNodes = new List<BinarySearchTreeNode>();
                }

                parent.ChildNodes.Add(node);
            }
        }
        async private Task<List<int>> SpatialIndexNodeIDs2(string FCName, int NID)
        {
            if (_conn == null)
            {
                return null;
            }

            FCName = OriginFcName(FCName);
            DataTable oids = await _conn.Select(DbColName("FDB_OID"), FcTableName(FCName), DbColName("FDB_NID") + "=" + NID, DbColName("FDB_OID"));
            if (oids == null)
            {
                _errMsg = _conn.errorMessage;
                return null;
            }

            List<int> IDs = new List<int>();
            foreach (DataRow row in oids.Rows)
            {
                IDs.Add(Convert.ToInt32(row[0]));
            }
            return IDs;
        }
        public Task<List<SpatialIndexNode>> SpatialIndexNodes2(string FCName)
        {
            return SpatialIndexNodes2(FCName, false);
        }

        async protected Task<List<SpatialIndexNode>> SpatialIndexNodes2(string FCName, bool ignoreIDs)
        {
            FCName = OriginFcName(FCName);
            List<SpatialIndexNode> nodes = new List<SpatialIndexNode>();
            BinarySearchTree tree = await SpatialSearchTree(FCName) as BinarySearchTree;
            if (tree == null)
            {
                return nodes;
            }

            SpatialIndexNodes2(tree.Root, -1, nodes);

            foreach (SpatialIndexNode node in nodes)
            {
                if (ignoreIDs)
                {
                    node.IDs = null;
                }
                else
                {
                    node.IDs = await SpatialIndexNodeIDs2(FCName, node.NID);
                }
            }

            return nodes;
        }

        private void SpatialIndexNodes2(BinarySearchTreeNode node, int PID, List<SpatialIndexNode> list)
        {
            if (node == null)
            {
                return;
            }

            SpatialIndexNode snode = new SpatialIndexNode();
            snode.NID = node.NID;
            snode.PID = PID;
            snode.Page = node.Page;
            list.Add(snode);
            if (node.ChildNodes == null)
            {
                return;
            }

            foreach (BinarySearchTreeNode childnode in node.ChildNodes)
            {
                SpatialIndexNodes2(childnode, node.NID, list);
            }
        }

        async public Task<ISpatialTreeInfo> SpatialTreeInfo(string FCName)
        {
            if (_conn == null)
            {
                return null;
            }

            FCName = OriginFcName(FCName);

            try
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'");
                if (tab == null)
                {
                    _errMsg = _conn.errorMessage;
                    return null;
                }
                if (tab.Rows.Count == 0)
                {
                    _errMsg = "Featureclass not found...";
                    return null;
                }
                SpatialTreeInfo si = new SpatialTreeInfo();
                si.type = tab.Rows[0]["SI"].ToString();
                si.SpatialRatio = (double)tab.Rows[0]["SIRATIO"];
                si.MaxFeaturesPerNode = Convert.ToInt32(tab.Rows[0]["MaxPerNode"]);
                si.Bounds = new Envelope(
                    (double)tab.Rows[0]["SIMinX"],
                    (double)tab.Rows[0]["SIMinY"],
                    (double)tab.Rows[0]["SIMaxX"],
                    (double)tab.Rows[0]["SIMaxY"]);
                return si;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return null;
            }
        }

        async protected Task<bool> SetSpatialTreeInfo(string FCName, ISpatialTreeInfo info)
        {
            if (_conn == null)
            {
                return false;
            }

            if (info == null)
            {
                return false;
            }

            FCName = OriginFcName(FCName);
            try
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'", "", true);
                if (tab == null)
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                if (tab.Rows.Count == 0)
                {
                    _errMsg = "Featureclass not found...";
                    return false;
                }
                tab.Rows[0]["SI"] = info.type;
                tab.Rows[0]["SIRATIO"] = info.SpatialRatio;
                tab.Rows[0]["MaxPerNode"] = info.MaxFeaturesPerNode;
                tab.Rows[0]["SIMinX"] = info.Bounds.minx;
                tab.Rows[0]["SIMinY"] = info.Bounds.miny;
                tab.Rows[0]["SIMaxX"] = info.Bounds.maxx;
                tab.Rows[0]["SIMaxY"] = info.Bounds.maxy;
                if (!_conn.Update(tab))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }
        #endregion

        #region BinaryTree2
        async virtual public Task<bool> ShrinkSpatialIndex(string fcName)
        {
            if (_conn == null)
            {
                return false;
            }

            // Nur jetzt zum test Tabelle löschen und neu erzeugen
            // Grund: liegen noch von erstellen her in alter Structur vor!!
            //DropTable("FCSI_" + fcName);
            //if (!InitSpatialIndex2(fcName)) return false;

            DataTable distinct = await _conn.Select("DISTINCT " + DbColName("FDB_NID"), FcTableName(fcName));
            if (distinct == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            List<long> nodeNumbers = new List<long>();
            foreach (DataRow row in distinct.Rows)
            {
                nodeNumbers.Add(Convert.ToInt64(row[0]));
            }

            return await ShrinkSpatialIndex(fcName, nodeNumbers);
        }

        async virtual public Task<bool> ShrinkSpatialIndex(string fcName, List<long> NIDs)
        {
            if (NIDs == null || _conn == null)
            {
                return false;
            }

            NIDs.Sort();

            DataTable si = await _conn.Select("*", FcsiTableName(fcName), "", "", true);
            if (si == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            //si.Rows.Clear();
            foreach (DataRow row in si.Rows)
            {
                row.Delete();
            }

            foreach (long nid in NIDs)
            {
                DataRow row = si.NewRow();
                row["NID"] = nid;
                si.Rows.Add(row);
            }
            if (!_conn.Update(si))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            await IncSpatialIndexVersion(fcName);
            return true;
        }

        async public Task<BinaryTreeDef> BinaryTreeDef(string fcName)
        {
            if (_conn == null)
            {
                return null;
            }

            try
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + fcName + "'", "");
                if (tab == null)
                {
                    _errMsg = _conn.errorMessage;
                    return null;
                }
                if (tab.Rows.Count == 0)
                {
                    _errMsg = "FeatureClass not found...";
                    return null;
                }
                Envelope bounds = new Envelope((double)tab.Rows[0]["SIMinX"], (double)tab.Rows[0]["SIMinY"], (double)tab.Rows[0]["SIMaxX"], (double)tab.Rows[0]["SIMaxY"]);
                int maxLevels = Convert.ToInt32(tab.Rows[0]["MaxLevels"]);
                int maxPerNode = Convert.ToInt32(tab.Rows[0]["MaxPerNode"]);
                double splitRatio = (double)tab.Rows[0]["SIRATIO"];

                string dsName = await this.DatasetNameByFeatureClass(fcName);
                ISpatialReference sRef = await this.SpatialReference(dsName);
                BinaryTreeDef def = new BinaryTreeDef(bounds, maxLevels, maxPerNode, splitRatio);
                def.SpatialReference = sRef;
                return def;
            }
            catch (Exception ex)
            {
                _errMsg = "Exception: " + ex.Message + "\n" + ex.StackTrace; ;
                return null;
            }
        }

        #region Rebuild Index
        async virtual public Task<bool> RebuildSpatialIndexDef(string fcName, BinaryTreeDef def, EventHandler callback)
        {
            if (_conn == null)
            {
                return false;
            }

            try
            {
                int fcID = await GetFeatureClassID(fcName);
                if (fcID < 0)
                {
                    _errMsg = "Can't find featureclass '" + fcName + "'!";
                    return false;
                }
                DataTable featureclasses = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("ID") + "=" + fcID, "", true);
                if (featureclasses == null)
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
                if (featureclasses.Rows.Count != 1)
                {
                    _errMsg = "Featureclass '" + fcName + "' don't exists!";
                    return false;
                }
                featureclasses.Rows[0]["SIMinX"] = def.Bounds.minx;
                featureclasses.Rows[0]["SIMinY"] = def.Bounds.miny;
                featureclasses.Rows[0]["SIMaxX"] = def.Bounds.maxx;
                featureclasses.Rows[0]["SIMaxY"] = def.Bounds.maxy;
                featureclasses.Rows[0]["MaxLevels"] = def.MaxLevel;
                if (!_conn.Update(featureclasses))
                {
                    _errMsg = "Can't update Spatial Index Definition!\n" + _conn.errorMessage;
                    return false;
                }
                await IncSpatialIndexVersion(fcName);
                if (callback != null)
                {
                    callback(this, new UpdateSIDefEventArgs());
                }

                if (!await RecalcFeatureSpatialIndex(fcName, callback))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Exception: " + ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }
        async virtual protected Task<bool> RecalcFeatureSpatialIndex(string fcName, EventHandler callback)
        {
            try
            {
                BinarySearchTree2 tree = null;
                await CheckSpatialSearchTreeVersion(fcName);
                if (_spatialSearchTrees[fcName] == null)
                {
                    _spatialSearchTrees[fcName] = await this.SpatialSearchTree(fcName);
                }
                tree = _spatialSearchTrees[fcName] as BinarySearchTree2;

                if (tree == null)
                {
                    _errMsg = "Fatal Error: Can't determine spatial index!!!";
                    return false;
                }

                IFeatureClass fc = await this.GetFeatureclass(fcName);
                if (fc == null)
                {
                    _errMsg = "Fatal Error: Can't determine featureclass!!!";
                    return false;
                }

                int countFeatures = await fc.CountFeatures();
                UpdateSICalculateNodes args1 = new UpdateSICalculateNodes(0, countFeatures);
                UpdateSIUpdateNodes args2 = new UpdateSIUpdateNodes(0, countFeatures);

                QueryFilter filter = new QueryFilter();
                filter.AddField(fc.IDFieldName);
                filter.AddField(fc.ShapeFieldName);

                Dictionary<int, long> nids = new Dictionary<int, long>();
                using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                {
                    if (cursor == null)
                    {
                        _errMsg = "Fatal Error: Can't determine featurecursor!!!";
                        return false;
                    }

                    IFeature feature;
                    while ((feature = await cursor.NextFeature()) != null)
                    {
                        if (feature.Shape != null)
                        {
                            long nid = tree.InsertSINodeFast(feature.Shape.Envelope);
                            nids.Add(feature.OID, nid);
                        }

                        if (callback != null)
                        {
                            args1.Pos++;
                            callback(this, args1);
                        }
                    }
                }
                foreach (int oid in nids.Keys)
                {
                    if (!UpdateFeatureSpatialNodeID(fc, oid, nids[oid]))
                    {
                        return false;
                    }

                    if (callback != null)
                    {
                        args2.Pos++;
                        callback(this, args2);
                    }
                }
                return await ShrinkSpatialIndex(fcName);
            }
            catch (Exception ex)
            {
                _errMsg = "Exception: " + ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }
        abstract protected bool UpdateFeatureSpatialNodeID(IFeatureClass fc, int oid, long nid);

        #endregion

        #region Repair
        async virtual public Task<bool> RepairSpatialIndex(string fcName, EventHandler callback)
        {
            try
            {
                await CheckSpatialSearchTreeVersion(fcName);
                BinarySearchTree2 tree = await this.SpatialSearchTree(fcName) as BinarySearchTree2;
                if (tree == null)
                {
                    _errMsg = "Can't get determine tree for featureclass '" + fcName + "'!";
                    return false;
                }

                IFeatureClass fc = await this.GetFeatureclass(fcName);
                if (fc == null)
                {
                    _errMsg = "Fatal Error: Can't determine featureclass!!!";
                    return false;
                }

                int countFeatures = await fc.CountFeatures();
                RepairSICheckNodes args1 = new RepairSICheckNodes(0, countFeatures);
                RepairSIUpdateNodes args2 = new RepairSIUpdateNodes(0, 0);

                QueryFilter filter = new QueryFilter();
                filter.AddField(fc.IDFieldName);
                filter.AddField(fc.ShapeFieldName);
                filter.AddField("FDB_NID");

                Dictionary<int, long> nids = new Dictionary<int, long>();
                using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                {
                    if (cursor == null)
                    {
                        _errMsg = "Fatal Error: Can't determine featurecursor!!!";
                        return false;
                    }

                    IFeature feature;
                    while ((feature = await cursor.NextFeature()) != null)
                    {
                        long NID = Convert.ToInt64(feature["FDB_NID"]);
                        if (feature.Shape != null)
                        {
                            long nid = tree.UpdadeSINodeFast(feature.Shape.Envelope, NID);
                            if (nid != NID)
                            {
                                nids.Add(feature.OID, nid);
                                args1.WrongNIDs++;
                            }
                        }

                        if (callback != null)
                        {
                            args1.Pos++;
                            callback(this, args1);
                        }
                    }
                }

                if (nids.Count != 0)
                {
                    args2.Count = nids.Count;
                    foreach (int oid in nids.Keys)
                    {
                        if (!UpdateFeatureSpatialNodeID(fc, oid, nids[oid]))
                        {
                            return false;
                        }

                        if (callback != null)
                        {
                            args2.Pos++;
                            callback(this, args2);
                        }
                    }
                    return await ShrinkSpatialIndex(fcName);
                }
                return true;
            }
            catch (Exception ex)
            {
                _errMsg = "Exception: " + ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }
        #endregion

        #region Statistics

        #endregion
        #endregion

        #region ImageDataset

        async public Task<int> CreateImageDataset(string name, ISpatialReference sRef, ISpatialIndexDef sIndexDef, string imageSpace, IFieldCollection additionalFields)
        {
            int dsID = await this.CreateDataset(name, sRef, sIndexDef, true, imageSpace);
            if (dsID == -1)
            {
                return -1;
            }

            FieldCollection fields = new FieldCollection();

            fields.Add(new Field(ColumnName("PATH"), FieldType.String, 255));
            fields.Add(new Field(ColumnName("LAST_MODIFIED"), FieldType.Date));
            fields.Add(new Field(ColumnName("PATH2"), FieldType.String, 255));
            fields.Add(new Field(ColumnName("LAST_MODIFIED2"), FieldType.Date));
            fields.Add(new Field(ColumnName("MANAGED"), FieldType.boolean));
            fields.Add(new Field(ColumnName("RF_PROVIDER"), FieldType.String, 40));
            fields.Add(new Field(ColumnName("MANAGED_FILE"), FieldType.String, 255));
            fields.Add(new Field(ColumnName("FORMAT"), FieldType.String, 12));
            fields.Add(new Field(ColumnName("CELLX"), FieldType.Double));
            fields.Add(new Field(ColumnName("CELLY"), FieldType.Double));
            fields.Add(new Field(ColumnName("LEVELS"), FieldType.integer));
            
            if (additionalFields != null)
            {
                foreach (IField field in additionalFields.ToEnumerable())
                {
                    fields.Add(field);
                }
            }

            int fcID = await this.CreateFeatureClass(name, name + "_IMAGE_POLYGONS", new GeometryDef(GeometryType.Polygon), fields);
            if (fcID == -1)
            {
                // Delete Dataset;
                return -1;
            }
            if (sIndexDef is gViewSpatialIndexDef)
            {
                if (!await SetSpatialIndexBounds(name + "_IMAGE_POLYGONS", "BinaryTree2", sIndexDef.SpatialIndexBounds, sIndexDef.SplitRatio, sIndexDef.MaxPerNode, sIndexDef.Levels))
                {
                    await DeleteFeatureClass(name + "_IMAGE_POLYGONS");
                    return -1;
                }
            }
            else if (sIndexDef is MSSpatialIndex &&
                    _conn.dbType == DBType.sql)
            {
                if (!SetMSSpatialIndex((MSSpatialIndex)sIndexDef, name + "_IMAGE_POLYGONS"))
                {
                    await DeleteFeatureClass(name + "_IMAGE_POLYGONS");
                    return -1;
                }
            }
            if (_conn.dbType == DBType.oledb)
            {
                if (!_conn.createIndex("FC_" + name + "_PATH", "FC_" + name + "_IMAGE_POLYGONS", "PATH", true))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
            }
            else if (_conn.dbType == DBType.sql)
            {
                if (!_conn.createIndex("FC_" + System.Guid.NewGuid().ToString("N") + "_PATH", "FC_" + name + "_IMAGE_POLYGONS", "PATH", true, false))
                {
                    _errMsg = _conn.errorMessage;
                    return -1;
                }
            }

            fields.Clear();
            fields.Add(new Field(ColumnName("ID"), FieldType.ID));
            fields.Add(new Field(ColumnName("IMAGE_ID"), FieldType.integer));
            fields.Add(new Field(ColumnName("SHAPE"), FieldType.binary));
            fields.Add(new Field(ColumnName((_conn.dbType == DBType.sql ? "IMAGE" : "IMG")), FieldType.binary));
            fields.Add(new Field(ColumnName("LEV"), FieldType.integer));
            fields.Add(new Field(ColumnName("X"), FieldType.Double));
            fields.Add(new Field(ColumnName("Y"), FieldType.Double));
            fields.Add(new Field(ColumnName("dx1"), FieldType.Double));
            fields.Add(new Field(ColumnName("dx2"), FieldType.Double));
            fields.Add(new Field(ColumnName("dy1"), FieldType.Double));
            fields.Add(new Field(ColumnName("dy2"), FieldType.Double));
            fields.Add(new Field(ColumnName("cellX"), FieldType.Double));
            fields.Add(new Field(ColumnName("cellY"), FieldType.Double));
            fields.Add(new Field(ColumnName("iWidth"), FieldType.integer));
            fields.Add(new Field(ColumnName("iHeight"), FieldType.integer));

            if (!this.CreateTable(name + "_IMAGE_DATA", fields, false))
            {
                // Delete Dataset;
                await DeleteFeatureClass(name + "_IMAGE_POLYGONS");
                return -1;
            }

            if (!_conn.createIndex(name + "_" + Guid.NewGuid().ToString("N") + "_IMAGE_DATA", name + "_IMAGE_DATA", "IMAGE_ID", false, false))
            {
                _errMsg = _conn.errorMessage;
                // Delete Dataset
                _conn.dropTable(name + "_IMAGE_DATA");
                await DeleteFeatureClass(name + "_IMAGE_POLYGONS");
                return -1;
            }


            return dsID;
        }

        async public Task<(bool isImageDataset, string imageSpace)> IsImageDataset(string dsname)
        {
            string imageSpace = String.Empty;
            if (_conn == null)
            {
                return (false, imageSpace);
            }

            object field = await _conn.QuerySingleField("SELECT " + DbColName("ImageDataset") + " FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("Name") + "='" + dsname + "'", ColumnName("ImageDataset"));
            if (field is bool)
            {
                imageSpace = (string)await _conn.QuerySingleField("SELECT " + DbColName("ImageSpace") + " FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("Name") + "='" + dsname + "'", ColumnName("ImageSpace"));
                if (imageSpace == null)
                {
                    imageSpace = String.Empty;
                }

                return ((bool)field, imageSpace);
            }
            imageSpace = String.Empty;
            return (false, imageSpace);
        }
        #endregion

        #region Linked Featureclasses

        protected bool IsLinkedFeatureClass(DataRow row)
        {
            if (row["SI"] is string && row["SI"].ToString().ToLower() == "linked")
            {
                return true;
            }

            return false;
        }

        protected int LinkedDatasetId(DataRow row)
        {
            if (!IsLinkedFeatureClass(row))
            {
                return 0;
            }

            return Convert.ToInt32(row["SIVersion"]);
        }

        protected LinkedDatasetCache LinkedDatasetCacheInstance = new LinkedDatasetCache(true);
        protected class LinkedDatasetCache
        {
            private Dictionary<string, IDataset> _cache = new Dictionary<string, IDataset>();
            private bool _disposeDatasets = false;

            public LinkedDatasetCache(bool disposeDatasets = false)
            {
                _disposeDatasets = disposeDatasets;
            }

            async public Task<IDataset> GetDataset(Guid pluginId, string connectionString, bool open)
            {
                string key = pluginId.ToString() + ":" + connectionString;

                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                IDataset ds = PlugInManager.Create(pluginId) as IDataset;
                if (ds == null)
                {
                    return null;
                }

                await ds.SetConnectionString(connectionString);
                if (open)
                {
                    await ds.Open();
                }

                _cache.Add(key, ds);
                return ds;
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (_disposeDatasets)
                {
                    foreach (IDataset ds in _cache.Values)
                    {
                        if (ds != null)
                        {
                            ds.Dispose();
                        }
                    }
                }
                _cache.Clear();
            }

            #endregion
        }

        async protected Task<IDataset> LinkedDataset(LinkedDatasetCache cache, int linkedDsId)
        {
            try
            {
                DataTable tab = await _conn.Select("*", TableName("FDB_LinkedConnections"), DbColName("ID") + "=" + linkedDsId);
                if (tab != null && tab.Rows.Count == 1)
                {
                    Guid pluginId = new Guid(tab.Rows[0]["Plugin"].ToString());
                    string connectionString = Crypto.Decrypt(tab.Rows[0]["Connection"].ToString(), "gView.Linked.Dataset");

                    return await cache.GetDataset(pluginId, connectionString, true);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region MS Spatial Index
        virtual public bool SetMSSpatialIndex(MSSpatialIndex index, string fcName)
        {
            return false;
        }
        #endregion

        async public Task RefreshClasses(IDataset dataset)
        {
            if (dataset == null)
            {
                return;
            }

            List<IDatasetElement> elements = await DatasetLayers(dataset);
            if (elements == null)
            {
                return;
            }

            foreach (IDatasetElement element in elements)
            {
                if (element == null || element.Class == null)
                {
                    continue;
                }

                foreach (IDatasetElement orig in await dataset.Elements())
                {
                    if (orig == null || orig.Class == null)
                    {
                        continue;
                    }

                    try
                    {
                        _spatialSearchTrees.Remove(orig.Class.Name);
                    }
                    catch { }
                    if (orig.Class.Name == element.Class.Name)
                    {
                        if (orig.Class is IRefreshable)
                        {
                            ((IRefreshable)orig.Class).RefreshFrom(element.Class);
                        }
                    }
                }
            }
        }
        async public Task<int> CreateSpatialReference(ISpatialReference sRef)
        {
            if (_conn == null)
            {
                return -1;
            }

            if (sRef == null)
            {
                return -1;
            }

            string name = sRef.Name;
            string desc = sRef.Description;
            string parm = "";
            foreach (string p in sRef.Parameters)
            {
                if (parm != "")
                {
                    parm += " ";
                }

                parm += p;
            }
            string datumName = (sRef.Datum != null) ? sRef.Datum.Name : "";
            string datumParm = (sRef.Datum != null) ? sRef.Datum.Parameter : "";

            string where = DbColName("Name") + "='" + name + "' AND " + DbColName("Description") + "='" + desc + "' AND " + DbColName("Params") + " LIKE '" + parm + "' AND " + DbColName("DatumName") + "='" + datumName + "' AND " + DbColName("DatumParam") + "='" + datumParm + "'";
            DataTable tab = await _conn.Select("*", TableName("FDB_SpatialReference"), where, "", true);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }
            if (tab.Rows.Count > 0)
            {
                _conn.ReleaseUpdateAdapter();
                return Convert.ToInt32(tab.Rows[0]["ID"]);
            }
            DataRow nRow = tab.NewRow();
            nRow["Name"] = name;
            nRow["Description"] = desc;
            nRow["Params"] = parm;
            nRow["datumName"] = datumName;
            nRow["datumParam"] = datumParm;
            tab.Rows.Add(nRow);
            if (!_conn.Update(tab))
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }

            tab = await _conn.Select("*", TableName("FDB_SpatialReference"), where, "");
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return -1;
            }
            if (tab.Rows.Count > 0)
            {
                return Convert.ToInt32(tab.Rows[0]["ID"]);
            }

            _errMsg = "Unknown...";
            return -1;
        }
        async public Task<bool> SetSpatialReferenceID(string dsname, int SpatialReferenceID)
        {
            if (_conn == null)
            {
                return false;
            }

            DataTable tab = await _conn.Select("*", TableName("FDB_Datasets"), DbColName("Name") + "='" + dsname + "'", "", true);
            if (tab == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (tab.Rows.Count == 0)
            {
                _errMsg = "Dataset not found...";
                return false;
            }
            tab.Rows[0]["SpatialReferenceID"] = SpatialReferenceID;
            if (!_conn.Update(tab))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }
        protected bool Report(object report)
        {
            if (reportProgress == null)
            {
                return false;
            }

            reportProgress(report);
            return true;
        }

        async virtual public Task<bool> CalculateExtent(string fcName)
        {
            IFeatureClass fc = await this.GetFeatureclass(fcName);
            if (fc == null)
            {
                _errMsg = "Can't find featureclass '" + fcName + "'...";
                return false;
            }

            return await CalculateExtent(fc);
        }
        async virtual public Task<bool> CalculateExtent(IFeatureClass fc)
        {
            if (_conn == null || fc == null)
            {
                _errMsg = "No Connection...";
                return false;
            }

            ProgressReport report = new ProgressReport();
            report.Message = "Calculate Extent...";
            report.featureMax = await CountFeatures(fc.Name);

            QueryFilter filter = new QueryFilter();
            filter.AddField(ColumnName("FDB_SHAPE"));

            IFeatureCursor cursor = await Query(fc, filter);
            IFeature feat;
            Envelope envelope = null;

            int counter = 0;
            while ((feat = await cursor.NextFeature()) != null)
            {
                if (feat.Shape == null || feat.Shape.Envelope == null)
                {
                    continue;
                }

                if (envelope == null)
                {
                    envelope = new Envelope(feat.Shape.Envelope);
                }
                else
                {
                    envelope.Union(feat.Shape.Envelope);
                }

                if (counter > 99)
                {
                    counter = 0;
                    Report(report);
                }
                counter++;
                report.featurePos++;
            }
            cursor.Dispose();
            if (envelope == null)
            {
                // Keine Features
                return true;
            }

            return await SetFeatureclassExtent(fc.Name, envelope);
        }
        async virtual public Task<bool> SetFeatureclassExtent(string fcName, IEnvelope envelope)
        {
            if (_conn == null)
            {
                _errMsg = "No Connection...";
                return false;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcName + "'", "FCS", true))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (ds.Tables[0].Rows.Count == 0)
            {
                _errMsg = "Can't find Featureclass...";
                return false;
            }

            DataRow fcRow = ds.Tables[0].Rows[0];
            fcRow["MinX"] = envelope.minx;
            fcRow["MinY"] = envelope.miny;
            fcRow["MaxX"] = envelope.maxx;
            fcRow["MaxY"] = envelope.maxy;

            if (!_conn.UpdateData(ref ds, "FCS"))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            return true;
        }

        async public Task<IEnvelope> QueryExtent(string FCName)
        {
            if (_conn == null)
            {
                _errMsg = "No Connection...";
                return null;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + FCName + "'", "FCS"))
            {
                _errMsg = _conn.errorMessage;
                return null;
            }

            if (ds.Tables[0].Rows.Count == 0)
            {
                _errMsg = "Can't find Featureclass...";
                return null;
            }

            try
            {
                return new Envelope(
                    (double)ds.Tables[0].Rows[0]["MinX"],
                    (double)ds.Tables[0].Rows[0]["MinY"],
                    (double)ds.Tables[0].Rows[0]["MaxX"],
                    (double)ds.Tables[0].Rows[0]["MaxY"]);
            }
            catch
            {
                return new Envelope(0, 0, 0, 0);
            }

        }

        virtual protected Task<bool> UpdateSpatialIndexID(string FCName, List<SpatialIndexNode> nodes)
        {
            throw new NotImplementedException();
        }

        async public Task<bool> CalculateSpatialIndex(IFeatureClass fc, int maxPerNode, int maxLevels)
        {
            if (_conn == null)
            {
                _errMsg = "No Connection...";
                return false;
            }

            try
            {
                if (!await CalculateExtent(fc))
                {
                    return false;
                }

                ProgressReport report = new ProgressReport();
                report.featureMax = await CountFeatures(fc.Name);
                report.featurePos = 0;

                IEnvelope Bounds = await this.QueryExtent(fc.Name);
                if (Bounds == null)
                {
                    return false;
                }

                if (reportProgress != null)
                {
                    report.Message = "Calculate Index...";
                    reportProgress(report);
                }

                if (!_conn.ExecuteNoneQuery("DELETE FROM " + FcsiTableName(fc)))
                {
                    return false;
                }

                DualTree tree = new DualTree(maxPerNode);
                tree.CreateTree(Bounds);

                QueryFilter filter = new QueryFilter();
                filter.AddField(ColumnName("FDB_OID"));
                filter.AddField(ColumnName("FDB_SHAPE"));

                IFeatureCursor cursor = await Query(fc, filter);
                IFeature feat;

                while ((feat = await cursor.NextFeature()) != null)
                {
                    if (feat.Shape == null)
                    {
                        continue;
                    }

                    SHPObject shpObj = new SHPObject(feat.OID, feat.Shape.Envelope);
                    tree.AddShape(shpObj);

                    if (reportProgress != null)
                    {
                        report.featurePos++;
                        if ((report.featurePos % 1000) == 0)
                        {
                            reportProgress(report);
                        }
                    }
                }
                tree.FinishIt();
                cursor.Dispose();

                List<SpatialIndexNode> nodes = tree.Nodes;

                if (nodes.Count == 0)
                {
                    return true;
                }

                if (!await this.SetSpatialIndexBounds(fc.Name, "BinaryTree", Bounds, 0.55, maxPerNode, maxLevels))
                {
                    return false;
                }
                if (!await InsertSpatialIndexNodes2(fc.Name, nodes))
                {
                    return false;
                }

                return await UpdateSpatialIndexID(fc.Name, nodes);
            }
            catch (Exception ex)
            {
                _errMsg = ex.Message + "\n" + ex.StackTrace; ;
                return false;
            }
        }

        virtual public Task<bool> ReorderRecords(IFeatureClass fClass)
        {
            throw new NotImplementedException();
        }

        async public Task<IGeometryDef> GetGeometryDef(string FCName)
        {
            if (FCName.Contains("@"))
            {
                string[] viewNames = SpatialViewNames(FCName);
                FCName = viewNames[0];
            }
            if (_conn == null)
            {
                return new GeometryDef(GeometryType.Unknown, null, false);
            };
            string sql = "SELECT * FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + FCName + "'";

            DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClasses"), DbColName("Name") + "='" + FCName + "'");
            if (tab == null || tab.Rows.Count == 0)
            {
                return new GeometryDef(GeometryType.Unknown, null, false);
            }

            DataRow row = tab.Rows[0];

            GeometryDef geomDef;
            if (row.Table.Columns["HasZ"] != null)
            {
                geomDef = new GeometryDef((GeometryType)row["GeometryType"], null, (bool)row["HasZ"]);
            }
            else
            {  // alte Version war immer 3D
                geomDef = new GeometryDef((GeometryType)row["GeometryType"], null, true);
            }
            string dsName = await this.DatasetName(Convert.ToInt32(row["DatasetID"]));
            geomDef.SpatialReference = await this.SpatialReference(dsName);
            return geomDef;
        }

        async public Task<List<IDataset>> Datasets()
        {
            _errMsg = "";
            if (_conn == null)
            {
                return null;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + DbColName("FDB_Datasets"), "DS"))
            {
                _errMsg = _conn.errorMessage;
                return null;
            }

            List<IDataset> datasets = new List<IDataset>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                AccessFDBDataset dataset = new AccessFDBDataset(this);
                await dataset.SetConnectionString(_filename + ";" + row["Name"].ToString());
                if (await dataset.Open())
                {
                    datasets.Add(dataset);
                }
            }

            ds.Dispose();
            return datasets;
        }

        abstract public Task<List<IDatasetElement>> DatasetLayers(IDataset dataset);

        abstract public Task<IDatasetElement> DatasetElement(IDataset dataset, string elementName);

        async virtual public Task<IFeatureClass> GetFeatureclass(string fcName)
        {
            if (_conn == null)
            {
                return null;
            }

            int fcID = await this.GetFeatureClassID(fcName);

            DataTable featureclasses = await _conn.Select(DbColName("DatasetID"), TableName("FDB_FeatureClasses"), DbColName("ID") + "=" + fcID);
            if (featureclasses == null || featureclasses.Rows.Count != 1)
            {
                return null;
            }

            DataTable datasets = await _conn.Select(DbColName("Name"), TableName("FDB_Datasets"), DbColName("ID") + "=" + featureclasses.Rows[0]["DatasetID"].ToString());
            if (datasets == null || datasets.Rows.Count != 1)
            {
                return null;
            }

            return await GetFeatureclass(datasets.Rows[0]["Name"].ToString(), fcName);
        }
        abstract public Task<IFeatureClass> GetFeatureclass(string dsName, string fcName);
        async virtual public Task<IFeatureClass> GetFeatureclass(int fcId)
        {
            if (_conn == null)
            {
                return null;
            }

            DataTable featureclasses = await _conn.Select(DbColName("DatasetID") + "," + DbColName("Name"), TableName("FDB_FeatureClasses"), DbColName("ID") + "=" + fcId);
            if (featureclasses == null || featureclasses.Rows.Count != 1)
            {
                return null;
            }

            DataTable datasets = await _conn.Select(DbColName("Name"), TableName("FDB_Datasets"), DbColName("ID") + "=" + featureclasses.Rows[0]["DatasetID"].ToString());
            if (datasets == null || datasets.Rows.Count != 1)
            {
                return null;
            }

            return await GetFeatureclass(datasets.Rows[0]["Name"].ToString(), (string)featureclasses.Rows[0]["Name"]);
        }

        async protected Task<int> DatasetIDFromFeatureClassName(string fcName)
        {
            if (_conn == null)
            {
                return -1;
            }

            object obj = await _conn.QuerySingleField("SELECT " + DbColName("DatasetID") + " FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcName + "'", ColumnName("DatasetID"));
            if (obj == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(obj);
            }
            catch
            {
                return -1;
            }
        }
        async protected Task<string> DatasetNameFromFeatureClassName(string fcName)
        {
            if (_conn == null)
            {
                return "";
            }

            int dsID = await DatasetIDFromFeatureClassName(fcName);
            if (dsID == -1)
            {
                return "";
            }

            object obj = await _conn.QuerySingleField("SELECT " + DbColName("Name") + " FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("ID") + "=" + dsID, ColumnName("Name"));
            if (obj == null)
            {
                return "";
            }

            try
            {
                return (string)obj;
            }
            catch
            {
                return "";
            }
        }
        async virtual public Task<IEnumerable<IField>> FeatureClassFields(int dsID, string fcname)
        {
            _errMsg = "";
            if (dsID == -1)
            {
                return null;
            }

            int fcID = await this.FeatureClassID(dsID, fcname);
            if (fcID == -1)
            {
                return null;
            }

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_FeatureClassFields") + " WHERE " + DbColName("FClassID") + "=" + fcID, "FIELDS"))
            {
                _errMsg = _conn.errorMessage;
                return null;
            }

            DataTable schema = _conn.GetSchema2(FcTableName(fcname));

            FieldCollection fields = new FieldCollection();
            gView.Framework.Data.Field field = new Field();
            field.name = ColumnName("FDB_OID");
            field.aliasname = "OID";
            field.type = FieldType.ID;
            fields.Add(field);
            field = new Field();
            field.name = ColumnName("FDB_SHAPE");
            field.aliasname = "SHAPE";
            if (_conn.dbType == DBType.sql)
            {
                string indexType = await this.SpatialIndexType(fcname);
                if (indexType != null && indexType.ToUpper() == "GEOMETRY")
                {
                    field.type = FieldType.GEOMETRY;
                }
                else if (indexType != null && indexType.ToUpper() == "GEOGRAPHY")
                {
                    field.type = FieldType.GEOGRAPHY;
                }
                else
                {
                    field.type = FieldType.Shape;
                }
            }
            else
            {
                field.type = FieldType.Shape;
            }
            fields.Add(field);

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                switch ((FieldType)Convert.ToInt32(row["FieldType"]))
                {
                    case FieldType.Shape:
                    case FieldType.ID:
                        continue;
                }

                if (ds.Tables[0].Columns["AutoFieldGUID"] != null && row["AutoFieldGUID"] != DBNull.Value && row["AutoFieldGUID"]?.ToString() != String.Empty)
                {
                    try
                    {
                        Guid guid = new Guid(row["AutoFieldGUID"].ToString());
                        PlugInManager compMan = new PlugInManager();
                        field = compMan.CreateInstance(guid) as Field;
                    }
                    catch
                    {
                        field = null;
                    }
                    if (field == null)
                    {
                        field = new Field();
                    }
                }
                else
                {
                    field = new Field();
                }
                field.name = row["FieldName"].ToString();
                field.aliasname = row["Aliasname"].ToString();
                field.type = (FieldType)Convert.ToInt32(row["FieldType"]);
                field.IsRequired = Convert.ToInt32(row["IsRequired"]) != 0;
                field.IsEditable = Convert.ToInt32(row["IsEditable"]) != 0;
                field.DefautValue = row["DefaultValue"];

                if (schema != null)
                {
                    try
                    {
                        DataRow[] srow = schema.Select("ColumnName='" + field.name + "'");
                        if (srow.Length == 0)
                        {
                            continue;
                        }

                        switch (field.type)
                        {
                            case FieldType.biginteger:
                            case FieldType.integer:
                            case FieldType.smallinteger:
                            case FieldType.ID:
                                field.size = Convert.ToInt32(srow[0]["NumericPrecision"]);
                                break;
                            case FieldType.Double:
                                field.size = Convert.ToInt32(srow[0]["NumericPrecision"]);
                                field.precision = 5;//Convert.ToInt32(srow[0]["NumericPrecision"]);
                                break;
                            case FieldType.Float:
                                field.size = Convert.ToInt32(srow[0]["NumericPrecision"]);
                                field.precision = 3;//Convert.ToInt32(srow[0]["NumericPrecision"]);
                                break;
                            default:
                                field.size = Convert.ToInt32(srow[0]["ColumnSize"]);
                                break;
                        }
                    }
                    catch { }
                }

                fields.Add(field);
            }

            if (fcname.Contains("@"))
            {
                string[] viewNames = SpatialViewNames(fcname);
                IFieldCollection addFields = TableFields(viewNames[1]);

                if (addFields != null)
                {
                    foreach (IField f in addFields.ToEnumerable())
                    {
                        if (fields.FindField(f.name) != null)
                        {
                            continue;
                        }

                        fields.Add(f);
                    }
                }
            }
            return fields.ToEnumerable();
        }
        async virtual public Task<IEnumerable<IField>> FeatureClassFields(string dsname, string fcname)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return null;
            }

            int dsID = await this.DatasetID(dsname);
            return await FeatureClassFields(dsID, fcname);
        }
        async protected Task<IEnvelope> FeatureClassExtent(string FCname)
        {
            _errMsg = "";
            if (_conn == null)
            {
                return null;
            }

            FCname = OriginFcName(FCname);

            return await this.QueryExtent(FCname);
        }

        protected int round(double x)
        {
            if (x < 0)
            {
                return Convert.ToInt32(Math.Round(x - 1.0, 0));
            }

            return Convert.ToInt32(Math.Round(x + 1.0, 0));
        }

        async virtual public Task<DataTable> EmptyDatatable(string fcname, IQueryFilter filter)
        {
            if (_conn == null)
            {
                return null;
            }

            string subfields = "";
            if (filter != null)
            {
                filter.fieldPrefix = "[";
                filter.fieldPostfix = "]";
                subfields = filter.SubFields.Replace(" ", ",");
            }
            if (subfields == "")
            {
                subfields = "*";
            }

            string sql = "SELECT " + subfields + " FROM " + FcTableName(fcname) + " WHERE " + DbColName("FDB_OID") + "=-1";

            DataSet ds = new DataSet();
            if (!await _conn.SQLQuery(ds, sql, fcname))
            {
                return null;
            }
            ds.Tables[0].Rows.Clear();
            return ds.Tables[0];
        }
        virtual public string ConnectionString
        {
            get
            {
                //if(_conn==null) return "";
                //return _conn.connectionString;
                return _filename;
            }
        }

        public string ValidFieldName(string name)
        {
            if (name == "USER")
            {
                return "USER__";
            }

            if (name == "KEY")
            {
                return "KEY__";
            }

            if (name == "TEXT")
            {
                return "TEXT__";
            }

            return name;
        }

        async public Task<ISpatialReference> SpatialReference(string dsname)
        {
            return await SpatialReference(await DatasetID(dsname));
        }
        async public Task<ISpatialReference> SpatialReference(int dsID)
        {
            if (_conn == null)
            {
                return null;
            }

            object sID = await _conn.QuerySingleField("SELECT " + DbColName("SpatialReferenceID") + " FROM " + TableName("FDB_Datasets") + " WHERE " + DbColName("ID") + "=" + dsID, ColumnName("SpatialReferenceID"));
            if (sID == null)
            {
                return null;
            }

            DataSet ds = new DataSet();
            if (await _conn.SQLQuery(ds, "SELECT * FROM " + TableName("FDB_SpatialReference") + " WHERE " + DbColName("ID") + "=" + sID.ToString(), "SREF"))
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    IGeodeticDatum datum = null;
                    if (ds.Tables[0].Rows[0]["DatumParam"] != System.DBNull.Value &&
                        ds.Tables[0].Rows[0]["DatumParam"]?.ToString() != "")
                    {
                        datum = new GeodeticDatum();
                        ((GeodeticDatum)datum).Name = ds.Tables[0].Rows[0]["DatumName"].ToString();
                        datum.Parameter = ds.Tables[0].Rows[0]["DatumParam"].ToString();
                    }
                    return new SpatialReference(
                        ds.Tables[0].Rows[0]["Name"].ToString(),
                        ds.Tables[0].Rows[0]["Description"].ToString(),
                        ds.Tables[0].Rows[0]["Params"].ToString(),
                        datum);
                }
            }
            return null;
        }

        async public Task<bool> ProjectFeatureClass(IFeatureClass fc, ISpatialReference destSRef)
        {
            if (_conn == null || fc == null)
            {
                return false;
            }

            if (fc.SpatialReference == null || destSRef == null)
            {
                return true;
            }

            if (fc.SpatialReference.Equals(destSRef))
            {
                return true;
            }

            List<SpatialIndexNode> nodes = await this.SpatialIndexNodes2(fc.Name, true);
            if (nodes == null)
            {
                _errMsg = "No Spatial Index...";
                return false;
            }
            if (nodes.Count == 0)
            {
                _errMsg = "No Spatial Index...";
                return false;
            }

            IGeometricTransformer transformer = GeometricTransformerFactory.Create();
            //transformer.FromSpatialReference = fc.SpatialReference;
            //transformer.ToSpatialReference = destSRef;
            transformer.SetSpatialReferences(fc.SpatialReference, destSRef);

            foreach (SpatialIndexNode node in nodes)
            {
                DataTable tab = await _conn.Select(DbColName("FDB_OID") + "," + DbColName("FDB_SHAPE"), FcTableName(fc), DbColName("FDB_NID") + "=" + node.NID, "", true);
                if (tab == null)
                {
                    _errMsg = _conn.errorMessage;
                    transformer.Release();
                    return false;
                }

                foreach (DataRow feat in tab.Rows)
                {
                    byte[] obj = (byte[])feat[ColumnName("FDB_SHAPE")];

                    BinaryReader r = new BinaryReader(new MemoryStream());
                    r.BaseStream.Write((byte[])obj, 0, ((byte[])obj).Length);
                    r.BaseStream.Position = 0;

                    IGeometry p = null;
                    switch (fc.GeometryType)
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
                        p.Deserialize(r, fc);
                        r.Close();

                        p = (IGeometry)transformer.Transform2D((object)p);

                        BinaryWriter w = new BinaryWriter(new MemoryStream());
                        p.Serialize(w, fc);

                        byte[] geometry = new byte[w.BaseStream.Length];
                        w.BaseStream.Position = 0;
                        w.BaseStream.Read(geometry, (int)0, (int)w.BaseStream.Length);
                        w.Close();

                        feat[ColumnName("FDB_SHAPE")] = geometry;
                    }
                }

                if (!_conn.Update(tab))
                {
                    _errMsg = _conn.errorMessage;
                    transformer.Release();
                    return false;
                }
            }

            transformer.Release();

            //this.CalculateSpatialIndex(fc.Name,((ISpatialTreeInfo)fc).MaxFeaturesPerNode);

            return true;
        }

        public virtual bool TruncateTable(string table)
        {
            if (_conn == null)
            {
                return false;
            }

            if (!_conn.ExecuteNoneQuery("delete from " + FcTableName(table)))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (!_conn.ExecuteNoneQuery("delete from " + FcsiTableName(table)))
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

        async protected Task SetVersion()
        {
            try
            {
                if (_conn == null)
                {
                    return;
                }

                DataTable tab = await _conn.Select("*", TableName("FDB_ReleaseInfo"));
                if (tab != null && tab.Rows.Count == 1)
                {
                    _version = new Version(Convert.ToInt32(tab.Rows[0]["Major"]), Convert.ToInt32(tab.Rows[0]["Minor"]), Convert.ToInt32(tab.Rows[0]["Bugfix"]));
                }
            }
            catch { }
        }

        async public Task<bool> IsValidAccessFDB()
        {
            return await TableExists("FDB_Datasets");
        }

        /*
        public virtual bool AlterTable(string table, List<IField> fields)
        {
            if (_conn == null) return false;
            if (fields == null) return false;
        
            string dsname = DatasetNameFromFeatureClassName(table);
            if (dsname == "") return false;

            IFeatureClass fc = GetFeatureclass(dsname, table);
            if (fc == null || fc.Fields == null) return false;

            // ALTER COLUMN
            foreach (IField aField in fields)
            {
                IField oField = fc.FindField(aField.name);
                if (oField == null) continue;

                Field f1 = new Field(aField);
                if (!f1.Equals(oField))
                {
                    string sql = "ALTER TABLE FC_" + table +
                        " ALTER COLUMN " + f1.name + " " + FieldDataType(aField);

                    if (!_conn.ExecuteNoneQuery(sql))
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                }
            }

            // ADD COLUMN
            foreach (IField aField in fields)
            {
                IField oField = fc.FindField(aField.name);
                if (oField != null) continue;

                string sql = "ALTER TABLE FC_" + table +
                    " ADD " + aField.name + " AS " + FieldDataType(aField);

                if (!_conn.ExecuteNoneQuery(sql))
                {
                    _errMsg = _conn.errorMessage;
                    return false;
                }
            }

            // DROP COLUMN
            foreach (IField oField in fc.Fields)
            {
                bool found = false;
                foreach (IField aField in fields)
                {
                    if (aField.name == oField.name)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    string sql = "ALTER TABLE FC_" + table +
                        " DROP COLUMN " + oField.name;

                    if (!_conn.ExecuteNoneQuery(sql))
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                }
            }

            return true;
        }
        */

        async protected Task<bool> AlterFeatureclassField(int fcid, IField oldField, IField newField)
        {
            if (oldField == null && newField == null)
            {
                return true;
            }

            if (_conn == null)
            {
                return false;
            }

            if (oldField != null)
            {
                if (oldField.Equals(newField))
                {
                    return true;
                }

                if (newField != null)   // ALTER COLUMN
                {
                    DataTable tab = await _conn.Select("*", TableName("FDB_FeatureClassFields"), "(" + DbColName("FClassID") + "=" + fcid + ") AND (" + DbColName("FieldName") + " LIKE '" + oldField.name + "')", "", true);
                    if (tab == null)
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                    foreach (DataRow row in tab.Rows)
                    {
                        row["FieldName"] = newField.name;
                        row["Aliasname"] = newField.aliasname;
                        row["FieldType"] = (int)newField.type;
                        if (newField.DefautValue == null || newField.DefautValue == DBNull.Value)
                        {
                            row["DefaultValue"] = DBNull.Value;
                        }
                        else
                        {
                            row["DefaultValue"] = newField.DefautValue.ToString();
                        }

                        row["IsRequired"] = newField.IsRequired;
                        row["IsEditable"] = newField.IsEditable;
                    }
                    if (!_conn.Update(tab))
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                    return true;
                }
                else    // DROP COLUMN
                {
                    if (!_conn.ExecuteNoneQuery("DELETE FROM " + TableName("FDB_FeatureClassFields") + " WHERE " + DbColName("FClassID") + "=" + fcid + " AND " + DbColName("FieldName") + " LIKE '" + oldField.name + "'"))
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                    return true;
                }
            }
            // ADD COLUMN
            DataTable tab2 = await _conn.Select("*", TableName("FDB_FeatureClassFields"), DbColName("FClassID") + "=-1", "", true);
            if (tab2 == null)
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            DataRow row2 = tab2.NewRow();
            row2["FClassID"] = fcid;
            row2["FieldName"] = newField.name;
            row2["Aliasname"] = newField.aliasname;
            row2["FieldType"] = (int)newField.type;
            if (newField.DefautValue == null || newField.DefautValue == DBNull.Value)
            {
                row2["DefaultValue"] = DBNull.Value;
            }
            else
            {
                row2["DefaultValue"] = newField.DefautValue.ToString();
            }

            row2["IsRequired"] = newField.IsRequired;
            row2["IsEditable"] = newField.IsEditable;
            tab2.Rows.Add(row2);

            if (!_conn.Update(tab2))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }

            return true;
        }
        async protected virtual Task<bool> RenameField(string table, IField oldField, IField newField)
        {
            if (oldField == null || newField == null || _conn == null)
            {
                return false;
            }

            if (oldField.name == newField.name)
            {
                return true;
            }

            // ADOX vermeiden...
            // macht Versionsprobleme bei vista!
            //ADOX.Catalog adox = null;
            //ADODB.Connection adodbConnection = null;

            //try
            //{
            //    adodbConnection = new ADODB.ConnectionClass();
            //    adodbConnection.Open(_conn.connectionString, "", "", -1);

            //    adox = new ADOX.CatalogClass();
            //    adox.ActiveConnection = adodbConnection;
            //    adox.Tables["FC_" + table].Columns[oldField.name].Name = newField.name;
            //    adox.Tables.Refresh();

            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    _errMsg = ex.Message;
            //    return false;
            //}
            //finally
            //{
            //    if (adox != null)
            //    {
            //        System.Runtime.InteropServices.Marshal.ReleaseComObject(adox);
            //        adox = null;
            //    }
            //    if (adodbConnection != null)
            //    {
            //        adodbConnection.Close();
            //        System.Runtime.InteropServices.Marshal.ReleaseComObject(adodbConnection);
            //        adodbConnection = null;
            //    }
            //}

            if (!await AlterTable(table, null, newField))
            {
                return false;
            }
            if (!_conn.ExecuteNoneQuery("UPDATE " + FcTableName(table) + " SET " + newField.name + "=" + oldField.name))
            {
                _errMsg = _conn.errorMessage;
                return false;
            }
            if (!await AlterTable(table, oldField, null))
            {
                return false;
            }
            return true;
        }

        public delegate void AlterTableEventHandler(string table);
        public event AlterTableEventHandler TableAltered = null;

        async internal Task<DataTable> Select(string fields, string from, string where)
        {
            if (_conn == null)
            {
                return null;
            }

            return await _conn.Select(fields, from, where);
        }

        #region IAltertable
        async public virtual Task<bool> AlterTable(string table, IField oldField, IField newField)
        {
            if (oldField == null && newField == null)
            {
                return true;
            }

            if (oldField != null && newField != null &&
                oldField.name != newField.name &&
                this.GetType().Equals(typeof(gView.DataSources.Fdb.MSAccess.AccessFDB)))
            {
                // Umbenennen erst einmal verhindern. ADOX nicht verwenden...
                // Neues Feld anlegen, daten kopieren, ... erst prüfen!
                _errMsg = "Changing the field name is not possible for MS Access FDB";
                return false;
            }

            if (_conn == null)
            {
                return false;
            }

            int dsID = await DatasetIDFromFeatureClassName(table);
            string dsname = await DatasetNameFromFeatureClassName(table);
            if (dsname == "")
            {
                return false;
            }
            int fcID = await FeatureClassID(dsID, table);
            if (fcID == -1)
            {
                return false;
            }

            IFeatureClass fc = await GetFeatureclass(dsname, table);

            if (fc == null || fc.Fields == null)
            {
                return false;
            }

            try
            {
                if (oldField != null)
                {
                    if (fc.FindField(oldField.name) == null)
                    {
                        _errMsg = "Featureclass " + table + " do not contain field '" + oldField.name + "'";
                        return false;
                    }
                    if (oldField.Equals(newField))
                    {
                        return true;
                    }

                    if (newField != null)   // ALTER COLUMN
                    {
                        string sql = "ALTER TABLE " + FcTableName(table) +
                            " ALTER COLUMN " + DbColName(oldField.name) + " " + FieldDataType(newField);

                        if (!_conn.ExecuteNoneQuery(sql))
                        {
                            _errMsg = _conn.errorMessage;
                            return false;
                        }
                        if (oldField.name != newField.name)
                        {
                            if (!await RenameField(table, oldField, newField))
                            {
                                return false;
                            }
                        }
                    }
                    else    // DROP COLUMN
                    {
                        string replIDFieldname = await Replication.FeatureClassReplicationIDFieldname(fc);
                        if (!String.IsNullOrEmpty(replIDFieldname) &&
                            replIDFieldname == oldField.name)
                        {
                            Replication repl = new Replication();
                            if (!await repl.RemoveReplicationIDField(fc))
                            {
                                _errMsg = "Can't remove replication id field...";
                                return false;
                            }
                        }

                        string sql = "ALTER TABLE " + FcTableName(table) +
                            " DROP COLUMN " + DbColName(oldField.name);

                        if (!_conn.ExecuteNoneQuery(sql))
                        {
                            _errMsg = _conn.errorMessage;
                            return false;
                        }
                    }
                }
                else  // ADD COLUMN
                {
                    string sql = "ALTER TABLE " + FcTableName(table) +
                        " ADD " + DbColName(newField.name) + " " + FieldDataType(newField);

                    if (!_conn.ExecuteNoneQuery(sql))
                    {
                        _errMsg = _conn.errorMessage;
                        return false;
                    }
                }

                return await AlterFeatureclassField(fcID, oldField, newField);
            }
            finally
            {
                FireTableAltered(table);
            }
        }
        #endregion

        protected void FireTableAltered(string table)
        {
            if (TableAltered != null)
            {
                TableAltered(table);
            }
        }

        abstract protected Task<bool> TableExists(string tableName);

        virtual public Task<List<string>> DatabaseTables()
        {
            return Task.FromResult(new List<string>());
        }
        virtual public Task<List<string>> DatabaseViews()
        {
            return Task.FromResult(new List<string>());
        }


        #region ICheckoutableDatabase Member

        async virtual public Task<bool> CreateIfNotExists(string tableName, IFieldCollection fields)
        {
            if (await TableExists(tableName))
            {
                return true;
            }

            return CreateTable(tableName, fields, false);
        }
        async virtual public Task<bool> CreateObjectGuidColumn(string fcName, string fieldname)
        {
            if (!await AlterTable(fcName, null, new Field(fieldname, FieldType.replicationID)))
            {
                return false;
            }

            return true;
        }
        async virtual public Task<int> GetFeatureClassID(string fcName)
        {
            if (_conn == null)
            {
                return -1;
            }

            try
            {
                string sql = "SELECT " + DbColName("ID") + " FROM " + TableName("FDB_FeatureClasses") + " WHERE " + DbColName("Name") + "='" + fcName + "'";
                return Convert.ToInt32(await _conn.QuerySingleField(sql, ColumnName("ID")));
            }
            catch
            {
                return -1;
            }
        }
        async virtual public Task<string> GetFeatureClassName(int fcID)
        {
            if (_conn == null)
            {
                return string.Empty;
            }

            try
            {
                string sql = "SELECT " + DbColName("Name") + " from " + TableName("FDB_FeatureClasses") + " where " + DbColName("ID") + "=" + fcID;
                return (string)await _conn.QuerySingleField(sql, ColumnName("Name"));
            }
            catch
            {
                return string.Empty;
            }
        }

        abstract public bool InsertRow(string table, IRow row, IReplicationTransaction replTrans);
        abstract public bool InsertRows(string table, List<IRow> rows, IReplicationTransaction replTrans);
        abstract public bool UpdateRow(string table, IRow row, string IDField, IReplicationTransaction replTrans);
        abstract public bool DeleteRows(string table, string where, IReplicationTransaction replTrans);

        virtual public string DatabaseConnectionString
        {
            get
            {
                if (_conn != null)
                {
                    return _conn.ConnectionString;
                }

                return null;
            }
        }

        abstract public DbProviderFactory ProviderFactory { get; }

        virtual public string GuidToSql(Guid guid)
        {
            return "{guid {" + guid.ToString() + "}}";
        }
        virtual public void ModifyDbParameter(DbParameter parameter)
        {

        }

        virtual public bool IsFilebaseDatabase
        {
            get { return true; }
        }

        #endregion

        virtual public string TableName(string tableName)
        {
            tableName = tableName.Trim();
            if (!tableName.StartsWith("["))
            {
                tableName = "[" + tableName;
            }

            if (!tableName.EndsWith("]"))
            {
                tableName += "]";
            }

            return tableName;
        }
        virtual public string DbColName(string fieldName)
        {
            return fieldName;
        }
        virtual public string ColumnName(string fieldName)
        {
            return fieldName;
        }
        virtual protected string FcTableName(string fcName)
        {
            if (fcName.Contains("@"))
            {
                return "[" + SpatialViewNames(fcName)[1] + "]";
            }

            return "[FC_" + fcName + "]";
        }
        virtual protected string FcsiTableName(string fcName)
        {
            if (fcName.Contains("@"))
            {
                return "[FCSI_" + SpatialViewNames(fcName)[0] + "]";
            }

            return "[FCSI_" + fcName + "]";
        }
        virtual protected string FcTableName(IFeatureClass fc)
        {
            return FcTableName(fc.Name);
        }
        virtual protected string FcsiTableName(IFeatureClass fc)
        {
            return FcsiTableName(fc.Name);
        }

        virtual public string EscapeQueryValue(string value)
        {
            return value?.Replace(@"\", @"\\");
        }

        #region Network
        async private Task<bool> IsNetworkSubFeatureclass(string fcname)
        {
            if (fcname.ToLower().EndsWith("_nodes") ||
                fcname.ToLower().EndsWith("_complexedges"))
            {
                string nwname = fcname.Substring(0, fcname.LastIndexOf("_"));
                IFeatureClass nw = await GetFeatureclass(nwname);
                if (nw != null && nw.GeometryType == GeometryType.Network)
                {
                    return true;
                }
            }
            return false;
        }

        async public Task<GraphWeights> GraphWeights(string networkName)
        {
            if (_conn == null)
            {
                return null;
            }

            int fcId = await GetFeatureClassID(networkName);
            if (fcId < 0)
            {
                return null;
            }

            DataTable tab = await _conn.Select("*", TableName("FDB_NetworkWeights"), DbColName("NetworkId") + "=" + fcId);
            if (tab == null || tab.Rows.Count == 0)
            {
                return null;
            }

            GraphWeights weights = new GraphWeights();
            foreach (DataRow row in tab.Rows)
            {
                IGraphWeight weight = gView.Framework.Network.Build.NetworkObjectSerializer.DeserializeWeight((byte[])row["Properties"]);
                if (weight != null)
                {
                    weights.Add(weight);
                }
            }
            return weights;
        }

        async public Task<List<IFeatureClass>> NetworkFeatureClasses(string networkName)
        {
            List<IFeatureClass> ret = new List<IFeatureClass>();

            if (_conn == null)
            {
                return ret;
            }

            IFeatureClass networkFc = await GetFeatureclass(networkName);
            if (networkFc == null || networkFc.Dataset == null || !(networkFc is INetworkFeatureClass))
            {
                return ret;
            }

            DataTable tab = await _conn.Select(DbColName("FCID"), TableName("FDB_NetworkClasses"), DbColName("NetworkId") + "=" + FeatureClassID(await DatasetID(networkFc.Dataset.DatasetName), networkName));
            if (tab == null)
            {
                return ret;
            }

            foreach (DataRow row in tab.Rows)
            {
                try
                {
                    string fcname = (await _conn.QuerySingleField("SELECT " + DbColName("Name") + " FROM " + TableName("FDB_FeatureClasses") + " WHERE ID=" + row["FCID"].ToString(), ColumnName("Name")))?.ToString();
                    IFeatureClass fc = await GetFeatureclass(fcname);
                    if (fc != null)
                    {
                        ret.Add(fc);
                    }
                }
                catch { }
            }
            return ret;
        }
        #endregion

        #region Helper

        public string[] SpatialViewNames(string name)
        {
            if (!name.Contains("@"))
            {
                throw new Exception("Not a correct Spatial View Name: FC_Name@View_Name");
            }

            int pos = name.IndexOf("@");

            return new string[] { name.Substring(0, pos), name.Substring(pos + 1, name.Length - pos - 1) };
        }

        public string OriginFcName(string name)
        {
            if (name.Contains("@"))
            {
                return name.Split('@')[0];
            }

            return name;
        }

        #endregion

        public ICommonDbConnection DbConnection { get { return _conn; } }
    }

    internal class SpatialTreeInfo : gView.Framework.FDB.ISpatialTreeInfo
    {
        private string _si_type = "";
        private IEnvelope _si_bounds = null;
        private double _si_spatialRatio = 0.0;
        private int _si_maxPerNode = 0;

        #region ISpatialTreeInfo Member

        public IEnvelope Bounds
        {
            get
            {
                return _si_bounds;
            }
            set
            {
                _si_bounds = value;
            }
        }

        public double SpatialRatio
        {
            get
            {
                return _si_spatialRatio;
            }
            set
            {
                _si_spatialRatio = value;
            }
        }

        public string type
        {
            get
            {
                return _si_type;
            }
            set
            {
                _si_type = value;
            }
        }

        public int MaxFeaturesPerNode
        {
            get
            {
                return _si_maxPerNode;
            }
            set
            {
                _si_maxPerNode = value;
            }
        }

        public Task<List<SpatialIndexNode>> SpatialIndexNodes()
        {
            return Task.FromResult<List<SpatialIndexNode>>(null);
        }

        #endregion
    }
}
