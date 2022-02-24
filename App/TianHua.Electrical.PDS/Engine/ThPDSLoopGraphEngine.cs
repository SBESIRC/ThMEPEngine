using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using QuickGraph;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    /// <summary>
    /// 回路算法引擎
    /// </summary>
    public class ThPDSLoopGraphEngine
    {
        private ThPDSCircuitGraph PDSGraph;

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
        private List<Line> Cabletrays { get; set; }

        /// <summary>
        /// 回路集合
        /// </summary>
        private List<Curve> Cables { get; set; }

        /// <summary>
        /// 配电箱关键字
        /// </summary>
        private List<string> DistBoxKey { get; set; }

        private ThCADCoreNTSSpatialIndex DistBoxSpatialIndex;
        private ThCADCoreNTSSpatialIndex LoadSpatialIndex;
        private ThCADCoreNTSSpatialIndex CableSpatialIndex;
        public ThPDSCircuitGraphNode CabletrayNode;//桥架节点
        private ThMarkService MarkService;
        private Database Database;

        public ThPDSLoopGraphEngine(Database database, List<Entity> distBoxes,
            List<Entity> loads, List<Line> cabletrays, List<Curve> cables, ThMarkService markService,
            List<string> distBoxKey)
        {
            Database = database;
            MarkService = markService;
            DistBoxKey = distBoxKey;
            using (AcadDatabase acad = AcadDatabase.Use(this.Database))
            {
                DistBoxes = distBoxes;
                Loads = loads;
                CacheDistBoxes = new Dictionary<Entity, ThPDSCircuitGraphNode>();
                CacheLoads = new List<Entity>();
                Cabletrays = cabletrays;
                Cables = cables;

                DistBoxSpatialIndex = new ThCADCoreNTSSpatialIndex(DistBoxes.ToCollection());
                LoadSpatialIndex = new ThCADCoreNTSSpatialIndex(this.Loads.ToCollection());
                CableSpatialIndex = new ThCADCoreNTSSpatialIndex(Cables.ToCollection());

                PDSGraph = new ThPDSCircuitGraph
                {
                    Graph = new AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>()
                };
                CabletrayNode = new ThPDSCircuitGraphNode
                {
                    NodeType = PDSNodeType.Cabletray,
                };
                PDSGraph.Graph.AddVertex(CabletrayNode);
            }
        }

        /// <summary>
        /// 初始化图
        /// </summary>
        public void CreatGraph()
        {
            foreach (var cabletray in Cabletrays)
            {
                FindGraph(null, cabletray);
            }
            foreach (var distBox in DistBoxes.Except(CacheDistBoxes.Keys))
            {
                if (!CacheDistBoxes.ContainsKey(distBox))
                {
                    FindGraph(null, distBox);
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
                    node = ThPDSGraphService.CreateNode(startingEntity, Database, MarkService, DistBoxKey);
                    CacheDistBoxes.Add(startingEntity, node);
                    PDSGraph.Graph.AddVertex(node);
                }
                else
                {
                    node = CacheDistBoxes[startingEntity];
                }
                var polyline = ThPDSBufferService.Buffer(blockObj,Database);
                var results = FindNextLine(startingEntity, polyline);
                results.Remove(extraEntity);
                //配电箱搭着线
                foreach (Curve findcurve in results)
                {
                    //线得搭到块上才可遍历，否则认为线只是跨过块
                    if (polyline.Contains(findcurve.StartPoint) || polyline.Contains(findcurve.EndPoint))
                    {
                        PrepareNavigate(node, new List<Entity>(), new List<string>(), startingEntity, findcurve);
                    }
                }

                results = FindNextDistBox(blockObj, polyline);
                results.Remove(extraEntity);
                //配电箱搭着配电箱
                foreach (var distBox in results)
                {
                    if (!CacheDistBoxes.ContainsKey(distBox))
                    {
                        var newNode = ThPDSGraphService.CreateNode(distBox, Database,MarkService,DistBoxKey);
                        CacheDistBoxes.Add(distBox, newNode);
                        PDSGraph.Graph.AddVertex(newNode);

                        var newEdge = ThPDSGraphService.CreateEdge(node, newNode, new List<string>(),DistBoxKey);
                        PDSGraph.Graph.AddEdge(newEdge);

                        FindGraph(startingEntity, distBox);
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
                        PrepareNavigate(node, new List<Entity> { load }, new List<string>(), startingEntity, load);
                    }
                }
            }
            //是桥架
            else if (startingEntity is Curve curve)
            {
                var polyline = ThPDSBufferService.Buffer(curve, Database);
                //桥架第一次搭出去的肯定是线
                var results = FindNextLine(curve, polyline);
                foreach (Line findCurve in results)
                {
                    var IsStart = findCurve.StartPoint.DistanceTo(curve.GetClosestPointTo(findCurve.StartPoint, false)) 
                        < ThPDSCommon.AllowableTolerance;
                    var IsEnd = findCurve.EndPoint.DistanceTo(curve.GetClosestPointTo(findCurve.EndPoint, false)) 
                        < ThPDSCommon.AllowableTolerance;
                    //都不相邻即无关系，都相邻即近似平行，都不符合
                    if (IsStart != IsEnd)
                    {
                        PrepareNavigate(CabletrayNode, new List<Entity>(), new List<string>(), curve, findCurve);
                    }
                }
            }
        }

        /// <summary>
        /// 寻路（核心算法）
        /// </summary>
        public void PrepareNavigate(ThPDSCircuitGraphNode node, List<Entity> loads, List<string> logos, Entity sourceEntity,
            Entity nextEntity)
        {
            var distributionBox = Navigate(node, loads, logos, sourceEntity, nextEntity);
            if (loads.Count > 0)
            {
                var newNode = ThPDSGraphService.CreateNode(loads, Database,MarkService, DistBoxKey);
                PDSGraph.Graph.AddVertex(newNode);

                var newEdge = ThPDSGraphService.CreateEdge(node, newNode, logos,DistBoxKey);
                PDSGraph.Graph.AddEdge(newEdge);
                distributionBox.ForEach(box =>
                {
                    var distBoxNode = CacheDistBoxes[box.Item2];
                    var newDistBoxEdge = ThPDSGraphService.CreateEdge(newNode, distBoxNode, logos,DistBoxKey);
                    PDSGraph.Graph.AddEdge(newDistBoxEdge);

                    FindGraph(box.Item1, box.Item2);
                });
            }
        }

        /// <summary>
        /// 寻路算法，sourceEntity表示连接上级，nextEntity表示自身
        /// </summary>
        public List<Tuple<Entity, Entity>> Navigate(ThPDSCircuitGraphNode node, List<Entity> loads, List<string> logos,
            Entity sourceEntity, Entity nextEntity)
        {
            var results = new List<Tuple<Entity, Entity>>();
            var findLoop = FindRootNextElement(sourceEntity, nextEntity);
            foreach (var item in findLoop)
            {
                // 搜索回路标注
                item.Value.ForEach(curve =>
                {
                    logos.AddRange(MarkService.GetMarks(ThPDSBufferService.Buffer(curve, Database)));
                });

                if (DistBoxes.Contains(item.Key))
                {
                    ThPDSCircuitGraphNode newNode;
                    if (!CacheDistBoxes.ContainsKey(item.Key))
                    {
                        newNode = ThPDSGraphService.CreateNode(item.Key,Database,MarkService,DistBoxKey);
                        CacheDistBoxes.Add(item.Key, newNode);
                        PDSGraph.Graph.AddVertex(newNode);
                    }
                    else
                    {
                        newNode = CacheDistBoxes[item.Key];
                    }

                    if (DistBoxes.Contains(sourceEntity) || Cabletrays.Contains(sourceEntity))
                    {
                        //配电箱搭着配电箱
                        var newEdge = ThPDSGraphService.CreateEdge(node, newNode, logos,DistBoxKey);
                        PDSGraph.Graph.AddEdge(newEdge);
                        if (item.Value.Count > 0)
                        {
                            FindGraph(item.Value.Last(), item.Key);
                        }
                        else
                        {
                            FindGraph(nextEntity, item.Key);
                        }
                    }
                    else
                    {
                        //负载搭着配电箱
                        if (item.Value.Count > 0)
                        {
                            results.Add(Tuple.Create(item.Value.Last() as Entity, item.Key));
                        }
                        else
                        {
                            results.Add(Tuple.Create(nextEntity, item.Key));
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
                        var nextLoops = FindNext(item.Key, ThPDSBufferService.Buffer(item.Key, Database));
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
                            results.AddRange(Navigate(node, loads, new List<string>(), item.Key, entity));
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
        public Dictionary<Entity, List<Curve>> FindRootNextElement(Entity rootElement, Entity specifyElement)
        {
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
                    var obb = ThPDSBufferService.Buffer(rootBlk,Database, 0);
                    //起点连着块
                    if (obb.Distance(curve.StartPoint) < obb.Distance(curve.EndPoint))
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, false);
                    }
                    //终点连着块
                    else
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, true);
                    }
                }
                //桥架
                else if (rootElement is Curve rootCurve)
                {
                    //起点连着桥架
                    if (curve.StartPoint.DistanceTo(rootCurve.GetClosestPointTo(curve.StartPoint, false)) 
                        < ThPDSCommon.AllowableTolerance)
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, false);
                    }
                    //终点连着桥架
                    else
                    {
                        NextElement = FindRootNextPath(sharedpath, curve, true);
                    }
                }
            }
            return NextElement;
        }

        /// <summary>
        /// 由根节点查找下一个路径
        /// </summary>
        /// <param name="sourceElement">已存在的曲线</param>
        /// <param name="space">探针</param>
        /// <returns></returns>
        public Dictionary<Entity, List<Curve>> FindRootNextPath(List<Curve> sharedPath, Curve sourceElement, bool IsStartPoint)
        {
            var FindPath = new Dictionary<Entity, List<Curve>>();
            if (sharedPath.Contains(sourceElement))
            {
                return FindPath;
            }
            sharedPath.Add(sourceElement);
            var probe = (IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(2 * ThPDSCommon.AllowableTolerance);
            var probeResults = FindNext(sourceElement, probe);
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
                                    sourceline.StartPoint), ThPDSCommon.AllowableTolerance * 2);
                            }
                            else
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.EndPoint, ExtendLine(sourceline.StartPoint,
                                    sourceline.EndPoint), ThPDSCommon.AllowableTolerance * 2);
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
                                return FindRootNextPath(sharedPath, longProbeLineResults[0], isStartPoint);
                            }
                            else
                            {
                                FindPath.Add(sourceline, sharedPath);
                            }
                        }
                        break;
                    }
                //找到下一个元素，返回
                case 1:
                    {
                        if (probeResults[0] is BlockReference blk)
                        {
                            FindPath.Add(blk, sharedPath);
                        }
                        else if (probeResults[0] is Curve curve)
                        {
                            if (curve.EndPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) 
                                < ThPDSCommon.AllowableTolerance)
                            {
                                return FindRootNextPath(sharedPath, curve, true);
                            }
                            else if (curve.StartPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint)
                                < ThPDSCommon.AllowableTolerance)
                            {
                                return FindRootNextPath(sharedPath, curve, false);
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
                                                    < ThPDSCommon.AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.EndPoint, false)) 
                                                    < ThPDSCommon.AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, true).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
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
                            var blk = blkResults.First() as BlockReference;
                            FindPath.Add(blk, sharedPath);
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
                                                    < ThPDSCommon.AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetLine.GetClosestPointTo(secondCurve.EndPoint, false)) 
                                                    < ThPDSCommon.AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, true).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
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
            return FindPath;
        }

        /// <summary>
        /// 查找该空间所有线
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNext(Entity existingEntity, Polyline space)
        {
            var results = CableSpatialIndex.SelectCrossingPolygon(space);
            results = results.Union(LoadSpatialIndex.SelectCrossingPolygon(space));
            results = results.Union(DistBoxSpatialIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            return results.OfType<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间所有线
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextLine(Entity existingEntity, Polyline space)
        {
            var results = CableSpatialIndex.SelectCrossingPolygon(space);
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
            var results = LoadSpatialIndex.SelectCrossingPolygon(space);
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
            var results = DistBoxSpatialIndex.SelectCrossingPolygon(space);
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

        #region Test
        public AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GetGraph()
        {
            return PDSGraph.Graph;
        }
        #endregion
    }
}
