using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using System.Threading.Tasks;

namespace gView.DataSources.Fdb.MSSql
{
    internal class SqlFDBDatasetElement : gView.Framework.Data.DatasetElement, IFeatureSelection
    {
        private ISelectionSet m_selectionset;

        private SqlFDBDatasetElement() { }

        async static public Task<SqlFDBDatasetElement> Create(SqlFDB fdb, IDataset dataset, string name, GeometryDef geomDef)
        {
            var dsElement = new SqlFDBDatasetElement();

            if (geomDef.GeometryType == GeometryType.Network)
            {
                dsElement._class = await SqlFDBNetworkFeatureclass.Create(fdb, dataset, name, geomDef);
            }
            else
            {
                dsElement._class = await SqlFDBFeatureClass.Create(fdb, dataset, geomDef);
                ((SqlFDBFeatureClass)dsElement._class).Name =
                ((SqlFDBFeatureClass)dsElement._class).Aliasname = name;
            }
            dsElement.Title = name;

            return dsElement;
        }

        public SqlFDBDatasetElement(LinkedFeatureClass fc)
        {
            _class = fc;
            this.Title = fc.Name;
        }

        #region IFeatureSelection Member

        public ISelectionSet SelectionSet
        {
            get
            {
                return (ISelectionSet)m_selectionset;
            }
            set
            {
                if (m_selectionset != null && m_selectionset != value)
                {
                    m_selectionset.Clear();
                }

                m_selectionset = value;
            }
        }

        async public Task<bool> Select(IQueryFilter filter, gView.Framework.Data.CombinationMethod methode)
        {
            if (!(this.Class is ITableClass))
            {
                return false;
            }

            ISelectionSet selSet = await ((ITableClass)this.Class).Select(filter);

            SelectionSet = selSet;
            FireSelectionChangedEvent();

            return true;
            /*
            if(this.type!=LayerType.featureclass) return false;
            if(_fdb==null) return false;

            filter.AddField(this.ShapeFieldName);

            SqlFDBFeatureClass fc=new SqlFDBFeatureClass(this,_fdb);

            IQueryResult selection=fc.Search(filter,true);
            while(filter.HasMore) 
            {
                selection=fc.Search(filter,true);
            }
            switch(methode) 
            {
                case CombinationMethode.New:
                    this.ClearSelection();
                    m_selectionset=(QueryResult)selection;
                    break;
            }
            FireSelectionChangedEvent();

            return true;
            */
        }

        public event FeatureSelectionChangedEvent FeatureSelectionChanged;
        public event BeforeClearSelectionEvent BeforeClearSelection; // { add { throw new NotSupportedException(); } remove { } }

        public void ClearSelection()
        {
            if (m_selectionset != null)
            {
                BeforeClearSelection?.Invoke(this);

                m_selectionset.Clear();
                m_selectionset = null;
                FireSelectionChangedEvent();
            }
        }

        public void FireSelectionChangedEvent()
        {
            FeatureSelectionChanged?.Invoke(this);
        }

        #endregion
    }
}
