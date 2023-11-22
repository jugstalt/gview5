using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Fdb.Lib.Data;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;
public class TruncateFeatureClassCommand : ICommand
{
    public string Name => "FDB.TruncateFeatureClass";

    public string Description => "Truncate featureclasses";

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

        logger?.LogLine($"Truncate: {featureClass.Name}");

        var truncate = new TruncateFeatureClass(cancelTracker);
        truncate.ReportAction += (sender, message) =>
        {
            logger?.LogLine(message);
        };
        truncate.Truncate(featureClass);

        return true;
    }
}
