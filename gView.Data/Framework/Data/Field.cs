using gView.Framework.Data.Metadata;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.ComponentModel;
using System.Data;

namespace gView.Framework.Data
{
    public class Field : FieldMetadata, IField, IPersistable, IPriority
    {
        protected string m_name, m_aliasname = "";
        protected FieldType m_type;
        protected int m_precision, m_size;
        protected bool m_visible = true, m_isRequired = false, m_isEditable = true, _save_type = false;
        protected object m_defaultValue = null;
        protected IFieldDomain _domain = null;
        protected int _priority = -1;

        public Field()
        {
            type = FieldType.unknown;
        }
        public Field(string name) : this()
        {
            m_name = name;
        }

        public Field(string name, FieldType fieldType)
            : this(name)
        {
            type = fieldType;
        }

        public Field(string name, FieldType fieldType, int size)
            : this(name, fieldType)
        {
            m_size = size;
        }

        public Field(string name, FieldType fieldType, int size, int precision)
            : this(name, fieldType, size)
        {
            m_precision = precision;
        }

        public Field(IField field)
        {
            m_name = field.name;
            m_aliasname = field.aliasname;
            m_type = field.type;
            m_precision = field.precision;
            m_size = field.size;
            m_isRequired = field.IsRequired;
            m_isEditable = field.IsEditable;
            m_defaultValue = field.DefautValue;
            _domain = field.Domain;
            if (field is Field)
            {
                _save_type = ((Field)field).SaveType;
            }
        }

        public Field(DataRow schemaRow)
        {
            if (schemaRow == null)
            {
                return;
            }

            this.name = schemaRow["ColumnName"].ToString();

            if (schemaRow["DataType"].GetType() == typeof(Type))
            {
                if ((Type)schemaRow["DataType"] == typeof(System.Int32))
                {
                    this.type = FieldType.integer;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Int16))
                {
                    this.type = FieldType.smallinteger;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Int64))
                {
                    this.type = FieldType.biginteger;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.DateTime))
                {
                    this.type = FieldType.Date;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Double))
                {
                    this.type = FieldType.Double;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Single))
                {
                    this.type = FieldType.Float;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Boolean))
                {
                    this.type = FieldType.boolean;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Char))
                {
                    this.type = FieldType.character;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.String))
                {
                    this.type = FieldType.String;
                }
                else if ((Type)schemaRow["DataType"] == typeof(System.Byte[]))
                {
                    this.type = FieldType.binary;
                }
                else
                {
                    this.type = FieldType.unknown;
                }
            }
            else
            {
                if (schemaRow["DataType"]?.ToString() == typeof(System.Int32).ToString())
                {
                    this.type = FieldType.integer;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Int16).ToString())
                {
                    this.type = FieldType.smallinteger;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Int64).ToString())
                {
                    this.type = FieldType.biginteger;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.DateTime).ToString())
                {
                    this.type = FieldType.Date;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Double).ToString())
                {
                    this.type = FieldType.Double;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Single).ToString())
                {
                    this.type = FieldType.Float;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Boolean).ToString())
                {
                    this.type = FieldType.boolean;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Char).ToString())
                {
                    this.type = FieldType.character;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.String).ToString())
                {
                    this.type = FieldType.String;
                }
                else if (schemaRow["DataType"]?.ToString() == typeof(System.Byte[]).ToString())
                {
                    this.type = FieldType.binary;
                }
                else
                {
                    this.type = FieldType.unknown;
                }
            }

            this.size = Convert.ToInt32(schemaRow["ColumnSize"]);
            this.precision = Convert.ToInt32(schemaRow["NumericPrecision"]);
        }

        // Sometimes is good to save the type. i.e Database Joins Fields
        // For normal featurelayer fields, don't save the type -> get it from the featureclass, later
        public bool SaveType
        {
            get { return _save_type; }
            set { _save_type = value; }
        }

        public string name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }
        public string aliasname
        {
            get
            {
                if (m_aliasname == "")
                {
                    return name;
                }

                return m_aliasname;
            }
            set
            {
                m_aliasname = value;
            }
        }
        public int precision
        {
            get
            {
                //if (m_precision > 0)
                //{
                //    return m_precision;
                //}

                switch (type)
                {
                    case FieldType.Float:
                    case FieldType.Double:
                        return m_precision;
                }

                return 0;
            }
            set { m_precision = value; }
        }
        public int size
        {
            get
            {
                if (m_size > 0)
                {
                    return m_size;
                }

                switch (type)
                {
                    case FieldType.biginteger:
                        return 8;
                    case FieldType.String:
                    case FieldType.binary:
                        return m_size;
                    case FieldType.boolean:
                        return sizeof(bool);
                    case FieldType.character:
                        return 1;
                    case FieldType.Date:
                        return 8;
                    case FieldType.Double:
                        return 31;
                    case FieldType.Float:
                        return 19;
                    case FieldType.ID:
                    case FieldType.integer:
                        return 15;
                    case FieldType.Shape:
                        return 0;
                    case FieldType.smallinteger:
                        return 7;
                }
                return 0;
            }
            set { m_size = value; }
        }
        public FieldType type
        {
            get { return m_type; }
            set
            {
                m_type = value;
                if (m_type == FieldType.String && m_size == 0)
                {
                    m_size = 50;
                }
            }
        }

        [Browsable(false)]
        public bool IsRequired
        {
            get { return m_isRequired; }
            set { m_isRequired = value; }
        }

        [Browsable(false)]
        public bool IsEditable
        {
            get { return m_isEditable; }
            set { m_isEditable = value; }
        }

        [Browsable(false)]
        public object DefautValue
        {
            get { return m_defaultValue; }
            set { m_defaultValue = value; }
        }

        [Browsable(false)]
        public bool visible
        {
            get { return m_visible; }
            set { m_visible = value; }
        }

        public IFieldDomain Domain
        {
            get { return _domain; }
            set { _domain = value; }
        }

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            m_name = (string)stream.Load("Name", "");
            m_aliasname = (string)stream.Load("Alias", "");
            m_type = (FieldType)stream.Load("Type", (int)FieldType.unknown);

            //m_precision = (int)stream.Load("Precision", 0);
            //m_size = (int)stream.Load("Size", 0);

            m_visible = (bool)stream.Load("Visible", true);
            m_isEditable = (bool)stream.Load("Editable", true);
            m_isRequired = (bool)stream.Load("Required", false);

            _domain = stream.Load("Domain", null) as IFieldDomain;
            _priority = (int)stream.Load("Priority", (int)-1);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("Name", m_name);
            stream.Save("Alias", m_aliasname);
            if (_save_type)
            {
                stream.Save("Type", (int)m_type);
            }
            //stream.Save("Precision", m_precision);
            //stream.Save("Size", m_size);

            stream.Save("Visible", m_visible);
            stream.Save("Editable", m_isEditable);
            stream.Save("Required", m_isRequired);

            if (_domain != null)
            {
                stream.Save("Domain", _domain);
            }

            stream.Save("Priority", _priority);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is IField))
            {
                return false;
            }

            IField field = obj as IField;
            return this.type == field.type &&
                this.name == field.name &&
                this.aliasname == field.aliasname &&
                this.size == field.size &&
                this.precision == field.precision;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return this.name;
        }

        static public string shortName(string fieldname)
        {
            int pos = 0;
            string[] fieldnames = fieldname.Split(';');
            fieldname = "";
            for (int i = 0; i < fieldnames.Length; i++)
            {
                while ((pos = fieldnames[i].IndexOf(".")) != -1)
                {
                    fieldnames[i] = fieldnames[i].Substring(pos + 1, fieldnames[i].Length - pos - 1);
                }
                if (fieldname != "")
                {
                    fieldname += ";";
                }

                fieldname += fieldnames[i];
            }

            return fieldname;
        }

        #region IPriority Member

        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        #endregion

        static public string WhereClauseFieldName(string fieldName)
        {
            if (fieldName.Contains(":"))  // Joined Field
            {
                if (!fieldName.StartsWith("["))
                {
                    fieldName = "[" + fieldName;
                }

                if (!fieldName.EndsWith("]"))
                {
                    fieldName += "]";
                }
            }

            return fieldName;
        }
    }
}
