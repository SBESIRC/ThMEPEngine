using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Extension;
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
        private ThPDSCircuitGraph graph;
        private const double AllowableTolerance = 25;//允许公差
        /// <summary>
        /// 全部数据集合
        /// </summary>
        private List<Entity> DataCollection { get; set; }

        /// <summary>
        /// 配电箱集合
        /// </summary>
        private List<Entity> DistributionBoxCollection { get; set; }

        /// <summary>
        /// 已捕捉到的配电箱集合
        /// </summary>
        private Dictionary<Entity, ThPDSCircuitGraphNode> CacheDistributionBoxCollection { get; set; }

        /// <summary>
        /// 负载集合
        /// </summary>
        private List<BlockReference> LoadCollection { get; set; }

        /// <summary>
        /// 已捕捉到的负载集合
        /// </summary>
        private List<Entity> CacheLoadCollection { get; set; }

        private List<Line> BridgeCollection { get; set; }
        private List<Curve> LineCollection { get; set; }

        private ThCADCoreNTSSpatialIndex distributionBoxSpatialIndex;
        private ThCADCoreNTSSpatialIndex loadSpatialIndex;
        private ThCADCoreNTSSpatialIndex lineSpatialIndex;
        private ThPDSCircuitGraphNode BridgeNode;//桥架节点
        private ThMarkService markService;
        private Database database;

        public ThPDSLoopGraphEngine(Database Database, List<ThBlockReferenceData> DistributionBoxs, List<ThBlockReferenceData> Loads, List<Line> Bridges, List<Curve> Lines, ThMarkService MarkService)
        {
            database = Database;
            markService = MarkService; 
            using (AcadDatabase acad = AcadDatabase.Use(database))
            {
                DistributionBoxCollection = DistributionBoxs.Select(o => acad.Element<Entity>(o.ObjId, false)).ToList();
                LoadCollection = Loads.Select(o => acad.Element<BlockReference>(o.ObjId, false)).ToList();
                CacheDistributionBoxCollection = new Dictionary<Entity, ThPDSCircuitGraphNode>();
                CacheLoadCollection = new List<Entity>();
                BridgeCollection = Bridges;
                LineCollection = Lines;

                distributionBoxSpatialIndex = new ThCADCoreNTSSpatialIndex(DistributionBoxCollection.ToCollection());
                loadSpatialIndex = new ThCADCoreNTSSpatialIndex(LoadCollection.ToCollection());
                lineSpatialIndex = new ThCADCoreNTSSpatialIndex(LineCollection.ToCollection());

                graph = new ThPDSCircuitGraph() { Graph  = new AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>() };
                BridgeNode = new ThPDSCircuitGraphNode();
                BridgeNode.NodeType = PDSNodeType.Bridge;
                graph.Graph.AddVertex(BridgeNode);
            }
        }

        /// <summary>
        /// 初始化图
        /// </summary>
        public void CreatGraph()
        {
            foreach (Line bridge in BridgeCollection)
            {
                FindGraph(null, bridge);
            }
            foreach (Entity distributionBox in DistributionBoxCollection.Except(CacheDistributionBoxCollection.Keys))
            {
                FindGraph(null, distributionBox);
            }
        }

        /// <summary>
        /// 根据节点开始寻图
        /// 实现根据节点，寻找该节点所能到达回路的方法
        /// </summary>
        /// <param name="startingEntity"></param>
        public void FindGraph(Entity ExtraEntity, Entity startingEntity)
        {
            //是配电箱
            if (startingEntity is BlockReference blockObj)
            {
                ThPDSCircuitGraphNode Node;
                if (!CacheDistributionBoxCollection.ContainsKey(startingEntity))
                {
                    Node = CreatNode(startingEntity);
                    CacheDistributionBoxCollection.Add(startingEntity, Node);
                    graph.Graph.AddVertex(Node);
                }
                else
                {
                    Node = CacheDistributionBoxCollection[startingEntity];
                }
                Polyline polyline = Buffer(blockObj);
                var results = new List<Entity>();
                results = FindNextLine(startingEntity, polyline);
                results.Remove(ExtraEntity);
                //配电箱搭着线
                foreach (Curve findcurve in results)
                {
                    //线得搭到块上才可遍历，否则认为线只是跨过块
                    var blockobb = Buffer(blockObj, 0);
                    if (blockobb.Distance(findcurve.StartPoint) < AllowableTolerance || blockobb.Distance(findcurve.EndPoint) < AllowableTolerance)
                    {
                        PrepareNavigate(Node, new List<Entity>(), new List<string>(), startingEntity, findcurve);
                    }
                }

                results = FindNextDistributionBox(blockObj, polyline);
                results.Remove(ExtraEntity);
                //配电箱搭着配电箱
                foreach (var distributionBox in results)
                {
                    if (!CacheDistributionBoxCollection.ContainsKey(distributionBox))
                    {
                        ThPDSCircuitGraphNode newNode = CreatNode(distributionBox);
                        CacheDistributionBoxCollection.Add(distributionBox, newNode);
                        graph.Graph.AddVertex(newNode);

                        var newEdge = CreatEdge(Node, newNode, new List<string>());
                        graph.Graph.AddEdge(newEdge);

                        FindGraph(startingEntity, distributionBox);
                    }
                }

                results = FindNextLoad(blockObj, polyline);
                results.Remove(ExtraEntity);
                //配电箱搭着负载
                foreach (var load in results)
                {
                    if (!CacheLoadCollection.Contains(load))
                    {
                        CacheLoadCollection.Add(load);
                        PrepareNavigate(Node, new List<Entity>() { load }, new List<string>(), startingEntity, load);
                    }
                }
            }
            //是桥架
            else if (startingEntity is Curve curve)
            {
                Polyline polyline = Buffer(curve);
                //桥架第一次搭出去的肯定是线
                var results = FindNextLine(curve, polyline);
                foreach (Line findcurve in results)
                {
                    bool IsStart = findcurve.StartPoint.DistanceTo(curve.GetClosestPointTo(findcurve.StartPoint, false)) < AllowableTolerance;
                    bool IsEnd = findcurve.EndPoint.DistanceTo(curve.GetClosestPointTo(findcurve.EndPoint, false)) < AllowableTolerance;
                    //都不相邻即无关系，都相邻即近似平行，都不符合
                    if (IsStart != IsEnd)
                    {
                        PrepareNavigate(BridgeNode, new List<Entity>(), new List<string>(), curve, findcurve);
                    }
                }
            }
        }

        /// <summary>
        /// 寻路（核心算法）
        /// </summary>
        public void PrepareNavigate(ThPDSCircuitGraphNode Node, List<Entity> Loads, List<string> Logos, Entity SourceEntity, Entity nextEntity)
        {
            var DistributionBox = Navigate(Node, Loads, Logos, SourceEntity, nextEntity);
            if (Loads.Count > 0)
            {
                ThPDSCircuitGraphNode newNode = CreateNode(Loads);
                graph.Graph.AddVertex(newNode);

                var newEdge = CreatEdge(Node, newNode, new List<string>());
                graph.Graph.AddEdge(newEdge);
                DistributionBox.ForEach(distributionBox =>
                {
                    var distributionBoxNode = CacheDistributionBoxCollection[distributionBox.Item2];
                    var newDistributionBoxEdge = CreatEdge(newNode, distributionBoxNode, new List<string>());
                    graph.Graph.AddEdge(newDistributionBoxEdge);

                    FindGraph(distributionBox.Item1, distributionBox.Item2);
                });
            }
        }

        /// <summary>
        /// 寻路算法
        /// </summary>
        public List<Tuple<Entity,Entity>> Navigate(ThPDSCircuitGraphNode Node, List<Entity> Loads, List<string> Logos, Entity SourceEntity, Entity nextEntity)
        {
            List<Tuple<Entity, Entity>> results = new List<Tuple<Entity, Entity>>();
            var findLoop = FindRootNextElement(SourceEntity, nextEntity);
            foreach (var item in findLoop)
            {
                if (DistributionBoxCollection.Contains(item.Key))
                {
                    ThPDSCircuitGraphNode newNode;
                    if (!CacheDistributionBoxCollection.ContainsKey(item.Key))
                    {
                        newNode = CreatNode(item.Key);
                        CacheDistributionBoxCollection.Add(item.Key, newNode);
                        graph.Graph.AddVertex(newNode);
                    }
                    else
                    {
                        newNode = CacheDistributionBoxCollection[item.Key];
                    }

                    if (DistributionBoxCollection.Contains(SourceEntity) || BridgeCollection.Contains(SourceEntity))
                    {
                        //配电箱搭着配电箱
                        var newEdge = CreatEdge(Node, newNode, new List<string>());
                        graph.Graph.AddEdge(newEdge);
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
                            results.Add((item.Value.Last() as Entity, item.Key as Entity).ToTuple());
                        }
                        else
                        {
                            results.Add((nextEntity, item.Key as Entity).ToTuple());
                        }
                    }
                }
                else if (LoadCollection.Contains(item.Key))
                {
                    //负载搭着负载
                    if (!CacheLoadCollection.Contains(item.Key))
                    {
                        Loads.Add(item.Key);
                        CacheLoadCollection.Add(nextEntity);
                        var nextLoops = FindNext(item.Key, Buffer(item.Key));
                        if (item.Value.Count > 0)
                        {
                            nextLoops.Remove(item.Value.Last());
                        }
                        else
                        {
                            nextLoops.Remove(nextEntity);
                        }
                        foreach (Entity entity in nextLoops)
                        {
                            //这就是自己本身延伸出去的块
                            results.AddRange(Navigate(Node, Loads, new List<string>(), item.Key, entity));
                        }
                    }
                }
                else
                {
                    //未知负载
                    Loads.Add(item.Key);
                }
            }
            return results;
        }

        /// <summary>
        /// 由根节点找到下个元素(块)
        /// </summary>
        /// <param name="node">起始节点</param>
        /// <param name="loads">负载</param>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="sourceElement">源点块</param>
        /// <returns></returns>
        public Dictionary<Entity, List<Curve>> FindRootNextElement(Entity rootElement, Entity specifyElement)
        {
            Dictionary<Entity, List<Curve>> NextElement = new Dictionary<Entity, List<Curve>>();
            if (specifyElement is BlockReference blk)
                return new Dictionary<Entity, List<Curve>>() { { blk, new List<Curve>() } };
            //线需要寻块，且要考虑到一条线延伸多条线的情况
            else if (specifyElement is Curve curve)
            {
                List<Curve> sharedpath = new List<Curve>();
                //配电箱
                if (rootElement is BlockReference rootblk)
                {
                    //起点连着块
                    if (Buffer(rootblk, 0).Distance(curve.StartPoint) < AllowableTolerance)
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
                else if (rootElement is Curve rootcurve)
                {
                    //起点连着桥架
                    if (curve.StartPoint.DistanceTo(rootcurve.GetClosestPointTo(curve.StartPoint, false)) < AllowableTolerance)
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
            Dictionary<Entity, List<Curve>> FindPath = new Dictionary<Entity, List<Curve>>();
            if (sharedPath.Contains(sourceElement))
                return FindPath;
            sharedPath.Add(sourceElement);
            var probe = (IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(AllowableTolerance * 2);
            var probeResults = FindNext(sourceElement, probe);
            switch (probeResults.Count)
            {
                //没有找到任何元素，说明元素进行了跳过，创建长探针，进行搜索，如果还搜索不到，则是未知负载
                case 0:
                    {
                        if (sourceElement is Line sourceline)
                        {
                            Polyline longProbe = new Polyline();
                            if (IsStartPoint)
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.StartPoint, ExtendLine(sourceline.EndPoint, sourceline.StartPoint), AllowableTolerance * 2);
                            }
                            else
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.EndPoint, ExtendLine(sourceline.StartPoint, sourceline.EndPoint), AllowableTolerance * 2);
                            }
                            var longProbeResults = FindNext(sourceElement, longProbe);
                            var longProbeLineResults = longProbeResults.Cast<Entity>().Where(e => e is Line).Cast<Line>().ToList();
                            //长探针只能找到一个符合条件的线。如果遇到多条，只取最符合的一条线
                            var point = IsStartPoint ? sourceline.StartPoint : sourceline.EndPoint;
                            longProbeLineResults = longProbeLineResults.Where(o => ThGeometryTool.IsCollinearEx(sourceline.StartPoint, sourceline.EndPoint, o.StartPoint, o.EndPoint)).OrderBy(o => Math.Min(point.DistanceTo(o.StartPoint), point.DistanceTo(o.EndPoint))).ToList();
                            if (longProbeLineResults.Count > 0)
                            {
                                bool isStartPoint = point.DistanceTo(longProbeLineResults[0].StartPoint) > point.DistanceTo(longProbeLineResults[0].EndPoint);
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
                            if (curve.EndPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < AllowableTolerance)
                            {
                                return FindRootNextPath(sharedPath, curve, true);
                            }
                            else if (curve.StartPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < AllowableTolerance)
                            {
                                return FindRootNextPath(sharedPath, curve, false);
                            }
                            else if (sourceElement is Line sourceline && probeResults[0] is Line targetline)
                            {
                                var mainVec = sourceline.StartPoint.GetVectorTo(sourceline.EndPoint);
                                var branchVec = targetline.StartPoint.GetVectorTo(targetline.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetline);
                                    var square = Buffer(targetline);
                                    var Secondresults = FindNextLine(targetline,square);
                                    Secondresults.Remove(sourceline);
                                    if (Secondresults.Count == 0)
                                        break;
                                    else
                                    {
                                        foreach (var secondEntity in Secondresults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.StartPoint, false)) < AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.EndPoint, false)) < AllowableTolerance)
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
                        else if (sourceElement is Line sourceline)
                        {
                            var mainVec = sourceline.StartPoint.GetVectorTo(sourceline.EndPoint);
                            foreach (Line targetline in probeResults.Cast<Entity>().Where(e => e is Line).Cast<Line>())
                            {
                                var branchVec = targetline.StartPoint.GetVectorTo(targetline.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetline);
                                    var square = Buffer(targetline);
                                    var Secondresults = FindNext(sourceline, square);
                                    Secondresults.Remove(targetline);
                                    if (Secondresults.Count == 0)
                                        break;
                                    else
                                    {
                                        foreach (var secondEntity in Secondresults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.StartPoint, false)) < AllowableTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.EndPoint, false)) < AllowableTolerance )
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
            var results = lineSpatialIndex.SelectCrossingPolygon(space);
            results = results.Union(loadSpatialIndex.SelectCrossingPolygon(space));
            results = results.Union(distributionBoxSpatialIndex.SelectCrossingPolygon(space));
            results.Remove(existingEntity);
            return results.Cast<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间所有线
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextLine(Entity existingEntity, Polyline space)
        {
            var results = lineSpatialIndex.SelectCrossingPolygon(space);
            results.Remove(existingEntity);
            return results.Cast<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间负载
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextLoad(Entity existingEntity, Polyline space)
        {
            var results = loadSpatialIndex.SelectCrossingPolygon(space);
            results.Remove(existingEntity);
            return results.Cast<Entity>().ToList();
        }

        /// <summary>
        /// 查找该空间配电箱
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextDistributionBox(Entity existingEntity, Polyline space)
        {
            var results = distributionBoxSpatialIndex.SelectCrossingPolygon(space);
            results.Remove(existingEntity);
            return results.Cast<Entity>().ToList();
        }

        public Polyline Buffer(Entity entity, double distance = AllowableTolerance)
        {
            if (entity is Curve curve)
            {
                if (curve is Line line)
                {
                    return line.Buffer(distance);
                }
                else if (curve is Arc arc)
                {
                    var objs = arc.TessellateArcWithArc(100.0).BufferPL(distance);
                    return objs[0] as Polyline;
                }
                else if (curve is Polyline polyline)
                {
                    var objs = polyline.TessellatePolylineWithArc(100.0).BufferPL(distance);
                    return objs.Cast<Polyline>().OrderByDescending(o => o.Length).First();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (entity is BlockReference blk)
            {
                Polyline rectangle = database.GetBlockReferenceOBB(blk);
                return rectangle.Buffer(distance)[0] as Polyline;
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 延伸点至指定长度
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Point3d ExtendLine(Point3d sp, Point3d ep, double length = 2000.0)
        {
            var vec = sp.GetVectorTo(ep).GetNormal();
            return ep + vec.MultiplyBy(length);
        }

        #region Test
        private ThPDSCircuitGraphNode CreatNode(Entity entity)
        {
            var node = new ThPDSCircuitGraphNode() { NodeType = PDSNodeType.DistributionBox };
            var frame = Buffer(entity);
            var marks = markService.GetMarks(frame);
            return node;
        }

        private ThPDSCircuitGraphNode CreateNode(List<Entity> entitys)
        {
            if (entitys.Count(o => o is Line) >0)
            {
                return new ThPDSCircuitGraphNode() { NodeType = PDSNodeType.None, Loads = entitys.Select(o => new ThPDSLoad()).ToList() };
            }
            return new ThPDSCircuitGraphNode() { NodeType = PDSNodeType.Load, Loads = entitys.Select(o => new ThPDSLoad()).ToList() };
        }

        private ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> CreatEdge(ThPDSCircuitGraphNode source, ThPDSCircuitGraphNode tatget, List<string> list)
        {
            return new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(source,tatget);
        }

        public AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GetGraph()
        {
            return this.graph.Graph;
        }
        #endregion
    }
}
