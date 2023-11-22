using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Fdb.Lib.SpatialIndex;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;

public class ShrinkDatasetSpatialIndexCommand : ICommand
{
    public string Name => "FDB.ShrinkDatasetSpatialIndex";

    public string Description => "Shrink spatial index of all featureclasses in a dataset";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<IDataset>("dataset")
        {
            Description="FDB Dataset"
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        var datasetBuilder = new DatasetParameterBuilder("dataset");
        var dataset = await datasetBuilder.Build<IFeatureDataset>(parameters);

        if (dataset is null)
        {
            throw new Exception("Can't build dataset");
        }

        if (!(dataset.Database is AccessFDB))
        {
            throw new Exception("Dataset is not a gView FDB Dataset");
        }

        var featureClasses = (await dataset.Elements())
            .Where(e => e.Class is not null)
            .Select(e => e.Class);

        var shrinker = new Shrinker(cancelTracker);
        shrinker.ReportAction += (sender, message) =>
        {
            logger?.LogLine(message);
        };

        await shrinker.RebuildIndicesAsync(featureClasses);

        return true;
    }
}
