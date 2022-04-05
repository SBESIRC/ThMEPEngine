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
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.Project
{
    public static class PDSProjectExtend
    {
        /// <summary>
        /// 创建PDSProjectGraph
        /// </summary>
        /// <param name="Graph"></param>
        public static ThPDSProjectGraph CreatPDSProjectGraph(this AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            var ProjectGraph = new ThPDSProjectGraph(graph);
            ProjectGraph.CalculateProjectInfo();
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
            var edges = PDSProjectGraph.Graph.Edges.Where(e => e.Source.Equals(node)).ToList();
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
                var count = PDSProjectGraph.Graph.Edges.Count(o => o.Target.Equals(node));
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
        /// Node元器件选型
        /// </summary>
        public static void ComponentSelection(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> edges)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                var MaxCalculateCurrent = Math.Max(CalculateCurrent, CascadeCurrent);//进线回路暂时没有需要级联的元器件
                var PolesNum = "3P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
                var SpecialPolesNum = "4P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
                if (node.Load.Phase == ThPDSPhase.一相)
                {
                    PolesNum = "1P";
                    //当相数为1时，若负载类型不为“Outdoor Lights”，且断路器不是ATSE前的主进线开关，则断路器选择1P；
                    //当相数为1时，若负载类型为“Outdoor Lights”，或断路器是ATSE前的主进线开关，则断路器选择2P；
                    if (node.Load.LoadTypeCat_2 != ThPDSLoadTypeCat_2.OutdoorLights && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                    {
                        SpecialPolesNum = "1P";
                    }
                    else
                    {
                        SpecialPolesNum = "2P";
                    }
                }
                else if (node.Load.Phase == ThPDSPhase.三相)
                {
                    if (node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                    {
                        SpecialPolesNum = "3P";
                    }
                }
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    oneWayInCircuit.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                }
                else if(node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    twoWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                    twoWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                    twoWayInCircuit.transferSwitch = new AutomaticTransferSwitch(CalculateCurrent, PolesNum);
                }
                else if(node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    threeWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                    threeWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                    threeWayInCircuit.isolatingSwitch3 = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
                    threeWayInCircuit.transferSwitch1 = new AutomaticTransferSwitch(CalculateCurrent, PolesNum);
                    threeWayInCircuit.transferSwitch2 = new ManualTransferSwitch(CalculateCurrent, PolesNum);
                }
                else if(node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    centralized.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, SpecialPolesNum);
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
        /// 回路元器件选型/默认选型
        /// </summary>
        /// <param name="pDSCircuit"></param>
        /// <returns></returns>
        public static void ComponentSelection(this ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            edge.Details = new CircuitDetails();
            var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            var CascadeCurrent = edge.Target.Details.CascadeCurrent;
            var MaxCalculateCurrent = Math.Max(CalculateCurrent, CascadeCurrent);
            var PolesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            var SpecialPolesNum = "3P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (edge.Target.Load.Phase == ThPDSPhase.一相)
            {
                PolesNum = "1P";
                if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.OutdoorLights)
                {
                    SpecialPolesNum = "1P";
                }
                else
                {
                    SpecialPolesNum = "2P";
                }
            }
            var Characteristics = "";//瞬时脱扣器类型
            var TripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out Characteristics);//脱扣器类型
            if (edge.Target.Type == PDSNodeType.None)
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent,edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray,edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        edge.Details.CircuitForm = new Motor_DiscreteComponentsCircuit()
                        {
                            breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                            contactor = new Contactor(CalculateCurrent, PolesNum),
                            thermalRelay = new ThermalRelay(CalculateCurrent),
                            Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                        };
                    }
                    else
                    {
                        edge.Details.CircuitForm = new Motor_DiscreteComponentsStarTriangleStartCircuit()
                        {
                            breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                            contactor1 = new Contactor(CalculateCurrent, PolesNum),
                            thermalRelay = new ThermalRelay(CalculateCurrent),
                            contactor2 = new Contactor(CalculateCurrent, PolesNum),
                            contactor3 = new Contactor(CalculateCurrent, PolesNum),
                            Conductor1 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                            Conductor2 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                        };
                    }
                }
                else
                {
                    if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        edge.Details.CircuitForm = new Motor_CPSCircuit()
                        {
                            cps = new CPS(CalculateCurrent),
                            Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                        };
                    }
                    else
                    {
                        edge.Details.CircuitForm = new Motor_CPSStarTriangleStartCircuit()
                        {
                            cps = new CPS(CalculateCurrent),
                            contactor1 = new Contactor(CalculateCurrent, PolesNum),
                            contactor2 = new Contactor(CalculateCurrent, PolesNum),
                            Conductor1 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                            Conductor2 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                        };
                    }
                }
            }
            else if (edge.Target.Load.ID.BlockName == "E-BDB006-1" && edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
            {
                edge.Details.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                {
                    breaker1 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    meter = new CurrentTransformer(CalculateCurrent, PolesNum),
                    breaker2 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket || (new List<string>() { "E-BDB111", "E-BDB112", "E-BDB114", "E-BDB131" }.Contains(edge.Target.Load.ID.BlockName) && edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.LumpedLoad))
            {
                //漏电
                edge.Details.CircuitForm = new LeakageCircuit()
                {
                    breaker= new ResidualCurrentBreaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics,edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLuminaire)
            {
                //消防应急照明回路
                edge.Details.CircuitForm = new FireEmergencyLighting()
                {
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }

            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
        }

        /// <summary>
        /// 回路元器件选型/指定元器件选型
        /// </summary>
        /// <returns></returns>
        public static PDSBaseComponent ComponentSelection(this ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge, Type type, CircuitFormOutType circuitFormOutType)
        {
            if (type.IsSubclassOf(typeof(PDSBaseComponent)))
            {
                var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
                var CascadeCurrent = edge.Target.Details.CascadeCurrent;
                var MaxCalculateCurrent = Math.Max(CalculateCurrent, CascadeCurrent);
                var PolesNum = "3P"; //极数
                var SpecialPolesNum = "3P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
                if (edge.Target.Load.Phase == ThPDSPhase.一相)
                {
                    PolesNum = "1P";
                    if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.OutdoorLights)
                    {
                        SpecialPolesNum = "1P";
                    }
                    else
                    {
                        SpecialPolesNum = "2P";
                    }
                }
                var Characteristics = "";//瞬时脱扣器类型
                var TripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out Characteristics);//脱扣器类型

                if (type.Equals(typeof(BreakerBaseComponent)))
                {
                    if(circuitFormOutType == CircuitFormOutType.漏电)
                        return new ResidualCurrentBreaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics, edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor);
                    else
                        return new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics);
                }
                else if(type.Equals(typeof(Breaker)))
                {
                    return new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics);
                }
                else if (type.Equals(typeof(ResidualCurrentBreaker)))
                {
                    return new ResidualCurrentBreaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics, edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor);
                }
                else if (type.Equals(typeof(ThermalRelay)))
                {
                    return new ThermalRelay(CalculateCurrent);
                }
                else if (type.Equals(typeof(Contactor)))
                {
                    return new Contactor(CalculateCurrent, PolesNum);
                }
                else if(type.Equals(typeof(Meter)))
                {
                    if (circuitFormOutType == CircuitFormOutType.配电计量_上海直接表 || circuitFormOutType == CircuitFormOutType.配电计量_直接表在前 || circuitFormOutType == CircuitFormOutType.配电计量_直接表在后)
                    {
                        return new MeterTransformer(CalculateCurrent, PolesNum);
                    }
                    else
                    {
                        return new CurrentTransformer(CalculateCurrent, PolesNum);
                    }
                }
                else if (type.Equals(typeof(MeterTransformer)))
                {
                    return new MeterTransformer(CalculateCurrent, PolesNum);
                }
                else if (type.Equals(typeof(CurrentTransformer)))
                {
                    return new CurrentTransformer(CalculateCurrent, PolesNum);
                }
                else if (type.Equals(typeof(CPS)))
                {
                    return new CPS(CalculateCurrent);
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
        public static void ComponentSelection(this ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge , CircuitFormOutType circuitFormOutType)
        {
            edge.Details = new CircuitDetails();
            var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            var CascadeCurrent = edge.Target.Details.CascadeCurrent;
            var MaxCalculateCurrent = Math.Max(CalculateCurrent, CascadeCurrent);
            var PolesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            var SpecialPolesNum = "3P"; //<新逻辑>极数 仅只针对断路器、隔离开关、漏电断路器
            if (edge.Target.Load.Phase == ThPDSPhase.一相)
            {
                PolesNum = "1P";
                if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.OutdoorLights)
                {
                    SpecialPolesNum = "1P";
                }
                else
                {
                    SpecialPolesNum = "2P";
                }
            }
            var Characteristics = "";//瞬时脱扣器类型
            var TripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out Characteristics);//脱扣器类型
            if (circuitFormOutType == CircuitFormOutType.常规)
            {
                edge.Details.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.漏电)
            {
                edge.Details.CircuitForm = new LeakageCircuit()
                {
                    breaker= new ResidualCurrentBreaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics, edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor),
                };
            }
            else if(circuitFormOutType == CircuitFormOutType.接触器控制)
            {
                edge.Details.CircuitForm = new ContactorControlCircuit()
                {
                    breaker= new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    contactor = new Contactor(CalculateCurrent, PolesNum),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.热继电器保护)
            {
                edge.Details.CircuitForm = new ThermalRelayProtectionCircuit()
                {
                    breaker= new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    thermalRelay = new ThermalRelay(CalculateCurrent),
                };
            }
            else if(circuitFormOutType == CircuitFormOutType.配电计量_上海CT)
            {
                edge.Details.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                {
                    breaker1 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    meter = new CurrentTransformer(CalculateCurrent, PolesNum),
                    breaker2 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.配电计量_上海直接表)
            {
                edge.Details.CircuitForm = new DistributionMetering_ShanghaiMTCircuit()
                {
                    breaker1 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    meter = new MeterTransformer(CalculateCurrent, PolesNum),
                    breaker2 = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.配电计量_CT表在前)
            {
                edge.Details.CircuitForm = new DistributionMetering_CTInFrontCircuit()
                {
                    meter = new CurrentTransformer(CalculateCurrent, PolesNum),
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.配电计量_直接表在前)
            {
                edge.Details.CircuitForm = new DistributionMetering_MTInFrontCircuit()
                {
                    meter = new MeterTransformer(CalculateCurrent, PolesNum),
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.配电计量_CT表在后)
            {
                edge.Details.CircuitForm = new DistributionMetering_CTInBehindCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    meter = new CurrentTransformer(CalculateCurrent, PolesNum),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.配电计量_直接表在后)
            {
                edge.Details.CircuitForm = new DistributionMetering_MTInBehindCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    meter = new MeterTransformer(CalculateCurrent, PolesNum),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.电动机_分立元件)
            {
                edge.Details.CircuitForm = new Motor_DiscreteComponentsCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    contactor = new Contactor(CalculateCurrent, PolesNum),
                    thermalRelay = new ThermalRelay(CalculateCurrent),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.电动机_CPS)
            {
                edge.Details.CircuitForm = new Motor_CPSCircuit()
                {
                    cps = new CPS(CalculateCurrent),
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.电动机_分立元件星三角启动)
            {
                edge.Details.CircuitForm = new Motor_DiscreteComponentsStarTriangleStartCircuit()
                {
                    breaker = new Breaker(MaxCalculateCurrent, TripDevice, SpecialPolesNum, Characteristics),
                    contactor1 = new Contactor(CalculateCurrent, PolesNum),
                    thermalRelay = new ThermalRelay(CalculateCurrent),
                    contactor2 = new Contactor(CalculateCurrent, PolesNum),
                    contactor3 = new Contactor(CalculateCurrent, PolesNum),
                };
            }
            else if(circuitFormOutType == CircuitFormOutType.电动机_CPS星三角启动)
            {
                edge.Details.CircuitForm = new Motor_CPSStarTriangleStartCircuit()
                {
                    cps = new CPS(CalculateCurrent),
                    contactor1 = new Contactor(CalculateCurrent, PolesNum),
                    contactor2 = new Contactor(CalculateCurrent, PolesNum),
                    Conductor1 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                    Conductor2 = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber),
                };
            }
            else if (circuitFormOutType == CircuitFormOutType.消防应急照明回路WFEL)
            {
                edge.Details.CircuitForm = new FireEmergencyLighting() 
                {
                    Conductor = new Conductor(CalculateCurrent, edge.Target.Load.Phase, edge.Target.Load.CircuitType, edge.Target.Load.LoadTypeCat_1, edge.Target.Load.FireLoad, edge.Circuit.ViaConduit, edge.Circuit.ViaCableTray, edge.Target.Load.Location.FloorNumber)
                };
            }
            else
            {
                //暂未支持该回路类型，请暂时不要选择该回路
                throw new NotSupportedException();
            }
            //统计回路级联电流
            edge.Details.CascadeCurrent = Math.Max(CascadeCurrent, edge.Details.CircuitForm.GetCascadeCurrent());
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
        public static void BalancedPhaseSequence(this List<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> edges)
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
        public static void Compatible(this ThPDSProjectGraph Graph, AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> NewGraph)
        {
            //暂时不考虑
        }

        public static void UpdateWithNode(this ThPDSProjectGraph graph, ThPDSProjectGraphNode node , bool permission = true)
        {
            var edges = graph.Graph.Edges.Where(e => e.Source.Equals(node)).ToList();
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
            node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            edges = graph.Graph.Edges.Where(e => e.Target.Equals(node)).ToList();
            edges.ForEach(e => graph.UpdateWithEdge(e));
        }

        public static void UpdateWithEdge(this ThPDSProjectGraph graph, ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge, bool permission = true)
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
        public static void ComponentCheck(this ThPDSProjectGraphNode node, List<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> edges)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    if(oneWayInCircuit.isolatingSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        oneWayInCircuit.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, oneWayInCircuit.isolatingSwitch.PolesNum);
                    }
                }
                else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    if (twoWayInCircuit.isolatingSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, twoWayInCircuit.isolatingSwitch1.PolesNum);
                    }
                    if (twoWayInCircuit.isolatingSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, twoWayInCircuit.isolatingSwitch2.PolesNum);
                    }
                    if (twoWayInCircuit.transferSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        twoWayInCircuit.transferSwitch = new AutomaticTransferSwitch(CalculateCurrent, (twoWayInCircuit.transferSwitch as AutomaticTransferSwitch).PolesNum);
                    }
                }
                else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    if (threeWayInCircuit.isolatingSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, threeWayInCircuit.isolatingSwitch1.PolesNum);
                    }
                    if (threeWayInCircuit.isolatingSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, threeWayInCircuit.isolatingSwitch2.PolesNum);
                    }
                    if (threeWayInCircuit.transferSwitch1.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.transferSwitch1 = new AutomaticTransferSwitch(CalculateCurrent, (threeWayInCircuit.transferSwitch1 as AutomaticTransferSwitch).PolesNum);
                    }
                    if (threeWayInCircuit.isolatingSwitch3.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.isolatingSwitch3 = new IsolatingSwitch(CalculateCurrent, threeWayInCircuit.isolatingSwitch2.PolesNum);
                    }
                    if (threeWayInCircuit.transferSwitch2.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        threeWayInCircuit.transferSwitch2 = new ManualTransferSwitch(CalculateCurrent, (threeWayInCircuit.transferSwitch2 as ManualTransferSwitch).PolesNum);
                    }
                }
                else if (node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    if (centralized.isolatingSwitch.GetCascadeRatedCurrent() < CalculateCurrent)
                    {
                        centralized.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, centralized.isolatingSwitch.PolesNum);
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
        public static void ComponentCheck(this ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            edge.Details = new CircuitDetails();
            var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            var CascadeCurrent = edge.Target.Details.CascadeCurrent;
            if(edge.Details.CircuitForm is RegularCircuit regularCircuit)
            {
                if(regularCircuit.breaker.GetCascadeRatedCurrent() <= CascadeCurrent)
                {
                    regularCircuit.breaker = new Breaker(CascadeCurrent, new List<string>() { regularCircuit.breaker.TripUnitType }, regularCircuit.breaker.PolesNum, regularCircuit.breaker.TripUnitType);
                }
            }
        }
    }
}
