using gView.Framework.Carto;
using gView.Framework.IO;
using gView.Framework.UI;
using System.Threading.Tasks;

namespace gView.Interoperability.Server
{
    class ServiceWrapper : IMetadataProvider, IPropertyPage
    {
        private IServiceMap _serviceMap;
        #region IMetadataProvider Member


        public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            if (Object is IServiceMap)
            {
                _serviceMap = (IServiceMap)Object; ;
                return Task.FromResult(true);
            }

            _serviceMap = null;
            return Task.FromResult(false);
        }

        public string Name
        {
            get { return "Service Wrapping"; }
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {

        }

        public void Save(IPersistStream stream)
        {

        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            return null;
        }

        public object PropertyPageObject()
        {
            return null;
        }

        #endregion
    }
}
