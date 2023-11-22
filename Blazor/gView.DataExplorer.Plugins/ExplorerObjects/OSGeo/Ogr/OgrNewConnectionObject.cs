using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using gView.Framework.system;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

// PG:dbname='postgis' host='localhost' port='5432' user='postgres' password='postgres'
[RegisterPlugIn("b279a036-56c9-499c-99a7-2ec490988be6")]
public class OgrNewConnectionObject : ExplorerObjectCls<OgrDatasetGroupObject>,
                                      IExplorerSimpleObject,
                                      IExplorerObjectDoubleClick,
                                      IExplorerObjectCreatable
{
    public OgrNewConnectionObject()
        : base() { }
    public OgrNewConnectionObject(OgrDatasetGroupObject parent)
        : base(parent, 1) { }

    #region IExplorerSimpleObject Members

    public string Icon => "basic:round-plus";

    #endregion

    #region IExplorerObject Members

    public string Name => "New Connection...";

    public string FullName => "";

    public string Type => "New OGR Connection";

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);
    
    #endregion

    #region IExplorerObjectDoubleClick Members

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        var model = await appScope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.OgrConnectionDialog),
                                                                    "EventTable Connection",
                                                                    new OgrConnectionModel());

        if (model != null)
        {
            ConfigConnections connStream = new ConfigConnections("OGR", "ca7011b3-0812-47b6-a999-98a900c4087d");

            string connectionString = model.ConnectionString;
            string id = "OGR Connection";
            id = connStream.GetName(id);

            connStream.Add(id, connectionString);

            e.NewExplorerObject = new OgrDatasetExplorerObject(this.TypedParent, id, connectionString);
        }
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return (parentExObject is OgrDatasetGroupObject);
    }
    
    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();
        await ExplorerObjectDoubleClick(appScope, e);
        return e.NewExplorerObject;
    }

    #endregion
}
