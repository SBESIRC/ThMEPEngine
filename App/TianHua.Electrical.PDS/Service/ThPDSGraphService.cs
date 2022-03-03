using System.Linq;
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

        public static ThPDSCircuitGraphNode CreateNode(List<Entity> entities, Database database, ThMarkService markService, 
            List<string> distBoxKey, ref string attributesCopy)
        {
            var node = new ThPDSCircuitGraphNode();
            var loads = new List<ThPDSLoad>();
            var noneLoad = false;
            foreach (var e in entities)
            {
                if (e is Line line)
                {
                    noneLoad = true;
                }
                else
                {
                    var service = new ThPDSMarkAnalysisService();
                    if (LoadBlocks[e].EffectiveName.IndexOf("电动机及负载标注") == 0)
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
            if(source.NodeType == PDSNodeType.Cabletray)
            {
                edge.Circuit.ViaCableTray = true;
            }
            if (target.NodeType == PDSNodeType.Load)
            {
                edge.Circuit.ViaConduit = true;
            }
            var circuitIDs = target.Loads.Select(o => o.ID.CircuitID).Distinct().OfType<string>().ToList();
            if(circuitIDs.Count == 1 && string.IsNullOrEmpty(edge.Circuit.ID.CircuitID))
            {
                edge.Circuit.ID.CircuitID = circuitIDs[0];
            }
            var circuitNumbers = target.Loads.Select(o => o.ID.CircuitNumber).Distinct().OfType<string>().ToList();
            if (circuitNumbers.Count == 1 && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber))
            {
                edge.Circuit.ID.CircuitNumber = circuitNumbers[0];
            }

            if (source.Loads.Count > 0
                && !string.IsNullOrEmpty(edge.Circuit.ID.CircuitID) 
                && string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber)
                && !string.IsNullOrEmpty(source.Loads[0].ID.LoadID))
            {
                edge.Circuit.ID.CircuitNumber = source.Loads[0].ID.LoadID + "-" + edge.Circuit.ID.CircuitID;
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
