using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Framework.DataExplorer.Events;

public delegate void ExplorerObjectRenamedEvent(IExplorerObject exObject);
public delegate void ExplorerObjectDeletedEvent(IExplorerObject exObject);
public delegate void ExplorerObjectCreatedEvent(IExplorerObject exObject);

public class ExplorerObjectEventArgs
{
    //public IExplorerObjectTreeNode Node;
    public IExplorerObject? NewExplorerObject = null;
}
