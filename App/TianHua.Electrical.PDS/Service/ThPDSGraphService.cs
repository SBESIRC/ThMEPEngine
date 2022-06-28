using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSGraphService
    {
        public static Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; }
        public static Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; }
        public static ThMEPOriginTransformer Transformer { get; set; }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, Database database, ThMarkService markService,
            List<string> distBoxKey, List<ObjectId> objectIds)
        {
            var node = new ThPDSCircuitGraphNode
            {
                NodeType = PDSNodeType.DistributionBox
            };
            var frame = ThPDSBufferService.Buffer(entity, database);
            var marks = markService.GetMarks(frame);
            marks.Texts = marks.Texts.Where(x => x != "E").ToList();
            objectIds.AddRange(marks.ObjectIds);
            var service = new ThPDSMarkAnalysisService();
            node.Loads = new List<ThPDSLoad>
            {
                service.DistBoxMarkAnalysis(marks.Texts, distBoxKey, DistBoxBlocks[entity]),
            };
            node.Loads.ForEach(o =>
            {
                o.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                o.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
            });
            return node;
        }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, List<string> marks, List<string> distBoxKey, Polyline frame)
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
            node.Loads.ForEach(o =>
            {
                o.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                o.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
            });
            return node;
        }

        public static ThPDSCircuitGraphNode CreateNode(List<Entity> entities, Database database, ThMarkService markService,
            List<string> distBoxKey, List<ObjectId> objectIds, ref string attributesCopy)
        {
            var node = new ThPDSCircuitGraphNode();
            var loads = new List<ThPDSLoad>();
            var noneLoad = true;
            var endPoint = new ThPDSPoint3d();
            var minPoint = new ThPDSPoint3d();
            var maxPoint = new ThPDSPoint3d();
            foreach (var e in entities)
            {
                if (e is Line line)
                {
                    endPoint = Transformer.Reset(line.EndPoint).ToPDSPoint3d();
                    var rectangle = ThPDSBufferService.Buffer(line).GeometricExtents;
                    minPoint = PointReset(rectangle.MinPoint);
                    maxPoint = PointReset(rectangle.MaxPoint);
                }
                else
                {
                    // 只要有一个有效负载，则认为整个节点为有效负载
                    noneLoad = false;
                    var service = new ThPDSMarkAnalysisService();
                    if (LoadBlocks[e].EffectiveName.IndexOf(ThPDSCommon.MOTOR_AND_LOAD_LABELS) == 0)
                    {
                        var load = service.LoadMarkAnalysis(LoadBlocks[e]);
                        if (ThPDSLoopGraphEngine.GeometryMap.ContainsKey(e))
                        {
                            var frame = ThPDSLoopGraphEngine.GeometryMap[e];
                            load.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                            load.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
                        }
                        else
                        {
                            var frame = load.Location.BasePoint.PDSPoint3dToPoint3d().CreateSquare(500.0);
                            load.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                            load.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
                        }
                        loads.Add(load);
                        objectIds.Add(LoadBlocks[e].ObjId);
                    }
                    else
                    {
                        var frame = ThPDSBufferService.Buffer(e, database);
                        var marks = markService.GetMarks(frame);
                        var load = service.LoadMarkAnalysis(marks.Texts, distBoxKey, LoadBlocks[e], ref attributesCopy);
                        load.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                        load.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
                        loads.Add(load);
                        objectIds.AddRange(marks.ObjectIds);
                    }
                }
            }

            if (loads.Count == 0)
            {
                loads.Add(new ThPDSLoad());
                loads[0].SetLocation(new ThPDSLocation
                {
                    BasePoint = endPoint,
                    MinPoint = minPoint,
                    MaxPoint = maxPoint,
                });
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
                var marks = markService.GetMarks(frame, true);
                objectIds.AddRange(marks.ObjectIds);
                var textFilter = marks.Texts.Where(t => t.IndexOf("WL") == 0).ToList();
                var load = service.LoadMarkAnalysis(textFilter, distBoxKey, LoadBlocks[entity], ref attributesCopy);
                load.Location.MinPoint = PointReset(frame.GeometricExtents.MinPoint);
                load.Location.MaxPoint = PointReset(frame.GeometricExtents.MaxPoint);
                load.SetOnLightingCableTray(true);
                loads.Add(load);
            }

            node.Loads = loads;
            node.NodeType = PDSNodeType.Load;
            return node;
        }

        public static ThPDSCircuitGraphNode NodeClone(ThPDSCircuitGraphNode sourceNode, string source, string target)
        {
            var node = new ThPDSCircuitGraphNode();
            node.NodeType = sourceNode.NodeType;
            sourceNode.Loads.ForEach(load =>
            {
                node.Loads.Add(load.Clone());
            });
            node.Loads[0].ID.LoadID = node.Loads[0].ID.LoadID.Replace(source, target);

            return node;
        }

        public static ThPDSCircuitGraphNode NodeClone(ThPDSCircuitGraphNode sourceNode)
        {
            var node = new ThPDSCircuitGraphNode();
            node.NodeType = sourceNode.NodeType;
            sourceNode.Loads.ForEach(load =>
            {
                node.Loads.Add(load.Clone());
            });

            return node;
        }

        public static ThPDSCircuitGraphNode CreatePowerTransformer(string loadID)
        {
            var node = new ThPDSCircuitGraphNode();
            node.NodeType = PDSNodeType.PowerTransformer;
            node.Loads.Add(new ThPDSLoad
            {
                ID = new ThPDSID
                {
                    LoadID = loadID,
                },
            });

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
            var service = new ThPDSMarkAnalysisService();
            var reversible = true;
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, target);
            if (source.NodeType == PDSNodeType.DistributionBox && target.NodeType == PDSNodeType.DistributionBox)
            {
                // 限制一相配电箱成为三相配电箱的上级
                if (edge.Source.Loads[0].Phase == ThPDSPhase.一相 && edge.Target.Loads[0].Phase == ThPDSPhase.三相)
                {
                    edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(target, source);
                    reversible = false;
                }
                else if (ThPDSTerminalPanelService.IsTerminalPanel(edge.Source)
                    && !ThPDSTerminalPanelService.IsTerminalPanel(edge.Target))
                {
                    edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(target, source);
                    reversible = false;
                }
                else if (!ThPDSTerminalPanelService.IsTerminalPanel(edge.Source)
                    && ThPDSTerminalPanelService.IsTerminalPanel(edge.Target))
                {
                    reversible = false;
                }
            }

            var srcPanelID = edge.Source.Loads.Count > 0 ? edge.Source.Loads[0].ID.LoadID : "";
            var tarPanelID = edge.Target.Loads.Count > 0 ? edge.Target.Loads[0].ID.LoadID : "";
            edge.Circuit = service.CircuitMarkAnalysis(srcPanelID, tarPanelID, infos, distBoxKey);
            AssignCircuitNumber(edge, circuitAssign);

            if (reversible && source.NodeType != PDSNodeType.CableCarrier
                && target.NodeType != PDSNodeType.Load
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber))
            {
                var anotherEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(target, source);
                var anotherSrcPanelID = anotherEdge.Source.Loads.Count > 0 ? anotherEdge.Source.Loads[0].ID.LoadID : "";
                var anotherTarPanelID = anotherEdge.Target.Loads.Count > 0 ? anotherEdge.Target.Loads[0].ID.LoadID : "";
                if (!string.IsNullOrEmpty(anotherSrcPanelID))
                {
                    anotherEdge.Circuit = service.CircuitMarkAnalysis(anotherSrcPanelID, anotherTarPanelID, infos, distBoxKey);
                    AssignCircuitNumber(anotherEdge, circuitAssign);
                    if (!string.IsNullOrEmpty(anotherEdge.Circuit.ID.CircuitNumber))
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

            var circuitModel = ThPDSCircuitConfig.SelectModel(edge.Circuit.ID.CircuitNumber);
            if (circuitModel.CircuitType != ThPDSCircuitType.None)
            {
                if (edge.Target.Loads.Count > 0)
                {
                    edge.Target.Loads[0].CircuitType = circuitModel.CircuitType;
                    edge.Target.Loads[0].SetFireLoad(circuitModel.FireLoad);
                    if (string.IsNullOrEmpty(edge.Target.Loads[0].ID.Description))
                    {
                        edge.Target.Loads[0].ID.DefaultDescription = circuitModel.DefaultDescription;
                    }
                }
            }

            if (edge.Source.Loads.Count > 0
                && !string.IsNullOrEmpty(edge.Circuit.ID.CircuitID)
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber)
                && !string.IsNullOrEmpty(edge.Source.Loads[0].ID.LoadID))
            {
                edge.Circuit.ID.SourcePanelIDList.Add(edge.Source.Loads[0].ID.LoadID);
            }

            if (edge.Target.NodeType == PDSNodeType.Unkown)
            {
                ThPDSLayerService.Assign(edge.Target.Loads[0]);
            }
            return edge;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> CreateEdge(ThPDSCircuitGraphNode powerTransformer,
            ThPDSCircuitGraphNode target, Tuple<string, string> powerTransformerNumber)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(powerTransformer, target);
            var service = new ThPDSMarkAnalysisService();
            edge.Circuit = service.CircuitMarkAnalysis(powerTransformerNumber);
            return edge;
        }

        private static void AssignCircuitNumber(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, bool circuitAssign)
        {
            // 仅当回路编号有效个数为1时，添加至回路的回路编号中
            if (circuitAssign && string.IsNullOrEmpty(edge.Circuit.ID.CircuitIDList.Last()))
            {
                var circuitIDs = new List<string>();
                edge.Target.Loads.ForEach(o => o.ID.CircuitIDList.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => circuitIDs.Add(id)));
                circuitIDs = circuitIDs.Distinct().ToList();
                var sourcePanelIDs = new List<string>();
                edge.Target.Loads.ForEach(o => o.ID.SourcePanelIDList.Where(id => !string.IsNullOrEmpty(id))
                    .ForEach(id => sourcePanelIDs.Add(id)));
                sourcePanelIDs = sourcePanelIDs.Distinct().ToList();
                if (circuitIDs.Count == 1 && sourcePanelIDs.Count == 1)
                {
                    if (edge.Source.Loads.Count == 0
                        || string.IsNullOrEmpty(edge.Source.Loads[0].ID.LoadID)
                        || edge.Source.Loads[0].ID.LoadID.Equals(sourcePanelIDs[0]))
                    {
                        edge.Circuit.ID.SourcePanelIDList.Add(sourcePanelIDs.First());
                        edge.Circuit.ID.CircuitIDList.Add(circuitIDs.First());
                    }
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

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> EdgeClone(ThPDSCircuitGraphNode sourceNode,
            ThPDSCircuitGraphNode targetNode, ThPDSCircuit circuit, string source, string target)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(sourceNode, targetNode);
            edge.Circuit = circuit;
            edge.Circuit.CircuitUID = System.Guid.NewGuid().ToString();
            var count = edge.Circuit.ID.SourcePanelIDList.Count;
            edge.Circuit.ID.SourcePanelIDList[count - 1] = edge.Circuit.ID.SourcePanelIDList[count - 1].Replace(source, target);
            return edge;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> EdgeClone(ThPDSCircuitGraphNode sourceNode,
            ThPDSCircuitGraphNode targetNode, ThPDSCircuit circuit)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(sourceNode, targetNode);
            edge.Circuit = circuit;
            edge.Circuit.CircuitUID = System.Guid.NewGuid().ToString();
            return edge;
        }

        public static ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> OutEdgeClone(
            ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> sourceEdge, ThPDSCircuitGraphNode source)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, sourceEdge.Target);
            edge.Circuit = sourceEdge.Circuit;
            edge.Circuit.CircuitUID = System.Guid.NewGuid().ToString();
            edge.Circuit.ID.SourcePanelIDList[edge.Circuit.ID.SourcePanelIDList.Count - 1] = source.Loads[0].ID.LoadID;
            return edge;
        }

        private static ThPDSPoint3d PointReset(Point3d point)
        {
            return Transformer.Reset(point).ToPDSPoint3d();
        }
    }
}
