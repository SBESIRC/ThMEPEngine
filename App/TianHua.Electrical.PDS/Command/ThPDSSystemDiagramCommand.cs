using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using QuikGraph;
using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Diagram;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSSystemDiagramCommand : ThMEPBaseCommand, IDisposable
    {
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }
        public ThPDSProjectGraphNode StartNode { get; set; }

        public ThPDSSystemDiagramCommand(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph,
            ThPDSProjectGraphNode startNode)
        {
            Graph = graph;
            StartNode = startNode;
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                if(StartNode.Type != PDSNodeType.DistributionBox)
                {
                    return;
                }
                var enterType = StartNode.Details.CircuitFormType.CircuitFormType.GetDescription();
                if (enterType == CircuitFormInType.None.ToString())
                {
                    return;
                }

                if (!TrySelectPoint(out var basePoint))
                {
                    return;
                }

                var scaleFactor = 1.0;
                var scale = new Scale3d(scaleFactor, scaleFactor, scaleFactor);
                activeDb.Layers.Import(configDb.Layers.ElementOrDefault("0"), false);
                var insertEngine = new ThPDSBlockInsertEngine();
                var assignment = new ThPDSDiagramAssignment();
                // 插入内框及表头
                var title = insertEngine.InsertHeader(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_HEADER, basePoint, scale);
                assignment.TableTitleAssign(activeDb, title, StartNode);

                // 插入进线回路
                basePoint = new Point3d(basePoint.X + 5700 * scaleFactor, basePoint.Y - 6500 * scaleFactor, 0);
                var enterCircuit = insertEngine.Insert(activeDb, configDb, enterType, basePoint, scale);
                assignment.TableTitleAssign(activeDb, title, StartNode);

                // 插入出线回路
                basePoint = new Point3d(basePoint.X + 5000 * scaleFactor, basePoint.Y - 1000 * scaleFactor, 0);
                var edges = Graph.Edges.Where(e => e.Source == StartNode).ToList();
                foreach (var edge in edges)
                {
                    var outType = edge.Details.CircuitForm.CircuitFormType.GetDescription();
                    basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                    var outCircuit = insertEngine.Insert(activeDb, configDb, outType, basePoint, scale);
                }

                // 插入表尾
                basePoint = new Point3d(basePoint.X - 5000 * scaleFactor, basePoint.Y - 1500 * scaleFactor, 0);
                var tail = insertEngine.Insert(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE, basePoint, scale);
            }
        }

        private static bool TrySelectPoint(out Point3d basePt)
        {
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var result = Active.Editor.GetPoint(basePtOptions);
            if (result.Status != PromptStatus.OK)
            {
                basePt = default;
                return false;
            }
            basePt = result.Value.TransformBy(Active.Editor.UCS2WCS());
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
