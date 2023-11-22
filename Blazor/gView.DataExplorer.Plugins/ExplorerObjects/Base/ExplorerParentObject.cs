using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base;

public class ExplorerParentObject<TParent, TObjectType> : 
                                    ExplorerObjectCls<TParent, TObjectType>, 
                                    IExplorerParentObject
    where TParent : IExplorerObject
{
    private ICollection<IExplorerObject>? _childObjects = null;

    public ExplorerParentObject() : base() { }

    public ExplorerParentObject(int priority) : base(priority) { }

    public ExplorerParentObject(TParent parent, int priority)
        : base(parent, priority)
    {
    }

    #region IExplorerParentObject Member

    async virtual public Task<IEnumerable<IExplorerObject>> ChildObjects()
    {
        if (_childObjects == null)
        {
            await Refresh();
        }

        return _childObjects ?? Array.Empty<IExplorerObject>();
    }
   
    virtual public bool RequireRefresh()
    {
        return _childObjects == null || _childObjects.Count == 0;
    }

    virtual public bool HandleRefreshException(Exception exception)
    {
        AddChildObject(new ExceptionObject(exception));

        return true;
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

    void Child_ExplorerObjectDeleted(IExplorerObject exObject)
    {
        IExplorerObject? delExObject = null;

        if (_childObjects != null)
        {
            foreach (IExplorerObject child in _childObjects)
            {
                if (ExObjectComparer.Equals(child, exObject))
                {
                    delExObject = child;
                    break;
                }
            }
        }

        if (delExObject != null && _childObjects != null)
        {
            _childObjects.Remove(delExObject);
        }
    }

    virtual public void Dispose()
    {
        DiposeChildObjects();
    }
}

public class ExplorerParentObject<TParent> : ExplorerParentObject<TParent, UnknownObjectType> 
    where TParent : IExplorerObject
{
    public ExplorerParentObject() : base() { }
    public ExplorerParentObject(int priority) : base(priority) { }
    public ExplorerParentObject(TParent parent, int priority)
        : base(parent, priority)
    {
    }
}

public class ExplorerParentObject : ExplorerParentObject<IExplorerObject, UnknownObjectType>
{
    public ExplorerParentObject() : base() { }
    public ExplorerParentObject(int priority) : base(priority) { }
}
