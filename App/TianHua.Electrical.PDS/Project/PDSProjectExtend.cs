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
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project
{
    public static class PDSProjectExtend
    {
        /// <summary>
        /// 统计功率
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
                    node.Details.CircuitFormType = "集中电源";
                }
                else
                {
                    var count = Graph.Edges.Count(o => o.Target.Equals(node));
                    if (count == 1)
                    {
                        node.Details.CircuitFormType = "1路进线";
                    }
                    else if (count == 2)
                    {
                        node.Details.CircuitFormType = "2路进线ATSE";
                    }
                    else if (count == 3)
                    {
                        node.Details.CircuitFormType = "3路进线";
                    }
                }
            });
            ProjectGraph.Graph = Graph;
            ProjectGraph.CalculatePower();
            ProjectGraph.Graph.Edges.ForEach(edge =>
            {
                edge.Circuit = edge.Circuit.RichPDSCircuit();
                edge.Details = edge.CreatCircuitDetails();
                edge.CalculateCircuitDetails();
            });
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

            if (edge.Target.Type == PDSNodeType.None)
            {
                circuitDetails.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker() { BreakerType = "MCB", FrameSpecifications = "63", PolesNum ="3P", RatedCurrent ="32", TripUnitType ="TM" },
                };
                //edge.circuitDetails.CircuitFormType = CircuitFormOutType.常规;
            }
            else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                //电动机需要特殊处理-不通过读表的方式，而是通过读另一个配置表，直接选型
                circuitDetails.CircuitForm = new MotorCircuit_DiscreteComponents()
                {
                    breaker = new Breaker() { BreakerType = "MCB", FrameSpecifications = "32", PolesNum ="3P", RatedCurrent ="50", TripUnitType ="TM"},
                    contactor = new Contactor() { ContactorType = "CJ", PolesNum ="3P", RatedCurrent ="12" },
                    thermalRelay = new ThermalRelay() { ThermalRelayType = "KH", PolesNum ="3P", RatedCurrent ="7~10" },
                };
            }
            else
            {
                circuitDetails.CircuitForm = new RegularCircuit()
                {
                    breaker = new Breaker() { BreakerType = "MCB", FrameSpecifications = "63", PolesNum ="3P", RatedCurrent ="32", TripUnitType ="TM" },
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
