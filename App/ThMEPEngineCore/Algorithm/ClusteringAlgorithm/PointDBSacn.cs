using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.GraphDomain;

namespace ThMEPEngineCore.Algorithm.ClusteringAlgorithm
{
    /// <summary>
    /// 点根据密度聚类，如果两个点穿线不进行聚类，可以设置不可以穿过的线
    /// </summary>
    public class PointDBSacn
    {
        List<Point3d> _clusterPoints;
        List<Line> _notCorssLines;
        List<PointGraphNode> _pointGraphNodes;
        List<GraphNodeRelation> _graphNodeRelations;
        /// <summary>
        /// 根据点进行聚类（只考虑点之间的距离）
        /// </summary>
        /// <param name="clusterPoints">密度聚类的点</param>
        public PointDBSacn(List<Point3d> clusterPoints) 
            :this(clusterPoints,null)
        { }
        /// <summary>
        /// 根据点进行聚类（穿线的不进行聚类）
        /// </summary>
        /// <param name="clusterPoints">密度聚类的点</param>
        /// <param name="notCrossLines">不能穿的线</param>
        public PointDBSacn(List<Point3d> clusterPoints, List<Line> notCrossLines) 
        {
            _clusterPoints = new List<Point3d>();
            _notCorssLines = new List<Line>();
            _pointGraphNodes = new List<PointGraphNode>();
            _graphNodeRelations = new List<GraphNodeRelation>();
            if (null != clusterPoints && clusterPoints.Count > 0) 
            {
                foreach(var point in clusterPoints) 
                {
                    if (null == point)
                        continue;
                    _clusterPoints.Add(point);
                }
            }
            if (null != notCrossLines && notCrossLines.Count > 0) 
            {
                foreach (var line in notCrossLines) 
                {
                    if (null == line)
                        continue;
                    _notCorssLines.Add(line);
                }
            }
        }
        /// <summary>
        /// 获取聚类结果
        /// </summary>
        /// <param name="eps">聚类密度值</param>
        /// <param name="minPts">最少点数(如果少于这个点数的聚类结果会被丢掉，这里请谨慎设置)</param>
        /// <param name="maxCount">一类中最多的点数</param>
        /// <param name="mergeMinCluster">是否将点数少的进行合并（默认false,合并可能不是想要的）</param>
        /// <returns></returns>
        public List<List<Point3d>> ClusterResult(double eps = 12000.0, int minPts = 1, int maxCount = 25,bool mergeMinCluster=false) 
        {
            var retGroups = new List<List<Point3d>>();
            InitGraphNodeRelation();
            if (null == _pointGraphNodes || _pointGraphNodes.Count < 1 || _graphNodeRelations ==null)
                return retGroups;
            var dBSacn = new DBScanClustering(_pointGraphNodes, _graphNodeRelations);
            var clusters = dBSacn.GetClusters(eps, minPts, maxCount, mergeMinCluster);
            if (clusters == null || clusters.Count < 1)
                return retGroups;
            var hisPoints = new List<Point3d>();
            foreach (var group in clusters)
            {
                if (null == group || group.Count < 1)
                    continue;
                var points = group.Select(c => (Point3d)c.GraphNode).ToList();
                retGroups.Add(points);
            }
            return retGroups;
        }
        void InitGraphNodeRelation() 
        {
            _pointGraphNodes.Clear();
            _graphNodeRelations.Clear();
            if (null == _clusterPoints || _clusterPoints.Count < 1)
                return;
            foreach (var point in _clusterPoints) 
            {
                if (null == point)
                    continue;
                _pointGraphNodes.Add(new PointGraphNode(point));
            }
            for (int i = 0; i < _pointGraphNodes.Count; i++)
            {
                var node = _pointGraphNodes[i];
                for (int j = i + 1; j < _pointGraphNodes.Count; j++)
                {
                    var nextNode = _pointGraphNodes[j];
                    var dis = node.NodeDistanceToNode(nextNode);
                    var checkLine = new Line((Point3d)node.GraphNode, (Point3d)nextNode.GraphNode);
                    bool isCross = dis < 1;
                    if (null != _notCorssLines && _notCorssLines.Count > 0) 
                    {
                        foreach (var line in _notCorssLines) 
                        {
                            if (isCross)
                                break;
                            if (null == line)
                                continue;
                            isCross = line.LineIsIntersection(checkLine);
                        }
                    }
                    if (isCross)
                        continue;
                    _graphNodeRelations.Add(new GraphNodeRelation(node, nextNode, dis));
                }
            }
        }
    }
}
