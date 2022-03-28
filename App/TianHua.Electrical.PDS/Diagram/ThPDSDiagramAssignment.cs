using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuikGraph;
using System;
using System.Linq;
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
            var circuitNumbers = graph.Edges
                .Where(e => e.Target.Equals(node))
                .Select(e => e.Circuit.ID.CircuitNumber.Last())
                .OrderBy(str => str)
                .ToList();
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
                        var firstPosition = srcIsolatingSwitch.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        srcIsolatingSwitch.Erase();
                        var QLText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_QL).First();
                        QLText.TextString = circuit.isolatingSwitch.Content;
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
                        var firstPosition = firstIsolatingSwitch.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch1.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        firstIsolatingSwitch.Erase();
                        var firstQLText = QLTexts[0];
                        firstQLText.TextString = circuit.isolatingSwitch1.Content;

                        // 隔离开关2
                        var secondIsolatingSwitch = srcIsolatingSwitchs[1];
                        var secondPosition = secondIsolatingSwitch.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.isolatingSwitch2.ComponentType.GetDescription()], secondPosition, 100 * scale);
                        secondIsolatingSwitch.Erase();
                        var secondQLText = QLTexts[1];
                        secondQLText.TextString = circuit.isolatingSwitch2.Content;

                        // 转换开关
                        var srcTransferSwitch = components.Where(c => c.Name == ThPDSCommon.DEFAULT_TRANSFER_SWITCH).First();
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.transferSwitch.ComponentType.GetDescription()], srcTransferSwitch.Position, 100 * scale);
                        srcTransferSwitch.Erase();
                        var ATSEText = texts.Where(t => t.TextString == ThPDSCommon.ENTER_CIRCUIT_ATSE_320A_4P).First();
                        var type = ComponentTypeSelector.GetComponentType(circuit.transferSwitch.ComponentType);
                        ATSEText.TextString = type.GetProperty("Content").GetValue(circuit.transferSwitch).ToString();

                        break;
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

                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        srcBreaker.Erase();
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

                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        srcBreaker.Erase();
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
                        var firstPosition = srcBreaker.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        srcBreaker.Erase();
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        var srcContactor = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CONTACTOR).First();
                        var secondPosition = srcContactor.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.contactor.ComponentType.GetDescription()], secondPosition, 100 * scale);
                        srcContactor.Erase();
                        var QACText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_QAC).First();
                        QACText.TextString = GetContactorContent(circuit.contactor);

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
                        var firstPosition = srcBreaker.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.breaker.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        srcBreaker.Erase();
                        var CBText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB).First();
                        CBText.TextString = circuit.breaker.Content;

                        // 元器件2
                        var srcThermalRelay = components.Where(c => c.Name == ThPDSCommon.DEFAULT_THERMAL_RELAY).First();
                        var secondPosition = srcThermalRelay.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.thermalRelay.ComponentType.GetDescription()], secondPosition, 100 * scale);
                        srcThermalRelay.Erase();
                        var KHText = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_KH).First();
                        KHText.TextString = GetThermalRelayContent(circuit.thermalRelay);

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_ShanghaiCTCircuit;

                        var srcBreakers = components.Where(c => c.Name == ThPDSCommon.DEFAULT_CIRCUIT_BREAKER)
                            .OrderBy(c => c.Position.X).ToList();
                        // 元器件1
                        var firstBreaker = srcBreakers[0];
                        var firstPosition = firstBreaker.Position;
                        insertEngine.Insert(activeDb, configDb,
                            ThPDSComponentMap.ComponentMap[circuit.breaker1.ComponentType.GetDescription()], firstPosition, 100 * scale);
                        firstBreaker.Erase();
                        var CB1Text = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CB1).First();
                        CB1Text.TextString = circuit.breaker1.Content;

                        // 元器件2

                        // 元器件3

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_ShanghaiMTCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_CTInFrontCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_MTInFrontCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_CTInBehindCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        var circuit = edge.Details.CircuitForm as DistributionMetering_MTInBehindCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.电动机_分立元件:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_DiscreteComponentsCircuit;

                        // Conductor
                        var conductor = texts.Where(t => t.TextString == ThPDSCommon.OUT_CIRCUIT_CONDUCTOR).First();
                        conductor.TextString = circuit.Conductor.Content;
                        break;
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        var circuit = edge.Details.CircuitForm as Motor_CPSCircuit;

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

        private string GetThermalRelayContent(ThermalRelay thermalRelay)
        {
            return $"{thermalRelay.ThermalRelayType.GetEnumDescription()} {thermalRelay.RatedCurrent}A";
        }

        private string GetContactorContent(Contactor contactor)
        {
            return $"{contactor.ContactorType} {contactor.RatedCurrent}/{contactor.PolesNum}";
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
