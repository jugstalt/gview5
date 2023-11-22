using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Core.Extensions;
using gView.Cmd.Fdb.Lib.Data;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;

public class CreateFeatureClassCommand : ICommand
{
    public string Name => "FDB.CreateFeatureClass";

    public string Description => "Creates a new gView Feature Database FeatureClass";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<IFeatureClass>("dataset")
        {
            Description = "FDB Featureclass"
        },
        new RequiredCommandParameter<string>("geometry_type")
        {
            Description = "Geometry Type: [Point, Polyline, Polygon]"
        },
        new RequiredCommandParameter<IEnvelope>("bounds")
        {
            Description = "Spatial Index Bounds"
        },
        new RequiredCommandParameter<int>("max_levels")
        {
            Description = "Maximal Spatial Index Levels"
        },
        new RequiredCommandParameter<string>("fields")
        {
            Description = new FieldsParameterBuilder().ParameterDescriptions?.FirstOrDefault()?.Description ?? String.Empty
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        try
        {
            #region Dataset/FeatureClass Name

            var datasetBuilder = new DatasetParameterBuilder("dataset");
            var dataset = await datasetBuilder.Build<IFeatureDataset>(parameters);

            var fcName = parameters.GetRequiredValue<string>("dataset_fc");

            #endregion

            #region geometryDef

            var geometryDef = new GeometryDef(
                parameters.GetRequiredValue<string>("geometry_type").ToLower() switch
                {
                    "point" => GeometryType.Point,
                    "polyline" => GeometryType.Polyline,
                    "polygon" => GeometryType.Polygon,
                    _ => throw new Exception($"Unknown geomtry type")
                });


            #endregion

            #region Fields

            var fieldsBuilder = new FieldsParameterBuilder();
            var fields = await fieldsBuilder.Build<IFieldCollection>(parameters);

            #endregion

            #region Spatial Index Def

            var envelopeBuilder = new EnvelopeParameterBuilder("bounds");
            var bounds = await envelopeBuilder.Build<IEnvelope>(parameters);

            var maxLevels = parameters.GetRequiredValue<int>("max_levels");

            var binaryTreeDef = new BinaryTreeDef(bounds, maxLevels);

            #endregion

            var creator = new CreateFeatureClass();
            return await creator.Create(dataset,
                                        fcName,
                                        geometryDef,
                                        fields,
                                        binaryTreeDef);
        }
        catch (Exception ex)
        {
            logger?.LogLine($"Error: {ex.Message}");

            return false;
        }
    }
}
