﻿using QuikGraph;
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
            var root = new ThPDSCircuitGraphTreeModel()
            {
                Name = "",
                NodeUID = "",
                IsRoot = true,
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
                    DataList =new ObservableCollection<ThPDSCircuitGraphTreeModel>(),
                };
                root.DataList.Add(m);
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
                            NodeUID = target.Load.LoadUID,
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
                                    NodeUID = virtualLoadTarget.Load.LoadUID,
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
            return root;
        }
    }
}
