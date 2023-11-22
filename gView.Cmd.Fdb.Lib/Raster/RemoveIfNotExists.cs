using gView.DataSources.Fdb.ImageDataset;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.Raster;

internal class RemoveIfNotExists
{
    private ICancelTracker _cancelTracker;
    public delegate void ReportActionEvent(RemoveIfNotExists sender, string action);
    public event ReportActionEvent? ReportAction;

    public RemoveIfNotExists(ICancelTracker? cancelTracker)
    {
        _cancelTracker = cancelTracker ?? new CancelTracker();
    }

    async public Task<bool> Run(IFeatureDataset ds)
    {
        AccessFDB? fdb = ds.Database as AccessFDB;

        if (fdb == null)
        {
            return false;
        }

        FDBImageDataset import = new FDBImageDataset(ds.Database as IImageDB, ds.DatasetName);
        var result = await import.RemoveUnexisting();

        if (result)
        {
            IFeatureClass fc = await fdb.GetFeatureclass(ds.DatasetName, $"{ds.DatasetName}_IMAGE_POLYGONS");
            await fdb.CalculateExtent(fc);
        }

        return result;
    }
}
