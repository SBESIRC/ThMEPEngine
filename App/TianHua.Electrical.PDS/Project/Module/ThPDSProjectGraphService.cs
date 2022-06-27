using System;
using QuikGraph;
using System.Linq;
using Dreambuild.AutoCAD;
using QuikGraph.Algorithms;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;
using TianHua.Electrical.PDS.Project.Module.Configure.ComponentFactory;
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
                                Component = componentFactory.CreatOneWayIsolatingSwitch(),
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
        public static void DeleteCircuit(ProjectGraph graph,ThPDSProjectGraphEdge edge)
        {
            //删除回路只删除这个连接关系，前后节点都还保留
            //所以删除后，后面的负载会失去原有的回路,需要人再去分配
            graph.RemoveEdge(edge);
            if (edge.Target.Type == PDSNodeType.Empty || edge.Target.Type == PDSNodeType.Unkown)
            {
                graph.RemoveVertex(edge.Target);
            }
            var minibar = edge.Source.Details.MiniBusbars.FirstOrDefault(o => o.Value.Contains(edge)).Key;
            if (!minibar.IsNull())
            {
                edge.Source.Details.MiniBusbars[minibar].Remove(edge);
                if (edge.Source.Details.MiniBusbars[minibar].Count < 1)
                {
                    DeleteSmallBusbar(edge.Source, minibar);
                }
            }
        }

        /// <summary>
        /// 删除回路
        /// </summary>
        public static void DeleteCircuit(ProjectGraph graph, ThPDSProjectGraphNode source, ThPDSProjectGraphNode target)
        {
            if (source.IsNull())
                return;
            var edge = graph.Edges.FirstOrDefault(o => o.Source == source && o.Target == target);
            DeleteCircuit(graph, edge);
        }

        /// <summary>
        /// 指定上下级连接关系
        /// </summary>
        public static void SpecifyConnectionCircuit(ProjectGraph graph, ThPDSProjectGraphNode source, ThPDSProjectGraphNode target)
        {
            if (source.IsNull())
                return;
            //新建回路
            var newEdge = AddCircuit(graph, source, CircuitFormOutType.常规, target.Load.Phase);
            DistributeLoad(graph, newEdge, target);
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
            catch (NotFoundComponentException ex)
            {
                msg = ex.Message;
                return false;
            }
            catch
            {
                throw;
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
                if (componentType == ComponentType.CB)
                {
                    node.Details.AllowBreakerSwitch = true;
                }
                var Componenttype = componentType.GetComponentType();
                if (component.GetType().BaseType.Equals(Componenttype.BaseType))
                {
                    node.Details.CircuitFormType.SetCircuitComponentValue(component, node.ComponentSelection(Componenttype, node.Details.CircuitFormType.CircuitFormType == CircuitFormInType.一路进线));
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
            var edges = graph.OutEdges(node).Where(edge => !edge.Details.CircuitForm.IsMotorCircuit());
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
        /// 获取未分配的负载
        /// </summary>
        /// <param name="FilterEdged">是否过滤已分配回路的负载</param>
        /// <returns></returns>
        public static List<ThPDSProjectGraphNode> GetUndistributeLoad(
            ProjectGraph graph,
            bool FilterEdged = false)
        {
            //分配负载 就是拿到所有的 未知负载
            return graph.Vertices
                .Where(node => node.Type != PDSNodeType.Unkown && node.Type != PDSNodeType.Empty)
                .Where(node => !FilterEdged || (graph.InDegree(node) == 0))
                .ToList();
        }

        /// <summary>
        /// 分配负载
        /// </summary>
        public static void DistributeLoad(ProjectGraph graph, ThPDSProjectGraphEdge edge, ThPDSProjectGraphNode target)
        {
            var oldLoad = edge.Target;
            if (!oldLoad.Equals(target))
            {
                //新建回路
                var newEdge = new ThPDSProjectGraphEdge(edge.Source, target) { Circuit = edge.Circuit, Details = edge.Details, Tag = edge.Tag };
                graph.AddEdge(newEdge);
                graph.RemoveEdge(edge);
                if (oldLoad.Type == PDSNodeType.Empty || oldLoad.Type == PDSNodeType.Unkown)
                {
                    graph.RemoveVertex(oldLoad);
                }
                newEdge.ComponentSelection();
                newEdge.Source.CheckWithNode();
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
        public static ThPDSProjectGraphNode CreatNewLoad(ThPDSPhase defaultPhase = ThPDSPhase.三相, Circuit.PhaseSequence defaultPhaseSequence = Circuit.PhaseSequence.L123, string defaultLoadID = "", double defaultPower = 0, string defaultDescription = "备用", bool defaultFireLoad = false, ImageLoadType imageLoadType = ImageLoadType.None ,string floorNumber = "1F")
        {
            //业务逻辑：业务新建的负载，都是空负载，建立不出别的负载
            var node = new ThPDSProjectGraphNode();
            node.Load.Phase = defaultPhase;
            node.Details.PhaseSequence = defaultPhaseSequence;
            node.Load.KV = node.Load.Phase == ThPDSPhase.三相 ? 0.38 : 0.22;
            node.Load.ID.LoadID = defaultLoadID;
            node.Details.HighPower = defaultPower;
            node.Load.ID.Description = defaultDescription;
            node.Load.SetFireLoad(defaultFireLoad);
            node.Load.SetLocation(new ThPDSLocation() { FloorNumber = floorNumber });
            switch (imageLoadType)
            {
                case ImageLoadType.None:
                    {
                        node.Type = PDSNodeType.Empty;//空负载
                        break;
                    }
                case ImageLoadType.AL:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.LightingDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.AP:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.PowerDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.ALE:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.EmergencyLightingDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        node.Load.SetFireLoad(true);
                        break;
                    }
                case ImageLoadType.APE:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.EmergencyPowerDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        node.Load.SetFireLoad(true);
                        break;
                    }
                case ImageLoadType.FEL:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.AW:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.ElectricalMeterPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.ACB:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.ElectricalControlPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.RS:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.FireResistantShutter;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.INT:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.IsolationSwitchPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.RD:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.ResidentialDistributionPanel;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.AX:
                    {
                        node.Type = PDSNodeType.DistributionBox;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.DistributionPanel;
                        node.Load.LoadTypeCat_2 = defaultPhase == ThPDSPhase.一相? ThPDSLoadTypeCat_2.OnePhaseSocket: ThPDSLoadTypeCat_2.ThreePhaseSocket;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.Light:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.Luminaire;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.None;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.Socket:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.Socket;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.None;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.AC:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.ACCharger;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.DC:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.DCCharger;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.Motor:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.None;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
                        break;
                    }
                case ImageLoadType.Pump:
                    {
                        node.Type = PDSNodeType.Load;
                        node.Load.LoadTypeCat_1 = ThPDSLoadTypeCat_1.Motor;
                        node.Load.LoadTypeCat_2 = ThPDSLoadTypeCat_2.Pump;
                        node.Load.LoadTypeCat_3 = ThPDSLoadTypeCat_3.SubmersiblePump;
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();//未支持的类型
                    }
            }
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
            var target = CreatNewLoad(node.Load.Phase, node.Details.PhaseSequence, floorNumber:node.Load.Location.FloorNumber);
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
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() { ID = new ThPDSID() { SourcePanelIDList = new List<string>() { node.Load.ID.LoadID } } } };
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
            var target = CreatNewLoad(node.Load.Phase, node.Details.PhaseSequence, floorNumber: node.Load.Location.FloorNumber);
            //Step 2:新建回路
            var newEdge = new ThPDSProjectGraphEdge(node, target) { Circuit = new ThPDSCircuit() { ID = new ThPDSID() { SourcePanelIDList = new List<string>() { node.Load.ID.LoadID } } } };
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
            SelectionComponentFactory componentFactory = new SelectionComponentFactory(edge);
            var secondaryCircuit = new SecondaryCircuit(secondaryCircuitInfo);
            var sameGroupSecondaryCircuits = edge.Source.Details.SecondaryCircuits.Where(o => o.Value.Contains(edge));
            var maxIndex = sameGroupSecondaryCircuits.Count() > 0 ? sameGroupSecondaryCircuits.Max(o => o.Key.Index) + 1 : 1;
            if (int.TryParse(System.Text.RegularExpressions.Regex.Replace(edge.Circuit.ID.CircuitID, @"[^0-9]+", ""), out int circuitIndex))
            {
                secondaryCircuit.CircuitID = $"WC{circuitIndex}-{maxIndex.ToString("00")}";
            }
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
                if (node.Details.SecondaryCircuits[secondaryCircuit].Count > 1)
                {
                    var index = secondaryCircuit.Index;
                    if (index > 0)
                    {
                        secondaryCircuit.CircuitID = $"WC{index.ToString("00")}";
                    }
                }
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
                    //消防
                    if (edge.Target.Load.FireLoad)
                    {
                        if (edge.Target.Details.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                        {
                            return CircuitFormOutType.电动机_分立元件;
                        }
                        else
                        {
                            switch(PDSProject.Instance.projectGlobalConfiguration.FireStartType)
                            {
                                case FireStartType.星三角启动:
                                    {
                                        return CircuitFormOutType.电动机_分立元件星三角启动;
                                    }
                                default:
                                    {
                                        //等后续扩展
                                        return CircuitFormOutType.电动机_分立元件星三角启动;
                                    }
                            }
                        }
                    }
                    else
                    {
                        if (edge.Target.Details.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                        {
                            return CircuitFormOutType.电动机_分立元件;
                        }
                        else
                        {
                            switch (PDSProject.Instance.projectGlobalConfiguration.NormalStartType)
                            {
                                case FireStartType.星三角启动:
                                    {
                                        return CircuitFormOutType.电动机_分立元件星三角启动;
                                    }
                                default:
                                    {
                                        //等后续扩展
                                        return CircuitFormOutType.电动机_分立元件星三角启动;
                                    }
                            }
                        }
                    }
                }
                else
                {
                    //消防
                    if (edge.Target.Load.FireLoad)
                    {
                        if (edge.Target.Details.HighPower < PDSProject.Instance.projectGlobalConfiguration.FireMotorPower)
                        {
                            return CircuitFormOutType.电动机_CPS;
                        }
                        else
                        {
                            switch (PDSProject.Instance.projectGlobalConfiguration.FireStartType)
                            {
                                case FireStartType.星三角启动:
                                    {
                                        return CircuitFormOutType.电动机_CPS星三角启动;
                                    }
                                default:
                                    {
                                        //等后续扩展
                                        return CircuitFormOutType.电动机_CPS星三角启动;
                                    }
                            }
                        }
                    }
                    else
                    {
                        if (edge.Target.Details.HighPower < PDSProject.Instance.projectGlobalConfiguration.NormalMotorPower)
                        {
                            return CircuitFormOutType.电动机_CPS;
                        }
                        else
                        {
                            switch (PDSProject.Instance.projectGlobalConfiguration.NormalStartType)
                            {
                                case FireStartType.星三角启动:
                                    {
                                        return CircuitFormOutType.电动机_CPS星三角启动;
                                    }
                                default:
                                    {
                                        //等后续扩展
                                        return CircuitFormOutType.电动机_CPS星三角启动;
                                    }
                            }
                        }
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
            PDSProjectManagement.ExportProject(filePath, fileName);
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
            PDSProjectManagement.ExportGlobalConfiguration(filePath, fileName);
        }

        /// <summary>
        /// 导入全局配置
        /// </summary>
        public static void ImportGlobalConfiguration(string filePath)
        {
            PDSProjectManagement.ImportGlobalConfiguration(filePath);
        }

        /// <summary>
        /// 自动编号
        /// </summary>
        public static void AutoNumbering(ProjectGraph graph)
        {
            var nodes = graph.Vertices.ToList();
            AutoNumbering(graph, nodes);
        }

        /// <summary>
        /// 自动编号
        /// </summary>
        public static void AutoNumbering(ProjectGraph graph, ThPDSProjectGraphNode node)
        {
            AutoNumbering(graph, new List<ThPDSProjectGraphNode>() { node });
        }

        /// <summary>
        /// 自动编号
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node">Source</param>
        public static void AutoNumbering(ProjectGraph graph, List<ThPDSProjectGraphNode> nodes)
        {
            graph.TopologicalSort().ForEach(node =>
            {
                if (nodes.Contains(node))
                {
                    var edges = graph.OutEdges(node);
                    //回路编号
                    {
                        var eligibleEdges = edges.Where(o => o.Target.Load.CircuitType == ThPDSCircuitType.EmergencyPowerEquipment || o.Target.Load.CircuitType == ThPDSCircuitType.PowerEquipment);
                        var emergencyPowerEquipment = eligibleEdges.Where(o => o.Target.Load.FireLoad);//WPE
                        var powerEquipment = eligibleEdges.Where(o => !o.Target.Load.FireLoad);//WP

                        eligibleEdges = edges.Where(o => o.Target.Load.CircuitType == ThPDSCircuitType.Lighting || o.Target.Load.CircuitType == ThPDSCircuitType.EmergencyLighting);
                        var lighting = eligibleEdges.Where(o => !o.Target.Load.FireLoad);//WL
                        var emergencyLighting = eligibleEdges.Where(o => o.Target.Load.FireLoad);//WLE

                        var fireEmergencyLighting = edges.Where(o => o.Target.Load.CircuitType == ThPDSCircuitType.FireEmergencyLighting);//WFEL
                        var socket = edges.Where(o => o.Target.Load.CircuitType == ThPDSCircuitType.Socket);//WS
                        var otherEdges = edges.Except(emergencyPowerEquipment).Except(powerEquipment).Except(lighting).Except(emergencyLighting).Except(fireEmergencyLighting).Except(socket);
                        var otherOnePhase = otherEdges.Where(o => o.Target.Load.Phase == ThPDSPhase.一相);
                        var otherThreePhase = otherEdges.Where(o => o.Target.Load.Phase == ThPDSPhase.三相);

                        //WPE
                        {
                            var emergencyPowerEquipmentNo = emergencyPowerEquipment.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = emergencyPowerEquipmentNo.Count() > 0 ? emergencyPowerEquipmentNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            emergencyPowerEquipment.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WPE"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }
                        int WPmaxNo = 0;
                        //WP
                        {
                            var powerEquipmentNo = powerEquipment.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            WPmaxNo = powerEquipmentNo.Count() > 0 ? powerEquipmentNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            powerEquipment.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WP"+ (++WPmaxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }
                        int WLmaxNo = 0;
                        //WL
                        {
                            var lightingNo = lighting.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            WLmaxNo = lightingNo.Count() > 0 ? lightingNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            lighting.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WL"+ (++WLmaxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }

                        //WLE
                        {
                            var emergencyLightingNo = emergencyLighting.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = emergencyLightingNo.Count() > 0 ? emergencyLightingNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            emergencyLighting.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WLE"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }

                        //WFEL
                        {
                            var fireEmergencyLightingNo = fireEmergencyLighting.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = fireEmergencyLightingNo.Count() > 0 ? fireEmergencyLightingNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            fireEmergencyLighting.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WFEL"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }

                        //WS
                        {
                            var socketNo = socket.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = socketNo.Count() > 0 ? socketNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0;
                            socket.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WS"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }

                        //WL
                        {
                            var otherOnePhaseNo = otherOnePhase.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = Math.Max(WLmaxNo, otherOnePhaseNo.Count() > 0 ? otherOnePhaseNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0);
                            otherOnePhase.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WL"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }

                        //WP
                        {
                            var otherThreePhaseNo = otherThreePhase.Where(o => !o.Circuit.ID.CircuitID.IsNullOrWhiteSpace());
                            var maxNo = Math.Max(WPmaxNo, otherThreePhaseNo.Count() > 0 ? otherThreePhaseNo.Max(o => int.Parse(System.Text.RegularExpressions.Regex.Replace(o.Circuit.ID.CircuitID.IsNullOrEmpty() ? "0" : o.Circuit.ID.CircuitID, @"[^0-9]+", ""))) : 0);
                            otherThreePhase.Where(o => o.Circuit.ID.CircuitID.IsNullOrWhiteSpace()).ForEach(o =>
                            {
                                o.Circuit.ID.CircuitID = "WP"+ (++maxNo).ToString("00");
                                o.Circuit.ID.SourcePanelID = o.Source.Load.ID.LoadID;
                            });
                        }
                    }
                    //控制回路编号
                    {
                        var singleSecondaryCircuits = node.Details.SecondaryCircuits.Where(o => o.Value.Count == 1);
                        edges.ForEach(edge =>
                        {
                            var secondaryCircuits = singleSecondaryCircuits.Where(o => o.Value.First().Equals(edge));
                            if (int.TryParse(System.Text.RegularExpressions.Regex.Replace(edge.Circuit.ID.CircuitID, @"[^0-9]+", ""), out int circuitIndex))
                            {
                                foreach (var secondarycircuit in secondaryCircuits)
                                {
                                    var index = secondarycircuit.Key.Index;
                                    if (index <= 0)
                                    {
                                        index = secondaryCircuits.Max(o => o.Key.Index) + 1;
                                    }
                                    secondarycircuit.Key.CircuitID = $"WC{circuitIndex}-{index.ToString("00")}";
                                }
                            }
                            else
                            {
                                secondaryCircuits.ForEach(o => o.Key.CircuitID = "");
                            }
                        });
                        var otherSecondaryCircuits = node.Details.SecondaryCircuits.Except(singleSecondaryCircuits);
                        foreach (var secondarycircuit in otherSecondaryCircuits)
                        {
                            var index = secondarycircuit.Key.Index;
                            if (index <= 0)
                            {
                                index = otherSecondaryCircuits.Max(o => o.Key.Index) + 1;
                            }
                            secondarycircuit.Key.CircuitID = $"WC{index.ToString("00")}";
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 接受全部Tag
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns> 
        public static bool AcceptAllTag(ProjectGraph graph)
        {
            return graph.Vertices.All(node => AcceptNodeTag(node)) && graph.Edges.All(edge => AcceptEdgeTag(edge));
        }
        
        /// <summary>
        /// 拒绝全部Tag
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static bool RefuseAllTag(ProjectGraph graph)
        {
            return graph.Vertices.All(node => RefuseNodeTag(node)) && graph.Edges.All(edge => RefuseEdgeTag(edge));
        }

        /*
         * 20220510，通过和张皓以及泽林的沟通，现在暂时只先支持全部支持/全部拒绝，其他的单个节点和回路的拒绝和接受暂不支持
         * 并且，支持和拒绝操作，目前也仅支持Data Change的Accept/Refuse，节点的位置变化则默认为这个一定是用户想要的改变，暂时不允许拒绝
         */
        /// <summary>
        /// 拒绝Node Tag
        /// </summary>
        private static bool RefuseNodeTag(ThPDSProjectGraphNode node)
        {
            try
            {
                // 节点负载数据更新
                if (node.Tag is ThPDSProjectGraphNodeIdChangeTag idTag)
                {
                    if (idTag.ChangeFrom)
                    {
                        node.Load.ID.LoadID = idTag.ChangedID;
                    }
                }
                else if (node.Tag is ThPDSProjectGraphNodeExchangeTag exchangeTag)
                {
                    //交换ID，暂时不知道怎么玩
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (node.Tag is ThPDSProjectGraphNodeDataTag dataTag)
                {
                    if (dataTag.TagP)
                    {
                        node.Load.InstalledCapacity = dataTag.TarP;
                    }
                    if (dataTag.TagD)
                    {
                        node.Load.ID.Description = dataTag.TarD;
                    }
                    if (dataTag.TagF)
                    {
                        node.Load.SetFireLoad(dataTag.TarF);
                    }
                }
                else if (node.Tag is ThPDSProjectGraphNodeDeleteTag deleteTag)
                {
                    //删除节点
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (node.Tag is ThPDSProjectGraphNodeMoveTag moveTag)
                {
                    //移动节点
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (node.Tag is ThPDSProjectGraphNodeAddTag addTag)
                {
                    //增加节点
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (node.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                {
                    var DataTag = compositeTag?.DataTag;
                    if (DataTag != null)
                    {
                        if (DataTag.TagP)
                        {
                            node.Load.InstalledCapacity = DataTag.TarP;
                        }
                        if (DataTag.TagD)
                        {
                            node.Load.ID.Description = DataTag.TarD;
                        }
                        if (DataTag.TagF)
                        {
                            node.Load.SetFireLoad(DataTag.TarF);
                        }
                    }
                }
                node.Tag = null;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 拒绝Edge Tag
        /// </summary>
        private static bool RefuseEdgeTag(ThPDSProjectGraphEdge edge)
        {
            try
            {
                var graph = PDSProject.Instance.graphData.Graph;
                // 节点负载数据更新
                if (edge.Tag is ThPDSProjectGraphEdgeIdChangeTag idTag)
                {
                    if (idTag.ChangeFrom)
                    {
                        edge.Circuit.ID.CircuitID = idTag.ChangedLastCircuitID.Substring(edge.Circuit.ID.SourcePanelID.Length + 1);
                    }
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeDeleteTag deleteTag)
                {
                    //删除回路
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeMoveTag moveTag)
                {
                    //移动回路
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeAddTag addTag)
                {
                    //增加回路
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeCompositeTag compositeTag)
                {
                    //回路复合Tag
                    //2022/05/10 张皓讲此类Tag无法拒绝/接受
                    //do not
                }
                edge.Tag = null;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 接受Node Tag
        /// </summary>
        private static bool AcceptNodeTag(ThPDSProjectGraphNode node)
        {
            try
            {
                // 节点负载数据更新
                if (node.Tag is ThPDSProjectGraphNodeCompareTag idTag)
                {
                    node.Tag = null;
                }
                else if (node.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                {
                    if(compositeTag.DupTag.IsNull() && compositeTag.ValidateTag.IsNull())
                    {
                        node.Tag = null;
                    }
                    else if(compositeTag.DupTag.IsNull())
                    {
                        node.Tag = compositeTag.ValidateTag;
                    }
                    else if(compositeTag.ValidateTag.IsNull())
                    {
                        node.Tag = compositeTag.DupTag;
                    }
                    else
                    {
                        compositeTag.DataTag = null;
                        compositeTag.CompareTag = null;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 接受Edge Tag
        /// </summary>
        private static bool AcceptEdgeTag(ThPDSProjectGraphEdge edge)
        {
            try
            {
                // 节点负载数据更新
                if (edge.Tag is ThPDSProjectGraphEdgeCompareTag)
                {
                    edge.Tag = null;
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeCompositeTag compositeTag)
                {
                    if (compositeTag.DupTag.IsNull() && compositeTag.SingleTag.IsNull())
                    {
                        edge.Tag = null;
                    }
                    else if (compositeTag.DupTag.IsNull())
                    {
                        edge.Tag = compositeTag.SingleTag;
                    }
                    else if (compositeTag.SingleTag.IsNull())
                    {
                        edge.Tag = compositeTag.DupTag;
                    }
                    else
                    {
                        compositeTag.CompareTag = null;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 全局配置保存
        /// </summary>
        /// <returns></returns>
        public static bool GlobalConfigurationUpdate()
        {
            PDSProjectExtend.GlobalConfigurationUpdate();
            return true;
        }

        public static IEnumerable<ThPDSProjectGraphEdge> GetSortedEdges(this IEnumerable<ThPDSProjectGraphEdge> edges)
        {
            return edges.OrderBy(e => e.GetCircuitID().Length == 0 ? 1 : 0).ThenBy(e => ProjectSystemConfiguration.CircuitIDSortNames.IndexOf(ProjectSystemConfiguration.CircuitIDSortNames.FirstOrDefault(x => e.GetCircuitID().ToUpper().StartsWith(x))) + e.GetCircuitID());
        }
    }
}
