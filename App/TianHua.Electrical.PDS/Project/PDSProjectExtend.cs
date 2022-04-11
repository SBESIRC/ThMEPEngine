using Dreambuild.AutoCAD;
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
            ProjectGraph.CalculateSecondaryCircuit();
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
            if (node.Details.LowPower <= 0)
            {
                edges.BalancedPhaseSequence();
                node.Details.LowPower = edges.Select(e => e.Target).ToList().CalculatePower();
            }
            node.CalculateCurrent();
            PDSProjectGraph.CalculateCircuitFormInType(node);
            node.ComponentSelection(edges);
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
                node.Load.CalculateCurrent = Math.Round(node.Details.LowPower * DemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }

        /// <summary>
        /// Node元器件选型/默认选型
        /// </summary>
        public static void ComponentSelection(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> edges)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
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
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;//额定级联电流
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
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
                if (type.Equals(typeof(Meter)))
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
                    breaker= componentFactory.CreatBreaker(),
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
                        if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
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
                        if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
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
                    var secondaryCircuitInfos = SecondaryCircuitConfiguration.SecondaryCircuitInfos[edge.Target.Load.LoadTypeCat_3.ToString()];
                    foreach (SecondaryCircuitInfo item in secondaryCircuitInfos)
                    {
                        int index = edge.Source.Details.SecondaryCircuits.Count+ 1;
                        var secondaryCircuit = new SecondaryCircuit(index);
                        secondaryCircuit.CircuitDescription = item.Description;
                        secondaryCircuit.conductor = new Conductor(item.Conductor,item.ConductorCategory , edge.Target.Load.Phase, edge.Target.Load.CircuitType,  edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber);
                        edge.Source.Details.SecondaryCircuits.Add(secondaryCircuit, new List<ThPDSProjectGraphEdge>() { });

                        ThPDSProjectGraphService.AssignCircuit2ControlCircuit(edge.Source, secondaryCircuit, edge);
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
                        };
                        break;
                    }
                case CircuitFormOutType.漏电:
                    {
                        edge.Details.CircuitForm = new LeakageCircuit()
                        {
                            breaker= componentFactory.CreatResidualCurrentBreaker(),
                        };
                        break;
                    }
                case CircuitFormOutType.接触器控制:
                    {
                        edge.Details.CircuitForm = new ContactorControlCircuit()
                        {
                            breaker= componentFactory.CreatBreaker(),
                            contactor = componentFactory.CreatContactor(),
                        };
                        break;
                    }
                case CircuitFormOutType.热继电器保护:
                    {
                        edge.Details.CircuitForm = new ThermalRelayProtectionCircuit()
                        {
                            breaker= componentFactory.CreatBreaker(),
                            thermalRelay = componentFactory.CreatThermalRelay(),
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
                            L1Power += node.Details.LowPower;
                            break;
                    }
                    case PhaseSequence.L2:
                        {
                            L2Power += node.Details.LowPower;
                            break;
                        }
                    case PhaseSequence.L3:
                        {
                            L3Power += node.Details.LowPower;
                            break;
                        }
                    case PhaseSequence.L123:
                        {
                            power += node.Details.LowPower;
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
                    if (edge.Target.Details.LowPower <= 0)
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
                var powerSum = nodes.Sum(x => x.Details.LowPower);
                var averagePower = powerSum/3;
                var HighPowerNodes = nodes.Where(o => o.Details.LowPower > averagePower).ToList();
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
                vs[i] = (int)Math.Ceiling(nodes[i].Details.LowPower/multiple);
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
        /// 兼容图
        /// </summary>
        /// <param name="Graph"></param>
        /// <param name="NewGraph"></param>
        public static void Compatible(this ThPDSProjectGraph Graph, BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> NewGraph)
        {
            //暂时不考虑
        }

        public static void UpdateWithNode(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node , bool permission = true)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            if (permission)
            {
                node.Details.LowPower = edges.Select(e => e.Target).ToList().CalculatePower();
            }
            node.CalculateCurrent();
            if (permission)
            {
                node.ComponentCheck(edges);
            }
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.SmallBusbars.Count > 0 ? node.Details.SmallBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = graph.Graph.InEdges(node).ToList();
            edges.ForEach(e => graph.UpdateWithEdge(e));
        }

        public static void UpdateWithEdge(this ThPDSProjectGraph graph, ThPDSProjectGraphEdge edge, bool permission = true)
        {
            if (permission)
            {
                edge.ComponentCheck();
            }
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(edge.Details.CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
            graph.UpdateWithNode(edge.Source);
        }

        /// <summary>
        /// Node元器件选型检查
        /// </summary>
        public static void ComponentCheck(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge> edges)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    if(oneWayInCircuit.isolatingSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        oneWayInCircuit.isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    }
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    if (twoWayInCircuit.isolatingSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    }
                    if (twoWayInCircuit.isolatingSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    }
                    if (twoWayInCircuit.transferSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.transferSwitch = componentFactory.CreatAutomaticTransferSwitch();
                    }
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    if (threeWayInCircuit.isolatingSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch1 = componentFactory.CreatIsolatingSwitch();
                    }
                    if (threeWayInCircuit.isolatingSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch2 = componentFactory.CreatIsolatingSwitch();
                    }
                    if (threeWayInCircuit.transferSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch();
                    }
                    if (threeWayInCircuit.isolatingSwitch3.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch3 = componentFactory.CreatIsolatingSwitch();
                    }
                    if (threeWayInCircuit.transferSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.transferSwitch2 = componentFactory.CreatManualTransferSwitch();
                    }
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    if (centralized.isolatingSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        centralized.isolatingSwitch = componentFactory.CreatIsolatingSwitch();
                    }
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 回路元器件选型检查
        /// </summary>
        public static void ComponentCheck(this ThPDSProjectGraphEdge edge)
        {
            edge.Details = new CircuitDetails();
            var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            var CascadeCurrent = edge.Target.Details.CascadeCurrent;
            if(edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                if(regularCircuit.breaker.GetCascadeRatedCurrent() <= CascadeCurrent)
                {
                    regularCircuit.breaker = new Breaker(CascadeCurrent, new List<string>() { regularCircuit.breaker.TripUnitType }, regularCircuit.breaker.PolesNum, regularCircuit.breaker.TripUnitType, false, false) ;
                }
            }
        }
    }
}
