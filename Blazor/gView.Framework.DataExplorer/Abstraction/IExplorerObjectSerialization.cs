using System;
using System.Collections.Generic;

namespace gView.Framework.DataExplorer.Abstraction
{
    public interface IExplorerObjectSerialization
    {
        Guid Guid { get; }
        string FullName { get; }
        List<Type> ExplorerObjectTypes { get; }
        List<Type> ObjectTypes { get; }
    }

}
