using System.Collections.Generic;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerObjectContextTools
{
    IEnumerable<IExplorerObjectContextTool> ContextTools { get; }
}
