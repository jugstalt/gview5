using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.Network;
using gView.Framework.Network.Build;
using gView.Framework.Offline;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.Data
{
    internal class NetworkClassCreator : IProgressReporter, INetworkCreator
    {
        private IFeatureDataset? _dataset = null;
        private AccessFDB? _fdb = null;
        private List<IFeatureClass>? _edgeFcs = null, _nodeFcs = null;
        private string _networkName = String.Empty;
        private CancelTracker _cancelTracker = new CancelTracker();
        private string _onewayFieldName = "oneway";
        private List<int>? _complexEdgeFcs;
        private Dictionary<int, string>? _switchNodeFcs;
        private Dictionary<int, NetworkNodeType>? _nodeTypeFcs;
        private double _tolerance = double.Epsilon;
        private GraphWeights? _graphWeights = null;

        public NetworkClassCreator() { }
        public NetworkClassCreator(IFeatureDataset dataset,
                                   string networkName,
                                   List<IFeatureClass> edgeFcs, List<IFeatureClass> nodeFcs)
        {
            if (dataset == null || !(dataset.Database is AccessFDB))
            {
                return;
            }

            _dataset = dataset;
            _fdb = (AccessFDB)_dataset.Database;

            _networkName = networkName;
            this.EdgeFeatureClasses = edgeFcs;
            this.NodeFeatureClasses = nodeFcs;

            // Zum Testen -> wenn -1 -> alle als ComplexEdges möglich
            _complexEdgeFcs = new List<int>();
        }

        #region Properties

        public IFeatureDataset? FeatureDataset
        {
            get { return _dataset; }
            set
            {
                if (value == null || !(value.Database is AccessFDB))
                {
                    throw new Exception("Dataset is not an FDB Dataset");
                }

                _dataset = value;
                _fdb = (AccessFDB)_dataset.Database;
            }
        }

        public string NetworkName
        {
            get { return _networkName; }
            set { _networkName = value; }
        }

        public List<IFeatureClass>? EdgeFeatureClasses
        {
            get { return _edgeFcs; }
            set { _edgeFcs = value; }
        }
        public List<IFeatureClass>? NodeFeatureClasses
        {
            get { return _nodeFcs; }
            set { _nodeFcs = value; }
        }

        public double SnapTolerance
        {
            get { return _tolerance; }
            set { _tolerance = value; }
        }
        public List<int>? ComplexEdgeFcIds
        {
            get { return _complexEdgeFcs; }
            set { _complexEdgeFcs = value; }
        }
        public Dictionary<int, string>? SwitchNodeFcIdAndFieldnames
        {
            get { return _switchNodeFcs; }
            set { _switchNodeFcs = value; }
        }
        public Dictionary<int, NetworkNodeType>? NodeTypeFcIds
        {
            get { return _nodeTypeFcs; }
            set { _nodeTypeFcs = value; }
        }
        public GraphWeights? GraphWeights
        {
            get { return _graphWeights; }
            set { _graphWeights = value; }
        }

        public bool DeleteIfAlreadyExists { get; set; }

        #endregion

        async public Task Run()
        {
            if (_dataset == null || !(_fdb is IFeatureDatabaseReplication) || _edgeFcs == null)
            {
                return;
            }

            IFeatureDatabaseReplication db = (IFeatureDatabaseReplication)_fdb;

            if (await _dataset.Element(_networkName) != null)
            {
                if (DeleteIfAlreadyExists)
                {
                    if(await _fdb.DeleteFeatureClass(_networkName) == false)
                    {
                        throw new Exception(_fdb.LastErrorMessage);
                    }
                }
                else
                {
                    throw new Exception($"Featureclass '{_networkName}' already exists!");
                }
            }
            bool succeeded = false;
            List<int> FcIds = new List<int>();
            try
            {
                ProgressReport report = new ProgressReport();

                int datasetId = await _fdb.DatasetID(_dataset.DatasetName);
                if (datasetId == -1)
                {
                    return;
                }

                NetworkBuilder networkBuilder = new NetworkBuilder(await _dataset.Envelope(), _tolerance);
                if (ReportProgress != null)
                {
                    networkBuilder.reportProgress += new ProgressReporterEvent(networkBuilder_reportProgress);
                }

                #region Spatial Index

                BinaryTreeDef? edgeTreeDef = null, nodeTreeDef = null;
                
                foreach (IFeatureClass fc in _edgeFcs)
                {
                    BinaryTreeDef treeDef = await _fdb.BinaryTreeDef(fc.Name);
                    if (treeDef == null)
                    {
                        IEnvelope bounds = fc.Envelope;
                        if (Envelope.IsNull(bounds))
                        {
                            continue;
                        }

                        treeDef = new BinaryTreeDef(fc.Envelope, 10);
                    }
                    if (edgeTreeDef == null)
                    {
                        edgeTreeDef = new BinaryTreeDef(treeDef.Bounds, Math.Min(treeDef.MaxLevel, 10));
                    }
                    else
                    {
                        Envelope bounds = edgeTreeDef.Bounds;
                        bounds.Union(treeDef.Bounds);
                        edgeTreeDef = new BinaryTreeDef(bounds, Math.Min(Math.Max(edgeTreeDef.MaxLevel, treeDef.MaxLevel), 10));
                    }
                }

                if (_nodeFcs != null)
                {
                    foreach (IFeatureClass fc in _nodeFcs)
                    {
                        BinaryTreeDef treeDef = await _fdb.BinaryTreeDef(fc.Name);
                        if (treeDef == null)
                        {
                            IEnvelope bounds = fc.Envelope;
                            if (Envelope.IsNull(bounds))
                            {
                                continue;
                            }

                            treeDef = new BinaryTreeDef(fc.Envelope, 10);
                        }
                        if (nodeTreeDef == null)
                        {
                            nodeTreeDef = new BinaryTreeDef(treeDef.Bounds, Math.Min(treeDef.MaxLevel, 10));
                        }
                        else
                        {
                            Envelope bounds = nodeTreeDef.Bounds;
                            bounds.Union(treeDef.Bounds);
                            nodeTreeDef = new BinaryTreeDef(bounds, Math.Min(Math.Max(nodeTreeDef.MaxLevel, treeDef.MaxLevel), 10));
                        }
                    }
                }

                #endregion

                #region Add Edges

                foreach (IFeatureClass fc in _edgeFcs)
                {
                    int fcId = await _fdb.FeatureClassID(datasetId, fc.Name);
                    if (fcId == -1)
                    {
                        continue;
                    }

                    FcIds.Add(fcId);

                    bool useOneway = false;
                    QueryFilter filter = new QueryFilter();
                    filter.AddField(fc.IDFieldName);
                    filter.AddField(fc.ShapeFieldName);
                    if (!String.IsNullOrEmpty(_onewayFieldName) && fc.FindField(_onewayFieldName) != null)
                    {
                        filter.AddField(_onewayFieldName);
                        useOneway = true;
                    }
                    Dictionary<Guid, IGraphWeightFeatureClass>? gwfcs = new Dictionary<Guid, IGraphWeightFeatureClass>();
                    if (_graphWeights != null)
                    {
                        foreach (IGraphWeight weight in _graphWeights)
                        {
                            foreach (IGraphWeightFeatureClass gwfc in weight.FeatureClasses)
                            {
                                if (gwfc.FcId == fcId && !String.IsNullOrEmpty(gwfc.FieldName))
                                {
                                    gwfcs.Add(weight.Guid, gwfc);
                                    filter.AddField(gwfc.FieldName);
                                }
                            }
                        }
                    }
                    if (gwfcs.Keys.Count == 0)
                    {
                        gwfcs = null;
                    }

                    bool useWithComplexEdges = false;

                    if (_complexEdgeFcs != null &&
                       (_complexEdgeFcs.Contains(fcId) || _complexEdgeFcs.Contains(-1)))
                    {
                        useWithComplexEdges = true;
                    }

                    using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                    {
                        #region Report
                        report.Message = "Analize Edges: " + fc.Name;
                        report.featureMax = await fc.CountFeatures();
                        report.featurePos = 0;
                        if (ReportProgress != null)
                        {
                            ReportProgress(report);
                        }
                        #endregion

                        IFeature feature;
                        
                        while ((feature = await cursor.NextFeature()) != null)
                        {
                            bool oneway = false;
                            
                            if (useOneway)
                            {
                                int ow = Convert.ToInt32(feature[_onewayFieldName]);
                                oneway = (ow != 0);
                            }
                            
                            Hashtable? gw = null;

                            if (gwfcs != null)
                            {
                                foreach (Guid weightGuid in gwfcs.Keys)
                                {
                                    IGraphWeightFeatureClass gwfc = gwfcs[weightGuid];
                                    object objVal = feature[gwfc.FieldName];

                                    double val = (objVal == DBNull.Value) ? 0.0 : Convert.ToDouble(feature[gwfc.FieldName]);
                                    if (gwfc.SimpleNumberCalculation != null)
                                    {
                                        val = gwfc.SimpleNumberCalculation.Calculate(val);
                                    }

                                    if (gw == null)
                                    {
                                        gw = new Hashtable();
                                    }

                                    gw.Add(weightGuid, val);
                                }
                            }
                            networkBuilder.AddEdgeFeature(fcId, feature, oneway, gw, useWithComplexEdges);

                            #region Report
                            report.featurePos++;
                            if (report.featurePos % 1000 == 0)
                            {
                                if (ReportProgress != null)
                                {
                                    ReportProgress(report);
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Calculate ComplexEdges

                if (_complexEdgeFcs != null && _complexEdgeFcs.Count > 0)
                {
                    List<NetworkNode> networkNodes = networkBuilder.NetworkNodes.ToList();

                    #region Report
                    report.Message = "Create Complex Edges...";
                    report.featureMax = networkNodes.Count;
                    report.featurePos = 0;
                    if (ReportProgress != null)
                    {
                        ReportProgress(report);
                    }
                    #endregion

                    foreach (NetworkNode node in networkNodes)
                    {
                        networkBuilder.SplitToComplexEdges(node);

                        #region Report
                        report.featurePos++;
                        if (report.featurePos % 1000 == 0)
                        {
                            if (ReportProgress != null)
                            {
                                ReportProgress(report);
                            }
                        }
                        #endregion
                    }
                }

                #endregion

                #region Add Nodes
                
                if (_nodeFcs != null)
                {
                    foreach (IFeatureClass fc in _nodeFcs)
                    {
                        int fcId = await _fdb.FeatureClassID(datasetId, fc.Name);
                        if (fcId == -1)
                        {
                            continue;
                        }

                        FcIds.Add(fcId);

                        bool isSwitchable = false;
                        string switchStateFieldname = String.Empty;
                        if (_switchNodeFcs != null && _switchNodeFcs.ContainsKey(fcId))
                        {
                            isSwitchable = true;
                            switchStateFieldname = _switchNodeFcs[fcId];
                        }
                        NetworkNodeType nodeType = NetworkNodeType.Unknown;
                        if (_nodeTypeFcs != null && _nodeTypeFcs.ContainsKey(fcId))
                        {
                            nodeType = _nodeTypeFcs[fcId];
                        }

                        QueryFilter filter = new QueryFilter();
                        filter.AddField(fc.IDFieldName);
                        filter.AddField(fc.ShapeFieldName);
                        filter.AddField(switchStateFieldname);

                        using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                        {
                            #region Report
                            report.Message = "Analize Nodes: " + fc.Name;
                            report.featureMax = await fc.CountFeatures();
                            report.featurePos = 0;
                            if (ReportProgress != null)
                            {
                                ReportProgress(report);
                            }
                            #endregion

                            IFeature feature;
                            while ((feature = await cursor.NextFeature()) != null)
                            {
                                bool switchState = isSwitchable;
                                if (isSwitchable && !String.IsNullOrEmpty(switchStateFieldname))
                                {
                                    object so = feature[switchStateFieldname];
                                    if (so != null)
                                    {
                                        if (so is bool)
                                        {
                                            switchState = (bool)so;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                switchState = Convert.ToInt32(so) > 0;
                                            }
                                            catch { switchState = false; }
                                        }
                                    }
                                }
                                networkBuilder.AddNodeFeature(fcId, feature, isSwitchable, switchState, nodeType);

                                #region Report
                                report.featurePos++;
                                if (report.featurePos % 1000 == 0)
                                {
                                    if (ReportProgress != null)
                                    {
                                        ReportProgress(report);
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                }
                
                #endregion

                #region CreateGraph
                #region Report
                
                report.Message = "Create Graph";
                report.featurePos = 0;
                if (ReportProgress != null)
                {
                    ReportProgress(report);
                
                }

                #endregion

                networkBuilder.CreateGraph();

                #endregion

                #region Create Edge Featureclass

                #region Simple Edges Table

                FieldCollection fields = new FieldCollection();
                fields.Add(new Field("Page", FieldType.integer));
                fields.Add(new Field("Data", FieldType.binary));
                await db.CreateIfNotExists(_networkName + "_Edges", fields);
                
                #endregion

                #region Edge Index Table
                
                fields = new FieldCollection();
                fields.Add(new Field("EID", FieldType.integer));
                fields.Add(new Field("FCID", FieldType.integer));
                fields.Add(new Field("OID", FieldType.integer));
                fields.Add(new Field("ISCOMPLEX", FieldType.boolean));
                await db.CreateIfNotExists(_networkName + "_EdgeIndex", fields);
                
                #endregion

                #region Complex Edges FeatureClass
                
                fields = new FieldCollection();
                fields.Add(new Field("EID", FieldType.integer));
                fields.Add(new Field("N1", FieldType.integer));
                fields.Add(new Field("N2", FieldType.integer));
                fields.Add(new Field("FCID", FieldType.integer));
                fields.Add(new Field("OID", FieldType.integer));
                //fields.Add(new Field("Length", FieldType.integer));
                //fields.Add(new Field("GeoLength", FieldType.integer));

                await _fdb.ReplaceFeatureClass(_dataset.DatasetName,
                    _networkName + "_ComplexEdges",
                    new GeometryDef(GeometryType.Polyline),
                    fields);
                IDatasetElement element = await _dataset.Element(_networkName + "_ComplexEdges");
                if (element == null || !(element.Class is IFeatureClass))
                {
                    return;
                }

                if (edgeTreeDef != null)
                {
                    await _fdb.SetSpatialIndexBounds(_networkName + "_ComplexEdges", "BinaryTree2", edgeTreeDef.Bounds, edgeTreeDef.SplitRatio, edgeTreeDef.MaxPerNode, edgeTreeDef.MaxLevel);
                }
                
                #endregion

                int edge_page = 0;
                List<IFeature> features = new List<IFeature>(), features2 = new List<IFeature>();
                List<IRow> rows = new List<IRow>();
                IFeatureClass edgeFc = (IFeatureClass)element.Class;
                IFeatureCursor c = networkBuilder.Edges;
                IFeature f;

                #region Report
                
                report.Message = "Create Edges";
                if (c is ICount)
                {
                    report.featureMax = ((ICount)c).Count;
                }

                report.featurePos = 0;
                if (ReportProgress != null)
                {
                    ReportProgress(report);
                }

                #endregion

                string tabEdgesName = _fdb.TableName(_networkName + "_Edges");
                string tabEdgeIndexName = _fdb.TableName(_networkName + "_EdgeIndex");

                while ((f = await c.NextFeature()) != null)
                {
                    int eid = (int)f["EID"];
                    if ((bool)f["ISCOMPLEX"] == true)
                    {
                        #region Complex Edges

                        features.Add(f);

                        report.featurePos++;
                        if (features.Count > 0 && features.Count % 1000 == 0)
                        {
                            if (ReportProgress != null)
                            {
                                ReportProgress(report);
                            }

                            await _fdb.Insert(edgeFc, features);
                            features.Clear();
                        }
                        
                        #endregion
                    }
                    #region Edges Table

                    features2.Add(f);
                    if (NetworkObjectSerializer.Page(eid + 1) > edge_page)
                    {
                        IRow row = new Row();
                        row.Fields.Add(new FieldValue("Page", edge_page++));
                        row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.SerializeEdges(features2)));
                        db.InsertRow(tabEdgesName, row, null);
                        features2.Clear();
                    }
                    #endregion

                    #region Edge Index

                    IRow eir = new Row();
                    eir.Fields.Add(new FieldValue("EID", (int)f["EID"]));
                    eir.Fields.Add(new FieldValue("FCID", (int)f["FCID"]));
                    eir.Fields.Add(new FieldValue("OID", (int)f["OID"]));
                    eir.Fields.Add(new FieldValue("ISCOMPLEX", (bool)f["ISCOMPLEX"]));
                    rows.Add(eir);
                    if (rows.Count > 0 && rows.Count % 1000 == 0)
                    {
                        _fdb.InsertRows(tabEdgeIndexName, rows, null);
                        rows.Clear();
                    }

                    #endregion
                }
                if (rows.Count > 0)
                {
                    _fdb.InsertRows(tabEdgeIndexName, rows, null);
                    rows.Clear();
                }
                if (features2.Count > 0)
                {
                    IRow row = new Row();
                    row.Fields.Add(new FieldValue("Page", edge_page));
                    row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.SerializeEdges(features2)));
                    db.InsertRow(tabEdgesName, row, null);
                    features2.Clear();
                }
                if (features.Count > 0)
                {
                    if (ReportProgress != null)
                    {
                        ReportProgress(report);
                    }

                    await _fdb.Insert(edgeFc, features);
                }
                await _fdb.CalculateExtent(edgeFc);

                #endregion
                
                #region Create Weights
                
                if (_graphWeights != null)
                {
                    foreach (IGraphWeight weight in _graphWeights)
                    {
                        fields = new FieldCollection();
                        fields.Add(new Field("Page", FieldType.integer));
                        fields.Add(new Field("Data", FieldType.binary));
                        await db.CreateIfNotExists(_networkName + "_Weights_" + weight.Guid.ToString("N").ToLower(), fields);

                        string tabWeightName = _fdb.TableName(_networkName + "_Weights_" + weight.Guid.ToString("N").ToLower());

                        NetworkEdges edges = networkBuilder.NetworkEdges;
                        int weight_page = 0;
                        int counter = 0;
                        BinaryWriter bw = NetworkObjectSerializer.GetBinaryWriter();
                        foreach (NetworkEdge edge in edges)
                        {
                            counter++;
                            if (NetworkObjectSerializer.Page(counter) > weight_page)
                            {
                                IRow row = new Row();
                                row.Fields.Add(new FieldValue("Page", weight_page++));
                                row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.GetBuffer(bw)));
                                db.InsertRow(tabWeightName, row, null);
                                bw = NetworkObjectSerializer.GetBinaryWriter();
                            }

                            if (edge.Weights != null && edge.Weights.ContainsKey(weight.Guid))
                            {
                                NetworkObjectSerializer.WriteWeight(bw, weight, (double)edge.Weights[weight.Guid]);
                            }
                            else
                            {
                                NetworkObjectSerializer.WriteWeight(bw, weight, (double)0.0);
                            }


                        }

                        if (bw.BaseStream.Position > 0)
                        {
                            IRow row = new Row();
                            row.Fields.Add(new FieldValue("Page", weight_page++));
                            row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.GetBuffer(bw)));
                            db.InsertRow(tabWeightName, row, null);
                        }
                    }
                }
                #endregion

                #region Create Node Featureclass
                
                fields = new FieldCollection();
                //fields.Add(new Field("NID", FieldType.integer));
                //fields.Add(new Field("G1", FieldType.integer));
                //fields.Add(new Field("G2", FieldType.integer));
                fields.Add(new Field("SWITCH", FieldType.boolean));
                fields.Add(new Field("STATE", FieldType.boolean));
                fields.Add(new Field("FCID", FieldType.integer));
                fields.Add(new Field("OID", FieldType.integer));
                fields.Add(new Field("NODETYPE", FieldType.integer));

                await _fdb.ReplaceFeatureClass(_dataset.DatasetName,
                    _networkName + "_Nodes",
                    new GeometryDef(GeometryType.Point),
                    fields);

                element = await _dataset.Element(_networkName + "_Nodes");
                if (element == null || !(element.Class is IFeatureClass))
                {
                    return;
                }

                if (nodeTreeDef != null)
                {
                    await _fdb.SetSpatialIndexBounds(_networkName + "_Nodes", "BinaryTree2", nodeTreeDef.Bounds, nodeTreeDef.SplitRatio, nodeTreeDef.MaxPerNode, nodeTreeDef.MaxLevel);
                }
                else if (edgeTreeDef != null)
                {
                    await _fdb.SetSpatialIndexBounds(_networkName + "_Nodes", "BinaryTree2", edgeTreeDef.Bounds, edgeTreeDef.SplitRatio, edgeTreeDef.MaxPerNode, edgeTreeDef.MaxLevel);
                }

                features.Clear();
                IFeatureClass nodeFc = (IFeatureClass)element.Class;
                c = networkBuilder.Nodes;

                #region Report
                report.Message = "Create Nodes";
                if (c is ICount)
                {
                    report.featureMax = ((ICount)c).Count;
                }

                report.featurePos = 0;
                if (ReportProgress != null)
                {
                    ReportProgress(report);
                }

                #endregion

                while ((f = await c.NextFeature()) != null)
                {
                    features.Add(f);

                    report.featurePos++;
                    if (features.Count > 0 && features.Count % 1000 == 0)
                    {
                        if (ReportProgress != null)
                        {
                            ReportProgress(report);
                        }

                        await _fdb.Insert(nodeFc, features);
                        features.Clear();
                    }
                }
                if (features.Count > 0)
                {
                    if (ReportProgress != null)
                    {
                        ReportProgress(report);
                    }

                    await _fdb.Insert(nodeFc, features);
                }
                await _fdb.CalculateExtent(nodeFc);
                
                #endregion
                
                #region Create Graph Class
                
                int graph_page = 0;
                fields = new FieldCollection();
                fields.Add(new Field("Page", FieldType.integer));
                fields.Add(new Field("Data", FieldType.binary));
                //fields.Add(new Field("N1", FieldType.integer));
                //fields.Add(new Field("N2", FieldType.integer));
                //fields.Add(new Field("EID", FieldType.integer));
                //fields.Add(new Field("LENGTH", FieldType.Double));
                //fields.Add(new Field("GEOLENGTH", FieldType.Double));

                await _fdb.ReplaceFeatureClass(_dataset.DatasetName,
                    _networkName,
                    new GeometryDef(GeometryType.Network),
                    fields);

                element = await _dataset.Element(_networkName);
                if (element == null || !(element.Class is IFeatureClass))
                {
                    return;
                }

                features.Clear();
                IFeatureClass networkFc = (IFeatureClass)element.Class;
                c = networkBuilder.Graph;

                #region Report

                report.Message = "Create Network";
                if (c is ICount)
                {
                    report.featureMax = ((ICount)c).Count;
                }

                report.featurePos = 0;
                if (ReportProgress != null)
                {
                    ReportProgress(report);
                }

                #endregion

                string fcNetworkName = _fdb.TableName("FC_" + _networkName);

                while ((f = await c.NextFeature()) != null)
                {
                    report.featurePos++;

                    // Wenn aktuelles Feature in neue Page gehört -> 
                    // bestehende, vor neuem Einfügen speichern und pageIndex erhöhen (graph_page++)
                    if (NetworkObjectSerializer.Page((int)f["N1"]) > graph_page)
                    {
                        if (ReportProgress != null)
                        {
                            ReportProgress(report);
                        }

                        IRow row = new Row();
                        row.Fields.Add(new FieldValue("Page", graph_page++));
                        row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.SerializeGraph(features)));
                        db.InsertRow(fcNetworkName, row, null);
                        features.Clear();
                    }

                    features.Add(f);
                }

                if (features.Count > 0)
                {
                    IRow row = new Row();
                    row.Fields.Add(new FieldValue("Page", graph_page));
                    row.Fields.Add(new FieldValue("Data", NetworkObjectSerializer.SerializeGraph(features)));
                    db.InsertRow(fcNetworkName, row, null);
                    features.Clear();
                }

                #endregion

                #region Create FDB_Networks

                int netId = await _fdb.GetFeatureClassID(_networkName);
                fields = new FieldCollection();
                fields.Add(new Field("ID", FieldType.integer));
                fields.Add(new Field("Properties", FieldType.binary));
                await db.CreateIfNotExists("FDB_Networks", fields);

                NetworkObjectSerializer.NetworkProperties networkProps = new NetworkObjectSerializer.NetworkProperties(
                    NetworkObjectSerializer.PageSize, _tolerance);

                IRow row2 = new Row();
                row2.Fields.Add(new FieldValue("ID", netId));
                row2.Fields.Add(new FieldValue("Properties", networkProps.Serialize()));
                db.InsertRow(_fdb.TableName("FDB_Networks"), row2, null);
                
                #endregion

                #region Create FDB_NetworkClasses
                
                fields = new FieldCollection();
                fields.Add(new Field("NetworkId", FieldType.integer));
                fields.Add(new Field("FCID", FieldType.integer));
                fields.Add(new Field("Properties", FieldType.binary));

                await db.CreateIfNotExists("FDB_NetworkClasses", fields);
                foreach (int fcId in FcIds)
                {
                    bool isSwitchable = (_switchNodeFcs != null && _switchNodeFcs.ContainsKey(fcId));
                    string switchStateFieldname = (isSwitchable ? _switchNodeFcs![fcId] : String.Empty);

                    NetworkObjectSerializer.NetworkClassProperties nwclsProps = new NetworkObjectSerializer.NetworkClassProperties(
                        _complexEdgeFcs?.Contains(fcId) == true,
                        isSwitchable, switchStateFieldname);

                    IRow row = new Row();
                    row.Fields.Add(new FieldValue("NetworkId", netId));
                    row.Fields.Add(new FieldValue("FCID", fcId));
                    row.Fields.Add(new FieldValue("Properties", nwclsProps.Serialize()));
                    db.InsertRow(_fdb.TableName("FDB_NetworkClasses"), row, null);
                }

                #endregion

                #region FDB_NetworkWeighs
                
                fields = new FieldCollection();
                fields.Add(new Field("NetworkId", FieldType.integer));
                fields.Add(new Field("Name", FieldType.String));
                fields.Add(new Field("WeightGuid", FieldType.guid));
                fields.Add(new Field("Properties", FieldType.binary));

                await db.CreateIfNotExists("FDB_NetworkWeights", fields);
                if (_graphWeights != null)
                {
                    foreach (IGraphWeight weight in _graphWeights)
                    {
                        IRow row = new Row();
                        row.Fields.Add(new FieldValue("NetworkId", netId));
                        row.Fields.Add(new FieldValue("Name", weight.Name));
                        row.Fields.Add(new FieldValue("WeightGuid", weight.Guid));
                        row.Fields.Add(new FieldValue("Properties", NetworkObjectSerializer.SerializeWeight(weight)));
                        db.InsertRow(_fdb.TableName("FDB_NetworkWeights"), row, null);
                    }
                }

                #endregion

                succeeded = true;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (!succeeded)
                {
                    if (_fdb is IAlterDatabase)
                    {
                        ((IAlterDatabase)_fdb).DropTable(_networkName + "_Edges");
                    }

                    await _fdb.DeleteFeatureClass(_networkName + "_ComplexEdges");
                    await _fdb.DeleteFeatureClass(_networkName + "_Nodes");
                    await _fdb.DeleteFeatureClass(_networkName);
                }
            }
        }

        void networkBuilder_reportProgress(ProgressReport progressEventReport)
        {
            if (ReportProgress != null)
            {
                ReportProgress(progressEventReport);
            }
        }

        #region IProgressReporter Member

        public event ProgressReporterEvent? ReportProgress = null;

        public ICancelTracker CancelTracker
        {
            get { return _cancelTracker; }
        }

        #endregion
    }
}
