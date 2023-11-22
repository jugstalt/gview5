using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;

public class RepairSpatialIndexCommand : ICommand
{
    public string Name => "FDB.RepairSpatialIndex";

    public string Description => "Repair spatial index of a featureclasses";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<IFeatureClass>("dataset")
        {
            Description="FDB Featureclass"
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        var featureClassBuilder = new FeatureClassParameterBuilder("dataset");
        var featureClass = await featureClassBuilder.Build<IFeatureClass>(parameters);

        if (featureClass is null)
        {
            throw new Exception("Can't build featureclass");
        }

        if (!(featureClass.Dataset?.Database is AccessFDB))
        {
            throw new Exception("Dataset is not a gView FDB Dataset");
        }

        var fdb = (AccessFDB)featureClass.Dataset.Database;

        logger?.LogLine($"Repair spatial index: {featureClass.Name}");

        int wrongNids = 0, lastPercent = 0;
        bool firstCheck = true, firstUpdate = true;
        if (await fdb.RepairSpatialIndex(featureClass.Name, (sender, args) =>
        {
            if (args is RepairSICheckNodes)
            {
                var checkArgs = (RepairSICheckNodes)args;

                if (firstCheck)
                {
                    logger?.LogLine($"Check {checkArgs.Count} features/nodes...");
                    firstCheck = false;
                }

                wrongNids = checkArgs.WrongNIDs;

                int percent = checkArgs.Pos * 100 / checkArgs.Count;
                if (percent != lastPercent)
                {
                    logger?.Log($" .. {percent}%");
                    lastPercent = percent;
                }
            }
            else if (args is RepairSIUpdateNodes)
            {
                var updateArgs = (RepairSIUpdateNodes)args;

                if(firstUpdate)
                {
                    logger?.LogLine($"Repair {updateArgs.Count} features/nodes...");
                    firstUpdate = false;
                }

                logger?.Log($" .. {updateArgs.Pos}");
            }
        }) == false)
        {
            logger?.LogLine($"FDB.ERROR: {fdb.LastErrorMessage}");
            return false;
        }

        logger?.LogLine($"Repaired SI Nodes: {wrongNids}");

        return true;
    }
}
