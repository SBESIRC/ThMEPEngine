using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using QuikGraph;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Diagram;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using ProjectGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSSystemDiagramService
    {
        private ProjectGraph Graph { get; set; }
        private List<ThPDSProjectGraphNode> Nodes { get; set; }
        public ThPDSSystemDiagramService()
        {

        }

        /// <summary>
        /// 单独生成
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public void Draw(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            Graph = graph;
            Nodes = new List<ThPDSProjectGraphNode> { node };
            Draw();
        }

        /// <summary>
        /// 批量生成
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nodes"></param>
        public void Draw(ProjectGraph graph, List<ThPDSProjectGraphNode> nodes)
        {
            Graph = graph;
            Nodes = nodes;
            Draw();
        }

        /// <summary>
        /// 全部生成
        /// </summary>
        /// <param name="graph"></param>
        public void Draw(ProjectGraph graph)
        {
            Graph = graph;
            Nodes = Graph.Vertices.ToList();
            Draw();
        }

        private void Draw()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
            using (var activeDb = AcadDatabase.Active())
            {
                if (!ThPDSSelectPointService.TrySelectPoint(out var selectPoint, "\n选择图纸基点"))
                {
                    return;
                }

                var basePoint = Point3d.Origin;
                var scaleFactor = 1.0;
                var scale = new Scale3d(scaleFactor, scaleFactor, scaleFactor);
                activeDb.Layers.Import(configDb.Layers.ElementOrDefault("0"), false);
                var insertEngine = new ThPDSBlockInsertEngine();
                var assignment = new ThPDSDiagramAssignment();
                var ucsToWcs = ThPDSUcsToWcsDecomposeService.Decompose();
                var transform = ucsToWcs.PreMultiplyBy(Matrix3d.Displacement(selectPoint - Point3d.Origin));
                var results = new List<Entity>();
                var meterLocation = MeterLocation.None;

                // 插入内框
                var rowCount = 1;
                insertEngine.Insert2(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_TABLE_FRAME, basePoint, scale * 100.0, rowCount, results);
                var nextInner = PointClone(basePoint);
                basePoint = new Point3d(basePoint.X + 5700.0 * scaleFactor, basePoint.Y + 57400.0 * scaleFactor, 0);
                var anotherStartPoint = new Point3d(basePoint.X + 36300 * scaleFactor, basePoint.Y, 0);
                var residue = ThPDSCommon.INNER_TOLERANCE - 1000.0;

                foreach (var thisNode in Nodes.Distinct())
                {
                    if (!Graph.Vertices.Contains(thisNode))
                    {
                        continue;
                    }

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
                    basePoint = new Point3d(firstRowPoint.X, firstRowPoint.Y - 1000.0 * scaleFactor, 0);
                    var edgeCount = 0;

                    // 所有不在小母排/控制回路上的分支
                    var ordinaryEdges = ThPDSProjectGraphService.GetOrdinaryCircuit(Graph, thisNode).GetSortedEdges().ToList();
                    DrawCircuit(ordinaryEdges, activeDb, configDb, scale, scaleFactor, tableObjs, meterLocation, ref basePoint);
                    edgeCount += ordinaryEdges.Count;

                    // 小母排节下分支
                    thisNode.Details.MiniBusbars.Keys.ForEach(o =>
                    {
                        basePoint = new Point3d(basePoint.X, basePoint.Y - 500.0 * scaleFactor, 0);
                        var smallBusbar = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.SMALL_BUSBAR, basePoint, scale);
                        var smallBusbarLine = assignment.SmallBusbarAssign(activeDb, configDb, smallBusbar, tableObjs, o, scale);
                        basePoint = new Point3d(basePoint.X, basePoint.Y + 500.0 * scaleFactor, 0);

                        var smallBusbarEdges = ThPDSProjectGraphService.GetSmallBusbarCircuit(Graph, thisNode, o).GetSortedEdges().ToList();
                        DrawCircuit(smallBusbarEdges, activeDb, configDb, scale, scaleFactor, tableObjs, meterLocation, ref basePoint, true);
                        edgeCount += smallBusbarEdges.Count;

                        if (smallBusbarEdges.Count < 2)
                        {
                            for (var i = 0; i < 2 - smallBusbarEdges.Count; i++)
                            {
                                insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                                basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                            }
                        }

                        var smallBusbarEndPoint = new Point3d(basePoint.X + 4700.0 * scaleFactor, basePoint.Y + -500.0 * scaleFactor, 0);
                        smallBusbarLine.SetPointAt(1, smallBusbarEndPoint.ToPoint2D());
                    });

                    // 控制回路节下分支
                    if (thisNode.Details.SecondaryCircuits.Keys.Count > 0)
                    {
                        var circuitDatas = new List<ThPDSControlCircuitData>();
                        thisNode.Details.SecondaryCircuits.Keys.ForEach(o =>
                        {
                            var circuitData = new ThPDSControlCircuitData();
                            var controlEdges = ThPDSProjectGraphService.GetControlCircuit(Graph, thisNode, o).GetSortedEdges().ToList();
                            circuitData.CircuitUID = controlEdges[0].Circuit.CircuitUID;
                            var dataList = circuitDatas.Where(data => data.CircuitUID.Equals(circuitData.CircuitUID)).ToList();
                            if (dataList.Count > 0)
                            {
                                circuitData.BelongToCPS = dataList[0].BelongToCPS;
                                circuitData.StartPoint = dataList[0].StartPoint;
                            }
                            else
                            {
                                var controlStartPoint1 = new Point3d(basePoint.X + 1333.4936 * scaleFactor, basePoint.Y - 211.6025 * scaleFactor, 0);
                                var controlStartPoint2 = new Point3d(basePoint.X + 3979.3671 * scaleFactor, basePoint.Y - 156.25 * scaleFactor, 0);
                                var belongToCPS = false;

                                DrawCircuit(controlEdges, activeDb, configDb, scale, scaleFactor, tableObjs, meterLocation, ref basePoint, ref belongToCPS);
                                edgeCount += controlEdges.Count;
                                circuitDatas.Add(circuitData);
                                circuitData.BelongToCPS = belongToCPS;
                                circuitData.StartPoint = belongToCPS ? controlStartPoint1 : controlStartPoint2;
                            }

                            if (circuitData.BelongToCPS)
                            {
                                var controlCircuit = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.CONTROL_CIRCUIT_BELONG_TO_CPS, basePoint, scale);
                                assignment.ControlCircuitAssign(activeDb, controlCircuit, tableObjs, o);
                                var controlEndPoint1 = new Point3d(basePoint.X + 1333.4936 * scaleFactor, basePoint.Y, 0);
                                circuitData.EndPoint = controlEndPoint1;
                                if (dataList.Count > 0)
                                {
                                    dataList[0].EndPoint = controlEndPoint1;
                                }
                            }
                            else
                            {
                                var controlCircuit = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.CONTROL_CIRCUIT_BELONG_TO_QAC, basePoint, scale);
                                assignment.ControlCircuitAssign(activeDb, controlCircuit, tableObjs, o);
                                var controlEndPoint2 = new Point3d(basePoint.X + 3979.3671 * scaleFactor, basePoint.Y, 0);
                                circuitData.EndPoint = controlEndPoint2;
                                if (dataList.Count > 0)
                                {
                                    dataList[0].EndPoint = controlEndPoint2;
                                }
                            }

                            edgeCount++;
                            basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                        });

                        circuitDatas.ForEach(data =>
                        {
                            var line = new Line(data.StartPoint, data.EndPoint);
                            tableObjs.Add(line);
                            activeDb.ModelSpace.Add(line);
                            line.Layer = ThPDSLayerService.ControlCircuitLayer();
                        });
                    }

                    // 浪涌保护器
                    if (thisNode.Details.SurgeProtection != SurgeProtectionDeviceType.None)
                    {
                        var surgeProtection = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.SURGE_PROTECTION, basePoint, scale);
                        assignment.SurgeProtectionAssign(activeDb, surgeProtection, tableObjs, thisNode.Details.SurgeProtection.ToString());
                        insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                        basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                        edgeCount++;
                    }

                    if (edgeCount < blankLineCount)
                    {
                        for (var i = 0; i < blankLineCount - edgeCount; i++)
                        {
                            insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                            basePoint = new Point3d(basePoint.X, basePoint.Y - 1000 * scaleFactor, 0);
                        }
                    }

                    // 母排终点
                    var busbarEndPoint = new Point3d(basePoint.X, basePoint.Y + 500 * scaleFactor, 0);

                    // 变换母排
                    if (busbar.Item1)
                    {
                        busbar.Item2.SetPointAt(0, busbarStartPoint.ToPoint2D());
                        busbar.Item2.SetPointAt(1, busbarEndPoint.ToPoint2D());
                    }

                    // 插入空白行
                    insertEngine.InsertBlankLine(activeDb, configDb, basePoint, scale, tableObjs);
                    basePoint = new Point3d(basePoint.X - 5000 * scaleFactor, basePoint.Y - 500 * scaleFactor, 0);
                    var tailPoint = PointClone(basePoint);

                    // 插入表尾
                    var tableTable = thisNode.Load.Phase == ThPDSPhase.一相
                        ? ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE : ThPDSCommon.SYSTEM_DIAGRAM_TABLE_TAIL_THREE_PHASE;
                    if (thisNode.Details.LoadCalculationInfo.IsDualPower && thisNode.Details.LoadCalculationInfo.LowPower > 0)
                    {
                        var tailInLow = insertEngine.Insert1(activeDb, configDb, tableTable, basePoint, scale);
                        assignment.TableTailAssign(activeDb, tailInLow, thisNode, tableObjs, thisNode.Details.LoadCalculationInfo.LowPower);
                        basePoint = new Point3d(basePoint.X, basePoint.Y - 936.3333 * scaleFactor, 0);
                    }
                    var tailInHigh = insertEngine.Insert1(activeDb, configDb, tableTable, basePoint, scale);
                    assignment.TableTailAssign(activeDb, tailInHigh, thisNode, tableObjs, thisNode.Details.LoadCalculationInfo.HighPower);
                    basePoint = new Point3d(basePoint.X, basePoint.Y - 936.3333 * scaleFactor, 0);

                    // 计算表身终点
                    var bodyEndPoint = new Point3d(basePoint.X + 27900 * scaleFactor, tailPoint.Y, 0);
                    var rectangleStartPoint = new Point3d(bodyStartPoint.X, bodyEndPoint.Y, 0);
                    var rectangleEndPoint = new Point3d(bodyEndPoint.X, bodyStartPoint.Y, 0);
                    var body = (new Extents3d(rectangleStartPoint, rectangleEndPoint)).ToRectangle();
                    activeDb.ModelSpace.Add(body);
                    body.Layer = ThPDSLayerService.TableFrameLayer();
                    tableObjs.Add(body);

                    // 二次结线
                    if (!enterType.Equals(CircuitFormInType.集中电源.GetDescription()))
                    {
                        var allCircuits = ThPDSProjectGraphService.GetCircuit(Graph, thisNode);
                        var motorCircuit = allCircuits.Where(c => c.Target.Load.LoadTypeCat_1.Equals(ThPDSLoadTypeCat_1.Motor)).ToList();
                        if (motorCircuit.Count > 0)
                        {
                            var motorCircuitSort = MotorCircuitSort(motorCircuit);
                            motorCircuitSort.ForEach(circuits =>
                            {
                                if (circuits[0].Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
                                {
                                    var secJunction = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_SECONDARY_JUNCTION, basePoint, scale);
                                    assignment.SecJunctionAssign(activeDb, secJunction, tableObjs);
                                    basePoint = new Point3d(basePoint.X, basePoint.Y - 7000.0 * scaleFactor, 0);
                                }
                                else
                                {
                                    var primarySpareSort = PrimarySpareSort(circuits);
                                    primarySpareSort.ForEach(primary =>
                                    {
                                        var dualPowerSort = DualPowerSort(primary);
                                        dualPowerSort.ForEach(dualPower =>
                                        {
                                            var secJunction = insertEngine.Insert1(activeDb, configDb, ThPDSCommon.SYSTEM_DIAGRAM_SECONDARY_JUNCTION, basePoint, scale);
                                            assignment.SecJunctionAssign(activeDb, secJunction, tableObjs, thisNode, dualPower);
                                            basePoint = new Point3d(basePoint.X, basePoint.Y - 7000.0 * scaleFactor, 0);
                                        });
                                    });
                                }
                            });
                        }
                    }

                    basePoint = new Point3d(basePoint.X, basePoint.Y - 1000.0 * scaleFactor, 0);
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
                                nextInner, scale * 100.0, rowCount, results);
                            basePoint = new Point3d(nextInner.X + 5700.0 * scaleFactor, nextInner.Y + 57400.0 * scaleFactor, 0);
                            var displaceMent = Matrix3d.Displacement(basePoint - startPoint);
                            tableObjs.ForEach(o => o.TransformBy(displaceMent));
                            basePoint += (endPoint - startPoint);
                        }
                    }
                    results.AddRange(tableObjs);
                }
                results.ForEach(o => o.TransformBy(transform));
            }

            // 刷新表格中的公式计算值
            Active.Editor.Regen();
        }

        private static void DrawCircuit(List<ThPDSProjectGraphEdge> edges, AcadDatabase activeDb, AcadDatabase configDb,
            Scale3d scale, double scaleFactor, List<Entity> tableObjs, MeterLocation meterLocation, ref Point3d basePoint, bool isSmallBusbar = false)
        {
            foreach (var edge in edges)
            {
                if (edge.Details == null)
                {
                    continue;
                }
                var outType = GetOutType(edge.Details.CircuitForm);
                if (isSmallBusbar)
                {
                    outType = Tuple.Create(ThPDSCommon.SMALL_BUSBAR_Circuit, 1000.0);
                }
                var insertEngine = new ThPDSBlockInsertEngine();
                var assignment = new ThPDSDiagramAssignment();
                var outCircuit = insertEngine.Insert1(activeDb, configDb, outType.Item1, basePoint, scale);
                var circuitEntities = assignment.OutCircuitAssign(activeDb, configDb, outCircuit, edge, scale, ref meterLocation, ref basePoint);
                tableObjs.AddRange(circuitEntities);
                basePoint = new Point3d(basePoint.X, basePoint.Y - outType.Item2 * scaleFactor, 0);
            }
        }

        private static void DrawCircuit(List<ThPDSProjectGraphEdge> edges, AcadDatabase activeDb, AcadDatabase configDb,
            Scale3d scale, double scaleFactor, List<Entity> tableObjs, MeterLocation meterLocation, ref Point3d basePoint, ref bool belongToCPS)
        {
            foreach (var edge in edges)
            {
                if (edge.Details == null)
                {
                    continue;
                }
                if (!belongToCPS && edge.Details.CircuitForm.CircuitFormType.GetDescription().Contains("CPS"))
                {
                    belongToCPS = true;
                }
                var outType = GetOutType(edge.Details.CircuitForm);
                var insertEngine = new ThPDSBlockInsertEngine();
                var assignment = new ThPDSDiagramAssignment();
                var outCircuit = insertEngine.Insert1(activeDb, configDb, outType.Item1, basePoint, scale);
                var circuitEntities = assignment.OutCircuitAssign(activeDb, configDb, outCircuit, edge, scale, ref meterLocation, ref basePoint);
                tableObjs.AddRange(circuitEntities);
                basePoint = new Point3d(basePoint.X, basePoint.Y - outType.Item2 * scaleFactor, 0);
            }
        }

        private static Tuple<string, double> GetOutType(PDSBaseOutCircuit circuitForm)
        {
            switch (circuitForm.CircuitFormType)
            {
                case CircuitFormOutType.常规:
                    {
                        return Tuple.Create(CircuitFormOutType.常规.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.漏电:
                    {
                        return Tuple.Create(CircuitFormOutType.漏电.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        return Tuple.Create(CircuitFormOutType.接触器控制.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        return Tuple.Create(CircuitFormOutType.热继电器保护.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        var circuit = circuitForm as DistributionMetering_ShanghaiCTCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_上海直接表.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_上海CT.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        var circuit = circuitForm as DistributionMetering_ShanghaiMTCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_上海CT.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_上海直接表.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                    {
                        var circuit = circuitForm as DistributionMetering_CTInFrontCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_直接表在前.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_CT表在前.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        var circuit = circuitForm as DistributionMetering_MTInFrontCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_CT表在前.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_直接表在前.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                    {
                        var circuit = circuitForm as DistributionMetering_CTInBehindCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(MeterTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_直接表在后.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_CT表在后.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        var circuit = circuitForm as DistributionMetering_MTInBehindCircuit;
                        var type = ComponentTypeSelector.GetComponentType(circuit.meter.ComponentType);
                        if (type.Equals(typeof(CurrentTransformer)))
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_CT表在后.GetDescription(), 1000.0);
                        }
                        else
                        {
                            return Tuple.Create(CircuitFormOutType.配电计量_直接表在后.GetDescription(), 1000.0);
                        }
                    }
                case CircuitFormOutType.电动机_分立元件:
                    {
                        return Tuple.Create(CircuitFormOutType.电动机_分立元件.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        return Tuple.Create(CircuitFormOutType.电动机_CPS.GetDescription(), 1000.0);
                    }
                case CircuitFormOutType.电动机_分立元件星三角启动:
                    {
                        return Tuple.Create(CircuitFormOutType.电动机_分立元件星三角启动.GetDescription(), 3000.0);
                    }
                case CircuitFormOutType.电动机_CPS星三角启动:
                    {
                        return Tuple.Create(CircuitFormOutType.电动机_CPS星三角启动.GetDescription(), 3000.0);
                    }
                case CircuitFormOutType.双速电动机_分立元件detailYY:
                    {
                        return Tuple.Create(CircuitFormOutType.双速电动机_分立元件detailYY.GetDescription(), 3000.0);
                    }
                case CircuitFormOutType.双速电动机_分立元件YY:
                    {
                        return Tuple.Create(CircuitFormOutType.双速电动机_分立元件YY.GetDescription(), 2000.0);
                    }
                case CircuitFormOutType.双速电动机_CPSdetailYY:
                    {
                        return Tuple.Create(CircuitFormOutType.双速电动机_CPSdetailYY.GetDescription(), 2000.0);
                    }
                case CircuitFormOutType.双速电动机_CPSYY:
                    {
                        return Tuple.Create(CircuitFormOutType.双速电动机_CPSYY.GetDescription(), 3000.0);
                    }
                case CircuitFormOutType.消防应急照明回路WFEL:
                    {
                        return Tuple.Create(CircuitFormOutType.消防应急照明回路WFEL.GetDescription(), 1000.0);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private static List<List<ThPDSProjectGraphEdge>> MotorCircuitSort(List<ThPDSProjectGraphEdge> motorCircuit)
        {
            var result = new List<List<ThPDSProjectGraphEdge>>
            {
                new List<ThPDSProjectGraphEdge>(),
            };
            result[0].Add(motorCircuit[0]);
            for (var i = 1; i < motorCircuit.Count; i++)
            {
                for (var j = 0; j < result.Count; j++)
                {
                    if (motorCircuit[i].Target.Load.LoadTypeCat_3.Equals(result[j][0].Target.Load.LoadTypeCat_3))
                    {
                        result[j].Add(motorCircuit[i]);
                    }
                    else if (j == result.Count - 1)
                    {
                        result.Add(new List<ThPDSProjectGraphEdge>());
                        result.Last().Add(motorCircuit[i]);
                        break;
                    }
                }
            }

            for (var i = 0; i < result.Count - 1; i++)
            {
                if (result[i][0].Target.Load.LoadTypeCat_3.Equals(ThPDSLoadTypeCat_3.None))
                {
                    var temp = result[i];
                    result[i] = result[result.Count - 1];
                    result[result.Count - 1] = temp;
                    break;
                }
            }
            return result;
        }

        private static List<List<ThPDSProjectGraphEdge>> PrimarySpareSort(List<ThPDSProjectGraphEdge> motorCircuit)
        {
            var result = new List<List<ThPDSProjectGraphEdge>>
            {
                new List<ThPDSProjectGraphEdge>(),
            };
            result[0].Add(motorCircuit[0]);
            for (var i = 1; i < motorCircuit.Count; i++)
            {
                for (var j = 0; j < result.Count; j++)
                {
                    if (motorCircuit[i].Target.Load.PrimaryAvail.Equals(result[j][0].Target.Load.PrimaryAvail)
                        && motorCircuit[i].Target.Load.SpareAvail.Equals(result[j][0].Target.Load.SpareAvail))
                    {
                        result[j].Add(motorCircuit[i]);
                    }
                    else if (j == result.Count - 1)
                    {
                        result.Add(new List<ThPDSProjectGraphEdge>());
                        result.Last().Add(motorCircuit[i]);
                        break;
                    }
                }
            }
            return result;
        }

        private static List<List<ThPDSProjectGraphEdge>> DualPowerSort(List<ThPDSProjectGraphEdge> motorCircuit)
        {
            var result = new List<List<ThPDSProjectGraphEdge>>
            {
                new List<ThPDSProjectGraphEdge>(),
            };
            result[0].Add(motorCircuit[0]);
            for (var i = 1; i < motorCircuit.Count; i++)
            {
                for (var j = 0; j < result.Count; j++)
                {
                    if (motorCircuit[i].Target.Load.InstalledCapacity.IsDualPower.Equals(result[j][0].Target.Load.InstalledCapacity.IsDualPower))
                    {
                        result[j].Add(motorCircuit[i]);
                    }
                    else if (j == result.Count - 1)
                    {
                        result.Add(new List<ThPDSProjectGraphEdge>());
                        result.Last().Add(motorCircuit[i]);
                        break;
                    }
                }
            }
            return result;
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

    public enum MeterLocation
    {
        None,
        Head,
        Middle,
        Tail,
    }
}
