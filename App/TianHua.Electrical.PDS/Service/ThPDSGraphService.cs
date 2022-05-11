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
                    if (LoadBlocks[e].EffectiveName.IndexOf(ThPDSCommon.MOTOR_AND_LOAD_LABELS) == 0)
                    {
                        loads.Add(service.LoadMarkAnalysis(LoadBlocks[e]));
                        objectIds.Add(LoadBlocks[e].ObjId);
                    }
                    else
                    {
                        var frame = ThPDSBufferService.Buffer(e, database);
                        var marks = markService.GetMarks(frame);
                        loads.Add(service.LoadMarkAnalysis(marks.Texts, distBoxKey, LoadBlocks[e], ref attributesCopy));
                        objectIds.AddRange(marks.ObjectIds);
                    }
                }
            }

            if (loads.Count == 0)
            {
                loads.Add(new ThPDSLoad());
                loads[0].SetLocation(new ThPDSLocation());
            }

            node.Loads = loads;
            if (noneLoad)
            {
                node.NodeType = PDSNodeType.Unkown;
            }
            else
            {
                node.NodeType = PDSNodeType.Load;
            }
            return node;
        }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, Database database, ThMarkService markService,
            List<string> distBoxKey, List<ObjectId> objectIds, ref string attributesCopy)
        {
            var node = new ThPDSCircuitGraphNode();
            var loads = new List<ThPDSLoad>();

            if (LoadBlocks[entity].EffectiveName.IndexOf(ThPDSCommon.LIGHTING_LOAD) == 0)
            {
                var service = new ThPDSMarkAnalysisService();
                objectIds.Add(LoadBlocks[entity].ObjId);
                var frame = ThPDSBufferService.Buffer(entity, database);
                var marks = markService.GetMarks(frame);
                var load = service.LoadMarkAnalysis(marks.Texts, distBoxKey, LoadBlocks[entity], ref attributesCopy);
                load.SetOnLightingCableTray(true);
                loads.Add(load);
            }

            node.Loads = loads;
            node.NodeType = PDSNodeType.Load;
            return node;
        }

        // circuitAssign参数是否是必须的，存疑
        /// <summary>
        /// circuitAssign为true表示可以通过节点身上的回路编号给回路赋值
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="infos"></param>
        /// <param name="distBoxKey"></param>
        /// <param name="circuitAssign"></param>
        /// <returns></returns>
        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> CreateEdge(ThPDSCircuitGraphNode source,
            ThPDSCircuitGraphNode target, List<string> infos, List<string> distBoxKey, bool circuitAssign = false)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            var service = new ThPDSMarkAnalysisService();
            var srcPanelID = edge.Source.Loads.Count > 0 ? edge.Source.Loads[0].ID.LoadID : "";
            edge.Circuit = service.CircuitMarkAnalysis(srcPanelID, infos, distBoxKey);
            AssignCircuitNumber(edge, circuitAssign);

            if (source.NodeType != PDSNodeType.CableCarrier
                && target.NodeType != PDSNodeType.Load
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber.Last()))
            {
                var anotherEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(target, source);
                var anotherSrcPanelID = anotherEdge.Source.Loads.Count > 0 ? anotherEdge.Source.Loads[0].ID.LoadID : "";
                if (!string.IsNullOrEmpty(anotherSrcPanelID))
                {
                    anotherEdge.Circuit = service.CircuitMarkAnalysis(anotherSrcPanelID, infos, distBoxKey);
                    AssignCircuitNumber(anotherEdge, circuitAssign);
                    if (!string.IsNullOrEmpty(anotherEdge.Circuit.ID.CircuitNumber.Last()))
                    {
                        edge = anotherEdge;
                    }
                }
            }

            if (edge.Source.NodeType == PDSNodeType.CableCarrier)
            {
                edge.Circuit.ViaCableTray = true;
            }
            if (edge.Target.NodeType == PDSNodeType.Load || edge.Target.NodeType == PDSNodeType.Unkown)
            {
                edge.Circuit.ViaConduit = true;
            }

            var circuitModel = ThPDSCircuitConfig.SelectModel(edge.Circuit.ID.CircuitNumber.Last());
            if (circuitModel.CircuitType != ThPDSCircuitType.None)
            {
                if (edge.Target.Loads.Count > 0)
                {
                    edge.Target.Loads[0].CircuitType = circuitModel.CircuitType;
                }
            }

            if (edge.Source.Loads.Count > 0
                && !string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.Last())
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber.Last())
                && !string.IsNullOrEmpty(edge.Source.Loads[0].ID.LoadID))
            {
                edge.Circuit.ID.SourcePanelID.Add(edge.Source.Loads[0].ID.LoadID);
            }

            if (edge.Target.NodeType == PDSNodeType.Unkown)
            {
                ThPDSLayerService.Assign(edge.Target.Loads[0]);
            }
            return edge;
        }

        private static void AssignCircuitNumber(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, bool circuitAssign)
        {
            // 仅当回路编号有效个数为1时，添加至回路的回路编号中
            if (circuitAssign && string.IsNullOrEmpty(edge.Circuit.ID.CircuitID.Last()))
            {
                var circuitIDs = new List<string>();
                edge.Target.Loads.ForEach(o => o.ID.CircuitID.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => circuitIDs.Add(id)));
                circuitIDs = circuitIDs.Distinct().ToList();
                var sourcePanelIDs = new List<string>();
                edge.Target.Loads.ForEach(o => o.ID.SourcePanelID.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => sourcePanelIDs.Add(id)));
                sourcePanelIDs = sourcePanelIDs.Distinct().ToList();
                if (circuitIDs.Count == 1 && sourcePanelIDs.Count == 1)
                {
                    edge.Circuit.ID.SourcePanelID.Add(sourcePanelIDs.First());
                    edge.Circuit.ID.CircuitID.Add(circuitIDs.First());
                }
            }
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
