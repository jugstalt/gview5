using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.SpatialIndex;

internal class Shrinker
{
    private ICancelTracker _cancelTracker;

    public delegate void ReportActionEvent(Shrinker sender, string action);

    public event ReportActionEvent? ReportAction;

    public Shrinker(ICancelTracker? cancelTracker)
    {
        _cancelTracker = cancelTracker ?? new CancelTracker();
    }

    public Task RebuildIndicesAsync(IClass @class) => RebuildIndicesAsync(new[] { @class });

    async public Task RebuildIndicesAsync(IEnumerable<IClass> classes)
    {
        foreach (IClass cl in classes)
        {
            if (!(cl is IFeatureClass) || cl.Dataset == null || !(cl.Dataset.Database is AccessFDB))
            {
                continue;
            }

            ReportAction?.Invoke(this, $"Shrink spatial index for featureclass {cl.Name}...");

            AccessFDB? fdb = cl.Dataset.Database as AccessFDB;
            if (fdb == null)
            {
                throw new Exception("Database is not and gView FeatureDatabase");
            }
            if (!await fdb.ShrinkSpatialIndex(cl.Name))
            {
                throw new Exception($"Error rebuilding {cl.Name} index:\n{fdb.LastErrorMessage}");
            }
            ReportAction?.Invoke(this, $"succeeded");

            if (!_cancelTracker.Continue)
            {
                ReportAction?.Invoke(this, $"canceled");
                return;
            }
        }
    }
}
