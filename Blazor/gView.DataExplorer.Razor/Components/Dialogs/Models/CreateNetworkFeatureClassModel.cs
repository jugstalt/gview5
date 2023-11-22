using gView.Blazor.Models.Dialogs;
using gView.Cmd.Fdb.Lib.Model;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.Network;
using gView.Framework.system;
using System.Text;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class CreateNetworkFeatureClassModel : IDialogResultItem
{
    private readonly IApplicationScope? _applicationScope;
    private readonly string _exObjectFullname;

    public CreateNetworkFeatureClassModel()
    {
        _applicationScope = null;
        _exObjectFullname = string.Empty;
    }

    public CreateNetworkFeatureClassModel(IApplicationScope? scope = null,
                                          string exObjectFullname = "")
    {
        _applicationScope = scope;
        _exObjectFullname = exObjectFullname;
    }

    public CreateNetworkFeatureClassModel(IApplicationScope? scope,
                                          string exObjectFullname,
                                          IFeatureDataset featureDataset)
        : this(scope, exObjectFullname)
    {
        FeatureDataset = featureDataset;
    }

    public ResultClass Result { get; } = new ResultClass();

    public IFeatureDataset? FeatureDataset { get; set; }

    #region Methods

    public bool HasExistingModelNames()
    {
        if (String.IsNullOrEmpty(_exObjectFullname) || _applicationScope == null)
        {
            return false;
        }

        return _applicationScope.GetToolConfigFiles("network", "create",
               _exObjectFullname.ToLower().ToSHA256Hash())
               .Any();
    }

    public IEnumerable<string> GetExistingModelNames()
    {
        if (String.IsNullOrEmpty(_exObjectFullname) || _applicationScope == null)
        {
            return Array.Empty<string>();
        }

        return _applicationScope.GetToolConfigFiles("network", "create",
               _exObjectFullname.ToLower().ToSHA256Hash())
               .Select(filename => Path.GetFileNameWithoutExtension(filename));
    }

    async public Task<string> LoadFromExisting(string name)
    {
        if (_applicationScope == null)
        {
            throw new Exception("Can't load file. Appliation Scope is not set");
        }

        if (this.FeatureDataset == null)
        {
            throw new Exception("Can't laod file. FeatureDataset is NULL");
        }

        StringBuilder warnings = new();
        string filename = _applicationScope.GetToolConfigFilename("network", "create",
            _exObjectFullname.ToLower().ToSHA256Hash(), $"{name}.xml");

        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"File not found: {filename}");
        }

        XmlStream stream = new XmlStream("network");
        stream.ReadStream(filename);

        var commandModel = new CreateNetworkModel();
        commandModel.Load(stream);

        this.Result.Reset();
        this.Result.Name = name;
        this.Result.DeleteExisting = commandModel.DeleteIfAlredyExists;

        if (commandModel.Edges != null)
        {
            foreach (var edge in commandModel.Edges)
            {
                IFeatureClass? fc = (await FeatureDataset.Element(edge.Name)).Class as IFeatureClass;
                if (fc == null)
                {
                    warnings.AppendLine($"Edge Featureclass {edge.Name} not found in dataset");
                    continue;
                }
                if (fc.GeometryType != Framework.Geometry.GeometryType.Polyline)
                {
                    warnings.AppendLine($"Edge Featureclass {edge.Name} has the wrong geometry type {fc.GeometryType}.");
                    continue;
                }

                this.Result.EdgeFeatureClasses.Add(fc);
                if (edge.IsComplexEdge)
                {
                    this.Result.ComplexEdges.Add(fc);
                }
            }
            this.Result.UseComplexEdges = this.Result.ComplexEdges.Count > 0;
        }

        if (commandModel.Nodes != null)
        {
            foreach (var node in commandModel.Nodes)
            {
                IFeatureClass? fc = (await FeatureDataset.Element(node.Name)).Class as IFeatureClass;
                if (fc == null)
                {
                    warnings.AppendLine($"Node Featureclass {node.Name} not found in dataset");
                    continue;
                }
                if (fc.GeometryType != Framework.Geometry.GeometryType.Polyline)
                {
                    warnings.AppendLine($"Node Featureclass {node.Name} has the wrong geometry type {fc.GeometryType}.");
                    continue;
                }

                this.Result.NodeFeatureClasses.Add(fc);
                this.Result.Nodes.Add(new ResultClass.Node(fc)
                {
                    IsSwitch = node.IsSwitch,
                    Fieldname = node.Name,
                    NodeType = node.NodeType
                });
            }
        }

        this.Result.UseSnapTolerance = commandModel.UseSnapTolerance;
        this.Result.SnapTolerance = commandModel.SnapTolerance;

        foreach(var weight in commandModel.Weights)
        {
            this.Result.Weights.Add(weight);
        }

        return warnings.ToString();
    }

    #endregion

    #region Classes

    public class ResultClass
    {
        public string Name { get; set; } = "NET_NETWORK";
        public bool DeleteExisting { get; set; }
        public ICollection<IFeatureClass> NodeFeatureClasses { get; } = new List<IFeatureClass>();
        public ICollection<IFeatureClass> EdgeFeatureClasses { get; } = new List<IFeatureClass>();

        public bool UseSnapTolerance { get; set; }
        public double SnapTolerance { get; set; }

        public bool UseComplexEdges { get; set; }
        public ICollection<IFeatureClass> ComplexEdges { get; } = new List<IFeatureClass>();

        public ICollection<Node> Nodes { get; } = new List<Node>();

        public GraphWeights Weights { get; } = new GraphWeights();

        public void Reset()
        {
            this.NodeFeatureClasses.Clear();
            this.EdgeFeatureClasses.Clear();
            this.UseSnapTolerance = false;
            this.SnapTolerance = 0D;
            this.UseComplexEdges = false;
            this.ComplexEdges.Clear();
            this.Nodes.Clear();
            this.Weights.Clear();
        }

        public class Node
        {
            public Node(IFeatureClass fc)
            {
                this.FeatureClass = fc;
            }

            public bool IsSwitch { get; set; }
            public IFeatureClass FeatureClass { get; set; }
            public string Fieldname { get; set; } = String.Empty;
            public NetworkNodeType NodeType { get; set; } = NetworkNodeType.Unknown;
        }
    }

    #endregion
}
