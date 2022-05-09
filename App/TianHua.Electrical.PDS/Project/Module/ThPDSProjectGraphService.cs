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
using Dreambuild.AutoCAD;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

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
        public static void UpdateFormInType(ProjectGraph graph,
            ThPDSProjectGraphNode node, CircuitFormInType type)
        {
            if (node.Load.LoadTypeCat_1 == Model.ThPDSLoadTypeCat_1.DistributionPanel && node.Details.CircuitFormType.CircuitFormType != type && type != CircuitFormInType.None)
            {
                var edges = graph.Edges.Where(e => e.Source.Equals(node)).ToList();
                //统计节点级联电流
                var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
                CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
                SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
                switch (type)
                {
                    case CircuitFormInType.一路进线:
                        {
                            node.Details.CircuitFormType = new OneWayInCircuit()
                            {
                                Component = componentFactory.CreatIsolatingSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.二路进线ATSE:
                        {
                            node.Details.CircuitFormType = new TwoWayInCircuit()
                            {
                                Component1 = componentFactory.CreatIsolatingSwitch(),
                                Component2 = componentFactory.CreatIsolatingSwitch(),
                                transferSwitch = componentFactory.CreatAutomaticTransferSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.三路进线:
                        {
                            node.Details.CircuitFormType = new ThreeWayInCircuit()
                            {
                                Component1 = componentFactory.CreatIsolatingSwitch(),
                                Component2 = componentFactory.CreatIsolatingSwitch(),
                                Component3 = componentFactory.CreatIsolatingSwitch(),
                                transferSwitch1 = componentFactory.CreatAutomaticTransferSwitch(),
                                transferSwitch2 = componentFactory.CreatManualTransferSwitch(),
                            };
                            break;
                        }
                    case CircuitFormInType.集中电源:
                        {
                            node.Details.CircuitFormType = new CentralizedPowerCircuit()
                            {
                                Component = componentFactory.CreatIsolatingSwitch(),
                            };
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
                //统计节点级联电流
                node.Details.CascadeCurrent = Math.Max(CascadeCurrent, node.Details.CircuitFormType.GetCascadeCurrent());
            }
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="edge"></param>
        public static void DeleteCircuit(ProjectGraph graph,
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
        public static bool InsertUndervoltageProtector(ProjectGraph graph, ThPDSProjectGraphNode node, out string msg)
        {
            msg = "";
            var edges = graph.OutEdges(node).ToList();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
            OUVP ouvp;
            try
            {
                ouvp = componentFactory.CreatOUVP();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return false;
            }
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent.IsNull() || oneWayInCircuit.reservedComponent is Meter)
                {
                    oneWayInCircuit.reservedComponent = ouvp;
                }
            }
            else if (node.Details.CircuitFormType is TwoWayInCircuit twoWayInCircuit)
            {
                if (twoWayInCircuit.reservedComponent.IsNull() || twoWayInCircuit.reservedComponent is Meter)
                {
                    twoWayInCircuit.reservedComponent = ouvp;
                }
            }
            else if (node.Details.CircuitFormType is ThreeWayInCircuit threeWayInCircuit)
            {
                if (threeWayInCircuit.reservedComponent.IsNull() || threeWayInCircuit.reservedComponent is Meter)
                {
                    threeWayInCircuit.reservedComponent = ouvp;
                }
            }
            else
            {
                //业务逻辑：别的回路不允许插入该元器件
                msg = "集中电源无法插入OUVP";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 移除过欠电压保护
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void RemoveUndervoltageProtector(ThPDSProjectGraphNode node)
        {
            if (node.Details.CircuitFormType is OneWayInCircuit oneWayInCircuit)
            {
                if (oneWayInCircuit.reservedComponent is OUVP)
                {
                    oneWayInCircuit.reservedComponent = null;
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
        public static void InsertEnergyMeter(
            ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.OutEdges(node).ToList();
            //统计节点级联电流
            var CascadeCurrent = edges.Count > 0 ? edges.Max(e => e.Details.CascadeCurrent) : 0;
            CascadeCurrent = Math.Max(CascadeCurrent, node.Details.MiniBusbars.Count > 0 ? node.Details.MiniBusbars.Max(o => o.Key.CascadeCurrent) : 0);
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(node, CascadeCurrent);
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
        public static void RemoveEnergyMeter(ThPDSProjectGraphNode node)
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
        /// 对节点检查级联
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        public static void CheckCascadeWithNode(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.InEdges(node);
            foreach (var edge in edges)
            {
                edge.CheckCascadeWithEdge();
            }
        }

        /// <summary>
        /// 对小母排检查级联
        /// </summary>
        /// <param name="node"></param>
        public static void CheckCascadeWithMiniBusbar(ThPDSProjectGraphNode node)
        {
            node.CheckCascadeWithNode();
        }

        /// <summary>
        /// 对回路检查级联
        /// </summary>
        /// <param name="edge"></param>
        public static void CheckCascadeWithEdge(ThPDSProjectGraphEdge edge)
        {
            var miniBusbar = edge.Source.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge));
            if (miniBusbar.Key.IsNull())
            {
                edge.Source.CheckCascadeWithNode();
            }
            else
            {
                edge.Source.CheckCascadeWithMiniBusbar(miniBusbar.Key);
            }
        }

        /// <summary>
        /// 元器件切换
        /// </summary>
        /// <param name="node"></param>
        public static void ComponentSwitching(ThPDSProjectGraphNode node, PDSBaseComponent component, ComponentType componentType)
        {
            if (component.ComponentType != componentType && node.Details.CircuitFormType.Contains(component))
            {
                if(componentType == ComponentType.CB)
                {
                    node.Details.AllowBreakerSwitch = true;
                }
                var Componenttype = componentType.GetComponentType();
                if (component.GetType().BaseType.Equals(Componenttype.BaseType))
                {
                    node.Details.CircuitFormType.SetCircuitComponentValue(component, node.ComponentSelection(Componenttype));
                }
            }
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
                    edge.Details.CircuitForm.SetCircuitComponentValue(component, edge.ComponentSelection(ComponentType, edge.Details.CircuitForm.CircuitFormType));
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
        /// MoterUI Config改变
        /// </summary>
        public static void MotorChoiseChange()
        {
            var MotorSelection = PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise;
            var edges = PDSProject.Instance.graphData.Graph.Edges;
            var type = MotorSelection == MotorUIChoise.分立元件 ? CircuitFormOutType.电动机_CPS : CircuitFormOutType.电动机_分立元件;
            var typeStar = MotorSelection == MotorUIChoise.分立元件 ? CircuitFormOutType.电动机_CPS星三角启动 : CircuitFormOutType.电动机_分立元件星三角启动;
            var typeYY = MotorSelection == MotorUIChoise.分立元件 ? CircuitFormOutType.双速电动机_CPSYY : CircuitFormOutType.双速电动机_分立元件YY;
            var typedetailYY = MotorSelection == MotorUIChoise.分立元件 ? CircuitFormOutType.双速电动机_CPSdetailYY : CircuitFormOutType.双速电动机_分立元件detailYY;
            foreach (var edge in edges)
            {
                if (edge.Details.CircuitForm.CircuitFormType == type || edge.Details.CircuitForm.CircuitFormType == typeStar)
                {
                    SwitchFormOutType(edge, "电动机配电回路");
                }
                else if (edge.Details.CircuitForm.CircuitFormType == typeYY)
                {
                    SwitchFormOutType(edge, "双速电机Y-Y");
                }
                else if (edge.Details.CircuitForm.CircuitFormType == typedetailYY)
                {
                    SwitchFormOutType(edge, "双速电机D-YY");
                }
            }
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
        /// 平衡相序
        /// </summary>
        public static void BalancedPhaseSequence(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.OutEdges(node).ToList();
            edges.BalancedPhaseSequence();
        }

        /// <summary>
        /// 创建备用回路
        /// </summary>
        public static void CreatBackupCircuit(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = graph.OutEdges(node);
            var OnePhaseCB = new List<ThPDSProjectGraphEdge>();
            var OnePhaseRCD = new List<ThPDSProjectGraphEdge>();
            var ThreePhaseCB = new List<ThPDSProjectGraphEdge>();
            var ThreePhaseRCD = new List<ThPDSProjectGraphEdge>();

            var BackupOnePhaseCB = new List<ThPDSProjectGraphEdge>();
            var BackupOnePhaseRCD = new List<ThPDSProjectGraphEdge>();
            var BackupThreePhaseCB = new List<ThPDSProjectGraphEdge>();
            var BackupThreePhaseRCD = new List<ThPDSProjectGraphEdge>();

            foreach (var edge in edges)
            {
                var breakers = edge.Details.CircuitForm.GetCircuitBreakers();
                if (breakers.Count > 0)
                {
                    if (edge.Target.Type != PDSNodeType.None && edge.Target.Type != PDSNodeType.Empty)
                    {
                        if (edge.Target.Load.Phase == ThPDSPhase.三相)
                        {
                            if (breakers.Any(o => o.ComponentType == ComponentType.一体式RCD|| o.ComponentType == ComponentType.组合式RCD))
                            {
                                ThreePhaseRCD.Add(edge);
                            }
                            else
                            {
                                ThreePhaseCB.Add(edge);
                            }
                        }
                        else
                        {
                            if (breakers.Any(o => o.ComponentType == ComponentType.一体式RCD|| o.ComponentType == ComponentType.组合式RCD))
                            {
                                OnePhaseRCD.Add(edge);
                            }
                            else
                            {
                                OnePhaseCB.Add(edge);
                            }
                        }
                    }
                    else
                    {
                        if (edge.Target.Load.Phase == ThPDSPhase.三相)
                        {
                            if (breakers.Any(o => o.ComponentType == ComponentType.一体式RCD|| o.ComponentType == ComponentType.组合式RCD))
                            {
                                BackupThreePhaseRCD.Add(edge);
                            }
                            else
                            {
                                BackupThreePhaseCB.Add(edge);
                            }
                        }
                        else
                        {
                            if (breakers.Any(o => o.ComponentType == ComponentType.一体式RCD|| o.ComponentType == ComponentType.组合式RCD))
                            {
                                BackupOnePhaseRCD.Add(edge);
                            }
                            else
                            {
                                BackupOnePhaseCB.Add(edge);
                            }
                        }
                    }
                }
            }
            //单项CB
            {
                int CreatBackupCircuitCount;
                if(OnePhaseCB.Count > 0 && (CreatBackupCircuitCount = (int)Math.Ceiling(OnePhaseCB.Count * 0.2) - BackupOnePhaseCB.Count) > 0)
                {
                    var breakers = OnePhaseCB.SelectMany(o => o.Details.CircuitForm.GetCircuitBreakers());
                    double maxRatedCurrent = breakers.Max(o => double.Parse(o.RatedCurrent));
                    string modeRatedCurrent = breakers.GroupBy(o => o.RatedCurrent).OrderBy(o => o.Count()).ThenBy(o => double.Parse(o.Key)).First().Key;
                    var newCircuits = new List<ThPDSProjectGraphEdge>();
                    for (int i = 0; i < CreatBackupCircuitCount; i++)
                    {
                        newCircuits.Add(AddCircuit(graph, node, CircuitFormOutType.常规, ThPDSPhase.一相));
                    }
                    (newCircuits.First().Details.CircuitForm as Circuit.RegularCircuit).breaker.SetRatedCurrent(maxRatedCurrent.ToString());
                    newCircuits.Skip(1).ForEach(o => (o.Details.CircuitForm as Circuit.RegularCircuit).breaker.SetRatedCurrent(modeRatedCurrent));
                }
            }

            //三相CB
            {
                int CreatBackupCircuitCount;
                if (ThreePhaseCB.Count > 0 && (CreatBackupCircuitCount = (int)Math.Ceiling(ThreePhaseCB.Count * 0.2) - BackupThreePhaseCB.Count) > 0)
                {
                    var breakers = ThreePhaseCB.SelectMany(o => o.Details.CircuitForm.GetCircuitBreakers());
                    double maxRatedCurrent = breakers.Max(o => double.Parse(o.RatedCurrent));
                    string modeRatedCurrent = breakers.GroupBy(o => o.RatedCurrent).OrderBy(o => o.Count()).ThenBy(o => double.Parse(o.Key)).First().Key;
                    var newCircuits = new List<ThPDSProjectGraphEdge>();
                    for (int i = 0; i < CreatBackupCircuitCount; i++)
                    {
                        newCircuits.Add(AddCircuit(graph, node, CircuitFormOutType.常规));
                    }
                    (newCircuits.First().Details.CircuitForm as Circuit.RegularCircuit).breaker.SetRatedCurrent(maxRatedCurrent.ToString());
                    newCircuits.Skip(1).ForEach(o => (o.Details.CircuitForm as Circuit.RegularCircuit).breaker.SetRatedCurrent(modeRatedCurrent));
                }
            }

            //单项RCD
            {
                int CreatBackupCircuitCount;
                if (OnePhaseRCD.Count > 0 && (CreatBackupCircuitCount = (int)Math.Ceiling(OnePhaseRCD.Count * 0.2) - BackupOnePhaseRCD.Count) > 0)
                {
                    var breakers = OnePhaseRCD.SelectMany(o => o.Details.CircuitForm.GetCircuitBreakers());
                    double maxRatedCurrent = breakers.Max(o => double.Parse(o.RatedCurrent));
                    string modeRatedCurrent = breakers.GroupBy(o => o.RatedCurrent).OrderBy(o => o.Count()).ThenBy(o => double.Parse(o.Key)).First().Key;
                    var newCircuits = new List<ThPDSProjectGraphEdge>();
                    for (int i = 0; i < CreatBackupCircuitCount; i++)
                    {
                        newCircuits.Add(AddCircuit(graph, node, CircuitFormOutType.漏电, ThPDSPhase.一相));
                    }
                    (newCircuits.First().Details.CircuitForm as Circuit.LeakageCircuit).breaker.SetRatedCurrent(maxRatedCurrent.ToString());
                    newCircuits.Skip(1).ForEach(o => (o.Details.CircuitForm as Circuit.LeakageCircuit).breaker.SetRatedCurrent(modeRatedCurrent));
                }
            }

            //三相RCD
            {
                int CreatBackupCircuitCount;
                if (ThreePhaseRCD.Count > 0 && (CreatBackupCircuitCount = (int)Math.Ceiling(ThreePhaseRCD.Count * 0.2) - BackupThreePhaseRCD.Count) > 0)
                {
                    var breakers = ThreePhaseRCD.SelectMany(o => o.Details.CircuitForm.GetCircuitBreakers());
                    double maxRatedCurrent = breakers.Max(o => double.Parse(o.RatedCurrent));
                    string modeRatedCurrent = breakers.GroupBy(o => o.RatedCurrent).OrderBy(o => o.Count()).ThenBy(o => double.Parse(o.Key)).First().Key;
                    var newCircuits = new List<ThPDSProjectGraphEdge>();
                    for (int i = 0; i < CreatBackupCircuitCount; i++)
                    {
                        newCircuits.Add(AddCircuit(graph, node, CircuitFormOutType.漏电));
                    }
                    (newCircuits.First().Details.CircuitForm as Circuit.LeakageCircuit).breaker.SetRatedCurrent(maxRatedCurrent.ToString());
                    newCircuits.Skip(1).ForEach(o => (o.Details.CircuitForm as Circuit.LeakageCircuit).breaker.SetRatedCurrent(modeRatedCurrent));
                }
            }
        }

        /// <summary>
        /// 获取未分配负载
        /// </summary>
        /// <param name="FilterEdged">是否过滤已分配回路的负载</param>
        /// <returns></returns>
        public static List<ThPDSProjectGraphNode> GetUndistributeLoad(
            ProjectGraph graph,
            bool FilterEdged = false)
        {
            //分配负载 就是拿到所有的 未知负载
            return graph.Vertices
                .Where(node => node.Type == PDSNodeType.Unkown || (graph.InDegree(node) == 0 && graph.OutDegree(node) == 0))//未知负载/未分配负载
                .Where(node => !FilterEdged || (graph.InDegree(node) == 0 && graph.OutDegree(node) == 0))//已分配回路负载
                .ToList();
        }

        /// <summary>
        /// 分配负载
        /// </summary>
        public static void DistributeLoad(
            ProjectGraph graph,
            ThPDSProjectGraphNode source,
            ThPDSProjectGraphNode target)
        {
            //本身在别的边的负载还不知道怎么处理
            if (graph.InDegree(target) == 0 && graph.OutDegree(target) == 0)
            {
                var newEdge = new ThPDSProjectGraphEdge(source, target) { Circuit = new ThPDSCircuit() };
                newEdge.ComponentSelection();
                graph.AddEdge(newEdge);
                source.CheckWithNode();
            }
        }

        /// <summary>
        /// 新建负载
        /// </summary>
        /// <param name="defaultPhase">项数</param>
        /// <param name="defaultPhaseSequence">相序</param>
        /// <param name="defaultLoadID">设备编号</param>
        /// <param name="defaultPower">设备功率</param>
        /// <param name="defaultDescription">描述信息</param>
        /// <param name="defaultFireLoad">是否消防</param>
        public static ThPDSProjectGraphNode CreatNewLoad(ThPDSPhase defaultPhase = ThPDSPhase.三相, Circuit.PhaseSequence defaultPhaseSequence = Circuit.PhaseSequence.L123, string defaultLoadID = "", double defaultPower = 0, string defaultDescription = "" , bool defaultFireLoad = false)
        {
            //业务逻辑：业务新建的负载，都是空负载，建立不出别的负载
            var node = new ThPDSProjectGraphNode();
            node.Load.Phase = defaultPhase;
            node.Details.PhaseSequence = defaultPhaseSequence;
            node.Load.KV = node.Load.Phase == ThPDSPhase.三相 ? 0.38 : 0.22;
            node.Load.ID.LoadID = defaultLoadID;
            node.Details.HighPower = defaultPower;
            node.Load.ID.Description = defaultDescription;
            node.Load.ID.Description = "备用";
            
            node.Load.SetFireLoad(defaultFireLoad);
            PDSProject.Instance.graphData.Graph.AddVertex(node);
            return node;
        }

        /// <summary>
        /// 编辑负载
        /// </summary>
        /// <param name="load">负载</param>
        /// <param name="defaultPhase">项数</param>
        /// <param name="defaultLoadID">设备编号</param>
        /// <param name="defaultPower">设备功率</param>
        /// <param name="defaultDescription">描述信息</param>
        /// <param name="defaultFireLoad">是否消防</param>
        public static ThPDSProjectGraphNode EditorLoad(ThPDSProjectGraphNode node, ThPDSPhase defaultPhase = ThPDSPhase.三相, string defaultLoadID = "", double defaultPower = 0, string defaultDescription = "", bool defaultFireLoad = false)
        {
            node.Load.Phase = defaultPhase;
            node.Details.PhaseSequence = defaultPhase == ThPDSPhase.三相 ? Circuit.PhaseSequence.L123 : Circuit.PhaseSequence.L1;
            node.Load.KV = node.Load.Phase == ThPDSPhase.三相 ? 0.38 : 0.22;
            node.Load.ID.LoadID = defaultLoadID;
            node.Details.HighPower = defaultPower;
            node.Load.ID.Description = defaultDescription;
            node.Load.SetFireLoad(defaultFireLoad);
            node.Type = node.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.DistributionPanel ? PDSNodeType.DistributionBox : PDSNodeType.Load;

            CheckCascadeWithNode(PDSProject.Instance.graphData.Graph, node);
            return node;
        }


        /// <summary>
        /// 新建回路
        /// </summary>
        public static ThPDSProjectGraphEdge AddCircuit(ProjectGraph graph, ThPDSProjectGraphNode node, CircuitFormOutType type , ThPDSPhase defaultPhase = ThPDSPhase.三相)
        {
            //Step 1:新建空负载
            var target = CreatNewLoad(node.Load.Phase, node.Details.PhaseSequence);
            if (node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.集中电源)
            {
                target.Load.Phase = ThPDSPhase.一相;
                target.Details.PhaseSequence = Circuit.PhaseSequence.L;
            }
            else if (defaultPhase == ThPDSPhase.一相)//三相配电箱搭配一相负载
            {
                target.Load.Phase = ThPDSPhase.一相;
                target.Details.PhaseSequence = Circuit.PhaseSequence.L1;
            }
            //Step  2:新建回路
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() };
            //Step 3:回路选型
            newEdge.ComponentSelection(type);
            //Step 4:添加到Graph
            graph.AddEdge(newEdge);

            return newEdge;
        }

        /// <summary>
        /// 新建回路
        /// </summary>
        public static ThPDSProjectGraphEdge AddCircuit(ProjectGraph graph, ThPDSProjectGraphNode node, string type)
        {
            //Step 1:新建空负载
            var target = CreatNewLoad(node.Load.Phase, node.Details.PhaseSequence);
            //Step 2:新建回路
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() };
            //Step 3:获取对应的CircuitFormOutType
            var CircuitFormOutType = Switch(newEdge, type);
            //Step 4:回路选型
            newEdge.ComponentSelection(CircuitFormOutType);
            //Step 5:添加到Graph
            graph.AddEdge(newEdge);
            if (type.Contains("消防应急照明回路"))
            {
                target.Load.Phase = ThPDSPhase.一相;
                target.Details.PhaseSequence = Circuit.PhaseSequence.L;
                newEdge.Circuit.ID.Description = "疏散照明/指示灯";
            }
            return newEdge;
        }

        /// <summary>
        /// 获取负载节下所有分支
        /// (包含在小母排和控制回路上的分支)
        /// </summary>
        /// <returns></returns>
        public static List<ThPDSProjectGraphEdge> GetCircuit(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            return graph.OutEdges(node).ToList();
        }

        /// <summary>
        /// 获取负载节下所有直接连接分支
        /// (不在小母排/控制回路上的分支)
        /// </summary>
        /// <returns></returns>
        public static List<ThPDSProjectGraphEdge> GetOrdinaryCircuit(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            //所有分支 - 小母排所属分支 - 控制回路所属分支
            return graph.OutEdges(node).Except(node.Details.MiniBusbars.SelectMany(o => o.Value)).Except(node.Details.SecondaryCircuits.SelectMany(o => o.Value)).ToList();
        }

        /// <summary>
        /// 获取小母排节下分支
        /// </summary>
        public static List<ThPDSProjectGraphEdge> GetSmallBusbarCircuit(ProjectGraph graph, ThPDSProjectGraphNode node, MiniBusbar smallBusbar)
        {
            if (node.Details.MiniBusbars.ContainsKey(smallBusbar))
            {
                return node.Details.MiniBusbars[smallBusbar];
            }
            else
            {
                return new List<ThPDSProjectGraphEdge>();
            }
        }

        /// <summary>
        /// 获取控制回路节下所有分支
        /// </summary>
        /// <returns></returns>
        public static List<ThPDSProjectGraphEdge> GetControlCircuit(ProjectGraph graph, ThPDSProjectGraphNode node, SecondaryCircuit secondaryCircuit)
        {
            if (node.Details.SecondaryCircuits.ContainsKey(secondaryCircuit))
            {
                return node.Details.SecondaryCircuits[secondaryCircuit];
            }
            else
            {
                return new List<ThPDSProjectGraphEdge>();
            }
        }

        /// <summary>
        /// 新建小母排
        /// </summary>
        public static void AddSmallBusbar(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var miniBusbar = new MiniBusbar();
            node.Details.MiniBusbars.Add(miniBusbar, new List<ThPDSProjectGraphEdge>());
            var edge = AddCircuit(graph, node, CircuitFormOutType.常规);
            AssignCircuit2SmallBusbar(node, miniBusbar, edge);
        }

        /// <summary>
        /// 删除小母排
        /// </summary>
        public static void DeleteSmallBusbar(ThPDSProjectGraphNode node, MiniBusbar miniBusbar)
        {
            if (node.Details.MiniBusbars.ContainsKey(miniBusbar))
            {
                node.Details.MiniBusbars.Remove(miniBusbar);
            }
        }

        /// <summary>
        /// 小母排新建母排分支
        /// </summary>
        public static void SmallBusbarAddCircuit(ProjectGraph graph, ThPDSProjectGraphNode node, MiniBusbar smallBusbar)
        {
            if (node.Details.MiniBusbars.ContainsKey(smallBusbar))
            {
                var edge = AddCircuit(graph, node, CircuitFormOutType.常规);
                AssignCircuit2SmallBusbar(node, smallBusbar, edge);
            }
        }

        /// <summary>
        /// 获取可并入小母排的回路
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<ThPDSProjectGraphEdge> GetSuitableSmallBusbarCircuit(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            var edges = GetOrdinaryCircuit(graph, node);
            return edges.Where(o => o.Details.CircuitForm.CircuitFormType == CircuitFormOutType.常规).ToList();
        }

        /// <summary>
        /// 指定回路至小母排
        /// </summary>
        public static void AssignCircuit2SmallBusbar(ThPDSProjectGraphNode node, MiniBusbar smallBusbar, ThPDSProjectGraphEdge edge)
        {
            if (node.Details.MiniBusbars.ContainsKey(smallBusbar) && edge.Source.Equals(node) && edge.Details.CircuitForm.CircuitFormType == CircuitFormOutType.常规 && !node.Details.MiniBusbars.Any(o => o.Value.Contains(edge)))
            {
                node.Details.MiniBusbars[smallBusbar].Add(edge);

                if (node.Details.MiniBusbars[smallBusbar].Count == 1)
                {
                    smallBusbar.Phase = edge.Target.Load.Phase;
                    smallBusbar.PhaseSequence = edge.Target.Details.PhaseSequence;
                }
                else if (smallBusbar.PhaseSequence != edge.Target.Details.PhaseSequence || edge.Target.Details.PhaseSequence == Circuit.PhaseSequence.L123)
                {
                    smallBusbar.Phase = ThPDSPhase.三相;
                    smallBusbar.PhaseSequence = Circuit.PhaseSequence.L123;
                }
                else
                {
                    smallBusbar.Phase = ThPDSPhase.一相;
                    smallBusbar.PhaseSequence = edge.Target.Details.PhaseSequence;
                }

                node.CheckWithMiniBusbar(smallBusbar);
                node.CheckWithNode();
            }
        }

        /// <summary>
        /// 获取可选择二次回路
        /// </summary>
        public static List<SecondaryCircuitInfo> GetSecondaryCircuitInfos(ThPDSProjectGraphEdge edge)
        {
            return edge.Target.Load.FireLoad ? SecondaryCircuitConfiguration.FireSecondaryCircuitInfos : SecondaryCircuitConfiguration.NonFireSecondaryCircuitInfos;
        }

        /// <summary>
        /// 添加控制回路
        /// </summary>
        public static SecondaryCircuit AddControlCircuit(ProjectGraph graph, ThPDSProjectGraphEdge edge, SecondaryCircuitInfo secondaryCircuitInfo)
        {
            int index = edge.Source.Details.SecondaryCircuits.Count > 0 ? edge.Source.Details.SecondaryCircuits.Sum(o => o.Value.Count) + 1 : 1;
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);

            var secondaryCircuit = new SecondaryCircuit(index, secondaryCircuitInfo);
            secondaryCircuit.Conductor = componentFactory.GetSecondaryCircuitConductor(secondaryCircuitInfo);

            edge.Source.Details.SecondaryCircuits.Add(secondaryCircuit, new List<ThPDSProjectGraphEdge>());
            AssignCircuit2ControlCircuit(edge.Source, secondaryCircuit, edge);
            return secondaryCircuit;
        }

        /// <summary>
        /// 删除控制回路
        /// </summary>
        public static void DeleteControlCircuit(ThPDSProjectGraphNode node, SecondaryCircuit secondaryCircuit)
        {
            if (node.Details.SecondaryCircuits.ContainsKey(secondaryCircuit))
            {
                node.Details.SecondaryCircuits.Remove(secondaryCircuit);
            }
        }

        /// <summary>
        /// 指定回路至控制回路
        /// </summary>
        public static void AssignCircuit2ControlCircuit(ThPDSProjectGraphNode node, SecondaryCircuit secondaryCircuit, ThPDSProjectGraphEdge edge)
        {
            if (node.Details.SecondaryCircuits.ContainsKey(secondaryCircuit) && edge.Source.Equals(node))
            {
                node.Details.SecondaryCircuits[secondaryCircuit].Add(edge);
            }
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
            if (node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.一路进线)
            {
                return new List<CircuitFormInType>() { CircuitFormInType.二路进线ATSE, CircuitFormInType.三路进线 };
            }
            else if (node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.二路进线ATSE)
            {
                var result = new List<CircuitFormInType>() { CircuitFormInType.三路进线 };
                var inCircuitNumCount = ThPDSCircuitNumberSeacher.Seach(node, graph).Count;
                if (inCircuitNumCount < 2)
                {
                    result.Insert(0, CircuitFormInType.一路进线);
                }
                return result;
            }
            else if (node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.三路进线)
            {
                var result = new List<CircuitFormInType>();
                var inCircuitNumCount = ThPDSCircuitNumberSeacher.Seach(node, graph).Count;
                if (inCircuitNumCount < 2)
                {
                    result.Add(CircuitFormInType.一路进线);
                }
                if (inCircuitNumCount < 3)
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
        public static CircuitFormOutType Switch(ThPDSProjectGraphEdge edge, string circuitName)
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
                if (edge.Target.Details.HighPower < 100)
                    return CircuitFormOutType.配电计量_上海直接表;
                else
                    return CircuitFormOutType.配电计量_上海CT;
            }
            else if (circuitName == "计量(表在前)")
            {
                if (edge.Target.Details.HighPower < 100)
                    return CircuitFormOutType.配电计量_直接表在前;
                else
                    return CircuitFormOutType.配电计量_CT表在前;
            }
            else if (circuitName == "计量(表在后)")
            {
                if (edge.Target.Details.HighPower < 100)
                    return CircuitFormOutType.配电计量_直接表在后;
                else
                    return CircuitFormOutType.配电计量_CT表在后;
            }
            else if (circuitName == "电动机配电回路")
            {
                if (PDSProject.Instance.projectGlobalConfiguration.MotorUIChoise == MotorUIChoise.分立元件)
                {
                    if (edge.Target.Details.HighPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
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
                    if (edge.Target.Details.HighPower <PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                    {
                        return CircuitFormOutType.电动机_CPS;
                    }
                    else
                    {
                        return CircuitFormOutType.电动机_CPS星三角启动;
                    }
                }
            }
            else if (circuitName == "双速电机D-YY")
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
            else if (circuitName == "双速电机Y-Y")
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
            else if (circuitName == "消防应急照明回路（WFEL）")
            {
                return CircuitFormOutType.消防应急照明回路WFEL;
            }
            else
            {
                //其他目前暂不支持
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 导出项目
        /// </summary>
        public static void ExportProject(string filePath, string fileName)
        {
            PDSProject.Instance.ExportProject(filePath, fileName);
        }

        /// <summary>
        /// 导入项目
        /// </summary>
        public static void ImportProject(string filePath)
        {
            PDSProject.Instance.Load(filePath);
        }

        /// <summary>
        /// 导出全局配置
        /// </summary>
        public static void ExportGlobalConfiguration(string filePath, string fileName)
        {
            PDSProject.Instance.ExportGlobalConfiguration(filePath, fileName);
        }

        /// <summary>
        /// 导入全局配置
        /// </summary>
        public static void ImportGlobalConfiguration(string filePath)
        {
            PDSProject.Instance.ImportGlobalConfiguration(filePath);
        }
    }
}
