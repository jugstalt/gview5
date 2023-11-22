using gView.Framework.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.IO
{

    public class JsonStream : ErrorReport, IPersistStream
    {
        

        #region IPersistStream

        public object Load(string key)
        {
            throw new global::System.NotImplementedException();
        }

        public object Load(string key, object defVal)
        {
            throw new global::System.NotImplementedException();
        }

        public object Load(string key, object defVal, object objectInstance)
        {
            throw new global::System.NotImplementedException();
        }

        public Task<T> LoadAsync<T>(string key, T objectInstance, T defaultValue = default) where T : IPersistableLoadAsync
        {
            throw new global::System.NotImplementedException();
        }

        public Task<T> LoadPluginAsync<T>(string key, T unknownPlugin = default) where T : IPersistableLoadAsync
        {
            throw new global::System.NotImplementedException();
        }

        public void Save(string key, object val)
        {
            throw new global::System.NotImplementedException();
        }

        public void SaveEncrypted(string key, string val)
        {
            throw new global::System.NotImplementedException();
        }

        #endregion
    }
}
