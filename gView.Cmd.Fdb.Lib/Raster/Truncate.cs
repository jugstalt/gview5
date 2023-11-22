using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using System;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.Raster;

internal class Truncate
{
    async public Task Run(IFeatureDataset ds, string fcname)
    {
        AccessFDB? fdb = ds.Database as AccessFDB;

        if (fdb == null)
        {
            return;
        }

        if (!fdb.TruncateTable(fcname))
        {
            throw new Exception(ds.LastErrorMessage);
        }

        IFeatureClass fc = await fdb.GetFeatureclass(ds.DatasetName, $"{ds.DatasetName}_IMAGE_POLYGONS");
        await fdb.CalculateExtent(fc);
        //fdb.RebuildSpatialIndex(ds.DatasetName + "_IMAGE_POLYGONS");

    }
}
