using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class ExplorerDialogFilter
{
    public bool BrowseAll = false;

    private List<Guid> _guids = new List<Guid>();
    private string _name;

    public ExplorerDialogFilter(string name)
    {
        _name = name;
    }

    public List<Guid> ExplorerObjectGUIDs { get { return _guids; } }

    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    public override string ToString()
    {
        return Name;
    }

    public virtual Task<bool> Match(IExplorerObject exObject)
    {
        if (exObject == null)
        {
            return Task.FromResult(false);
        }

        bool found = false;
        foreach (Guid guid in ExplorerObjectGUIDs)
        {
            if (PlugInManager.PlugInID(exObject) == guid)
            {
                found = true;
                break;
            }
        }
        return Task.FromResult(found);
    }

    public virtual object? FilterObject
    {
        get { return null; }
    }

    public virtual IEnumerable<IExplorerObject> FilterExplorerObjects(IEnumerable<IExplorerObject> explorerObjects)
    {
        return explorerObjects;
    }
}
