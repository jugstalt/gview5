using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace gView.Framework.IO
{
    public class ConfigConnections
    {
        private string _root = String.Empty;
        private string _encKey = String.Empty;
        private Encoding _encoding = Encoding.Default;

        public ConfigConnections(string name)
        {
            if (name == null)
            {
                return;
            }

            _root = SystemVariables.MyApplicationConfigPath + @"/connections/" + name;
            DirectoryInfo di = new DirectoryInfo(_root);
            if (!di.Exists)
            {
                di.Create();
            }
        }
        public ConfigConnections(string name, string encKey)
            : this(name)
        {
            _encKey = encKey;
        }

        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        public Dictionary<string, string> Connections
        {
            get
            {
                Dictionary<string, string> connections = new Dictionary<string, string>();

                if (String.IsNullOrEmpty(_root))
                {
                    return connections;
                }

                DirectoryInfo di = new DirectoryInfo(_root);
                if (!di.Exists)
                {
                    return connections;
                }

                foreach (FileInfo fi in di.GetFiles("*.con"))
                {
                    StreamReader sr = new StreamReader(fi.FullName, _encoding);
                    string conn = sr.ReadToEnd();
                    sr.Close();

                    string name = InvReplaceSlash(fi.Name.Substring(0, fi.Name.Length - 4));
                    if (String.IsNullOrEmpty(_encKey))
                    {
                        connections.Add(name, conn);
                    }
                    else
                    {
                        byte[] bytes = Convert.FromBase64String(conn);
                        bytes = Crypto.Decrypt(bytes, _encKey);
                        connections.Add(name, _encoding.GetString(bytes));
                    }
                }
                return connections;
            }
        }

        public bool Add(string name, string connectionstring)
        {
            if (String.IsNullOrEmpty(_root))
            {
                return false;
            }

            DirectoryInfo di = new DirectoryInfo(_root);
            if (!di.Exists)
            {
                di.Create();
            }

            if (!String.IsNullOrEmpty(_encKey))
            {
                byte[] bytes = Crypto.Encrypt(_encoding.GetBytes(connectionstring), _encKey);
                connectionstring = Convert.ToBase64String(bytes);
            }

            StreamWriter sw = new StreamWriter(
                Path.Combine(di.FullName, $"{ReplaceSlash(name)}.con"), false, _encoding);

            sw.Write(connectionstring);
            sw.Close();

            return true;
        }

        public bool Remove(string name)
        {
            if (String.IsNullOrEmpty(_root))
            {
                return false;
            }

            DirectoryInfo di = new DirectoryInfo(_root);
            if (!di.Exists)
            {
                di.Create();
            }

            try
            {
                FileInfo fi = new FileInfo(Path.Combine(di.FullName, $"{ReplaceSlash(name)}.con"));
                if (fi.Exists)
                {
                    fi.Delete();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Rename(string oldName, string newName)
        {
            if (String.IsNullOrEmpty(_root))
            {
                return false;
            }

            DirectoryInfo di = new DirectoryInfo(_root);
            if (!di.Exists)
            {
                di.Create();
            }

            try
            {
                FileInfo fi = new FileInfo(Path.Combine(di.FullName, $"{ReplaceSlash(oldName)}.con"));
                if (fi.Exists)
                {
                    fi.MoveTo(Path.Combine(di.FullName, $"{ReplaceSlash(newName)}.con"));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetName(string name)
        {
            if (String.IsNullOrEmpty(_root))
            {
                return String.Empty;
            }

            DirectoryInfo di = new DirectoryInfo(_root);
            if (!di.Exists)
            {
                di.Create();
            }

            string ret = name;

            int i = 2;
            while (di.GetFiles(ReplaceSlash(ret) + ".con").Length != 0)
            {
                ret = name + "(" + i++ + ")";
            }

            return ret;
        }

        private string ReplaceSlash(string name)
        {
            return name.Replace("/", "&slsh;").Replace("\\", "&bkslsh;").Replace(":", "&colon;");
        }
        private string InvReplaceSlash(string name)
        {
            return name.Replace("&slsh;", "/").Replace("&bkslsh;", "\\").Replace("&colon;", ":");
        }
    }
}
