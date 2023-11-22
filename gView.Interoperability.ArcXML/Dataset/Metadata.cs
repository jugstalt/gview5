using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.MapServer;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.ArcXML.Dataset
{
    [gView.Framework.system.RegisterPlugIn("704FCEAF-7BD8-4e50-81AB-8F5581584F9A")]
    public class Metadata : IMetadataProvider, IPropertyPage
    {
        public enum credentialMethod { none = 0, def = 1, net = 2, custom = 3 }
        private string _modifyOutputFile = String.Empty, _modifyOutputUrl = String.Empty;
        private credentialMethod _credMethod = credentialMethod.none;
        private string _credUser = String.Empty, _credPwd = String.Empty, _credDomain = String.Empty;
        private static object lockThis = new object();

        #region IMetadataProvider Member

        async public Task<bool> ApplyTo(object Object, bool setDefaults = false)
        {
            if (Object is IServiceMap)
            {
                IServiceMap map = (IServiceMap)Object;
                IMapServer server = map.MapServer;

                if (await ServiceMapIsSVC(server, map))
                {
                    ArcIMSClass cls = ArcIMSServiceClass(map);
                    if (cls != null)
                    {
                        lock (lockThis)
                        {
                            cls.ModifyResponseOuput -= new EventHandler(cls_ModifyResponseOuput);
                            cls.ModifyResponseOuput += new EventHandler(cls_ModifyResponseOuput);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        void cls_ModifyResponseOuput(object sender, EventArgs e)
        {
            if (!(e is ModifyOutputEventArgs) ||
                ((ModifyOutputEventArgs)e).OutputNode == null)
            {
                return;
            }

            XmlNode output = ((ModifyOutputEventArgs)e).OutputNode;

            if (_credMethod != credentialMethod.none)
            {
                XmlNode credNode = output.OwnerDocument.CreateElement("credentials");
                XmlAttribute attr;
                switch (_credMethod)
                {
                    case credentialMethod.def:
                        attr = output.OwnerDocument.CreateAttribute("default");
                        attr.Value = "true";
                        credNode.Attributes.Append(attr);
                        break;
                    case credentialMethod.net:
                        attr = output.OwnerDocument.CreateAttribute("default");
                        attr.Value = "net";
                        credNode.Attributes.Append(attr);
                        break;
                    case credentialMethod.custom:
                        attr = output.OwnerDocument.CreateAttribute("domain");
                        attr.Value = _credDomain;
                        credNode.Attributes.Append(attr);
                        attr = output.OwnerDocument.CreateAttribute("user");
                        attr.Value = _credUser;
                        credNode.Attributes.Append(attr);
                        attr = output.OwnerDocument.CreateAttribute("pwd");
                        attr.Value = _credPwd;
                        credNode.Attributes.Append(attr);
                        break;
                }
                output.AppendChild(credNode);
            }

            if (output.Attributes["file"] != null &&
                !String.IsNullOrEmpty(_modifyOutputFile))
            {
                output.Attributes["file"].Value =
                String.Format(_modifyOutputFile, output.Attributes["file"].Value);
            }

            if (output.Attributes["url"] != null &&
                !String.IsNullOrEmpty(_modifyOutputUrl))
            {
                output.Attributes["url"].Value =
                String.Format(_modifyOutputUrl, output.Attributes["url"].Value);
            }
        }

        public string Name
        {
            get { return "ArcIMS Service"; }
        }

        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            _credMethod = (credentialMethod)stream.Load("credMethod", (int)credentialMethod.none);
            _credUser = (string)stream.Load("credUser", String.Empty);
            _credPwd = (string)stream.Load("credPwd", String.Empty);
            if (!String.IsNullOrEmpty(_credPwd))
            {
                _credPwd = Crypto.Decrypt(_credPwd, "5A01A23E93664a8e8A3DC8F84C05FAD1");
            }

            _credDomain = (string)stream.Load("credDomain", String.Empty);

            _modifyOutputFile = (string)stream.Load("modOFile", String.Empty);
            _modifyOutputUrl = (string)stream.Load("modOUrl", String.Empty);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("credMethod", (int)_credMethod);
            stream.Save("credUser", _credUser);
            stream.Save("credPwd",
                String.IsNullOrEmpty(_credPwd) ?
                    String.Empty :
                    Crypto.Encrypt(_credPwd, "5A01A23E93664a8e8A3DC8F84C05FAD1")
                );
            stream.Save("credDomain", _credDomain);

            stream.Save("modOFile", _modifyOutputFile);
            stream.Save("modOUrl", _modifyOutputUrl);
        }

        #endregion

        #region Properties
        public credentialMethod CredentialMethod
        {
            get { return _credMethod; }
            set { _credMethod = value; }
        }
        public string CredentialUser
        {
            get { return _credUser; }
            set { _credUser = value; }
        }
        public string CredentialPwd
        {
            get { return _credPwd; }
            set { _credPwd = value; }
        }
        public string CredentialDomain
        {
            get { return _credDomain; }
            set { _credDomain = value; }
        }

        public string ModifyOutputFile
        {
            get { return _modifyOutputFile; }
            set { _modifyOutputFile = value; }
        }
        public string ModifyOutputUrl
        {
            get { return _modifyOutputUrl; }
            set { _modifyOutputUrl = value; }
        }
        #endregion

        #region Helper
        private ArcIMSClass ArcIMSServiceClass(IServiceMap map)
        {
            if (map.MapElements == null)
            {
                return null;
            }

            foreach (IDatasetElement element in map.MapElements)
            {
                if (element != null && element.Class is ArcIMSClass)
                {
                    return (ArcIMSClass)element.Class;
                }
            }
            return null;
        }
        async private Task<bool> ServiceMapIsSVC(IMapServer server, IServiceMap map)
        {
            if (server == null || map == null)
            {
                return false;
            }

            foreach (IMapService service in await server.Maps(null))
            {
                if (service == null)
                {
                    continue;
                }

                if (service.Name == map.Name && service.Type == MapServiceType.SVC)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region IPropertyPage Member

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Interoperability.ArcXML.UI.dll");

            IPlugInParameter p = uiAssembly.CreateInstance("gView.Interoperability.ArcXML.UI.PropertyPage.Metadata") as IPlugInParameter;
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
    }
}
