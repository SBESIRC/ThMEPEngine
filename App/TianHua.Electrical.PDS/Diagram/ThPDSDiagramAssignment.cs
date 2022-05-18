using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using QuikGraph;

using ThCADExtension;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSDiagramAssignment
    {
        public void FrameAssign(ObjectId objectId, int frameNum)
        {
            var key = "内框名称";
            var value = "配电箱系统图（" + frameNum.NumberToChinese() + "）";
            objectId.UpdateAttributesInBlock(new Dictionary<string, string> { { key, value } });
        }

        public void TableTitleAssign(AcadDatabase activeDb, BlockReference title, ThPDSProjectGraphNode node, List<Entity> tableObjs)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, title);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var texts = objs.OfType<DBText>().ToList();

            // 配电箱编号
            var loadId = texts.Where(t => t.TextString == ThPDSCommon.DISTRIBUTION_BOX_ID).First();
            loadId.TextString = node.Load.ID.LoadID;

            // 设备用途
            var application = texts.Where(t => t.TextString == ThPDSCommon.APPLICATION).First();
            application.TextString = node.Load.ID.Description;

            // 消防负荷
            var fireLoad = texts.Where(t => t.TextString == ThPDSCommon.FIRE_LOAD).First();
            fireLoad.TextString = node.Load.FireLoad ? "是" : "否";

            // 参考尺寸
            var overallDimensions = texts.Where(t => t.TextString == ThPDSCommon.OVERALL_DIMENSIONS).First();
            overallDimensions.TextString = node.Details.BoxSize.GetDescription();

            // 安装位置
            var location = texts.Where(t => t.TextString == ThPDSCommon.LOCATION).First();
            location.TextString = node.Load.Location.FloorNumber;

            // 安装方式
            var installMethod = texts.Where(t => t.TextString == ThPDSCommon.INSTALLMETHOD).First();
            installMethod.TextString = node.Details.BoxInstallationType.GetDescription();
        }

        public Tuple<bool, Polyline> EnterCircuitAssign(AcadDatabase activeDb, AcadDatabase configDb, BlockReference circuitBlock,
            BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, Scale3d scale, List<Entity> tableObjs)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuitBlock);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var texts = objs.OfType<DBText>().ToList();
            var components = objs.OfType<BlockReference>().ToList();

            // 进线回路编号
            var circuitTexts = texts.Where(t => t.TextString.Equals(ThPDSCommon.ENTER_CIRCUIT_ID))
                .OrderByDescending(t => t.Position.Y)
                .ToList();
            var circuitNumbers = ThPDSCircuitNumberSeacher.Seach(node, graph);
            if (circuitNumbers.Count == circuitTexts.Count)
            {
                for (var i = 0; i < circuitNumbers.Count; i++)
                {
                    circuitTexts[i].TextString = circuitNumbers[i];
                }
            }
            else
            {
                for (var i = 0; i < circuitTexts.Count; i++)
                {
                    circuitTexts[i].Erase();
                }
            }

            var insertEngine = new ThPDSBlockInsertEngine();
            switch (FetchDescription(circuitBlock.Name))
            {
                case CircuitFormInType.一路进线:
                    {
                        var circuit = node.Details.CircuitFormType as OneWayInCircuit;

                        // 隔离开关
                        if (circuit.Component == null)
                        {
                            break;
                        }
                        var srcIsolatingSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH).First();
                        var componentName = ThPDSComponentMap.ComponentMap[circuit.Component.ComponentType.GetDescription()];
                        var firstPosition = srcIsolatingSwitch.Position;
                        if (!componentName.Equals(srcIsolatingSwitch.BlockName))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, componentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcIsolatingSwitch.Erase();
                        }
                        var QLText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL).First();
                        if (circuit.Component is IsolatingSwitch isolatingSwitch)
                        {
                            QLText.TextString = isolatingSwitch.Content();
                        }
                        else if (circuit.Component is Breaker breaker)
                        {
                            QLText.TextString = breaker.Content();
                        }

                        // 电能表
                        // To do

                        // 消防电源监控
                        if (node.Details.FirePowerMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.FIRE_POWER_MONITORING_1, firstPosition, scale);
                        }
                        // 电气火灾监控
                        if (node.Details.ElectricalFireMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.ELECTRICAL_FIRE_MONITORING_1, firstPosition, scale);
                        }

                        return Tuple.Create(true, objs.OfType<Polyline>().First());
                    }
                case CircuitFormInType.二路进线ATSE:
                    {
                        var circuit = node.Details.CircuitFormType as TwoWayInCircuit;

                        var srcIsolatingSwitchs = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        var QLTexts = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL_250_4P)
                            .OrderByDescending(t => t.Position.Y).ToList();

                        // 隔离开关1
                        var firstIsolatingSwitch = srcIsolatingSwitchs[0];
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.Component1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(firstIsolatingSwitch.BlockName))
                        {
                            var firstPosition = firstIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            firstIsolatingSwitch.Erase();
                        }
                        var firstQLText = QLTexts[0];
                        if (circuit.Component1 is IsolatingSwitch isolatingSwitch)
                        {
                            firstQLText.TextString = isolatingSwitch.Content();
                        }
                        else if (circuit.Component1 is Breaker breaker)
                        {
                            firstQLText.TextString = breaker.Content();
                        }

                        // 隔离开关2
                        var secondIsolatingSwitch = srcIsolatingSwitchs[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.Component2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondIsolatingSwitch.BlockName))
                        {
                            var secondPosition = secondIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            secondIsolatingSwitch.Erase();
                        }
                        var secondQLText = QLTexts[1];
                        if (circuit.Component2 is IsolatingSwitch isolatingSwitch2)
                        {
                            secondQLText.TextString = isolatingSwitch2.Content();
                        }
                        else if (circuit.Component2 is Breaker breaker)
                        {
                            secondQLText.TextString = breaker.Content();
                        }

                        // 转换开关
                        var srcTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_TRANSFER_SWITCH).First();
                        var transferSwitchName = ThPDSComponentMap.ComponentMap[circuit.transferSwitch.ComponentType.GetDescription()];
                        var thirdPosition = srcTransferSwitch.Position;
                        if (!transferSwitchName.Equals(srcTransferSwitch.BlockName))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, transferSwitchName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcTransferSwitch.Erase();
                        }
                        var ATSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_ATSE_320A_4P).First();
                        var type = ComponentTypeSelector.GetComponentType(circuit.transferSwitch.ComponentType);
                        // ATSEText.TextString = circuit.transferSwitch.ComponentType.content;

                        // 消防电源监控
                        if (node.Details.FirePowerMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.FIRE_POWER_MONITORING_2, thirdPosition, scale);
                        }
                        // 电气火灾监控
                        if (node.Details.ElectricalFireMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.ELECTRICAL_FIRE_MONITORING_2, thirdPosition, scale);
                        }

                        return Tuple.Create(true, objs.OfType<Polyline>().First());
                    }
                case CircuitFormInType.三路进线:
                    {
                        var circuit = node.Details.CircuitFormType as ThreeWayInCircuit;

                        var srcIsolatingSwitchs = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        var QLTexts = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL_250_4P)
                            .OrderByDescending(t => t.Position.Y).ToList();

                        // 隔离开关1
                        var firstIsolatingSwitch = srcIsolatingSwitchs[0];
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.Component1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(firstIsolatingSwitch.BlockName))
                        {
                            var firstPosition = firstIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            firstIsolatingSwitch.Erase();
                        }
                        var firstQLText = QLTexts[0];
                        if (circuit.Component1 is IsolatingSwitch isolatingSwitch)
                        {
                            firstQLText.TextString = isolatingSwitch.Content();
                        }
                        else if (circuit.Component1 is Breaker breaker)
                        {
                            firstQLText.TextString = breaker.Content();
                        }

                        // 隔离开关2
                        var secondIsolatingSwitch = srcIsolatingSwitchs[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.Component2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondIsolatingSwitch.BlockName))
                        {
                            var secondPosition = secondIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            secondIsolatingSwitch.Erase();
                        }
                        var secondQLText = QLTexts[1];
                        if (circuit.Component2 is IsolatingSwitch isolatingSwitch2)
                        {
                            secondQLText.TextString = isolatingSwitch2.Content();
                        }
                        else if (circuit.Component2 is Breaker breaker)
                        {
                            secondQLText.TextString = breaker.Content();
                        }

                        // 隔离开关3
                        var thirdIsolatingSwitch = srcIsolatingSwitchs[2];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.Component3.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(thirdIsolatingSwitch.BlockName))
                        {
                            var thirdPosition = thirdIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            thirdIsolatingSwitch.Erase();
                        }
                        var thirdQLText = QLTexts[2];
                        if (circuit.Component3 is IsolatingSwitch isolatingSwitch3)
                        {
                            thirdQLText.TextString = isolatingSwitch3.Content();
                        }
                        else if (circuit.Component3 is Breaker breaker)
                        {
                            thirdQLText.TextString = breaker.Content();
                        }

                        // 转换开关1
                        var srcTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_TRANSFER_SWITCH).First();
                        var transferSwitchName = ThPDSComponentMap.ComponentMap[circuit.transferSwitch1.ComponentType.GetDescription()];
                        var forthPosition = srcTransferSwitch.Position;
                        if (!transferSwitchName.Equals(srcTransferSwitch.BlockName))
                        {

                            var newComponent = insertEngine.Insert1(activeDb, configDb, transferSwitchName, forthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcTransferSwitch.Erase();
                        }
                        var ATSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_ATSE_320A_4P).First();
                        var ATSEtype = ComponentTypeSelector.GetComponentType(circuit.transferSwitch1.ComponentType);
                        //ATSEText.TextString = type.GetProperty("Content").GetValue(circuit.transferSwitch).ToString();

                        // 转换开关2
                        var srcManualTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_MANUAL_TRANSFER_SWITCH).First();
                        var manualTransferSwitchName = ThPDSComponentMap.ComponentMap[circuit.transferSwitch2.ComponentType.GetDescription()];
                        if (!manualTransferSwitchName.Equals(srcManualTransferSwitch.BlockName))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, manualTransferSwitchName, srcManualTransferSwitch.Position, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcManualTransferSwitch.Erase();
                        }
                        var MTSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_MTSE_320A_4P).First();
                        var MTSEtype = ComponentTypeSelector.GetComponentType(circuit.transferSwitch2.ComponentType);
                        //ATSEText.TextString = type.GetProperty("Content").GetValue(circuit.transferSwitch).ToString();

                        // 消防电源监控
                        if (node.Details.FirePowerMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.FIRE_POWER_MONITORING_2, forthPosition, scale);
                        }
                        // 电气火灾监控
                        if (node.Details.ElectricalFireMonitoring)
                        {
                            insertEngine.Insert(activeDb, configDb, ThPDSCommon.ELECTRICAL_FIRE_MONITORING_2, forthPosition, scale);
                        }

                        return Tuple.Create(true, objs.OfType<Polyline>().First());
                    }
                case CircuitFormInType.集中电源:
                    {
                        var circuit = node.Details.CircuitFormType as CentralizedPowerCircuit;

                        // 隔离开关
                        var srcIsolatingSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH_1).First();
                        var componentName = ThPDSComponentMap.ComponentMap[circuit.Component.ComponentType.GetDescription()];
                        if (!componentName.Equals(srcIsolatingSwitch.BlockName))
                        {
                            var firstPosition = srcIsolatingSwitch.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, componentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcIsolatingSwitch.Erase();
                        }
                        var QLText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL_25_1P).First();
                        if (circuit.Component is IsolatingSwitch isolatingSwitch)
                        {
                            QLText.TextString = isolatingSwitch.Content();
                        }
                        else if (circuit.Component is Breaker breaker)
                        {
                            QLText.TextString = breaker.Content();
                        }

                        break;
                    }
                case CircuitFormInType.None:
                    {
                        throw new NotImplementedException();
                    }
            }
            return Tuple.Create(false, new Polyline());
        }

        public void TableTailAssign(AcadDatabase activeDb, BlockReference tail, ThPDSProjectGraphNode node, List<Entity> tableObjs, double power)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, tail);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var table = objs.OfType<Table>().First();

            // Pn
            CellAssign(table.Cells[0, 1], power);
            // Kx
            CellAssign(table.Cells[0, 5], node.Load.DemandFactor);
            // cos(\Phi)
            CellAssign(table.Cells[0, 12], node.Load.PowerFactor);
        }

        private void CellAssign(Cell cell, double value)
        {
            var dataFormat = cell.DataFormat;
            cell.Value = value;
            cell.DataFormat = dataFormat;
        }

        public void OutCircuitAssign(AcadDatabase activeDb, AcadDatabase configDb, BlockReference circuitBlock,
             ThPDSProjectGraphEdge edge, Scale3d scale, List<Entity> tableObjs)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuitBlock);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var texts = objs.OfType<DBText>().ToList();
            var components = objs.OfType<BlockReference>().ToList();

            // 回路编号
            var circuitNumber = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CIRCUIT_NUMBER).First();
            circuitNumber.TextString = edge.Circuit.ID.CircuitID;

            // 相序
            var phaseSequence = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_PHSAE).First();
            phaseSequence.TextString = edge.Target.Details.PhaseSequence.GetDescription();

            // 功率
            var power = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_POWER).ToList();
            if (power.Count == 1)
            {
                power[0].TextString = edge.Target.Details.HighPower == 0 ? "" : edge.Target.Details.HighPower.ToString();
            }
            else
            {
                var lowPower = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_LOW_POWER).First();
                lowPower.TextString = edge.Target.Details.LowPower == 0 ? "" : edge.Target.Details.LowPower.ToString();
                var highPower = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_HIGH_POWER).First();
                highPower.TextString = edge.Target.Details.HighPower == 0 ? "" : edge.Target.Details.HighPower.ToString();
            }

            // 负载编号
            var loadID = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_LOAD_ID).First();

            // 功能用途
            var description = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_DESCRIPTION).First();

            var offset = new Vector3d(0, 250, 0);
            if (!string.IsNullOrEmpty(edge.Target.Load.ID.LoadID)
                && !string.IsNullOrEmpty(edge.Target.Load.ID.Description))
            {
                // 负载编号和功能用途都不为空时
                loadID.TextString = edge.Target.Load.ID.LoadID;
                description.TextString = edge.Target.Load.ID.Description;
            }
            else if (string.IsNullOrEmpty(edge.Target.Load.ID.LoadID)
                && !string.IsNullOrEmpty(edge.Target.Load.ID.Description))
            {
                loadID.Erase();
                description.TextString = edge.Target.Load.ID.Description;
                description.TransformBy(Matrix3d.Displacement(offset));
            }
            else if (!string.IsNullOrEmpty(edge.Target.Load.ID.LoadID)
                && string.IsNullOrEmpty(edge.Target.Load.ID.Description))
            {
                description.Erase();
                loadID.TextString = edge.Target.Load.ID.LoadID;
                loadID.TransformBy(Matrix3d.Displacement(-offset));
            }
            else
            {
                loadID.Erase();
                description.Erase();
            }

            var insertEngine = new ThPDSBlockInsertEngine();
            switch (edge.Details.CircuitForm.CircuitFormType)
            {
                case CircuitFormOutType.常规:
                    {
                        var circuit = edge.Details.CircuitForm as RegularCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstPosition = srcBreaker.Position;
                        var secondPosition = new Point3d(firstPosition.X + 2750, firstPosition.Y, 0);
                        var thirdPosition = new Point3d(secondPosition.X + 2025, secondPosition.Y, 0);

                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        // 元器件2
                        if (circuit.reservedComponent1 != null)
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.reservedComponent1.ComponentType.GetDescription()], secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                        }

                        // 元器件3
                        if (circuit.reservedComponent2 != null)
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.reservedComponent2.ComponentType.GetDescription()], thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                        }

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.漏电:
                    {
                        var circuit = edge.Details.CircuitForm as LeakageCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_RESIDUAL_CURRENT_DEVICE).First();
                        var firstPosition = srcBreaker.Position;
                        var secondPosition = new Point3d(firstPosition.X + 2750, firstPosition.Y, 0);
                        var thirdPosition = new Point3d(secondPosition.X + 2025, secondPosition.Y, 0);

                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var RCDText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_RCD).First();
                        RCDText.TextString = circuit.breaker.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        var circuit = edge.Details.CircuitForm as ContactorControlCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }

                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        // 元器件2
                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor.Name))
                        {
                            var secondPosition = srcContactor.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor.Erase();
                        }
                        var QACText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC).First();
                        QACText.TextString = circuit.contactor.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        var circuit = edge.Details.CircuitForm as ThermalRelayProtectionCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }

                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        // 元器件2
                        var srcThermalRelay = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcThermalRelay.Name))
                        {
                            var secondPosition = srcThermalRelay.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay.Erase();
                        }
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = circuit.thermalRelay.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_上海CT:
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        Breaker breaker1, breaker2;
                        Meter meter;
                        Conductor conductor;
                        if (edge.Details.CircuitForm.CircuitFormType == CircuitFormOutType.配电计量_上海CT)
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_ShanghaiCTCircuit;
                            breaker1 = circuit.breaker1;
                            meter = circuit.meter;
                            breaker2 = circuit.breaker2;
                            conductor = circuit.Conductor;
                        }
                        else
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_ShanghaiMTCircuit;
                            breaker1 = circuit.breaker1;
                            meter = circuit.meter;
                            breaker2 = circuit.breaker2;
                            conductor = circuit.Conductor;
                        }

                        var srcBreakers = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER)
                            .OrderBy(c => c.Position.X).ToList();
                        // 元器件1
                        var firstBreaker = srcBreakers[0];
                        var firstComponentName = ThPDSComponentMap.ComponentMap[breaker1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(firstBreaker.Name))
                        {
                            var firstPosition = firstBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            firstBreaker.Erase();
                        }
                        var CB1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB1).First();
                        CB1Text.TextString = breaker1.Content();

                        // 元器件2
                        var srcMeter = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CURRENT_TRANSFORMER).FirstOrDefault();
                        if (srcMeter == null)
                        {
                            // 无CT表
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            MTText.TextString = meter.Content();
                        }
                        else
                        {
                            // 有CT表
                            var CTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CT).First();
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            CTText.TextString = meter.Content();
                            if (meter is CurrentTransformer ct)
                            {
                                MTText.TextString = ct.Content();
                            }
                        }

                        // 元器件3
                        var secondBreaker = srcBreakers[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[breaker2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondBreaker.Name))
                        {
                            var secondPosition = secondBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            secondBreaker.Erase();
                        }
                        var CB2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB2).First();
                        CB2Text.TextString = breaker2.Content();

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!conductor.IsNull())
                        {
                            conductorText.TextString = conductor.Content;
                        }
                        else
                        {
                            conductorText.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        Breaker breaker;
                        Meter meter;
                        Conductor conductor;
                        if (edge.Details.CircuitForm.CircuitFormType == CircuitFormOutType.配电计量_CT表在前)
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_CTInFrontCircuit;
                            breaker = circuit.breaker;
                            meter = circuit.meter;
                            conductor = circuit.Conductor;
                        }
                        else
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_MTInFrontCircuit;
                            breaker = circuit.breaker;
                            meter = circuit.meter;
                            conductor = circuit.Conductor;
                        }

                        // 元器件1
                        var srcMeter = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CURRENT_TRANSFORMER).FirstOrDefault();
                        if (srcMeter == null)
                        {
                            // 无CT表
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            var CTtype = ComponentTypeSelector.GetComponentType(meter.ComponentType);
                            MTText.TextString = CTtype.GetProperty("Content").GetValue(meter).ToString();
                        }
                        else
                        {
                            // 有CT表
                            var CTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CT).First();
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            var CTtype = ComponentTypeSelector.GetComponentType(meter.ComponentType);
                            CTText.TextString = CTtype.GetProperty("ContentCT").GetValue(meter).ToString();
                            MTText.TextString = CTtype.GetProperty("ContentMT").GetValue(meter).ToString();
                        }

                        // 元器件2
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstPosition = srcBreaker.Position;
                        var firstComponentName = ThPDSComponentMap.ComponentMap[breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = breaker.Content();

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!conductor.IsNull())
                        {
                            conductorText.TextString = conductor.Content;
                        }
                        else
                        {
                            conductorText.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        Breaker breaker;
                        Meter meter;
                        Conductor conductor;
                        if (edge.Details.CircuitForm.CircuitFormType == CircuitFormOutType.配电计量_CT表在后)
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_CTInBehindCircuit;
                            breaker = circuit.breaker;
                            meter = circuit.meter;
                            conductor = circuit.Conductor;
                        }
                        else
                        {
                            var circuit = edge.Details.CircuitForm as DistributionMetering_MTInBehindCircuit;
                            breaker = circuit.breaker;
                            meter = circuit.meter;
                            conductor = circuit.Conductor;
                        }

                        // 元器件1
                        var srcMeter = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CURRENT_TRANSFORMER).FirstOrDefault();
                        if (srcMeter == null)
                        {
                            // 无CT表
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            MTText.TextString = meter.Content();
                        }
                        else
                        {
                            // 有CT表
                            var CTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CT).First();
                            var MTText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_MT).First();
                            CTText.TextString = meter.Content();
                            if (meter is CurrentTransformer ct)
                            {
                                MTText.TextString = ct.Content();
                            }
                        }

                        // 元器件2
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstPosition = srcBreaker.Position;
                        var firstComponentName = ThPDSComponentMap.ComponentMap[breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = breaker.Content();

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!conductor.IsNull())
                        {
                            conductorText.TextString = conductor.Content;
                        }
                        else
                        {
                            conductorText.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.电动机_分立元件:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_DiscreteComponentsCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        // 元器件2
                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor.Name))
                        {
                            var secondPosition = srcContactor.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor.Erase();
                        }
                        var QACText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC).First();
                        QACText.TextString = circuit.contactor.Content();

                        // 元器件3
                        var srcThermalRelay = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).First();
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(srcThermalRelay.Name))
                        {
                            var thirdPosition = srcThermalRelay.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay.Erase();
                        }
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = circuit.thermalRelay.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }
                        break;
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_CPSCircuit;

                        // 元器件1
                        var srcCPS = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CPS).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.cps.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcCPS.Name))
                        {
                            var firstPosition = srcCPS.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS).First();
                        CBText.TextString = circuit.cps.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.电动机_分立元件星三角启动:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_DiscreteComponentsStarTriangleStartCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件2
                        var srcContactor1 = srcContactor[0];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor1.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor1.Name))
                        {
                            var secondPosition = srcContactor1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor1.Erase();
                        }
                        var QAC1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC1).First();
                        QAC1Text.TextString = circuit.contactor1.Content();

                        // 元器件3
                        var srcContactor2 = srcContactor[1];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(srcContactor2.Name))
                        {
                            var thirdPosition = srcContactor2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor2.Erase();
                        }
                        var QAC2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC2).First();
                        QAC2Text.TextString = circuit.contactor2.Content();

                        // 元器件4
                        var srcContactor3 = srcContactor[2];
                        var forthComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!forthComponentName.Equals(srcContactor3.Name))
                        {
                            var forthPosition = srcContactor3.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, forthComponentName, forthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor3.Erase();
                        }
                        var QAC3Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC3).First();
                        QAC3Text.TextString = circuit.contactor3.Content();

                        // 元器件5
                        var srcThermalRelay = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).First();
                        var fifthComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay.ComponentType.GetDescription()];
                        if (!fifthComponentName.Equals(srcThermalRelay.Name))
                        {
                            var fifthPosition = srcThermalRelay.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, fifthComponentName, fifthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay.Erase();
                        }
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = circuit.thermalRelay.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.Conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.Conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        conductor2.TextString = circuit.Conductor2.Content;
                        if (!circuit.Conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.Conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.电动机_CPS星三角启动:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_CPSStarTriangleStartCircuit;

                        // 元器件1
                        var srcCPS = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CPS).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.cps.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcCPS.Name))
                        {
                            var firstPosition = srcCPS.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS.Erase();
                        }
                        var CPSText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS).First();
                        CPSText.TextString = circuit.cps.Content();

                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件2
                        var srcContactor1 = srcContactor[0];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor1.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor1.Name))
                        {
                            var secondPosition = srcContactor1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor1.Erase();
                        }
                        var QAC1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC1).First();
                        QAC1Text.TextString = circuit.contactor1.Content();

                        // 元器件3
                        var srcContactor2 = srcContactor[1];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(srcContactor2.Name))
                        {
                            var thirdPosition = srcContactor2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor2.Erase();
                        }
                        var QAC2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC2).First();
                        QAC2Text.TextString = circuit.contactor2.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.Conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.Conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        if (!circuit.Conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.Conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.双速电动机_分立元件detailYY:
                    {
                        var circuit = edge.Details.CircuitForm as TwoSpeedMotor_DiscreteComponentsDYYCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件2
                        var srcContactor1 = srcContactor[0];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor1.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor1.Name))
                        {
                            var secondPosition = srcContactor1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor1.Erase();
                        }
                        var QAC1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC1).First();
                        QAC1Text.TextString = circuit.contactor1.Content();

                        // 元器件3
                        var srcContactor2 = srcContactor[1];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(srcContactor2.Name))
                        {
                            var thirdPosition = srcContactor2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor2.Erase();
                        }
                        var QAC2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC2).First();
                        QAC2Text.TextString = circuit.contactor2.Content();

                        // 元器件4
                        var srcContactor3 = srcContactor[2];
                        var forthComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!forthComponentName.Equals(srcContactor3.Name))
                        {
                            var forthPosition = srcContactor3.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, forthComponentName, forthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor3.Erase();
                        }
                        var QAC3Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC3).First();
                        QAC3Text.TextString = circuit.contactor3.Content();

                        var thermalRelays = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).ToList();
                        // 元器件5
                        var srcThermalRelay1 = thermalRelays[0];
                        var fifthComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay1.ComponentType.GetDescription()];
                        if (!fifthComponentName.Equals(srcThermalRelay1.Name))
                        {
                            var fifthPosition = srcThermalRelay1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, fifthComponentName, fifthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay1.Erase();
                        }
                        var KH1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH1).First();
                        KH1Text.TextString = circuit.thermalRelay1.Content();

                        // 元器件6
                        var srcThermalRelay2 = thermalRelays[1];
                        var sixthComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay2.ComponentType.GetDescription()];
                        if (!sixthComponentName.Equals(srcThermalRelay2.Name))
                        {
                            var sixthPosition = srcThermalRelay2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, sixthComponentName, sixthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay2.Erase();
                        }
                        var KH2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH2).First();
                        KH2Text.TextString = circuit.thermalRelay2.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        if (!circuit.conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.双速电动机_分立元件YY:
                    {
                        var circuit = edge.Details.CircuitForm as TwoSpeedMotor_DiscreteComponentsYYCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content();

                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件2
                        var srcContactor1 = srcContactor[0];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor1.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor1.Name))
                        {
                            var secondPosition = srcContactor1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor1.Erase();
                        }
                        var QAC1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC1).First();
                        QAC1Text.TextString = circuit.contactor1.Content();

                        // 元器件3
                        var srcContactor2 = srcContactor[1];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor2.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(srcContactor2.Name))
                        {
                            var thirdPosition = srcContactor2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcContactor2.Erase();
                        }
                        var QAC2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC2).First();
                        QAC2Text.TextString = circuit.contactor2.Content();

                        var thermalRelays = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).ToList();
                        // 元器件4
                        var srcThermalRelay1 = thermalRelays[0];
                        var forthComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay1.ComponentType.GetDescription()];
                        if (!forthComponentName.Equals(srcThermalRelay1.Name))
                        {
                            var forthPosition = srcThermalRelay1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, forthComponentName, forthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay1.Erase();
                        }
                        var KH1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH1).First();
                        KH1Text.TextString = circuit.thermalRelay1.Content();

                        // 元器件5
                        var srcThermalRelay2 = thermalRelays[1];
                        var fifthComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay2.ComponentType.GetDescription()];
                        if (!fifthComponentName.Equals(srcThermalRelay2.Name))
                        {
                            var fifthPosition = srcThermalRelay2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, fifthComponentName, fifthPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcThermalRelay2.Erase();
                        }
                        var KH2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH2).First();
                        KH2Text.TextString = circuit.thermalRelay2.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        if (!circuit.conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.双速电动机_CPSdetailYY:
                    {
                        var circuit = edge.Details.CircuitForm as TwoSpeedMotor_CPSDYYCircuit;

                        var CPS = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CPS)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件1
                        var srcCPS1 = CPS[0];
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.cps1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcCPS1.Name))
                        {
                            var firstPosition = srcCPS1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS1.Erase();
                        }
                        var CPS1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS1).First();
                        CPS1Text.TextString = circuit.cps1.Content();

                        // 元器件2
                        var srcCPS2 = CPS[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.cps2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcCPS2.Name))
                        {
                            var secondPosition = srcCPS2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS2.Erase();
                        }
                        var CPS2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS2).First();
                        CPS2Text.TextString = circuit.cps2.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        if (!circuit.conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.双速电动机_CPSYY:
                    {
                        var circuit = edge.Details.CircuitForm as TwoSpeedMotor_CPSYYCircuit;

                        var CPS = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CPS)
                            .OrderByDescending(c => c.Position.Y).ToList();
                        // 元器件1
                        var srcCPS1 = CPS[0];
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.cps1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcCPS1.Name))
                        {
                            var firstPosition = srcCPS1.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS1.Erase();
                        }
                        var CPS1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS1).First();
                        CPS1Text.TextString = circuit.cps1.Content();

                        // 元器件2
                        var srcCPS2 = CPS[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.cps2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcCPS2.Name))
                        {
                            var secondPosition = srcCPS2.Position;
                            var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            tableObjs.Add(newComponent);
                            srcCPS2.Erase();
                        }
                        var CPS2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CPS2).First();
                        CPS2Text.TextString = circuit.cps2.Content();

                        //// 元器件3
                        //var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        //var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()];
                        //if (!thirdComponentName.Equals(srcContactor.Name))
                        //{
                        //    var thirdPosition = srcContactor.Position;
                        //    var newComponent = insertEngine.Insert1(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                        //    tableObjs.Add(newComponent);
                        //    srcContactor.Erase();
                        //}
                        //var QACText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC).First();
                        //QACText.TextString = circuit.contactor.Content();

                        // Conductor1
                        var conductor1 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR1).First();
                        if (!circuit.conductor1.IsNull())
                        {
                            conductor1.TextString = circuit.conductor1.Content;
                        }
                        else
                        {
                            conductor1.TextString = "";
                        }

                        // Conductor2
                        var conductor2 = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR2).First();
                        if (!circuit.conductor2.IsNull())
                        {
                            conductor2.TextString = circuit.conductor2.Content;
                        }
                        else
                        {
                            conductor2.TextString = "";
                        }

                        break;
                    }
                case CircuitFormOutType.消防应急照明回路WFEL:
                    {
                        var circuit = edge.Details.CircuitForm as FireEmergencyLighting;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        if (!circuit.Conductor.IsNull())
                        {
                            conductor.TextString = circuit.Conductor.Content;
                        }
                        else
                        {
                            conductor.TextString = "";
                        }

                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        public Polyline SmallBusbarAssign(AcadDatabase activeDb, AcadDatabase configDb, BlockReference block,
            List<Entity> tableObjs, MiniBusbar smallBusbar, Scale3d scale)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, block);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var components = objs.OfType<BlockReference>().ToList();
            var insertEngine = new ThPDSBlockInsertEngine();

            // 元器件1
            var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER).First();
            var firstPosition = srcBreaker.Position;
            var secondPosition = new Point3d(firstPosition.X + 2750, firstPosition.Y, 0);

            var firstComponentName = ThPDSComponentMap.ComponentMap[smallBusbar.Breaker.ComponentType.GetDescription()];
            if (!firstComponentName.Equals(srcBreaker.Name))
            {
                var newComponent = insertEngine.Insert1(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                tableObjs.Add(newComponent);
                srcBreaker.Erase();
            }

            // 元器件2
            if (!smallBusbar.ReservedComponent.IsNull())
            {
                var secondComponentName = ThPDSComponentMap.ComponentMap[smallBusbar.ReservedComponent.ComponentType.GetDescription()];
                var newComponent = insertEngine.Insert1(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                tableObjs.Add(newComponent);
            }

            return objs.OfType<Polyline>().First();
        }

        public void ControlCircuitAssign(AcadDatabase activeDb, BlockReference block, List<Entity> tableObjs, SecondaryCircuit secondaryCircuit)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, block);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));

            var texts = objs.OfType<DBText>().ToList();
            // Conductor
            var conductor = texts.Where(t => t.TextString.Equals(ThPDSCommon.OUT_CIRCUIT_CONDUCTOR)).First();
            conductor.TextString = secondaryCircuit.Conductor.Content;

            // 回路编号
            var circuitId = texts.Where(t => t.TextString.Equals(ThPDSCommon.OUT_CIRCUIT_CIRCUIT_NUMBER)).First();
            circuitId.TextString = secondaryCircuit.CircuitID;

            // 控制回路
            var description = texts.Where(t => t.TextString.Equals(ThPDSCommon.CONTROL_CIRCUIT_DESCRIPTION)).First();
            description.TextString = secondaryCircuit.CircuitDescription;
        }

        public void SurgeProtectionAssign(AcadDatabase activeDb, BlockReference block, List<Entity> tableObjs, string content)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, block);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));

            objs.OfType<DBText>().First().TextString = content;
        }

        /// <summary>
        /// 空二次结线
        /// </summary>
        /// <param name="activeDb"></param>
        /// <param name="block"></param>
        /// <param name="tableObjs"></param>
        public void SecJunctionAssign(AcadDatabase activeDb, BlockReference block, List<Entity> tableObjs)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, block);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var texts = objs.OfType<DBText>().ToList();

            var text1 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT1).First();
            text1.TextString = "";

            var text2 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT2).First();
            text2.TextString = "";

            var text3 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT3).First();
            text3.TextString = "";

            var text4 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT4).First();
            text4.TextString = "";

            var text5 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT5).First();
            text5.TextString = "";

            var text6 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT6).First();
            text6.TextString = "";
        }

        public void SecJunctionAssign(AcadDatabase activeDb, BlockReference block, List<Entity> tableObjs,
            ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> motors)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, block);
            objs.OfType<Entity>().ForEach(o => tableObjs.Add(o));
            var texts = objs.OfType<DBText>().ToList();

            var text1 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT1).First();
            text1.TextString = Text1Assign(node);

            var text2 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT2).First();
            text2.TextString = Text2Assign(node, motors);

            var motorInfo = ThPDSMotorInfoService.Select(motors[0].Target.Load.LoadTypeCat_3, motors[0].Target.Load.FireLoad);
            if (motorInfo.IsNull())
            {
                motorInfo = new ThPDSMotorInfo();
            }
            var text3 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT3).First();
            text3.TextString = Text3Assign(motors) + "，" + motorInfo.OperationMode;

            var text4 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT4).First();
            text4.TextString = motorInfo.Signal;

            var text5 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT5).First();
            text5.TextString = motorInfo.FaultProtection;

            var text6 = texts.Where(t => t.TextString == ThPDSCommon.SECONDARY_JUNCTION_TEXT6).First();
            text6.TextString = Text6Assign(motors);
        }

        private string Text1Assign(ThPDSProjectGraphNode node)
        {
            var enterType = node.Details.CircuitFormType.CircuitFormType.GetDescription();
            if (enterType.Equals(CircuitFormInType.一路进线.GetDescription()))
            {
                if (node.Load.FireLoad)
                {
                    return "一路电源（上级二路电源，自切自复）";
                }
                else
                {
                    return "一路电源";
                }
            }
            else if (enterType.Equals(CircuitFormInType.二路进线ATSE.GetDescription()))
            {
                return "二路电源，自切自复";
            }
            else if (enterType.Equals(CircuitFormInType.三路进线.GetDescription()))
            {
                return "二路平时电源，自切自复，战时电源手动切换";
            }
            return "";
        }

        private string Text2Assign(ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> motors)
        {
            var result = motors.Count.ToString() + "台";
            if (motors[0].Target.Load.InstalledCapacity.IsDualPower)
            {
                result += "，双速";
            }
            result += "，" + motors[0].Target.Load.PrimaryAvail.ToString() + "用";
            if (motors[0].Target.Load.SpareAvail > 0)
            {
                result += motors[0].Target.Load.SpareAvail.ToString() + "备";
            }
            return result;
        }

        private string Text3Assign(List<ThPDSProjectGraphEdge> motors)
        {
            var typeOne = false;
            var typeTwo = false;
            var circuitType = motors.Select(m => m.Details.CircuitForm.CircuitFormType).Distinct().ToList();
            circuitType.ForEach(type =>
            {
                switch (type)
                {
                    case CircuitFormOutType.电动机_分立元件:
                    case CircuitFormOutType.电动机_CPS:
                    case CircuitFormOutType.双速电动机_分立元件detailYY:
                    case CircuitFormOutType.双速电动机_分立元件YY:
                    case CircuitFormOutType.双速电动机_CPSdetailYY:
                    case CircuitFormOutType.双速电动机_CPSYY:
                        typeOne = true;
                        break;
                    case CircuitFormOutType.电动机_分立元件星三角启动:
                    case CircuitFormOutType.电动机_CPS星三角启动:
                        typeTwo = true;
                        break;
                }
            });

            var result = "";
            if (typeOne)
            {
                result += "直接启动";
                if (typeTwo)
                {
                    result += "/星三角启动";
                }
            }
            else if (typeTwo)
            {
                result += "星三角启动";
            }
            return result;
        }

        private string Text6Assign(List<ThPDSProjectGraphEdge> motors)
        {
            var result = new List<string>();
            motors.ForEach(m =>
            {
                result.Add(Text6Assign(m));
            });
            result = result.Distinct().ToList();
            var text = "";
            for (var i = 0; i < result.Count; i++)
            {
                text += result[i] + "；";
            }
            return text.Substring(0, text.Count() - 1);
        }

        private string Text6Assign(ThPDSProjectGraphEdge edge)
        {
            switch (edge.Target.Load.LoadTypeCat_3)
            {
                case ThPDSLoadTypeCat_3.SmokeExhaustFan:
                case ThPDSLoadTypeCat_3.MakeupAirFan:
                case ThPDSLoadTypeCat_3.StaircasePressurizationFan:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第13~14页";
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第17~18页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件detailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第29~30页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件YY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第37~38页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSdetailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第33~34页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第41~42页";
                            }
                            break;
                    }
                    break;
                case ThPDSLoadTypeCat_3.ExhaustFan_Smoke:
                case ThPDSLoadTypeCat_3.SupplyFan_Smoke:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第21~22页";
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第25~26页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件detailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第29~30页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件YY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第37~38页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSdetailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第33~34页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第41~42页";
                            }
                            break;
                    }
                    break;
                case ThPDSLoadTypeCat_3.ExhaustFan:
                case ThPDSLoadTypeCat_3.SupplyFan:
                case ThPDSLoadTypeCat_3.KitchenExhaustFan:
                case ThPDSLoadTypeCat_3.PostEmergencyFan:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                var circuit = edge.Details.CircuitForm as Motor_DiscreteComponentsCircuit;
                                if (circuit.Conductor.IsBAControl)
                                {
                                    return "《常用风机控制电路图》16D303-2第77~78页";
                                }
                                else
                                {
                                    return "《常用风机控制电路图》16D303-2第85~86页";
                                }
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                var circuit = edge.Details.CircuitForm as Motor_CPSCircuit;
                                if (circuit.Conductor.IsBAControl)
                                {
                                    return "《常用风机控制电路图》16D303-2第79~80页";
                                }
                                else
                                {
                                    return "《常用风机控制电路图》16D303-2第87~88页";
                                }
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件detailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第45~46页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_分立元件YY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第53~54页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSdetailYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第49~50页";
                            }
                            break;
                        case CircuitFormOutType.双速电动机_CPSYY:
                            if (edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                return "《常用风机控制电路图》16D303-2第57~58页";
                            }
                            break;
                    }
                    break;
                case ThPDSLoadTypeCat_3.EmergencyFan:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                var circuit = edge.Details.CircuitForm as Motor_DiscreteComponentsCircuit;
                                if (circuit.Conductor.IsBAControl)
                                {
                                    return "《常用风机控制电路图》16D303-2第89~90页";
                                }
                                else
                                {
                                    return "《常用风机控制电路图》16D303-2第91~92页";
                                }
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (!edge.Target.Load.InstalledCapacity.IsDualPower)
                            {
                                var circuit = edge.Details.CircuitForm as Motor_CPSCircuit;
                                if (circuit.Conductor.IsBAControl)
                                {
                                    return "《常用风机控制电路图》16D303-2第93~94页";
                                }
                                else
                                {
                                    return "《常用风机控制电路图》16D303-2第95~96页";
                                }
                            }
                            break;
                    }
                    break;
                case ThPDSLoadTypeCat_3.DomesticWaterPump:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第136~138页";
                            }
                            else if (edge.Target.Load.PrimaryAvail == 2 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第177~180页";
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第139~140页";
                            }
                            else if (edge.Target.Load.PrimaryAvail == 2 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第181~184页";
                            }
                            break;
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第157~160页";
                            }
                            else if (edge.Target.Load.PrimaryAvail == 2 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第177~180页";
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第161~163页";
                            }
                            else if (edge.Target.Load.PrimaryAvail == 2 && edge.Target.Load.SpareAvail == 1)
                            {
                                return "《常用水泵控制电路图》16D303-3第181~184页";
                            }
                            break;
                    }
                    break;
                case ThPDSLoadTypeCat_3.SubmersiblePump:
                    switch (edge.Details.CircuitForm.CircuitFormType)
                    {
                        case CircuitFormOutType.电动机_分立元件:
                        case CircuitFormOutType.电动机_分立元件星三角启动:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 0)
                            {
                                return "《常用水泵控制电路图》16D303-3第230~231页";
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 2)
                            {
                                if (!edge.Target.Load.FireLoad)
                                {
                                    return "《常用水泵控制电路图》16D303-3第240~242页";
                                }
                                else
                                {
                                    return "《常用水泵控制电路图》16D303-3第252~254页";
                                }
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 3)
                            {
                                return "《常用水泵控制电路图》16D303-3第270~273页";
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 4)
                            {
                                return "《常用水泵控制电路图》16D303-3第282~285页";
                            }
                            break;
                        case CircuitFormOutType.电动机_CPS:
                        case CircuitFormOutType.电动机_CPS星三角启动:
                            if (edge.Target.Load.PrimaryAvail == 1 && edge.Target.Load.SpareAvail == 0)
                            {
                                return "《常用水泵控制电路图》16D303-3第232~233页";
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 2)
                            {
                                if (!edge.Target.Load.FireLoad)
                                {
                                    return "《常用水泵控制电路图》16D303-3第243~245页";
                                }
                                else
                                {
                                    return "《常用水泵控制电路图》16D303-3第255~257页";
                                }
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 3)
                            {
                                return "《常用水泵控制电路图》16D303-3第274~277页";
                            }
                            else if (edge.Target.Load.PrimaryAvail + edge.Target.Load.SpareAvail == 4)
                            {
                                return "《常用水泵控制电路图》16D303-3第282~285页";
                            }
                            break;
                    }
                    break;
            }
            return "";
        }

        private CircuitFormInType FetchDescription(string str)
        {
            foreach (CircuitFormInType item in Enum.GetValues(typeof(CircuitFormInType)))
            {
                if (str.Equals(item.GetDescription()))
                {
                    return item;
                }
            }
            return CircuitFormInType.None;
        }
    }
}
