using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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

namespace TianHua.Electrical.PDS.Model
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
        private Dictionary<Entity,ThPDSCircuitGraphNode> CacheDistributionBoxCollection { get; set; }

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

        public ThPDSLoopGraphEngine(Database database, List<ThBlockReferenceData> DistributionBoxs, List<ThBlockReferenceData> Loads, List<Line> Bridges, List<Curve> Lines)
        {
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

                BridgeNode = new ThPDSCircuitGraphNode();
                BridgeNode.NodeType = PDSNodeType.Bridge;
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
        public void FindGraph(Entity ExtraEntity , Entity startingEntity)
        {
            //是配电箱
            if (startingEntity is BlockReference blockObj)
            {
                ThPDSCircuitGraphNode Node = CreatNode(startingEntity);
                CacheDistributionBoxCollection.Add(startingEntity, Node);
                graph.Graph.AddVertex(Node);

                Polyline polyline = Buffer(blockObj);
                var results = new List<Entity>();
                results = FindNextLine(blockObj, polyline);
                results.Remove(ExtraEntity);
                //配电箱搭着线
                foreach (Line findcurve in results)
                {
                    //线得搭到块上才可遍历，否则认为线只是跨过块
                    var blockobb = Buffer(blockObj, 0);
                    if (blockobb.Distance(findcurve.StartPoint) < AllowableTolerance || blockobb.Distance(findcurve.EndPoint) < AllowableTolerance)
                    {
                        FirstNavigate(Node, new List<Entity>(), new List<string>(), startingEntity, findcurve);
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

                        FindGraph(startingEntity,distributionBox);
                    }
                }

                results = FindNextLoad(blockObj, polyline);
                results.Remove(ExtraEntity);
                //配电箱搭着负载
                foreach (var load in results)
                {
                    FirstNavigate(Node, new List<Entity>() { load }, new List<string>(), startingEntity, load);
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
                        FirstNavigate(BridgeNode,new List<Entity>(),new List<string>(), curve, findcurve);
                    }
                }
            }
        }

        /// <summary>
        /// 根据初始点
        /// 初次寻路
        /// </summary>
        public void FirstNavigate(ThPDSCircuitGraphNode Node, List<Entity> Loads, List<string> Logos, Entity SourceEntity, Entity nextEntity)
        {

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
                Polyline rectangle = new Polyline();// Database.GetBlockReferenceOBB(blk);
                return rectangle.Buffer(distance)[0] as Polyline;
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }


        #region Test
        private ThPDSCircuitGraphNode CreatNode(Entity entity)
        {
            return new ThPDSCircuitGraphNode();
        }

        private ThPDSCircuitGraphNode CreateNode(List<Entity> entitys)
        {
            return new ThPDSCircuitGraphNode();
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
