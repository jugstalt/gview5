﻿using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Framework.Sys.UI
{
    public class ExplorerObjectManager : ISerializableExplorerObjectCache
    {
        private List<IExplorerObject> _exObjectsCache = new List<IExplorerObject>();

        public void Dispose()
        {
            foreach (IExplorerObject exObject in _exObjectsCache)
            {
                exObject.Dispose();
            }
            _exObjectsCache.Clear();
        }

        private IExplorerObject GetExObjectFromCache(string FullName)
        {
            foreach (IExplorerObject exObject in _exObjectsCache)
            {
                if (exObject == null)
                {
                    continue;
                }

                if (exObject.FullName == FullName)
                {
                    return exObject;
                }
            }
            return null;
        }
        async public Task<IExplorerObject> DeserializeExplorerObject(Guid guid, string FullName)
        {
            IExplorerObject cached = GetExObjectFromCache(FullName);
            if (cached != null)
            {
                return cached;
            }

            PlugInManager compManager = new PlugInManager();
            object obj = compManager.CreateInstance(guid);
            if (!(obj is ISerializableExplorerObject))
            {
                return null;
            }

            return await ((ISerializableExplorerObject)obj).CreateInstanceByFullName(FullName, this);
        }
        async public Task<IExplorerObject> DeserializeExplorerObject(IExplorerObjectSerialization exObjectSerialization)
        {
            try
            {
                if (exObjectSerialization == null)
                {
                    return null;
                }

                return await DeserializeExplorerObject(
                    exObjectSerialization.Guid,
                    exObjectSerialization.FullName);
            }
            catch { return null; }
        }

        async public Task<List<IExplorerObject>> DeserializeExplorerObject(IEnumerable<IExplorerObjectSerialization> list)
        {
            List<IExplorerObject> l = new List<IExplorerObject>();
            if (list == null)
            {
                return l;
            }

            foreach (IExplorerObjectSerialization ser in list)
            {
                IExplorerObject exObject = await DeserializeExplorerObject(ser);
                if (exObject == null)
                {
                    continue;
                }

                l.Add(exObject);
            }
            return l;
        }
        static public ExplorerObjectSerialization SerializeExplorerObject(IExplorerObject exObject)
        {
            if (!(exObject is ISerializableExplorerObject))
            {
                return null;
            }
            else
            {
                return new ExplorerObjectSerialization(exObject);
            }
        }

        #region ISerializableExplorerObjectCache Member

        public void Append(IExplorerObject exObject)
        {
            if (exObject == null || Contains(exObject.FullName))
            {
                return;
            }

            _exObjectsCache.Add(exObject);
        }

        public bool Contains(string FullName)
        {
            foreach (IExplorerObject exObject in _exObjectsCache)
            {
                if (exObject == null)
                {
                    continue;
                }

                if (exObject.FullName == FullName)
                {
                    return true;
                }
            }
            return false;
        }

        public IExplorerObject this[string FullName]
        {
            get
            {
                return GetExObjectFromCache(FullName);
            }
        }

        #endregion
    }
}
