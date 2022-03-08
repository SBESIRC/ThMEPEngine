using Dreambuild.AutoCAD;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// 计算功率
        /// </summary>
        public static void CalculatePower(this ThPDSProjectGraph PDSProjectGraph)
        {
            var Nodes = PDSProjectGraph.Graph.Vertices.ToList();
            ThPDSProjectGraphNode node = null;
            while (!(node = Nodes.FirstOrDefault()).IsNull())
            {
                ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge;
                while (!(edge = PDSProjectGraph.Graph.Edges.FirstOrDefault(o => o.Source.Equals(node))).IsNull())
                {
                    node = edge.Target;
                }
                while(!(edge = PDSProjectGraph.Graph.Edges.FirstOrDefault(o => o.Target.Equals(node))).IsNull())
                {
                    var superiorNode = edge.Source;
                    var edges = PDSProjectGraph.Graph.Edges.Where(o => o.Source.Equals(superiorNode));
                    edges.ForEach(e => Nodes.Remove(e.Target));
                    if(!superiorNode.Details.IsDualPower && superiorNode.Details.LowPower <= 0)
                    {
                        superiorNode.Details.LowPower = edges.Sum(e => e.Target.Details.LowPower);
                    }
                    node = superiorNode;
                }
                Nodes.Remove(node);
            }
        }

        /// <summary>
        /// 计算电流
        /// </summary>
        public static void CalculateCurrent(this ThPDSProjectGraph PDSProjectGraph)
        {
            PDSProjectGraph.Graph.Vertices.ForEach(v =>
            {
                //if单相/三相，因为还没有这部分的内容，所有默认所有都是三相
                //单向：I_c=S_c/U_n =P_c/(cos⁡φ×U_n )=(P_n×K_d)/(cos⁡φ×U_n )
                //三相：I_c=S_c/(√3 U_n )=P_c/(cos⁡φ×√3 U_n )=(P_n×K_d)/(cos⁡φ×√3 U_n )
                var edge = PDSProjectGraph.Graph.Edges.FirstOrDefault(o => o.Target.Equals(v));
                if (edge.IsNull())
                {

                }
                else
                {
                    var Phase = edge.Circuit.Phase;
                    if (Phase != 1 && Phase != 3)
                    {
                        v.Load.CalculateCurrent = 0;
                    }
                    else
                    {
                        var DemandFactor = edge.Circuit.DemandFactor;
                        if (v.Details.IsOnlyLoad)
                            DemandFactor = 1;
                        var PowerFactor = edge.Circuit.PowerFactor;
                        var KV = Phase == 1 ? 0.22 : 0.38;
                        v.Load.CalculateCurrent =Math.Round(v.Details.LowPower * DemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
                    }
                }
            });
        }

        /// <summary>
        /// 计算元器件选型
        /// </summary>
        public static void CalculateComponent(this ThPDSProjectGraph PDSProjectGraph)
        {
            var leafNodes = PDSProjectGraph.Graph.Vertices.Where(v => !PDSProjectGraph.Graph.Edges.Any(e => e.Source.Equals(v))).ToList();
            leafNodes.ForEach(node =>
            {
                PDSProjectGraph.LeafComponentSelection(node);
            });
            while(leafNodes.Count > 0)
            {
                var node = leafNodes.First();
                var superiorNodes = PDSProjectGraph.Graph.Edges.Where(e => e.Target.Equals(node)).Select(e => e.Source).ToList();//上级节点
                foreach (var superiorNode in superiorNodes)
                {
                    PDSProjectGraph.Graph.Edges.Where(e => e.Source.Equals(superiorNode)).ForEach(e => leafNodes.Remove(e.Target));
                    PDSProjectGraph.ComponentSelection(superiorNode);
                }
            }
        }

        /// <summary>
        /// 叶子节点元器件选型
        /// </summary>
        public static void LeafComponentSelection(this ThPDSProjectGraph PDSProjectGraph,ThPDSProjectGraphNode node)
        {
            if(node.Type == PDSNodeType.DistributionBox)
            {
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var PolesNum = "3P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
                if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
                {
                    oneWayInCircuit.isolatingSwitch = new IsolatingSwitch(CalculateCurrent, PolesNum);
                }
                else
                {
                    //暂未定义，后续补充
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// 非叶子节点元器件选型
        /// </summary>
        public static void ComponentSelection(this ThPDSProjectGraph PDSProjectGraph, ThPDSProjectGraphNode node)
        {
            var edges = PDSProjectGraph.Graph.Edges.Where(e => e.Source.Equals(node));
            foreach (var edge in edges)
            {
                edge.Details = edge.CreatCircuitDetails();
            }
            PDSProjectGraph.LeafComponentSelection(node);
            var superiorNodes = PDSProjectGraph.Graph.Edges.Where(e => e.Target.Equals(node)).Select(e => e.Source).ToList();//上级节点
            foreach (var superiorNode in superiorNodes)
            {
                PDSProjectGraph.ComponentSelection(superiorNode);
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
        /// 创建PDSProjectGraph
        /// </summary>
        /// <param name="Graph"></param>
        public static ThPDSProjectGraph CreatPDSProjectGraph(this AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph)
        {
            var ProjectGraph = new ThPDSProjectGraph();
            Graph.Vertices.ForEach(node =>
            {
                if (node.Load.LoadTypeCat_1 ==ThPDSLoadTypeCat_1.DistributionPanel && node.Load.LoadTypeCat_2 ==ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)
                {
                    //node.nodeDetails.CircuitFormType = CircuitFormInType.集中电源;
                    node.Details.CircuitFormType = new CentralizedPowerCircuit();
                }
                else
                {
                    var count = Graph.Edges.Count(o => o.Target.Equals(node));
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
            });
            ProjectGraph.Graph = Graph;
            ProjectGraph.CalculatePower();
            ProjectGraph.CalculateCurrent();
            ProjectGraph.CalculateComponent();
            //ProjectGraph.Graph.Edges.ForEach(edge =>
            //{
            //    edge.Circuit = edge.Circuit.RichPDSCircuit();
            //    edge.Details = edge.CreatCircuitDetails();
            //    edge.CalculateCircuitDetails();
            //});
            //ProjectGraph.BalancedPhaseSequence();
            return ProjectGraph;
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
        /// 填充回路默认元素
        /// </summary>
        /// <param name="pDSCircuit"></param>
        /// <returns></returns>
        public static ThPDSCircuit RichPDSCircuit(this ThPDSCircuit pDSCircuit)
        {
            //if(pDSCircuit.)
            return pDSCircuit;
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
            var PolesNum = "3P";//极数 参考ID1002581 业务逻辑-元器件选型-断路器选型-3.极数的确定方法
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
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                circuitDetails.CircuitForm = new MotorCircuit_DiscreteComponents()
                {
                    breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                    contactor = new Contactor(CalculateCurrent, PolesNum),
                    thermalRelay = new ThermalRelay(CalculateCurrent),
                };
            }
            else
            {
                circuitDetails.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker(CalculateCurrent, TripDevice, PolesNum, Characteristics),
                };
            }
            //else if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
            //{
            //    edge.circuitDetails.CircuitFormType = CircuitFormOutType.配电计量_上海CT;
            //}
            //else if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ACCharger|| edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.DCCharger)
            //{
            //    edge.circuitDetails.CircuitFormType = CircuitFormOutType.漏电;
            //}
            //else if (edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLuminaire)
            //{
            //    edge.circuitDetails.CircuitFormType = CircuitFormOutType.消防应急照明回路WFEL;
            //}
            //else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket)
            //{
            //    edge.circuitDetails.CircuitFormType = CircuitFormOutType.漏电;
            //}
            //else
            //{
            //    edge.circuitDetails.CircuitFormType = CircuitFormOutType.常规;
            //}
            return circuitDetails;
        }
    }
}
