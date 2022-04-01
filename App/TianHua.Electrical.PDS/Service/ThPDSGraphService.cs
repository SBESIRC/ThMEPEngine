using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSGraphService
    {
        public static Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; }
        public static Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, Database database, ThMarkService markService,
            List<string> distBoxKey, List<ObjectId> objectIds)
        {
            var node = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.DistributionBox
            };
            var frame = ThPDSBufferService.Buffer(entity, database);
            var marks = markService.GetMarks(frame);
            objectIds.AddRange(marks.ObjectIds);
            var service = new ThPDSMarkAnalysisService();
            node.Loads = new List<ThPDSLoad>
            {
                service.DistBoxMarkAnalysis(marks.Texts, distBoxKey, DistBoxBlocks[entity]),
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
            List<string> distBoxKey, List<ObjectId> objectIds, ref string attributesCopy)
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
                    objectIds.Add(LoadBlocks[e].ObjId);
                    if (LoadBlocks[e].EffectiveName.IndexOf(ThPDSCommon.MOTOR_AND_LOAD_LABELS) == 0)
                    {
                        loads.Add(service.LoadMarkAnalysis(LoadBlocks[e]));
                    }
                    else
                    {
                        var frame = ThPDSBufferService.Buffer(e, database);
                        var marks = markService.GetMarks(frame);
                        loads.Add(service.LoadMarkAnalysis(marks.Texts, distBoxKey, LoadBlocks[e], ref attributesCopy));
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
            ThPDSCircuitGraphNode target, List<string> infos, List<string> distBoxKey)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            var service = new ThPDSMarkAnalysisService();
            var srcPanelID = source.Loads.Count > 0 ? source.Loads[0].ID.LoadID : "";
            edge.Circuit = service.CircuitMarkAnalysis(srcPanelID, infos, distBoxKey);
            if (source.NodeType == PDSNodeType.CableCarrier)
            {
                edge.Circuit.ViaCableTray = true;
            }
            // 可能存在问题
            if (target.NodeType == PDSNodeType.Load)
            {
                edge.Circuit.ViaConduit = true;
            }

            var circuitModel = ThPDSCircuitConfig.SelectModel(edge.Circuit.ID.CircuitNumber.Last());
            if (circuitModel.CircuitType != ThPDSCircuitType.None)
            {
                if (target.Loads.Count > 0)
                {
                    target.Loads[0].CircuitType = circuitModel.CircuitType;
                }
            }
            // 仅当回路编号有效个数为1时，添加至回路的回路编号中
            if (string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.Last()))
            {
                var circuitIDs = new List<string>();
                target.Loads.ForEach(o => o.ID.CircuitID.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => circuitIDs.Add(id)));
                circuitIDs = circuitIDs.Distinct().ToList();
                var sourcePanelIDs = new List<string>();
                target.Loads.ForEach(o => o.ID.SourcePanelID.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => sourcePanelIDs.Add(id)));
                sourcePanelIDs = sourcePanelIDs.Distinct().ToList();
                if (circuitIDs.Count == 1 && sourcePanelIDs.Count == 1)
                {
                    edge.Circuit.ID.SourcePanelID.Add(sourcePanelIDs.First());
                    edge.Circuit.ID.CircuitID.Add(circuitIDs.First());
                }
            }

            if (source.Loads.Count > 0
                && !string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.Last())
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber.Last())
                && !string.IsNullOrEmpty(source.Loads[0].ID.LoadID))
            {
                edge.Circuit.ID.SourcePanelID.Add(source.Loads[0].ID.LoadID);
            }

            if (target.NodeType == PDSNodeType.None)
            {
                ThPDSLayerService.Assign(edge.Circuit, target.Loads[0]);
            }
            return edge;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> UnionEdge(ThPDSCircuitGraphNode source,
            ThPDSCircuitGraphNode target, List<string> srcPanelID, List<string> circuitID)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            var service = new ThPDSMarkAnalysisService();
            edge.Circuit = service.CircuitMarkAnalysis(srcPanelID, circuitID);
            return edge;
        }
    }
}
