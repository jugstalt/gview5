using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Framework.system.UI
{
    public class ExplorerObjectCls
    {
        private IExplorerObject _parent;
        private Type _type = null;

        public ExplorerObjectCls(IExplorerObject parent, Type type, int priority)
        {
            _parent = parent;
            _type = type;
            this.Priority = priority;
        }

        public IExplorerObject ParentExplorerObject
        {
            get { return _parent; }
        }

        public Type ObjectType
        {
            get { return _type; }
        }

        public int Priority { get; }
    }

    public class ExplorerParentObject : ExplorerObjectCls, IExplorerParentObject
    {
        private List<IExplorerObject> _childObjects = null;

        public ExplorerParentObject(IExplorerObject parent, Type type, int priority)
            : base(parent, type, priority)
        {
        }

        #region IExplorerParentObject Member

        async virtual public Task<List<IExplorerObject>> ChildObjects()
        {
            if (_childObjects == null)
            {
                await Refresh();
            }

            return _childObjects;
        }

        async virtual public Task<bool> Refresh()
        {
            await this.DiposeChildObjects();
            _childObjects = new List<IExplorerObject>();

            return true;
        }

        public Task<bool> DiposeChildObjects()
        {
            if (_childObjects == null)
            {
                return Task.FromResult(false);
            }

            foreach (IExplorerObject exObject in _childObjects)
            {
                if (exObject == null)
                {
                    continue;
                }

                exObject.Dispose();
            }
            _childObjects = null;

            return Task.FromResult(true);
        }

        #endregion

        protected void RemoveAllChildObjects()
        {
            if (_childObjects != null)
            {
                _childObjects.Clear();
            }
        }

        protected void AddChildObject(IExplorerObject child)
        {
            if (child == null)
            {
                return;
            }

            if (_childObjects == null)
            {
                _childObjects = new List<IExplorerObject>();
            }

            if (child is IExplorerObjectDeletable)
            {
                ((IExplorerObjectDeletable)child).ExplorerObjectDeleted += new ExplorerObjectDeletedEvent(Child_ExplorerObjectDeleted);
            }

            _childObjects.Add(child);
        }

        protected void SortChildObjects(IComparer<IExplorerObject> comparer)
        {
            if (_childObjects == null || comparer == null)
            {
                return;
            }

            _childObjects.Sort(comparer);
        }
        void Child_ExplorerObjectDeleted(IExplorerObject exObject)
        {
            IExplorerObject delExObject = null;
            foreach (IExplorerObject child in _childObjects)
            {
                if (ExObjectComparer.Equals(child, exObject))
                {
                    delExObject = child;
                    break;
                }
            }
            if (delExObject != null)
            {
                _childObjects.Remove(delExObject);
            }
        }

        virtual public void Dispose()
        {
            DiposeChildObjects();
        }
    }

    public class ExObjectComparer
    {
        static public bool Equals(IExplorerObject ex1, IExplorerObject ex2)
        {
            if (ex1 == null || ex2 == null)
            {
                return false;
            }

            return ex1.GetType().Equals(ex2.GetType())
                && ex1.Name == ex2.Name &&
                ex1.Type == ex2.Type &&
                ex1.FullName == ex2.FullName;
        }
    }
}
