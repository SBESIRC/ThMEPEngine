using QuikGraph;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSCircuitGraphTreeBuilder
    {
        public ThPDSCircuitGraphTreeModel Build(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            var lst = new ObservableCollection<ThPDSCircuitGraphTreeModel>();
            var nodeId = -1;
            var idDict = new Dictionary<ThPDSProjectGraphNode, int>();
            foreach (var node in graph.Vertices)
            {
                if (!idDict.ContainsKey(node))
                {
                    idDict[node] = ++nodeId;
                }
            }
            foreach (var edge in graph.Edges)
            {
                if (!idDict.ContainsKey(edge.Source))
                {
                    idDict[edge.Source] = ++nodeId;
                }
                if (!idDict.ContainsKey(edge.Target))
                {
                    idDict[edge.Target] = ++nodeId;
                }
            }
            {
                var vertices = graph.Vertices.ToList();
                var roots = graph.Vertices.Where(x => x.IsStartVertexOfGraph).Select(x => idDict[x]).ToList();
                var tb = new TreeBuilder();
                foreach (var eg in graph.Edges)
                {
                    tb.Add(idDict[eg.Source], idDict[eg.Target]);
                }
                foreach (var root in roots)
                {
                    var node = tb.Visit(root);
                    var si = 0;
                    void dfs(TreeNode node, ThPDSCircuitGraphTreeModel parent)
                    {
                        ++si;
                        if (si > 1000) return;
                        var v = vertices[node.Id];
                        if (v.Type != Model.PDSNodeType.DistributionBox) return;
                        var name = v.Load?.ID?.LoadID;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = v.Load?.ID?.Description;
                        }
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = "未知配电箱";
                        }
                        name ??= "";
                        var m = new ThPDSCircuitGraphTreeModel()
                        {
                            Id = idDict[v],
                            Name = name,
                            DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                        };
                        if (parent is null)
                        {
                            lst.Add(m);
                            foreach (var subnode in node.Children)
                            {
                                dfs(subnode, m);
                            }
                        }
                        else
                        {
                            parent.DataList.Add(m);
                            foreach (var subnode in node.Children)
                            {
                                dfs(subnode, m);
                            }
                        }
                    }
                    dfs(node, null);
                }
            }
            return new ThPDSCircuitGraphTreeModel()
            {
                DataList = lst,
            };
        }
    }
}
