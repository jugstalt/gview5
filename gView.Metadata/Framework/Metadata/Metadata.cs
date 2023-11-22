using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace gView.Framework.Metadata
{
    [gView.Framework.system.RegisterPlugIn("32C9CDB8-5A2D-4fc1-87AD-2AE960D1E31A")]
    public class GeneralMetadata : IMetadataProvider, IPropertyPage, IPropertyModel
    {
        private PropertyModel _properties;

        public GeneralMetadata()
        {
            _properties = new PropertyModel();
        }

        #region Properties
        public string Abstract
        {
            get { return _properties.Abstract; }
            set { _properties.Abstract = value; }
        }
        public string Purpose
        {
            get { return _properties.Purpose; }
            set { _properties.Purpose = value; }
        }
        public string Language
        {
            get { return _properties.Language; }
            set { _properties.Language = value; }
        }
        public string Supplemental_Information
        {
            get { return _properties.SupplementalInformation; }
            set { _properties.SupplementalInformation = value; }
        }
        public string Access_Constraints
        {
            get { return _properties.AccessConstraints; }
            set { _properties.AccessConstraints = value; }
        }
        public string Use_Constraints
        {
            get { return _properties.UseConstraints; }
            set { _properties.UseConstraints = value; }
        }
        public string Contact
        {
            get { return _properties.Contact; }
            set { _properties.Contact = value; }
        }
        public string Credits
        {
            get { return _properties.Credits; }
            set { _properties.Credits = value; }
        }
        #endregion

        #region IMetadataProvider Member

        public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            return Task.FromResult(true);
        }

        public string Name
        {
            get { return "General"; }
        }


        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _properties.Abstract = (string)stream.Load("Abstract", String.Empty);
            _properties.Purpose = (string)stream.Load("Purpose", String.Empty);
            _properties.Language = (string)stream.Load("Language", String.Empty);
            _properties.SupplementalInformation = (string)stream.Load("Supplemental_Information", String.Empty);
            _properties.AccessConstraints = (string)stream.Load("Access_Constraints", String.Empty);
            _properties.UseConstraints = (string)stream.Load("Use_Constraints", String.Empty);
            _properties.Contact = (string)stream.Load("Contact", String.Empty);
            _properties.Credits = (string)stream.Load("Credits", String.Empty);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("Abstract", _properties.Abstract);
            stream.Save("Purpose", _properties.Purpose);
            stream.Save("Language", _properties.Language);
            stream.Save("Supplemental_Information", _properties.SupplementalInformation);
            stream.Save("Access_Constraints", _properties.AccessConstraints);
            stream.Save("Use_Constraints", _properties.UseConstraints);
            stream.Save("Contact", _properties.Contact);
            stream.Save("Credits", _properties.Credits);
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Metadata.UI.dll");

            IPlugInParameter p = uiAssembly.CreateInstance("gView.Framework.Metadata.UI.GeneralMetadataControl") as IPlugInParameter;
            if (p != null)
            {
                p.Parameter = this;
            }

            return p;
        }

        public object PropertyPageObject()
        {
            return this;
        }

        #endregion

        #region IPropertyModel

        public Type PropertyModelType => typeof(PropertyModel);

        public object GetPropertyModel()
        {
            return _properties;
        }

        public void SetPropertyModel(object propertyModel)
        {
            _properties = propertyModel as PropertyModel ?? new PropertyModel();
        }

        #endregion

        #region Classes

        private class PropertyModel
        {
            public string Abstract { get; set; } = "";
            public string Purpose { get; set; } = "";
            public string Language { get; set; } = "";
            public string SupplementalInformation { get; set; } = "";
            public string AccessConstraints { get; set; } = "";
            public string UseConstraints { get; set; } = "";
            public string Credits { get; set; } = "";
            public string Contact { get; set; } = "";
        }

        #endregion
    }
}
