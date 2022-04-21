﻿using Dreambuild.AutoCAD;
using QuikGraph;
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

namespace TianHua.Electrical.PDS.Project
{
    public static class PDSProjectExtend
    {
        /// <summary>
        /// 创建PDSProjectGraph
        /// </summary>
        /// <param name="Graph"></param>
        public static ThPDSProjectGraph CreatPDSProjectGraph(this BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            var ProjectGraph = new ThPDSProjectGraph(graph);
            ProjectGraph.CalculateProjectInfo();
            //ProjectGraph.CalculateSecondaryCircuit();
            return ProjectGraph;
        }

        /// <summary>
        /// 计算项目选型
        /// </summary>
        public static void CalculateProjectInfo(this ThPDSProjectGraph PDSProjectGraph)
        {
            var projectGraph = PDSProjectGraph.Graph;
            var RootNodes = projectGraph.Vertices.Where(x => x.IsStartVertexOfGraph);
            foreach (var rootNode in RootNodes)
            {
                PDSProjectGraph.CalculateProjectInfo(rootNode);
            }
        }

        /// <summary>
        /// 计算项目选型
        /// </summary>
        public static void CalculateProjectInfo(this ThPDSProjectGraph PDSProjectGraph, ThPDSProjectGraphNode node)
        {
            if (node.Details.IsStatistical)
            {
                return;
            }
            var edges = PDSProjectGraph.Graph.OutEdges(node).ToList();
            edges.ForEach(e =>
            {
                PDSProjectGraph.CalculateProjectInfo(e.Target);
                e.ComponentSelection();
            });
            edges.BalancedPhaseSequence();
            node.Details.HighPower =Math.Max(node.Load.InstalledCapacity.HighPower, edges.Select(e => e.Target).ToList().CalculatePower());
            node.CalculateCurrent();
            PDSProjectGraph.CalculateCircuitFormInType(node);
            node.ComponentSelection(edges);
            PDSProjectGraph.CalculateSecondaryCircuit(edges);
            node.Details.IsStatistical = true;
            return;
        }

        /// <summary>
        /// 计算进线回路类型
        /// </summary>
        public static void CalculateCircuitFormInType(this ThPDSProjectGraph PDSProjectGraph, ThPDSProjectGraphNode node)
        {
            if (node.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.DistributionPanel && node.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)
            {
                node.Details.CircuitFormType = new CentralizedPowerCircuit();
            }
            else
            {
                var count = PDSProjectGraph.Graph.InDegree(node);
                if (count == 1)
                {
                    node.Details.CircuitFormType = new OneWayInCircuit();
                }
                else if (count == 2)
                {
                    node.Details.CircuitFormType = new TwoWayInCircuit();
                }
                else if (count == 3)
                {
                    node.Details.CircuitFormType = new ThreeWayInCircuit();
                }
            }
        }

        /// <summary>
        /// 计算电流
        /// </summary>
        public static void CalculateCurrent(this ThPDSProjectGraphNode node)
        {
            //if单相/三相，因为还没有这部分的内容，所有默认所有都是三相
            //单向：I_c=S_c/U_n =P_c/(cos⁡φ×U_n )=(P_n×K_d)/(cos⁡φ×U_n )
            //三相：I_c=S_c/(√3 U_n )=P_c/(cos⁡φ×√3 U_n )=(P_n×K_d)/(cos⁡φ×√3 U_n )
            var Phase = node.Load.Phase;
            if (Phase != ThPDSPhase.一相 && Phase != ThPDSPhase.三相)
            {
                node.Load.CalculateCurrent = 0;
            }
            else
            {
                var DemandFactor = node.Load.DemandFactor;
                if (node.Details.IsOnlyLoad)
                    DemandFactor = 1.0;
                var PowerFactor = node.Load.PowerFactor;
                var KV = Phase == ThPDSPhase.一相 ? 0.22 : 0.38;
                node.Load.CalculateCurrent = Math.Round(node.Details.HighPower * DemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }

        /// <summary>
        /// 计算电流
        /// </summary>
        public static void CalculateCurrent(this MiniBusbar miniBusbar)
        {
            //if单相/三相，因为还没有这部分的内容，所有默认所有都是三相
            //单向：I_c=S_c/U_n =P_c/(cos⁡φ×U_n )=(P_n×K_d)/(cos⁡φ×U_n )
            //三相：I_c=S_c/(√3 U_n )=P_c/(cos⁡φ×√3 U_n )=(P_n×K_d)/(cos⁡φ×√3 U_n )
            var Phase = miniBusbar.Phase;
            if (Phase != ThPDSPhase.一相 && Phase != ThPDSPhase.三相)
            {
                miniBusbar.CalculateCurrent = 0;
            }
            else
            {
                var DemandFactor = miniBusbar.DemandFactor;
                //if (miniBusbar.IsOnlyLoad)
                    //DemandFactor = 1.0;
                var PowerFactor = miniBusbar.PowerFactor;
                var KV = Phase == ThPDSPhase.一相 ? 0.22 : 0.38;
                miniBusbar.CalculateCurrent = Math.Round(miniBusbar.Power * DemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }

        /// <summary>
        /// Node元器件选型/默认选型
        /// </summary>
        public static void ComponentSelection(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> edges)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                //统计节点级联电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    oneWayInCircuit.isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                }
                else if(node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    twoWayInCircuit.isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                }
                else if(node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    threeWayInCircuit.isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch3 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    threeWayInCircuit.transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                }
                else if(node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    centralized.isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }

                //统计节点级联电流
                node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            }
        }

        /// <summary>
        /// Node元器件选型/指定元器件选型
        /// </summary>
        public static PDSBaseComponent ComponentSelection(this ThPDSProjectGraphNode node, Type type)
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
                else if(type.Equals(typeof(AutomaticTransferSwitch)))
                {
                    return componentFactory.CreatAutomaticTransferSwitch();
                }
                else if (type.Equals(typeof(ManualTransferSwitch)))
                {
                    return componentFactory.CreatManualTransferSwitch();
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
        public static T ComponentChange<T>(this T component, T newComponent) where T:PDSBaseComponent
        {
            if(component is IsolatingSwitch isolatingSwitch && newComponent is IsolatingSwitch newIsolatingSwitch)
            {
                if (isolatingSwitch.Model!= newIsolatingSwitch.Model &&  newIsolatingSwitch.GetModels().Contains(isolatingSwitch.Model))
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
            else if(component is TransferSwitch transferSwitch && newComponent is TransferSwitch newTransferSwitch)
            {
                if (transferSwitch.Model != newTransferSwitch.Model &&  newTransferSwitch.GetModels().Contains(transferSwitch.Model))
                {
                    newTransferSwitch.SetModel(transferSwitch.Model);
                }
                if (transferSwitch.PolesNum != newTransferSwitch.PolesNum &&  newTransferSwitch.GetPolesNums().Contains(transferSwitch.PolesNum))
                {
                    newTransferSwitch.SetPolesNum(transferSwitch.PolesNum);
                }
                if (transferSwitch.FrameSpecification != newTransferSwitch.FrameSpecification &&  newTransferSwitch.GetFrameSizes().Contains(transferSwitch.FrameSpecification))
                {
                    newTransferSwitch.SetFrameSize(transferSwitch.FrameSpecification);
                }
                if (transferSwitch.RatedCurrent != newTransferSwitch.RatedCurrent &&  newTransferSwitch.GetRatedCurrents().Contains(transferSwitch.RatedCurrent))
                {
                    newTransferSwitch.SetRatedCurrent(transferSwitch.RatedCurrent);
                }
            }
            else if(component is Breaker breaker && newComponent is Breaker newBreaker)
            {
                newBreaker.SetBreakerType(breaker.ComponentType);
                if (newBreaker.Model != breaker.Model &&  newBreaker.GetModels().Contains(breaker.Model))
                {
                    newBreaker.SetModel(breaker.Model);
                }
                if (newBreaker.PolesNum  != breaker.PolesNum &&  newBreaker.GetPolesNums().Contains(breaker.PolesNum))
                {
                    newBreaker.SetPolesNum(breaker.PolesNum);
                }
                if (newBreaker.FrameSpecification  != breaker.FrameSpecification &&  newBreaker.GetFrameSpecifications().Contains(breaker.FrameSpecification))
                {
                    newBreaker.SetFrameSpecification(breaker.FrameSpecification);
                }
                if (newBreaker.TripUnitType  != breaker.TripUnitType &&  newBreaker.GetTripDevices().Contains(breaker.TripUnitType))
                {
                    newBreaker.SetTripDevice(breaker.TripUnitType);
                }
                if (newBreaker.RatedCurrent  != breaker.RatedCurrent &&  newBreaker.GetRatedCurrents().Contains(breaker.RatedCurrent))
                {
                    newBreaker.SetRatedCurrent(breaker.TripUnitType);
                }
            }
            else if(component is ThermalRelay thermalRelay && newComponent is ThermalRelay newThermalRelay)
            {
                //do not
                //热继电器没有选型范围，固定选型
            }
            else if (component is Contactor contactor && newComponent is Contactor newContactor)
            {
                if (newContactor.Model  != contactor.Model &&  newContactor.GetModels().Contains(contactor.Model))
                {
                    newContactor.SetModel(contactor.Model);
                }
                if (newContactor.PolesNum  != contactor.PolesNum &&  newContactor.GetPolesNums().Contains(contactor.PolesNum))
                {
                    newContactor.SetPolesNum(contactor.PolesNum);
                }
                if (newContactor.RatedCurrent  != contactor.RatedCurrent &&  newContactor.GetRatedCurrents().Contains(contactor.RatedCurrent))
                {
                    newContactor.SetRatedCurrent(contactor.RatedCurrent);
                }
            }
            else if(component is Conductor conductor && newComponent is Conductor newConductor)
            {
                if (newConductor.NumberOfPhaseWire  != conductor.NumberOfPhaseWire &&  newConductor.GetNumberOfPhaseWires().Contains(conductor.NumberOfPhaseWire))
                {
                    newConductor.SetNumberOfPhaseWire(conductor.NumberOfPhaseWire);
                }
                if (newConductor.ConductorCrossSectionalArea  != conductor.ConductorCrossSectionalArea &&  newConductor.GetConductorCrossSectionalAreas().Contains(conductor.ConductorCrossSectionalArea))
                {
                    newConductor.SetConductorCrossSectionalArea(conductor.ConductorCrossSectionalArea);
                }
            }
            else if (component is CPS cps && newComponent is CPS newCPS)
            {
                if (newCPS.Model  != cps.Model &&  newCPS.GetModels().Contains(cps.Model))
                {
                    newCPS.SetModel(cps.Model);
                }
                if (newCPS.PolesNum  != cps.PolesNum &&  newCPS.GetPolesNums().Contains(cps.PolesNum))
                {
                    newCPS.SetPolesNum(cps.PolesNum);
                }
                if (newCPS.RatedCurrent  != cps.RatedCurrent &&  newCPS.GetRatedCurrents().Contains(cps.RatedCurrent))
                {
                    newCPS.SetRatedCurrent(cps.RatedCurrent);
                }
                if (newCPS.Combination  != cps.Combination &&  newCPS.GetCombinations().Contains(cps.Combination))
                {
                    newCPS.SetCombination(cps.Combination);
                }
                if (newCPS.CodeLevel  != cps.CodeLevel &&  newCPS.GetCodeLevels().Contains(cps.CodeLevel))
                {
                    newCPS.SetCodeLevel(cps.CodeLevel);
                }
                if (newCPS.ResidualCurrent  != cps.ResidualCurrent &&  newCPS.GetResidualCurrents().Contains(cps.ResidualCurrent))
                {
                    newCPS.SetResidualCurrent(cps.ResidualCurrent);
                }
            }
            else if(component is Meter meter && newComponent is Meter newMeter)
            {
                if (newMeter.MeterParameter  != meter.MeterParameter &&  newMeter.GetParameters().Contains(meter.MeterParameter))
                {
                    newMeter.SetParameters(meter.MeterParameter);
                }
            }
            else if (component is OUVP oucp && newComponent is OUVP newOUVP)
            {
                if (newOUVP.Model  != oucp.Model &&  newOUVP.GetModels().Contains(oucp.Model))
                {
                    newOUVP.SetModel(oucp.Model);
                }
                if (newOUVP.PolesNum  != oucp.PolesNum &&  newOUVP.GetPolesNums().Contains(oucp.PolesNum))
                {
                    newOUVP.SetPolesNum(oucp.PolesNum);
                }
                if (newOUVP.RatedCurrent  != oucp.RatedCurrent &&  newOUVP.GetRatedCurrents().Contains(oucp.RatedCurrent))
                {
                    newOUVP.SetRatedCurrent(oucp.RatedCurrent);
                }
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
            if (edge.Target.Type == PDSNodeType.None)
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
                    breaker1 = componentFactory.CreatBreaker(),
                    meter = componentFactory.CreatCurrentTransformer(),
                    breaker2 = componentFactory.CreatBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket || (new List<string>() { "E-BDB111", "E-BDB112", "E-BDB114", "E-BDB131" }.Contains(edge.Target.Load.ID.BlockName) && edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.LumpedLoad))
            {
                //漏电
                edge.Details.CircuitForm = new LeakageCircuit()
                {
                    breaker= componentFactory.CreatResidualCurrentBreaker(),
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLuminaire)
            {
                //消防应急照明回路
                edge.Details.CircuitForm = new FireEmergencyLighting()
                {
                    Conductor = componentFactory.CreatConductor(),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                SpecifyComponentFactory specifyComponentFactory = new SpecifyComponentFactory(edge);
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Details.IsDualPower)
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorDiscreteComponentsYYCircuit();
                    }
                    else
                    {
                        if (edge.Target.Details.HighPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
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
                    if (edge.Target.Details.IsDualPower)
                    {
                        edge.Details.CircuitForm = specifyComponentFactory.GetTwoSpeedMotorCPSYYCircuit();
                    }
                    else
                    {
                        if (edge.Target.Details.HighPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
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
        public static void CalculateSecondaryCircuit(this ThPDSProjectGraph PDSProjectGraph)
        {
            var projectGraph = PDSProjectGraph.Graph;
            foreach (ThPDSProjectGraphEdge edge in projectGraph.Edges)
            {
                if(edge.Target.Load.LoadTypeCat_3 != ThPDSLoadTypeCat_3.None)
                {
                    var secondaryCircuitInfos = SecondaryCircuitConfiguration.SecondaryCircuitConfigs[edge.Target.Load.LoadTypeCat_3.ToString()];
                    foreach (SecondaryCircuitInfo item in secondaryCircuitInfos)
                    {
                        ThPDSProjectGraphService.AddControlCircuit(projectGraph ,edge, item);
                    }
                }
            }
        }

        /// <summary>
        /// 计算控制回路
        /// </summary>
        public static void CalculateSecondaryCircuit(this ThPDSProjectGraph PDSProjectGraph, List<ThPDSProjectGraphEdge> edges)
        {
            var projectGraph = PDSProjectGraph.Graph;
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
                    var secondaryCircuitInfos = SecondaryCircuitConfiguration.SecondaryCircuitConfigs[edge.Target.Load.LoadTypeCat_3.ToString()];
                    foreach (SecondaryCircuitInfo item in secondaryCircuitInfos)
                    {
                        ThPDSProjectGraphService.AddControlCircuit(projectGraph, edge, item);
                    }
                }
            }
        }

        /// <summary>
        /// 回路元器件选型/指定元器件选型
        /// </summary>
        /// <returns></returns>
        public static PDSBaseComponent ComponentSelection(this ThPDSProjectGraphEdge edge, Type type, CircuitFormOutType circuitFormOutType)
        {
            if (type.IsSubclassOf(typeof(PDSBaseComponent)))
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
                if (type.Equals(typeof(Breaker)))
                {
                    if(circuitFormOutType == CircuitFormOutType.漏电)
                        return componentFactory.CreatResidualCurrentBreaker();
                    else
                        return componentFactory.CreatBreaker();
                }
                else if (type.Equals(typeof(ThermalRelay)))
                {
                    return componentFactory.CreatThermalRelay();
                }
                else if (type.Equals(typeof(Contactor)))
                {
                    return componentFactory.CreatContactor();
                }
                else if(type.Equals(typeof(Meter)))
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
                            breaker= componentFactory.CreatResidualCurrentBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        edge.Details.CircuitForm = new ContactorControlCircuit()
                        {
                            breaker= componentFactory.CreatBreaker(),
                            contactor = componentFactory.CreatContactor(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        edge.Details.CircuitForm = new ThermalRelayProtectionCircuit()
                        {
                            breaker= componentFactory.CreatBreaker(),
                            thermalRelay = componentFactory.CreatThermalRelay(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_上海CT:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                        {
                            breaker1 = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatCurrentTransformer(),
                            breaker2 = componentFactory.CreatBreaker(),
                            Conductor = componentFactory.CreatConductor(),
                        };
                        break;
                    }
                case CircuitFormOutType.配电计量_上海直接表:
                    {
                        edge.Details.CircuitForm = new DistributionMetering_ShanghaiMTCircuit()
                        {
                            breaker1 = componentFactory.CreatBreaker(),
                            meter = componentFactory.CreatMeterTransformer(),
                            breaker2 = componentFactory.CreatBreaker(),
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
                            Conductor= componentFactory.CreatConductor(),
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
                        edge.Details.CircuitForm = new FireEmergencyLighting()
                        {
                            Conductor = componentFactory.CreatConductor()
                        };
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

        public static double CalculatePower(this List<ThPDSProjectGraphNode> nodes)
        {
            double power = 0;
            double L1Power =0, L2Power =0, L3Power = 0;
            nodes.ForEach(node =>
            {
                switch(node.Details.PhaseSequence)
                {
                    case PhaseSequence.L1:
                    {
                            L1Power += node.Details.HighPower;
                            break;
                    }
                    case PhaseSequence.L2:
                        {
                            L2Power += node.Details.HighPower;
                            break;
                        }
                    case PhaseSequence.L3:
                        {
                            L3Power += node.Details.HighPower;
                            break;
                        }
                    case PhaseSequence.L123:
                        {
                            power += node.Details.HighPower;
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            });
            return power + 3 * Math.Max(Math.Max(L1Power, L2Power),L3Power);
        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this List<ThPDSProjectGraphEdge> edges)
        {
            var onePhase = new List<ThPDSProjectGraphNode>();
            foreach (var edge in edges)
            {
                if (edge.Target.Load.Phase == ThPDSPhase.三相)
                {
                    edge.Target.Details.PhaseSequence = PhaseSequence.L123;
                }
                else if (edge.Target.Load.Phase == ThPDSPhase.一相)
                {
                    if (edge.Target.Details.HighPower <= 0)
                    {
                        edge.Target.Details.PhaseSequence = PhaseSequence.L1;
                    }
                    else
                    {
                        onePhase.Add(edge.Target);
                    }
                }
            }
            if (onePhase.Count > 0)
                onePhase.BalancedPhaseSequence();
        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this List<ThPDSProjectGraphNode> nodes)
        {
            if(nodes.Count == 1)
            {
                nodes[0].Details.PhaseSequence = PhaseSequence.L1;
            }
            else if(nodes.Count == 2)
            {
                nodes[0].Details.PhaseSequence = PhaseSequence.L1;
                nodes[1].Details.PhaseSequence = PhaseSequence.L2;
            }
            else if (nodes.Count == 3)
            {
                nodes[0].Details.PhaseSequence = PhaseSequence.L1;
                nodes[1].Details.PhaseSequence = PhaseSequence.L2;
                nodes[2].Details.PhaseSequence = PhaseSequence.L3;
            }
            else
            {
                var powerSum = nodes.Sum(x => x.Details.HighPower);
                var averagePower = powerSum/3;
                var HighPowerNodes = nodes.Where(o => o.Details.HighPower > averagePower).ToList();
                if(HighPowerNodes.Count == 2)
                {
                    HighPowerNodes[0].Details.PhaseSequence = PhaseSequence.L2;
                    HighPowerNodes[1].Details.PhaseSequence = PhaseSequence.L3;
                    nodes.Except(HighPowerNodes).ForEach(o => o.Details.PhaseSequence = PhaseSequence.L1);
                }
                else if(HighPowerNodes.Count == 1)
                {
                    HighPowerNodes[0].Details.PhaseSequence = PhaseSequence.L3;
                    nodes.Remove(HighPowerNodes[0]);
                    var L1Nodes = nodes.ChoisePhaseSequence(averagePower);
                    L1Nodes.ForEach(o => o.Details.PhaseSequence = PhaseSequence.L1);
                    nodes.Except(L1Nodes).ForEach(o => o.Details.PhaseSequence = PhaseSequence.L2);
                }
                else
                {
                    var L1Nodes = nodes.ChoisePhaseSequence(averagePower);
                    L1Nodes.ForEach(o => o.Details.PhaseSequence = PhaseSequence.L1);
                    var L23 = nodes.Except(L1Nodes).ToList();
                    var L2Nodes = L23.ChoisePhaseSequence(averagePower);
                    L2Nodes.ForEach(o => o.Details.PhaseSequence = PhaseSequence.L2);
                    L23.Except(L2Nodes).ForEach(o => o.Details.PhaseSequence = PhaseSequence.L3);
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
        public static List<ThPDSProjectGraphNode> ChoisePhaseSequence(this List<ThPDSProjectGraphNode> nodes,double target)
        {
            List<ThPDSProjectGraphNode> result = new List<ThPDSProjectGraphNode>();
            var target_int = (int)Math.Ceiling(target);
            int multiple = (int)Math.Ceiling(target / 100);
            int[] vs = new int[nodes.Count];
            for(int i = 0; i < vs.Length; i++)
            {
                vs[i] = (int)Math.Ceiling(nodes[i].Details.HighPower/multiple);
            }
            var capacity = (int)(target_int/multiple);
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
            while(x > 0 && y > 0)
            {
                if(dp[x,y] != dp[x - 1 ,y])
                {
                    result.Add(nodes[x-1]);
                    y -= vs[x - 1];
                    x --;
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
        public static void UpdateWithNode(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node, bool  IsPhaseSequenceChange)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            if (IsPhaseSequenceChange)
            {
                node.Details.HighPower = edges.Select(e => e.Target).ToList().CalculatePower();
            }
            node.CalculateCurrent();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.ComponentCheck(CascadeCurrent);

            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = graph.Graph.InEdges(node).ToList();
            edges.ForEach(e => graph.CheckWithEdge(e));
        }

        /// <summary>
        /// 修改小母排
        /// </summary>
        /// <param name="graph">图</param>
        /// <param name="node">节点</param>
        /// <param name="IsPhaseSequenceChange">是否改变相序</param>
        public static void UpdateWithMiniBusbar(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node, MiniBusbar miniBusbar, bool IsPhaseSequenceChange)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            if (IsPhaseSequenceChange)
            {
                miniBusbar.Power = edges.Select(e => e.Target).ToList().CalculatePower();
            }
            miniBusbar.CalculateCurrent();

            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            node.ComponentCheck(miniBusbar, CascadeCurrent);

            miniBusbar.CascadeCurrent = Math.Max(CascadeCurrent, miniBusbar.GetCascadeCurrent());
            graph.CheckCascadeWithNode(node);
        }

        /// <summary>
        /// 检查节点
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public static void CheckWithNode(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            node.Details.HighPower = Math.Max(node.Details.HighPower, edges.Select(e => e.Target).ToList().CalculatePower());
            node.CalculateCurrent();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.ComponentCheck(CascadeCurrent);

            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = graph.Graph.InEdges(node).ToList();
            edges.ForEach(e => graph.CheckWithEdge(e));
        }

        /// <summary>
        /// 检查小母排
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="miniBusbar"></param>
        public static void CheckWithMiniBusbar(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node , MiniBusbar miniBusbar)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            miniBusbar.Power = Math.Max(miniBusbar.Power, edges.Select(e => e.Target).ToList().CalculatePower());
            miniBusbar.CalculateCurrent();

            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            node.ComponentCheck(miniBusbar , CascadeCurrent);

            miniBusbar.CascadeCurrent = Math.Max(CascadeCurrent, miniBusbar.GetCascadeCurrent());
        }

        /// <summary>
        /// 检查回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void CheckWithEdge(this ThPDSProjectGraph graph, ThPDSProjectGraphEdge edge)
        {
            edge.ComponentCheck();
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());

            var node = edge.Source;
            var miniBusbar = node.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge)).Key;
            if(miniBusbar.IsNull())
            {
                graph.CheckWithNode(edge.Source);
            }
            else
            {
                graph.CheckWithMiniBusbar(node ,miniBusbar);
                graph.CheckWithNode(node);
            }
        }

        /// <summary>
        /// 检查节点级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public static void CheckCascadeWithNode(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);

            node.ComponentCheckCascade(CascadeCurrent);

            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = graph.Graph.InEdges(node).ToList();
            edges.ForEach(e => graph.CheckCascadeWithEdge(e));
        }

        /// <summary>
        /// 检查小母排级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="miniBusbar"></param>
        public static void CheckCascadeWithMiniBusbar(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node, MiniBusbar miniBusbar)
        {
            var edges = node.Details.MiniBusbars[miniBusbar];
            miniBusbar.Power = Math.Max(miniBusbar.Power, edges.Select(e => e.Target).ToList().CalculatePower());
            miniBusbar.CalculateCurrent();

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
        public static void CheckCascadeWithEdge(this ThPDSProjectGraph graph, ThPDSProjectGraphEdge edge)
        {
            edge.ComponentCheckCascade();
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());

            var node = edge.Source;
            var miniBusbar = node.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge)).Key;
            if (miniBusbar.IsNull())
            {
                graph.CheckCascadeWithNode(edge.Source);
            }
            else
            {
                graph.CheckCascadeWithMiniBusbar(node, miniBusbar);
                graph.CheckCascadeWithNode(node);
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
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    oneWayInCircuit.isolatingSwitch = oneWayInCircuit.isolatingSwitch.ComponentChange(isolatingSwitch);
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    var isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.isolatingSwitch1 =twoWayInCircuit.isolatingSwitch1.ComponentChange(isolatingSwitch1);

                    var isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.isolatingSwitch2 =twoWayInCircuit.isolatingSwitch2.ComponentChange(isolatingSwitch2);

                    TransferSwitch transferSwitch;
                    if (twoWayInCircuit.transferSwitch is ManualTransferSwitch MTSE)
                    {
                        transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch = componentFactory.CreatManualTransferSwitch();
                    }
                    twoWayInCircuit.transferSwitch =twoWayInCircuit.transferSwitch.ComponentChange(transferSwitch);
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    var isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch1 = threeWayInCircuit.isolatingSwitch1.ComponentChange(isolatingSwitch1);

                    var isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch2 = threeWayInCircuit.isolatingSwitch2.ComponentChange(isolatingSwitch2);

                    var isolatingSwitch3 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch3 = threeWayInCircuit.isolatingSwitch3.ComponentChange(isolatingSwitch3);

                    TransferSwitch transferSwitch1;
                    if (threeWayInCircuit.transferSwitch1 is ManualTransferSwitch)
                    {
                        transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch1 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch1 =threeWayInCircuit.transferSwitch1.ComponentChange(transferSwitch1);

                    TransferSwitch transferSwitch2;
                    if (threeWayInCircuit.transferSwitch2 is ManualTransferSwitch)
                    {
                        transferSwitch2 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch2 =threeWayInCircuit.transferSwitch2.ComponentChange(transferSwitch2);
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    centralized.isolatingSwitch = centralized.isolatingSwitch.ComponentChange(isolatingSwitch);
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
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    oneWayInCircuit.isolatingSwitch = oneWayInCircuit.isolatingSwitch.ComponentChange(isolatingSwitch);
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    var isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.isolatingSwitch1 =twoWayInCircuit.isolatingSwitch1.ComponentChange(isolatingSwitch1);

                    var isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    twoWayInCircuit.isolatingSwitch2 =twoWayInCircuit.isolatingSwitch2.ComponentChange(isolatingSwitch2);

                    TransferSwitch transferSwitch;
                    if (twoWayInCircuit.transferSwitch is ManualTransferSwitch MTSE)
                    {
                        transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch = componentFactory.CreatManualTransferSwitch();
                    }
                    twoWayInCircuit.transferSwitch =twoWayInCircuit.transferSwitch.ComponentChange(transferSwitch);
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    var isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch1 = threeWayInCircuit.isolatingSwitch1.ComponentChange(isolatingSwitch1);

                    var isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch2 = threeWayInCircuit.isolatingSwitch2.ComponentChange(isolatingSwitch2);

                    var isolatingSwitch3 = componentFactory.CreatIsolatingSwitch();
                    threeWayInCircuit.isolatingSwitch3 = threeWayInCircuit.isolatingSwitch3.ComponentChange(isolatingSwitch3);

                    TransferSwitch transferSwitch1;
                    if (threeWayInCircuit.transferSwitch1 is ManualTransferSwitch)
                    {
                        transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch1 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch1 =threeWayInCircuit.transferSwitch1.ComponentChange(transferSwitch1);

                    TransferSwitch transferSwitch2;
                    if (threeWayInCircuit.transferSwitch2 is ManualTransferSwitch)
                    {
                        transferSwitch2 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    else
                    {
                        transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                    }
                    threeWayInCircuit.transferSwitch2 =threeWayInCircuit.transferSwitch2.ComponentChange(transferSwitch2);
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    var isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    centralized.isolatingSwitch = centralized.isolatingSwitch.ComponentChange(isolatingSwitch);
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
        public static void ComponentCheck(this ThPDSProjectGraphNode node , MiniBusbar miniBusbar, double cascadeCurrent)
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
            if(edge.Details.CircuitForm is RegularCircuit regularCircuit)
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
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                contactorControlCircuit.breaker = contactorControlCircuit.breaker.ComponentChange(breaker);

                var contacter = componentFactory.CreatContactor();
                contactorControlCircuit.contactor = contactorControlCircuit.contactor.ComponentChange(contacter);

                var conductor = componentFactory.CreatConductor();
                contactorControlCircuit.Conductor = contactorControlCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is ThermalRelayProtectionCircuit thermalRelayCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                thermalRelayCircuit.breaker = thermalRelayCircuit.breaker.ComponentChange(breaker);

                var thermalRelay = componentFactory.CreatThermalRelay();
                thermalRelayCircuit.thermalRelay = thermalRelayCircuit.thermalRelay.ComponentChange(thermalRelay);

                var conductor = componentFactory.CreatConductor();
                thermalRelayCircuit.Conductor = thermalRelayCircuit.Conductor.ComponentChange(conductor);
            }
            else if(edge.Details.CircuitForm is DistributionMetering_ShanghaiCTCircuit shanghaiCTCircuit)
            {
                var breaker1 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiCTCircuit.breaker1 = shanghaiCTCircuit.breaker1.ComponentChange(breaker1);

                Meter meter;
                if(shanghaiCTCircuit.meter.ComponentType == ComponentType.MT)
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

                var breaker2 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiCTCircuit.breaker2 = shanghaiCTCircuit.breaker2.ComponentChange(breaker2);

                var conductor = componentFactory.CreatConductor();
                shanghaiCTCircuit.Conductor = shanghaiCTCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiMTCircuit shanghaiMTCircuit)
            {
                var breaker1 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiMTCircuit.breaker1 = shanghaiMTCircuit.breaker1.ComponentChange(breaker1);

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

                var breaker2 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiMTCircuit.breaker2 = shanghaiMTCircuit.breaker2.ComponentChange(breaker2);

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

                var breaker = componentFactory.CreatResidualCurrentBreaker();
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

                var breaker = componentFactory.CreatResidualCurrentBreaker();
                MTInFrontCircuit.breaker = MTInFrontCircuit.breaker.ComponentChange(breaker);

                var conductor = componentFactory.CreatConductor();
                MTInFrontCircuit.Conductor = MTInFrontCircuit.Conductor.ComponentChange(conductor);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInBehindCircuit CTInBehindCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
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
                var breaker = componentFactory.CreatResidualCurrentBreaker();
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
            else if(edge.Details.CircuitForm is FireEmergencyLighting fireEmergencyLighting)
            {
                var conductor = componentFactory.CreatConductor();
                fireEmergencyLighting.Conductor = fireEmergencyLighting.Conductor.ComponentChange(conductor);
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
                //框架已搭好，后续补充完成
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
            if (edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                var breaker = componentFactory.CreatBreaker();
                regularCircuit.breaker = regularCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is LeakageCircuit leakageCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                leakageCircuit.breaker = leakageCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is ContactorControlCircuit contactorControlCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                contactorControlCircuit.breaker = contactorControlCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is ThermalRelayProtectionCircuit thermalRelayCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                thermalRelayCircuit.breaker = thermalRelayCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiCTCircuit shanghaiCTCircuit)
            {
                var breaker1 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiCTCircuit.breaker1 = shanghaiCTCircuit.breaker1.ComponentChange(breaker1);

                var breaker2 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiCTCircuit.breaker2 = shanghaiCTCircuit.breaker2.ComponentChange(breaker2);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_ShanghaiMTCircuit shanghaiMTCircuit)
            {
                var breaker1 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiMTCircuit.breaker1 = shanghaiMTCircuit.breaker1.ComponentChange(breaker1);

                var breaker2 = componentFactory.CreatResidualCurrentBreaker();
                shanghaiMTCircuit.breaker2 = shanghaiMTCircuit.breaker2.ComponentChange(breaker2);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInFrontCircuit CTInFrontCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                CTInFrontCircuit.breaker = CTInFrontCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInFrontCircuit MTInFrontCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                MTInFrontCircuit.breaker = MTInFrontCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_CTInBehindCircuit CTInBehindCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                CTInBehindCircuit.breaker = CTInBehindCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is DistributionMetering_MTInBehindCircuit MTInBehindCircuit)
            {
                var breaker = componentFactory.CreatResidualCurrentBreaker();
                MTInBehindCircuit.breaker = MTInBehindCircuit.breaker.ComponentChange(breaker);
            }
            else if (edge.Details.CircuitForm is FireEmergencyLighting fireEmergencyLighting)
            {
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
