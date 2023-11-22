using gView.Framework.DataExplorer.Abstraction;
using System;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base;

public class ExplorerObjectCls<TParent, TObjectType>
    where TParent : IExplorerObject
{
    private readonly Type _type;

    private bool _hasParent;
    private TParent? _parent;

    public ExplorerObjectCls() : this(0)
    {
        
    }

    public ExplorerObjectCls(int priority)
    {
        _parent = default;
        _type = typeof(TObjectType);
        _hasParent = false;

        this.Priority = priority;
    }

    public ExplorerObjectCls(TParent parent, int priority)
    {
        _parent = parent;
        _type = typeof(TObjectType);
        _hasParent = _parent != null && 
                     typeof(TParent) != typeof(NullParentExplorerObject);
        
        this.Priority = priority;
    }

    public IExplorerObject? ParentExplorerObject
        => _hasParent ? _parent : null;

    public Type? ObjectType
        => _type == typeof(UnknownObjectType) ? null : _type;

    public int Priority { get; }

    protected TParent TypedParent
    {
        get
        {
            if (_parent == null)
            {
                throw new Exception("Parent is not initialized correctly!");
            }

            return _parent;
        }
    }

    protected TParent? TryGetParent() => _parent;

    protected IExplorerObject Parent
    {
        get
        {
            return _parent != null ?
                _parent :
                new NullParentExplorerObject();
        }
        set
        {
            if (value is TParent)
            {
                _parent = (TParent)value;
            }
            else
            {
                _parent = default;
            }
           
            _hasParent = _parent != null && typeof(TParent) != typeof(NullParentExplorerObject);
        }
    }
}

public class ExplorerObjectCls<TParent> 
                : ExplorerObjectCls<TParent, UnknownObjectType>
      where TParent : IExplorerObject
{
    public ExplorerObjectCls() : base() { }
    public ExplorerObjectCls(int priorty) : base(priorty) { }
    public ExplorerObjectCls(TParent parent, int priority) 
        : base(parent, priority) { }
}
