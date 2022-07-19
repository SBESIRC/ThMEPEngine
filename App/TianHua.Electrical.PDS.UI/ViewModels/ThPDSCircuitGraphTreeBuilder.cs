using QuikGraph;
using System.Linq;
using System.Collections.ObjectModel;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSCircuitGraphTreeBuilder
    {
        public static ThPDSCircuitGraphTreeModel Build(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            var lst = new ObservableCollection<ThPDSCircuitGraphTreeModel>();
            var root = new ThPDSCircuitGraphTreeModel()
            {
                Name = "",
                NodeUID = "",
                IsRoot = true,
                Parent = null,
                DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
            };

            // 获取所有一级负载
            var nodes = graph.Vertices.Where(x => graph.InDegree(x) == 0 && x.Type == Model.PDSNodeType.DistributionBox);
            foreach (var rootNode in nodes.ToDictionary(key => key, value => value.LoadIdString()).OrderBy(x => x.Value))
            {
                ThPDSCircuitGraphTreeModel m = new ThPDSCircuitGraphTreeModel()
                {
                    IsRoot = false,
                    Name = rootNode.Value,
                    NodeUID = rootNode.Key.Load.LoadUID,
                    Parent = root,
                    DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                };
                root.DataList.Add(m);
                DeepFirstSearch(m, rootNode.Key);
            }

            void DeepFirstSearch(ThPDSCircuitGraphTreeModel m, ThPDSProjectGraphNode node)
            {
                var edges = graph.OutEdges(node);
                foreach (var nextNode in edges.ToDictionary(key => key.Target, value => value.Target.LoadIdString()).OrderBy(o => o.Value))
                {
                    var target = nextNode.Key;
                    if (target.Type == Model.PDSNodeType.DistributionBox)
                    {
                        var targetModel = new ThPDSCircuitGraphTreeModel()
                        {
                            NodeUID = target.Load.LoadUID,
                            Name = nextNode.Value,
                            Parent = m,
                            DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                        };
                        m.DataList.Add(targetModel);
                        DeepFirstSearch(targetModel, target);
                    }
                    else if (target.Type == Model.PDSNodeType.VirtualLoad)
                    {
                        var VirtualLoadEdges = graph.OutEdges(target);
                        foreach (var virtualLoadNextNode in VirtualLoadEdges.ToDictionary(key => key.Target, value => value.Target.LoadIdString()).OrderBy(o => o.Value))
                        {
                            var virtualLoadTarget = virtualLoadNextNode.Key;
                            if (virtualLoadTarget.Type == Model.PDSNodeType.DistributionBox)
                            {
                                var targetModel = new ThPDSCircuitGraphTreeModel()
                                {
                                    NodeUID = virtualLoadTarget.Load.LoadUID,
                                    Name = virtualLoadNextNode.Value,
                                    Parent = m,
                                    DataList = new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                                };
                                m.DataList.Add(targetModel);
                                DeepFirstSearch(targetModel, virtualLoadTarget);
                            }
                        }
                    }
                }
            }
            return root;
        }
    }
}
