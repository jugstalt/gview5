using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Core.Extensions;
using gView.Cmd.Fdb.Lib.SpatialIndex;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;

public class RebuildSpatiallIndexDefCommand : ICommand
{
    public string Name => "FDB.RebuildSpatiallIndexDef";

    public string Description => "Rebuild spatial index of a featureclasses";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<IFeatureClass>("dataset")
        {
            Description="FDB Featureclass"
        },
        new RequiredCommandParameter<IEnvelope>("bounds")
        {
            Description="Spatial Index Bounds"
        },
        new RequiredCommandParameter<int>("max_levels")
        {
            Description="Maximal Spatial Index Levels"
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

        var envelopeBuilder = new EnvelopeParameterBuilder("bounds");
        var bounds = await envelopeBuilder.Build<IEnvelope>(parameters);

        var maxLevels = parameters.GetRequiredValue<int>("max_levels");

        var binaryTreeDef = new BinaryTreeDef(bounds, maxLevels);

        var rebuilder = new Rebuilder(cancelTracker);
        rebuilder.ReportAction += (sender, message) =>
        {
            if (message.Contains("%"))
            {
                logger?.Log(message);
            }
            else
            {
                logger?.LogLine("");
                logger?.LogLine(message);
            }
        };
        await rebuilder.RebuildIndicesAsync(featureClass, binaryTreeDef);

        return true;
    }
}
