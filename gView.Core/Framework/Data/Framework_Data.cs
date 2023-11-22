using gView.Framework.Carto;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace gView.Framework.Data
{
    public interface IDatasetEnum
    {
        void Reset();
        IDataset Next { get; }
    }

    public interface IRasterDataset : IDataset
    {
        Task<IEnvelope> Envelope();

        Task<ISpatialReference> GetSpatialReference();
        void SetSpatialReference(ISpatialReference sRef);
    }

    public interface IRasterFileDataset : IRasterDataset
    {
        IRasterLayer AddRasterFile(string filename);
        IRasterLayer AddRasterFile(string filename, IPolygon polygon);

        string SupportedFileFilter { get; }
        int SupportsFormat(string extension);
    }

    public interface IFeatureDataset : IDataset
    {
        Task<IEnvelope> Envelope();

        Task<ISpatialReference> GetSpatialReference();
        void SetSpatialReference(ISpatialReference sRef);
    }

    public interface IDataset2 : IDataset
    {
        Task<IDataset2> EmptyCopy();
        Task AppendElement(string elementName);
    }

    //public interface IQueryOperations
    //{
    //    void BeginQueryOperations();
    //    void EndQueryOperations();
    //}

    public interface IFeatureDataset2 : IFeatureDataset, IDataset2
    {
    }

    public interface IImageDataset : IFeatureDataset
    {
        bool RenderImage(IDisplay display);
        global::System.Drawing.Image Bitmap { get; }
    }

    public interface IWebServiceLayer : ILayer
    {
        IWebServiceClass WebServiceClass { get; }
    }

    public class GeorefBitmap : IDisposable
    {
        public GraphicsEngine.Abstraction.IBitmap Bitmap = null;
        public IEnvelope Envelope = null;
        public ISpatialReference SpatialReference = null;
        public float Opacity = 1.0f;

        public GeorefBitmap(GraphicsEngine.Abstraction.IBitmap bitmap)
        {
            Bitmap = bitmap;
        }

        public void MakeTransparent(GraphicsEngine.ArgbColor transColor)
        {
            if (Bitmap == null)
            {
                return;
            }

            try
            {
                var b = GraphicsEngine.Current.Engine.CreateBitmap(Bitmap.Width, Bitmap.Height, GraphicsEngine.PixelFormat.Rgba32);
                using (var g = b.CreateCanvas())
                {
                    g.DrawBitmap(Bitmap, new GraphicsEngine.CanvasPoint(0, 0));
                }
                b.MakeTransparent(transColor);
                Bitmap.Dispose();
                Bitmap = b;
            }
            catch { }
        }

        #region IDisposable Member

        public void Dispose()
        {
            if (Bitmap != null)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }
            Envelope = null;
            SpatialReference = null;
        }

        #endregion
    }

    public delegate void AfterMapRequestEventHandler(IWebServiceClass sender, IDisplay display, GeorefBitmap image);

    public interface IWebServiceClass : IClass, IClone
    {
        event AfterMapRequestEventHandler AfterMapRequest;

        Task<bool> MapRequest(IDisplay display);
        Task<bool> LegendRequest(IDisplay display);
        GeorefBitmap Image { get; }
        GraphicsEngine.Abstraction.IBitmap Legend { get; }

        IEnvelope Envelope { get; }
        ISpatialReference SpatialReference { get; set; }
        List<IWebServiceTheme> Themes { get; }
    }

    public interface IWebServiceTheme : IFeatureLayer
    {
        string LayerID { get; }
        bool Locked { get; set; }
        IWebServiceClass ServiceClass { get; }
    }

    public enum DatasetState { unknown = 0, opened = 1 }

    public enum DatasetNameCase { ignore = 0, upper = 1, lower = 2, classNameUpper = 3, classNameLower = 4, fieldNamesUpper = 5, fieldNamesLower = 6 }
    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class)]
    public class UseDatasetNameCaseAttribute : global::System.Attribute
    {
        private DatasetNameCase _case;
        public UseDatasetNameCaseAttribute(DatasetNameCase nameCase)
        {
            _case = nameCase;
        }
        public DatasetNameCase Value
        {
            get { return _case; }
        }
    }

    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class)]
    public class ImportFeatureClassNameWithSchemaAttribute : global::System.Attribute
    {

    }

    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class)]
    public class ImportFeaturesBufferSizeAttribute : Attribute
    {
        public ImportFeaturesBufferSizeAttribute(int bufferSize)
        {
            this.BufferSize = bufferSize;
        }
        public int BufferSize { get; set; }
    }

    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class)]
    public class MaximumFieldnameLength : global::System.Attribute
    {
        private int _max;
        public MaximumFieldnameLength(int maxLength)
        {
            _max = maxLength;
        }
        public int Value
        {
            get { return _max; }
        }
    }

    public interface IDataset : IDisposable, gView.Framework.IO.IMetadata, IErrorMessage, IPersistableLoadAsync
    {
        string ConnectionString
        {
            get;
        }
        Task<bool> SetConnectionString(string connectionString);

        string DatasetGroupName
        {
            get;
        }
        string DatasetName
        {
            get;
        }

        string ProviderName
        {
            get;
        }

        DatasetState State { get; }

        Task<bool> Open();

        Task<List<IDatasetElement>> Elements();
        Task<IDatasetElement> Element(string title);

        string Query_FieldPrefix { get; }
        string Query_FieldPostfix { get; }

        IDatabase Database { get; }

        Task RefreshClasses();
    }

    public interface IServiceableDataset : gView.Framework.IO.IPersistable
    {
        string Name { get; }
        string Provider { get; }

        List<IDataset> Datasets { get; }
        bool GenerateNew();
    }

    public interface IRequestDependentDataset
    {
        Task<bool> Open(gView.MapServer.IServiceRequestContext context);
    }

    /// <summary>
    /// Provides access to members and properties that return information about a feature object.
    /// </summary>
    /// <remarks>
    /// <c>IFeature</c> implements <see cref="gView.Framework.IRow"/> to provide access to the object ID and field information.
    /// </remarks>
    public interface IFeature : IRow
    {
        /// <summary>
        /// The Shape of the feature.
        /// </summary>
        IGeometry Shape { get; set; }
    }
    public interface IGlobalFeature : IGlobalRow
    {
        /// <summary>
        /// The Shape of the feature.
        /// </summary>
        IGeometry Shape { get; set; }
    }

    public interface IClass
    {
        string Name { get; }
        string Aliasname { get; }

        IDataset Dataset { get; }
    }

    public interface ITableClass : IClass
    {
        //IQueryResult QueryResult { get ; set ; }
        //IQueryResult Search(IQueryFilter filter,bool queryGeometry);

        Task<ICursor> Search(IQueryFilter filter);
        Task<ISelectionSet> Select(IQueryFilter filter);

        IFieldCollection Fields { get; }
        IField FindField(string name);

        string IDFieldName { get; }
    }

    public interface ITableClass2 : ITableClass
    {
        Task<int> ExecuteCount(IQueryFilter filter);
    }

    public interface IFeatureClass : ITableClass, IGeometryDef
    {
        string ShapeFieldName { get; }
        IEnvelope Envelope { get; }

        Task<int> CountFeatures();

        //IFeature GetFeature(int id, getFeatureQueryType type);
        //IFeatureCursor GetFeatures(List<int> ids, getFeatureQueryType type);
        Task<IFeatureCursor> GetFeatures(IQueryFilter filter);
    }

    public interface IFeatureClass2 : IFeatureClass, ITableClass2
    {

    }

    public delegate void BeforeQueryEventHandler(ITableClass sender, ref IQueryFilter filter);
    public interface IBeforeQueryEventHandler
    {
        event BeforeQueryEventHandler BeforeQuery;
        void FireBeforeQureyEvent(ref IQueryFilter filter);
    }

    public interface IWebFeatureClass : IFeatureClass
    {
        string ID { get; }
    }

    public interface IWebRasterClass : IClass
    {
        string ID { get; }
    }

    public delegate void RowsAddedToTableEvent(int count);

    public interface ITable
    {
        event RowsAddedToTableEvent RowsAddedToTable;

        Task<int> Fill();
        Task<int> Fill(int next_N_Rows);
        Task<int> FillAtLeast(List<int> IDs);

        bool hasMore { get; }

        DataTable Table { get; }
        string IDFieldName { get; }
    }

    public interface IFeatureTable : ITable
    {
        IGeometry Shape(object ObjectID);
    }

    public interface IID
    {
        int ID { get; set; }
    }

    public interface IStringID
    {
        string SID { get; set; }
        bool HasSID { get; }
    }

    public delegate void PropertyChangedHandler();

    public interface IDatasetElement : IID, IStringID, gView.Framework.IO.IMetadata
    {
        string Title { get; set; }
        IClass Class { get; }
        //string Group { get ; set ; }

        int DatasetID { get; set; }

        event PropertyChangedHandler PropertyChanged;
        void FirePropertyChanged();
    }

    public interface ILayer : IDatasetElement, INamespace
    {
        bool Visible { get; set; }

        double MinimumScale { get; set; }
        double MaximumScale { get; set; }

        double MinimumLabelScale { get; set; }
        double MaximumLabelScale { get; set; }

        double MaximumZoomToFeatureScale { get; set; }

        IGroupLayer GroupLayer { get; }
    }

    public enum MapServerGrouplayerStyle
    {
        Dropdownable = 0,
        Checkbox = 1
    }

    public interface IGroupLayer : ILayer
    {
        List<ILayer> ChildLayer { get; }

        MapServerGrouplayerStyle MapServerStyle { get; set; }
    }

    public interface IFieldCollection : /*IEnumerable<IField>,*/ IClone
    {
        IField FindField(string aliasname);
        IField PrimaryDisplayField { get; set; }
        IField this[int i] { get; }
        int Count { get; }
        IEnumerable<IField> ToEnumerable();
    }

    public interface ITableLayer : IDatasetElement
    {
        ITableClass TableClass { get; }
    }

    public enum InterpolationMethod
    {
        Fast = GraphicsEngine.InterpolationMode.Low,
        NearestNeighbor = GraphicsEngine.InterpolationMode.NearestNeighbor,
        Bilinear = GraphicsEngine.InterpolationMode.Bilinear,
        Bicubic = GraphicsEngine.InterpolationMode.Bicubic /*,
        HighQuality = System.Drawing.Drawing2D.InterpolationMode.High */
    }

    public enum TileGridType
    {
        image_jpg = 0,
        image_png = 1,
        binary_float = 2
    }
    public enum TileLevelType
    {
        ConstantImagesize = 0,
        ConstantGeographicTilesize = 1
    }

    public interface IRasterClass : IClass
    {
        IPolygon Polygon { get; }

        double oX { get; }
        double oY { get; }
        double dx1 { get; }
        double dx2 { get; }
        double dy1 { get; }
        double dy2 { get; }

        ISpatialReference SpatialReference { get; set; }

        Task<IRasterPaintContext> BeginPaint(IDisplay display, ICancelTracker cancelTracker);
    }

    public interface IRasterCatalogClass : IFeatureClass, IRasterClass
    {
        Task<ICursor> ImageList();
    }

    public enum GridRenderMethode { None = 0, Colors = 1, HillShade = 4, NullValue = 8 }
    public class GridColorClass : gView.Framework.IO.IPersistable
    {
        public double MinValue;
        public double MaxValue;
        public ArgbColor Color;
        public string Legend;

        public GridColorClass(double minValue, double maxValue, ArgbColor color)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Color = color;
        }

        static public ArgbColor FindColor(double Value, GridColorClass[] classes)
        {
            if (classes == null)
            {
                return ArgbColor.White;
            }

            foreach (GridColorClass cc in classes)
            {
                if (Value >= cc.MinValue &&
                    Value <= cc.MaxValue)
                {
                    return cc.Color;
                }
            }

            return ArgbColor.Transparent;
        }

        #region IPersistable Member

        public void Load(gView.Framework.IO.IPersistStream stream)
        {
            MinValue = (double)stream.Load("MinValue", 0.0);
            MaxValue = (double)stream.Load("MaxValue", 0.0);
            Color = ArgbColor.FromArgb((int)stream.Load("Color", ArgbColor.White.ToArgb()));
            Legend = (string)stream.Load("Legend", String.Empty);
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            stream.Save("MinValue", MinValue);
            stream.Save("MaxValue", MaxValue);
            stream.Save("Color", Color.ToArgb());
            stream.Save("Legend", Legend);
        }

        #endregion
    }
    public interface IGridClass
    {
        GridRenderMethode ImplementsRenderMethods { get; }

        GridColorClass[] ColorClasses { get; set; }

        bool UseHillShade { get; set; }
        double[] HillShadeVector
        {
            get;
            set;
        }

        double MinValue { get; }
        double MaxValue { get; }
        double IgnoreDataValue { get; set; }
        bool UseIgnoreDataValue { get; set; }

        bool RenderRawGridValues { get; set; }
    }

    public interface IRasterLayer : ILayer
    {
        InterpolationMethod InterpolationMethod { get; set; }
        float Opacity { get; set; }
        ArgbColor TransparentColor { get; set; }
        FilterImplementations FilterImplementation { get; set; }

        IRasterClass RasterClass { get; }
    }

    public interface IRasterCatalogLayer : IFeatureLayer, IRasterLayer
    {
    }

    public interface IRasterFile
    {
        string Filename { get; }
        IRasterWorldFile WorldFile { get; }
    }

    public interface IRasterWorldFile
    {
        string Filename { get; }
        bool isGeoReferenced
        {
            get;
        }

        double dx_X { get; }
        double dx_Y { get; }
        double dy_X { get; }
        double dy_Y { get; }

        double cellX { get; }
        double cellY { get; }

        double X { get; }
        double Y { get; }
    }

    public interface IRasterFileBitmap
    {
        GraphicsEngine.Abstraction.IBitmap LoadBitmap();
    }

    public interface IRasterLayerCursor : ICursor
    {
        Task<IRasterLayer> NextRasterLayer();
    }

    public interface IParentRasterLayer
    {
        Task<IRasterLayerCursor> ChildLayers(gView.Framework.Carto.IDisplay display, string filterClause);
    }

    public enum joinType
    {
        LeftOuterJoin = 0,
        LeftInnerJoin = 1
    }

    public interface IFeatureLayerJoin : gView.Framework.IO.IPersistable, IDisposable, IClone
    {
        string Name { get; }

        string JoinName { get; set; }

        string Field { get; set; }

        IFieldCollection JoinFields { get; set; }
        Task<IRow> GetJoinedRow(string val);
        Task PerformCacheQuery(string[] vals);
        Task<ICursor> PerformQuery(IQueryFilter filter);

        void Init(string selectFieldNames);

        joinType JoinType { get; set; }

        void OnCreate(IMap map);
    }

    public class FeatureLayerJoins : List<IFeatureLayerJoin>, IPersistable, IClone
    {
        #region Members

        public IFeatureLayerJoin this[string joinName]
        {
            get
            {
                foreach (IFeatureLayerJoin join in this)
                {
                    if (join.JoinName == joinName)
                    {
                        return join;
                    }
                }
                return null;
            }
        }

        new public void Add(IFeatureLayerJoin join)
        {
            int counter = 1;
            string joinName = join.JoinName;
            while (this[join.JoinName] != null)
            {
                join.JoinName = joinName + "_" + (counter++);
            }

            base.Add(join);
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            IFeatureLayerJoin join;
            while ((join = stream.Load("Join", null) as IFeatureLayerJoin) != null)
            {
                this.Add(join);
            }
        }

        public void Save(IPersistStream stream)
        {
            foreach (IFeatureLayerJoin join in this)
            {
                stream.Save("Join", join);
            }
        }

        #endregion

        #region IClone Member

        public object Clone()
        {
            FeatureLayerJoins joins = new FeatureLayerJoins();

            foreach (IFeatureLayerJoin join in this)
            {
                joins.Add((IFeatureLayerJoin)join.Clone());
            }

            return joins;
        }

        #endregion
    }

    public interface ITableRelation : IPersistable
    {
        string RelationName { get; }
        IDatasetElement LeftTable { get; }
        IDatasetElement RightTable { get; }
        string LeftTableField { get; }
        string RightTableField { get; }

        string LogicalOperator { get; }

        Task<ICursor> GetLeftRows(string leftFields, object rightValue);
        Task<ICursor> GetRightRows(string rightFields, object leftValue);

        IQueryFilter GetLeftFilter(string leftFields, object rightValue);
        IQueryFilter GetRightFilter(string rightFields, object leftValue);
    }

    public interface ITableRelations : IEnumerable<ITableRelation>, IPersistable
    {
        void Add(ITableRelation tableRelation);
        bool Remove(ITableRelation tableRelation);

        IEnumerable<ITableRelation> GetRelations(IDatasetElement element);
    }

    public interface IFeatureLayer : ILayer
    {
        IFeatureRenderer FeatureRenderer { get; set; }
        IFeatureRenderer SelectionRenderer { get; set; }
        ILabelRenderer LabelRenderer { get; set; }

        bool ApplyRefScale { get; set; }
        bool ApplyLabelRefScale { get; set; }

        float MaxRefScaleFactor { get; set; }
        float MaxLabelRefScaleFactor { get; set; }

        IFeatureClass FeatureClass { get; }
        IQueryFilter FilterQuery { get; set; }

        IFieldCollection Fields { get; }

        FeatureLayerJoins Joins { get; set; }

        GeometryType LayerGeometryType { get; set; }
    }

    public delegate void BeforePointIdentifyEventHandler(object sender, IDisplay display, ref IPoint point, ref ISpatialReference sRef, IUserData userdata);
    public interface IBeforePointIdentifyEventHandler
    {
        event BeforePointIdentifyEventHandler BeforePointIdentify;
        void FireBeforePointIdentify(IDisplay display, ref IPoint point, ref ISpatialReference sRef, IUserData userdata);
    }

    public interface IPointIdentifyContext : IDisposable
    {
       
    }

    public class DummyPointIdentifyContext : IPointIdentifyContext
    {
        public void Dispose()
        {
        }
    }

    public interface IPointIdentify
    {
        IPointIdentifyContext CreatePointIdentifyContext();
        Task<ICursor> PointQuery(IDisplay display, IPoint point, ISpatialReference sRef, IUserData userdata, IPointIdentifyContext context);
    }

    public interface IMulitPointIdentify
    {
        Task<ICursor> MultiPointQuery(IDisplay display, IPointCollection points, ISpatialReference sRef, IUserData userdata);
    }

    public interface IGridIdentify
    {
        void InitGridQuery();
        float GridQuery(IDisplay display, IPoint point, ISpatialReference sRef);
        void ReleaseGridQuery();
    }

    public interface IMultiGridIdentify
    {
        Task<float[]> MultiGridQuery(gView.Framework.Carto.IDisplay display, IPoint[] Points, double dx, double dy, ISpatialReference sRef, IUserData userdata);
    }

    public interface ISearchTree
    {
        List<long> CollectNIDs(IGeometry geometry);
        List<long> CollectNIDsPlus(IEnvelope envelope);
        //List<long> CollectNIDs(IEnvelope bounds);
    }

    public interface ISelectionSet
    {
        void Clear();
        int Count { get; }
        void Combine(ISelectionSet selSet, CombinationMethod method);
    }

    public interface IIDSelectionSet : ISelectionSet
    {
        void AddID(int ID);
        void AddIDs(List<int> IDs);

        void RemoveID(int ID);
        void RemoveIDs(List<int> IDs);

        List<int> IDs { get; }
    }

    public interface IGlobalIDSelectionSet : ISelectionSet
    {
        void AddID(long ID);
        void AddIDs(List<long> IDs);

        void RemoveID(long ID);
        void RemoveIDs(List<long> IDs);

        List<long> IDs { get; }
    }

    public interface ISpatialIndexedIDSelectionSet : IIDSelectionSet
    {
        void AddID(int ID, IGeometry geometry);
        long NID(int id);
        List<int> IDsInEnvelope(IEnvelope envelope);
    }

    public interface ISpatialIndexedGlobalIDSelectionSet : IGlobalIDSelectionSet
    {
        void AddID(long ID, IGeometry geometry);
        long NID(long id);
        List<long> IDsInEnvelope(IEnvelope envelope);
    }

    public interface IQueryFilteredSelectionSet : ISelectionSet
    {
        IQueryFilter QueryFilter { get; }
    }

    public interface ISelectionCache
    {
        IFeatureCursor GetSelectedFeatures();
        IFeatureCursor GetSelectedFeatures(IDisplay display);
    }

    public enum CombinationMethod { New, Union, Intersection, Difference, SymDifference }
    public delegate void FeatureSelectionChangedEvent(IFeatureSelection sender);
    public delegate void BeforeClearSelectionEvent(IFeatureSelection sender);

    [global::System.AttributeUsageAttribute(global::System.AttributeTargets.Class)]
    public class UseWithinSelectableDatasetElements : global::System.Attribute
    {
        private bool _value = true;

        public UseWithinSelectableDatasetElements(bool val)
        {
            _value = val;
        }

        public bool Value
        {
            get { return _value; }
        }
    }

    public interface IFeatureSelection
    {
        event FeatureSelectionChangedEvent FeatureSelectionChanged;
        event BeforeClearSelectionEvent BeforeClearSelection;

        ISelectionSet SelectionSet { get; set; }
        Task<bool> Select(IQueryFilter filter, CombinationMethod methode);
        void ClearSelection();
        void FireSelectionChangedEvent();
    }

    public interface IConnectionString
    {
        string ConnectionString { get; set; }
    }

    public interface ICachedFeatureCollection
    {
        void AddFeature(IFeature feature);

        Guid CollectionGUID { get; }
        IQueryFilter QueryFilter { get; }
        bool UsableWith(IQueryFilter filter);

        IFeatureCursor FeatureCursor();
        IFeatureCursor FeatureCursor(IQueryFilter filter);
    }

    public interface IDataCopyright
    {
        bool HasDataCopyright { get; }
        string DataCopyrightText { get; }
    }
}