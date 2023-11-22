using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.EventTable.ContextTools;
using gView.DataSources.EventTable;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.EventTable;

[gView.Framework.system.RegisterPlugIn("B498C801-D7F2-4e1d-AA09-7A3599549DD8")]
public class EventTableObject : ExplorerObjectCls<IExplorerObject, IFeatureClass>, 
                                IExplorerSimpleObject, 
                                IExplorerObjectDeletable, 
                                IExplorerObjectRenamable,
                                IExplorerObjectContextTools
{
    private EventTableConnection? _etconn = null;
    private string _name = String.Empty;
    private IFeatureClass? _fc = null;
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

    public EventTableObject() : base() { }
    public EventTableObject(IExplorerObject parent, string name, EventTableConnection etconn)
        : base(parent, 1)
    {
        _name = name;
        _etconn = etconn;

        _contextTools = new IExplorerObjectContextTool[]
        {
            new UpdateConnectionString()
        };
    }

    public EventTableConnection? GetEventTableConnection() => _etconn;
    public Task<bool> UpdateEventTableConnection(EventTableConnection etconn)
    {
        _etconn = etconn;

        ConfigConnections connStream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        connStream.Add(_name, etconn.ToXmlString());

        return Task.FromResult(true);
    }

    #region IExplorerObject Member

    public string Name
    {
        get
        {
            if (_etconn == null)
            {
                return "???";
            }

            return _name;
        }
    }

    public string FullName
    {
        get
        {
            if (_etconn == null)
            {
                return "???";
            }

            return @$"VectorData\EventTableConnections\{_name}";
        }
    }

    public string Type=>"Database Event Table";

    public string Icon => "basic:table";

    async public Task<object?> GetInstanceAsync()
    {
        if (_fc != null)
        {
            return _fc;
        }

        if (_etconn != null)
        {
            try
            {
                Dataset ds = new Dataset();

                await ds.SetConnectionString(_etconn.ToXmlString());
                if(!await ds.Open())
                {
                    throw new Exception($"Can't open dataset: {ds.LastErrorMessage}");
                }
                _fc = (await ds.Elements())[0].Class as IFeatureClass;
                
                return _fc;
            }
            catch(Exception)  
            {
                _fc = null;

                throw;
            }
        }

        return null;
    }

    #endregion

    #region IExplorerObjectContextTools

    public IEnumerable<IExplorerObjectContextTool> ContextTools => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();

    #endregion

    #region IDisposable Member

    public void Dispose()
    {

    }

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        EventTableGroupObject group = new EventTableGroupObject();
        if (FullName.StartsWith(group.FullName))
        {
            foreach (IExplorerObject exObject in await group.ChildObjects())
            {
                if (exObject.FullName == FullName)
                {
                    cache?.Append(exObject);
                    return exObject;
                }
            }
        }

        return null;
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted = null;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        ConfigConnections stream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        bool ret = stream.Remove(_name);

        if (ret)
        {
            if (ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }
        }
        return Task.FromResult(ret);
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

    public Task<bool> RenameExplorerObject(string newName)
    {
        ConfigConnections stream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        bool ret = stream.Rename(_name, newName);

        if (ret == true)
        {
            _name = newName;
            if (ExplorerObjectRenamed != null)
            {
                ExplorerObjectRenamed(this);
            }
        }
        return Task.FromResult(ret);
    }

    #endregion
}