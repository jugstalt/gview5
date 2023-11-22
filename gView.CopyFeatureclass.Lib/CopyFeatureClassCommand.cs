using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Builders;
using gView.Cmd.Core.Extensions;
using gView.CopyFeatureclass.Lib.Fdb;
using gView.CopyFeatureclass.Lib.Features;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.Offline;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.CopyFeatureclass.Lib;
public class CopyFeatureClassCommand : ICommand
{
    public string Name => "CopyFeatureClass";

    public string Description => "Copies a featureclass from one datasoure to another";

    public string ExecutableName => "gView.Cmd.CopyFeatureclass.exe";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<IFeatureClass>("source")
        {
            Description="Source FeatureClass"
        },
        new RequiredCommandParameter<IFeatureClass>("dest")
        {
            Description="Destination Featureclass"
        },
        new CommentParameter("optional:"),
        new CommandParameter<string>("source_geometrytype")
        {
            Description="<Point,Polyline,Polygon> ... if source geometrytype is not explizit specified (SQL Geometry)"
        },
        new CommentParameter("optional, when check out featureclass:"),
        new CommandParameter<string>("checkout")
        {
            Description="<Description> ... Write checkout information"
        },
        new CommandParameter<string>("ch")
        {
            Description="<none|normal|parent_wins|child_wins|newer_wins> ... conflict handling"
        },
        new CommandParameter<string>("pr")
        {
            Description="parent rights. <iud|iu|ud|...> (i..INSERT, u..UPDATE, d..DELETE)"
        },
        new CommandParameter<string>("cr")
        {
            Description="child rights.  <iud|iu|ud|...> (i..INSERT, u..UPDATE, d..DELETE)"
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        var sourceBuilder = new FeatureClassParameterBuilder("source");
        var destBuilder = new FeatureClassParameterBuilder("dest");

        IFeatureDataset? sourceDataset = null;
        IFeatureDataset? destDataset = null;

        string[] sourceFields = parameters.GetArray<string>("sourcefields").ToArray(),
                 destFields = parameters.GetArray<string>("destfields").ToArray();

        GeometryType? sourceGeometryType = null;

        bool checkout = parameters.HasKey("checkout");
        string? checkoutDescription = parameters.GetValue<string>("checkout");

        try
        {
            #region Open Source/Dest Dataset

            sourceDataset = await sourceBuilder.Build<IFeatureDataset>(parameters);
            var sourceFC = await sourceBuilder.BuildFeatureClass(sourceDataset, parameters);
            if (sourceFC is null)
            {
                throw new Exception($"Can't find featureclass in source dataset...");
            }

            destDataset = await destBuilder.Build<IFeatureDataset>(parameters);

            string destFcName = parameters.GetRequiredValue<string>("dest_fc");

            #endregion

            if (String.IsNullOrWhiteSpace(sourceFC.IDFieldName))
            {
                logger?.LogLine("WARNING: Souorce FeatureClass has no IDField -> Bad performance!!");
            }
            logger?.LogLine("Source FeatureClass: " + sourceFC.Name);
            logger?.LogLine("-----------------------------------------------------");
            logger?.LogLine("Shape Field: " + sourceFC.ShapeFieldName);
            if (String.IsNullOrWhiteSpace(sourceFC.IDFieldName))
            {
                logger?.LogLine("WARNING: Souorce FeatureClass has no IDField -> Bad performance!!");
            }
            else
            {
                logger?.LogLine("Id Field   : " + sourceFC.IDFieldName);
            }

            logger?.LogLine("");
            logger?.LogLine("Import: " + sourceFC.Name);
            logger?.LogLine("-----------------------------------------------------");

            #region FieldMapping/Translation

            FieldTranslation fieldTranslation = new FieldTranslation();
            if (sourceFields.Length > 0 && destFields.Length > 0)
            {
                if (sourceFields.Length != destFields.Length)
                {
                    throw new Exception("Error in field definition...");
                }

                for (int i = 0; i < sourceFields.Length; i++)
                {
                    IField field = sourceFC.FindField(sourceFields[i]);

                    if (field == null)
                    {
                        throw new Exception("Error: Can't find field '" + sourceFields[i] + "'...");
                    }

                    fieldTranslation.Add(field, destFields[i]);
                }
            }
            else
            {
                foreach (IField field in sourceFC.Fields.ToEnumerable())
                {
                    if (field.type == FieldType.ID ||
                        field.type == FieldType.Shape)
                    {
                        continue;
                    }

                    fieldTranslation.Add(field, FieldTranslation.CheckName(field.name));
                }
            }

            #endregion

            if (destDataset.Database is not IFeatureDatabase)
            {
                throw new Exception("Destination dataset has no feature database. Can't create featureclasses for this kind of dataset...");
            }

            #region Checkout Featureclass

            string replIDField = String.Empty;
            if (checkout)
            {
                if (!(destDataset.Database is IFeatureDatabaseReplication) ||
                    !(sourceDataset.Database is IFeatureDatabaseReplication))
                {
                    throw new Exception("Can't checkout FROM/TO databasetype...");
                }

                replIDField = await Replication.FeatureClassReplicationIDFieldname(sourceFC);
                if (String.IsNullOrEmpty(replIDField))
                {
                    throw new Exception("Can't checkout from source featureclass. No replication ID!");
                }

                IDatasetElement element = await destDataset.Element(destFcName);
                if (element != null)
                {
                    List<Guid> checkout_guids = await Replication.FeatureClassSessions(element.Class as IFeatureClass);
                    if (checkout_guids != null && checkout_guids.Count != 0)
                    {
                        string errMsg = "Can't check out to this featureclass\n";
                        errMsg += "Check in the following Sessions first:\n";
                        foreach (Guid g in checkout_guids)
                        {
                            errMsg += "   CHECKOUT_GUID: " + g.ToString();
                        }

                        throw new Exception($"ERROR: {errMsg}");
                    }
                }
            }

            #endregion

            if (parameters.HasKey("source_geometrytype"))
            {
                GeometryType geomType;
                if (Enum.TryParse<GeometryType>(parameters.GetValue<string>("source_geometry"), out geomType))
                {
                    sourceGeometryType = geomType;
                    Console.WriteLine("Source geometry type: " + sourceGeometryType);
                }
            }

            if (destDataset.Database is AccessFDB)
            {
                #region Copy to FDB

                FdbImport import = new FdbImport(cancelTracker, ((IFeatureUpdater)destDataset.Database).SuggestedInsertFeatureCountPerTransaction);
                import.ReportAction += (sender, action) => logger?.LogLine(action);
                import.ReportProgress += (sender, progress) => logger?.Log($" .. {progress}");

                ISpatialIndexDef? treeDef = null;

                if (checkout)
                {
                    if (sourceDataset.Database is AccessFDB)
                    {
                        treeDef = await ((AccessFDB)sourceDataset.Database).FcSpatialIndexDef(sourceFC.Name);
                        if (destDataset.Database is AccessFDB)
                        {
                            ISpatialIndexDef dsTreeDef = await ((AccessFDB)destDataset.Database).SpatialIndexDef(destDataset.DatasetName);
                            if (treeDef.GeometryType != dsTreeDef.GeometryType)
                            {
                                treeDef = dsTreeDef;
                            }
                        }
                    }
                }

                if (!await import.ImportToNewFeatureclass((IFeatureDatabase)destDataset.Database,
                                                           destDataset.DatasetName,
                                                           parameters.GetRequiredValue<string>("dest_fc"),
                                                           sourceFC,
                                                           fieldTranslation,
                                                           project: true,
                                                           filters: null,
                                                           sIndexDef: treeDef,
                                                           sourceGeometryType: sourceGeometryType))
                {
                    throw new Exception($"ERROR: {import.lastErrorMsg}");
                }

                #endregion
            }
            else
            {
                #region Copy to general Database

                logger?.LogLine("Create: " + destFcName);
                logger?.LogLine("-----------------------------------------------------");

                FeatureImport import = new FeatureImport(cancelTracker);

                import.ReportAction += (sender, action) => logger?.LogLine(action);
                import.ReportProgress += (sender, progress) => logger?.Log($"...{progress}");

                if (!await import.ImportToNewFeatureclass(destDataset,
                                                          destFcName,
                                                          sourceFC,
                                                          fieldTranslation,
                                                          true,
                                                          sourceGeometryType: sourceGeometryType))
                {
                    throw new Exception($"ERROR: {import.lastErrorMsg}");
                }

                #endregion
            }

            #region Checkout ...

            if (checkout)
            {
                IDatasetElement element = await destDataset.Element(destFcName);
                if (element == null)
                {
                    throw new Exception("ERROR: Can't write checkout information...");
                }
                IFeatureClass? destFC = element.Class as IFeatureClass;

                if (!await Replication.InsertReplicationIDFieldname(destFC, replIDField))
                {
                    throw new Exception("ERROR: at InsertReplicationIDFieldname");
                }

                Replication.VersionRights cr = Replication.VersionRights.NONE;
                Replication.VersionRights pr = Replication.VersionRights.NONE;
                Replication.ConflictHandling ch = Replication.ConflictHandling.NORMAL;

                string parent_rights = parameters.GetValue<string>("cr") ?? string.Empty;
                string child_rights = parameters.GetValue<string>("pr") ?? string.Empty;
                string conflict_handling = parameters.GetValue<string>("ch") ?? string.Empty;

                if (child_rights.ToLower().Contains("i"))
                {
                    cr |= Replication.VersionRights.INSERT;
                }

                if (child_rights.ToLower().Contains("u"))
                {
                    cr |= Replication.VersionRights.UPDATE;
                }

                if (child_rights.ToLower().Contains("d"))
                {
                    cr |= Replication.VersionRights.DELETE;
                }

                if (parent_rights.ToLower().Contains("i"))
                {
                    pr |= Replication.VersionRights.INSERT;
                }

                if (parent_rights.ToLower().Contains("u"))
                {
                    pr |= Replication.VersionRights.UPDATE;
                }

                if (parent_rights.ToLower().Contains("d"))
                {
                    pr |= Replication.VersionRights.DELETE;
                }

                switch (conflict_handling.ToLower())
                {
                    case "none":
                        ch = Replication.ConflictHandling.NONE;
                        break;
                    case "normal":
                        ch = Replication.ConflictHandling.NORMAL;
                        break;
                    case "parent_wins":
                        ch = Replication.ConflictHandling.PARENT_WINS;
                        break;
                    case "child_wins":
                        ch = Replication.ConflictHandling.CHILD_WINS;
                        break;
                    case "newer_wins":
                        ch = Replication.ConflictHandling.NEWER_WINS;
                        break;
                }

                if (!await Replication.InsertNewCheckoutSession(sourceFC,
                    pr,
                    destFC,
                    cr,
                    ch,
                    ReplicationInformation.Replace(checkoutDescription)))
                {
                    throw new Exception("ERROR: at InsertNewCheckoutSession");
                }

                if (!await Replication.InsertCheckoutLocks(sourceFC, destFC))
                {
                    throw new Exception("ERROR: at  InsertCheckoutLocks");
                }
            }

            #endregion

            return true;
        }
        catch (Exception ex)
        {
            logger?.LogLine("Exception:");
            logger?.LogLine("-----------------------------------------------------");
            logger?.LogLine(ex.Message);

            return false;
        }
        finally
        {
            if (sourceDataset is not null)
            {
                sourceDataset.Dispose();
            }

            if (destDataset is not null)
            {
                destDataset.Dispose();
            }
        }
    }
}
  