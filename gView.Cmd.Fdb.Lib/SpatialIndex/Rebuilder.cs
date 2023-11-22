using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib.SpatialIndex;
internal class Rebuilder
{
    private ICancelTracker _cancelTracker;

    public delegate void ReportActionEvent(Rebuilder sender, string action);

    public event ReportActionEvent? ReportAction;

    public Rebuilder(ICancelTracker? cancelTracker)
    {
        _cancelTracker = cancelTracker ?? new CancelTracker();
    }

    public Task RebuildIndicesAsync(IClass @class, BinaryTreeDef binaryTreeDef) => RebuildIndicesAsync(new[] { @class }, binaryTreeDef);

    async public Task RebuildIndicesAsync(IEnumerable<IClass> classes, BinaryTreeDef binaryTreeDef)
    {
        foreach (IClass cl in classes)
        {
            if (!(cl is IFeatureClass) || cl.Dataset == null || !(cl.Dataset.Database is AccessFDB))
            {
                continue;
            }

            ReportAction?.Invoke(this, $"Rebuild spatial index for featureclass {cl.Name}...");

            AccessFDB? fdb = cl.Dataset.Database as AccessFDB;
            if (fdb == null)
            {
                throw new Exception("Database is not and gView FeatureDatabase");
            }

            bool calc = false, update = false;
            string lastMessage = "";
            if (!await fdb.RebuildSpatialIndexDef(cl.Name, binaryTreeDef, (sender, args) =>
                        {
                            if (calc == false && args is UpdateSICalculateNodes)
                            {
                                calc = true;
                                ReportAction?.Invoke(this, "Calculate SI Nodes");
                            }
                            else if(update==false && args is UpdateSIUpdateNodes)
                            {
                                update = true;
                                ReportAction?.Invoke(this, "Update SI Nodes");
                            }

                            var message = args switch
                            {
                                UpdateSICalculateNodes cn => $" .. {cn.Pos * 100 / cn.Count}%",
                                UpdateSIUpdateNodes un => $" .. {un.Pos * 100 / un.Count}%",
                                _ => ""
                            };

                            if (lastMessage != message)
                            {
                                ReportAction?.Invoke(this, lastMessage = message);
                            }
                        }))
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
