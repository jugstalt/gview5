using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Framework.XML
{
    public class AXLFeatureClass : IWebFeatureClass, IBeforeQueryEventHandler
    {
        private const int _maxResultConst = 200;
        protected IDataset _dataset;
        private string _name = "", _aliasname = "";
        private string _idfield = "", _shapeField = "", _id = "";
        private FieldCollection _fields;
        private IEnvelope _envelope = null;
        private GeometryDef _geomDef = new GeometryDef();

        public AXLFeatureClass(IDataset dataset, string id)
        {
            _dataset = dataset;
            _fields = new FieldCollection();
            _id = id;
        }

        #region IFeatureClass Member

        /*
		public IQueryResult QueryResult 
		{
			get { return m_result; }
			set { m_result=value; }
		}

		public IQueryResult Search(IQueryFilter filter, bool queryGeometry)
		{
			if(filter is ISpatialFilter)
				return SpatialQuery((ISpatialFilter)filter,queryGeometry);
			else
				return Query(filter,queryGeometry);
		}
		*/

        public string Name { get { return _name; } set { _name = value; } }
        public string Aliasname { get { return _aliasname; } set { _name = value; } }

        public Task<int> CountFeatures()
        {
            return Task.FromResult(-1);
        }

        //public ArrayList SpatialIndexNodes { get { return null; } }

        async public Task<ICursor> Search(IQueryFilter filter)
        {
            filter = await WrapFilter2ArcXML(filter);

            if (BeforeQuery != null)
            {
                BeforeQuery(this, ref filter);
            }

            return new AXLFeatureCursor(this, filter);
        }

        async public Task<ISelectionSet> Select(IQueryFilter filter)
        {
            if (BeforeQuery != null)
            {
                BeforeQuery(this, ref filter);
            }

            if (this.IDFieldName != String.Empty && this.FindField(this.IDFieldName) != null)
            {
                filter.SubFields = this.IDFieldName;

                IFeatureCursor cursor = (IFeatureCursor)await this.Search(filter);
                IFeature feat;

                GlobalIDSelectionSet selSet = new GlobalIDSelectionSet();
                while ((feat = await cursor.NextFeature()) != null)
                {
                    if (feat is IGlobalFeature)
                    {
                        selSet.AddID(((IGlobalFeature)feat).GlobalOID);
                    }
                    else
                    {
                        selSet.AddID(feat.OID);
                    }
                }
                return selSet;
            }
            else
            {
                return await QueryFilteredSelectionSet.CreateAsync(this, filter);
            }
        }

        /*
        public IFeature GetFeature(int id, getFeatureQueryType type)
        {
            string sql = this.IDFieldName + "=" + id.ToString();

            QueryFilter filter = new QueryFilter();
            filter.WhereClause = sql;

            switch (type)
            {
                case getFeatureQueryType.All:
                case getFeatureQueryType.Attributes:
                    filter.SubFields = "#ALL#";
                    break;
                case getFeatureQueryType.Geometry:
                    filter.SubFields = this.ShapeFieldName;
                    break;
            }

            _maxResult = 1;
            string resp = this.AXLQuery(filter, type != getFeatureQueryType.Attributes);
            _maxResult = _maxResultConst;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                return AXLFeatureClass.ConvertAXLNode2Feature(
                    doc.SelectSingleNode("//FEATURE"),
                    this);
            }
            catch
            {
                return null;
            }
        }

        public IFeatureCursor GetFeatures(List<int> ids, getFeatureQueryType type)
        {
            StringBuilder sql = new StringBuilder();
            int count = 0;
            foreach (object id in ids)
            {
                if (count != 0) sql.Append(" OR ");
                sql.Append(this.IDFieldName + "=" + id.ToString());
                count++;
            }

            QueryFilter filter = new QueryFilter();
            filter.WhereClause = sql.ToString();

            switch (type)
            {
                case getFeatureQueryType.All:
                case getFeatureQueryType.Attributes:
                    filter.SubFields = "#ALL#";
                    break;
                case getFeatureQueryType.Geometry:
                    filter.SubFields = this.ShapeFieldName;
                    break;
            }

            _maxResult = count;
            string resp = this.AXLQuery(filter, type != getFeatureQueryType.Attributes);
            _maxResult = _maxResultConst;

            return new AXLFeatureCursor(resp, this);
        }
        */

        async public Task<IFeatureCursor> GetFeatures(IQueryFilter filter/*, getFeatureQueryType type*/)
        {
            filter = await WrapFilter2ArcXML(filter);

            if (BeforeQuery != null)
            {
                BeforeQuery(this, ref filter);
            }

            return new AXLFeatureCursor(this, filter);
        }

        public IGeometryDef GeometryDef
        {
            get { return _geomDef; }
        }

        public IFieldCollection Fields
        {
            get
            {
                return _fields;
            }
        }

        public IField FindField(string name)
        {
            if (_fields == null)
            {
                return null;
            }

            foreach (IField field in _fields.ToEnumerable())
            {
                if (field.name == name)
                {
                    return field;
                }
            }
            return null;
        }

        public string IDFieldName
        {
            get
            {
                return _idfield;
            }
            set { _idfield = value; }
        }
        public string ShapeFieldName
        {
            get { return _shapeField; }
            set { _shapeField = value; }
        }
        public IEnvelope Envelope
        {
            get { return _envelope; }
            set { _envelope = value; }
        }

        public IDataset Dataset
        {
            get { return _dataset; }
        }
        #endregion

        public string fClassTypeString
        {
            set
            {
                switch (value.ToLower())
                {
                    case "point":
                        _geomDef.GeometryType = GeometryType.Point;
                        break;
                    case "line":
                        _geomDef.GeometryType = GeometryType.Polyline;
                        break;
                    case "polygon":
                        _geomDef.GeometryType = GeometryType.Polygon;
                        break;
                    case "multipoint":
                        _geomDef.GeometryType = GeometryType.Multipoint;
                        break;
                    case "aggragate":
                        _geomDef.GeometryType = GeometryType.Aggregate;
                        break;
                    case "envelope":
                        _geomDef.GeometryType = GeometryType.Envelope;
                        break;
                    default:
                        _geomDef.GeometryType = GeometryType.Unknown;
                        break;
                }
            }
        }

        public string fieldsFromAXL
        {
            set
            {
                try
                {
                    _fields = new FieldCollection();
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml("<LAYERDEF>" + value + "</LAYERDEF>");

                    foreach (XmlNode field in doc.SelectNodes("//FIELD"))
                    {
                        if (field.Attributes["name"] == null)
                        {
                            continue;
                        }

                        Field f = new Field();
                        f.name = ArcXMLGeometry.shortName(field.Attributes["name"].Value);

                        if (field.Attributes["type"] != null)
                        {
                            switch (field.Attributes["type"].Value)
                            {
                                case "-99":
                                    this.IDFieldName = f.name;
                                    f.type = FieldType.ID;
                                    break;
                                case "-98":
                                    this.ShapeFieldName = f.name;
                                    f.type = FieldType.Shape;
                                    break;
                                case "-7":
                                    f.type = FieldType.boolean;
                                    break;
                                case "-5":
                                    f.type = FieldType.biginteger;
                                    break;
                                case "1":
                                    f.type = FieldType.character;
                                    break;
                                case "4":
                                    f.type = FieldType.integer;
                                    break;
                                case "5":
                                    f.type = FieldType.smallinteger;
                                    break;
                                case "6":
                                    f.type = FieldType.Float;
                                    break;
                                case "8":
                                    f.type = FieldType.Double;
                                    break;
                                case "12":
                                    f.type = FieldType.String;
                                    break;
                                case "91":
                                    f.type = FieldType.Date;
                                    break;
                            }
                        }

                        if (field.Attributes["precision"] != null)
                        {
                            f.precision = Convert.ToInt32(field.Attributes["precision"].Value);
                        }

                        if (field.Attributes["size"] != null)
                        {
                            f.size = Convert.ToInt32(field.Attributes["size"].Value);
                        }

                        _fields.Add(f);
                    }
                }
                catch
                {
                }
            }
        }

        static public IFeature ConvertAXLNode2Feature(XmlNode feature, IFeatureClass fc, IArcXmlGET_FEATURES_Attributes attributes)
        {
            ArcXMLFeature feat = new ArcXMLFeature();
            feat.ConvertAXL(feature, fc, attributes);
            return feat;
        }

        private IFeatureTable CreateQueryResult(IQueryFilter filter, string resp)
        {
            try
            {
                /*
                XmlDocument doc=new XmlDocument();
                doc.LoadXml(resp);

                QueryResult queryResult=
                    (m_result==null) ? new QueryResult() : (QueryResult)m_result;
                if(m_result==null) 
                {
                    queryResult.Filter=filter;
                }
                queryResult.IDFieldName=m_layer.IDFieldName;

                DataTable tab=queryResult.Table;

                XmlNode featureCount=doc.SelectSingleNode("//FEATURECOUNT");
                if(featureCount!=null) 
                {
                    ((QueryFilter)filter).LastQueryFeatureCount=
                        Convert.ToInt32(featureCount.Attributes["count"].Value);
                    ((QueryFilter)filter).HasMore=
                        Convert.ToBoolean(featureCount.Attributes["hasmore"].Value); 
                }
                foreach(XmlNode feature in doc.SelectNodes("//FEATURE")) 
                {
                    string sql="";
                    DataRow row=tab.NewRow();
                    bool cont=false;
                    foreach(XmlNode field in feature.SelectNodes("FIELDS/FIELD")) 
                    {
                        IField pField=m_layer.FindField(ArcXML.shortName(field.Attributes["name"].Value));
                        if(pField==null) continue;
                        if(tab.Columns[pField.aliasname]==null) 
                        {
                            switch(pField.type) 
                            {
                                case FieldType.biginteger:
                                case FieldType.ID:
                                case FieldType.integer:
                                case FieldType.smallinteger:
                                    tab.Columns.Add(pField.aliasname,typeof(int));
                                    break;
                                case FieldType.boolean:
                                    tab.Columns.Add(pField.aliasname,typeof(bool));
                                    break;
                                case FieldType.Double:
                                    tab.Columns.Add(pField.aliasname,typeof(double));
                                    break;
                                case FieldType.Float:
                                    tab.Columns.Add(pField.aliasname,typeof(float));
                                    break;
                                default:
                                    tab.Columns.Add(pField.aliasname,typeof(string));
                                    break;
                            }
                        }
				
                        row[pField.aliasname]=field.Attributes["value"].Value.ToString();

                        // Testen, ob Zeile schon vorhanden ist...
                        if(pField.name==m_layer.IDFieldName && m_result!=null) 
                        {
                            sql=pField.aliasname+"="+row[pField.aliasname].ToString();
                            if(m_result.Table.Select(sql).Length>0) 
                            {
                                cont=true;
                                break;
                            }
                        }
                    }
                    if(cont) continue;
				
                    queryResult.SetGeometry(row[m_layer.IDFieldName],ArcXML.AXL2Geometry(feature.InnerXml));
                    tab.Rows.Add(row);
                }
                return m_result=queryResult;
                */
                return null;
            }
            catch (Exception /*ex*/)
            {
                return null;
            }
        }

        async private Task<IQueryFilter> WrapFilter2ArcXML(IQueryFilter filter)
        {
            if (filter is ArcXMLQueryFilter ||
                filter is ArcXMLSpatialFilter)
            {
                return filter;
            }

            if (filter is ArcXMLBufferQueryFilter)
            {
                ArcXMLBufferQueryFilter bFilter = filter as ArcXMLBufferQueryFilter;
                filter = await BufferQueryFilter.ConvertToSpatialFilter(bFilter);
                if (filter == null)
                {
                    return null;
                }

                ArcXMLSpatialFilter aFilter = new ArcXMLSpatialFilter(filter as ISpatialFilter);
                aFilter.BeginRecord = bFilter.BeginRecord;
                aFilter.checkesc = bFilter.checkesc;
                aFilter.compact = bFilter.compact;
                aFilter.dataframe = bFilter.dataframe;
                aFilter.envelope = bFilter.envelope;
                aFilter.featurelimit = bFilter.featurelimit;
                aFilter.geometry = bFilter.geometry;
                aFilter.globalenvelope = bFilter.globalenvelope;
                aFilter.Properties = bFilter.Properties;
                aFilter.outputmode = OutputMode.newxml;

                return aFilter;
            }
            if (filter is ISpatialFilter)
            {
                ArcXMLSpatialFilter sFilter = new ArcXMLSpatialFilter(filter as ISpatialFilter);
                sFilter.outputmode = OutputMode.newxml;
                return sFilter;
            }
            if (filter is IBufferQueryFilter)
            {
                filter = await BufferQueryFilter.ConvertToSpatialFilter(filter as IBufferQueryFilter);
                if (filter == null)
                {
                    return null;
                }

                ArcXMLSpatialFilter sFilter = new ArcXMLSpatialFilter(filter as ISpatialFilter);
                sFilter.outputmode = OutputMode.newxml;
                return sFilter;
            }
            if (filter is IRowIDFilter)
            {
                return filter;
            }
            if (filter is IGlobalRowIDFilter)
            {
                return filter;
            }
            if (filter is IQueryFilter)
            {
                ArcXMLQueryFilter qFilter = new ArcXMLQueryFilter(filter as IQueryFilter);
                qFilter.outputmode = OutputMode.newxml;
                return qFilter;
            }
            return filter;
        }
        async private Task<IFeatureTable> Query(IQueryFilter filter)
        {
            return this.CreateQueryResult(filter, await AXLQuery(filter));
        }
        async private Task<string> AXLQuery(IQueryFilter filter)
        {
            AXLWriter axl = new AXLWriter();
            axl.WriteStartRequest("GET_FEATURES");
            //axl.WriteAttribute("featurelimit", _maxResult.ToString());
            //axl.WriteAttribute("beginrecord", filter.BeginRecord.ToString());
            //axl.WriteAttribute("outputmode", "newxml");
            //axl.WriteAttribute("geometry", ((queryGeometry == QueryGeometryType.both || queryGeometry == QueryGeometryType.geometry) ? "true" : "false"));
            //axl.WriteAttribute("envelope", ((queryGeometry == QueryGeometryType.both || queryGeometry == QueryGeometryType.envelope) ? "true" : "false"));
            //axl.WriteAttribute("compact", "false");
            //axl.WriteAttribute("checkesc", "true");

            IArcXmlGET_FEATURES_Attributes attributes = filter as IArcXmlGET_FEATURES_Attributes;
            if (attributes != null)
            {
                attributes.WriteAttributes(axl.xmlWriter);
            }
            else
            {
                if (filter != null)
                {
                    axl.WriteAttribute("beginrecord", filter.BeginRecord.ToString());
                }

                axl.WriteAttribute("outputmode", "newxml");
            }
            axl.WriteStartElement("LAYER");
            axl.WriteAttribute("id", _id);
            axl.WriteEndElement(); // LAYER

            axl.WriteStartElement("QUERY");
            if (filter.SubFields == "" || filter.SubFields == "*")
            {
                axl.WriteAttribute("subfields", "#ALL#");
            }
            else
            {
                filter.AddField(this.IDFieldName);
                axl.WriteAttribute("subfields", filter.SubFields);
            }
            string where = String.Empty;
            if (filter is IGlobalRowIDFilter)
            {
                where = ((IGlobalRowIDFilter)filter).RowIDWhereClause;
            }
            else if (filter is IRowIDFilter)
            {
                where = ((IRowIDFilter)filter).RowIDWhereClause;
            }
            else
            {
                where = filter.WhereClause;
            }

            if (!String.IsNullOrEmpty(where))
            {
                axl.WriteAttribute("where", where);
            }

            if (attributes != null && attributes.BufferNode != null)
            {
                WriteBufferNode(axl, attributes.BufferNode, filter);
            }

            if (filter.FeatureSpatialReference != null)
            {
                axl.WriteStartElement("FEATURECOORDSYS");
                if (filter.FeatureSpatialReference.Name.ToLower().StartsWith("epsg:"))
                {
                    axl.WriteAttribute("id", filter.FeatureSpatialReference.Name.Split(':')[1]);
                }
                else
                {
                    axl.WriteAttribute("string", gView.Framework.Geometry.SpatialReference.ToESRIWKT(filter.FeatureSpatialReference));
                }
                if (filter.FeatureSpatialReference.Datum != null)
                {
                    axl.WriteAttribute("datumtransformstring", gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(filter.FeatureSpatialReference));
                }
                axl.WriteEndElement();
            }
            else if (attributes != null && attributes.Properties != null)
            {
                attributes.Properties.Write(axl.xmlWriter, "FILTERCOORDSYS");
                attributes.Properties.Write(axl.xmlWriter, "FEATURECOORDSYS");
            }
            else if (attributes != null && attributes.Properties != null)
            {
                attributes.Properties.Write(axl.xmlWriter, "FEATURECOORDSYS");
                //axl.WriteAttribute("subfields", filter.SubFields);
            }

            axl.WriteEndElement(); // QUERY
            axl.WriteEndElement(); // REQUEST

            string req = axl.Request;

            //string resp = m_parent.Connector.SendRequest(req, m_parent.server, m_parent.service, "Query");
            string resp = await this.SendRequest(filter, req);
            return resp;
        }

        async private Task<IFeatureTable> SpatialQuery(ISpatialFilter filter)
        {
            return this.CreateQueryResult(filter, await AXLSpatialQuery(filter));
        }
        async private Task<string> AXLSpatialQuery(ISpatialFilter filter)
        {
            AXLWriter axl = new AXLWriter();
            axl.WriteStartRequest("GET_FEATURES");
            //axl.WriteAttribute("featurelimit", _maxResult.ToString());
            //axl.WriteAttribute("beginrecord", filter.BeginRecord.ToString());
            //axl.WriteAttribute("outputmode", "newxml");
            //axl.WriteAttribute("geometry", ((queryGeometry == QueryGeometryType.both || queryGeometry == QueryGeometryType.geometry) ? "true" : "false"));
            //axl.WriteAttribute("envelope", ((queryGeometry == QueryGeometryType.both || queryGeometry == QueryGeometryType.envelope) ? "true" : "false"));
            //axl.WriteAttribute("compact", "false");
            //axl.WriteAttribute("checkesc", "true");
            IArcXmlGET_FEATURES_Attributes attributes = filter as IArcXmlGET_FEATURES_Attributes;
            if (attributes != null)
            {
                attributes.WriteAttributes(axl.xmlWriter);
            }
            else
            {
                if (filter != null)
                {
                    axl.WriteAttribute("beginrecord", filter.BeginRecord.ToString());
                }

                axl.WriteAttribute("outputmode", "newxml");
            }

            axl.WriteStartElement("LAYER");
            axl.WriteAttribute("id", _id);
            axl.WriteEndElement(); // LAYER

            axl.WriteStartElement("SPATIALQUERY");

            if (filter.SubFields != "#ALL#" && filter.SubFields != "*")
            {
                filter.AddField(this.IDFieldName);
                if (attributes != null && attributes.geometry)
                {
                    filter.AddField(this.ShapeFieldName);
                }
            }

            string where = (filter is IRowIDFilter) ? ((IRowIDFilter)filter).RowIDWhereClause : filter.WhereClause;
            if (where != "")
            {
                if (filter.SubFields == "" || filter.SubFields == "*")
                {
                    axl.WriteAttribute("subfields", "#ALL#");
                }
                else
                {
                    filter.AddField(this.IDFieldName);
                    axl.WriteAttribute("subfields", filter.SubFields);
                }
                axl.WriteAttribute("where", (filter is IRowIDFilter) ? ((IRowIDFilter)filter).RowIDWhereClause : filter.WhereClause);
            }
            else
            {
                axl.WriteAttribute("subfields", (filter.SubFields == "*") ? "#ALL#" : filter.SubFields);
            }

            if (attributes != null && attributes.BufferNode != null)
            {
                WriteBufferNode(axl, attributes.BufferNode, filter);
            }

            if (filter.FilterSpatialReference != null || filter.FeatureSpatialReference != null)
            {
                if (filter.FilterSpatialReference != null)
                {
                    axl.WriteStartElement("FILTERCOORDSYS");
                    if (filter.FilterSpatialReference.Name.ToLower().StartsWith("epsg:"))
                    {
                        axl.WriteAttribute("id", filter.FilterSpatialReference.Name.Split(':')[1]);
                    }
                    else
                    {
                        axl.WriteAttribute("string", gView.Framework.Geometry.SpatialReference.ToESRIWKT(filter.FilterSpatialReference));
                    }
                    if (filter.FilterSpatialReference.Datum != null)
                    {
                        axl.WriteAttribute("datumtransformstring", gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(filter.FilterSpatialReference));
                    }
                    axl.WriteEndElement();
                }
                if (filter.FeatureSpatialReference != null)
                {
                    axl.WriteStartElement("FEATURECOORDSYS");
                    if (filter.FeatureSpatialReference.Name.ToLower().StartsWith("epsg:"))
                    {
                        axl.WriteAttribute("id", filter.FeatureSpatialReference.Name.Split(':')[1]);
                    }
                    else
                    {
                        axl.WriteAttribute("string", gView.Framework.Geometry.SpatialReference.ToESRIWKT(filter.FeatureSpatialReference));
                    }
                    if (filter.FeatureSpatialReference.Datum != null)
                    {
                        axl.WriteAttribute("datumtransformstring", gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(filter.FeatureSpatialReference));
                    }
                    axl.WriteEndElement();
                }
            }
            else if (attributes != null && attributes.Properties != null)
            {
                attributes.Properties.Write(axl.xmlWriter, "FILTERCOORDSYS");
                attributes.Properties.Write(axl.xmlWriter, "FEATURECOORDSYS");
            }
            else if (filter.FilterSpatialReference != null)
            {
                // SPATIALREFERENCE
                axl.WriteStartElement("SPATIALREFERENCE");
                axl.WriteAttribute("name", filter.FilterSpatialReference.Name);
                axl.WriteAttribute("param", gView.Framework.Geometry.SpatialReference.ToProj4(filter.FilterSpatialReference));
                axl.WriteEndElement();

                // FILTERCOORDSYS
                axl.WriteStartElement("FILTERCOORDSYS");
                axl.WriteAttribute("string", gView.Framework.Geometry.SpatialReference.ToESRIWKT(filter.FilterSpatialReference));
                axl.WriteEndElement();
            }
            if (filter.Geometry != null)
            {
                axl.WriteStartElement("SPATIALFILTER");
                axl.WriteAttribute("relation", "area_intersection");

                axl.WriteRaw(ArcXMLGeometry.Geometry2AXL(filter.Geometry));

                axl.WriteEndElement(); // SPATIALFILTER
            }

            axl.WriteEndElement(); // SPATIALQUERY
            axl.WriteEndElement(); // REQUEST

            string req = axl.Request;

            //string resp = m_parent.Connector.SendRequest(req, m_parent.server, m_parent.service, "Query");
            string resp = await this.SendRequest(filter, req);
            return resp;
        }

        private void WriteBufferNode(AXLWriter axl, XmlNode bufferNode, IQueryFilter filter)
        {
            if (bufferNode == null || axl == null)
            {
                return;
            }

            //if (bufferNode.Attributes["project"] == null)
            //{
            //    XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("project");
            //    attr.Value = "true";
            //    bufferNode.Attributes.Append(attr);
            //}

            XmlNode targetLayer = bufferNode.SelectSingleNode("TARGETLAYER");
            if (targetLayer != null)
            {
                XmlNode spatialQueryNode = bufferNode.SelectSingleNode("SPATIALQUERY");
                if (spatialQueryNode == null)
                {
                    spatialQueryNode = bufferNode.OwnerDocument.CreateElement("SPATIALQUERY");
                    XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("subfields");
                    attr.Value = "#ALL#";
                    spatialQueryNode.Attributes.Append(attr);
                }

                // FEATURECOORDSYS und FILTERCOORDSYS müssen beim Buffern auch im 
                // TARGETLAYER stehen!!! Sonst projeziert IMS nicht richtig!!!
                if (spatialQueryNode.SelectSingleNode("FEATURECOORDSYS") == null &&
                    filter != null && filter.FeatureSpatialReference != null)
                {
                    XmlNode featureCoordsys = bufferNode.OwnerDocument.CreateElement("FEATURECOORDSYS");
                    if (filter.FeatureSpatialReference.Name.ToLower().StartsWith("epsg:"))
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("id");
                        attr.Value = filter.FeatureSpatialReference.Name.Split(':')[1];
                        featureCoordsys.Attributes.Append(attr);
                    }
                    else
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("string");
                        attr.Value = gView.Framework.Geometry.SpatialReference.ToESRIWKT(filter.FeatureSpatialReference);
                        featureCoordsys.Attributes.Append(attr);
                    }
                    if (filter.FeatureSpatialReference.Datum != null)
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("datumtransformstring");
                        attr.Value = gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(filter.FeatureSpatialReference);
                        featureCoordsys.Attributes.Append(attr);
                    }
                    spatialQueryNode.AppendChild(featureCoordsys);
                }
                if (spatialQueryNode.SelectSingleNode("FILTERCOORDSYS") == null &&
                    filter is ISpatialFilter &&
                    ((ISpatialFilter)filter).FilterSpatialReference != null)
                {
                    XmlNode filterCoordsys = bufferNode.OwnerDocument.CreateElement("FILTERCOORDSYS");
                    if (((ISpatialFilter)filter).FilterSpatialReference.Name.ToLower().StartsWith("epsg:"))
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("id");
                        attr.Value = ((ISpatialFilter)filter).FilterSpatialReference.Name.Split(':')[1];
                        filterCoordsys.Attributes.Append(attr);
                    }
                    else
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("string");
                        attr.Value = gView.Framework.Geometry.SpatialReference.ToESRIWKT(((ISpatialFilter)filter).FilterSpatialReference);
                        filterCoordsys.Attributes.Append(attr);
                    }
                    if (((ISpatialFilter)filter).FilterSpatialReference.Datum != null)
                    {
                        XmlAttribute attr = bufferNode.OwnerDocument.CreateAttribute("datumtransformstring");
                        attr.Value = gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(((ISpatialFilter)filter).FilterSpatialReference);
                        filterCoordsys.Attributes.Append(attr);
                    }
                    spatialQueryNode.AppendChild(filterCoordsys);
                }

                if (bufferNode.SelectSingleNode("SPATIALQUERY") == null &&
                    spatialQueryNode.ChildNodes.Count > 0)
                {
                    bufferNode.AppendChild(spatialQueryNode);
                }
            }

            axl.xmlWriter.WriteRaw(bufferNode.OuterXml);
        }

        async public Task<string> AXLSearch(IQueryFilter filter)
        {
            if (filter is IBufferQueryFilter)
            {
                filter = await BufferQueryFilter.ConvertToSpatialFilter(filter as IBufferQueryFilter);
                if (filter == null)
                {
                    return "";
                }
            }
            if (filter is ISpatialFilter)
            {
                return await AXLSpatialQuery((ISpatialFilter)filter);
            }
            else
            {
                return await AXLQuery(filter);
            }
        }

        #region IGeometryDef Member

        public bool HasZ
        {
            get { return _geomDef.HasZ; }
        }

        public bool HasM
        {
            get { return _geomDef.HasM; }
        }

        public GeometryType GeometryType
        {
            get { return _geomDef.GeometryType; }
        }

        public ISpatialReference SpatialReference
        {
            get
            {
                return _geomDef.SpatialReference;
            }
            set
            {
                _geomDef.SpatialReference = value;
            }
        }

        public GeometryFieldType GeometryFieldType
        {
            get
            {
                return GeometryFieldType.Default;
            }
        }
        #endregion

        protected virtual Task<string> SendRequest(IUserData userData, string axlRequest)
        {
            return Task.FromResult("");
        }

        #region IWebFeatureClass Member

        public string ID
        {
            get { return _id; }
        }

        #endregion

        #region IBeforeQueryEventHandler Member

        public event BeforeQueryEventHandler BeforeQuery = null;

        public void FireBeforeQureyEvent(ref IQueryFilter filter)
        {
            if (BeforeQuery != null)
            {
                BeforeQuery(this, ref filter);
            }
        }

        #endregion
    }

    public class AXLFeatureCursor : FeatureCursor
    {
        private XmlNodeList _features;
        private int _pos;
        private AXLFeatureClass _fc = null;
        private IQueryFilter _filter = null;
        private IArcXmlGET_FEATURES_Attributes _attributes;

        public AXLFeatureCursor(string resp, ISpatialReference fcSRef, ISpatialReference toSRef)
            : base(fcSRef, toSRef)
        {
            _pos = 0;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                _features = doc.SelectNodes("//FEATURE");
            }
            catch (Exception /*ex*/)
            {
                _features = null;
            }
        }
        public AXLFeatureCursor(string resp, AXLFeatureClass fc, ISpatialReference toSRef)
            : base((fc != null) ? fc.SpatialReference : null,
                   toSRef)
        {
            _pos = 0;
            _fc = fc;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                _features = doc.SelectNodes("//FEATURE");
            }
            catch (Exception /*ex*/)
            {
                _features = null;
            }
        }
        public AXLFeatureCursor(AXLFeatureClass fc, IQueryFilter filter)
            : base((fc != null) ? fc.SpatialReference : null,
                   (filter != null) ? filter.FeatureSpatialReference : null)
        {
            _fc = fc;
            _filter = filter;
            _attributes = _filter as IArcXmlGET_FEATURES_Attributes;

            if (_attributes == null)
            {
                _filter.BeginRecord = 1;
            }
        }

        private XmlDocument _doc;
        async private Task<bool> PerformQuery()
        {
            try
            {
                _doc = new XmlDocument();
                _doc.LoadXml(CBF.CBF2AXL(await _fc.AXLSearch(_filter)));

                XmlNode featureCount = _doc.SelectSingleNode("//FEATURECOUNT");
                if (featureCount != null)
                {
                    ((QueryFilter)_filter).LastQueryFeatureCount =
                        Convert.ToInt32(featureCount.Attributes["count"].Value);
                    ((QueryFilter)_filter).HasMore =
                        Convert.ToBoolean(featureCount.Attributes["hasmore"].Value);
                }
                if (_filter.HasMore)
                {
                    _filter.BeginRecord += _filter.LastQueryFeatureCount;
                }

                _features = null; // _doc.SelectNodes("//FEATURE");

                return true;
            }
            catch (Exception ex)
            {
                _fc = null;
                _filter = null;
                _features = null;
                _doc = null;
                throw (ex);
            }
        }

        public string ArcXML
        {
            get
            {
                if (_doc == null)
                {
                    return "";
                }

                return _doc.OuterXml;
            }
        }

        #region IFeatureCursor Member

        public void Reset()
        {
            _pos = 0;
            if (_filter != null)
            {
                if (_filter.BeginRecord > 1 || _features == null)
                {
                    _filter.BeginRecord = 1;
                    _features = null;
                    _doc = null;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        async public override Task<IFeature> NextFeature()
        {
            if (_features == null)  // First Call (XPath for features)
            {
                if (_doc == null)
                {
                    if (!await PerformQuery())
                    {
                        return null;
                    }
                }

                _features = _doc.SelectNodes("//FEATURE");
            }

            if (_fc == null && _filter == null)
            {
                if (_pos >= _features.Count)
                {
                    return null;
                }

                IFeature feature = AXLFeatureClass.ConvertAXLNode2Feature(_features[_pos++], _fc, _attributes);
                Transform(feature);
                return feature;
            }
            else
            {
                if (_filter != null)
                {
                    if (_pos >= _features.Count)
                    {
                        // bei diesen Typen wurden nur "featurelimit" features abgefragt...
                        // Kein automatisches Nachladen...!!!
                        //if (_filter is ArcXMLQueryFilter ||
                        //    _filter is ArcXMLSpatialFilter) return null;

                        if (_filter.HasMore)
                        {
                            _pos = 0;
                            await PerformQuery();

                            return await this.NextFeature();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                IFeature feature = AXLFeatureClass.ConvertAXLNode2Feature(_features[_pos++], _fc, _attributes);

                // kein Transform durchführen, da der Server schon automatisch
                // die Projektion durchführen sollte, wenn ein FeatureCoordSys Knoten
                // im Request steht...
                // Transform(feature);

                return feature;
            }
        }
        #endregion
    }

    public class AXLRasterClass : IWebRasterClass
    {
        protected IDataset _dataset;
        protected string _id;
        protected string _name = "", _aliasname = "";

        public AXLRasterClass(IDataset dataset, string id)
        {
            _dataset = dataset;
            _id = id;
        }


        #region IClass Member

        public string Name { get { return _name; } set { _name = value; } }
        public string Aliasname { get { return _aliasname; } set { _name = value; } }

        public IDataset Dataset
        {
            get { return _dataset; }
        }

        #endregion

        #region IWebRasterClass Member

        public string ID
        {
            get { return _id; }
        }

        #endregion
    }

    public class AXLQueryableRasterClass : AXLRasterClass, IPointIdentify, IBeforePointIdentifyEventHandler
    {
        public AXLQueryableRasterClass(IDataset dataset, string id)
            : base(dataset, id)
        {
        }

        #region IPointIdentify Member

        public IPointIdentifyContext CreatePointIdentifyContext() => new DummyPointIdentifyContext();

        async public Task<ICursor> PointQuery(gView.Framework.Carto.IDisplay display, IPoint point, ISpatialReference sRef, IUserData userdata, IPointIdentifyContext context)
        {
            if (point == null)
            {
                return null;
            }

            FireBeforePointIdentify(display, ref point, ref sRef, userdata);

            try
            {
                AXLWriter axl = new AXLWriter();
                axl.WriteStartRequest("GET_RASTER_INFO");
                axl.WriteAttribute("layerid", this.ID);
                axl.WriteAttribute("x", point.X.ToString());
                axl.WriteAttribute("y", point.Y.ToString());

                if (sRef != null)
                {
                    axl.WriteStartElement("COORDSYS");
                    axl.WriteAttribute("string", gView.Framework.Geometry.SpatialReference.ToESRIWKT(sRef));
                    if (sRef.Datum != null)
                    {
                        axl.WriteAttribute("datumtransformstring", gView.Framework.Geometry.SpatialReference.ToESRIGeotransWKT(sRef));
                    }

                    axl.WriteEndElement(); // COORDSYS
                }
                if (display != null)
                {
                    axl.WriteStartElement("gv_display");
                    axl.WriteAttribute("iwidth", display.ImageWidth.ToString());
                    axl.WriteAttribute("iheight", display.ImageHeight.ToString());
                    if (display.Envelope != null)
                    {
                        axl.WriteEnvelope(display.Envelope.minx, display.Envelope.miny,
                                          display.Envelope.maxx, display.Envelope.maxy);
                    }

                    axl.WriteEndElement(); // gv_display
                }
                string req = axl.Request;

                string resp = await this.SendRequest(userdata, req);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(resp);

                XmlNode respNode = doc.SelectSingleNode("//RESPONSE");
                if (respNode == null || respNode.ChildNodes.Count == 0)
                {
                    return null;
                }

                XmlNode infoNode = respNode.ChildNodes[0];
                switch (respNode.ChildNodes[0].Name)
                {
                    case "RASTER_INFO":
                        return new AXLRasterInfoRowCursor(respNode.SelectNodes("RASTER_INFO"));
                    case "gv_url_info":
                        if (infoNode.Attributes["url"] != null)
                        {
                            return new AXLUrlCursor(infoNode.Attributes["url"].Value);
                        }

                        break;
                    case "gv_text_info":
                        if (infoNode.Attributes["text"] != null)
                        {
                            return new AXLTextCursur(infoNode.Attributes["text"].Value.Replace("\\n", "\n"));
                        }

                        break;
                }
            }
            catch { }
            return null;
        }

        #endregion

        protected virtual Task<string> SendRequest(IUserData userData, string axlRequest)
        {
            return Task.FromResult("");
        }

        #region IBeforePointIdentifyEventHandler Member

        public event BeforePointIdentifyEventHandler BeforePointIdentify = null;

        public void FireBeforePointIdentify(gView.Framework.Carto.IDisplay display, ref IPoint point, ref ISpatialReference sRef, IUserData userdata)
        {
            if (BeforePointIdentify != null)
            {
                BeforePointIdentify(this, display, ref point, ref sRef, userdata);
            }
        }

        #endregion
    }

    public class AXLTextCursur : ITextCursor
    {
        private string _text;

        public AXLTextCursur(string text)
        {
            _text = text;
        }

        #region ITextCursor Member

        public string Text
        {
            get { return _text; }
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {

        }

        #endregion
    }
    public class AXLUrlCursor : IUrlCursor
    {
        private string _url;

        public AXLUrlCursor(string url)
        {
            _url = url;
        }

        #region IUrlCursor Member

        public string Url
        {
            get { return _url; }
        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {

        }

        #endregion
    }
    public class AXLRasterInfoRowCursor : IRowCursor
    {
        private int _pos = 0;
        private XmlNodeList _raster_infos;

        public AXLRasterInfoRowCursor(XmlNodeList raster_infos)
        {
            _raster_infos = raster_infos;
        }
        #region IRowCursor Member

        async public Task<IRow> NextRow()
        {
            if (_raster_infos == null || _pos >= _raster_infos.Count)
            {
                return null;
            }

            XmlNode raster_info = _raster_infos[_pos++];
            if (raster_info == null)
            {
                return await NextRow();
            }

            Row row = new Row();

            foreach (XmlNode bands in raster_info.SelectNodes("BANDS"))
            {
                string rasterid = (bands.Attributes["rasterid"] != null) ?
                    bands.Attributes["rasterid"].Value : String.Empty;

                foreach (XmlNode band in bands.SelectNodes("BAND"))
                {
                    if (band.Attributes["number"] == null ||
                        band.Attributes["value"] == null)
                    {
                        continue;
                    }

                    string fieldName = "Band " + band.Attributes["number"].Value;
                    if (raster_info.SelectNodes("BANDS").Count > 1 &&
                        !String.IsNullOrEmpty(rasterid))
                    {
                        fieldName += " (" + rasterid + ")";
                    }
                    row.Fields.Add(new FieldValue(fieldName, band.Attributes["value"].Value));
                }

                foreach (XmlNode attribute in bands.SelectNodes("attribute"))
                {
                    if (attribute.Attributes["name"] == null ||
                        attribute.Attributes["value"] == null)
                    {
                        continue;
                    }

                    string fieldName = attribute.Attributes["name"].Value;
                    if (raster_info.SelectNodes("BANDS").Count > 1 &&
                        !String.IsNullOrEmpty(rasterid))
                    {
                        fieldName += " (" + rasterid + ")";
                    }
                    row.Fields.Add(new FieldValue(fieldName, attribute.Attributes["value"].Value));
                }
            }

            if (raster_info.Attributes["x"] != null)
            {
                row.Fields.Add(new FieldValue("x", raster_info.Attributes["x"].Value));
            }

            if (raster_info.Attributes["y"] != null)
            {
                row.Fields.Add(new FieldValue("y", raster_info.Attributes["y"].Value));
            }

            return row;

        }

        #endregion

        #region IDisposable Member

        public void Dispose()
        {

        }

        #endregion
    }

    public enum OutputMode { binary = 0, xml = 1, newxml = 2 }
    public interface IArcXmlGET_FEATURES_Attributes
    {
        //   attributes ="true | false"  [true] 
        //   beginrecord ="integer"  [0] 
        //   checkesc ="true | false"  [false] 
        //   compact ="true | false"  [false] 
        //   dataframe ="string" 
        //   envelope ="true | false"  [false] 
        //   featurelimit ="integer"  [all features] 
        //   geometry ="true | false"  [true] 
        //   globalenvelope ="true | false"  [false] 
        //   outputmode ="binary | xml | newxml"  [binary] 
        //   skipfeatures ="true | false"  [false] 

        bool attributes { get; set; }
        int beginrecord { get; set; }
        int featurelimit { get; set; }
        bool checkesc { get; set; }
        bool compact { get; set; }
        string dataframe { get; set; }
        bool geometry { get; set; }
        bool envelope { get; set; }
        bool globalenvelope { get; set; }
        OutputMode outputmode { get; set; }
        bool skipfeatures { get; set; }

        void WriteAttributes(XmlWriter writer);

        ArcXMLProperties Properties { get; set; }

        XmlNode BufferNode { get; set; }
    }
    public interface IArcXMLQueryFilter : IQueryFilter, IArcXmlGET_FEATURES_Attributes
    {
    }

    public class ArcXMLQueryFilter : QueryFilter, IArcXMLQueryFilter
    {
        public ArcXMLQueryFilter() : base() { }
        public ArcXMLQueryFilter(IQueryFilter filter)
            : base(filter)
        {
            CopyFrom(filter as IArcXMLQueryFilter);
        }
        public ArcXMLQueryFilter(IArcXMLQueryFilter filter)
            : base(filter as IQueryFilter)
        {
            CopyFrom(filter);
        }

        private void CopyFrom(IArcXMLQueryFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            attributes = filter.attributes;
            beginrecord = filter.beginrecord;
            featurelimit = filter.featurelimit;
            checkesc = filter.checkesc;
            compact = filter.compact;
            dataframe = filter.dataframe;
            geometry = filter.geometry;
            envelope = filter.envelope;
            globalenvelope = filter.globalenvelope;
            outputmode = filter.outputmode;
            skipfeatures = filter.skipfeatures;
            Properties = filter.Properties;
            BufferNode = filter.BufferNode;
        }

        #region IArcXmlGET_FEATURES_Attributes Member

        private bool _attributes = true;
        public bool attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public int beginrecord
        {
            get { return base.BeginRecord; }
            set { base.BeginRecord = value; }
        }

        private int _featurelimit = 100;
        public int featurelimit
        {
            get { return _featurelimit; }
            set { _featurelimit = value; }
        }

        private bool _checkesc = false;
        public bool checkesc
        {
            get { return _checkesc; }
            set { _checkesc = value; }
        }

        private bool _compact = false;
        public bool compact
        {
            get { return _compact; }
            set { _compact = value; }
        }

        private string _dataframe = "";
        public string dataframe
        {
            get { return _dataframe; }
            set { _dataframe = value; }
        }

        private bool _geometry = true;
        public bool geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        private bool _enelope = false;
        public bool envelope
        {
            get { return _enelope; }
            set { _enelope = value; }
        }

        private bool _globalenvelope = false;
        public bool globalenvelope
        {
            get { return _globalenvelope; }
            set { _globalenvelope = value; }
        }

        private OutputMode _outputmode = OutputMode.newxml;
        public OutputMode outputmode
        {
            get { return _outputmode; }
            set { _outputmode = value; }
        }

        private bool _skipfeatures = false;
        public bool skipfeatures
        {
            get { return _skipfeatures; }
            set { _skipfeatures = value; }
        }

        public void WriteAttributes(XmlWriter writer)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteAttributeString("beginrecord", beginrecord.ToString());
            writer.WriteAttributeString("featurelimit", featurelimit.ToString());
            writer.WriteAttributeString("geometry", geometry.ToString().ToLower());
            writer.WriteAttributeString("envelope", envelope.ToString().ToLower());

            if (attributes == false)
            {
                writer.WriteAttributeString("attributes", "false");
            }

            if (checkesc == true)
            {
                writer.WriteAttributeString("checkesc", "true");
            }

            if (compact == true)
            {
                writer.WriteAttributeString("compact", "true");
            }

            if (dataframe != "")
            {
                writer.WriteAttributeString("dataframe", dataframe);
            }

            if (globalenvelope == true)
            {
                writer.WriteAttributeString("globalenvelope", "true");
            }

            if (skipfeatures == true)
            {
                writer.WriteAttributeString("skipfeatures", "true");
            }

            if (outputmode != OutputMode.binary)
            {
                writer.WriteAttributeString("outputmode", outputmode.ToString());
            }
        }

        private ArcXMLProperties _properties = null;
        public ArcXMLProperties Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }

        private XmlNode _bufferNode = null;
        public XmlNode BufferNode
        {
            get { return _bufferNode; }
            set { _bufferNode = value; }
        }
        #endregion

        public override object Clone()
        {
            return new ArcXMLQueryFilter(this);
        }

        public override object Clone(Type type)
        {
            if (type == typeof(ISpatialFilter))
            {
                return new ArcXMLSpatialFilter(this);
            }
            if (type == typeof(IQueryFilter))
            {
                return new ArcXMLQueryFilter(this);
            }
            return base.Clone(type);
        }
    }
    /*
    public class ArcXMLRowIDFilter : RowIDFilter,IArcXMLQueryFilter
    {
        public ArcXMLRowIDFilter(IRowIDFilter filter)
            : base(filter.SubFields, filter.IDs)
        {
        }
        public ArcXMLRowIDFilter(ArcXMLSpatialFilter filter)
            : this(filter as ArcXMLRowIDFilter)
        {
            attributes = filter.attributes;
            beginrecord = filter.beginrecord;
            featurelimit = filter.featurelimit;
            checkesc = filter.checkesc;
            compact = filter.compact;
            dataframe = filter.dataframe;
            geometry = filter.geometry;
            envelope = filter.envelope;
            globalenvelope = filter.globalenvelope;
            outputmode = filter.outputmode;
            skipfeatures = filter.skipfeatures;
            Properties = filter.Properties;
            BufferNode = filter.BufferNode;
        }

        #region IArcXmlGET_FEATURES_Attributes Member

        private bool _attributes = true;
        public bool attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public int beginrecord
        {
            get { return base.BeginRecord; }
            set { base.BeginRecord = value; }
        }

        private int _featurelimit = 100;
        public int featurelimit
        {
            get { return _featurelimit; }
            set { _featurelimit = value; }
        }
        private bool _checkesc = false;
        public bool checkesc
        {
            get { return _checkesc; }
            set { _checkesc = value; }
        }

        private bool _compact = false;
        public bool compact
        {
            get { return _compact; }
            set { _compact = value; }
        }

        private string _dataframe = "";
        public string dataframe
        {
            get { return _dataframe; }
            set { _dataframe = value; }
        }

        private bool _geometry = true;
        public bool geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        private bool _enelope = false;
        public bool envelope
        {
            get { return _enelope; }
            set { _enelope = value; }
        }

        private bool _globalenvelope = false;
        public bool globalenvelope
        {
            get { return _globalenvelope; }
            set { _globalenvelope = value; }
        }

        private OutputMode _outputmode = OutputMode.newxml;
        public OutputMode outputmode
        {
            get { return _outputmode; }
            set { _outputmode = value; }
        }

        private bool _skipfeatures = false;
        public bool skipfeatures
        {
            get { return _skipfeatures; }
            set { _skipfeatures = value; }
        }

        public void WriteAttributes(XmlWriter writer)
        {
            if (writer == null) return;

            writer.WriteAttributeString("beginrecord", beginrecord.ToString());
            writer.WriteAttributeString("featurelimit", featurelimit.ToString());
            writer.WriteAttributeString("geometry", geometry.ToString().ToLower());
            writer.WriteAttributeString("envelope", envelope.ToString().ToLower());

            if (attributes == false)
                writer.WriteAttributeString("attributes", "false");
            if (checkesc == true)
                writer.WriteAttributeString("checkesc", "true");
            if (compact == true)
                writer.WriteAttributeString("compact", "true");
            if (dataframe != "")
                writer.WriteAttributeString("dataframe", dataframe);
            if (globalenvelope == true)
                writer.WriteAttributeString("globalenvelope", "true");
            if (skipfeatures == true)
                writer.WriteAttributeString("skipfeatures", "true");
            if (outputmode != OutputMode.binary)
                writer.WriteAttributeString("outputmode", outputmode.ToString());
        }

        private ArcXMLProperties _properties = null;
        public ArcXMLProperties Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }

        private XmlNode _bufferNode = null;
        public XmlNode BufferNode
        {
            get { return _bufferNode; }
            set { _bufferNode = value; }
        }
        #endregion

        public override object Clone()
        {
            return new ArcXMLSpatialFilter(this);
        }
    }
    */
    public class ArcXMLSpatialFilter : SpatialFilter, IArcXMLQueryFilter
    {
        public ArcXMLSpatialFilter() : base() { }
        public ArcXMLSpatialFilter(IQueryFilter filter)
            : base(filter)
        {
            CopyFrom(filter as IArcXMLQueryFilter);
        }
        public ArcXMLSpatialFilter(IArcXMLQueryFilter filter)
            : base(filter as IQueryFilter)
        {
            CopyFrom(filter);
        }

        private void CopyFrom(IArcXMLQueryFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            attributes = filter.attributes;
            beginrecord = filter.beginrecord;
            featurelimit = filter.featurelimit;
            checkesc = filter.checkesc;
            compact = filter.compact;
            dataframe = filter.dataframe;
            geometry = filter.geometry;
            envelope = filter.envelope;
            globalenvelope = filter.globalenvelope;
            outputmode = filter.outputmode;
            skipfeatures = filter.skipfeatures;
            Properties = filter.Properties;
            BufferNode = filter.BufferNode;
        }

        #region IArcXmlGET_FEATURES_Attributes Member

        private bool _attributes = true;
        public bool attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public int beginrecord
        {
            get { return base.BeginRecord; }
            set { base.BeginRecord = value; }
        }

        private int _featurelimit = 100;
        public int featurelimit
        {
            get { return _featurelimit; }
            set { _featurelimit = value; }
        }
        private bool _checkesc = false;
        public bool checkesc
        {
            get { return _checkesc; }
            set { _checkesc = value; }
        }

        private bool _compact = false;
        public bool compact
        {
            get { return _compact; }
            set { _compact = value; }
        }

        private string _dataframe = "";
        public string dataframe
        {
            get { return _dataframe; }
            set { _dataframe = value; }
        }

        private bool _geometry = true;
        public bool geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        private bool _enelope = false;
        public bool envelope
        {
            get { return _enelope; }
            set { _enelope = value; }
        }

        private bool _globalenvelope = false;
        public bool globalenvelope
        {
            get { return _globalenvelope; }
            set { _globalenvelope = value; }
        }

        private OutputMode _outputmode = OutputMode.newxml;
        public OutputMode outputmode
        {
            get { return _outputmode; }
            set { _outputmode = value; }
        }

        private bool _skipfeatures = false;
        public bool skipfeatures
        {
            get { return _skipfeatures; }
            set { _skipfeatures = value; }
        }

        public void WriteAttributes(XmlWriter writer)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteAttributeString("beginrecord", beginrecord.ToString());
            writer.WriteAttributeString("featurelimit", featurelimit.ToString());
            writer.WriteAttributeString("geometry", geometry.ToString().ToLower());
            writer.WriteAttributeString("envelope", envelope.ToString().ToLower());

            if (attributes == false)
            {
                writer.WriteAttributeString("attributes", "false");
            }

            if (checkesc == true)
            {
                writer.WriteAttributeString("checkesc", "true");
            }

            if (compact == true)
            {
                writer.WriteAttributeString("compact", "true");
            }

            if (dataframe != "")
            {
                writer.WriteAttributeString("dataframe", dataframe);
            }

            if (globalenvelope == true)
            {
                writer.WriteAttributeString("globalenvelope", "true");
            }

            if (skipfeatures == true)
            {
                writer.WriteAttributeString("skipfeatures", "true");
            }

            if (outputmode != OutputMode.binary)
            {
                writer.WriteAttributeString("outputmode", outputmode.ToString() /*"binary"*/);
            }
        }

        private ArcXMLProperties _properties = null;
        public ArcXMLProperties Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }

        private XmlNode _bufferNode = null;
        public XmlNode BufferNode
        {
            get { return _bufferNode; }
            set { _bufferNode = value; }
        }
        #endregion

        public override object Clone()
        {
            return new ArcXMLSpatialFilter(this);
        }

        public override object Clone(Type type)
        {
            if (type == typeof(ISpatialFilter))
            {
                return new ArcXMLSpatialFilter(this);
            }
            if (type == typeof(IQueryFilter))
            {
                return new ArcXMLQueryFilter(this);
            }
            return base.Clone(type);
        }
    }

    public class ArcXMLBufferQueryFilter : BufferQueryFilter, IArcXMLQueryFilter
    {
        public ArcXMLBufferQueryFilter() : base() { }
        public ArcXMLBufferQueryFilter(IQueryFilter filter)
            : base(filter)
        {
            CopyFrom(filter as IArcXMLQueryFilter);
        }
        public ArcXMLBufferQueryFilter(ArcXMLBufferQueryFilter filter)
            : base(filter as IQueryFilter)
        {
            CopyFrom(filter);
        }

        private void CopyFrom(IArcXMLQueryFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            attributes = filter.attributes;
            beginrecord = filter.beginrecord;
            featurelimit = filter.featurelimit;
            checkesc = filter.checkesc;
            compact = filter.compact;
            dataframe = filter.dataframe;
            geometry = filter.geometry;
            envelope = filter.envelope;
            globalenvelope = filter.globalenvelope;
            outputmode = filter.outputmode;
            skipfeatures = filter.skipfeatures;
            Properties = filter.Properties;
            BufferNode = filter.BufferNode;
        }

        #region IArcXmlGET_FEATURES_Attributes Member

        private bool _attributes = true;
        public bool attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public int beginrecord
        {
            get { return base.BeginRecord; }
            set { base.BeginRecord = value; }
        }

        private int _featurelimit = 100;
        public int featurelimit
        {
            get { return _featurelimit; }
            set { _featurelimit = value; }
        }

        private bool _checkesc = false;
        public bool checkesc
        {
            get { return _checkesc; }
            set { _checkesc = value; }
        }

        private bool _compact = false;
        public bool compact
        {
            get { return _compact; }
            set { _compact = value; }
        }

        private string _dataframe = "";
        public string dataframe
        {
            get { return _dataframe; }
            set { _dataframe = value; }
        }

        private bool _geometry = true;
        public bool geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        private bool _enelope = false;
        public bool envelope
        {
            get { return _enelope; }
            set { _enelope = value; }
        }

        private bool _globalenvelope = false;
        public bool globalenvelope
        {
            get { return _globalenvelope; }
            set { _globalenvelope = value; }
        }

        private OutputMode _outputmode = OutputMode.newxml;
        public OutputMode outputmode
        {
            get { return _outputmode; }
            set { _outputmode = value; }
        }

        private bool _skipfeatures = false;
        public bool skipfeatures
        {
            get { return _skipfeatures; }
            set { _skipfeatures = value; }
        }

        public void WriteAttributes(XmlWriter writer)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteAttributeString("beginrecord", beginrecord.ToString());
            writer.WriteAttributeString("featurelimit", featurelimit.ToString());
            writer.WriteAttributeString("geometry", geometry.ToString().ToLower());
            writer.WriteAttributeString("envelope", envelope.ToString().ToLower());

            if (attributes == false)
            {
                writer.WriteAttributeString("attributes", "false");
            }

            if (checkesc == true)
            {
                writer.WriteAttributeString("checkesc", "true");
            }

            if (compact == true)
            {
                writer.WriteAttributeString("compact", "true");
            }

            if (dataframe != "")
            {
                writer.WriteAttributeString("dataframe", dataframe);
            }

            if (globalenvelope == true)
            {
                writer.WriteAttributeString("globalenvelope", "true");
            }

            if (skipfeatures == true)
            {
                writer.WriteAttributeString("skipfeatures", "true");
            }

            if (outputmode != OutputMode.binary)
            {
                writer.WriteAttributeString("outputmode", outputmode.ToString());
            }
        }

        private ArcXMLProperties _properties = null;
        public ArcXMLProperties Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }
        private XmlNode _bufferNode = null;
        public XmlNode BufferNode
        {
            get { return _bufferNode; }
            set { _bufferNode = value; }
        }
        #endregion

        public override object Clone()
        {
            return new ArcXMLBufferQueryFilter(this);
        }
    }

    public class ArcXMLFeature : GlobalFeature
    {

        private IEnvelope _envelope;
        public IEnvelope Envelope
        {
            get { return _envelope; }
            set { _envelope = value; }
        }

        public void ConvertAXL(XmlNode feature, IFeatureClass fc, IArcXmlGET_FEATURES_Attributes attributes)
        {
            if (feature == null)
            {
                return;
            }

            GlobalFeature feat = this;
            List<FieldValue> fields = feat.Fields;

            foreach (XmlNode field in feature.SelectNodes("FIELDS/FIELD"))
            {
                if (field.Attributes["name"] == null || field.Attributes["value"] == null)
                {
                    continue;
                }

                FieldValue fVal = new FieldValue(ArcXMLGeometry.shortName(field.Attributes["name"].Value));


                if (fVal.Name == fc.IDFieldName)
                {
                    feat.GlobalOID = Convert.ToInt64(field.Attributes["value"].Value);
                }
                IField pField = fc.FindField(fVal.Name);
                if (pField != null)
                {
                    try
                    {
                        switch (pField.type)
                        {
                            case FieldType.biginteger:
                            case FieldType.ID:
                                fVal.Value = Convert.ToInt64(field.Attributes["value"].Value);
                                break;
                            case FieldType.integer:
                                fVal.Value = Convert.ToInt32(field.Attributes["value"].Value);
                                break;
                            case FieldType.smallinteger:
                                fVal.Value = Convert.ToInt16(field.Attributes["value"].Value);
                                break;
                            case FieldType.boolean:
                                fVal.Value = Convert.ToBoolean(field.Attributes["value"].Value);
                                break;
                            case FieldType.Float:
                            case FieldType.Double:
                                fVal.Value = Convert.ToDouble(field.Attributes["value"].Value.Replace(".", ","));
                                break;
                            default:
                                fVal.Value = field.Attributes["value"].Value;
                                break;
                        }
                    }
                    catch
                    {
                        fVal.Value = System.DBNull.Value;
                    }
                }

                fields.Add(fVal);
            }

            XmlNode envelope = feature.SelectSingleNode("ENVELOPE");
            if (envelope != null)
            {
                feature.RemoveChild(envelope);
            }

            if (attributes != null)
            {
                if (attributes.envelope && envelope != null)
                {
                    this.Envelope = ArcXMLGeometry.AXL2Geometry(envelope.OuterXml) as IEnvelope;
                }

                if (attributes.geometry)
                {
                    this.Shape = ArcXMLGeometry.AXL2Geometry(feature.InnerXml);
                }
            }
            else
            {
                feat.Shape = ArcXMLGeometry.AXL2Geometry(feature.InnerXml);
            }
        }

    }


}
