using Dreambuild.AutoCAD;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;

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
            var ProjectGraph = new ThPDSProjectGraph() { Graph = graph };
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
                e.Details = e.CreatCircuitDetails();
            });
            if (node.Details.LowPower <= 0)
            {
                node.Details.LowPower = edges.Sum(e => e.Target.Details.LowPower);
            }
            node.CalculateCurrent();
            PDSProjectGraph.CalculateCircuitFormInType(node);
            PDSProjectGraph.LeafComponentSelection(node);
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
                //node.nodeDetails.CircuitFormType = CircuitFormInType.集中电源;
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
                    DemandFactor = 1;
                var PowerFactor = node.Load.PowerFactor;
                var KV = Phase == ThPDSPhase.一相 ? 0.22 : 0.38;
                node.Load.CalculateCurrent = Math.Round(node.Details.LowPower * DemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }

        /// <summary>
        /// 叶子节点元器件选型
        /// </summary>
        public static void LeafComponentSelection(this ThPDSProjectGraph PDSProjectGraph, ThPDSProjectGraphNode node)
        {
            if (node.Type == PDSNodeType.DistributionBox)
            {
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var PolesNum = "4P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
                if (node.Load.Phase == ThPDSPhase.一相)
                {
                    //当相数为1时，若负载类型不为“Outdoor Lights”，且断路器不是ATSE前的主进线开关，则断路器选择1P；
                    //当相数为1时，若负载类型为“Outdoor Lights”，或断路器是ATSE前的主进线开关，则断路器选择2P；
                    if (node.Load.LoadTypeCat_2 != ThPDSLoadTypeCat_2.OutdoorLights && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                    {
                        PolesNum = "1P";
                    }
                    else
                    {
                        PolesNum = "2P";
                    }
                }
                else if (node.Load.Phase == ThPDSPhase.三相)
                {
                    if (node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.二路进线ATSE && node.Details.CircuitFormType.CircuitFormType != CircuitFormInType.三路进线)
                    {
                        PolesNum = "3P";
                    }
                }
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    oneWayInCircuit.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, PolesNum);
                }
                else if(node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
                {
                    twoWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, PolesNum);
                    twoWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, PolesNum);
                    twoWayInCircuit.transferSwitch = new AutomaticTransferSwitch(CalculateCurrent, PolesNum);
                }
                else if(node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
                {
                    threeWayInCircuit.isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, PolesNum);
                    threeWayInCircuit.isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, PolesNum);
                    threeWayInCircuit.transferSwitch1 = new AutomaticTransferSwitch(CalculateCurrent, PolesNum);
                    threeWayInCircuit.isolatingSwitch3 = new IsolatingSwitch(CalculateCurrent, PolesNum);
                    threeWayInCircuit.transferSwitch1 = new ManualTransferSwitch(CalculateCurrent, PolesNum);
                }
                else if(node.Details.CircuitFormType is CentralizedPowerCircuit centralized)
                {
                    centralized.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, PolesNum);
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this ThPDSProjectGraph PDSProjectGraph)
        {

        }

        /// <summary>
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(this ThPDSProjectGraph PDSProjectGraph, ThPDSProjectGraphNode Node)
        {

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

        /// <summary>
        /// 创建回路细节信息
        /// </summary>
        /// <param name="pDSCircuit"></param>
        /// <returns></returns>
        public static CircuitDetails CreatCircuitDetails(this ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            var circuitDetails = new CircuitDetails();
            var CalculateCurrent = edge.Target.Load.CalculateCurrent;//计算电流
            var PolesNum = "3P"; //极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
            if (edge.Target.Load.Phase == ThPDSPhase.一相)
            {
                if(edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.OutdoorLights)
                {
                    PolesNum = "1P";
                }
                else
                {
                    PolesNum = "2P";
                }
            }
            var Characteristics = "";//瞬时脱扣器类型
            var TripDevice = edge.Target.Load.LoadTypeCat_1.GetTripDevice(edge.Target.Load.FireLoad, out Characteristics);//脱扣器类型
            if (edge.Target.Type == PDSNodeType.None)
            {
                circuitDetails.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                };
                //edge.circuitDetails.CircuitFormType = CircuitFormOutType.常规;
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                //2022/03/14 为了本周尽快实现联动，目前发动机暂只支持 分立元件 与 分立元件-星三角启动
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                if (ProjectGlobalConfiguration.MotorUIChoise == "分立元件")
                {
                    if (edge.Target.Details.LowPower <ProjectGlobalConfiguration.MotorPower)
                    {
                        circuitDetails.CircuitForm = new Motor_DiscreteComponentsCircuit()
                        {
                            breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                            contactor = new Contactor(CalculateCurrent, PolesNum),
                            thermalRelay = new ThermalRelay(CalculateCurrent),
                        };
                    }
                    else
                    {
                        circuitDetails.CircuitForm = new Motor_DiscreteComponentsStarTriangleStartCircuit()
                        {
                            breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                            contactor1 = new Contactor(CalculateCurrent, PolesNum),
                            thermalRelay = new ThermalRelay(CalculateCurrent),
                            contactor2 = new Contactor(CalculateCurrent, PolesNum),
                            contactor3 = new Contactor(CalculateCurrent, PolesNum),
                        };
                    }
                }
            }
            else if (edge.Target.Load.ID.BlockName == "E-BDB006-1" && edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
            {
                circuitDetails.CircuitForm = new DistributionMetering_ShanghaiCTCircuit()
                {
                    breaker1 = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                    meter = new MeterTransformer(CalculateCurrent),
                    breaker2 = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                };
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket || (new List<string>() { "E-BDB111", "E-BDB112", "E-BDB114", "E-BDB131" }.Contains(edge.Target.Load.ID.BlockName) && edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.LumpedLoad))
            {
                //漏电
                circuitDetails.CircuitForm = new LeakageCircuit()
                {
                    breaker= new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                };
            }
            else if(edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLuminaire)
            {
                //消防应急照明回路
                circuitDetails.CircuitForm = new FireEmergencyLighting();
            }
            else
            {
                circuitDetails.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                };
            }
            return circuitDetails;
        }
    }
}
