using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class ExplorerOpenDialogFilter : ExplorerDialogFilter
{
    public ExplorerOpenDialogFilter(string name)
        : base(name)
    {
    }
    private List<Type> _types = new List<Type>();

    public List<Type> ObjectTypes { get { return _types; } }

    async public override Task<bool> Match(IExplorerObject exObject)
    {
        Type? objType = exObject.ObjectType;
        if (exObject == null)
        {
            return false;
        }

        bool found = false;
        if (objType != null)
        {
            foreach (Type type in ObjectTypes)
            {
                if (objType == type)
                {
                    found = true;
                    break;
                }
                foreach (Type inter in objType.GetInterfaces())
                {
                    if (inter == type)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }
        }
        if (!found)
        {
            found = await base.Match(exObject);
        }

        return found;
    }
}
