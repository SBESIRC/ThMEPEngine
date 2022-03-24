﻿using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSGraphService
    {
        public static Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; }
        public static Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, Database database, ThMarkService markService,
            List<string> distBoxKey)
        {
            var node = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.DistributionBox
            };
            var frame = ThPDSBufferService.Buffer(entity, database);
            var marks = markService.GetMarks(frame);
            var service = new ThPDSMarkAnalysisService();
            node.Loads = new List<ThPDSLoad>
            {
                service.DistBoxMarkAnalysis(marks, distBoxKey, DistBoxBlocks[entity]),
            };
            return node;
        }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, List<string> marks, List<string> distBoxKey)
        {
            var node = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.DistributionBox
            };
            var service = new ThPDSMarkAnalysisService();
            node.Loads = new List<ThPDSLoad>
            {
                service.DistBoxMarkAnalysis(marks, distBoxKey, DistBoxBlocks[entity]),
            };
            return node;
        }

        public static ThPDSCircuitGraphNode CreateNode(List<Entity> entities, Database database, ThMarkService markService,
            List<string> distBoxKey, ref string attributesCopy)
        {
            var node = new ThPDSCircuitGraphNode();
            var loads = new List<ThPDSLoad>();
            var noneLoad = true;
            foreach (var e in entities)
            {
                if (e is Line line)
                {
                    //
                }
                else
                {
                    // 只要有一个有效负载，则认为整个节点为有效负载
                    noneLoad = false;
                    var service = new ThPDSMarkAnalysisService();
                    if (LoadBlocks[e].EffectiveName.IndexOf(ThPDSCommon.MOTOR_AND_LOAD_LABELS) == 0)
                    {
                        loads.Add(service.LoadMarkAnalysis(LoadBlocks[e]));
                    }
                    else
                    {
                        var frame = ThPDSBufferService.Buffer(e, database);
                        var marks = markService.GetMarks(frame);
                        loads.Add(service.LoadMarkAnalysis(marks, distBoxKey, LoadBlocks[e], ref attributesCopy));
                    }
                }
            }

            if (loads.Count == 0)
            {
                loads.Add(new ThPDSLoad());
            }

            node.Loads = loads;
            if (noneLoad)
            {
                node.NodeType = PDSNodeType.None;
            }
            else
            {
                node.NodeType = PDSNodeType.Load;
            }
            return node;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> CreateEdge(ThPDSCircuitGraphNode source,
            ThPDSCircuitGraphNode target, List<string> list, List<string> distBoxKey)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            var service = new ThPDSMarkAnalysisService();
            edge.Circuit = service.CircuitMarkAnalysis(list, distBoxKey);
            if (source.NodeType == PDSNodeType.CableCarrier)
            {
                edge.Circuit.ViaCableTray = true;
            }
            // 可能存在问题
            if (target.NodeType == PDSNodeType.Load)
            {
                edge.Circuit.ViaConduit = true;
            }

            var circuitModel = ThPDSCircuitConfig.SelectModel(edge.Circuit.ID.CircuitNumber.FirstOrDefault());
            if (circuitModel.CircuitType != ThPDSCircuitType.None)
            {
                if (target.Loads.Count > 0)
                {
                    target.Loads[0].CircuitType = circuitModel.CircuitType;
                }
            }
            var circuitIDs = target.Loads.Select(o => o.ID.CircuitID).Distinct().OfType<string>().ToList();
            if (circuitIDs.Count == 1 && string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.FirstOrDefault()))
            {
                edge.Circuit.ID.CircuitID.Add(circuitIDs[0]);
            }
            else
            {
                edge.Circuit.ID.CircuitID.Add("");
            }
            var circuitNumbers = target.Loads.Select(o => o.ID.CircuitNumber).Distinct().OfType<string>().ToList();
            if (circuitNumbers.Count == 1 && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber.FirstOrDefault()))
            {
                edge.Circuit.ID.CircuitNumber.Add(circuitNumbers[0]);
            }
            else
            {
                edge.Circuit.ID.CircuitNumber.Add("");
            }

            if (source.Loads.Count > 0
                && !string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.FirstOrDefault())
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber.FirstOrDefault())
                && !string.IsNullOrEmpty(source.Loads[0].ID.LoadID))
            {
                edge.Circuit.ID.CircuitNumber.Add(source.Loads[0].ID.LoadID + "-" + edge.Circuit.ID.CircuitID.First());
            }
            else
            {
                edge.Circuit.ID.CircuitNumber.Add("");
            }

            if (target.NodeType == PDSNodeType.None)
            {
                ThPDSLayerService.Assign(edge.Circuit, target.Loads[0]);
            }
            return edge;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> UnionEdge(ThPDSCircuitGraphNode source,
            ThPDSCircuitGraphNode target, List<string> list)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            var service = new ThPDSMarkAnalysisService();
            edge.Circuit = service.CircuitMarkAnalysis(list);
            return edge;
        }
    }
}
