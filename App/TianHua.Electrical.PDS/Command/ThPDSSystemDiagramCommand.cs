using System;
using System.Collections.Generic;
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
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph { get; set; }
        public List<ThPDSProjectGraphNode> NodeList { get; set; }

        public ThPDSSystemDiagramCommand(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            List<ThPDSProjectGraphNode> nodeList)
        {
            Graph = graph;
            NodeList = nodeList;
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
            {
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

                // 插入内框
                var rowCount = 1;
                insertEngine.Insert2(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_FRAME,
                    basePoint, scale * 100.0, rowCount);
                var nextInner = PointClone(basePoint);
                basePoint = new Point3d(basePoint.X + 5700.0 * scaleFactor, basePoint.Y + 57400.0 * scaleFactor, 0);
                var anotherStartPoint = new Point3d(basePoint.X + 36300 * scaleFactor, basePoint.Y, 0);
                var residue = ThPDSCommon.INNER_TOLERANCE - 1000.0;

                foreach (var thisNode in NodeList)
                {
                    if (thisNode.Type != PDSNodeType.DistributionBox)
                    {
                        continue;
                    }

                    var enterType = thisNode.Details.CircuitFormType.CircuitFormType.GetDescription();
                    if (enterType == CircuitFormInType.None.ToString())
                    {
                        continue;
                    }

                    var blankLineCount = 3;
                    if (enterType.Equals(CircuitFormInType.二路进线ATSE.GetDescription()))
                    {
                        blankLineCount = 7;
                    }
                    else if (enterType.Equals(CircuitFormInType.三路进线.GetDescription()))
                    {
                        blankLineCount = 7;
                    }
                    else if (enterType.Equals(CircuitFormInType.集中电源.GetDescription()))
                    {
                        blankLineCount = 8;
                    }

                    var tableObjs = new List<Entity>();
                    var startPoint = PointClone(basePoint);
                    // 插入表头
                    var title = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TITLE,
                        basePoint, scale);
                    assignment.TableTitleAssign(activeDb, title, thisNode, tableObjs);

                    // 计算表身起点
                    var bodyStartPoint = new Point3d(basePoint.X, basePoint.Y - 2500 * scaleFactor, 0);

                    // 插入进线回路
                    basePoint = new Point3d(bodyStartPoint.X, bodyStartPoint.Y - 500 * scaleFactor, 0);
                    var enterCircuit = insertEngine.Insert1(activeDb, configDb, enterType, basePoint, scale);
                    var busbar = assignment.EnterCircuitAssign(activeDb, configDb, enterCircuit, Graph, thisNode, scale, tableObjs);

                    // 插入空白行
                    var firstRowPoint = new Point3d(bodyStartPoint.X + 5000 * scaleFactor, bodyStartPoint.Y - 500 * scaleFactor, 0);
                    insertEngine.InsertBlankLine(activeDb, configDb, firstRowPoint, scale, tableObjs);

                    // 母排起点
                    var busbarStartPoint = new Point3d(firstRowPoint.X, firstRowPoint.Y - 500 * scaleFactor, 0);

                    // 插入出线回路
                    basePoint = PointClone(firstRowPoint);
                    var edges = Graph.Edges.Where(e => e.Source.Equals(thisNode))
                        .OrderBy(e => e.Circuit.ID.CircuitNumber.Last())
                        .ToList();
                    foreach (var edge in edges)
                    {
                        if (edge.Details == null)
                        {
                            continue;
                        }
                        var outType = GetOutType(edge.Details.CircuitForm);
                        basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                        var outCircuit = insertEngine.Insert1(activeDb, configDb, outType, basePoint, scale);
                        assignment.OutCircuitAssign(activeDb, configDb, outCircuit, edge, scale, tableObjs);
                    }

                    if (edges.Count < blankLineCount)
                    {
                        for (var i = 0; i < blankLineCount - edges.Count; i++)
                        {
                            basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                            insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                        }
                    }

                    // 母排终点
                    var busbarEndPoint = new Point3d(basePoint.X, basePoint.Y - 500 * scaleFactor, 0);

                    // 变换母排
                    if (busbar.Item1)
                    {
                        busbar.Item2.SetPointAt(0, busbarStartPoint.ToPoint2D());
                        busbar.Item2.SetPointAt(1, busbarEndPoint.ToPoint2D());
                    }

                    // 插入空白行
                    var lastRowPoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                    insertEngine.InsertBlankLine(activeDb, configDb, lastRowPoint, scale, tableObjs);

                    // 插入表尾
                    basePoint = new Point3d(lastRowPoint.X - 5000 * scaleFactor, lastRowPoint.Y - 500 * scaleFactor, 0);
                    var tableTable = thisNode.Load.Phase == ThPDSPhase.一相
                        ? ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE : ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_THREE_PHASE;
                    var tail = insertEngine.Insert1(activeDb, configDb, tableTable, basePoint, scale);
                    assignment.TableTailAssign(activeDb, tail, thisNode, tableObjs);

                    // 计算表身终点
                    var bodyEndPoint = new Point3d(basePoint.X + 27900 * scaleFactor, basePoint.Y, 0);
                    var rectangleStartPoint = new Point3d(bodyStartPoint.X, bodyEndPoint.Y, 0);
                    var rectangleEndPoint = new Point3d(bodyEndPoint.X, bodyStartPoint.Y, 0);
                    var body = (new Extents3d(rectangleStartPoint, rectangleEndPoint)).ToRectangle();
                    activeDb.ModelSpace.Add(body);
                    body.Layer = ThPDSLayerService.TableFrameLayer();
                    tableObjs.Add(body);

                    basePoint = new Point3d(basePoint.X, basePoint.Y - 1936.3333 * scaleFactor, 0);
                    var endPoint = PointClone(basePoint);

                    residue -= (startPoint.Y - endPoint.Y);
                    if (residue < -1.0)
                    {
                        rowCount++;
                        residue = ThPDSCommon.INNER_TOLERANCE - 1000.0 - (startPoint.Y - endPoint.Y);
                        if (rowCount % 2 == 0)
                        {
                            var displaceMent = Matrix3d.Displacement(anotherStartPoint - startPoint);
                            tableObjs.ForEach(o => o.TransformBy(displaceMent));
                            basePoint = anotherStartPoint + (endPoint - startPoint);
                        }
                        else
                        {
                            if (rowCount / 10 > 0 && rowCount % 10 == 1)
                            {
                                // 换行
                                nextInner = new Point3d(nextInner.X - 356400.0 * scaleFactor, nextInner.Y - 64400.0 * scaleFactor, 0);
                                anotherStartPoint = new Point3d(nextInner.X + 42000 * scaleFactor, nextInner.Y + 57400.0 * scaleFactor, 0);
                            }
                            else
                            {
                                nextInner = new Point3d(nextInner.X + 89100.0 * scaleFactor, nextInner.Y, 0);
                                anotherStartPoint = new Point3d(nextInner.X + 42000 * scaleFactor, nextInner.Y + 57400.0 * scaleFactor, 0);
                            }
                            insertEngine.Insert2(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_FRAME,
                                nextInner, scale * 100.0, rowCount);
                            basePoint = new Point3d(nextInner.X + 5700.0 * scaleFactor, nextInner.Y + 57400.0 * scaleFactor, 0);
                            var displaceMent = Matrix3d.Displacement(basePoint - startPoint);
                            tableObjs.ForEach(o => o.TransformBy(displaceMent));
                            basePoint += (endPoint - startPoint);
                        }
                    }
                }
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
