using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Razor.Base;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;

internal class SetSpatialReference : IExplorerObjectContextTool
{
    public string Name => "Spatial Reference";
    public string Icon => "basic:globe";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return true;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var dataset = await exObject.GetInstanceAsync() as IFeatureDataset;
        if (dataset == null)
        {
            throw new GeneralException("Can't determine feature dataset");
        }

        var _fdb = dataset.Database as AccessFDB;
        if(_fdb == null)
        {
            throw new GeneralException("Database is not a gView FeatureDatabase");
        }

        var model = await scope.ToExplorerScopeService().ShowKnownDialog(
            Framework.Blazor.KnownDialogs.SpatialReferenceDialog,
            model: new BaseDialogModel<ISpatialReference>()
                           {
                               Value = await dataset.GetSpatialReference()
                           }
            );

        if(model?.Value != null)
        {
            int id = await _fdb.CreateSpatialReference(model.Value);
            if (id == -1)
            {
                throw new GeneralException($"Can't create Spatial Reference: {_fdb.LastErrorMessage}");
            }
            if (!await _fdb.SetSpatialReferenceID(dataset.DatasetName, id))
            {
                throw new GeneralException($"Can't set Spatial Reference: {_fdb.LastErrorMessage}");
            }
            dataset.SetSpatialReference(model.Value);
        }

        return true;
    }
}
