using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Core.Extensions;
using gView.DataSources.Fdb.MSAccess;
using gView.DataSources.Fdb.MSSql;
using gView.DataSources.Fdb.PostgreSql;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace gView.Cmd.Fdb.Lib;
public class CreateDatasetCommand : ICommand
{
    public string Name => "FDB.CreateFeatureClass";

    public string Description => "Creates a new gView Feature Database FeatureClass";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<string>("fdb")
        {
            Description = "FDB Type [SqlFDB | pgFDB | SQLiteFDB]"
        },
        new RequiredCommandParameter<string>("connection_string")
        {
            Description = "FDB Connectionstring"
        },
        new RequiredCommandParameter<string>("ds_name")
        {
            Description = "Dataset Name"
        },
        new RequiredCommandParameter<string>("sref_espg")
        {
            Description="EPSG Code <int> for datasets spatial reference system"
        },
        new RequiredCommandParameter<string>("ds_type")
        {
            Description="Dataset Type [FeatureDataset | ImageDataset]"
        },
        new RequiredCommandParameter<IEnvelope>("si_bounds")
        {
            Description = "Spatial Index Bounds"
        },
        new RequiredCommandParameter<int>("si_max_levels")
        {
            Description = "Maximal Spatial Index Levels"
        },
        new RequiredCommandParameter<string>("autofields")
        {
            Description = new AutoFieldsParameterBuilder().ParameterDescriptions?.FirstOrDefault()?.Description ?? String.Empty
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        try
        {

            #region FDB / Connectionstring

            AccessFDB? fdb = null;
            string connectionString = parameters.GetRequiredValue<string>("connection_string");

            switch (parameters.GetRequiredValue<string>("fdb").ToLower())
            {
                case "sql":
                case "sqlfdb":
                    fdb = new SqlFDB();
                    break;
                case "pg":
                case "postgres":
                case "pgfdb":
                    fdb = new pgFDB();
                    break;
                case "sqlite":
                case "sqlitefdb":
                    fdb = new SQLiteFDB();
                    break;
                default:
                    throw new ArgumentException($"Unknown FDB Type {parameters.GetRequiredValue<string>("fdb")}");
            }

            #endregion

            #region SpatialReference

            ISpatialReference spatialReference = new SpatialReference($"epsg:{parameters.GetRequiredValue<int>("sref_epsg")}");

            #endregion

            if (!await fdb.Open(connectionString))
            {
                throw new Exception($"Can't open FDB: {fdb.LastErrorMessage}");
            }

            var datasetType = parameters.GetRequiredValue<string>("ds_type");

            if (datasetType.Equals("FeatureDataset", StringComparison.OrdinalIgnoreCase))
            {
                if (await fdb.CreateDataset(parameters.GetRequiredValue<string>("ds_name"), spatialReference) < 0)
                {
                    throw new Exception($"Unable to create dataset: {fdb.LastErrorMessage}");
                }
            }
            else if (datasetType.Equals("ImageDataset", StringComparison.OrdinalIgnoreCase))
            {
                #region Spatial Index Def

                var envelopeBuilder = new EnvelopeParameterBuilder("si_bounds");
                var bounds = await envelopeBuilder.Build<IEnvelope>(parameters);

                var maxLevels = parameters.GetRequiredValue<int>("si_max_levels");

                var spatialIndexDef = new gViewSpatialIndexDef(bounds, maxLevels);

                #endregion

                #region AutoFields

                var autoFieldsBuilder = new AutoFieldsParameterBuilder();
                var autoFields = await autoFieldsBuilder.Build<IFieldCollection>(parameters);

                #endregion

                if (await fdb.CreateImageDataset(
                    parameters.GetRequiredValue<string>("ds_name"), 
                    spatialReference, spatialIndexDef, string.Empty, autoFields) < 0)
                {
                    throw new Exception($"Unable to create dataset: {fdb.LastErrorMessage}");
                }
            }
            else
            {
                throw new Exception($"Unknown dataset type: {datasetType}");
            }

            return true;
        } 
        catch (Exception ex)
        {
            logger?.LogLine($"ERROR: {ex.Message}");

            return false;
        }
    }
}
