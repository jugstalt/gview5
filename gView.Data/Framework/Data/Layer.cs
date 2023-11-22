using gView.Framework.Carto;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Data.Metadata;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.system.Diagnostics;
using gView.GraphicsEngine.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Framework.Data
{
    public class DataElementID : DatasetElementMetadata, IID, IStringID
    {
        private int _id = 0;
        private string _sid = null;

        #region IID Member

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        #endregion

        #region IStringID Member

        public string SID
        {
            get
            {
                if (String.IsNullOrEmpty(_sid))
                {
                    return _id.ToString();
                }

                return _sid;
            }
            set
            {
                _sid = value;
            }
        }

        public bool HasSID
        {
            get { return !String.IsNullOrEmpty(_sid); }
        }
        #endregion
    }

    public class DatasetElement : DataElementID, IDatasetElement
    {
        protected string _title = "";
        protected IClass _class = null;
        protected int _datasetID = 0;

        public DatasetElement() { }
        public DatasetElement(IClass Class)
        {
            _class = Class;
            if (_class != null)
            {
                _title = _class.Name;
            }
        }
        public DatasetElement(IDatasetElement element)
        {
            CopyFrom(element);
        }

        internal virtual void CopyFrom(IDatasetElement element)
        {
            if (element == null)
            {
                return;
            }

            this.ID = element.ID;
            this.SID = element.HasSID ? element.SID : null;

            _title = element.Title;
            //_class = element.Class;
            _datasetID = element.DatasetID;
        }

        #region IDatasetElement Member

        public string Title { get { return _title; } set { _title = value; } }
        public IClass Class
        {
            get { return _class; }
            set { _class = value; }
        }
        public virtual int DatasetID
        {
            get
            {
                return _datasetID;
            }
            set
            {
                _datasetID = value;
            }
        }
        #endregion

        public IClass Class2
        {
            set { _class = value; }
        }

        #region IDatasetElement Member


        public event PropertyChangedHandler PropertyChanged = null;

        public void FirePropertyChanged()
        {
            Refresh();

            if (PropertyChanged != null)
            {
                PropertyChanged();
            }
        }

        #endregion

        virtual protected void Refresh()
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Layer : DatasetElement, ILayer, IPersistable
    {
        protected bool _visible = true;
        protected double _minimumScale = -1, _maximumScale = -1;
        protected double _minimumLabelScale = -1, _maximumLabelScale = -1;
        protected double _MaximumZoomToFeatureScale = 100;
        protected IGroupLayer _groupLayer = null;
        protected string _namespace = String.Empty;

        //protected QueryResult m_queryResult;

        public Layer()
        {
        }

        public Layer(ILayer layer)
        {
            CopyFrom(layer);
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is ILayer)
            {
                ILayer layer = element as ILayer;

                if (layer == null)
                {
                    return;
                }

                _visible = layer.Visible;
                _minimumScale = layer.MinimumScale;
                _maximumScale = layer.MaximumScale;

                _minimumLabelScale = layer.MinimumLabelScale;
                _maximumLabelScale = layer.MaximumLabelScale;

                _MaximumZoomToFeatureScale = layer.MaximumZoomToFeatureScale;

            }
        }

        static private void minimumScale(ILayer layer, ref double scale)
        {
            if (layer == null || layer.GroupLayer == null)
            {
                return;
            }

            if (scale <= 1.0)
            {
                scale = layer.GroupLayer.MinimumScale;
            }
            else if (layer.GroupLayer.MinimumScale > 1.0)
            {
                scale = Math.Max(layer.GroupLayer.MinimumScale, scale);
            }
        }
        static private void maximumScale(ILayer layer, ref double scale)
        {
            if (layer == null || layer.GroupLayer == null)
            {
                return;
            }

            if (scale <= 1.0)
            {
                scale = layer.GroupLayer.MaximumScale;
            }
            else if (layer.GroupLayer.MaximumScale > 1.0)
            {
                scale = Math.Min(layer.GroupLayer.MaximumScale, scale);
            }
        }

        static private void minimumLabelScale(ILayer layer, ref double scale)
        {
            if (layer == null || layer.GroupLayer == null)
            {
                return;
            }

            if (scale <= 1.0)
            {
                scale = layer.GroupLayer.MinimumLabelScale;
            }
            else if (layer.GroupLayer.MinimumLabelScale > 1.0)
            {
                scale = Math.Max(layer.GroupLayer.MinimumLabelScale, scale);
            }
        }
        static private void maximumLabelScale(ILayer layer, ref double scale)
        {
            if (layer == null || layer.GroupLayer == null)
            {
                return;
            }

            if (scale <= 1.0)
            {
                scale = layer.GroupLayer.MaximumLabelScale;
            }
            else if (layer.GroupLayer.MaximumLabelScale > 1.0)
            {
                scale = Math.Min(layer.GroupLayer.MaximumLabelScale, scale);
            }
        }

        static private void maximumZoomToFeatureScale(ILayer layer, ref double scale)
        {
            if (layer == null || layer.GroupLayer == null)
            {
                return;
            }

            if (scale <= 1.0)
            {
                scale = layer.GroupLayer.MaximumZoomToFeatureScale;
            }
            else if (layer.GroupLayer.MaximumZoomToFeatureScale > 1.0)
            {
                scale = Math.Max(layer.GroupLayer.MaximumZoomToFeatureScale, scale);
            }
        }

        static private void visible(ILayer layer, ref bool visible)
        {
            if (layer == null || layer.GroupLayer == null || visible == false)
            {
                return;
            }

            if (layer.GroupLayer.Visible == false)
            {
                visible = false;
            }
        }

        #region ILayer Member

        public bool Visible
        {
            get
            {
                bool visible = _visible;
                Layer.visible(this, ref visible);
                return visible;
            }
            set
            {
                _visible = value;
            }
        }

        public double MinimumScale
        {
            get
            {
                double scale = _minimumScale;
                Layer.minimumScale(this, ref scale);
                return scale;
            }
            set { _minimumScale = value; }
        }
        public double MaximumScale
        {
            get
            {
                double scale = _maximumScale;
                Layer.maximumScale(this, ref scale);
                return scale;
            }
            set { _maximumScale = value; }
        }

        public double MinimumLabelScale
        {
            get
            {
                double scale = _minimumLabelScale;
                Layer.minimumLabelScale(this, ref scale);
                return scale;
            }
            set { _minimumLabelScale = value; }
        }
        public double MaximumLabelScale
        {
            get
            {
                double scale = _maximumLabelScale;
                Layer.maximumLabelScale(this, ref scale);
                return scale;
            }
            set { _maximumLabelScale = value; }
        }

        public double MaximumZoomToFeatureScale
        {
            get
            {
                double scale = _MaximumZoomToFeatureScale;
                Layer.maximumZoomToFeatureScale(this, ref scale);
                return scale;
            }
            set { _MaximumZoomToFeatureScale = value; }
        }

        public IGroupLayer GroupLayer
        {
            get { return _groupLayer; }
            set { _groupLayer = value; }
        }
        #endregion

        #region INamespace Member

        public string Namespace
        {
            get
            {
                return _namespace;
            }
            set
            {
                _namespace = value;
            }
        }

        #endregion

        #region IPersistable Member

        virtual public void Load(IPersistStream stream)
        {
            this.ID = (int)stream.Load("ID", 0);
            this.SID = (string)stream.Load("SID", null);

            _title = (string)stream.Load("Title", "");
            _datasetID = (int)stream.Load("DatasetID");

            _namespace = (string)stream.Load("Namespace", String.Empty);

            _visible = (bool)stream.Load("visible", true);
            _minimumScale = (double)stream.Load("MinimumScale", 0.0);
            _maximumScale = (double)stream.Load("MaximumScale", 0.0);

            _minimumLabelScale = (double)stream.Load("MinimumLabelScale", 0.0);
            _maximumLabelScale = (double)stream.Load("MaximumLabelScale", 0.0);

            _MaximumZoomToFeatureScale = (double)stream.Load("MaximumZoomToFeatureScale", 0.0);
        }

        virtual public void Save(IPersistStream stream)
        {
            stream.Save("ID", this.ID);
            if (HasSID)
            {
                stream.Save("SID", this.SID);
            }

            stream.Save("Title", _title);
            stream.Save("DatasetID", _datasetID);

            stream.Save("Namespace", _namespace);

            stream.Save("visible", _visible);
            stream.Save("MinimumScale", _minimumScale);
            stream.Save("MaximumScale", _maximumScale);

            stream.Save("MinimumLabelScale", _minimumLabelScale);
            stream.Save("MaximumLabelScale", _maximumLabelScale);

            stream.Save("MaximumZoomToFeatureScale", _MaximumZoomToFeatureScale);
        }

        #endregion
    }


    public class GroupLayer : Layer, IGroupLayer
    {
        private List<ILayer> _childLayers = new List<ILayer>();

        public GroupLayer() { }
        public GroupLayer(string name)
        {
            this.Title = name;
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);
        }

        #region IGroupLayer Member

        public List<ILayer> ChildLayer
        {
            get { return gView.Framework.system.ListOperations<ILayer>.Clone(_childLayers); }
        }

        public MapServerGrouplayerStyle MapServerStyle { get; set; }

        #endregion

        public void Add(Layer layer)
        {
            if (layer == null || _childLayers.Contains(layer))
            {
                return;
            }

            _childLayers.Add(layer);
            layer.GroupLayer = this;
        }
        public void Remove(Layer layer)
        {
            if (layer == null || !_childLayers.Contains(layer))
            {
                return;
            }

            _childLayers.Remove(layer);
            layer.GroupLayer = null;
        }

        #region IPersitable

        public override void Load(IPersistStream stream)
        {
            base.Load(stream);

            this.MapServerStyle = (MapServerGrouplayerStyle)(int)stream.Load("MapServerStyle", (int)MapServerGrouplayerStyle.Dropdownable);
        }

        public override void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("MapServerStyle", (int)this.MapServerStyle);
        }

        #endregion
    }
    public class FieldCollection : IFieldCollection, IPersistable
    {
        protected List<IField> _fields = null;
        protected IField _primaryField = null;
        private object _privateLocker = new object();

        public FieldCollection()
        {
            _fields = new List<IField>();
        }
        public FieldCollection(IFieldCollection fields)
            : this(fields?.ToEnumerable())
        {
        }

        public FieldCollection(IEnumerable<IField> fields)
            : this()
        {
            if (fields != null)
            {
                foreach (IField field in fields)
                {
                    if (field == null)
                    {
                        continue;
                    }

                    _fields.Add(field);
                }
            }
        }

        public FieldCollection(DataTable schemaTable)
        {
            _fields = new List<IField>();
            if (schemaTable != null)
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    this.Add(new Field(row));
                }
            }
        }

        internal void CopyFrom(IFieldCollection fields, IClass Class)
        {
            if (fields != null)
            {
                _fields = new List<IField>();
                foreach (IField field in fields.ToEnumerable())
                {
                    if (Class is ITableClass && ((ITableClass)Class).Fields != null)
                    {
                        IField classField = ((ITableClass)Class).FindField(field.name);
                        if (classField != null)
                        {
                            Field f = new Field(classField);
                            f.visible = field.visible;
                            f.aliasname = field.aliasname;
                            f.IsEditable = field.IsEditable;
                            f.IsRequired = field.IsRequired;
                            f.DefautValue = field.DefautValue;
                            f.Domain = field.Domain;

                            if (field is Field)
                            {
                                f.Priority = ((Field)field).Priority;
                            }
                            _fields.Add(f);
                            if (fields.PrimaryDisplayField != null && f.name == fields.PrimaryDisplayField.name)
                            {
                                _primaryField = f;
                            }
                        }
                    }
                    else
                    {
                        _fields.Add(new Field(field));
                    }
                }
            }

            // Add new fields "invisible" to the layer
            if (Class is ITableClass && ((ITableClass)Class).Fields != null)
            {
                IFieldCollection classFields = ((ITableClass)Class).Fields;
                if (_fields == null)
                {
                    _fields = new List<IField>();
                }

                foreach (IField classField in classFields.ToEnumerable())
                {
                    bool found = false;

                    foreach (IField field in _fields)
                    {
                        if (field.name == classField.name)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Field f = new Field(classField);
                        f.visible = false;
                        _fields.Add(f);
                    }
                }
            }


            // Primary Field
            if (_primaryField != null && _fields != null)
            {
                bool found = false;
                foreach (IField field in _fields)
                {
                    if (field.name == _primaryField.name)
                    {
                        _primaryField = field;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _primaryField = null;
                }
            }
            if (_primaryField == null && _fields != null)
            {
                foreach (IField field in _fields)
                {
                    if (field.type == FieldType.String)
                    {
                        _primaryField = field;
                        break;
                    }
                }
            }
            if (_primaryField == null && _fields != null && _fields.Count > 0)
            {
                _primaryField = _fields[0];
            }
        }

        public IField[] ToArray() => _fields.ToArray();

        #region IFields Member
        public IField FindField(string aliasname)
        {
            if (_fields == null)
            {
                return null;
            }

            foreach (IField field in _fields)
            {
                if (field.aliasname == aliasname)
                {
                    return field;
                }
            }

            return null;
        }
        public IField PrimaryDisplayField
        {
            get
            {
                return _primaryField;
            }
            set
            {
                if (_fields == null || !_fields.Contains(value))
                {
                    return;
                }

                _primaryField = value;
            }
        }
        #endregion

        #region IPersistable
        public void Load(IPersistStream stream)
        {
            string primaryField = (string)stream.Load("PrimaryField", "");

            if (_fields == null)
            {
                _fields = new List<IField>();
            }

            IField field;
            while ((field = stream.Load("Field", null, new Field()) as IField) != null)
            {
                _fields.Add(field);
            }

            int priority = 0;
            foreach (IField fieldPriority in this.ToEnumerable())
            {
                if (fieldPriority is Field)
                {
                    ((Field)fieldPriority).Priority = priority++;
                }
            }

            _primaryField = null;
            foreach (IField f in _fields)
            {
                if (f.name == primaryField)
                {
                    _primaryField = f;
                }
            }
        }
        public void Save(IPersistStream stream)
        {
            if (_fields != null)
            {
                foreach (IField field in this.ToEnumerable())
                {
                    stream.Save("Field", field);
                }
            }

            if (_primaryField != null)
            {
                stream.Save("PrimaryField", _primaryField.name);
            }
        }
        #endregion

        #region IEnumerable<IField> Member

        //public IEnumerator<IField> GetEnumerator()
        //{
        //    return new FieldsEnumerator(_fields);
        //}

        #endregion

        #region IEnumerable Member

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    throw new Exception("The method or operation is not implemented.");
        //}

        #endregion

        public void Add(IField field)
        {
            if (field is Field && ((Field)field).Priority == -1)
            {
                ((Field)field).Priority = _fields.Count;
            }
            _fields.Add(field);
        }
        public void Insert(int index, IField field)
        {
            if (field is Field && ((Field)field).Priority == -1)
            {
                ((Field)field).Priority = _fields.Count;
            }
            _fields.Insert(index, field);
        }

        public void Clear()
        {
            _fields.Clear();
        }
        public void Remove(IField field)
        {
            _fields.Remove(field);
        }
        private class FieldsEnumerator : IEnumerator<IField>
        {
            List<IField> _fields;
            public FieldsEnumerator(List<IField> fields)
            {
                _fields = fields;
                _fields.Sort(new FieldPriorityComparer());
            }
            int index = 0;
            #region IEnumerator<IField> Member

            public IField Current
            {
                get { return (index > 0 && index <= _fields.Count) ? _fields[index - 1] : null; }
            }

            #endregion

            #region IDisposable Member

            public void Dispose()
            {

            }

            #endregion

            #region IEnumerator Member

            object IEnumerator.Current
            {
                get { return (index > 0 && index <= _fields.Count) ? _fields[index - 1] : null; }
            }

            public bool MoveNext()
            {
                if (_fields == null)
                {
                    return false;
                }

                index++;
                return index <= _fields.Count;
            }

            public void Reset()
            {
                index = 0;
            }

            #endregion
        }

        #region IClone Member

        public object Clone()
        {
            FieldCollection fields = new FieldCollection();

            foreach (IField field in this.ToEnumerable())
            {
                if (field == null)
                {
                    continue;
                }

                fields.Add(new Field(field));
            }

            return fields;
        }

        #endregion

        #region IFields Member

        public IField this[int i]
        {
            get
            {
                if (_fields == null || i < 0 || i >= _fields.Count)
                {
                    return null;
                }

                return _fields[i];
            }
        }

        public int Count
        {
            get
            {
                return (_fields != null) ? _fields.Count : 0;
            }
        }

        public IEnumerable<IField> ToEnumerable()
        {
            lock (_privateLocker)  // Threadsafe
            {
                return _fields.ToArray().OrderBy(f => f is IPriority ? ((IPriority)f).Priority : int.MaxValue);
            }
        }

        #endregion

        #region HelperClasses
        private class FieldPriorityComparer : IComparer<IField>
        {
            #region IComparer<IField> Member

            public int Compare(IField x, IField y)
            {
                if (x is IPriority && y is IPriority)
                {
                    if (((IPriority)x).Priority < ((IPriority)y).Priority)
                    {
                        return -1;
                    }
                    else if (((IPriority)x).Priority < ((IPriority)y).Priority)
                    {
                        return 1;
                    }
                }
                return 0;
            }

            #endregion
        }
        #endregion

        //public IEnumerable<IField> ToArray()
        //{
        //    //List<IField> array = new List<IField>(_fields);
        //    //return array;
        //    return ToEnumerable();
        //}
    }

    public class FeatureLayer : Layer, IFeatureLayer, IFeatureLayerComposition
    {
        protected IFeatureRenderer _renderer = null, _selectionrenderer = null;
        protected ILabelRenderer _labelRenderer = null;
        protected IQueryFilter _filterQuery = null;
        protected FieldCollection _fields = new FieldCollection();
        protected bool _applyRefScale = true, _applyLabelRefScale = true;
        protected FeatureLayerJoins _joins = null;
        protected GeometryType _geometryType = GeometryType.Unknown;

        public FeatureLayer()
        {
        }
        public FeatureLayer(IFeatureClass featureClass)
        {
            this.FeatureClass = featureClass;
            if (featureClass != null)
            {
                _title = _class.Name;

                //if (featureClass.Fields != null)
                //{
                //    _fields = new Fields();
                //    foreach (IField field in featureClass.Fields)
                //    {
                //        Field f = new Field(field);
                //        _fields.Add(f);
                //        if (_fields.PrimaryDisplayField == null && f.type == FieldType.String)
                //        {
                //            _fields.PrimaryDisplayField = f;
                //        }
                //    }
                //    if (_fields.PrimaryDisplayField == null)
                //    {
                //        foreach (IField field in _fields)
                //        {
                //            if (field.type != FieldType.Shape &&
                //                field.type != FieldType.binary &&
                //                field.type != FieldType.unknown)
                //            {
                //                _fields.PrimaryDisplayField = field;
                //                break;
                //            }
                //        }
                //    }
                //}
            }
        }

        public FeatureLayer(IFeatureLayer layer)
        {
            CopyFrom(layer);
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is IFeatureLayer)
            {
                IFeatureLayer layer = (IFeatureLayer)element;

                if (layer == null)
                {
                    return;
                }

                _renderer = layer.FeatureRenderer;
                _selectionrenderer = layer.SelectionRenderer;
                _labelRenderer = layer.LabelRenderer;
                _filterQuery = layer.FilterQuery;

                _applyRefScale = layer.ApplyRefScale;
                _applyLabelRefScale = layer.ApplyLabelRefScale;

                this.MaxRefScaleFactor = layer.MaxRefScaleFactor;
                this.MaxLabelRefScaleFactor = layer.MaxLabelRefScaleFactor;

                _fields.CopyFrom(layer.Fields, this.Class);
                _geometryType = layer.LayerGeometryType;

                if (layer.Joins != null)
                {
                    this.Joins = (FeatureLayerJoins)layer.Joins.Clone();
                }

                if (layer is IFeatureLayerComposition)
                {
                    this.CompositionMode = ((IFeatureLayerComposition)layer).CompositionMode;
                    this.CompositionModeCopyTransparency = ((IFeatureLayerComposition)layer).CompositionModeCopyTransparency;
                }
            }
        }

        private void RefreshFields()
        {
            FieldCollection newFields = new FieldCollection();
            if (_class is ITableClass)
            {
                foreach (IField field in ((ITableClass)_class).Fields?.ToEnumerable() ?? Array.Empty<IField>())
                {
                    Field f = new Field(field);
                    newFields.Add(f);
                    if (newFields.PrimaryDisplayField == null && f.type == FieldType.String)
                    {
                        newFields.PrimaryDisplayField = f;
                    }
                }
                if (newFields.PrimaryDisplayField == null && _fields != null)
                {
                    foreach (IField field in _fields.ToEnumerable())
                    {
                        if (field.type != FieldType.Shape &&
                            field.type != FieldType.binary &&
                            field.type != FieldType.unknown)
                        {
                            newFields.PrimaryDisplayField = field;
                            break;
                        }
                    }
                }

                if (_fields != null)
                {
                    foreach (IField field in _fields.ToEnumerable())
                    {
                        IField f = newFields.FindField(field.name);
                        if (f != null)
                        {
                            ((Field)f).aliasname = field.aliasname;
                            ((Field)f).IsEditable = field.IsEditable;
                            ((Field)f).IsRequired = field.IsRequired;
                            ((Field)f).DefautValue = field.DefautValue;
                            ((Field)f).Domain = field.Domain;
                            f.visible = field.visible;
                        }
                    }

                    if (_fields.PrimaryDisplayField != null)
                    {
                        IField f = newFields.FindField(_fields.PrimaryDisplayField.name);
                        if (f != null)
                        {
                            newFields.PrimaryDisplayField = f;
                        }
                    }
                }
            }
            _fields = newFields;
        }

        protected override void Refresh()
        {
            this.FeatureClass = (IFeatureClass)_class; // Refresh WrappedClass!
        }

        #region IFeatureLayer Member

        public IFeatureRenderer FeatureRenderer
        {
            get { return _renderer; }
            set { _renderer = value; }
        }
        public IFeatureRenderer SelectionRenderer
        {
            get { return _selectionrenderer; }
            set { _selectionrenderer = value; }
        }
        public ILabelRenderer LabelRenderer
        {
            get { return _labelRenderer; }
            set { _labelRenderer = value; }
        }
        virtual public IFeatureClass FeatureClass
        {
            get { return _class as IFeatureClass; }
            private set
            {
                _class = value as IFeatureClass;
                if (_class == null)
                {
                    return;
                }

                if (_joins != null)
                {
                    if (_class is WrappedFeatureClassWithJoins)
                    {
                        ((WrappedFeatureClassWithJoins)_class).Joins = _joins;
                    }
                    else
                    {
                        _class = new WrappedFeatureClassWithJoins((IFeatureClass)_class, _joins);
                    }
                }
                else
                {
                    if (_class is WrappedFeatureClassWithJoins)
                    {
                        _class = ((WrappedFeatureClassWithJoins)_class).WrappedFeatureclass;
                    }
                }

                if (_class is IFeatureClass &&
                    ((IFeatureClass)_class).GeometryType != GeometryType.Unknown)
                {
                    _geometryType = GeometryType.Unknown;
                }

                RefreshFields();
            }
        }
        public IQueryFilter FilterQuery
        {
            get { return _filterQuery; }
            set { _filterQuery = value; }
        }

        public bool ApplyRefScale
        {
            get
            {
                return _applyRefScale;
            }
            set
            {
                _applyRefScale = value;
            }
        }
        public bool ApplyLabelRefScale
        {
            get
            {
                return _applyLabelRefScale;
            }
            set
            {
                _applyLabelRefScale = value;
            }
        }

        public float MaxRefScaleFactor { get; set; }
        public float MaxLabelRefScaleFactor { get; set; }

        public IFieldCollection Fields
        {
            get { return _fields; }
        }

        public FeatureLayerJoins Joins
        {
            get { return _joins; }
            set
            {
                _joins = value;
                if (_joins != null)
                {
                    this.FeatureClass = (IFeatureClass)_class; // Refresh WrappedClass!
                }
            }
        }

        public GeometryType LayerGeometryType
        {
            get
            {
                if (this.FeatureClass != null && this.FeatureClass.GeometryType != GeometryType.Unknown)
                {
                    return this.FeatureClass.GeometryType;
                }

                return _geometryType;
            }
            set { _geometryType = value; }
        }

        #endregion

        #region IFeatureLayerComposition

        public FeatureLayerCompositionMode CompositionMode { get; set; }
        public float CompositionModeCopyTransparency { get; set; }

        #endregion

        #region IPersistable Member

        override public void Load(IPersistStream stream)
        {
            base.Load(stream);

            //_featureclass=new FeatureClass();
            //((FeatureClass)_featureclass).Name=(string)stream.Load("Featureclass","");

            _renderer = (IFeatureRenderer)stream.Load("IRenderer");
            _labelRenderer = (ILabelRenderer)stream.Load("ILabelRenderer");
            _selectionrenderer = (IFeatureRenderer)stream.Load("ISelectionRenderer");
            _fields = stream.Load("IFields", new FieldCollection(), new FieldCollection()) as FieldCollection;

            _applyRefScale = (bool)stream.Load("applyRefScale", true);
            _applyLabelRefScale = (bool)stream.Load("applyLRefScale", true);

            this.MaxRefScaleFactor = (float)stream.Load("maxRefScaleFactor", 0f);
            this.MaxLabelRefScaleFactor = (float)stream.Load("maxLabelRefScaleFactor", 0f);

            _geometryType = (GeometryType)stream.Load("geomType", (int)GeometryType.Unknown);

            string filterQuery = (string)stream.Load("FilterQuery", "");
            if (filterQuery != null)
            {
                QueryFilter filter = new QueryFilter();
                filter.WhereClause = filterQuery;
                this.FilterQuery = filter;
            }

            this.Joins = (FeatureLayerJoins)stream.Load("Joins", null, new FeatureLayerJoins());

            if (this is IFeatureLayerComposition)
            {
                var flComposition = (IFeatureLayerComposition)this;
                flComposition.CompositionMode = (FeatureLayerCompositionMode)(int)stream.Load("composition_mode", (int)FeatureLayerCompositionMode.Over);
                flComposition.CompositionModeCopyTransparency = (float)stream.Load("compostion_copy_transp", 1f);
            }
        }

        override public void Save(IPersistStream stream)
        {
            base.Save(stream);

            //stream.Save("Featureclass", FeatureClass != null ? FeatureClass.Name : _title);

            if (_renderer != null)
            {
                stream.Save("IRenderer", _renderer);
            }

            if (_labelRenderer != null)
            {
                stream.Save("ILabelRenderer", _labelRenderer);
            }

            if (_selectionrenderer != null)
            {
                stream.Save("ISelectionRenderer", _selectionrenderer);
            }

            if (_fields != null)
            {
                stream.Save("IFields", _fields);
            }

            if (_applyRefScale == false)
            {
                stream.Save("applyRefScale", _applyRefScale);
            }

            if (_applyLabelRefScale == false)
            {
                stream.Save("applyLRefScale", _applyLabelRefScale);
            }

            if (this.MaxRefScaleFactor > 0D)
            {
                stream.Save("maxRefScaleFactor", this.MaxRefScaleFactor);
            }

            if (this.MaxLabelRefScaleFactor > 0D)
            {
                stream.Save("maxLabelRefScaleFactor", this.MaxLabelRefScaleFactor);
            }

            if (_geometryType != GeometryType.Unknown)
            {
                stream.Save("geomType", (int)_geometryType);
            }

            if (this.FilterQuery != null)
            {
                if (!String.IsNullOrEmpty(this.FilterQuery.JsonWhereClause))
                {
                    stream.Save("FilterQuery", this.FilterQuery.JsonWhereClause);
                }
                else
                {
                    stream.Save("FilterQuery", this.FilterQuery.WhereClause);
                }
            }

            if (_joins != null)
            {
                stream.Save("Joins", _joins);
            }

            if (this is IFeatureLayerComposition)
            {
                var flComposition = (IFeatureLayerComposition)this;
                stream.Save("composition_mode", (int)flComposition.CompositionMode);
                stream.Save("compostion_copy_transp", flComposition.CompositionModeCopyTransparency);
            }
        }

        #endregion
    }

    public class FeatureSelection : FeatureLayer, IFeatureSelection
    {
        ISelectionSet _selectionSet = new IDSelectionSet();

        public FeatureSelection() { }
        public FeatureSelection(IFeatureClass featureClass)
            : base(featureClass)
        {
        }
        public FeatureSelection(IFeatureLayer layer)
            : base(layer)
        {
        }

        #region IFeatureSelection Member

        public event FeatureSelectionChangedEvent FeatureSelectionChanged;
        public event BeforeClearSelectionEvent BeforeClearSelection;

        public ISelectionSet SelectionSet
        {
            get
            {
                return _selectionSet;
            }
            set
            {
                if (_selectionSet != null && _selectionSet is IDSelectionSet)
                {
                    ((IDSelectionSet)_selectionSet).Dispose();
                }

                _selectionSet = value;
            }
        }

        async public Task<bool> Select(IQueryFilter filter, CombinationMethod methode)
        {
            if (this.FeatureClass != null)
            {
                if (this.FilterQuery != null && !String.IsNullOrEmpty(this.FilterQuery.WhereClause))
                {
                    filter.WhereClause = String.IsNullOrEmpty(filter.WhereClause) ? this.FilterQuery.WhereClause : filter.WhereClause + " AND " + this.FilterQuery.WhereClause;
                }
                ISelectionSet selSet = await this.FeatureClass.Select(filter);

                if (methode == CombinationMethod.New)
                {
                    _selectionSet = selSet;
                }
                else
                {
                    if (_selectionSet == null)
                    {
                        if (selSet is SpatialIndexedIDSelectionSet)
                        {
                            _selectionSet = new SpatialIndexedIDSelectionSet(
                                ((SpatialIndexedIDSelectionSet)_selectionSet).Bounds);
                        }
                        else
                        {
                            _selectionSet = new IDSelectionSet();
                        }
                    }

                    _selectionSet.Combine(selSet, methode);
                }

                return true;
            }
            return false;
        }

        public void ClearSelection()
        {
            if (_selectionSet != null)
            {
                BeforeClearSelection?.Invoke(this);

                _selectionSet.Clear();
            }
        }

        public void FireSelectionChangedEvent()
        {
            FeatureSelectionChanged?.Invoke(this);
        }

        #endregion
    }

    public class FeatureLayer2 : FeatureSelection
    {
        public FeatureLayer2()
        {
        }
        public FeatureLayer2(IFeatureClass featureClass)
            : base(featureClass)
        {
        }
        public FeatureLayer2(IFeatureLayer layer)
            : base(layer)
        {
        }
    }


    public class RasterLayer : Layer, IRasterLayer
    {
        private InterpolationMethod _interpolMethod = InterpolationMethod.Fast;
        private float _transparency = 0.0f;
        private GraphicsEngine.ArgbColor _transColor = GraphicsEngine.ArgbColor.Transparent;

        public RasterLayer()
        {
            this.FilterImplementation = FilterImplementations.Default;
        }
        public RasterLayer(IRasterClass rasterClass)
            : this()
        {
            _class = rasterClass;
            if (rasterClass != null)
            {
                _title = rasterClass.Name;
            }
        }
        public RasterLayer(IRasterLayer layer)
            : this()
        {
            CopyFrom(layer);
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is IRasterLayer)
            {
                IRasterLayer layer = (IRasterLayer)element;

                _interpolMethod = layer.InterpolationMethod;
                _transparency = layer.Opacity;
                _transColor = layer.TransparentColor;
                FilterImplementation = layer.FilterImplementation;
            }
        }
        #region IRasterLayer Member

        public InterpolationMethod InterpolationMethod
        {
            get
            {
                return _interpolMethod;
            }
            set
            {
                _interpolMethod = value;
            }
        }

        public float Opacity
        {
            get
            {
                return _transparency;
            }
            set
            {
                _transparency = value;
            }
        }

        public GraphicsEngine.ArgbColor TransparentColor
        {
            get
            {
                return _transColor;
            }
            set
            {
                _transColor = value;
            }
        }

        public FilterImplementations FilterImplementation { get; set; }

        public IRasterClass RasterClass
        {
            get { return _class as IRasterClass; }
        }

        #endregion

        public void SetRasterClass(IRasterClass rc)
        {
            _class = rc;
        }

        #region IPersistable Member

        public string PersistID
        {
            get
            {
                return null;
            }
        }

        override public void Load(IPersistStream stream)
        {
            base.Load(stream);

            _interpolMethod = (InterpolationMethod)stream.Load("interpolation", (int)InterpolationMethod.Fast);
            _transparency = (float)stream.Load("transparency", 0f);
            _transColor = GraphicsEngine.ArgbColor.FromArgb((int)stream.Load("transcolor", GraphicsEngine.ArgbColor.Transparent.ToArgb()));
            FilterImplementation = (FilterImplementations)stream.Load("filter", (int)FilterImplementations.Default);
        }

        override public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("interpolation", (int)_interpolMethod);
            stream.Save("transparency", _transparency);
            stream.Save("transcolor", _transColor.ToArgb());
            stream.Save("filter", (int)FilterImplementation);
        }

        #endregion
    }

    public class RasterCatalogLayer : FeatureLayer2, IRasterCatalogLayer
    {
        private InterpolationMethod _interpolMethod = InterpolationMethod.Fast;
        private float _transparency = 0.0f;
        private GraphicsEngine.ArgbColor _transColor = GraphicsEngine.ArgbColor.Transparent;
        private FilterImplementations _filter = FilterImplementations.Default;

        public RasterCatalogLayer() { }
        public RasterCatalogLayer(IRasterCatalogClass rasterClass) :
            base(rasterClass as IFeatureClass)
        {
            _class = rasterClass;
            if (rasterClass != null)
            {
                _title = rasterClass.Name;
            }
        }
        public RasterCatalogLayer(IRasterCatalogLayer layer)
            : base(layer as IFeatureLayer)
        {
            CopyFrom(layer);
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is IRasterLayer)
            {
                IRasterLayer layer = (IRasterLayer)element;

                _interpolMethod = layer.InterpolationMethod;
                _transparency = layer.Opacity;
                _transColor = layer.TransparentColor;
            }
        }

        #region IRasterLayer Member

        public InterpolationMethod InterpolationMethod
        {
            get
            {
                return _interpolMethod;
            }
            set
            {
                _interpolMethod = value;
            }
        }

        public float Opacity
        {
            get
            {
                return _transparency;
            }
            set
            {
                _transparency = value;
            }
        }

        public GraphicsEngine.ArgbColor TransparentColor
        {
            get
            {
                return _transColor;
            }
            set
            {
                _transColor = value;
            }
        }

        public GraphicsEngine.Filters.FilterImplementations FilterImplementation
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
            }
        }

        public IRasterClass RasterClass
        {
            get { return _class as IRasterClass; }
        }

        #endregion

        #region IPersistable Member

        public string PersistID
        {
            get
            {
                return null;
            }
        }

        override public void Load(IPersistStream stream)
        {
            base.Load(stream);

            _interpolMethod = (InterpolationMethod)stream.Load("interpolation", (int)InterpolationMethod.Fast);
            _transparency = (float)stream.Load("transparency", 0f);
            _transColor = GraphicsEngine.ArgbColor.FromArgb((int)stream.Load("transcolor", GraphicsEngine.ArgbColor.Transparent.ToArgb()));
            _filter = (FilterImplementations)stream.Load("filter", (int)FilterImplementations.Default);
        }

        override public void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("interpolation", (int)_interpolMethod);
            stream.Save("transparency", _transparency);
            stream.Save("transcolor", _transColor.ToArgb());
            stream.Save("filter", (int)_filter);
        }

        #endregion
    }

    public class WebServiceLayer : Layer, IWebServiceLayer
    {
        private List<IWebServiceTheme> _themes = null;

        public WebServiceLayer() { }
        public WebServiceLayer(IWebServiceClass webClass)
        {
            _class = webClass;
            if (webClass != null)
            {
                _title = webClass.Name;
            }
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is WebServiceLayer)
            {

            }
        }
        public override int DatasetID
        {
            get
            {
                return base.DatasetID;
            }
            set
            {
                base.DatasetID = value;

                if (WebServiceClass != null)
                {
                    foreach (IWebServiceTheme theme in WebServiceClass.Themes)
                    {
                        theme.DatasetID = value;
                    }
                }
            }
        }

        #region IWebServiceLayer Member

        public IWebServiceClass WebServiceClass
        {
            get { return _class as IWebServiceClass; }
        }

        #endregion

        public void SetWebServiceClass(IWebServiceClass wc)
        {
            _class = wc;
            SerializeThemes();
        }

        private void SerializeThemes()
        {
            if (!(_class is IWebServiceClass) || _themes == null)
            {
                return;
            }

            if (((IWebServiceClass)_class).Themes == null)
            {
                return;
            }

            foreach (IWebServiceTheme theme in ((IWebServiceClass)_class).Themes)
            {
                foreach (IWebServiceTheme t in _themes)
                {
                    if (t.LayerID == theme.LayerID)
                    {
                        theme.Locked = t.Locked;
                        theme.Title = t.Title;
                        theme.MinimumScale = t.MinimumScale;
                        theme.MaximumScale = t.MaximumScale;
                        theme.Visible = t.Visible;
                        theme.ID = t.ID;
                        theme.MinimumLabelScale = t.MinimumLabelScale;
                        theme.MaximumLabelScale = t.MaximumLabelScale;
                        theme.MaximumZoomToFeatureScale = t.MaximumZoomToFeatureScale;
                        theme.FeatureRenderer = t.FeatureRenderer;
                        theme.LabelRenderer = t.LabelRenderer;
                        theme.SelectionRenderer = t.SelectionRenderer;
                        theme.FilterQuery = t.FilterQuery;

                        // DatasetID wird in TOCCoClass.InsertLayer zugewiesen!!!
                        break;
                    }
                }
            }
            _themes = null;
        }
        public override void Load(IPersistStream stream)
        {
            base.Load(stream);

            WebServiceTheme theme;
            while ((theme = (WebServiceTheme)stream.Load("IWebServiceTheme", null, new WebServiceTheme())) != null)
            {
                if (_themes == null)
                {
                    _themes = new List<IWebServiceTheme>();
                }

                _themes.Add(theme);
            }

            SerializeThemes();
        }

        public override void Save(IPersistStream stream)
        {
            base.Save(stream);

            if (_class is IWebServiceClass && ((IWebServiceClass)_class).Themes != null)
            {
                foreach (IWebServiceTheme theme in ((IWebServiceClass)_class).Themes)
                {
                    if (theme is IPersistable)
                    {
                        stream.Save("IWebServiceTheme", theme);
                    }
                }
            }
        }
    }

    public class WebServiceTheme : FeatureLayer, IWebServiceTheme
    {
        private string _id;
        private bool _locked;
        private IWebServiceClass _serviceClass;

        public WebServiceTheme() { }
        public WebServiceTheme(IClass Class, string name, string id, bool visible, IWebServiceClass serviceClass) :
            base(Class as IFeatureClass)
        {
            _class = Class;
            _serviceClass = serviceClass;
            _title = name;
            _id = id;
            _visible = visible;

            _locked = false;
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is IWebServiceTheme)
            {
                IWebServiceTheme theme = (IWebServiceTheme)element;

                _locked = theme.Locked;
                _id = theme.LayerID;
            }
        }

        #region IWebServiceTheme Member

        public string LayerID
        {
            get { return _id; }
        }

        //public bool Visible
        //{
        //    get
        //    {
        //        return _visible;
        //    }
        //    set
        //    {
        //        _visible = value;
        //    }
        //}

        public bool Locked
        {
            get
            {
                return _locked;
            }
            set
            {
                _locked = value;
            }
        }

        public IWebServiceClass ServiceClass
        {
            get { return _serviceClass; }
            set { _serviceClass = value; }
        }

        #endregion

        #region IPersistable Member

        public override void Load(IPersistStream stream)
        {
            base.Load(stream);

            _id = (string)stream.Load("id", "");
            _locked = (bool)stream.Load("locked", false);
        }

        public override void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("id", _id);
            stream.Save("locked", _locked);
        }

        #endregion
    }

    public class WebServiceTheme2 : FeatureSelection, IWebServiceTheme
    {
        private string _id;
        private bool _locked;
        private IWebServiceClass _serviceClass;

        public WebServiceTheme2() { }
        public WebServiceTheme2(IClass Class, string name, string id, bool visible, IWebServiceClass serviceClass) :
            base(Class as IFeatureClass)
        {
            _class = Class;
            _serviceClass = serviceClass;
            _title = name;
            _id = id;
            _visible = visible;

            _locked = false;
        }

        internal override void CopyFrom(IDatasetElement element)
        {
            base.CopyFrom(element);

            if (element is IWebServiceTheme)
            {
                IWebServiceTheme theme = (IWebServiceTheme)element;

                _locked = theme.Locked;
                _id = theme.LayerID;
            }
        }

        #region IWebServiceTheme Member

        public string LayerID
        {
            get { return _id; }
        }

        //public bool Visible
        //{
        //    get
        //    {
        //        return _visible;
        //    }
        //    set
        //    {
        //        _visible = value;
        //    }
        //}

        public bool Locked
        {
            get
            {
                return _locked;
            }
            set
            {
                _locked = value;
            }
        }

        public IWebServiceClass ServiceClass
        {
            get { return _serviceClass; }
            set { _serviceClass = value; }
        }

        #endregion

        #region IPersistable Member

        public override void Load(IPersistStream stream)
        {
            base.Load(stream);

            _id = (string)stream.Load("id", "");
            _locked = (bool)stream.Load("locked", false);
        }

        public override void Save(IPersistStream stream)
        {
            base.Save(stream);

            stream.Save("id", _id);
            stream.Save("locked", _locked);
        }

        #endregion
    }

    public class FeatureClass : IFeatureClass
    {
        private string _name = String.Empty, _idFieldName = String.Empty, _shapeFieldName = String.Empty;
        private IFieldCollection _fields = new FieldCollection();
        private IEnvelope _envelope = null;

        #region IFeatureClass Member

        virtual public Task<IFeatureCursor> GetFeatures(IQueryFilter filter/*, gView.Framework.Data.getFeatureQueryType type*/)
        {
            return Task.FromResult<IFeatureCursor>(null);
        }

        public Task<int> CountFeatures()
        {
            return Task.FromResult(0);
        }

        public string ShapeFieldName
        {
            get
            {
                return _shapeFieldName;
            }
            set { _shapeFieldName = value; }
        }

        public gView.Framework.Geometry.IEnvelope Envelope
        {
            get
            {
                return _envelope;
            }
            set { _envelope = value; }
        }

        #endregion

        #region ITableClass Member

        public string IDFieldName
        {
            get
            {
                return _idFieldName;
            }
            set { _idFieldName = value; }
        }

        public string Aliasname
        {
            get
            {
                return _name;
            }
        }

        public IField FindField(string name)
        {
            foreach (IField field in _fields.ToEnumerable())
            {
                if (field.name == name)
                {
                    return field;
                }
            }
            return null;
        }

        virtual public Task<ICursor> Search(IQueryFilter filter)
        {
            return Task.FromResult<ICursor>(null);
        }

        virtual public Task<ISelectionSet> Select(IQueryFilter filter)
        {
            return Task.FromResult<ISelectionSet>(null);
        }

        public IFieldCollection Fields
        {
            get
            {
                return _fields;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public virtual IDataset Dataset
        {
            get { return null; }
        }
        #endregion

        #region IGeometryDef Member

        private bool _hasZ = false, _hasM = false;
        private GeometryType _geomType = GeometryType.Unknown;
        private ISpatialReference _sRef = null;
        private GeometryFieldType _geometryFieldType = GeometryFieldType.Default;

        public bool HasZ
        {
            get { return _hasZ; }
            set { _hasZ = value; }
        }

        public bool HasM
        {
            get { return _hasM; }
            set { _hasM = value; }
        }

        public gView.Framework.Geometry.GeometryType GeometryType
        {
            get { return _geomType; }
            set { _geomType = value; }
        }

        public gView.Framework.Geometry.ISpatialReference SpatialReference
        {
            get
            {
                return _sRef;
            }
            set
            {
                _sRef = value;
            }
        }

        public gView.Framework.Data.GeometryFieldType GeometryFieldType
        {
            get
            {
                return _geometryFieldType;
            }
            set
            {
                _geometryFieldType = value;
            }
        }
        #endregion
    }

    public abstract class FeatureCursor : IFeatureCursorSkills, IDiagnostics
    {
        private IGeometricTransformer _transformer = null;
        private bool _knowsFunctions = true;

        public FeatureCursor(ISpatialReference fcSRef, ISpatialReference toSRef)
        {
            if (fcSRef != null && !fcSRef.Equals(toSRef))
            {
                _transformer = GeometricTransformerFactory.Create();
                //_transformer.FromSpatialReference = fcSRef;
                //_transformer.ToSpatialReference = toSRef;
                _transformer.SetSpatialReferences(fcSRef, toSRef);
            }
        }

        protected void Transform(IFeature feature)
        {
            if (feature != null && _transformer != null && feature.Shape != null)
            {
                feature.Shape = _transformer.Transform2D(feature.Shape) as IGeometry;
            }
        }

        protected IFeature CloneIfTransform(IFeature feature)
        {
            if (feature != null && _transformer != null && feature.Shape != null)
            {
                Feature cloned = new Feature();

                foreach (var f in feature.Fields)
                {
                    cloned.Fields.Add(new FieldValue(f.Name, f.Value));
                }

                cloned.Shape = (IGeometry)feature.Shape.Clone();
                cloned.Shape = _transformer.Transform2D(cloned.Shape) as IGeometry;

                return cloned;
            }

            return feature;
        }

        protected ICancelTracker CancelTracker { get; set; }

        #region IFeatureCursor Member

        abstract public Task<IFeature> NextFeature();

        #endregion

        #region IDisposable Member

        virtual public void Dispose()
        {
            if (_transformer != null)
            {
                _transformer.Release();
            }
        }

        #endregion

        #region IFeatureCursorSkills Member

        public bool KnowsFunctions
        {
            get { return _knowsFunctions; }
            set { _knowsFunctions = false; }
        }

        #endregion

        #region DataTable Functions

        async public static Task<DataTable> ToDataTable(IFeatureCursor cursor)
        {
            DataTable tab = new DataTable();
            tab.Columns.Add("#SHAPE#", typeof(object));
            tab.Columns.Add("#OID#", typeof(int));

            if (cursor != null)
            {
                IFeature feature;
                while ((feature = await cursor.NextFeature()) != null)
                {
                    #region Columns
                    foreach (FieldValue fv in feature.Fields)
                    {
                        if (fv.Value == null || fv.Value == DBNull.Value)
                        {
                            continue;
                        }

                        if (tab.Columns[fv.Name] == null)
                        {
                            tab.Columns.Add(fv.Name, fv.Value.GetType());
                        }
                    }
                    #endregion

                    DataRow row = tab.NewRow();
                    foreach (FieldValue fv in feature.Fields)
                    {
                        row[fv.Name] = fv.Value;
                    }

                    row["#SHAPE#"] = feature.Shape;
                    row["#OID#"] = feature.OID;

                    tab.Rows.Add(row);
                }
            }

            return tab;
        }

        public class DataRowCursor : IFeatureCursor
        {
            private DataRow[] _rows;
            private int _pos = 0;

            public DataRowCursor(DataRow[] rows)
            {
                _rows = rows;
            }

            #region IFeatureCursor Member

            public Task<IFeature> NextFeature()
            {
                if (_rows == null || _pos >= _rows.Length)
                {
                    return null;
                }

                DataRow row = _rows[_pos++];
                DataTable tab = row.Table;
                Feature feature = new Feature();

                if (tab.Columns["#OID#"] != null)
                {
                    feature.OID = (int)row["#OID#"];
                }

                if (tab.Columns["#SHAPE#"] != null)
                {
                    feature.Shape = row["#SHAPE#"] as IGeometry;
                }

                foreach (DataColumn col in tab.Columns)
                {
                    if (col.ColumnName == "#OID#" || col.ColumnName == "#SHAPE#")
                    {
                        continue;
                    }

                    feature.Fields.Add(new FieldValue(col.ColumnName, row[col.ColumnName]));
                }

                return Task.FromResult<IFeature>(feature);
            }

            #endregion

            #region IDisposable Member

            public void Dispose()
            {

            }

            #endregion
        }

        #endregion

        #region IDiagnostics

        public DiagnosticParameters DiagnosticParameters { get; protected set; }

        #endregion
    }

    public class NullLayer : Layer
    {
        private int _id_ = -1, _datasetID_ = -1;
        private bool _IsWebTheme = false;
        private string _ID = String.Empty, _className = String.Empty;

        public NullLayer()
            : base()
        {
        }

        public int PersistLayerID
        {
            get { return _id_; }
            set { _id_ = value; }
        }
        public int PersistDatasetID
        {
            get { return _datasetID_; }
            set { _datasetID_ = value; }
        }
        public bool PersistIsWebTheme
        {
            get { return _IsWebTheme; }
            set { _IsWebTheme = value; }
        }
        public string PersistWebThemeID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        public string PersistClassName
        {
            get { return _className; }
            set { _className = value; }
        }
    }

    public class LayerFactory
    {
        private static bool UseSelectableElement(System.Type t)
        {
            bool selectionSet = true;
            foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(t))
            {
                if (attr is UseWithinSelectableDatasetElements)
                {
                    selectionSet = ((UseWithinSelectableDatasetElements)attr).Value;
                }
            }
            return selectionSet;
        }

        public static ILayer Create(IClass Class)
        {
            return Create(Class, true, null);
        }
        public static ILayer Create(IClass Class, IWebServiceClass serviceClass)
        {
            return Create(Class, true, serviceClass);
        }
        private static ILayer Create(IClass Class, bool initalize)
        {
            return Create(Class, initalize, null);
        }
        private static ILayer Create(IClass Class, bool initalize, IWebServiceClass serviceClass)
        {
            if (Class is IWebFeatureClass)
            {
                IWebServiceTheme theme;
                if (UseSelectableElement(Class.GetType()))
                {
                    theme = new WebServiceTheme2(Class, Class.Name, ((IWebFeatureClass)Class).ID, false, serviceClass);
                }
                else
                {
                    theme = new WebServiceTheme(Class, Class.Name, ((IWebFeatureClass)Class).ID, false, serviceClass);
                }

                if (initalize && theme.FeatureClass != null)
                {
                    if (theme.FeatureClass.GeometryType == GeometryType.Unknown)
                    {
                        theme.FeatureRenderer = null;
                        theme.LabelRenderer = null;
                        IFeatureRenderer renderer = PlugInManager.Create(KnownObjects.Carto_UniversalGeometryRenderer) as IFeatureRenderer;
                        theme.SelectionRenderer = renderer;
                    }
                    else
                    {
                        IFeatureRenderer2 renderer = PlugInManager.Create(KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer2;
                        if (renderer is ISymbolCreator && renderer.CanRender(theme, null))
                        {
                            theme.FeatureRenderer = null;
                            theme.LabelRenderer = null;
                            renderer.Symbol = ((ISymbolCreator)renderer).CreateStandardSelectionSymbol(theme.FeatureClass.GeometryType);
                            theme.SelectionRenderer = renderer;
                        }
                        else if (renderer != null)
                        {
                            renderer.Release();
                            renderer = null;
                        }
                    }
                }
                return theme;
            }
            else if (Class is IWebRasterClass)
            {
                IWebServiceTheme theme = new WebServiceTheme(Class, Class.Name, ((IWebRasterClass)Class).ID, false, serviceClass);
                return theme;
            }
            else if (Class is IWebServiceClass)
            {
                WebServiceLayer wsLayer = new WebServiceLayer(Class as IWebServiceClass);
                return wsLayer;
            }
            else if (Class is IRasterCatalogClass)
            {
                IRasterCatalogLayer layer = new RasterCatalogLayer(Class as IRasterCatalogClass);
                if (initalize)
                {
                    IFeatureRenderer2 renderer = PlugInManager.Create(KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer2;
                    if (renderer is ISymbolCreator && renderer.CanRender(layer, null))
                    {
                        //layer.FeatureRenderer = renderer;
                        //renderer.Symbol = ((ISymbolCreator)renderer).CreateStandardSymbol(layer.FeatureClass.GeometryType);

                        //IFeatureRenderer2 selectionRenderer = ComponentManager.Create(KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer2;
                        //selectionRenderer.Symbol = ((ISymbolCreator)selectionRenderer).CreateStandardSelectionSymbol(layer.FeatureClass.GeometryType);
                        //layer.SelectionRenderer = selectionRenderer;
                        renderer.Symbol = ((ISymbolCreator)renderer).CreateStandardSelectionSymbol(layer.FeatureClass.GeometryType);
                        layer.SelectionRenderer = renderer;
                    }
                    else if (renderer != null)
                    {
                        renderer.Release();
                        renderer = null;
                    }
                }

                return layer;
            }
            else if (Class is IFeatureClass)
            {
                IFeatureLayer layer;
                if (UseSelectableElement(Class.GetType()))
                {
                    layer = new FeatureLayer2(Class as IFeatureClass);
                }
                else
                {
                    layer = new FeatureLayer(Class as IFeatureClass);
                }

                if (initalize && layer.FeatureClass != null)
                {
                    if (layer.FeatureClass.GeometryType == GeometryType.Unknown)
                    {
                        layer.FeatureRenderer = PlugInManager.Create(KnownObjects.Carto_UniversalGeometryRenderer) as IFeatureRenderer; ;
                        layer.SelectionRenderer = PlugInManager.Create(KnownObjects.Carto_UniversalGeometryRenderer) as IFeatureRenderer;
                    }
                    else
                    {
                        IFeatureRenderer2 renderer = PlugInManager.Create(KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer2;
                        if (renderer is ISymbolCreator && renderer.CanRender(layer, null))
                        {
                            layer.FeatureRenderer = renderer;
                            renderer.Symbol = ((ISymbolCreator)renderer).CreateStandardSymbol(layer.LayerGeometryType/*layer.FeatureClass.GeometryType*/);

                            IFeatureRenderer2 selectionRenderer = PlugInManager.Create(KnownObjects.Carto_SimpleRenderer) as IFeatureRenderer2;
                            selectionRenderer.Symbol = ((ISymbolCreator)selectionRenderer).CreateStandardSelectionSymbol(layer.FeatureClass.GeometryType);
                            layer.SelectionRenderer = selectionRenderer;
                        }
                        else if (renderer != null)
                        {
                            renderer.Release();
                            renderer = null;

                            IFeatureRenderer uRenderer = PlugInManager.Create(KnownObjects.Carto_UniversalGeometryRenderer) as IFeatureRenderer;
                            layer.FeatureRenderer = uRenderer;
                        }
                    }
                }

                return layer;
            }
            else if (Class is IRasterClass)
            {
                RasterLayer layer = new RasterLayer(Class as IRasterClass);

                return layer;
            }

            return null;
        }

        public static ILayer Create(IClass Class, ILayer protoType)
        {
            return Create(Class, protoType, null);
        }
        public static ILayer Create(IClass Class, ILayer protoType, IWebServiceClass serviceClass)
        {
            if (protoType is GroupLayer && Class == null)
            {
                GroupLayer grLayer = new GroupLayer();
                grLayer.Title = protoType.Title;
                grLayer.CopyFrom(protoType);
                return grLayer;
            }

            ILayer layer = Create(Class, false, serviceClass);
            if (layer is DatasetElement)
            {
                ((DatasetElement)layer).CopyFrom(protoType);
                //((DatasetElement)layer).Class2 = Class;   
            }
            return layer;
        }
    }
}
