using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSComponentGraph : NotifyPropertyChangedBase
    {
        public ThPDSProjectGraphNode GetNodeById(IEnumerable<ThPDSProjectGraphNode> nodes, string id)
        {
            return nodes.FirstOrDefault(node => GetIdFromNode(node) == id);
        }
        public string GetIdFromNode(ThPDSProjectGraphNode node)
        {
            return node.Load.LoadUID;
        }
        public ThPDSProjectGraphEdge<ThPDSProjectGraphNode> GetEdgeById(IEnumerable<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> edges, string id)
        {
            return edges.FirstOrDefault(edge => edge.Circuit.CircuitUID == id);
        }
        public void Build(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            Graph = new AdjacencyGraph<ThPDSComponentGraphNode, ThPDSComponentGraphEdge<ThPDSComponentGraphNode>>();
            Graph.AddVertexRange(graph.Vertices.Select(v => new ThPDSComponentGraphNode() { NodeID = v.Load.LoadUID }));
            Graph.AddEdgeRange(graph.Edges.Select(eg => new ThPDSComponentGraphEdge<ThPDSComponentGraphNode>(Graph.Vertices.First(x=>x.NodeID== GetIdFromNode(eg.Source)), Graph.Vertices.First(x => x.NodeID == GetIdFromNode(eg.Target)))));
        }
        AdjacencyGraph<ThPDSComponentGraphNode, ThPDSComponentGraphEdge<ThPDSComponentGraphNode>> _Graph;
        public AdjacencyGraph<ThPDSComponentGraphNode, ThPDSComponentGraphEdge<ThPDSComponentGraphNode>> Graph
        {
            get => _Graph;
            set
            {
                if (value != _Graph)
                {
                    _Graph = value;
                    OnPropertyChanged(nameof(Graph));
                }
            }
        }
    }
}
