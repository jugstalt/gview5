using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;

namespace gView.Cmd.Fdb.Lib.Data;
internal class TruncateFeatureClass
{
    private ICancelTracker _cancelTracker;

    public delegate void ReportActionEvent(TruncateFeatureClass sender, string action);

    public event ReportActionEvent? ReportAction;

    public TruncateFeatureClass(ICancelTracker? cancelTracker)
    {
        _cancelTracker = cancelTracker ?? new CancelTracker();
    }

    public void Truncate(IClass @class) => Truncate(new[] { @class });

    public void Truncate(IEnumerable<IClass> classes)
    {
        foreach (IClass cl in classes)
        {
            if (!(cl is IFeatureClass) || cl.Dataset == null || !(cl.Dataset.Database is AccessFDB))
            {
                continue;
            }

            ReportAction?.Invoke(this, $"Tuncate featureclass {cl.Name}...");

            AccessFDB? fdb = cl.Dataset.Database as AccessFDB;
            if (fdb == null)
            {
                throw new Exception("Database is not and gView FeatureDatabase");
            }

            fdb.TruncateTable(cl.Name);

            ReportAction?.Invoke(this, $"succeeded");

            if (!_cancelTracker.Continue)
            {
                ReportAction?.Invoke(this, $"canceled");
                return;
            }
        }
    }
}
