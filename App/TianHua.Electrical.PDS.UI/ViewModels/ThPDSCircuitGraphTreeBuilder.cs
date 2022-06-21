using QuikGraph;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSCircuitGraphTreeBuilder
    {
        public ThPDSCircuitGraphTreeModel Build(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
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
                var roots = graph.Vertices.Where(x => graph.InDegree(x) == 0).Select(x => idDict[x]).ToList();
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
                        var m = new ThPDSCircuitGraphTreeModel()
                        {
                            Id = idDict[v],
                            Name = v.LoadIdString(),
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
            {
                var node = new ThPDSCircuitGraphTreeModel()
                {
                    DataList = lst,
                };
                {
                    int count = 0;
                    void dfs(ThPDSCircuitGraphTreeModel node)
                    {
                        ++count;
                        foreach (var n in node.DataList)
                        {
                            dfs(n);
                        }
                    }
                    dfs(node);
                    if (count == 1)
                    {
                        var vertices = graph.Vertices.ToList();
                        var vts = vertices.Where(v => v.Type == Model.PDSNodeType.DistributionBox).ToList();
                        if (vts.Count == 0) vts = vertices;
                        foreach (var v in vts)
                        {
                            var m = new ThPDSCircuitGraphTreeModel()
                            {
                                Id = idDict[v],
                                Name = v.LoadIdString(),
                                DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                            };
                            node.DataList.Add(m);
                        }
                    }
                }
                {
                    var vertices = graph.Vertices.ToList();
                    var vts = vertices.Where(v => v.Type == Model.PDSNodeType.DistributionBox).ToList();
                    var nodes = new List<ThPDSProjectGraphNode>();
                    void dfs(ThPDSCircuitGraphTreeModel nd)
                    {
                        if (nd != node) nodes.Add(vertices[nd.Id]);
                        foreach (var n in nd.DataList)
                        {
                            dfs(n);
                        }
                    }
                    dfs(node);
                    foreach (var v in vts)
                    {
                        if (!nodes.Contains(v))
                        {
                            nodes.Add(v);
                            var m = new ThPDSCircuitGraphTreeModel()
                            {
                                Id = idDict[v],
                                Name = v.LoadIdString(),
                                DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                            };
                            node.DataList.Add(m);
                        }
                    }
                }
                {
                    void dfs(ThPDSCircuitGraphTreeModel node)
                    {
                        if (node.DataList != null)
                        {
                            var lst = node.DataList.OrderBy(x => x.Name).ToList();
                            node.DataList.Clear();
                            foreach (var n in lst)
                            {
                                node.DataList.Add(n);
                            }
                            foreach (var n in node.DataList)
                            {
                                dfs(n);
                            }
                        }
                    }
                    dfs(node);
                }
                return node;
            }
        }

        public ThPDSCircuitGraphTreeModel BuildV1_1(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
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

            var TopNode = new ThPDSCircuitGraphTreeModel()
            {
                DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
            };

            var RootNodes = graph.Vertices.Where(x => graph.InDegree(x) == 0 && x.Type == Model.PDSNodeType.DistributionBox);
            foreach (var rootNode in RootNodes.ToDictionary(key => key, value => value.LoadIdString()).OrderBy(x => x.Value))
            {
                ThPDSCircuitGraphTreeModel m = new ThPDSCircuitGraphTreeModel()
                {
                    Id=idDict[rootNode.Key],
                    Name=rootNode.Value,
                    DataList =new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                };
                TopNode.DataList.Add(m);
                DFSSearch(m, rootNode.Key);
            }

            void DFSSearch(ThPDSCircuitGraphTreeModel m,ThPDSProjectGraphNode node)
            {
                var edges = graph.OutEdges(node);
                foreach (var nextNode in edges.ToDictionary(key=> key.Target,value => value.Target.LoadIdString()).OrderBy(o => o.Value))
                {
                    var target = nextNode.Key;
                    if (target.Type == Model.PDSNodeType.DistributionBox)
                    {
                        var targetModel = new ThPDSCircuitGraphTreeModel()
                        {
                            Id = idDict[target],
                            Name = nextNode.Value,
                            DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                        };
                        m.DataList.Add(targetModel);
                        DFSSearch(targetModel, target);
                    }
                    else if(target.Type == Model.PDSNodeType.VirtualLoad)
                    {
                        var VirtualLoadEdges = graph.OutEdges(target);
                        foreach (var virtualLoadNextNode in VirtualLoadEdges.ToDictionary(key => key.Target, value => value.Target.LoadIdString()).OrderBy(o => o.Value))
                        {
                            var virtualLoadTarget = virtualLoadNextNode.Key;
                            if (virtualLoadTarget.Type == Model.PDSNodeType.DistributionBox)
                            {
                                var targetModel = new ThPDSCircuitGraphTreeModel()
                                {
                                    Id = idDict[virtualLoadTarget],
                                    Name = virtualLoadNextNode.Value,
                                    DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                                };
                                m.DataList.Add(targetModel);
                                DFSSearch(targetModel, virtualLoadTarget);
                            }
                        }
                    }
                }
            }
            return TopNode;
        }
    }
}
