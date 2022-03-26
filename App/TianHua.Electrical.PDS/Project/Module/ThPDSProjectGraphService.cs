using System;
using System.Linq;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;

namespace TianHua.Electrical.PDS.Project.Module
{
    public static class ThPDSProjectGraphService
    {
        /// <summary>
        /// 新建回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        public static void AddCircuit(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, CircuitFormOutType type)
        {
            //Step 1:新建未知负载
            var target = new ThPDSProjectGraphNode();
            graph.Graph.AddVertex(target);
            //Step 2:新建回路
            var newEdge = new ThPDSProjectGraphEdge<ThPDSProjectGraphNode>(node, target) { Circuit = new ThPDSCircuit()};
            //Step 3:回路选型
            newEdge.ComponentSelection(type);
            //Step 4:添加到Graph
            graph.Graph.AddEdge(newEdge);
        }

        /// <summary>
        /// 切换进线形式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void UpdateFormInType(ThPDSProjectGraph graph, ThPDSProjectGraphNode node, CircuitFormInType type)
        {
            if (node.Load.LoadTypeCat_1 == Model.ThPDSLoadTypeCat_1.DistributionPanel && node.Details.CircuitFormType.CircuitFormType != type && type != CircuitFormInType.None)
            {
                var CalculateCurrent = node.Load.CalculateCurrent;//计算电流
                var edges = graph.Graph.Edges.Where(e => e.Source.Equals(node)).ToList();
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                var MaxCalculateCurrent = Math.Max(CalculateCurrent, CascadeCurrent);//进线回路暂时没有需要级联的元器件
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
                switch (type)
                {
                    case CircuitFormInType.一路进线:
                        {
                            node.Details.CircuitFormType = new OneWayInCircuit()
                            {
                                isolatingSwitch = new IsolatingSwitch(CalculateCurrent, PolesNum)
                            };
                            break;
                        }
                    case CircuitFormInType.二路进线ATSE:
                        {
                            node.Details.CircuitFormType = new TwoWayInCircuit()
                            {
                                isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, PolesNum),
                                isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, PolesNum),
                                transferSwitch = new AutomaticTransferSwitch(CalculateCurrent, PolesNum)
                            };
                            break;
                        }
                    case CircuitFormInType.三路进线:
                        {
                            node.Details.CircuitFormType = new ThreeWayInCircuit()
                            {
                                isolatingSwitch1 = new IsolatingSwitch(CalculateCurrent, PolesNum),
                                isolatingSwitch2 = new IsolatingSwitch(CalculateCurrent, PolesNum),
                                transferSwitch1 = new AutomaticTransferSwitch(CalculateCurrent, PolesNum),
                                isolatingSwitch3 = new IsolatingSwitch(CalculateCurrent, PolesNum),
                                transferSwitch2 = new AutomaticTransferSwitch(CalculateCurrent, PolesNum),
                            };
                            break;
                        }
                    case CircuitFormInType.集中电源:
                        {
                            node.Details.CircuitFormType = new CentralizedPowerCircuit()
                            {
                                isolatingSwitch = new IsolatingSwitch(CalculateCurrent, PolesNum),
                            };
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            }
        }

        /// <summary>
        /// 切换回路样式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        public static void SwitchFormOutType(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge, CircuitFormOutType type)
        {
            if (!edge.Details.CircuitForm.CircuitFormType.Equals(type))
            {
                //回路类型相同时没有必要转换
                edge.UpdateCircuit(edge.Details.CircuitForm, type);
            }
        }

        /// <summary>
        /// 锁定回路（解锁回路）
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="doLock"></param>
        public static void Lock(ThPDSProjectGraph graph, ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge, bool doLock)
        {
            edge.Details.CircuitLock = doLock;
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void Delete(ThPDSProjectGraph graph, ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            //删除回路只删除这个连接关系，前后节点都还保留
            //所以删除后，后面的负载会失去原有的回路,需要人再去分配
            graph.Graph.RemoveEdge(edge);
        }

        /// <summary>
        /// 插入过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 插入电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 更新图
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void UpdateWithNode(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            graph.UpdateWithNode(node , false);
        }

        public static void UpdateWithEdge(ThPDSProjectGraph graph, ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            graph.UpdateWithEdge(edge, false);
        }
    }
}
