using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerParentObject
{
    Task<IEnumerable<IExplorerObject>> ChildObjects();
    bool RequireRefresh();
    bool HandleRefreshException(Exception exception);

    Task<bool> Refresh();
    Task<bool> DiposeChildObjects();
}
