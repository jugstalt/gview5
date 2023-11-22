using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.EventTable.ContextTools;

public class UpdateConnectionString : IExplorerObjectContextTool
{
    #region IExplorerObjectContextTool

    public string Name => "Connection String";

    public string Icon => "basic:edit-database";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return exObject is EventTableObject;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var etconn = ((EventTableObject)exObject).GetEventTableConnection();
        if(etconn==null)
        {
            return false;
        }

        var model = await scope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.EventTableConnection),
                                                                 "EventTable Connection",
                                                                  new EventTableConnectionModel()
                                                                  {
                                                                      ConnectionString = etconn.DbConnectionString,
                                                                      TableName = etconn.TableName,
                                                                      IdFieldName = etconn.IdFieldName,
                                                                      XFieldName = etconn.XFieldName,
                                                                      YFieldName = etconn.YFieldName,
                                                                      SpatialReference = etconn.SpatialReference
                                                                  });

        if (model != null)
        {
            etconn = new DataSources.EventTable.EventTableConnection(
                model.ConnectionString,
                model.TableName,
                model.IdFieldName,
                model.XFieldName,
                model.YFieldName,
                model.SpatialReference);

            return await ((EventTableObject)exObject).UpdateEventTableConnection(etconn);
        }
        
        return false;
    }

    #endregion
}
