using System;
using QuikGraph;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory;

namespace TianHua.Electrical.PDS.Project.Module
{
    public static class ThPDSProjectGraphService
    {
        /// <summary>
        /// 切换进线形式
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="type"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void UpdateFormInType(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, CircuitFormInType type)
        {
            if (node.Load.LoadTypeCat_1 == Model.ThPDSLoadTypeCat_1.DistributionPanel && node.Details.CircuitFormType.CircuitFormType != type && type != CircuitFormInType.None)
            {
                var edges = graph.Edges.Where(e => e.Source.Equals(node)).ToList();
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
                switch (type)
                {
                    case CircuitFormInType.一路进线:
                        {
                            node.Details.CircuitFormType = new OneWayInCircuit()
                            {
                                isolatingSwitch = componentFactory.CreatIsolatingSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.二路进线ATSE:
                        {
                            node.Details.CircuitFormType = new TwoWayInCircuit()
                            {
                                isolatingSwitch1 = componentFactory.CreatIsolatingSwitch(),
                                isolatingSwitch2 = componentFactory.CreatIsolatingSwitch(),
                                transferSwitch = componentFactory.CreatAutomaticTransferSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.三路进线:
                        {
                            node.Details.CircuitFormType = new ThreeWayInCircuit()
                            {
                                isolatingSwitch1 = componentFactory.CreatIsolatingSwitch(),
                                isolatingSwitch2 = componentFactory.CreatIsolatingSwitch(),
                                isolatingSwitch3 = componentFactory.CreatIsolatingSwitch(),
                                transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch(),
                                transferSwitch2 = componentFactory.CreatManualTransferSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.集中电源:
                        {
                            node.Details.CircuitFormType = new CentralizedPowerCircuit()
                            {
                                isolatingSwitch = componentFactory.CreatIsolatingSwitch(),
                            };
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
                //统计节点级联电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            }
        }

        /// <summary>
        /// 分配负载
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void DistributeLoad(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphEdge edge)
        {
            //
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void DeleteCircuit(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphEdge edge)
        {
            //删除回路只删除这个连接关系，前后节点都还保留
            //所以删除后，后面的负载会失去原有的回路,需要人再去分配
            graph.RemoveEdge(edge);
        }

        /// <summary>
        /// 插入过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent.IsNull() || oneWayInCircuit.reservedComponent is Meter)
                {
                    oneWayInCircuit.reservedComponent = componentFactory.CreatOUVP();
                }
            }
            else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
            {
                if (twoWayInCircuit.reservedComponent.IsNull() || twoWayInCircuit.reservedComponent is Meter)
                {
                    twoWayInCircuit.reservedComponent = componentFactory.CreatOUVP();
                }
            }
            else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
            {
                if (threeWayInCircuit.reservedComponent.IsNull() || threeWayInCircuit.reservedComponent is Meter)
                {
                    threeWayInCircuit.reservedComponent = componentFactory.CreatOUVP();
                }
            }
            else
            {
                //业务逻辑：别的回路不允许插入该元器件
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 移除过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveUndervoltageProtector(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent is OUVP)
                {
                    oneWayInCircuit.reservedComponent = null ;
                }
            }
            else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
            {
                if (twoWayInCircuit.reservedComponent is OUVP)
                {
                    twoWayInCircuit.reservedComponent = null;
                }
            }
            else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
            {
                if (threeWayInCircuit.reservedComponent is OUVP)
                {
                    threeWayInCircuit.reservedComponent = null;
                }
            }
            else
            {
                //业务逻辑：别的回路不允许删除该元器件
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 插入电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void InsertEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.Graph.OutEdges(node).ToList();
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, edges);
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent.IsNull() || oneWayInCircuit.reservedComponent is OUVP)
                {
                    oneWayInCircuit.reservedComponent = componentFactory.CreatMeterTransformer();
                }
            }
            else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
            {
                if (twoWayInCircuit.reservedComponent.IsNull() || twoWayInCircuit.reservedComponent is OUVP)
                {
                    twoWayInCircuit.reservedComponent = componentFactory.CreatMeterTransformer();
                }
            }
            else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
            {
                if (threeWayInCircuit.reservedComponent.IsNull() || threeWayInCircuit.reservedComponent is OUVP)
                {
                    threeWayInCircuit.reservedComponent = componentFactory.CreatMeterTransformer();
                }
            }
            else
            {
                //业务逻辑：别的回路不允许插入该元器件
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 移除电能表
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveEnergyMeter(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent is Meter)
                {
                    oneWayInCircuit.reservedComponent = null;
                }
            }
            else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
            {
                if (twoWayInCircuit.reservedComponent is Meter)
                {
                    twoWayInCircuit.reservedComponent = null;
                }
            }
            else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
            {
                if (threeWayInCircuit.reservedComponent is Meter)
                {
                    threeWayInCircuit.reservedComponent = null;
                }
            }
            else
            {
                //业务逻辑：别的回路不允许删除该元器件
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 更新图
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void UpdateWithNode(ThPDSProjectGraph graph, ThPDSProjectGraphNode node)
        {
            graph.UpdateWithNode(node, false);
        }

        public static void UpdateWithEdge(ThPDSProjectGraph graph, ThPDSProjectGraphEdge edge)
        {
            graph.UpdateWithEdge(edge, false);
        }
        public static void ComponentSwitching(ThPDSProjectGraphNode node, PDSBaseComponent component, ComponentType componentType)
        {

        }
        /// <summary>
        /// 元器件切换
        /// </summary>
        /// <returns></returns>
        public static void ComponentSwitching(ThPDSProjectGraphEdge edge, PDSBaseComponent component, ComponentType componentType)
        {
            if (component.ComponentType !=componentType && edge.Details.CircuitForm.Contains(component))
            {
                var ComponentType = componentType.GetComponentType();
                if (ComponentType.BaseType != typeof(PDSBaseComponent) && component.GetType().BaseType.Equals(ComponentType.BaseType))
                {
                    if (component is Breaker breaker && componentType == Component.ComponentType.一体式RCD)
                    {
                        //只有 CB -> RCD是特殊处理
                        var residualCurrentBreaker = new ResidualCurrentBreaker(breaker);
                        edge.Details.CircuitForm.SetCircuitComponentValue(component, residualCurrentBreaker);
                    }
                    else
                    {
                        edge.Details.CircuitForm.SetCircuitComponentValue(component, edge.ComponentSelection(ComponentType, edge.Details.CircuitForm.CircuitFormType));
                    }
                }
            }
        }

        /// <summary>
        /// 获取出线回路转换器
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static CircuitFormOutSwitcher GetCircuitFormOutSwitcher(ThPDSProjectGraphEdge edge)
        {
            return new CircuitFormOutSwitcher(edge);
        }
        
        /// <summary>
        /// 获取进线回路转换器
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public static CircuitFormInSwitcher GetCircuitFormInSwitcher(ThPDSProjectGraphNode node)
        {
            return new CircuitFormInSwitcher(node);
        }

        /// <summary>
        /// 切换出线回路形式
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="type"></param>
        public static void SwitchFormOutType(ThPDSProjectGraphEdge edge, CircuitFormOutType type)
        {
            if (!edge.Details.CircuitForm.CircuitFormType.Equals(type))
            {
                //回路类型相同时没有必要转换
                edge.UpdateCircuit(edge.Details.CircuitForm, type);
            }
        }

        /// <summary>
        /// 切换出线回路形式
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="type"></param>
        public static void SwitchFormOutType(ThPDSProjectGraphEdge edge, string type)
        {
            var CircuitFormOutType = Switch(edge, type);
            SwitchFormOutType(edge, CircuitFormOutType);
        }

        /// <summary>
        /// 新建回路
        /// </summary>
        public static void AddCircuit(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, CircuitFormOutType type)
        {
            //Step 1:新建未知负载
            var target = new ThPDSProjectGraphNode();
            graph.AddVertex(target);
            //Step 2:新建回路
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() };
            //Step 3:回路选型
            newEdge.ComponentSelection(type);
            //Step 4:添加到Graph
            graph.AddEdge(newEdge);
        }

        /// <summary>
        /// 新建回路
        /// </summary>
        public static void AddCircuit(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, string type)
        {
            //Step 1:新建未知负载
            var target = new ThPDSProjectGraphNode();
            graph.AddVertex(target);
            //Step 2:新建回路
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() };
            //Step 3:获取对应的CircuitFormOutType
            var CircuitFormOutType = Switch(newEdge, type);
            //Step 4:回路选型
            newEdge.ComponentSelection(CircuitFormOutType);
            //Step 5:添加到Graph
            graph.AddEdge(newEdge);
        }

        /// <summary>
        /// 获取可新建出线回路列表
        /// </summary>
        /// <returns></returns>
        public static List<string> AvailableTypes()
        {
            return new List<string>()
            {
                "常规配电回路",
                "漏电保护回路",
                "带接触器回路",
                "带热继电器回路",
                "计量(上海)",
                "计量(表在前)",
                "计量(表在后)",
                "电动机配电回路",
                "双速电机D-YY",
                "双速电机Y-Y",
                /*"分支母排"*/
            };
        }

        /// <summary>
        /// 获取可选择出线回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static List<string> AvailableTypes(ThPDSProjectGraphEdge edge)
        {
            CircuitGroup circuitGroup = edge.Details.CircuitForm.CircuitFormType.GetCircuitType().GetCircuitGroup();
            switch (circuitGroup)
            {
                case CircuitGroup.Group1:
                    { return ProjectSystemConfiguration.Group1Switcher; }
                case CircuitGroup.Group2:
                    { return ProjectSystemConfiguration.Group2Switcher; }
                case CircuitGroup.Group3:
                    { return ProjectSystemConfiguration.Group3Switcher; }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        /// <summary>
        /// 获取可选择进线回路列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static List<CircuitFormInType> AvailableTypes(ThPDSProjectGraphNode node)
        {
            var graph = PDSProject.Instance.graphData.Graph;
            if(node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.一路进线)
            {
                return new List<CircuitFormInType>() { CircuitFormInType.二路进线ATSE, CircuitFormInType.三路进线 };
            }
            else if(node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.二路进线ATSE)
            {
                var result = new List<CircuitFormInType>() { CircuitFormInType.三路进线 };
                var inCircuitNumCount = ThPDSCircuitNumberSeacher.Seach(node, graph).Count;
                if (inCircuitNumCount < 2)
                {
                    result.Insert(0, CircuitFormInType.一路进线);
                }
                return result;
            }
            else if(node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.三路进线)
            {
                var result = new List<CircuitFormInType>();
                var inCircuitNumCount = ThPDSCircuitNumberSeacher.Seach(node, graph).Count;
                if(inCircuitNumCount < 2)
                {
                    result.Add(CircuitFormInType.一路进线);
                }
                if(inCircuitNumCount < 3)
                {
                    result.Add(CircuitFormInType.二路进线ATSE);
                }
                return result;
            }
            else
            {
                return new List<CircuitFormInType>();
            }
        }

        /// <summary>
        /// 获取对应CircuitFormOutType
        /// </summary>
        /// <param name="CircuitName"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static CircuitFormOutType Switch(ThPDSProjectGraphEdge edge ,string circuitName)
        {
            if (circuitName == "常规配电回路")
                return CircuitFormOutType.常规;
            else if (circuitName == "漏电保护回路")
                return CircuitFormOutType.漏电;
            else if (circuitName == "带接触器回路")
                return CircuitFormOutType.接触器控制;
            else if (circuitName == "带热继电器回路")
                return CircuitFormOutType.热继电器保护;
            else if (circuitName == "计量(上海)")
            {
                if (edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_上海直接表;
                else
                    return CircuitFormOutType.配电计量_上海CT;
            }
            else if (circuitName == "计量(表在前)")
            {
                if (edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_直接表在前;
                else
                    return CircuitFormOutType.配电计量_CT表在前;
            }
            else if (circuitName == "计量(表在后)")
            {
                if (edge.Target.Details.LowPower < 100)
                    return CircuitFormOutType.配电计量_直接表在后;
                else
                    return CircuitFormOutType.配电计量_CT表在后;
            }
            else if (circuitName == "电动机配电回路")
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        return CircuitFormOutType.电动机_分立元件;
                    }
                    else
                    {
                        return CircuitFormOutType.电动机_分立元件星三角启动;
                    }
                }
                else
                {
                    if (edge.Target.Details.LowPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        return CircuitFormOutType.电动机_CPS;
                    }
                    else
                    {
                        return CircuitFormOutType.电动机_CPS星三角启动;
                    }
                }
            }
            else if(circuitName == "双速电机D-YY")
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    return CircuitFormOutType.双速电动机_分立元件detailYY;
                }
                else
                {
                    return CircuitFormOutType.双速电动机_CPSdetailYY;
                }
            }
            else if(circuitName == "双速电机Y-Y")
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    return CircuitFormOutType.双速电动机_分立元件YY;
                }
                else
                {
                    return CircuitFormOutType.双速电动机_CPSYY;
                }
            }
            else
            {
                //其他目前暂不支持
                throw new NotSupportedException();
            }
        }
    }
}
