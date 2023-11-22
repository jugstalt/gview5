using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace gView.Framework.Db
{
    public class DbConnectionString : UserData, IPersistable
    {
        private string _providerID = String.Empty;
        private string _schemaName = String.Empty;
        private bool _useProvider = true;

        public string ProviderId
        {
            get { return _providerID; }
            set { _providerID = value; }
        }

        public bool UseProviderInConnectionString
        {
            get { return _useProvider; }
            set { _useProvider = value; }
        }

        public string SchemaName
        {
            get { return _schemaName; }
            set { _schemaName = value; }
        }

        public string ConnectionString
        {
            get
            {
                try
                {
                    string configPath = Path.Combine(SystemVariables.ApplicationDirectory, "gView.DB.UI.json");

                    var commondDbConnections = JsonSerializer.Deserialize<CommonDbConnectionsModelModel>(File.ReadAllText(configPath),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    var provider = commondDbConnections?.Providers?.Where(p => p.Id == _providerID).FirstOrDefault();
                    if (provider == null)
                    {
                        return String.Empty;
                    }

                    var scheme = provider.Schemes?.Where(s => s.Name == _schemaName).FirstOrDefault();
                    if (scheme == null)
                    {
                        return String.Empty;
                    }

                    string connectionString = scheme.ConnectionString;

                    if (_useProvider)
                    {
                        connectionString = $"{provider.Id}:{connectionString}";
                    }

                    foreach (string key in this.UserDataTypes)
                    {
                        string val = (this.GetUserData(key) != null) ? this.GetUserData(key).ToString() : String.Empty;
                        connectionString = connectionString.Replace($"[{key}]", val);
                    }

                    return connectionString;
                }
                catch
                {
                    return String.Empty;
                }
            }
        }

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            foreach (String key in this.UserDataTypes)
            {
                this.RemoveUserData(key);
            }

            _providerID = (string)stream.Load("ProviderID", String.Empty);
            _schemaName = (string)stream.Load("SchemaName", String.Empty);
            _useProvider = (bool)stream.Load("UseProvider", true);

            KeyValue kv;
            while ((kv = (KeyValue)stream.Load("KeyValue", null, new KeyValue())) != null)
            {
                this.SetUserData(kv.Key, kv.Value);
            }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("ProviderID", _providerID);
            stream.Save("SchemaName", _schemaName);
            stream.Save("UseProvider", _useProvider);

            foreach (String key in this.UserDataTypes)
            {
                if (this.GetUserData(key) == null)
                {
                    stream.Save("KeyValue", new KeyValue(key, String.Empty));
                }
                else
                {
                    stream.Save("KeyValue", new KeyValue(key, this.GetUserData(key).ToString()));
                }
            }
        }

        #endregion

        #region HelperClasses
        private class KeyValue : IPersistable
        {
            private string _key = String.Empty, _value = String.Empty;

            public KeyValue() { }
            public KeyValue(string key, string val)
            {
                _key = key;
                _value = val;
            }

            public string Key
            {
                get { return _key; }
                set { _key = value; }
            }

            public string Value
            {
                get { return _value; }
                set { _value = value; }
            }

            #region IPersistable Member

            public void Load(IPersistStream stream)
            {
                _key = (string)stream.Load("Key", String.Empty);
                if (_key.ToLower() == "password")
                {
                    _value = Crypto.Decrypt((string)stream.Load("Val", String.Empty), "gView.Encrypted.Password");
                }
                else
                {
                    _value = (string)stream.Load("Val", String.Empty);
                }
            }

            public void Save(IPersistStream stream)
            {
                stream.Save("Key", _key);
                if (_key.ToLower() == "password")
                {
                    stream.Save("Val", Crypto.Encrypt(_value, "gView.Encrypted.Password"));
                }
                else
                {
                    stream.Save("Val", _value);
                }
            }

            #endregion
        }

        #endregion

        public override string ToString()
        {
            XmlStream stream = new XmlStream("DbConnectionString");
            Save(stream);

            return stream.ToString();
        }
        public void FromString(string connection)
        {
            XmlStream stream = new XmlStream("DbConnectionString");
            StringReader sr = new StringReader(connection);
            stream.ReadStream(sr);

            Load(stream);
        }

        public bool TryFromConnectionString(string providerId, string connectionString)
        {
            try
            {
                string configPath = Path.Combine(SystemVariables.ApplicationDirectory, "gView.DB.UI.json");

                var commondDbConnections = JsonSerializer.Deserialize<CommonDbConnectionsModelModel>(File.ReadAllText(configPath),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                while (connectionString.EndsWith(";"))
                {
                    connectionString = connectionString.Substring(0, connectionString.Length - 1).Trim();
                }

                Dictionary<string, string> connStrDic = ConfigTextStream.Extract(connectionString.Trim());

                var provider = commondDbConnections?.Providers?.Where(p => p.Id == providerId).FirstOrDefault();
                if (provider?.Schemes == null)
                {
                    return false;
                }

                foreach (var scheme in provider.Schemes)
                {
                    var schemeName = scheme.Name;
                    while (schemeName.EndsWith(";"))
                    {
                        schemeName = schemeName.Substring(0, schemeName.Length - 1).Trim();
                    }

                    Dictionary<string, string> schemaDic = ConfigTextStream.Extract(scheme.ConnectionString);

                    if (CompareConnectionStringDictionary(connStrDic, schemaDic))
                    {
                        SetUserParameters(connStrDic, schemaDic);
                        _providerID = provider.Id;
                        _schemaName = schemeName;

                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public DbConnectionString Clone()
        {
            var clone = DbConnectionString.Build(_providerID, _useProvider);
            clone._schemaName = _schemaName;

            foreach (var key in this.UserDataTypes)
            {
                clone.SetUserData(key, this.GetUserData(key));
            }

            return clone;
        }

        #region Helper

        private void SetUserParameters(
            Dictionary<string, string> connStrDic,
            Dictionary<string, string> schemaDic)
        {
            if (connStrDic.Count != schemaDic.Count)
            {
                return;
            }

            for (int i = 0; i < schemaDic.Count; i++)
            {
                string schemaKey = DictionaryKey(schemaDic, i);

                string connStrValue = connStrDic[schemaKey].Trim();
                string schemaValue = schemaDic[schemaKey].Trim();

                if (schemaValue.StartsWith("[") &&
                    schemaValue.EndsWith("]"))
                {
                    //this.SetUserData(schemaKey, connStrValue);
                    this.SetUserData(schemaValue.Substring(1, schemaValue.Length - 2), connStrValue);
                }
            }
        }

        private bool CompareConnectionStringDictionary(
            Dictionary<string, string> connStrDic,
            Dictionary<string, string> schemaDic)
        {
            if (connStrDic.Count != schemaDic.Count)
            {
                return false;
            }

            for (int i = 0; i < schemaDic.Count; i++)
            {
                string connStrKey = DictionaryKey(connStrDic, i);
                string schemaKey = DictionaryKey(schemaDic, i);

                if (connStrKey.ToLower() != schemaKey.ToLower())
                {
                    return false;
                }

                string connStrValue = connStrDic[connStrKey].Trim();
                string schemaValue = schemaDic[schemaKey].Trim();

                if (schemaValue.StartsWith("[") &&
                    schemaValue.EndsWith("]"))
                {
                    continue;
                }

                if (connStrValue.ToLower() != schemaValue.ToLower())
                {
                    return false;
                }
            }
            return true;
        }

        private string DictionaryKey(Dictionary<string, string> dic, int index)
        {
            int i = 0;
            foreach (string key in dic.Keys)
            {
                if (i == index)
                {
                    return key;
                }

                i++;
            }
            return String.Empty;
        }

        #endregion

        #region Static Members

        static public string ParseNpgsqlConnectionString(string connectionString)
        {
            string[] knownKeywords = new string[]
            {
                "host",
                "server",
                "port",
                "database",
                "userid",
                "username",
                "password",

                "pooling",
                "minpoolsize",
                "maxpoolsize",

                "timeout",
                "sslmode"
            };

            Dictionary<string, string> keywordTranslation = new Dictionary<string, string>()
            {
                { "server", "host" },
                { "userid","username" }
            };

            StringBuilder sb = new StringBuilder();

            foreach (var keywordParameter in connectionString.Split(';'))
            {
                if (keywordParameter.Contains("="))
                {
                    var keyword = keywordParameter.Substring(0, keywordParameter.IndexOf("=")).Trim().ToLower();
                    if (knownKeywords.Contains(keyword))
                    {
                        string kp = keywordParameter;
                        if (keywordTranslation.ContainsKey(keyword))
                        {
                            kp = keywordTranslation[keyword] + keywordParameter.Substring(keywordParameter.IndexOf("="));
                        }

                        sb.Append(kp);
                        sb.Append(";");
                    }
                }
            }

            return sb.ToString();
        }

        static public DbConnectionString Build(string providerId,
                                               bool useProviderInConnectinString = true,
                                               string connectionString = null)
        {
            var dbConnectionString = new DbConnectionString()
            {
                ProviderId = providerId,
                UseProviderInConnectionString = useProviderInConnectinString
            };

            if (connectionString != null)
            {
                dbConnectionString.TryFromConnectionString(providerId, connectionString);
            }

            return dbConnectionString;
        }

        #endregion
    }
}
