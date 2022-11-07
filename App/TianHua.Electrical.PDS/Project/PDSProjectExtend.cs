using Dreambuild.AutoCAD;
using QuikGraph;
using QuikGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project
{
    public static class PDSProjectExtend
    {
        private static ProjectGraph _projectGraph { get { return PDSProject.Instance.graphData.Graph; } }

        /// <summary>
        /// 创建PDSProjectGraph
        /// </summary>
        /// <param name="Graph"></param>
        public static ThPDSProjectGraph CreatPDSProjectGraph(this ProjectGraph graph)
        {
            var ProjectGraph = new ThPDSProjectGraph(graph);
            //ProjectGraph.CalculateSecondaryCircuit();
            return ProjectGraph;
        }

        /// <summary>
        /// 计算项目选型
        /// </summary>
        public static void CalculateProjectInfo()
        {
            var projectGraph = _projectGraph;
            var RootNodes = projectGraph.Vertices.Where(x => _projectGraph.InDegree(x) == 0);
            foreach (var rootNode in RootNodes)
            {
                rootNode.CalculateProjectInfo();
            }
        }

        /// <summary>
        /// 计算项目选型
        /// </summary>
        public static void CalculateProjectInfo(this ThPDSProjectGraphNode node, bool SuperiorNodeIsVirtualLoad = false)
        {
            if (node.Details.IsStatistical)
            {
                return;
            }
            node.CalculateCircuitFormInType();
            var edges = _projectGraph.OutEdges(node).ToList();
            if (edges.Count == 1)
            {
                edges[0].Target.Details.LoadCalculationInfo.HighDemandFactor = 1.0;
            }
            edges.ForEach(e =>
            {
                e.Target.CalculateProjectInfo(node.Type == PDSNodeType.VirtualLoad);
                e.ComponentSelection();
            });
            edges.BalancedPhaseSequence();
            node.Details.LoadCalculationInfo.HighPower = Math.Max(node.Details.LoadCalculationInfo.HighPower, node.CalculateHighPower());
            if (node.Details.LoadCalculationInfo.IsDualPower)
            {
                node.Details.LoadCalculationInfo.LowPower = Math.Max(node.Details.LoadCalculationInfo.LowPower, node.CalculateLowPower());
            }
            node.ComponentSelection(edges, SuperiorNodeIsVirtualLoad);
            edges.CalculateSecondaryCircuit();
            node.Details.IsStatistical = true;
            return;
        }

        /// <summary>
        /// 计算进线回路类型
        /// </summary>
        public static void CalculateCircuitFormInType(this ThPDSProjectGraphNode node)
        {
            if (node.Type == PDSNodeType.VirtualLoad)
            {
                node.Details.CircuitFormType = null;
            }
            else if (node.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.DistributionPanel && node.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)
            {
                node.Details.CircuitFormType = new CentralizedPowerCircuit();
            }
            else
            {
                var count = _projectGraph.InDegree(node);
                if (count <= 1)
                {
                    node.Details.CircuitFormType = new OneWayInCircuit();
                }
                else if (count == 2)
                {
                    node.Details.CircuitFormType = new TwoWayInCircuit();
                }
                else if (count >= 3)
                {
                    node.Details.CircuitFormType = new ThreeWayInCircuit();
                }
            }
        }

        /// <summary>
        /// 全局配置更新
        /// </summary>
        public static void GlobalConfigurationUpdate()
        {
            foreach (var node in _projectGraph.TopologicalSort().Reverse())
            {
                node.UpdateLoadGlobalConfiguration();
                var edges = _projectGraph.InEdges(node);
                edges.ForEach(e => e.UpdateCircuitGlobalConfiguration());
            }
        }

        /// <summary>
        /// 更新负载全局配置
        /// </summary>
        public static void UpdateLoadGlobalConfiguration(this ThPDSProjectGraphNode node)
        {
            var edges = _projectGraph.OutEdges(node).ToList();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);

            node.ComponentCheck(CascadeCurrent);

            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
        }

        /// <summary>
        /// 更新回路全局配置
        /// </summary>
        public static void UpdateCircuitGlobalConfiguration(this ThPDSProjectGraphEdge edge)
        {
            edge.ComponentGlobalConfigurationCheck();
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
        }

        /// <summary>
        /// Node元器件选型/默认选型
        /// </summary>
        public static void ComponentSelection(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> edges, bool superiorNodeIsVirtualLoad)
        {
            switch (node.Type)
            {
                case PDSNodeType.VirtualLoad:
                    {
                        var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                        node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
                        break;
                    }
                case PDSNodeType.DistributionBox:
                    {
                        //统计节点级联电流
                        var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                        CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
                        SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
                        if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                        {
                            if (superiorNodeIsVirtualLoad)
                            {
                                oneWayInCircuit.Component = componentFactory.CreatBreaker();
                            }
                            else
                            {
                                oneWayInCircuit.Component = componentFactory.CreatOneWayIsolatingSwitch();
                            }
                        }
                        else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                        {
                            if (superiorNodeIsVirtualLoad)
                            {
                                twoWayInCircuit.Component1 = componentFactory.CreatBreaker();
                                twoWayInCircuit.Component2 = componentFactory.CreatBreaker();
                            }
                            else
                            {
                                twoWayInCircuit.Component1 = componentFactory.CreatIsolatingSwitch();
                                twoWayInCircuit.Component2 = componentFactory.CreatIsolatingSwitch();
                            }
                            twoWayInCircuit.transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                        }
                        else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                        {
                            if (superiorNodeIsVirtualLoad)
                            {
                                threeWayInCircuit.Component1 = componentFactory.CreatBreaker();
                                threeWayInCircuit.Component2 = componentFactory.CreatBreaker();
                                threeWayInCircuit.Component3 = componentFactory.CreatBreaker();
                            }
                            else
                            {
                                threeWayInCircuit.Component1 = componentFactory.CreatIsolatingSwitch();
                                threeWayInCircuit.Component2 = componentFactory.CreatIsolatingSwitch();
                                threeWayInCircuit.Component3 = componentFactory.CreatIsolatingSwitch();
                            }
                            threeWayInCircuit.transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                            threeWayInCircuit.transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                        }
                        else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                        {
                            centralized.Component = componentFactory.CreatIsolatingSwitch();
                        }
                        else
                        {
                            //暂未定义，后续补充
                            throw new NotSupportedException();
                        }
                        //统计节点级联电流
                        node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Node元器件选型/指定元器件选型
        /// </summary>
        public static PDSBaseComponent ComponentSelection(this ThPDSProjectGraphNode node, Type type, bool isOneWayInCircuit)
        {
            if (type.IsSubclassOf(typeof(PDSBaseComponent)))
            {
                var edges = PDSProject.Instance.graphData.Graph.OutEdges(node).ToList();
                //统计节点级联电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
                if (type.Equals(typeof(Meter)))
                {
                    return componentFactory.CreatMeterTransformer();
                }
                else if (type.Equals(typeof(MeterTransformer)))
                {
                    return componentFactory.CreatMeterTransformer();
                }
                else if (type.Equals(typeof(CurrentTransformer)))
                {
                    return componentFactory.CreatCurrentTransformer();
                }
                else if (type.Equals(typeof(CPS)))
                {
                    return componentFactory.CreatCPS();
                }
                else if (type.Equals(typeof(AutomaticTransferSwitch)))
                {
                    return componentFactory.CreatAutomaticTransferSwitch();
                }
                else if (type.Equals(typeof(ManualTransferSwitch)))
                {
                    return componentFactory.CreatManualTransferSwitch();
                }
                else if (type.Equals(typeof(IsolatingSwitch)))
                {
                    if (isOneWayInCircuit)
                    {
                        return componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        return componentFactory.CreatIsolatingSwitch();
                    }
                }
                else if (type.Equals(typeof(Breaker)))
                {
                    return componentFactory.CreatBreaker();
                }
                else if (type.Equals(typeof(Contactor)))
                {
                    return componentFactory.CreatContactor();
                }
                else
                {
                    //暂未支持的元器件类型
                    throw new NotSupportedException();
                }
            }
            else
            {
                //非元器件类型
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 元器件选型
        /// </summary>
        public static T ComponentChange<T>(this T component, T newComponent) where T : PDSBaseComponent
        {
            if (component is IsolatingSwitch isolatingSwitch && newComponent is IsolatingSwitch newIsolatingSwitch)
            {
                if (isolatingSwitch.Model != newIsolatingSwitch.Model && newIsolatingSwitch.GetModels().Contains(isolatingSwitch.Model))
                {
                    newIsolatingSwitch.SetModel(isolatingSwitch.Model);
                }
                if (isolatingSwitch.PolesNum != newIsolatingSwitch.PolesNum && newIsolatingSwitch.GetPolesNums().Contains(isolatingSwitch.PolesNum))
                {
                    newIsolatingSwitch.SetPolesNum(isolatingSwitch.PolesNum);
                }
                if (isolatingSwitch.RatedCurrent != newIsolatingSwitch.RatedCurrent && newIsolatingSwitch.GetRatedCurrents().Contains(isolatingSwitch.RatedCurrent))
                {
                    newIsolatingSwitch.SetRatedCurrent(isolatingSwitch.RatedCurrent);
                }
            }
            else if (component is TransferSwitch transferSwitch && newComponent is TransferSwitch newTransferSwitch)
            {
                if (transferSwitch.Model != newTransferSwitch.Model && newTransferSwitch.GetModels().Contains(transferSwitch.Model))
                {
                    newTransferSwitch.SetModel(transferSwitch.Model);
                }
                if (transferSwitch.PolesNum != newTransferSwitch.PolesNum && newTransferSwitch.GetPolesNums().Contains(transferSwitch.PolesNum))
                {
                    newTransferSwitch.SetPolesNum(transferSwitch.PolesNum);
                }
                if (transferSwitch.FrameSpecification != newTransferSwitch.FrameSpecification && newTransferSwitch.GetFrameSizes().Contains(transferSwitch.FrameSpecification))
                {
                    newTransferSwitch.SetFrameSize(transferSwitch.FrameSpecification);
                }
                if (transferSwitch.RatedCurrent != newTransferSwitch.RatedCurrent && newTransferSwitch.GetRatedCurrents().Contains(transferSwitch.RatedCurrent))
                {
                    newTransferSwitch.SetRatedCurrent(transferSwitch.RatedCurrent);
                }
            }
            else if (component is Breaker breaker && newComponent is Breaker newBreaker)
            {
                newBreaker.SetBreakerType(breaker.ComponentType);
                if (newBreaker.Model != breaker.Model && newBreaker.GetModels().Contains(breaker.Model))
                {
                    newBreaker.SetModel(breaker.Model);
                }
                if (newBreaker.PolesNum != breaker.PolesNum && newBreaker.GetPolesNums().Contains(breaker.PolesNum))
                {
                    newBreaker.SetPolesNum(breaker.PolesNum);
                }
                if (newBreaker.FrameSpecification != breaker.FrameSpecification && newBreaker.GetFrameSpecifications().Contains(breaker.FrameSpecification))
                {
                    newBreaker.SetFrameSpecification(breaker.FrameSpecification);
                }
                if (newBreaker.TripUnitType != breaker.TripUnitType && newBreaker.GetTripDevices().Contains(breaker.TripUnitType))
                {
                    newBreaker.SetTripDevice(breaker.TripUnitType);
                }
                if (newBreaker.RatedCurrent != breaker.RatedCurrent && newBreaker.GetRatedCurrents().Contains(breaker.RatedCurrent))
                {
                    newBreaker.SetRatedCurrent(breaker.RatedCurrent);
                }
            }
            else if (component is ThermalRelay thermalRelay && newComponent is ThermalRelay newThermalRelay)
            {
                //do not
                //热继电器没有选型范围，固定选型
            }
            else if (component is Contactor contactor && newComponent is Contactor newContactor)
            {
                if (newContactor.Model != contactor.Model && newContactor.GetModels().Contains(contactor.Model))
                {
                    newContactor.SetModel(contactor.Model);
                }
                if (newContactor.PolesNum != contactor.PolesNum && newContactor.GetPolesNums().Contains(contactor.PolesNum))
                {
                    newContactor.SetPolesNum(contactor.PolesNum);
                }
                if (newContactor.RatedCurrent != contactor.RatedCurrent && newContactor.GetRatedCurrents().Contains(contactor.RatedCurrent))
                {
                    newContactor.SetRatedCurrent(contactor.RatedCurrent);
                }
            }
            else if (newComponent is Conductor newConductor)
            {
                if (component is Conductor conductor)
                {
                    if (newConductor.NumberOfPhaseWire != conductor.NumberOfPhaseWire && newConductor.GetNumberOfPhaseWires().Contains(conductor.NumberOfPhaseWire))
                    {
                        newConductor.SetNumberOfPhaseWire(conductor.NumberOfPhaseWire);
                    }
                    if (newConductor.ConductorCrossSectionalArea != conductor.ConductorCrossSectionalArea && newConductor.GetConductorCrossSectionalAreas().Contains(conductor.ConductorCrossSectionalArea))
                    {
                        newConductor.SetConductorCrossSectionalArea(conductor.ConductorCrossSectionalArea);
                    }
                    newConductor.SetConductorType(conductor.ConductorType);
                    newConductor.SetMaterialStructure(conductor.OuterSheathMaterial);
                    newConductor.SetConductorMaterial(conductor.ConductorMaterial);
                    newConductor.SetConductorLayingPath(conductor.ConductorLayingPath);
                    newConductor.SetLayingSite1(conductor.LayingSite1);
                    newConductor.SetLayingSite2(conductor.LayingSite2);
                }
                else if (!component.IsNull())//为了支持空负载没有导体的case
                {
                    throw new NotSupportedException();
                }
            }
            else if (component is CPS cps && newComponent is CPS newCPS)
            {
                if (newCPS.Model != cps.Model && newCPS.GetModels().Contains(cps.Model))
                {
                    newCPS.SetModel(cps.Model);
                }
                if (newCPS.PolesNum != cps.PolesNum && newCPS.GetPolesNums().Contains(cps.PolesNum))
                {
                    newCPS.SetPolesNum(cps.PolesNum);
                }
                if (newCPS.RatedCurrent != cps.RatedCurrent && newCPS.GetRatedCurrents().Contains(cps.RatedCurrent))
                {
                    newCPS.SetRatedCurrent(cps.RatedCurrent);
                }
                if (newCPS.Combination != cps.Combination && newCPS.GetCombinations().Contains(cps.Combination))
                {
                    newCPS.SetCombination(cps.Combination);
                }
                if (newCPS.CodeLevel != cps.CodeLevel && newCPS.GetCodeLevels().Contains(cps.CodeLevel))
                {
                    newCPS.SetCodeLevel(cps.CodeLevel);
                }
                if (newCPS.ResidualCurrent != cps.ResidualCurrent && newCPS.GetResidualCurrents().Contains(cps.ResidualCurrent))
                {
                    newCPS.SetResidualCurrent(cps.ResidualCurrent);
                }
            }
            else if (component is Meter meter && newComponent is Meter newMeter)
            {
                if (newMeter.MeterParameter != meter.MeterParameter && newMeter.GetParameters().Contains(meter.MeterParameter))
                {
                    newMeter.SetParameters(meter.MeterParameter);
                }
            }
            else if (component is OUVP oucp && newComponent is OUVP newOUVP)
            {
                if (newOUVP.Model != oucp.Model && newOUVP.GetModels().Contains(oucp.Model))
                {
                    newOUVP.SetModel(oucp.Model);
                }
                if (newOUVP.PolesNum != oucp.PolesNum && newOUVP.GetPolesNums().Contains(oucp.PolesNum))
                {
                    newOUVP.SetPolesNum(oucp.PolesNum);
                }
                if (newOUVP.RatedCurrent != oucp.RatedCurrent && newOUVP.GetRatedCurrents().Contains(oucp.RatedCurrent))
                {
                    newOUVP.SetRatedCurrent(oucp.RatedCurrent);
                }
            }
            else if (component.IsNull() && newComponent.IsNull())
            {
                //DO Nothing
            }
            else
            {
                throw new NotSupportedException();
            }
            return newComponent;
        }

        /// <summary>
        /// 回路元器件选型/默认选型
        /// </summary>
        /// <param name="pDSCircuit"></param>
        /// <returns></returns>
        public static void ComponentSelection(this ThPDSProjectGraphEdge edge)
        {
            edge.Details = new CircuitDetails();
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
            if (edge.Source.Type == PDSNodeType.VirtualLoad)
            {
                edge.Details.CircuitForm = null;
            }
            else if (edge.Target.Type == PDSNodeType.VirtualLoad)
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = componentFactory.CreatBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Source.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)
            {
                //消防应急照明回路
                edge.Target.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L;
                if (edge.Source.Details.LoadCalculationInfo.HighPower != 1)
                    edge.Source.Details.LoadCalculationInfo.HighPower = 1;
                edge.Details.CircuitForm = specifyComponentFactory.GetFireEmergencyLighting();
            }
            else if (edge.Source.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ElectricalMeterPanel)
            {
                var LeakageProtection = new List<string>() { "E-BDB111", "E-BDB112", "E-BDB114", "E-BDB131" }.Contains(edge.Target.Load.ID.BlockName);
                switch (PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType)
                {
                    case MeterBoxCircuitType.上海住宅:
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetShanghaiMTCircuit();
                            if (edge.Details.CircuitForm.IsNull())
                            {
                                var circuit = new DistributionMetering_ShanghaiMTCircuit()
                                {
                                    breaker2 = componentFactory.CreatBreaker(),
                                    meter = componentFactory.CreatMeterTransformer(),
                                    breaker1 = componentFactory.CreatBreaker(),
                                    Conductor = componentFactory.CreatConductor(),
                                };
                                if (LeakageProtection)
                                {
                                    circuit.breaker2.SetBreakerType(ComponentType.组合式RCD);
                                }
                                edge.Details.CircuitForm = circuit;
                            }
                            break;
                        }
                    case MeterBoxCircuitType.江苏住宅:
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetMTInFrontCircuit();
                            if (edge.Details.CircuitForm.IsNull())
                            {
                                var circuit = new DistributionMetering_MTInFrontCircuit()
                                {
                                    meter = componentFactory.CreatMeterTransformer(),
                                    breaker = componentFactory.CreatBreaker(),
                                    Conductor = componentFactory.CreatConductor(),
                                };
                                if (LeakageProtection)
                                {
                                    circuit.breaker.SetBreakerType(ComponentType.组合式RCD);
                                }
                                edge.Details.CircuitForm = circuit;
                            }
                            break;
                        }
                    case MeterBoxCircuitType.国标_表在断路器前:
                        {
                            var circuit = new DistributionMetering_CTInFrontCircuit()
                            {
                                meter = componentFactory.CreatMeterTransformer(),
                                breaker = componentFactory.CreatBreaker(),
                                Conductor = componentFactory.CreatConductor(),
                            };
                            if (LeakageProtection)
                            {
                                circuit.breaker.SetBreakerType(ComponentType.组合式RCD);
                            }
                            edge.Details.CircuitForm = circuit;
                            break;
                        }
                    case MeterBoxCircuitType.国标_表在断路器后:
                        {
                            var circuit = new DistributionMetering_CTInBehindCircuit()
                            {
                                meter = componentFactory.CreatCurrentTransformer(),
                                breaker = componentFactory.CreatBreaker(),
                                Conductor = componentFactory.CreatConductor(),
                            };
                            if (LeakageProtection)
                            {
                                circuit.breaker.SetBreakerType(ComponentType.组合式RCD);
                            }
                            edge.Details.CircuitForm = circuit;
                            break;
                        }
                    default:
                        break;
                }
            }
            else if (edge.Target.Type == PDSNodeType.Unkown)
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = componentFactory.CreatBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.ID.BlockName == "E-BDB006-1" && edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
            {
                edge.Details.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                {
                    breaker2 = componentFactory.CreatBreaker(),
                    meter = componentFactory.CreatCurrentTransformer(),
                    breaker1 = componentFactory.CreatBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket || (new List<string>() { "E-BDB111", "E-BDB112", "E-BDB114", "E-BDB131" }.Contains(edge.Target.Load.ID.BlockName) && edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.LumpedLoad))
            {
                //漏电
                edge.Details.CircuitForm = new LeakageCircuit()
                {
                    breaker = componentFactory.CreatResidualCurrentBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Details.LoadCalculationInfo.IsDualPower)
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
                    }
                    else
                    {
                        //消防
                        if (edge.Target.Load.FireLoad)
                        {
                            if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                            {
                                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
                            }
                            else
                            {
                                switch (PDSProject.Instance.projectGlobalConfiguration.FireStartType)
                                {
                                    case FireStartType.星三角启动:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                                            break;
                                        }
                                    default:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                                            break;
                                        }
                                }

                            }
                        }
                        else
                        {
                            if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                            {
                                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
                            }
                            else
                            {
                                switch (PDSProject.Instance.projectGlobalConfiguration.NormalStartType)
                                {
                                    case FireStartType.星三角启动:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                                            break;
                                        }
                                    default:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                                            break;
                                        }
                                }

                            }
                        }
                    }
                }
                else
                {
                    if (edge.Target.Details.LoadCalculationInfo.IsDualPower)
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
                    }
                    else
                    {
                        //消防
                        if (edge.Target.Load.FireLoad)
                        {
                            if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                            {
                                edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
                            }
                            else
                            {
                                switch (PDSProject.Instance.projectGlobalConfiguration.FireStartType)
                                {
                                    case FireStartType.星三角启动:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                                            break;
                                        }
                                    default:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                                            break;
                                        }
                                }
                            }
                        }
                        else
                        {
                            if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                            {
                                edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
                            }
                            else
                            {
                                switch (PDSProject.Instance.projectGlobalConfiguration.NormalStartType)
                                {
                                    case FireStartType.星三角启动:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                                            break;
                                        }
                                    default:
                                        {
                                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = componentFactory.CreatBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }

            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Target.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
        }

        /// <summary>
        /// 计算控制回路
        /// </summary>
        public static void CalculateSecondaryCircuit(this List<ThPDSProjectGraphEdge> edges)
        {
            var projectGraph = _projectGraph;
            var SubmersiblePumps = edges.Where(o => o.Target.Load.LoadTypeCat_3 == ThPDSLoadTypeCat_3.SubmersiblePump);
            if (SubmersiblePumps.Count() > 0)
            {
                var edge = SubmersiblePumps.First();
                var secondaryCircuitInfo = SecondaryCircuitConfiguration.SecondaryCircuitConfigs["SubmersiblePump"].First();
                var secondaryCircuit = ThPDSProjectGraphService.AddControlCircuit(projectGraph, edge, secondaryCircuitInfo);
                SubmersiblePumps.Skip(1).ForEach(e => ThPDSProjectGraphService.AssignCircuit2ControlCircuit(edge.Source, secondaryCircuit, e));
            }
            foreach (ThPDSProjectGraphEdge edge in edges.Except(SubmersiblePumps))
            {
                if (edge.Target.Load.LoadTypeCat_3 != ThPDSLoadTypeCat_3.None)
                {
                    var key = edge.Target.Load.LoadTypeCat_3.ToString();
                    if (SecondaryCircuitConfiguration.SecondaryCircuitConfigs.ContainsKey(key))
                    {
                        var secondaryCircuitInfos = SecondaryCircuitConfiguration.SecondaryCircuitConfigs[key];
                        foreach (SecondaryCircuitInfo item in secondaryCircuitInfos)
                        {
                            ThPDSProjectGraphService.AddControlCircuit(projectGraph, edge, item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 回路元器件选型/指定元器件选型
        /// </summary>
        /// <returns></returns>
        public static PDSBaseComponent ComponentSelection(this ThPDSProjectGraphEdge edge, Type type, CircuitFormOutType circuitFormOutType, Breaker breaker = null)
        {
            if (type.IsSubclassOf(typeof(PDSBaseComponent)))
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
                if (type.Equals(typeof(Breaker)))
                {
                    if (circuitFormOutType == CircuitFormOutType.漏电)
                        return componentFactory.CreatResidualCurrentBreaker();
                    else if (breaker.IsNull())
                        return componentFactory.CreatBreaker();
                    else
                        return componentFactory.CreatBreaker(breaker);
                }
                else if (type.Equals(typeof(ThermalRelay)))
                {
                    return componentFactory.CreatThermalRelay();
                }
                else if (type.Equals(typeof(Contactor)))
                {
                    return componentFactory.CreatContactor();
                }
                else if (type.Equals(typeof(Meter)))
                {
                    if (circuitFormOutType == CircuitFormOutType.配电计量_上海直接表 || circuitFormOutType == CircuitFormOutType.配电计量_直接表在前 || circuitFormOutType == CircuitFormOutType.配电计量_直接表在后)
                    {
                        return componentFactory.CreatMeterTransformer();
                    }
                    else
                    {
                        return componentFactory.CreatCurrentTransformer();
                    }
                }
                else if (type.Equals(typeof(MeterTransformer)))
                {
                    return componentFactory.CreatMeterTransformer();
                }
                else if (type.Equals(typeof(CurrentTransformer)))
                {
                    return componentFactory.CreatCurrentTransformer();
                }
                else if (type.Equals(typeof(CPS)))
                {
                    return componentFactory.CreatCPS();
                }
                else if (type.Equals(typeof(Conductor)))
                {
                    return componentFactory.CreatConductor();
                }
                else
                {
                    //暂未支持的元器件类型
                    throw new NotSupportedException();
                }
            }
            else
            {
                //非元器件类型
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 回路元器件选型/指定回路类型选型
        /// </summary>
        /// <param name="pDSCircuit"></param>
        /// <returns></returns>
        public static void ComponentSelection(this ThPDSProjectGraphEdge edge, CircuitFormOutType circuitFormOutType)
        {
            edge.Details = new CircuitDetails();
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
            switch (circuitFormOutType)
            {
                case CircuitFormOutType.常规:
                    {
                        edge.Details.CircuitForm = new RegularCircuit()
                        {
                            breaker = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.漏电:
                    {
                        edge.Details.CircuitForm = new LeakageCircuit()
                        {
                            breaker = componentFactory.CreatResidualCurrentBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        edge.Details.CircuitForm = new ContactorControlCircuit()
                        {
                            breaker = componentFactory.CreatBreaker(),
                            contactor = componentFactory.CreatContactor(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        edge.Details.CircuitForm = new ThermalRelayProtectionCircuit()
                        {
                            breaker = componentFactory.CreatBreaker(),
                            thermalRelay = componentFactory.CreatThermalRelay(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                        {
                            breaker2 = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatCurrentTransformer(),
                            breaker1 = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_ShanghaiMTCircuit()
                        {
                            breaker2 = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatMeterTransformer(),
                            breaker1 = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在前:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_CTInFrontCircuit()
                        {
                            meter = componentFactory.CreatCurrentTransformer(),
                            breaker = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在前:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_MTInFrontCircuit()
                        {
                            meter = componentFactory.CreatMeterTransformer(),
                            breaker = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_CT表在后:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_CTInBehindCircuit()
                        {
                            breaker = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatCurrentTransformer(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_直接表在后:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_MTInBehindCircuit()
                        {
                            breaker = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatMeterTransformer(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.电动机_分立元件:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
                        break;
                    }
                case CircuitFormOutType.电动机_CPS:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
                        break;
                    }
                case CircuitFormOutType.电动机_分立元件星三角启动:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                        break;
                    }
                case CircuitFormOutType.电动机_CPS星三角启动:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                        break;
                    }
                case CircuitFormOutType.消防应急照明回路WFEL:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetFireEmergencyLighting();
                        break;
                    }
                case CircuitFormOutType.双速电动机_CPSdetailYY:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSDYYCircuit();
                        break;
                    }
                case CircuitFormOutType.双速电动机_CPSYY:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
                        break;
                    }
                case CircuitFormOutType.双速电动机_分立元件detailYY:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsDYYCircuit();
                        break;
                    }
                case CircuitFormOutType.双速电动机_分立元件YY:
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
                        break;
                    }
                default:
                    {
                        //暂未支持该回路类型，请暂时不要选择该回路
                        throw new NotSupportedException();
                    }
            }
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Target.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
        }

        public static double CalculateHighPower(this ThPDSProjectGraphNode source)
        {
            var edges = _projectGraph.OutEdges(source).ToList();
            if (source.Details.CircuitFormType?.CircuitFormType == CircuitFormInType.集中电源)
            {
                return edges.Sum(o => o.Target.Details.LoadCalculationInfo.HighPower);
            }
            else if (source.Load.Phase == ThPDSPhase.一相)
            {
                return edges.Sum(o => o.Target.Details.LoadCalculationInfo.HighPower);
            }
            double power = 0;
            double L1Power = 0, L2Power = 0, L3Power = 0;
            edges.ForEach(edge =>
            {
                var node = edge.Target;
                switch (node.Details.LoadCalculationInfo.PhaseSequence)
                {
                    case PhaseSequence.L1:
                        {
                            L1Power += node.Details.LoadCalculationInfo.HighPower;
                            break;
                        }
                    case PhaseSequence.L2:
                        {
                            L2Power += node.Details.LoadCalculationInfo.HighPower;
                            break;
                        }
                    case PhaseSequence.L3:
                        {
                            L3Power += node.Details.LoadCalculationInfo.HighPower;
                            break;
                        }
                    case PhaseSequence.L:
                        {
                            L3Power += node.Details.LoadCalculationInfo.HighPower;
                            break;
                        }
                    case PhaseSequence.L123:
                        {
                            power += node.Details.LoadCalculationInfo.HighPower;
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            });
            return power + 3 * Math.Max(Math.Max(L1Power, L2Power), L3Power);
        }

        public static double CalculateLowPower(this ThPDSProjectGraphNode source)
        {
            if (!source.Details.LoadCalculationInfo.IsDualPower)
            {
                return 0;
            }
            var edges = _projectGraph.OutEdges(source).ToList();
            if (source.Load.Phase == ThPDSPhase.一相)
            {
                return edges.Sum(o => o.Target.Details.LoadCalculationInfo.LowPower);
            }
            double power = 0;
            double L1Power = 0, L2Power = 0, L3Power = 0;
            edges.ForEach(edge =>
            {
                var node = edge.Target;
                switch (node.Details.LoadCalculationInfo.PhaseSequence)
                {
                    case PhaseSequence.L1:
                        {
                            L1Power += node.Details.LoadCalculationInfo.LowPower;
                            break;
                        }
                    case PhaseSequence.L2:
                        {
                            L2Power += node.Details.LoadCalculationInfo.LowPower;
                            break;
                        }
                    case PhaseSequence.L3:
                        {
                            L3Power += node.Details.LoadCalculationInfo.LowPower;
                            break;
                        }
                    case PhaseSequence.L:
                        {
                            L3Power += node.Details.LoadCalculationInfo.LowPower;
                            break;
                        }
                    case PhaseSequence.L123:
                        {
                            power += node.Details.LoadCalculationInfo.LowPower;
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            });
            return power + 3 * Math.Max(Math.Max(L1Power, L2Power), L3Power);
        }

        public static double CalculatePower(this ThPDSProjectGraphNode source, MiniBusbar miniBusbar)
        {
            var edges = source.Details.MiniBusbars[miniBusbar];
            var edge = edges.FirstOrDefault();
            if (edge.IsNull())
            {
                return 0;
            }
            else
            {
                if (edge.Target.Load.Phase == ThPDSPhase.一相 && edges.All(o => o.Target.Details.LoadCalculationInfo.PhaseSequence == edge.Target.Details.LoadCalculationInfo.PhaseSequence))
                {
                    miniBusbar.PhaseSequence = edge.Target.Details.LoadCalculationInfo.PhaseSequence;
                    miniBusbar.Phase = ThPDSPhase.一相;
                    return edges.Sum(o => o.Target.Details.LoadCalculationInfo.HighPower);
                }
                else
                {
                    miniBusbar.PhaseSequence = PhaseSequence.L123;
                    miniBusbar.Phase = ThPDSPhase.三相;
                    double power = 0;
                    double L1Power = 0, L2Power = 0, L3Power = 0;
                    edges.ForEach(e =>
                    {
                        var node = e.Target;
                        switch (node.Details.LoadCalculationInfo.PhaseSequence)
                        {
                            case PhaseSequence.L1:
                                {
                                    L1Power += node.Details.LoadCalculationInfo.HighPower;
                                    break;
                                }
                            case PhaseSequence.L2:
                                {
                                    L2Power += node.Details.LoadCalculationInfo.HighPower;
                                    break;
                                }
                            case PhaseSequence.L3:
                                {
                                    L3Power += node.Details.LoadCalculationInfo.HighPower;
                                    break;
                                }
                            case PhaseSequence.L:
                                {
                                    L3Power += node.Details.LoadCalculationInfo.HighPower;
                                    break;
                                }
                            case PhaseSequence.L123:
                                {
                                    power += node.Details.LoadCalculationInfo.HighPower;
                                    break;
                                }
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                        }
                    });
                    return power + 3 * Math.Max(Math.Max(L1Power, L2Power), L3Power);
                }
            }

        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this List<ThPDSProjectGraphEdge> edges)
        {
            var onePhase = new List<ThPDSProjectGraphNode>();
            foreach (var edge in edges.GetSortedEdges())
            {
                if (edge.Target.Load.Phase == ThPDSPhase.三相)
                {
                    edge.Target.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L123;
                }
                else if (edge.Target.Load.Phase == ThPDSPhase.一相 && edge.Target.Details.LoadCalculationInfo.PhaseSequence != PhaseSequence.L)
                {
                    onePhase.Add(edge.Target);
                }
            }
            var zeroNodes = onePhase.Where(o => o.Details.LoadCalculationInfo.HighPower <= 0).ToList();
            for (int i = 0; i < zeroNodes.Count; i++)
            {
                zeroNodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1;
                if (++i < zeroNodes.Count)
                    zeroNodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2;
                if (++i < zeroNodes.Count)
                    zeroNodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3;
            }
            onePhase = onePhase.Except(zeroNodes).ToList();
            if (onePhase.Count > 0)
                onePhase.BalancedPhaseSequence();
        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this List<ThPDSProjectGraphNode> nodes)
        {
            if (nodes.All(o => o.Details.LoadCalculationInfo.HighPower == nodes[0].Details.LoadCalculationInfo.HighPower))
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1;
                    if (++i < nodes.Count)
                        nodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2;
                    if (++i < nodes.Count)
                        nodes[i].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3;
                }
            }
            else if (nodes.Count == 1)
            {
                nodes[0].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1;
            }
            else if (nodes.Count == 2)
            {
                nodes[0].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1;
                nodes[1].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2;
            }
            else if (nodes.Count == 3)
            {
                nodes[0].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1;
                nodes[1].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2;
                nodes[2].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3;
            }
            else
            {
                var powerSum = nodes.Sum(x => x.Details.LoadCalculationInfo.HighPower);
                var averagePower = powerSum / 3;
                var HighPowerNodes = nodes.Where(o => o.Details.LoadCalculationInfo.HighPower > averagePower).ToList();
                if (HighPowerNodes.Count == 2)
                {
                    HighPowerNodes[0].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2;
                    HighPowerNodes[1].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3;
                    nodes.Except(HighPowerNodes).ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1);
                }
                else if (HighPowerNodes.Count == 1)
                {
                    HighPowerNodes[0].Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3;
                    nodes.Remove(HighPowerNodes[0]);
                    var L1Nodes = nodes.ChoisePhaseSequence(averagePower);
                    L1Nodes.ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1);
                    nodes.Except(L1Nodes).ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2);
                }
                else
                {
                    var L1Nodes = nodes.ChoisePhaseSequence(averagePower);
                    L1Nodes.ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L1);
                    var L23 = nodes.Except(L1Nodes).ToList();
                    var L2Nodes = L23.ChoisePhaseSequence(averagePower);
                    L2Nodes.ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L2);
                    L23.Except(L2Nodes).ForEach(o => o.Details.LoadCalculationInfo.PhaseSequence = PhaseSequence.L3);
                }
            }
        }

        /// <summary>
        /// 选择相序
        /// 此算法暂时采用dp思想求解
        /// 或者换个说话，同权重的01背包问题
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<ThPDSProjectGraphNode> ChoisePhaseSequence(this List<ThPDSProjectGraphNode> nodes, double target)
        {
            List<ThPDSProjectGraphNode> result = new List<ThPDSProjectGraphNode>();
            var target_int = (int)Math.Ceiling(target);
            int multiple = (int)Math.Ceiling(target / 100);
            int[] vs = new int[nodes.Count];
            for (int i = 0; i < vs.Length; i++)
            {
                vs[i] = (int)Math.Ceiling(nodes[i].Details.LoadCalculationInfo.HighPower / multiple);
            }
            var capacity = (int)(target_int / multiple);
            int[,] dp = new int[nodes.Count + 1, capacity + 1];
            //dp[i,j]表示在可以装 第0至i件物品，并且背包容量为j   可以装的最大值
            for (int i = 1; i < nodes.Count + 1; i++)
            {
                for (int j = 1; j < capacity + 1; j++)
                {
                    if (j >= vs[i - 1])
                    {
                        dp[i, j] = (int)Math.Max(dp[i - 1, j], vs[i - 1] + dp[i - 1, j - vs[i - 1]]);
                    }
                    else
                    {
                        dp[i, j] = dp[i - 1, j];
                    }
                }
            }
            int x = nodes.Count, y = capacity;
            while (x > 0 && y > 0)
            {
                if (dp[x, y] != dp[x - 1, y])
                {
                    result.Add(nodes[x - 1]);
                    y -= vs[x - 1];
                    x--;
                }
                else
                {
                    x--;
                }
            }
            return result;
        }

        /// <summary>
        /// 修改节点
        /// </summary>
        /// <param name="graph">图</param>
        /// <param name="node">节点</param>
        /// <param name="IsPhaseSequenceChange">是否改变相序</param>
        public static void UpdateWithNode(this ThPDSProjectGraphNode node, bool IsPhaseSequenceChange)
        {
            var edges = _projectGraph.OutEdges(node).ToList();
            if (IsPhaseSequenceChange)
            {
                node.Details.LoadCalculationInfo.HighPower = node.CalculateHighPower();
                node.Details.LoadCalculationInfo.LowPower = Math.Max(node.Details.LoadCalculationInfo.LowPower, node.CalculateLowPower());
            }
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.ComponentCheck(CascadeCurrent);

            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = _projectGraph.InEdges(node).ToList();
            edges.ForEach(e => e.CheckWithEdge());
        }

        /// <summary>
        /// 修改小母排
        /// </summary>
        /// <param name="graph">图</param>
        /// <param name="node">节点</param>
        /// <param name="IsPhaseSequenceChange">是否改变相序</param>
        public static void UpdateWithMiniBusbar(this ThPDSProjectGraphNode node, MiniBusbar miniBusbar, bool IsPhaseSequenceChange)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            if (IsPhaseSequenceChange)
            {
                miniBusbar.Power = node.CalculatePower(miniBusbar);
            }
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            node.ComponentCheck(miniBusbar, CascadeCurrent);

            miniBusbar.CascadeCurrent = Math.Max(CascadeCurrent, miniBusbar.GetCascadeCurrent());
            node.CheckCascadeWithNode();
        }

        /// <summary>
        /// 检查节点
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public static void CheckWithNode(this ThPDSProjectGraphNode node)
        {
            var edges = _projectGraph.OutEdges(node).ToList();
            node.Details.LoadCalculationInfo.HighPower = Math.Max(node.Details.LoadCalculationInfo.HighPower, node.CalculateHighPower());
            if (node.Details.LoadCalculationInfo.IsDualPower)
            {
                node.Details.LoadCalculationInfo.LowPower = Math.Max(node.Details.LoadCalculationInfo.LowPower, node.CalculateLowPower());
            }
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.ComponentCheck(CascadeCurrent);

            var cascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            if (node.Details.CascadeCurrent > cascadeCurrent)
            {
                return;
            }
            node.Details.CascadeCurrent = cascadeCurrent;
            edges = _projectGraph.InEdges(node).ToList();
            edges.ForEach(e => e.CheckWithEdge());
        }

        /// <summary>
        /// 检查小母排
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="miniBusbar"></param>
        public static void CheckWithMiniBusbar(this ThPDSProjectGraphNode node, MiniBusbar miniBusbar)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            miniBusbar.Power = Math.Max(miniBusbar.Power, node.CalculatePower(miniBusbar));

            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            node.ComponentCheck(miniBusbar, CascadeCurrent);

            miniBusbar.CascadeCurrent = Math.Max(CascadeCurrent, miniBusbar.GetCascadeCurrent());
        }

        /// <summary>
        /// 检查回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void CheckWithEdge(this ThPDSProjectGraphEdge edge)
        {
            if (!edge.Details.CircuitLock)
            {
                edge.ComponentCheck();
            }
            //统计回路级联电流
            var cascadeCurrent = Math.Max(edge.Target.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
            if (edge.Details.CascadeCurrent > cascadeCurrent)
            {
                return;
            }
            edge.Details.CascadeCurrent = cascadeCurrent;

            var node = edge.Source;
            var miniBusbar = node.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge)).Key;
            if (miniBusbar.IsNull())
            {
                edge.Source.CheckWithNode();
            }
            else
            {
                node.CheckWithMiniBusbar(miniBusbar);
                node.CheckWithNode();
            }
        }

        /// <summary>
        /// 检查节点级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public static void CheckCascadeWithNode(this ThPDSProjectGraphNode node)
        {
            var edges = _projectGraph.OutEdges(node).ToList();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.ComponentCheckCascade(CascadeCurrent);

            var cascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            if (node.Details.CascadeCurrent > cascadeCurrent)
            {
                return;
            }
            node.Details.CascadeCurrent = cascadeCurrent;
            edges = _projectGraph.InEdges(node).ToList();
            edges.ForEach(e => e.CheckCascadeWithEdge());
        }

        /// <summary>
        /// 检查小母排级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="miniBusbar"></param>
        public static void CheckCascadeWithMiniBusbar(this ThPDSProjectGraphNode node, MiniBusbar miniBusbar)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            miniBusbar.Power = Math.Max(miniBusbar.Power, node.CalculatePower(miniBusbar));

            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            node.ComponentCheckCascade(miniBusbar, CascadeCurrent);

            miniBusbar.CascadeCurrent = Math.Max(CascadeCurrent, miniBusbar.GetCascadeCurrent());
        }

        /// <summary>
        /// 检查回路级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void CheckCascadeWithEdge(this ThPDSProjectGraphEdge edge)
        {
            edge.ComponentCheckCascade();
            //统计回路级联电流
            var cascadeCurrent = Math.Max(edge.Target.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
            if (edge.Details.CascadeCurrent > cascadeCurrent)
            {
                return;
            }
            edge.Details.CascadeCurrent = cascadeCurrent;

            var node = edge.Source;
            var miniBusbar = node.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge)).Key;
            if (miniBusbar.IsNull())
            {
                edge.Source.CheckCascadeWithNode();
            }
            else
            {
                node.CheckCascadeWithMiniBusbar(miniBusbar);
                node.CheckCascadeWithNode();
            }
        }

        /// <summary>
        /// Node元器件选型检查
        /// </summary>
        public static void ComponentCheck(this ThPDSProjectGraphNode node, double cascadeCurrent)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, cascadeCurrent);
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    PDSBaseComponent component;
                    if (oneWayInCircuit.Component.ComponentType == ComponentType.QL)
                    {
                        component = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component = componentFactory.CreatBreaker();
                    }
                    oneWayInCircuit.Component = oneWayInCircuit.Component.ComponentChange(component);
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    PDSBaseComponent component1;
                    PDSBaseComponent component2;
                    if (twoWayInCircuit.Component1.ComponentType == ComponentType.QL)
                    {
                        component1 = componentFactory.CreatOneWayIsolatingSwitch();
                        component2 = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component1 = componentFactory.CreatBreaker();
                        component2 = componentFactory.CreatBreaker();
                    }
                    twoWayInCircuit.Component1 = twoWayInCircuit.Component1.ComponentChange(component1);
                    twoWayInCircuit.Component2 = twoWayInCircuit.Component2.ComponentChange(component2);

                    TransferSwitch transferSwitch;
                    if (twoWayInCircuit.transferSwitch is ManualTransferSwitch MTSE)
                    {
                        transferSwitch = componentFactory.CreatManualTransferSwitch();
                    }
                    else
                    {
                        transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    twoWayInCircuit.transferSwitch = twoWayInCircuit.transferSwitch.ComponentChange(transferSwitch);
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    PDSBaseComponent component1;
                    PDSBaseComponent component2;
                    PDSBaseComponent component3;
                    if (threeWayInCircuit.Component1.ComponentType == ComponentType.QL)
                    {
                        component1 = componentFactory.CreatOneWayIsolatingSwitch();
                        component2 = componentFactory.CreatOneWayIsolatingSwitch();
                        component3 = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component1 = componentFactory.CreatBreaker();
                        component2 = componentFactory.CreatBreaker();
                        component3 = componentFactory.CreatBreaker();
                    }
                    threeWayInCircuit.Component1 = threeWayInCircuit.Component1.ComponentChange(component1);
                    threeWayInCircuit.Component2 = threeWayInCircuit.Component2.ComponentChange(component2);
                    threeWayInCircuit.Component3 = threeWayInCircuit.Component3.ComponentChange(component3);

                    TransferSwitch transferSwitch1;
                    if (threeWayInCircuit.transferSwitch1 is ManualTransferSwitch)
                    {
                        transferSwitch1 = componentFactory.CreatManualTransferSwitch();
                    }
                    else
                    {
                        transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch1 = threeWayInCircuit.transferSwitch1.ComponentChange(transferSwitch1);

                    TransferSwitch transferSwitch2;
                    if (threeWayInCircuit.transferSwitch2 is ManualTransferSwitch)
                    {
                        transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                    }
                    else
                    {
                        transferSwitch2 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch2 = threeWayInCircuit.transferSwitch2.ComponentChange(transferSwitch2);
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    centralized.Component = centralized.Component.ComponentChange(isolatingSwitch);
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Node元器件级联检查
        /// </summary>
        public static void ComponentCheckCascade(this ThPDSProjectGraphNode node, double cascadeCurrent)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, cascadeCurrent);
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    PDSBaseComponent component;
                    if (oneWayInCircuit.Component.ComponentType == ComponentType.QL)
                    {
                        component = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component = componentFactory.CreatBreaker();
                    }
                    oneWayInCircuit.Component = oneWayInCircuit.Component.ComponentChange(component);
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    PDSBaseComponent component1;
                    PDSBaseComponent component2;
                    if (twoWayInCircuit.Component1.ComponentType == ComponentType.QL)
                    {
                        component1 = componentFactory.CreatOneWayIsolatingSwitch();
                        component2 = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component1 = componentFactory.CreatBreaker();
                        component2 = componentFactory.CreatBreaker();
                    }
                    twoWayInCircuit.Component1 = twoWayInCircuit.Component1.ComponentChange(component1);
                    twoWayInCircuit.Component2 = twoWayInCircuit.Component2.ComponentChange(component2);

                    TransferSwitch transferSwitch;
                    if (twoWayInCircuit.transferSwitch is ManualTransferSwitch MTSE)
                    {
                        transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch = componentFactory.CreatManualTransferSwitch();
                    }
                    twoWayInCircuit.transferSwitch = twoWayInCircuit.transferSwitch.ComponentChange(transferSwitch);
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    PDSBaseComponent component1;
                    PDSBaseComponent component2;
                    PDSBaseComponent component3;
                    if (threeWayInCircuit.Component1.ComponentType == ComponentType.QL)
                    {
                        component1 = componentFactory.CreatOneWayIsolatingSwitch();
                        component2 = componentFactory.CreatOneWayIsolatingSwitch();
                        component3 = componentFactory.CreatOneWayIsolatingSwitch();
                    }
                    else
                    {
                        component1 = componentFactory.CreatBreaker();
                        component2 = componentFactory.CreatBreaker();
                        component3 = componentFactory.CreatBreaker();
                    }
                    threeWayInCircuit.Component1 = threeWayInCircuit.Component1.ComponentChange(component1);
                    threeWayInCircuit.Component2 = threeWayInCircuit.Component2.ComponentChange(component2);
                    threeWayInCircuit.Component3 = threeWayInCircuit.Component3.ComponentChange(component3);

                    TransferSwitch transferSwitch1;
                    if (threeWayInCircuit.transferSwitch1 is ManualTransferSwitch)
                    {
                        transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch1 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch1 = threeWayInCircuit.transferSwitch1.ComponentChange(transferSwitch1);

                    TransferSwitch transferSwitch2;
                    if (threeWayInCircuit.transferSwitch2 is ManualTransferSwitch)
                    {
                        transferSwitch2 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch2 = threeWayInCircuit.transferSwitch2.ComponentChange(transferSwitch2);
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    centralized.Component = centralized.Component.ComponentChange(isolatingSwitch);
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 小母排元器件选型检查
        /// </summary>
        public static void ComponentCheck(this ThPDSProjectGraphNode node, MiniBusbar miniBusbar, double cascadeCurrent)
        {
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, miniBusbar, cascadeCurrent);
            var breaker = componentFactory.CreatBreaker();
            if (miniBusbar.Breaker.IsNull())
                miniBusbar.Breaker = breaker;
            else
                miniBusbar.Breaker = miniBusbar.Breaker.ComponentChange(breaker);
        }

        /// <summary>
        /// 小母排元器件级联检查
        /// </summary>
        public static void ComponentCheckCascade(this ThPDSProjectGraphNode node, MiniBusbar miniBusbar, double cascadeCurrent)
        {
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, miniBusbar, cascadeCurrent);
            var breaker = componentFactory.CreatBreaker();
            if (miniBusbar.Breaker.IsNull())
                miniBusbar.Breaker = breaker;
            else
                miniBusbar.Breaker = miniBusbar.Breaker.ComponentChange(breaker);
        }

        /// <summary>
        /// 回路元器件选型检查
        /// </summary>
        public static void ComponentCheck(this ThPDSProjectGraphEdge edge)
        {
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
            if (edge.Details.CircuitForm.IsNull())
            {
                //DO NOT
            }
            else if (edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                regularCircuit.breaker = regularCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                regularCircuit.Conductor = regularCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is LeakageCircuit leakageCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                leakageCircuit.breaker = leakageCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                leakageCircuit.Conductor = leakageCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ContactorControlCircuit contactorControlCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                contactorControlCircuit.breaker = contactorControlCircuit.breaker.ComponentChange(breaker);

                var contacter = componentFactory.CreatContactor();
                contactorControlCircuit.contactor = contactorControlCircuit.contactor.ComponentChange(contacter);

                var conductor = componentFactory.CreatConductor();
                contactorControlCircuit.Conductor = contactorControlCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ThermalRelayProtectionCircuit thermalRelayCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                thermalRelayCircuit.breaker = thermalRelayCircuit.breaker.ComponentChange(breaker);

                var thermalRelay = componentFactory.CreatThermalRelay();
                thermalRelayCircuit.thermalRelay = thermalRelayCircuit.thermalRelay.ComponentChange(thermalRelay);

                var conductor = componentFactory.CreatConductor();
                thermalRelayCircuit.Conductor = thermalRelayCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiCTCircuit shanghaiCTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker2 = shanghaiCTCircuit.breaker2.ComponentChange(breaker2);

                Meter meter;
                if (shanghaiCTCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != shanghaiCTCircuit.meter.ComponentType)
                {
                    shanghaiCTCircuit.meter = meter;
                }
                else
                {
                    shanghaiCTCircuit.meter = shanghaiCTCircuit.meter.ComponentChange(meter);
                }

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker1 = shanghaiCTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiCTCircuit.Conductor = shanghaiCTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiMTCircuit shanghaiMTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker2 = shanghaiMTCircuit.breaker2.ComponentChange(breaker2);

                Meter meter;
                if (shanghaiMTCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != shanghaiMTCircuit.meter.ComponentType)
                {
                    shanghaiMTCircuit.meter = meter;
                }
                else
                {
                    shanghaiMTCircuit.meter = shanghaiMTCircuit.meter.ComponentChange(meter);
                }

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker1 = shanghaiMTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiMTCircuit.Conductor = shanghaiMTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInFrontCircuit CTInFrontCircuit)
            {
                Meter meter;
                if (CTInFrontCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != CTInFrontCircuit.meter.ComponentType)
                {
                    CTInFrontCircuit.meter = meter;
                }
                else
                {
                    CTInFrontCircuit.meter = CTInFrontCircuit.meter.ComponentChange(meter);
                }

                var breaker = componentFactory.CreatBreaker();
                CTInFrontCircuit.breaker = CTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                CTInFrontCircuit.Conductor = CTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInFrontCircuit MTInFrontCircuit)
            {
                Meter meter;
                if (MTInFrontCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != MTInFrontCircuit.meter.ComponentType)
                {
                    MTInFrontCircuit.meter = meter;
                }
                else
                {
                    MTInFrontCircuit.meter = MTInFrontCircuit.meter.ComponentChange(meter);
                }

                var breaker = componentFactory.CreatBreaker();
                MTInFrontCircuit.breaker = MTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                MTInFrontCircuit.Conductor = MTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInBehindCircuit CTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                CTInBehindCircuit.breaker = CTInBehindCircuit.breaker.ComponentChange(breaker);

                Meter meter;
                if (CTInBehindCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != CTInBehindCircuit.meter.ComponentType)
                {
                    CTInBehindCircuit.meter = meter;
                }
                else
                {
                    CTInBehindCircuit.meter = CTInBehindCircuit.meter.ComponentChange(meter);
                }

                var conductor = componentFactory.CreatConductor();
                CTInBehindCircuit.Conductor = CTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInBehindCircuit MTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                MTInBehindCircuit.breaker = MTInBehindCircuit.breaker.ComponentChange(breaker);

                Meter meter;
                if (MTInBehindCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != MTInBehindCircuit.meter.ComponentType)
                {
                    MTInBehindCircuit.meter = meter;
                }
                else
                {
                    MTInBehindCircuit.meter = MTInBehindCircuit.meter.ComponentChange(meter);
                }

                var conductor = componentFactory.CreatConductor();
                MTInBehindCircuit.Conductor = MTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is FireEmergencyLighting fireEmergencyLighting)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetFireEmergencyLighting();
            }
            else if (edge.Details.CircuitForm is Motor_DiscreteComponentsCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_CPSCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_DiscreteComponentsStarTriangleStartCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_CPSStarTriangleStartCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsDYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsDYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_CPSDYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSDYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_CPSYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
            }
            else
            {
                //框架已搭好，后续补充完成
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 回路元器件选型检查(全局配置更新)
        /// </summary>
        public static void ComponentGlobalConfigurationCheck(this ThPDSProjectGraphEdge edge)
        {
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
            if (edge.Details.CircuitForm.IsNull())
            {
                //DO NOT
            }
            else if (edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                regularCircuit.breaker = regularCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                regularCircuit.Conductor = regularCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is LeakageCircuit leakageCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                leakageCircuit.breaker = leakageCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                leakageCircuit.Conductor = leakageCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ContactorControlCircuit contactorControlCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                contactorControlCircuit.breaker = contactorControlCircuit.breaker.ComponentChange(breaker);

                var contacter = componentFactory.CreatContactor();
                contactorControlCircuit.contactor = contactorControlCircuit.contactor.ComponentChange(contacter);

                var conductor = componentFactory.CreatConductor();
                contactorControlCircuit.Conductor = contactorControlCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ThermalRelayProtectionCircuit thermalRelayCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                thermalRelayCircuit.breaker = thermalRelayCircuit.breaker.ComponentChange(breaker);

                var thermalRelay = componentFactory.CreatThermalRelay();
                thermalRelayCircuit.thermalRelay = thermalRelayCircuit.thermalRelay.ComponentChange(thermalRelay);

                var conductor = componentFactory.CreatConductor();
                thermalRelayCircuit.Conductor = thermalRelayCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Source.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ElectricalMeterPanel)
            {
                switch (PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType)
                {
                    case MeterBoxCircuitType.上海住宅:
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetShanghaiMTCircuit();
                            if (edge.Details.CircuitForm.IsNull())
                            {
                                edge.Details.CircuitForm = new DistributionMetering_ShanghaiMTCircuit()
                                {
                                    breaker2 = componentFactory.CreatBreaker(),
                                    meter = componentFactory.CreatMeterTransformer(),
                                    breaker1 = componentFactory.CreatBreaker(),
                                    Conductor = componentFactory.CreatConductor(),
                                };
                            }
                            break;
                        }
                    case MeterBoxCircuitType.江苏住宅:
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetMTInFrontCircuit();
                            if (edge.Details.CircuitForm.IsNull())
                            {
                                edge.Details.CircuitForm = new DistributionMetering_MTInFrontCircuit()
                                {
                                    meter = componentFactory.CreatMeterTransformer(),
                                    breaker = componentFactory.CreatBreaker(),
                                    Conductor = componentFactory.CreatConductor(),
                                };
                            }
                            break;
                        }
                    case MeterBoxCircuitType.国标_表在断路器前:
                        {
                            edge.Details.CircuitForm = new DistributionMetering_CTInFrontCircuit()
                            {
                                meter = componentFactory.CreatMeterTransformer(),
                                breaker = componentFactory.CreatBreaker(),
                                Conductor = componentFactory.CreatConductor(),
                            };
                            break;
                        }
                    case MeterBoxCircuitType.国标_表在断路器后:
                        {
                            edge.Details.CircuitForm = new DistributionMetering_CTInBehindCircuit()
                            {
                                meter = componentFactory.CreatCurrentTransformer(),
                                breaker = componentFactory.CreatBreaker(),
                                Conductor = componentFactory.CreatConductor(),
                            };
                            break;
                        }
                    default:
                        break;
                }
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiCTCircuit shanghaiCTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker2 = shanghaiCTCircuit.breaker2.ComponentChange(breaker2);

                Meter meter;
                if (shanghaiCTCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != shanghaiCTCircuit.meter.ComponentType)
                {
                    shanghaiCTCircuit.meter = meter;
                }
                else
                {
                    shanghaiCTCircuit.meter = shanghaiCTCircuit.meter.ComponentChange(meter);
                }

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker1 = shanghaiCTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiCTCircuit.Conductor = shanghaiCTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiMTCircuit shanghaiMTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker2 = shanghaiMTCircuit.breaker2.ComponentChange(breaker2);

                Meter meter;
                if (shanghaiMTCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != shanghaiMTCircuit.meter.ComponentType)
                {
                    shanghaiMTCircuit.meter = meter;
                }
                else
                {
                    shanghaiMTCircuit.meter = shanghaiMTCircuit.meter.ComponentChange(meter);
                }

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker1 = shanghaiMTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiMTCircuit.Conductor = shanghaiMTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInFrontCircuit CTInFrontCircuit)
            {
                Meter meter;
                if (CTInFrontCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != CTInFrontCircuit.meter.ComponentType)
                {
                    CTInFrontCircuit.meter = meter;
                }
                else
                {
                    CTInFrontCircuit.meter = CTInFrontCircuit.meter.ComponentChange(meter);
                }

                var breaker = componentFactory.CreatBreaker();
                CTInFrontCircuit.breaker = CTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                CTInFrontCircuit.Conductor = CTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInFrontCircuit MTInFrontCircuit)
            {
                Meter meter;
                if (MTInFrontCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != MTInFrontCircuit.meter.ComponentType)
                {
                    MTInFrontCircuit.meter = meter;
                }
                else
                {
                    MTInFrontCircuit.meter = MTInFrontCircuit.meter.ComponentChange(meter);
                }

                var breaker = componentFactory.CreatBreaker();
                MTInFrontCircuit.breaker = MTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                MTInFrontCircuit.Conductor = MTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInBehindCircuit CTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                CTInBehindCircuit.breaker = CTInBehindCircuit.breaker.ComponentChange(breaker);

                Meter meter;
                if (CTInBehindCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != CTInBehindCircuit.meter.ComponentType)
                {
                    CTInBehindCircuit.meter = meter;
                }
                else
                {
                    CTInBehindCircuit.meter = CTInBehindCircuit.meter.ComponentChange(meter);
                }

                var conductor = componentFactory.CreatConductor();
                CTInBehindCircuit.Conductor = CTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInBehindCircuit MTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                MTInBehindCircuit.breaker = MTInBehindCircuit.breaker.ComponentChange(breaker);

                Meter meter;
                if (MTInBehindCircuit.meter.ComponentType == ComponentType.MT)
                {
                    meter = componentFactory.CreatMeterTransformer();
                }
                else
                {
                    meter = componentFactory.CreatCurrentTransformer();
                }
                if (meter.ComponentType != MTInBehindCircuit.meter.ComponentType)
                {
                    MTInBehindCircuit.meter = meter;
                }
                else
                {
                    MTInBehindCircuit.meter = MTInBehindCircuit.meter.ComponentChange(meter);
                }

                var conductor = componentFactory.CreatConductor();
                MTInBehindCircuit.Conductor = MTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is FireEmergencyLighting fireEmergencyLighting)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetFireEmergencyLighting();
            }
            else if (edge.Details.CircuitForm is Motor_DiscreteComponentsCircuit || edge.Details.CircuitForm is Motor_CPSCircuit || edge.Details.CircuitForm is Motor_DiscreteComponentsStarTriangleStartCircuit || edge.Details.CircuitForm is Motor_CPSStarTriangleStartCircuit)
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Load.FireLoad)
                    {
                        if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
                        }
                        else
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                        }
                    }
                    else
                    {
                        if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
                        }
                        else
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
                        }
                    }
                }
                else
                {
                    if (edge.Target.Load.FireLoad)
                    {
                        if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
                        }
                        else
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                        }
                    }
                    else
                    {
                        if (edge.Target.Details.LoadCalculationInfo.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
                        }
                        else
                        {
                            edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
                        }
                    }
                }
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsYYCircuit || edge.Details.CircuitForm is TwoSpeedMotor_CPSYYCircuit)
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
                }
                else
                {
                    edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
                }
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsDYYCircuit || edge.Details.CircuitForm is TwoSpeedMotor_CPSDYYCircuit)
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsDYYCircuit();
                }
                else
                {
                    edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSDYYCircuit();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 回路元器件级联检查
        /// </summary>
        public static void ComponentCheckCascade(this ThPDSProjectGraphEdge edge)
        {
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
            if (edge.Details.CircuitForm.IsNull())
            {
                //DO NOT
            }
            else if (edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                regularCircuit.breaker = regularCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                regularCircuit.Conductor = regularCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is LeakageCircuit leakageCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                leakageCircuit.breaker = leakageCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                leakageCircuit.Conductor = leakageCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ContactorControlCircuit contactorControlCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                contactorControlCircuit.breaker = contactorControlCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                contactorControlCircuit.Conductor = contactorControlCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ThermalRelayProtectionCircuit thermalRelayCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                thermalRelayCircuit.breaker = thermalRelayCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                thermalRelayCircuit.Conductor = thermalRelayCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiCTCircuit shanghaiCTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker2 = shanghaiCTCircuit.breaker2.ComponentChange(breaker2);

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiCTCircuit.breaker1 = shanghaiCTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiCTCircuit.Conductor = shanghaiCTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiMTCircuit shanghaiMTCircuit)
            {
                var breaker2 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker2 = shanghaiMTCircuit.breaker2.ComponentChange(breaker2);

                var breaker1 = componentFactory.CreatBreaker();
                shanghaiMTCircuit.breaker1 = shanghaiMTCircuit.breaker1.ComponentChange(breaker1);

                var conductor = componentFactory.CreatConductor();
                shanghaiMTCircuit.Conductor = shanghaiMTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInFrontCircuit CTInFrontCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                CTInFrontCircuit.breaker = CTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                CTInFrontCircuit.Conductor = CTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInFrontCircuit MTInFrontCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                MTInFrontCircuit.breaker = MTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                MTInFrontCircuit.Conductor = MTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInBehindCircuit CTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                CTInBehindCircuit.breaker = CTInBehindCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                CTInBehindCircuit.Conductor = CTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInBehindCircuit MTInBehindCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                MTInBehindCircuit.breaker = MTInBehindCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                MTInBehindCircuit.Conductor = MTInBehindCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is FireEmergencyLighting fireEmergencyLighting)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetFireEmergencyLighting();
            }
            else if (edge.Details.CircuitForm is Motor_DiscreteComponentsCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_CPSCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetCPSCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_DiscreteComponentsStarTriangleStartCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetDiscreteComponentsStarTriangleStartCircuit();
            }
            else if (edge.Details.CircuitForm is Motor_CPSStarTriangleStartCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetCPSStarTriangleStartCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsDYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsDYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_DiscreteComponentsDYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_CPSDYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSDYYCircuit();
            }
            else if (edge.Details.CircuitForm is TwoSpeedMotor_CPSYYCircuit)
            {
                edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
