using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSGraphService
    {
        public static Dictionary<Entity, ThPDSBlockReferenceData> DistBoxBlocks { get; set; }
        public static Dictionary<Entity, ThPDSBlockReferenceData> LoadBlocks { get; set; }

        public static ThPDSCircuitGraphNode CreateNode(Entity entity, Database database, ThMarkService markService, List<string> distBoxKey)
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

        public static ThPDSCircuitGraphNode CreateNode(List<Entity> entities, Database database, ThMarkService markService)
        {
            var node = new ThPDSCircuitGraphNode();
            var loads = new List<ThPDSLoad>();
            var noneLoad = false;
            entities.ForEach(e =>
            {
                if (e is Line line)
                {
                    noneLoad = true;
                }
                else
                {
                    var frame = ThPDSBufferService.Buffer(e, database);
                    var marks = markService.GetMarks(frame);
                    var service = new ThPDSMarkAnalysisService();
                    loads.Add(service.LoadMarkAnalysis(marks, LoadBlocks[e]));
                }
            });

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
            ThPDSCircuitGraphNode tatget, List<string> list, List<string> distBoxKey, bool forced = false)
        {
            var edge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source, tatget);
            var service = new ThPDSMarkAnalysisService();
            if (forced)
            {
                edge.Circuit = service.CircuitMarkAnalysis(list);
            }
            else
            {
                edge.Circuit = service.CircuitMarkAnalysis(list, distBoxKey);
            }
            return edge;
        }
    }
}
