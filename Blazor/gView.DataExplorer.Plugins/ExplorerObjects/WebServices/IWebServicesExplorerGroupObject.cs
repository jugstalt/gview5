using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.WebServices;

public interface IWebServicesExplorerGroupObject : IExplorerObject
{
    void SetParentExplorerObject(IExplorerObject parentExplorerObject);
}
