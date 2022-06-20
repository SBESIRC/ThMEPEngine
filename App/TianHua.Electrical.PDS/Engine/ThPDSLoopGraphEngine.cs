using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using QuikGraph;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;
using DotNetARX;
using System.IO;
using CircuitGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode,
    TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;
using ThMEPEngineCore.Model.Electrical;

namespace TianHua.Electrical.PDS.Engine
{
    /// <summary>
    /// 回路算法引擎
    /// </summary>
    public class ThPDSLoopGraphEngine
    {
        private ThPDSCircuitGraph PDSGraph;

        private Dictionary<ThPDSCircuitGraphNode, List<ObjectId>> NodeMap;

        private Dictionary<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>, List<ObjectId>> EdgeMap;

        /// <summary>
        /// 全部数据集合
        /// </summary>
        private List<Entity> DataCollection { get; set; }

        /// <summary>
        /// 配电箱集合
        /// </summary>
        private List<Entity> DistBoxes { get; set; }

        /// <summary>
        /// 已捕捉到的配电箱集合
        /// </summary>
        private Dictionary<Entity, ThPDSCircuitGraphNode> CacheDistBoxes { get; set; }

        /// <summary>
        /// 配电箱框线中的配电箱集合
        /// </summary>
        private List<Entity> CacheDistBoxesInFrame { get; set; }

        /// <summary>
        /// 负载集合
        /// </summary>
        private List<Entity> Loads { get; set; }

        /// <summary>
        /// 已捕捉到的负载集合
        /// </summary>
        private List<Entity> CacheLoads { get; set; }

        /// <summary>
        /// 桥架集合
        /// </summary>
        private List<Curve> CableTrays { get; set; }

        /// <summary>
        /// 回路集合
        /// </summary>
        private List<Curve> Cables { get; set; }

        /// <summary>
        /// 配电箱关键字
        /// </summary>
        private List<string> DistBoxKey { get; set; }

        /// <summary>
        /// 配电箱框线
        /// </summary>
        private List<Polyline> DistBoxFrames { get; set; }

        /// <summary>
        /// 是否是标准层
        /// </summary>
        public bool IsStandardStorey { get; set; }

        /// <summary>
        /// 忽略块
        /// </summary>
        private List<Polyline> Ignore;

        /// <summary>
        /// 附着块
        /// </summary>
        private List<Polyline> Attached;

        /// <summary>
        /// 末端块
        /// </summary>
        private List<Polyline> Terminal;

        private ThCADCoreNTSSpatialIndex DistBoxIndex;
        private ThCADCoreNTSSpatialIndex LoadIndex;
        private ThCADCoreNTSSpatialIndex CableIndex;
        private ThCADCoreNTSSpatialIndex CableTrayIndex;
        private ThCADCoreNTSSpatialIndex IgnoreIndex;
        private ThCADCoreNTSSpatialIndex AttachedIndex;
        private ThCADCoreNTSSpatialIndex TerminalIndex;
        public ThPDSCircuitGraphNode CableTrayNode;//桥架节点
        private ThMarkService MarkService;
        private Database Database;

        public static Dictionary<Entity, Entity> GeometryMap;

        public ThPDSLoopGraphEngine(Database database, List<Entity> distBoxes,
            List<KeyValuePair<Entity, ThPDSBlockReferenceData>> loads, List<Curve> cabletrays, List<Curve> cables,
            ThMarkService markService, List<string> distBoxKey, ThPDSCircuitGraphNode cableTrayNode,
            Dictionary<ThPDSCircuitGraphNode, List<ObjectId>> nodeMap,
            Dictionary<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>, List<ObjectId>> edgeMap,
            List<Polyline> distBoxFrames, bool isStandardStorey, List<Polyline> ignore, List<Polyline> attached, List<Polyline> terminal)
        {
            Database = database;
            MarkService = markService;
            DistBoxKey = distBoxKey;
            NodeMap = nodeMap;
            EdgeMap = edgeMap;
            IsStandardStorey = isStandardStorey;
            Ignore = ignore;
            Attached = attached;
            Terminal = terminal;

            using (var acad = AcadDatabase.Use(this.Database))
            {
                DistBoxes = distBoxes;
                Loads = loads.Select(x => x.Key).ToList();
                GeometryMap = new Dictionary<Entity, Entity>();
                CacheDistBoxes = new Dictionary<Entity, ThPDSCircuitGraphNode>();
                CacheDistBoxesInFrame = new List<Entity>();
                CacheLoads = new List<Entity>();
                CableTrays = cabletrays;
                Cables = cables;
                DistBoxFrames = distBoxFrames;

                DistBoxIndex = new ThCADCoreNTSSpatialIndex(DistBoxes.ToCollection());
                CableIndex = new ThCADCoreNTSSpatialIndex(Cables.ToCollection());
                CableTrayIndex = new ThCADCoreNTSSpatialIndex(CableTrays.ToCollection());

                IgnoreIndex = new ThCADCoreNTSSpatialIndex(Ignore.ToCollection());
                AttachedIndex = new ThCADCoreNTSSpatialIndex(Attached.ToCollection());
                TerminalIndex = new ThCADCoreNTSSpatialIndex(Terminal.ToCollection());

                loads.ForEach(x =>
                {
                    if (x.Key is BlockReference block)
                    {
                        var blockName = x.Value.ObjId.GetBlockName();
                        if (blockName.Contains(ThPDSCommon.MOTOR_AND_LOAD_LABELS))
                        {
                            var objs = new DBObjectCollection();
                            block.Explode(objs);
                            var motor = objs.OfType<BlockReference>().First();
                            GeometryMap.Add(block, motor);
                        }
                        else if (blockName.Equals("E-BDB054"))
                        {
                            var objs = new DBObjectCollection();
                            block.Explode(objs);
                            var motor = objs.OfType<Circle>().First().TessellateCircleWithArc(200.00);
                            GeometryMap.Add(block, motor);
                        }
                        else
                        {
                            var obb = block.BlockOBB();
                            if (!obb.Bounds.HasValue)
                            {
                                GeometryMap.Add(block, block);
                            }
                            else
                            {
                                GeometryMap.Add(block, block.BlockOBB());
                            }
                        }
                    }
                    else
                    {
                        GeometryMap.Add(x.Key, x.Key);
                    }
                });
                LoadIndex = new ThCADCoreNTSSpatialIndex(GeometryMap.Values.ToCollection());

                PDSGraph = new ThPDSCircuitGraph
                {
                    Graph = new BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>()
                };
                CableTrayNode = cableTrayNode;
                PDSGraph.Graph.AddVertex(CableTrayNode);
            }
        }

        public void MultiDistBoxAnalysis()
        {
            DistBoxFrames.ForEach(frame =>
            {
                // 搜索框线中的配电箱
                var cacheNodes = new List<ThPDSCircuitGraphNode>();
                var distBoxes = DistBoxIndex.SelectCrossingPolygon(frame);

                var bufferFrame = ThPDSBufferService.Buffer(frame);
                var onLightingCableTray = false;
                var cableTray = CableTrayIndex.SelectCrossingPolygon(bufferFrame).OfType<Curve>().FirstOrDefault();
                if (!cableTray.IsNull() && ThPDSLayerService.LightingCableTrayLayer().Contains(cableTray.Layer))
                {
                    onLightingCableTray = true;
                }

                // 搜索框线周围的标注
                var markList = MarkService.GetMultiMarks(bufferFrame);
                MarkService.InfosClean(markList);

                var cacheDistBoxes = new List<BlockReference>();
                var cacheMarkList = new List<ThPDSTextInfo>();
                for (var i = 0; i < 2; i++)
                {
                    distBoxes.OfType<BlockReference>().ForEach(distBox =>
                    {
                        if (cacheDistBoxes.Contains(distBox))
                        {
                            return;
                        }

                        var distBoxKeyList = new List<string>();
                        var distBoxKey = "";
                        ThPDSGraphService.DistBoxBlocks[distBox].Attributes.ForEach(attri =>
                        {
                            DistBoxKey.ForEach(key =>
                            {
                                if (attri.Value.Contains(key))
                                {
                                    distBoxKey = attri.Value;
                                }
                                else if (attri.Value.Contains("K") || attri.Value.Contains("INT"))
                                {
                                    distBoxKeyList.Add(key);
                                }
                            });
                        });

                        distBoxKey = distBoxKey.Replace("~", "/");
                        if (distBoxKey.Contains("/"))
                        {
                            var regex = new Regex(@".+[/]");
                            var match = regex.Match(distBoxKey);
                            if (match.Success)
                            {
                                distBoxKeyList.Add(match.Value.Replace("/", ""));
                                var secRegex = new Regex(@".{1}[/]");
                                var secMatch = secRegex.Match(distBoxKey);
                                if (secMatch.Success)
                                {
                                    distBoxKeyList.Add(distBoxKey.Replace(secMatch.Value, ""));
                                }
                            }
                        }
                        else
                        {
                            distBoxKeyList.Add(distBoxKey);
                        }
                        var thisMark = new ThPDSTextInfo();
                        var thisCircuitMark = new ThPDSTextInfo();
                        var buffer = ThPDSBufferService.Buffer(distBox, Database);
                        var privateMark = MarkService.GetMarks(buffer);
                        privateMark.Texts.ForEach(o =>
                        {
                            if (o.Contains("/W") || o.Contains("-W"))
                            {
                                thisCircuitMark.Texts.Add(o);
                                thisCircuitMark.ObjectIds.AddRange(privateMark.ObjectIds);
                                thisCircuitMark.ObjectIds = thisMark.ObjectIds.Distinct().ToList();
                            }
                        });
                        distBoxKeyList.ForEach(key =>
                        {
                            foreach (var mark in markList)
                            {
                                if (cacheMarkList.Contains(mark))
                                {
                                    continue;
                                }
                                var strMatch = false;
                                foreach (var str in mark.Texts)
                                {
                                    if (str.Contains("/W") || str.Contains("-W"))
                                    {
                                        continue;
                                    }
                                    if (str.Contains(key))
                                    {
                                        // 第一次做严格匹配，第二次模糊匹配 具体匹配细节存疑
                                        if (i == 0)
                                        {
                                            if (key.Count() > 2)
                                            {
                                                var regex = new Regex(@key + "[a-zA-Z0-9]{0,}");
                                                var match = regex.Match(str);
                                                if (match.Success)
                                                {
                                                    strMatch = true;
                                                }
                                            }
                                            if (privateMark.Texts.Contains(str) && !str.Contains(key + "E"))
                                            {
                                                strMatch = true;
                                            }
                                        }
                                        else
                                        {
                                            strMatch = true;
                                        }
                                    }
                                }
                                if (strMatch)
                                {
                                    thisMark.Texts.AddRange(mark.Texts);
                                    thisMark.ObjectIds.AddRange(mark.ObjectIds);
                                    thisMark.ObjectIds = thisMark.ObjectIds.Distinct().ToList();
                                    cacheMarkList.Add(mark);
                                    break;
                                }
                            }
                            if (thisMark.Texts.Count == 0 && i == 0)
                            {
                                return;
                            }
                            thisMark.Texts.AddRange(thisCircuitMark.Texts);
                            thisMark.ObjectIds.AddRange(thisCircuitMark.ObjectIds);
                            var newNode = ThPDSGraphService.CreateNode(distBox, thisMark.Texts, DistBoxKey, buffer);
                            newNode.Loads[0].SetOnLightingCableTray(onLightingCableTray);
                            cacheNodes.Add(newNode);
                            cacheDistBoxes.Add(distBox);
                            if (!CacheDistBoxes.ContainsKey(distBox))
                            {
                                CacheDistBoxes.Add(distBox, newNode);
                                CacheDistBoxesInFrame.Add(distBox);
                            }

                            PDSGraph.Graph.AddVertex(newNode);
                            NodeMap.Add(newNode, thisMark.ObjectIds);

                            newNode.Loads[0].ID.CircuitNumberList.ForEach(number =>
                            {
                                var newEdge = ThPDSGraphService.CreateEdge(CableTrayNode, newNode, new List<string> { number }, DistBoxKey);
                                PDSGraph.Graph.AddEdge(newEdge);
                                // 此时节点需要和桥架建立多条回路，由于在dictionary中是通过判断两个节点是否都相同，
                                // 进而判断两个edge是否相同的，所以此时dictionary认为它们是同一个key
                                // 故需要对这种情况特殊处理，将所有回路图元ObjectId添加到一个key中
                                if (EdgeMap.ContainsKey(newEdge))
                                {
                                    EdgeMap[newEdge].AddRange(thisMark.ObjectIds.Distinct().ToList());
                                    EdgeMap[newEdge] = EdgeMap[newEdge].Distinct().ToList();
                                }
                                else
                                {
                                    EdgeMap.Add(newEdge, thisMark.ObjectIds);
                                }
                            });
                        });
                    });
                }

                var emergencyLightings = cacheNodes.Where(node => node.Loads[0].LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.EmergencyLightingDistributionPanel)).ToList();
                var fireEmergencyLightings = cacheNodes.Where(node => node.Loads[0].LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)).ToList();
                if (emergencyLightings.Count > 0 && fireEmergencyLightings.Count > 0)
                {
                    fireEmergencyLightings.ForEach(target =>
                    {
                        var source = emergencyLightings[0];
                        if (emergencyLightings.Count > 0)
                        {
                            source = ThPDSLoadIDMatchService.Match(target, emergencyLightings);
                        }
                        var newEdge = ThPDSGraphService.CreateEdge(emergencyLightings[0], target, new List<string>(), DistBoxKey, true);
                        PDSGraph.Graph.AddEdge(newEdge);
                        if (EdgeMap.ContainsKey(newEdge))
                        {
                            EdgeMap[newEdge].AddRange(NodeMap[target]);
                        }
                        else
                        {
                            EdgeMap.Add(newEdge, NodeMap[target]);
                        }
                    });
                }
            });
        }

        /// <summary>
        /// 初始化图
        /// </summary>
        public void CreatGraph()
        {
            foreach (var cabletray in CableTrays)
            {
                FindGraph(null, cabletray);
            }
            foreach (var cabletray in DistBoxFrames)
            {
                FindGraph(null, cabletray);
            }
            foreach (var cabletray in CableTrays)
            {
                FindDistBoxOnTrayGraph(null, cabletray);
            }
            foreach (var distBox in DistBoxes)
            {
                if (CacheDistBoxesInFrame.Contains(distBox) || !CacheDistBoxes.ContainsKey(distBox))
                {
                    FindGraph(null, distBox);
                }
            }
            foreach (var load in Loads)
            {
                if (!CacheLoads.Contains(load))
                {
                    FindGraph(load);
                }
            }
        }

        /// <summary>
        /// 根据节点开始寻图
        /// 实现根据节点，寻找该节点所能到达回路的方法
        /// </summary>
        /// <param name="startingEntity"></param>
        public void FindGraph(Entity extraEntity, Entity startingEntity)
        {
            //是配电箱
            if (startingEntity is BlockReference blockObj)
            {
                ThPDSCircuitGraphNode node;
                if (!CacheDistBoxes.ContainsKey(startingEntity))
                {
                    var objectIds = new List<ObjectId>();
                    node = ThPDSGraphService.CreateNode(startingEntity, Database, MarkService, DistBoxKey, objectIds);
                    CacheDistBoxes.Add(startingEntity, node);
                    PDSGraph.Graph.AddVertex(node);
                    NodeMap.Add(node, objectIds);
                }
                else
                {
                    node = CacheDistBoxes[startingEntity];
                    CacheDistBoxesInFrame.Remove(startingEntity);
                }
                var polyline = ThPDSBufferService.Buffer(blockObj, Database);
                var results = FindNextLine(startingEntity, polyline);
                results.Remove(extraEntity);
                //配电箱搭着线
                foreach (Curve findcurve in results)
                {
                    //线得搭到块上才可遍历，否则认为线只是跨过块
                    if (polyline.Contains(findcurve.StartPoint) || polyline.Contains(findcurve.EndPoint))
                    {
                        PrepareNavigate(node, new List<Entity>(), startingEntity, findcurve);
                    }
                }

                results = FindNextDistBox(blockObj, polyline);
                results.Remove(extraEntity);
                //配电箱搭着配电箱
                foreach (var distBox in results)
                {
                    if (!CacheDistBoxes.ContainsKey(distBox))
                    {
                        var objectIds = new List<ObjectId>();
                        var newNode = ThPDSGraphService.CreateNode(distBox, Database, MarkService, DistBoxKey, objectIds);
                        // 当上级配电箱为ALE，下级配电箱为FEL时，才认为配电箱直接相连
                        if (node.Loads[0].LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.EmergencyLightingDistributionPanel)
                            && newNode.Loads[0].LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel))
                        {
                            CacheDistBoxes.Add(distBox, newNode);
                            PDSGraph.Graph.AddVertex(newNode);
                            NodeMap.Add(newNode, objectIds);

                            newNode.Loads[0].ID.CircuitNumberList.ForEach(circuitNumber =>
                            {
                                if (string.IsNullOrEmpty(circuitNumber))
                                {
                                    return;
                                }

                                var newEdge = ThPDSGraphService.CreateEdge(node, newNode, new List<string> { circuitNumber }, DistBoxKey);
                                if (ThPDSEdgeContainsService.EdgeContains(newEdge, PDSGraph.Graph))
                                {
                                    return;
                                }
                                var newOBB = ThPDSBufferService.Buffer(distBox, Database);
                                var filter = CableTrayIndex.SelectCrossingPolygon(newOBB);
                                if (filter.Count > 0)
                                {
                                    newEdge.Circuit.ViaCableTray = true;
                                }
                                else
                                {
                                    newEdge.Circuit.ViaConduit = true;
                                }
                                PDSGraph.Graph.AddEdge(newEdge);
                                EdgeMap.Add(newEdge, objectIds);
                            });

                            FindGraph(startingEntity, distBox);
                        }
                    }
                }

                results = FindNextLoad(blockObj, polyline);
                results.Remove(extraEntity);
                //配电箱搭着负载
                foreach (var load in results)
                {
                    if (!CacheLoads.Contains(load))
                    {
                        CacheLoads.Add(load);
                        PrepareNavigate(node, new List<Entity> { load }, startingEntity, load);
                    }
                }
            }
            //是桥架
            else if (startingEntity is Curve curve)
            {
                var onLightingCableTray = ThPDSLayerService.LightingCableTrayLayer().Contains(curve.Layer);
                var polyline = ThPDSBufferService.Buffer(curve, Database);
                // 首先遍历从桥架搭出去的线
                var results = FindNextLine(curve, polyline).OfType<Curve>();
                foreach (var findCurve in results)
                {
                    var IsStart = findCurve.StartPoint.DistanceTo(curve.GetClosestPointTo(findCurve.StartPoint, false))
                        < ThPDSCommon.ALLOWABLE_TOLERANCE;
                    var IsEnd = findCurve.EndPoint.DistanceTo(curve.GetClosestPointTo(findCurve.EndPoint, false))
                        < ThPDSCommon.ALLOWABLE_TOLERANCE;
                    //都不相邻即无关系，都相邻即近似平行，都不符合
                    if (IsStart != IsEnd)
                    {
                        PrepareNavigate(CableTrayNode, new List<Entity>(), curve, findCurve,
                            onLightingCableTray);
                    }
                }

                // 遍历桥架内部的灯具负载
                if (!onLightingCableTray)
                {
                    return;
                }
                var loads = FindNextLoad(curve, polyline);
                loads.ForEach(x =>
                {
                    if (CacheLoads.Contains(x))
                    {
                        return;
                    }
                    var attributesCopy = "";
                    var objectIds = new List<ObjectId>();
                    var newNode = ThPDSGraphService.CreateNode(x, Database, MarkService, DistBoxKey,
                        objectIds, ref attributesCopy);
                    CacheLoads.Add(x);
                    if (newNode.Loads.Count > 0)
                    {
                        PDSGraph.Graph.AddVertex(newNode);
                        NodeMap.Add(newNode, objectIds);
                    }
                });
            }
        }

        public void FindGraph(Entity load)
        {
            var objectIds = new List<ObjectId>();
            var attributesCopy = "";
            var newNode = ThPDSGraphService.CreateNode(new List<Entity> { load }, Database, MarkService,
                DistBoxKey, objectIds, ref attributesCopy);
            PDSGraph.Graph.AddVertex(newNode);
            NodeMap.Add(newNode, objectIds);
        }

        public void FindDistBoxOnTrayGraph(Entity extraEntity, Entity startingEntity)
        {
            if (startingEntity is Curve curve)
            {
                var onLightingCableTray = ThPDSLayerService.LightingCableTrayLayer().Contains(curve.Layer);
                var polyline = ThPDSBufferService.Buffer(curve, Database);
                // 遍历挂在桥架上的配电箱
                var distBoxes = FindNextDistBox(curve, polyline);
                foreach (var distBox in distBoxes)
                {
                    if (!CacheDistBoxes.ContainsKey(distBox))
                    {
                        var objectIds = new List<ObjectId>();
                        var newNode = ThPDSGraphService.CreateNode(distBox, Database, MarkService, DistBoxKey, objectIds);
                        newNode.Loads[0].SetOnLightingCableTray(onLightingCableTray);
                        CacheDistBoxes.Add(distBox, newNode);
                        PDSGraph.Graph.AddVertex(newNode);
                        NodeMap.Add(newNode, objectIds);

                        newNode.Loads[0].ID.CircuitNumberList.ForEach(x =>
                        {
                            // new List<string> { newNode.Loads[0].ID.CircuitNumber } 可能会有bug
                            var newEdge = ThPDSGraphService.CreateEdge(CableTrayNode, newNode, new List<string> { x }, DistBoxKey);
                            if (ThPDSEdgeContainsService.EdgeContainsEx(newEdge, PDSGraph.Graph))
                            {
                                return;
                            }
                            PDSGraph.Graph.AddEdge(newEdge);
                            if (!EdgeMap.ContainsKey(newEdge))
                            {
                                EdgeMap.Add(newEdge, objectIds);
                            }
                            else
                            {
                                EdgeMap[newEdge].AddRange(objectIds);
                            }
                        });

                        FindGraph(startingEntity, distBox);
                    }
                    else
                    {
                        var node = CacheDistBoxes[distBox];
                        node.Loads[0].ID.CircuitNumberList.ForEach(x =>
                        {
                            // new List<string> { newNode.Loads[0].ID.CircuitNumber } 可能会有bug
                            var edge = ThPDSGraphService.CreateEdge(CableTrayNode, node, new List<string> { x }, DistBoxKey);
                            if (ThPDSEdgeContainsService.EdgeContainsEx(edge, PDSGraph.Graph))
                            {
                                return;
                            }
                            PDSGraph.Graph.AddEdge(edge);
                            if (!EdgeMap.ContainsKey(edge))
                            {
                                EdgeMap.Add(edge, new List<ObjectId>());
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 寻路（核心算法）
        /// </summary>
        public void PrepareNavigate(ThPDSCircuitGraphNode node, List<Entity> loads,
            Entity sourceEntity, Entity nextEntity, bool onLightingCableTray = false)
        {
            var findLoop = FindRootNextElement(sourceEntity, nextEntity, out var isBranch);
            var entityList = new List<Tuple<Entity, Entity, ThPDSTextInfo, List<Entity>>>();
            if (isBranch)
            {
                foreach (var item in findLoop)
                {
                    var newLoads = new List<Entity>();
                    loads.ForEach(x => newLoads.Add(x));
                    var logos = new ThPDSTextInfo();

                    // 搜索回路标注
                    item.Value.ForEach(curve =>
                    {
                        var marks = MarkService.GetMarks(ThPDSBufferService.Buffer(curve, Database));
                        logos.Texts.AddRange(marks.Texts);
                        logos.ObjectIds.AddRange(marks.ObjectIds);
                    });

                    if (DistBoxes.Contains(item.Key))
                    {
                        entityList.Add(Tuple.Create(sourceEntity, nextEntity, new ThPDSTextInfo(), loads));
                    }
                    else if (Loads.Contains(item.Key))
                    {
                        //搭着负载
                        if (!CacheLoads.Contains(item.Key))
                        {
                            newLoads.Add(item.Key);
                            CacheLoads.Add(item.Key);
                            var nextLoops = new List<Entity>();
                            if (item.Key is Curve curve)
                            {
                                nextLoops = FindNext(curve, ThPDSBufferService.Buffer(curve, Database));
                            }
                            else if (item.Key is BlockReference block)
                            {
                                if (GeometryMap.ContainsKey(block))
                                {
                                    nextLoops = FindNext(block, ThPDSBufferService.Buffer(GeometryMap[block], Database));
                                }
                                else
                                {
                                    nextLoops = FindNext(block, ThPDSBufferService.Buffer(block, Database));
                                }
                            }
                            if (item.Value.Count > 0)
                            {
                                nextLoops.Remove(item.Value.Last());
                            }
                            else
                            {
                                nextLoops.Remove(nextEntity);
                            }
                            foreach (var entity in nextLoops)
                            {
                                //这就是自己本身延伸出去的块
                                entityList.Add(Tuple.Create(item.Key, entity, logos, newLoads));
                            }
                            if (newLoads.Count > 0 && nextLoops.Count == 0)
                            {
                                newLoads = RemoveIgnoreAndTerminal(newLoads);
                                if (newLoads.Count > 0)
                                {
                                    Navigate(node, newLoads, onLightingCableTray, logos, nextEntity);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                entityList.Add(Tuple.Create(sourceEntity, nextEntity, new ThPDSTextInfo(), loads));
            }

            entityList.ForEach(tuple =>
            {
                var distributionBox = Navigate(node, tuple.Item4, tuple.Item3, tuple.Item1, tuple.Item2);
                if (tuple.Item4.Count > 0)
                {
                    // 控制单个节点中潜水泵的数量
                    var containsPump = false;
                    var removePump = new List<Entity>();
                    for (var i = 0; i < tuple.Item4.Count; i++)
                    {
                        if (tuple.Item4[i] is BlockReference block)
                        {
                            if (block.Name.Equals("E-BDB054"))
                            {
                                if (!containsPump)
                                {
                                    containsPump = true;
                                }
                                else
                                {
                                    removePump.Add(block);
                                }
                            }
                        }
                    }
                    if (containsPump)
                    {
                        removePump.ForEach(pump =>
                        {
                            tuple.Item4.Remove(pump);
                            CacheLoads.Remove(pump);
                        });
                    }

                    var newLoads = RemoveIgnoreAndTerminal(tuple.Item4);
                    if (newLoads.Count == 0)
                    {
                        return;
                    }
                    var navigate = Navigate(node, newLoads, onLightingCableTray, tuple.Item3, nextEntity);
                    if (!navigate.Item1)
                    {
                        return;
                    }
                    distributionBox.ForEach(box =>
                    {
                        var distBoxNode = CacheDistBoxes[box.Item2];
                        var newDistBoxEdge = ThPDSGraphService.CreateEdge(distBoxNode, navigate.Item2, box.Item3.Texts, DistBoxKey);
                        if (navigate.Item3.Target.Loads[0].CircuitType == ThPDSCircuitType.None && box.Item1 is Line otherCircuit)
                        {
                            var needAssign = false;
                            if (newDistBoxEdge.Target.Loads.Count == 0)
                            {
                                newDistBoxEdge.Target.Loads.Add(new ThPDSLoad());
                                needAssign = true;
                            }
                            ThPDSLayerService.SelectCircuitType(newDistBoxEdge.Target.Loads[0],
                                otherCircuit.Layer, needAssign);
                        }
                        if (ThPDSEdgeContainsService.EdgeContains(newDistBoxEdge, PDSGraph.Graph))
                        {
                            return;
                        }
                        PDSGraph.Graph.AddEdge(newDistBoxEdge);
                        EdgeMap.Add(newDistBoxEdge, box.Item3.ObjectIds);

                        FindGraph(box.Item1, box.Item2);
                    });
                }
            });
        }

        private Tuple<bool, ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Navigate
            (ThPDSCircuitGraphNode node, List<Entity> loads, bool onLightingCableTray, ThPDSTextInfo logos, Entity nextEntity)
        {
            var attributesCopy = "";
            var objectIds = new List<ObjectId>();
            var newNode = ThPDSGraphService.CreateNode(loads, Database, MarkService, DistBoxKey,
                objectIds, ref attributesCopy);
            if (onLightingCableTray)
            {
                newNode.Loads[0].SetOnLightingCableTray(onLightingCableTray);
            }
            PDSGraph.Graph.AddVertex(newNode);
            NodeMap.Add(newNode, objectIds);

            if (!string.IsNullOrEmpty(attributesCopy))
            {
                newNode.Loads[0].AttributesCopy = attributesCopy;
            }

            var textList = new List<List<string>>();
            logos.Texts = logos.Texts.Distinct().ToList();
            // 对标准层特殊处理
            if (IsStandardStorey)
            {
                logos.Texts.ForEach(t =>
                {
                    if (t.Contains("~"))
                    {
                        var regex = new Regex(@"[0-9]+~[a-zA-Z]*[0-9]+");
                        var match = regex.Match(t);
                        if (match.Success)
                        {
                            var numRegex = new Regex(@"[0-9]+");
                            var numMatch = numRegex.Match(match.Value);
                            if (numMatch.Success)
                            {
                                var start = Convert.ToInt16(numMatch.Value);
                                numMatch = numMatch.NextMatch();
                                if (numMatch.Success)
                                {
                                    var end = Convert.ToInt16(numMatch.Value);
                                    for (var i = start; i <= end; i++)
                                    {
                                        var numString = i.ToString();
                                        if (i < 10)
                                        {
                                            numString = "0" + i;
                                        }
                                        textList.Add(new List<string> { t.Replace(match.Value, numString) });
                                    }
                                }
                            }
                        }
                    }
                });
            }
            if (textList.Count == 0)
            {
                textList.Add(logos.Texts);
            }

            if (textList.Count == 1)
            {
                var newEdge = ThPDSGraphService.CreateEdge(node, newNode, textList[0], DistBoxKey);
                if (newEdge.Target.Loads[0].CircuitType == ThPDSCircuitType.None && nextEntity is Line circuit)
                {
                    ThPDSLayerService.SelectCircuitType(newEdge.Target.Loads[0], circuit.Layer, true);
                }
                if (ThPDSEdgeContainsService.EdgeContains(newEdge, PDSGraph.Graph))
                {
                    return Tuple.Create(false, newNode, newEdge);
                }
                PDSGraph.Graph.AddEdge(newEdge);
                EdgeMap.Add(newEdge, logos.ObjectIds);
                return Tuple.Create(true, newNode, newEdge);
            }
            else
            {
                var edges = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
                for (var i = 0; i < textList.Count; i++)
                {
                    var otherNode = new ThPDSCircuitGraphNode();
                    if (i == 0)
                    {
                        otherNode = newNode;
                    }
                    else
                    {
                        otherNode = ThPDSGraphService.NodeClone(newNode);
                        PDSGraph.Graph.AddVertex(otherNode);
                    }
                    var newEdge = ThPDSGraphService.CreateEdge(node, otherNode, textList[i], DistBoxKey);
                    if (newEdge.Target.Loads[0].CircuitType == ThPDSCircuitType.None && nextEntity is Line circuit)
                    {
                        ThPDSLayerService.SelectCircuitType(newEdge.Target.Loads[0], circuit.Layer, true);
                    }
                    if (ThPDSEdgeContainsService.EdgeContains(newEdge, PDSGraph.Graph))
                    {
                        continue;
                    }
                    PDSGraph.Graph.AddEdge(newEdge);
                    EdgeMap.Add(newEdge, logos.ObjectIds);
                    edges.Add(newEdge);
                }
                return Tuple.Create(true, newNode, edges[0]);
            }
        }

        /// <summary>
        /// 寻路算法，sourceEntity表示连接上级，nextEntity表示自身
        /// </summary>
        public List<Tuple<Entity, Entity, ThPDSTextInfo>> Navigate(ThPDSCircuitGraphNode node, List<Entity> loads,
            ThPDSTextInfo logos, Entity sourceEntity, Entity nextEntity)
        {
            var results = new List<Tuple<Entity, Entity, ThPDSTextInfo>>();
            var findLoop = FindRootNextElement(sourceEntity, nextEntity, out var isBranch);

            if (findLoop.Count == 0)
            {
                return results;
            }

            foreach (var item in findLoop)
            {
                // 搜索回路标注
                item.Value.ForEach(curve =>
                {
                    var marks = MarkService.GetMarks(ThPDSBufferService.Buffer(curve, Database));
                    logos.Texts.AddRange(marks.Texts);
                    logos.ObjectIds.AddRange(marks.ObjectIds);
                });

                if (DistBoxes.Contains(item.Key))
                {
                    ThPDSCircuitGraphNode newNode;
                    if (!CacheDistBoxes.ContainsKey(item.Key))
                    {
                        var objectIds = new List<ObjectId>();
                        newNode = ThPDSGraphService.CreateNode(item.Key, Database, MarkService, DistBoxKey, objectIds);
                        CacheDistBoxes.Add(item.Key, newNode);
                        PDSGraph.Graph.AddVertex(newNode);
                        NodeMap.Add(newNode, objectIds);
                    }
                    else
                    {
                        newNode = CacheDistBoxes[item.Key];
                        CacheDistBoxesInFrame.Remove(item.Key);
                    }

                    if (DistBoxes.Contains(sourceEntity) || CableTrays.Contains(sourceEntity))
                    {
                        //搭着配电箱
                        var newEdge = ThPDSGraphService.CreateEdge(node, newNode, logos.Texts, DistBoxKey, true);
                        if (item.Value.Count > 0)
                        {
                            newEdge.Circuit.ViaConduit = true;
                            if (node.NodeType == PDSNodeType.CableCarrier)
                            {
                                newEdge.Circuit.ViaCableTray = true;
                            }
                        }
                        if (!ThPDSEdgeContainsService.EdgeContainsAndInstand(newEdge, PDSGraph.Graph, EdgeMap, logos.ObjectIds))
                        {
                            PDSGraph.Graph.AddEdge(newEdge);
                            EdgeMap.Add(newEdge, logos.ObjectIds);
                            if (item.Value.Count > 0)
                            {
                                FindGraph(item.Value.Last(), item.Key);
                            }
                            else
                            {
                                FindGraph(nextEntity, item.Key);
                            }
                        }
                    }
                    else
                    {
                        //负载搭着配电箱
                        if (item.Value.Count > 0)
                        {
                            results.Add(Tuple.Create(item.Value.Last() as Entity, item.Key, logos));
                        }
                        else
                        {
                            results.Add(Tuple.Create(nextEntity, item.Key, logos));
                        }
                    }
                }
                else if (Loads.Contains(item.Key))
                {
                    //搭着负载
                    if (!CacheLoads.Contains(item.Key))
                    {
                        loads.Add(item.Key);
                        CacheLoads.Add(item.Key);
                        var nextLoops = FindNext(item.Key, ThPDSBufferService.Buffer(GeometryMap[item.Key], Database));
                        if (item.Value.Count > 0)
                        {
                            nextLoops.Remove(item.Value.Last());
                        }
                        else
                        {
                            nextLoops.Remove(nextEntity);
                        }
                        foreach (var entity in nextLoops)
                        {
                            //这就是自己本身延伸出去的块
                            results.AddRange(Navigate(node, loads, new ThPDSTextInfo(), item.Key, entity));
                        }
                    }
                }
                else
                {
                    //未知负载
                    loads.Add(item.Key);
                }
            }
            return results;
        }

        /// <summary>
        /// 由根节点找到下个元素(块)
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="specifyElement"></param>
        /// <returns></returns>
        public Dictionary<Entity, List<Curve>> FindRootNextElement(Entity rootElement, Entity specifyElement, out bool isBranch)
        {
            isBranch = false;
            var NextElement = new Dictionary<Entity, List<Curve>>();
            if (specifyElement is BlockReference blk)
            {
                return new Dictionary<Entity, List<Curve>>
                {
                    { blk, new List<Curve>() },
                };
            }
            //线需要寻块，且要考虑到一条线延伸多条线的情况
            else if (specifyElement is Curve curve)
            {
                var sharedpath = new List<Curve>();
                //配电箱
                if (rootElement is BlockReference rootBlk)
                {
                    var obb = ThPDSBufferService.Buffer(rootBlk, Database, 0);
                    //起点连着块
                    if (obb.Distance(curve.StartPoint) < obb.Distance(curve.EndPoint))
                    {
                        NextElement = FindRootNextPath(rootBlk, sharedpath, curve, false, out isBranch);
                    }
                    //终点连着块
                    else
                    {
                        NextElement = FindRootNextPath(rootBlk, sharedpath, curve, true, out isBranch);
                    }
                }
                //桥架
                else if (rootElement is Curve rootCurve)
                {
                    //起点连着桥架
                    if (curve.StartPoint.DistanceTo(rootCurve.GetClosestPointTo(curve.StartPoint, false))
                        < ThPDSCommon.ALLOWABLE_TOLERANCE)
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, false, out isBranch);
                    }
                    //终点连着桥架
                    else
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, true, out isBranch);
                    }
                }
            }
            return NextElement;
        }

        /// <summary>
        /// 由根节点查找下一个路径，并删除来源块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="sharedPath"></param>
        /// <param name="sourceElement"></param>
        /// <param name="isStartPoint"></param>
        /// <returns></returns>
        public Dictionary<Entity, List<Curve>> FindRootNextPath(BlockReference block, List<Curve> sharedPath, Curve sourceElement,
            bool isStartPoint, out bool isBranch)
        {
            isBranch = false;
            var findPath = new Dictionary<Entity, List<Curve>>();
            if (sharedPath.Contains(sourceElement))
            {
                return findPath;
            }
            sharedPath.Add(sourceElement);
            var probe = (isStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(ThPDSCommon.ALLOWABLE_TOLERANCE);
            var blockProbe = ShrinkLineFrame(sourceElement, isStartPoint);
            var probeResults = FindNext(sourceElement, block, probe, blockProbe);
            return Switch(probeResults, sharedPath, sourceElement, isStartPoint, out isBranch);
        }

        /// <summary>
        /// 由根节点查找下一个路径
        /// </summary>
        /// <param name="sourceElement">已存在的曲线</param>
        /// <param name="space">探针</param>
        /// <returns></returns>
        public Dictionary<Entity, List<Curve>> FindRootNextPath(List<Curve> sharedPath, Curve sourceElement, bool isStartPoint,
            out bool IsBranch)
        {
            IsBranch = false;
            var findPath = new Dictionary<Entity, List<Curve>>();
            if (sharedPath.Contains(sourceElement))
            {
                return findPath;
            }
            sharedPath.Add(sourceElement);
            var probe = (isStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(ThPDSCommon.ALLOWABLE_TOLERANCE);
            var blockProbe = ShrinkLineFrame(sourceElement, isStartPoint);
            var probeResults = FindNext(sourceElement, probe, blockProbe);
            return Switch(probeResults, sharedPath, sourceElement, isStartPoint, out IsBranch);
        }

        private Dictionary<Entity, List<Curve>> Switch(List<Entity> probeResults, List<Curve> sharedPath, Curve sourceElement,
            bool IsStartPoint, out bool IsBranch)
        {
            var findPath = new Dictionary<Entity, List<Curve>>();
            IsBranch = false;
            switch (probeResults.Count)
            {
                //没有找到任何元素，说明元素进行了跳过，创建长探针，进行搜索，如果还搜索不到，则是未知负载
                case 0:
                    {
                        if (sourceElement is Line sourceline)
                        {
                            var longProbe = new Polyline();
                            if (IsStartPoint)
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.StartPoint, ExtendLine(sourceline.EndPoint,
                                    sourceline.StartPoint), ThPDSCommon.ALLOWABLE_TOLERANCE * 2);
                            }
                            else
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.EndPoint, ExtendLine(sourceline.StartPoint,
                                    sourceline.EndPoint), ThPDSCommon.ALLOWABLE_TOLERANCE * 2);
                            }
                            var longProbeResults = FindNext(sourceElement, longProbe);
                            var longProbeLineResults = longProbeResults.Where(e => e is Line).OfType<Line>().ToList();
                            //长探针只能找到一个符合条件的线。如果遇到多条，只取最符合的一条线
                            var point = IsStartPoint ? sourceline.StartPoint : sourceline.EndPoint;
                            longProbeLineResults = longProbeLineResults
                                .Where(o => ThGeometryTool.IsCollinearEx(sourceline.StartPoint, sourceline.EndPoint, o.StartPoint, o.EndPoint))
                                .OrderBy(o => Math.Min(point.DistanceTo(o.StartPoint), point.DistanceTo(o.EndPoint)))
                                .ToList();
                            if (longProbeLineResults.Count > 0)
                            {
                                var isStartPoint = point.DistanceTo(longProbeLineResults[0].StartPoint)
                                    > point.DistanceTo(longProbeLineResults[0].EndPoint);
                                return FindRootNextPath(sharedPath, longProbeLineResults[0], isStartPoint, out _);
                            }
                            else
                            {
                                findPath.Add(sourceline, sharedPath);
                            }
                        }
                        break;
                    }
                //找到下一个元素，返回
                case 1:
                    {
                        if (probeResults[0] is BlockReference blk)
                        {
                            findPath.Add(blk, sharedPath);
                        }
                        else if (probeResults[0] is Curve curve)
                        {
                            if (curve.EndPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint)
                                < ThPDSCommon.ALLOWABLE_TOLERANCE)
                            {
                                return FindRootNextPath(sharedPath, curve, true, out IsBranch);
                            }
                            else if (curve.StartPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint)
                                < ThPDSCommon.ALLOWABLE_TOLERANCE)
                            {
                                return FindRootNextPath(sharedPath, curve, false, out IsBranch);
                            }
                            else if (sourceElement is Line sourceLine && probeResults[0] is Line targetLine)
                            {
                                var mainVec = sourceLine.StartPoint.GetVectorTo(sourceLine.EndPoint);
                                var branchVec = targetLine.StartPoint.GetVectorTo(targetLine.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetLine);
                                    var square = ThPDSBufferService.Buffer(targetLine, Database);
                                    var secondResults = FindNextLine(targetLine, square);
                                    secondResults.Remove(sourceLine);
                                    if (secondResults.Count == 0)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        foreach (var secondEntity in secondResults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.StartPoint, false))
                                                        < ThPDSCommon.ALLOWABLE_TOLERANCE)
                                                {
                                                    var newsharedPath = new List<Curve>();
                                                    sharedPath.ForEach(c => newsharedPath.Add(c));
                                                    foreach (var newPath in FindRootNextPath(newsharedPath, secondCurve, false, out IsBranch))
                                                    {
                                                        if (!findPath.ContainsKey(newPath.Key))
                                                        {
                                                            findPath.Add(newPath.Key, newPath.Value);
                                                            IsBranch = true;
                                                        }
                                                    }
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.EndPoint, false))
                                                    < ThPDSCommon.ALLOWABLE_TOLERANCE)
                                                {
                                                    var newsharedPath = new List<Curve>();
                                                    sharedPath.ForEach(c => newsharedPath.Add(c));
                                                    foreach (var newPath in FindRootNextPath(newsharedPath, secondCurve, true, out IsBranch))
                                                    {
                                                        if (!findPath.ContainsKey(newPath.Key))
                                                        {
                                                            findPath.Add(newPath.Key, newPath.Value);
                                                            IsBranch = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                //遇到多个元素，认为必定有块/出现分支现象
                default:
                    {
                        //遇到块的情况
                        var blkResults = probeResults.OfType<BlockReference>();
                        if (blkResults.Count() > 0)
                        {
                            blkResults.ForEach(b =>
                            {
                                findPath.Add(b, sharedPath);
                            });
                        }
                        //遇到分支的情况
                        else if (sourceElement is Line sourceLine)
                        {
                            var mainVec = sourceLine.StartPoint.GetVectorTo(sourceLine.EndPoint);
                            foreach (var targetLine in probeResults.OfType<Line>())
                            {
                                var branchVec = targetLine.StartPoint.GetVectorTo(targetLine.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetLine);
                                    var square = ThPDSBufferService.Buffer(targetLine, Database);
                                    var secondResults = FindNext(sourceLine, square);
                                    secondResults.Remove(targetLine);
                                    if (secondResults.Count == 0)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        foreach (var secondEntity in secondResults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.StartPoint, false))
                                                    < ThPDSCommon.ALLOWABLE_TOLERANCE)
                                                {
                                                    var newsharedPath = new List<Curve>();
                                                    sharedPath.ForEach(c => newsharedPath.Add(c));
                                                    foreach (var newPath in FindRootNextPath(newsharedPath, secondCurve, false, out _))
                                                    {
                                                        if (!findPath.ContainsKey(newPath.Key))
                                                        {
                                                            findPath.Add(newPath.Key, newPath.Value);
                                                            IsBranch = true;
                                                        }
                                                    }
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.EndPoint, false))
                                                    < ThPDSCommon.ALLOWABLE_TOLERANCE)
                                                {
                                                    var newsharedPath = new List<Curve>();
                                                    sharedPath.ForEach(c => newsharedPath.Add(c));
                                                    foreach (var newPath in FindRootNextPath(newsharedPath, secondCurve, true, out _))
                                                    {
                                                        if (!findPath.ContainsKey(newPath.Key))
                                                        {
                                                            findPath.Add(newPath.Key, newPath.Value);
                                                            IsBranch = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
            }
            return findPath;
        }

        /// <summary>
        /// 查找该空间所有线
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNext(Entity existingEntity, Polyline space)
        {
            var results = CableIndex.SelectCrossingPolygon(space);
            results = results.Union(FindNextAndAttachedBlock(space));
            results = results.Union(DistBoxIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        /// <summary>
        /// 查找线端点上的所有线，以及线上的所有块
        /// </summary>
        /// <param name="existingEntity"></param>
        /// <param name="space"></param>
        /// <param name="lineFrame"></param>
        /// <returns></returns>
        public List<Entity> FindNext(Entity existingEntity, BlockReference block, Polyline space, Polyline lineFrame)
        {
            var results = CableIndex.SelectCrossingPolygon(space);
            results = results.Union(FindNextAndAttachedBlock(lineFrame));
            results = results.Union(DistBoxIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            results.Remove(block);
            return results.OfType<Entity>().ToList();
        }

        public List<Entity> FindNext(Entity existingEntity, Polyline space, Polyline lineFrame)
        {
            var results = CableIndex.SelectCrossingPolygon(space);
            results = results.Union(FindNextAndAttachedBlock(lineFrame));
            results = results.Union(DistBoxIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        private DBObjectCollection FindNextAndAttachedBlock(Polyline space)
        {
            var results = GeometriesMap(LoadIndex.SelectCrossingPolygon(space));
            var attachedBlock = AttachedIndex.SelectCrossingPolygon(space).OfType<Polyline>();
            attachedBlock.ForEach(o =>
            {
                results = results.Union(GeometriesMap(LoadIndex.SelectCrossingPolygon(o)));
            });
            return results;
        }

        private List<Entity> RemoveIgnoreAndTerminal(List<Entity> entities)
        {
            var lines = entities.OfType<Line>().ToList();
            if (lines.Count == 0)
            {
                return entities;
            }

            foreach (var l in lines)
            {
                var ignore = IgnoreIndex.SelectCrossingPolygon(l.BufferSquare(10.0));
                if (ignore.Count > 0)
                {
                    return new List<Entity>();
                }

                var terminal = TerminalIndex.SelectCrossingPolygon(l.BufferSquare(10.0));
                if (terminal.Count > 0)
                {
                    entities.Remove(l);
                }
            }
            return entities;
        }

        /// <summary>
        /// 查找该空间所有线
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextLine(Entity existingEntity, Polyline space)
        {
            var results = CableIndex.SelectCrossingPolygon(space);
            if (results.Count == 0)
            {
                return new List<Entity>();
            }
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间负载
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextLoad(Entity existingEntity, Polyline space)
        {
            var results = GeometriesMap(LoadIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间配电箱
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextDistBox(Entity existingEntity, Polyline space)
        {
            var results = DistBoxIndex.SelectCrossingPolygon(space);
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        /// <summary>
        /// 延伸点至指定长度
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private Point3d ExtendLine(Point3d sp, Point3d ep, double length = 2000.0)
        {
            var vec = sp.GetVectorTo(ep).GetNormal();
            return ep + vec.MultiplyBy(length);
        }

        private Polyline ShrinkLineFrame(Curve curve, bool isStartPoint)
        {
            var shrinkLine = new Line();
            if (curve is Line line)
            {
                shrinkLine = line;
            }
            else if (curve is Polyline polyline)
            {
                shrinkLine = polyline.GetEdges().Last();
            }
            else if (curve is Arc arc)
            {
                shrinkLine = arc.TessellateArcWithArc(50.0).GetEdges().Last();
            }

            var tolerance = 10 * ThPDSCommon.ALLOWABLE_TOLERANCE;
            if (shrinkLine.Length < 20 * ThPDSCommon.ALLOWABLE_TOLERANCE + 1.0)
            {
                tolerance = shrinkLine.Length / 2;
            }

            if (isStartPoint)
            {
                var newLine = new Line(shrinkLine.StartPoint, shrinkLine.EndPoint - shrinkLine.LineDirection() * tolerance);
                return ThPDSBufferService.Buffer(newLine);
            }
            else
            {
                var newLine = new Line(shrinkLine.StartPoint + shrinkLine.LineDirection() * tolerance, shrinkLine.EndPoint);
                return ThPDSBufferService.Buffer(newLine);
            }
        }

        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GetGraph()
        {
            return PDSGraph.Graph;
        }

        public void CopyAttributes()
        {
            PDSGraph.Graph.Vertices.ForEach(o =>
            {
                if (o.Loads.Count > 0)
                {
                    var edgeList = PDSGraph.Graph.OutEdges(o).ToList();
                    var attributesCopy = edgeList
                        .Where(e => !string.IsNullOrEmpty(e.Target.Loads[0].AttributesCopy))
                        .Select(e => e.Target.Loads[0].AttributesCopy)
                        .FirstOrDefault();
                    if (attributesCopy.IsNull())
                    {
                        return;
                    }
                    edgeList = edgeList
                        .Where(e => e.Target.Loads.Count > 0
                            && e.Target.Loads[0].ID.BlockName == attributesCopy)
                        .ToList();
                    var targetEdge = edgeList.Where(e => e.Target.Loads[0].InstalledCapacity.HighPower == 0)
                        .ToList();
                    var sourceEdge = edgeList.Except(targetEdge).FirstOrDefault();
                    if (sourceEdge == null)
                    {
                        return;
                    }

                    targetEdge.ForEach(e =>
                    {
                        e.Target.Loads[0].InstalledCapacity = sourceEdge.Target.Loads[0].InstalledCapacity;
                        e.Target.Loads[0].LoadTypeCat_3 = sourceEdge.Target.Loads[0].LoadTypeCat_3;
                        if (sourceEdge.Target.Loads[0].PrimaryAvail != 0)
                        {
                            e.Target.Loads[0].PrimaryAvail = sourceEdge.Target.Loads[0].PrimaryAvail;
                        }
                        if (sourceEdge.Target.Loads[0].SpareAvail != 0)
                        {
                            e.Target.Loads[0].SpareAvail = sourceEdge.Target.Loads[0].SpareAvail;
                        }
                        if (!string.IsNullOrEmpty(sourceEdge.Target.Loads[0].ID.Description)
                            && string.IsNullOrEmpty(e.Target.Loads[0].ID.Description))
                        {
                            e.Target.Loads[0].ID.Description = sourceEdge.Target.Loads[0].ID.Description;
                        }
                    });
                }
            });
        }

        public void AssignDefaultDescription()
        {
            PDSGraph.Graph.Vertices.ForEach(e =>
            {
                if (e.NodeType == PDSNodeType.Unkown)
                {
                    var defaultDescription = "";
                    switch (e.Loads[0].CircuitType)
                    {
                        case ThPDSCircuitType.Lighting:
                            defaultDescription = "正常照明";
                            break;
                        case ThPDSCircuitType.Socket:
                            defaultDescription = "插座";
                            break;
                        case ThPDSCircuitType.PowerEquipment:
                            defaultDescription = "动力负载";
                            break;
                        case ThPDSCircuitType.EmergencyLighting:
                            defaultDescription = "消防备用照明";
                            break;
                        case ThPDSCircuitType.EmergencyPowerEquipment:
                            defaultDescription = "消防动力负载";
                            break;
                        case ThPDSCircuitType.FireEmergencyLighting:
                            defaultDescription = "应急照明/疏散指示";
                            break;
                    }
                    e.Loads[0].ID.DefaultDescription = defaultDescription;
                }
            });
        }

        public void UnionEdge()
        {
            var removeEdges = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            foreach (var edge in PDSGraph.Graph.Edges)
            {
                if (removeEdges.Contains(edge) || string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber))
                {
                    continue;
                }
                var sameEdge = PDSGraph.Graph.Edges
                    .Where(e => e.Source.NodeType == edge.Source.NodeType)
                    .Where(e => e.Target.NodeType == edge.Target.NodeType)
                    .Where(e => e.Circuit.ID.CircuitNumber.Equals(edge.Circuit.ID.CircuitNumber))
                    .Where(e => e.Target.Loads[0].Location.BasePoint.AlmostEqualsTo(edge.Target.Loads[0].Location.BasePoint))
                    .OrderByDescending(e => e.Target.Loads.Count)
                    .ToList();
                if (sameEdge.Count > 1)
                {
                    for (var i = 1; i < sameEdge.Count; i++)
                    {
                        removeEdges.Add(sameEdge[i]);
                    }
                }
            }
            removeEdges.Distinct().ForEach(e =>
            {
                PDSGraph.Graph.RemoveEdge(e);
                PDSGraph.Graph.RemoveVertex(e.Target);
            });
        }

        public void UnionLightingEdge()
        {
            var distBoxes = PDSGraph.Graph.Vertices
                .Where(v => v.NodeType == PDSNodeType.DistributionBox)
                .Where(v => v.Loads[0].GetOnLightingCableTray()).ToList();
            var loads = PDSGraph.Graph.Vertices
                    .Where(v => v.NodeType == PDSNodeType.Load)
                    .Where(v => v.Loads[0].GetOnLightingCableTray()).ToList();
            if (loads.Count == 0)
            {
                return;
            }

            var cableTrayLoads = loads.Where(v => PDSGraph.Graph.InDegree(v) == 0).ToList();
            var otherLoads = loads.Except(cableTrayLoads).ToList();
            var sortNode = SortNode(cableTrayLoads);
            var targets = new List<ThPDSCircuitGraphNode>();
            for (var i = 0; i < sortNode.Count; i++)
            {
                for (var j = 1; j < sortNode[i].Item2.Count; j++)
                {
                    sortNode[i].Item2[0].Loads.AddRange(sortNode[i].Item2[j].Loads);
                    NodeMap[sortNode[i].Item2[0]].AddRange(NodeMap[sortNode[i].Item2[j]]);
                }
                targets.Add(sortNode[i].Item2[0]);

                for (var j = 1; j < sortNode[i].Item2.Count; j++)
                {
                    PDSGraph.Graph.RemoveVertex(sortNode[i].Item2[j]);
                    NodeMap.Remove(sortNode[i].Item2[j]);
                }
            }

            if (distBoxes.Count > 0)
            {
                if (distBoxes.Count == 1)
                {
                    CreateLightingEdge(targets, distBoxes[0]);
                    return;
                }
                else
                {
                    var lightingBoxes = distBoxes.Where(o => o.Loads[0].LoadTypeCat_2 == ThPDSLoadTypeCat_2.LightingDistributionPanel)
                        .ToList();
                    if (lightingBoxes.Count == 1)
                    {
                        CreateLightingEdge(targets, lightingBoxes[0]);
                        return;
                    }
                }
            }

            if (otherLoads.Count > 0)
            {
                var sourcePanelId = PDSGraph.Graph.InEdges(otherLoads[0]).First().Circuit.ID.SourcePanelID;
                CreateLightingEdge(targets, sourcePanelId);
            }
        }

        private void CreateLightingEdge(List<ThPDSCircuitGraphNode> targets, ThPDSCircuitGraphNode distBox)
        {
            targets.ForEach(load =>
            {
                load.Loads.ForEach(o =>
                {
                    o.ID.SourcePanelIDList.Add(distBox.Loads[0].ID.LoadID);
                });
                var newEdge = ThPDSGraphService.CreateEdge(distBox, load, new List<string>(), DistBoxKey, true);
                if (ThPDSEdgeContainsService.EdgeContains(newEdge, PDSGraph.Graph))
                {
                    return;
                }
                PDSGraph.Graph.AddEdge(newEdge);
                EdgeMap.Add(newEdge, NodeMap[load]);
            });
        }

        private void CreateLightingEdge(List<ThPDSCircuitGraphNode> targets, string sourcePanelId)
        {
            targets.ForEach(load =>
            {
                load.Loads.ForEach(o =>
                {
                    o.ID.SourcePanelIDList.Add(sourcePanelId);
                });
                var newEdge = ThPDSGraphService.CreateEdge(CableTrayNode, load, new List<string>(), DistBoxKey);
                if (ThPDSEdgeContainsService.EdgeContains(newEdge, PDSGraph.Graph))
                {
                    return;
                }
                PDSGraph.Graph.AddEdge(newEdge);
                EdgeMap.Add(newEdge, NodeMap[load]);
            });
        }

        private List<Tuple<string, List<ThPDSCircuitGraphNode>>> SortNode(List<ThPDSCircuitGraphNode> loads)
        {
            var results = new List<Tuple<string, List<ThPDSCircuitGraphNode>>>();
            loads.ForEach(load =>
            {
                var added = true;
                foreach (var item in results)
                {
                    if (item.Item1.Equals(load.Loads[0].ID.CircuitID))
                    {
                        item.Item2.Add(load);
                        added = false;
                    }
                }

                if (added)
                {
                    results.Add(Tuple.Create(load.Loads[0].ID.CircuitID, new List<ThPDSCircuitGraphNode> { load }));
                }
            });
            return results;
        }

        public void AssignStorey(Database database, ThEStoreys storeys, Point3d storeyBasePoint)
        {
            var isStandardStorey = false;
            if (storeys.StoreyTypeString.Equals("标准层"))
            {
                isStandardStorey = true;
            }

            PDSGraph.Graph.Vertices.ForEach(o =>
            {
                o.Loads.ForEach(e =>
                {
                    if (e.GetLocationList().Count == 0)
                    {
                        e.SetLocation(new ThPDSLocation());
                    }
                    e.Location.FloorNumber = storeys.StoreyNumber;
                    e.Location.StoreyBasePoint = ThPDSPoint3dService.ToPDSPoint3d(storeyBasePoint);
                    e.Location.ReferenceDWG = Path.GetFileNameWithoutExtension(database.Filename);
                    e.Location.IsStandardStorey = isStandardStorey;
                });
            });
        }

        public List<CircuitGraph> HandleMultiBuilding()
        {
            // 处理多楼栋
            var regex = new Regex(@"[0-9]+/[0-9]+-");
            var numRegex = new Regex(@"[0-9]+");
            var sourceInfo = "";
            var storeyList = new List<string>();

            foreach (var vertice in PDSGraph.Graph.Vertices)
            {
                if (vertice.NodeType == PDSNodeType.CableCarrier)
                {
                    continue;
                }

                var match = regex.Match(vertice.Loads[0].ID.LoadID);
                if (match.Success)
                {
                    var first = numRegex.Match(match.Value);
                    var second = first.NextMatch();
                    if (first.Success && second.Success)
                    {
                        sourceInfo = match.Value;
                        storeyList.Add(first.Value + "-");
                        storeyList.Add(second.Value + "-");
                        break;
                    }
                }
            }

            if (storeyList.Count == 0)
            {
                return new List<CircuitGraph> { PDSGraph.Graph };
            }

            var graphList = new List<CircuitGraph>();
            for (var i = 0; i < storeyList.Count; i++)
            {
                var clone = new CircuitGraph();
                var nodeMap = new Dictionary<ThPDSCircuitGraphNode, ThPDSCircuitGraphNode>();
                PDSGraph.Graph.Vertices.ForEach(v =>
                {
                    if (v.Loads.Count > 0)
                    {
                        var newNode = ThPDSGraphService.NodeClone(v, sourceInfo, storeyList[i]);
                        clone.AddVertex(newNode);
                        nodeMap.Add(v, newNode);
                    }
                    else
                    {
                        clone.AddVertex(v);
                        nodeMap.Add(v, v);
                    }
                });
                PDSGraph.Graph.Edges.ForEach(e =>
                {
                    var sourceNode = nodeMap[e.Source];
                    var targetNode = nodeMap[e.Target];
                    var newEdge = ThPDSGraphService.EdgeClone(sourceNode, targetNode, e.Circuit, sourceInfo, storeyList[i]);
                    clone.AddEdge(newEdge);
                });
                graphList.Add(clone);
            }

            return graphList;
        }

        public void HandleStandardStorey(List<CircuitGraph> graphList)
        {
            graphList.ForEach(graph =>
            {
                var addNode = new List<ThPDSCircuitGraphNode>();
                var addEdge = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
                var removeNode = new List<ThPDSCircuitGraphNode>();
                var removeEdge = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();

                graph.Vertices.ForEach(v =>
                {
                    if (v.Loads.Count > 0)
                    {
                        if (v.Loads[0].ID.Storeys.Count > 0)
                        {
                            if (v.Loads[0].ID.LoadID.Contains("n") || v.Loads[0].ID.LoadID.Contains("N"))
                            {
                                v.Loads[0].ID.LoadID.Replace("N", "n");
                                v.Loads[0].ID.Storeys.ForEach(s =>
                                {
                                    var newNode = ThPDSGraphService.NodeClone(v, "n", s.ToString());
                                    addNode.Add(newNode);
                                    removeNode.Add(v);
                                    graph.InEdges(v).ForEach(e =>
                                    {
                                        var newEdge = ThPDSGraphService.EdgeClone(e.Source, newNode, e.Circuit);
                                        addEdge.Add(newEdge);
                                        removeEdge.Add(e);
                                    });
                                    graph.OutEdges(v).ForEach(e =>
                                    {
                                        var newEdge = ThPDSGraphService.EdgeClone(newNode, e.Target, e.Circuit, "n", s.ToString());
                                        addEdge.Add(newEdge);
                                        removeEdge.Add(e);
                                    });
                                });
                            }
                        }
                    }
                });

                addNode.ForEach(node =>
                {
                    graph.AddVertex(node);
                });
                addEdge.ForEach(edge =>
                {
                    graph.AddEdge(edge);
                });
                removeNode.Distinct().ForEach(node =>
                {
                    graph.RemoveVertex(node);
                });
                removeEdge.ForEach(edge =>
                {
                    graph.RemoveEdge(edge);
                });
            });
        }

        private DBObjectCollection GeometriesMap(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                GeometryMap.ForEach(o =>
                {
                    if (o.Value.Equals(e))
                    {
                        results.Add(o.Key);
                    }
                });
            });
            return results;
        }

        public Extents3d GetGeometry(Entity entity)
        {
            if (GeometryMap.ContainsKey(entity))
            {
                return GeometryMap[entity].GeometricExtents;
            }
            return new Extents3d();
        }
    }
}
