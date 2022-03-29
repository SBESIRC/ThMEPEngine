using System;
using System.Linq;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using QuikGraph;

using ThCADExtension;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Diagram;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
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
                if (StartNode.Type != PDSNodeType.DistributionBox)
                {
                    return;
                }
                var enterType = StartNode.Details.CircuitFormType.CircuitFormType.GetDescription();
                if (enterType == CircuitFormInType.None.ToString())
                {
                    return;
                }

                if (!TrySelectPoint(out var selectPoint))
                {
                    return;
                }

                var basePoint = PointClone(selectPoint);
                var scaleFactor = 1.0;
                var scale = new Scale3d(scaleFactor, scaleFactor, scaleFactor);
                activeDb.Layers.Import(configDb.Layers.ElementOrDefault("0"), false);
                var insertEngine = new ThPDSBlockInsertEngine();
                var assignment = new ThPDSDiagramAssignment();
                // 插入内框及表头
                var title = insertEngine.InsertHeader(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_HEADER, basePoint, scale);
                assignment.TableTitleAssign(activeDb, title, StartNode);

                // 计算表身起点
                var bodyStartPoint = new Point3d(basePoint.X + 5700 * scaleFactor, basePoint.Y - 6040 * scaleFactor, 0);

                // 插入进线回路
                basePoint = new Point3d(bodyStartPoint.X, bodyStartPoint.Y - 460 * scaleFactor, 0);
                var enterCircuit = insertEngine.Insert(activeDb, configDb, enterType, basePoint, scale);
                var busbar = assignment.EnterCircuitAssign(activeDb, configDb, enterCircuit, Graph, StartNode, scale);

                // 插入空白行
                var firstRowPoint = new Point3d(bodyStartPoint.X + 5000 * scaleFactor, bodyStartPoint.Y - 500 * scaleFactor, 0);
                insertEngine.InsertBlankLine(activeDb, configDb, firstRowPoint, scale);

                // 母排起点
                var busbarStartPoint = new Point3d(firstRowPoint.X, firstRowPoint.Y - 500 * scaleFactor, 0);

                // 插入出线回路
                basePoint = PointClone(firstRowPoint);
                var edges = Graph.Edges.Where(e => e.Source.Equals(StartNode))
                    .OrderBy(e => e.Circuit.ID.CircuitNumber.Last())
                    .ToList();
                foreach (var edge in edges)
                {
                    var outType = GetOutType(edge.Details.CircuitForm);
                    basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                    var outCircuit = insertEngine.Insert(activeDb, configDb, outType, basePoint, scale);
                    assignment.OutCircuitAssign(activeDb, configDb, outCircuit, edge, scale);
                }
                if (edges.Count < 3)
                {
                    for (var i = 0; i < 3 - edges.Count; i++)
                    {
                        basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                        insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale);
                    }
                }

                // 母排终点
                var busbarEndPoint = new Point3d(basePoint.X, basePoint.Y - 500 * scaleFactor, 0);

                // 变换母排
                busbar.SetPointAt(0, busbarStartPoint.ToPoint2D());
                busbar.SetPointAt(1, busbarEndPoint.ToPoint2D());

                // 插入空白行
                var lastRowPoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                insertEngine.InsertBlankLine(activeDb, configDb, lastRowPoint, scale);

                // 插入表尾
                basePoint = new Point3d(lastRowPoint.X - 5000 * scaleFactor, lastRowPoint.Y - 500 * scaleFactor, 0);
                var tableTable = StartNode.Load.Phase == ThPDSPhase.一相
                    ? ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE : ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_THREE_PHASE;
                var tail = insertEngine.Insert(activeDb, configDb, tableTable, basePoint, scale);
                assignment.TableTailAssign(activeDb, tail, StartNode);

                // 计算表身终点
                var bodyEndPoint = new Point3d(basePoint.X + 27900 * scaleFactor, basePoint.Y, 0);
                var rectangleStartPoint = new Point3d(bodyStartPoint.X, bodyEndPoint.Y, 0);
                var rectangleEndPoint = new Point3d(bodyEndPoint.X, bodyStartPoint.Y, 0);
                var body = (new Extents3d(rectangleStartPoint, rectangleEndPoint)).ToRectangle();
                activeDb.ModelSpace.Add(body);
                body.Layer = ThPDSLayerService.TableFrameLayer();
            }
        }

        private static string GetOutType(PDSBaseOutCircuit circuitForm)
        {
            switch (circuitForm.CircuitFormType)
            {
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        var circuit = circuitForm as DistributionMetering_ShanghaiCTCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return CircuitFormOutType.配电计量_上海直接表.GetDescription();
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        var circuit = circuitForm as DistributionMetering_ShanghaiMTCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return CircuitFormOutType.配电计量_上海CT.GetDescription();
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                    {
                        var circuit = circuitForm as DistributionMetering_CTInFrontCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return CircuitFormOutType.配电计量_直接表在前.GetDescription();
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        var circuit = circuitForm as DistributionMetering_MTInFrontCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return CircuitFormOutType.配电计量_CT表在前.GetDescription();
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                    {
                        var circuit = circuitForm as DistributionMetering_CTInBehindCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return CircuitFormOutType.配电计量_直接表在后.GetDescription();
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        var circuit = circuitForm as DistributionMetering_MTInBehindCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return CircuitFormOutType.配电计量_CT表在后.GetDescription();
                        }
                        break;
                    }
            }
            return circuitForm.CircuitFormType.GetDescription();
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

        private static Point3d PointClone(Point3d srcPoint)
        {
            return new Point3d(srcPoint.X, srcPoint.Y, 0);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
