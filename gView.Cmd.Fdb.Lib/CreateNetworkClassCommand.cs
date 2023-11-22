using gView.Cmd.Core;
using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Cmd.Fdb.Lib.Data;
using gView.Cmd.Fdb.Lib.Model;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.Network;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Fdb.Lib;
public class CreateNetworkClassCommand : ICommand
{
    public string Name => "FDB.CreateNetworkClass";

    public string Description => "Creates a new gView Feature Database NetworkClass";

    public string ExecutableName => "";

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions => new ICommandParameterDescription[]
    {
        new RequiredCommandParameter<string>("config")
        {
            Description = "Network Config XML File"
        }
    };

    async public Task<bool> Run(IDictionary<string, object> parameters, ICancelTracker? cancelTracker = null, ICommandLogger? logger = null)
    {
        try
        {
            var configFile = parameters.GetRequiredValue<string>("config");

            XmlStream stream = new XmlStream("network");
            stream.ReadStream(configFile);

            var commandModel = new CreateNetworkModel();
            commandModel.Load(stream);

            #region Create Dataset

            var dataset = PlugInManager.Create(commandModel.DatasetGuid) as IFeatureDataset;
            if (dataset == null)
            {
                throw new Exception("Plugin is not a FeatureDataset");
            }
            await dataset.SetConnectionString(commandModel.ConnectionString);
            if (await dataset.Open() == false)
            {
                throw new Exception($"Can't open dataset: {dataset.LastErrorMessage}");
            }

            var fdb = dataset?.Database as AccessFDB;
            if (fdb == null)
            {
                throw new Exception("Dataset is not a valid gView Feature Database");
            }

            int datasetId = await fdb.DatasetID(dataset.DatasetName);

            #endregion

            #region Edges/Nodes

            List<IFeatureClass> edges = new List<IFeatureClass>();
            List<IFeatureClass> nodes = new List<IFeatureClass>();
            List<int> complexEdges = new List<int>();
            Dictionary<int, string> switchNodeFcIdAndFieldnames = new();
            Dictionary<int, NetworkNodeType>? nodeTypeFcIds = new();

            if (commandModel.Edges != null)
            {
                foreach (var edge in commandModel.Edges)
                {
                    var edgeFc = (await dataset!.Element(edge.Name))?.Class as IFeatureClass;
                    if (edgeFc == null || edgeFc.GeometryType != Framework.Geometry.GeometryType.Polyline)
                    {
                        throw new Exception($"{edge.Name} is not a valid Edge Featureclass");
                    }

                    edges.Add(edgeFc);

                    if (edge.IsComplexEdge)
                    {
                        complexEdges.Add(await fdb.FeatureClassID(datasetId, edgeFc.Name));
                    }
                }
            }

            if (commandModel.Nodes != null)
            {
                foreach (var node in commandModel.Nodes)
                {
                    var nodeFc = (await dataset!.Element(node.Name))?.Class as IFeatureClass;
                    if (nodeFc == null || nodeFc.GeometryType != Framework.Geometry.GeometryType.Point)
                    {
                        throw new Exception($"{node.Name} is not a valid Node Featureclass");
                    }

                    nodes.Add(nodeFc);

                    if (node.IsSwitch)
                    {
                        switchNodeFcIdAndFieldnames.Add(await fdb.FeatureClassID(datasetId, nodeFc.Name), node.FieldName);
                    }
                    if (node.NodeType != NetworkNodeType.Unknown)
                    {
                        nodeTypeFcIds.Add(await fdb.FeatureClassID(datasetId, nodeFc.Name), node.NodeType);
                    }
                }
            }

            #endregion

            var creator = new NetworkClassCreator(dataset!, commandModel.Name, edges, nodes);
            
            creator.DeleteIfAlreadyExists = commandModel.DeleteIfAlredyExists;

            if (commandModel.UseSnapTolerance)
            {
                creator.SnapTolerance = commandModel.SnapTolerance;
            }
            if(complexEdges.Count > 0)
            {
                creator.ComplexEdgeFcIds = complexEdges;
            }
            if (switchNodeFcIdAndFieldnames.Count > 0)
            {
                creator.SwitchNodeFcIdAndFieldnames = switchNodeFcIdAndFieldnames;
            }
            if (nodeTypeFcIds.Count > 0)
            {
                creator.NodeTypeFcIds = nodeTypeFcIds;
            }
            if (commandModel.Weights.Count > 0)
            {
                creator.GraphWeights = commandModel.Weights;
            }

            if (logger != null)
            {
                string lastMessage = "";
                int lastPercentage = -1;
                creator.ReportProgress += (Framework.UI.ProgressReport progressEventReport) =>
                {
                    if (cancelTracker?.Continue == false)
                    {
                        throw new Exception("Operation is canceled");
                    }

                    if (!String.IsNullOrEmpty(progressEventReport.Message) && progressEventReport.Message != lastMessage)
                    {
                        logger.LogLine("");
                        logger.Log(lastMessage = progressEventReport.Message);
                        if (progressEventReport.featureMax > 0)
                        {
                            logger.Log($" ({progressEventReport.featureMax})");
                        }
                        logger.LogLine("");
                        lastPercentage = -1;
                    }

                    if (progressEventReport.featureMax > 0)
                    {
                        int percentage = progressEventReport.featurePos * 100 / progressEventReport.featureMax;
                        if (percentage != lastPercentage)
                        {
                            logger.Log($" .. {lastPercentage = percentage}%");
                        }
                    }
                };
            }

            await creator.Run();

            return true;
        }
        catch (Exception ex)
        {
            logger?.LogLine($"Error: {ex.Message}");

            return false;
        }
    }
}
