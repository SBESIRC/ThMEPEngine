﻿using System;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuikGraph;

using ThCADExtension;
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
        public void TableTitleAssign(AcadDatabase activeDb, BlockReference title, ThPDSProjectGraphNode node)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, title);
            var texts = objs.OfType<DBText>().ToList();

            // 配电箱编号
            var loadId = texts.Where(t => t.TextString == ThPDSCommon.DISTRIBUTION_BOX_ID).First();
            loadId.TextString = node.Load.ID.LoadID;

            // 设备用途
            var application = texts.Where(t => t.TextString == ThPDSCommon.APPLICATION).First();
            application.TextString = node.Load.ID.Description;

            // 消防负荷
            var fireLoad = texts.Where(t => t.TextString == ThPDSCommon.FIRE_LOAD).First();
            fireLoad.TextString = node.Load.FireLoad ? ThPDSCommon.FIRE_POWER_SUPPLY : ThPDSCommon.NON_FIRE_POWER_SUPPLY;

            // 参考尺寸
            var overallDimensions = texts.Where(t => t.TextString == ThPDSCommon.OVERALL_DIMENSIONS).First();
            overallDimensions.TextString = "";

            // 安装位置
            var location = texts.Where(t => t.TextString == ThPDSCommon.LOCATION).First();
            location.TextString = "";

            // 安装方式
            var installMethod = texts.Where(t => t.TextString == ThPDSCommon.INSTALLMETHOD).First();
            installMethod.TextString = "";
        }

        public Polyline EnterCircuitAssign(AcadDatabase activeDb, AcadDatabase configDb, BlockReference circuitBlock,
            AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph,
            ThPDSProjectGraphNode node, Scale3d scale)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuitBlock);
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
                        var srcIsolatingSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH).First();
                        var componentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch.ComponentType.GetDescription()];
                        if (!componentName.Equals(srcIsolatingSwitch.BlockName))
                        {
                            var firstPosition = srcIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, componentName, firstPosition, 100 * scale);
                            srcIsolatingSwitch.Erase();
                        }
                        var QLText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL).First();
                        QLText.TextString = circuit.isolatingSwitch.Content();

                        break;
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
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(firstIsolatingSwitch.BlockName))
                        {
                            var firstPosition = firstIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            firstIsolatingSwitch.Erase();
                        }
                        var firstQLText = QLTexts[0];
                        firstQLText.TextString = circuit.isolatingSwitch1.Content();

                        // 隔离开关2
                        var secondIsolatingSwitch = srcIsolatingSwitchs[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondIsolatingSwitch.BlockName))
                        {
                            var secondPosition = secondIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            secondIsolatingSwitch.Erase();
                        }
                        var secondQLText = QLTexts[1];
                        secondQLText.TextString = circuit.isolatingSwitch2.Content();

                        // 转换开关
                        var srcTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_TRANSFER_SWITCH).First();
                        var transferSwitchName = ThPDSComponentMap.ComponentMap[circuit.transferSwitch.ComponentType.GetDescription()];
                        if (!transferSwitchName.Equals(srcTransferSwitch.BlockName))
                        {
                            insertEngine.Insert(activeDb, configDb, transferSwitchName, srcTransferSwitch.Position, 100 * scale);
                            srcTransferSwitch.Erase();
                        }
                        var ATSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_ATSE_320A_4P).First();
                        var type = ComponentTypeSelector.GetComponentType(circuit.transferSwitch.ComponentType);
                        //ATSEText.TextString = type.GetProperty("Content").GetValue(circuit.transferSwitch).ToString();

                        break;
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
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch1.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(firstIsolatingSwitch.BlockName))
                        {
                            var firstPosition = firstIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            firstIsolatingSwitch.Erase();
                        }
                        var firstQLText = QLTexts[0];
                        firstQLText.TextString = circuit.isolatingSwitch1.Content();

                        // 隔离开关2
                        var secondIsolatingSwitch = srcIsolatingSwitchs[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondIsolatingSwitch.BlockName))
                        {
                            var secondPosition = secondIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            secondIsolatingSwitch.Erase();
                        }
                        var secondQLText = QLTexts[1];
                        secondQLText.TextString = circuit.isolatingSwitch2.Content();

                        // 隔离开关3
                        var thirdIsolatingSwitch = srcIsolatingSwitchs[2];
                        var thirdComponentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch3.ComponentType.GetDescription()];
                        if (!thirdComponentName.Equals(thirdIsolatingSwitch.BlockName))
                        {
                            var thirdPosition = thirdIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            thirdIsolatingSwitch.Erase();
                        }
                        var thirdQLText = QLTexts[2];
                        thirdQLText.TextString = circuit.isolatingSwitch3.Content();

                        // 转换开关1
                        var srcTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_TRANSFER_SWITCH).First();
                        var transferSwitchName = ThPDSComponentMap.ComponentMap[circuit.transferSwitch1.ComponentType.GetDescription()];
                        if (!transferSwitchName.Equals(srcTransferSwitch.BlockName))
                        {
                            insertEngine.Insert(activeDb, configDb, transferSwitchName, srcTransferSwitch.Position, 100 * scale);
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
                            insertEngine.Insert(activeDb, configDb, manualTransferSwitchName, srcManualTransferSwitch.Position, 100 * scale);
                            srcManualTransferSwitch.Erase();
                        }
                        var MTSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_MTSE_320A_4P).First();
                        var MTSEtype = ComponentTypeSelector.GetComponentType(circuit.transferSwitch2.ComponentType);
                        //ATSEText.TextString = type.GetProperty("Content").GetValue(circuit.transferSwitch).ToString();

                        break;
                    }
                case CircuitFormInType.集中电源:
                    {
                        var circuit = node.Details.CircuitFormType as CentralizedPowerCircuit;

                        // 隔离开关
                        var srcIsolatingSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_ISOLATING_SWITCH_1).First();
                        var componentName = ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch.ComponentType.GetDescription()];
                        if (!componentName.Equals(srcIsolatingSwitch.BlockName))
                        {
                            var firstPosition = srcIsolatingSwitch.Position;
                            insertEngine.Insert(activeDb, configDb, componentName, firstPosition, 100 * scale);
                            srcIsolatingSwitch.Erase();
                        }
                        var QLText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL_25_1P).First();
                        QLText.TextString = circuit.isolatingSwitch.Content();

                        break;
                    }
                case CircuitFormInType.None:
                    {
                        throw new NotImplementedException();
                    }
            }

            return objs.OfType<Polyline>().First();
        }

        public void TableTailAssign(AcadDatabase activeDb, BlockReference tail, ThPDSProjectGraphNode node)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, tail);
            var table = objs.OfType<Table>().First();

            // Pn
            CellAssign(table.Cells[0, 1], node.Details.LowPower);
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
             ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge, Scale3d scale)
        {
            var objs = ThPDSExplodeService.BlockExplode(activeDb, circuitBlock);
            var texts = objs.OfType<DBText>().ToList();
            var components = objs.OfType<BlockReference>().ToList();

            // 回路编号
            var circuitNumber = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CIRCUIT_NUMBER).First();
            circuitNumber.TextString = edge.Circuit.ID.CircuitID.Last();

            // 相序
            var phaseSequence = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_PHSAE).First();
            phaseSequence.TextString = edge.Target.Details.PhaseSequence.GetDescription();

            // 功率
            var power = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_POWER).First();
            power.TextString = edge.Target.Details.LowPower == 0 ? "" : edge.Target.Details.LowPower.ToString();

            // 负载编号
            var loadID = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_LOAD_ID).First();
            loadID.TextString = edge.Target.Load.ID.LoadID;

            // 功能用途
            var description = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_DESCRIPTION).First();
            description.TextString = edge.Target.Load.ID.Description;

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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        if (circuit.reservedComponent1 != null)
                        {
                            insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.reservedComponent1.ComponentType.GetDescription()], secondPosition, 100 * scale);
                        }

                        // 元器件3
                        if (circuit.reservedComponent2 != null)
                        {
                            insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.reservedComponent2.ComponentType.GetDescription()], thirdPosition, 100 * scale);
                        }

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var RCDText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_RCD).First();
                        RCDText.TextString = circuit.breaker.Content;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }

                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor.Name))
                        {
                            var secondPosition = srcContactor.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            srcContactor.Erase();
                        }
                        var QACText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC).First();
                        QACText.TextString = circuit.contactor.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }

                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        var srcThermalRelay = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.thermalRelay.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcThermalRelay.Name))
                        {
                            var secondPosition = srcThermalRelay.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            srcThermalRelay.Erase();
                        }
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = circuit.thermalRelay.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_上海CT | CircuitFormOutType.配电计量_上海直接表:
                    {
                        BreakerBaseComponent breaker1, breaker2;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            firstBreaker.Erase();
                        }
                        var CB1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB1).First();
                        CB1Text.TextString = breaker1.Content;

                        // 元器件2
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

                        // 元器件3
                        var secondBreaker = srcBreakers[1];
                        var secondComponentName = ThPDSComponentMap.ComponentMap[breaker2.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(secondBreaker.Name))
                        {
                            var secondPosition = secondBreaker.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
                            secondBreaker.Erase();
                        }
                        var CB2Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB2).First();
                        CB2Text.TextString = breaker2.Content;

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductorText.TextString = conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在前 | CircuitFormOutType.配电计量_直接表在前:
                    {
                        BreakerBaseComponent breaker;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = breaker.Content;

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductorText.TextString = conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在后 | CircuitFormOutType.配电计量_直接表在后:
                    {
                        BreakerBaseComponent breaker;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = breaker.Content;

                        // Conductor
                        var conductorText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductorText.TextString = conductor.Content;
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
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        var secondComponentName = ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()];
                        if (!secondComponentName.Equals(srcContactor.Name))
                        {
                            var secondPosition = srcContactor.Position;
                            insertEngine.Insert(activeDb, configDb, secondComponentName, secondPosition, 100 * scale);
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
                            insertEngine.Insert(activeDb, configDb, thirdComponentName, thirdPosition, 100 * scale);
                            srcThermalRelay.Erase();
                        }
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = circuit.thermalRelay.Content();

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_CPSCircuit;

                        // 元器件1
                        var srcBreaker = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CPS).First();
                        var firstComponentName = ThPDSComponentMap.ComponentMap[circuit.cps.ComponentType.GetDescription()];
                        if (!firstComponentName.Equals(srcBreaker.Name))
                        {
                            var firstPosition = srcBreaker.Position;
                            insertEngine.Insert(activeDb, configDb, firstComponentName, firstPosition, 100 * scale);
                            srcBreaker.Erase();
                        }
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        //CBText.TextString = circuit.cps.Content;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
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
