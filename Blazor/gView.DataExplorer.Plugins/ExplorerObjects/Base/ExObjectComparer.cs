using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base;

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
