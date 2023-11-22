using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.Network;
using gView.Framework.Network.Algorthm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gView.Cmd.Fdb.Lib.Model;

public class CreateNetworkModel : IPersistable
{
    public string Name { get; set; }=string.Empty;
    public bool DeleteIfAlredyExists { get; set; }
    public string ConnectionString { get; set; } = string.Empty;    
    public Guid DatasetGuid { get; set; }
    public string DatasetName { get; set; } = string.Empty;

    public IEnumerable<Edge>? Edges { get; set; }
    public IEnumerable<Node>? Nodes { get; set; }

    public bool UseSnapTolerance { get; set; }
    public double SnapTolerance { get; set; } = 0D;

    public GraphWeights Weights { get; set; } = new GraphWeights();

    #region IPersistable

    public void Load(IPersistStream stream)
    {
        this.Name = (string)stream.Load("Name", string.Empty);
        this.DeleteIfAlredyExists = (bool)stream.Load("DeleteIfAlreadyExists", false);

        this.ConnectionString = (string)stream.Load("ConnectionString", string.Empty);
        this.DatasetGuid = new Guid((string)stream.Load("DatasetGuid", Guid.Empty.ToString()));
        this.DatasetName = (string)stream.Load("DatasetName", string.Empty);

        var edges = (EdgeCollection?)stream.Load("Edges", null, new EdgeCollection());
        var nodes = (NodeCollection?)stream.Load("Nodes", null, new NodeCollection());

        if (edges?.Count > 0)
        {
            this.Edges = edges;
        }
        if (nodes?.Count > 0)
        {
            this.Nodes = nodes;
        }

        this.UseSnapTolerance = (bool)stream.Load("UseSnapTolerance", false);
        this.SnapTolerance = (double)stream.Load("SnapTolerance", 0D);

        var weights = (WeightCollection?)stream.Load("Weights", null, new WeightCollection());

        if(weights?.Count > 0)
        {
            this.Weights.AddRange(weights);
        }
    }

    public void Save(IPersistStream stream)
    {
        stream.Save("Name", this.Name);
        stream.Save("DeleteIfAlreadyExists", this.DeleteIfAlredyExists);

        stream.Save("ConnectionString", this.ConnectionString);
        stream.Save("DatasetGuid", this.DatasetGuid.ToString());
        stream.Save("DatasetName", this.DatasetName);

        stream.Save("Edges", new EdgeCollection(Edges));
        stream.Save("Nodes", new NodeCollection(Nodes));

        stream.Save("UseSnapTolerance", this.UseSnapTolerance);
        stream.Save("SnapTolerance", this.SnapTolerance);

        stream.Save("Weights", new WeightCollection(Weights));
    }

    #endregion

    #region Classes

    public class Edge : IPersistable
    {
        public string Name { get; set; } = String.Empty;
        public bool IsComplexEdge { get; set; }

        #region IPersistable

        public void Load(IPersistStream stream)
        {
            this.Name = (string)stream.Load("Name", string.Empty);
            this.IsComplexEdge = (bool)stream.Load("IsComplexEdge", false);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("Name", this.Name);
            stream.Save("IsComplexEdge", this.IsComplexEdge);
        }

        #endregion
    }

    public class Node : IPersistable
    {
        public string Name { get; set; } = String.Empty;
        public bool IsSwitch { get; set; }
        public string FieldName { get; set; } = String.Empty;
        public NetworkNodeType NodeType { get; set; } = NetworkNodeType.Unknown;

        #region IPersistable

        public void Load(IPersistStream stream)
        {
            this.Name = (string)stream.Load("Name", string.Empty);
            this.IsSwitch = (bool)stream.Load("IsSwitch", false);
            this.FieldName = (string)stream.Load("FieldName", string.Empty);
            this.NodeType=(NetworkNodeType)stream.Load("NodeType",(int)NetworkNodeType.Unknown);
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("Name", this.Name);
            stream.Save("IsSwitch", this.IsSwitch);
            stream.Save("FieldName", this.FieldName);
            stream.Save("NodeType", (int)this.NodeType);
        }

        #endregion
    }

    public class EdgeCollection : List<Edge>, IPersistable
    {
        public EdgeCollection(IEnumerable<Edge>? edges = null)
        { 
            if (edges != null)
            {
                this.AddRange(edges);
            }
        }

        #region IPersistable

        public void Load(IPersistStream stream)
        {
            Edge? edge;
            while ((edge = (Edge?)stream.Load("Edge", null, new Edge())) != null)
            {
                this.Add(edge);
            }
        }

        public void Save(IPersistStream stream)
        {
            foreach (var edge in this)
            {
                stream.Save("Edge", edge);
            }
        }

        #endregion
    }

    public class NodeCollection : List<Node>, IPersistable
    {
        public NodeCollection(IEnumerable<Node>? nodes = null)
        {
            if (nodes != null)
            {
                this.AddRange(nodes);
            }
        }

        #region IPersistable

        public void Load(IPersistStream stream)
        {
            Node? node;
            while ((node = (Node?)stream.Load("Node", null, new Node())) != null)
            {
                this.Add(node);
            }
        }

        public void Save(IPersistStream stream)
        {
            foreach (var node in this)
            {
                stream.Save("Node", node);
            }
        }

        #endregion
    }

    public class WeightCollection : List<IGraphWeight>, IPersistable
    {
        public WeightCollection(IEnumerable<IGraphWeight>? weights = null)
        {
            if (weights != null)
            {
                this.AddRange(weights);
            }
        }

        #region IPersistable

        public void Load(IPersistStream stream)
        {
            IGraphWeight? weight;
            while ((weight = (IGraphWeight?)stream.Load("Weight", null, new GraphWeight())) != null)
            {
                this.Add(weight);
            }
        }

        public void Save(IPersistStream stream)
        {
            foreach (var weight in this)
            {
                stream.Save("Weight", weight);
            }
        }

        #endregion
    }

    #endregion
}
