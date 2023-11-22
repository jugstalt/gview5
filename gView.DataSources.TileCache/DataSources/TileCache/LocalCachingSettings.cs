using gView.Framework.IO;
using gView.Framework.system;
using System;

namespace gView.DataSources.TileCache
{
    public class LocalCachingSettings
    {
        static private bool _loaded = false;
        static private bool _use = false;
        static private string _folder = String.Empty;

        #region Properties

        static public bool UseLocalCaching
        {
            get
            {
                if (!_loaded)
                {
                    Load();
                }

                return _use;
            }
            set
            {
                _use = value;
            }
        }
        static public string LocalCachingFolder
        {
            get
            {
                if (!_loaded)
                {
                    Load();
                }

                return _folder;
            }
            set
            {
                _folder = value;
            }
        }

        #endregion

        static private void Load()
        {
            try
            {
                _loaded = true;

                XmlStream stream = new XmlStream("localcaching");
                if (!stream.ReadStream(System.IO.Path.Combine(SystemVariables.CommonApplicationData, "options_tilecache_local_caching.xml")))
                {
                    #region Initialize with defaults and reload ...

                    stream = new XmlStream("localcaching");
                    stream.Save("use", (bool)true);
                    stream.Save("folder", System.IO.Path.Combine(SystemVariables.CommonApplicationData, "temp", "tilecache"));
                    stream.WriteStream(System.IO.Path.Combine(SystemVariables.CommonApplicationData, "options_tilecache_local_caching.xml"));

                    stream = new XmlStream("localcaching");
                    stream.ReadStream(System.IO.Path.Combine(SystemVariables.CommonApplicationData, "options_tilecache_local_caching.xml"));

                    #endregion
                }

                _use = (bool)stream.Load("use", (bool)true);
                _folder = (string)stream.Load("folder", SystemVariables.CommonApplicationData + @"\temp\tilecache");
            }
            catch (Exception /*ex*/)
            {
                _loaded = false;
            }
        }

        static public bool Commit()
        {
            try
            {
                XmlStream stream = new XmlStream("localcaching");
                stream.Save("use", _use);
                stream.Save("folder", _folder);
                stream.WriteStream(System.IO.Path.Combine(SystemVariables.CommonApplicationData, "options_tilecache_local_caching.xml"));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
