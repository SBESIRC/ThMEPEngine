using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.GraphDomain;

namespace ThMEPEngineCore.Algorithm.ClusteringAlgorithm
{
    /// <summary>
    /// 密度聚类（DBScan）
    /// </summary>
    public class DBScanClustering : GraphBase
    {
        double _eps;//密度聚类的密度半径
        int _minPts;//一组中的最小节点个数
        int _maxPts;//一组中的最大节点个数
        int _isMinCluster=8;//合并分组时将小于该个数的组和附近的进行合并
        int _noClassifyId = -1;
        int _unClassifyId = 0;
        bool _countByWeight;
        double _precision;
        /// <summary>
        /// 密度聚类
        /// </summary>
        /// <param name="nodes">聚类的节点</param>
        /// <param name="nodeRelations">节点间的关系</param>
        /// <param name="precision">节点的精度
        /// (这里密度考虑的是距离，这里使用double，再该范围内的点认为是同一个节点)
        /// </param>
        /// <param name="countByNodeWeight">如果一个节点代表多个点时,设置为ture,同时再Node中指定每个节点代表的点个数</param>
        public DBScanClustering(IEnumerable<IGraphNode> nodes, List<GraphNodeRelation> nodeRelations, double precision= 0.1,bool countByNodeWeight=false)
            : base(nodes, nodeRelations)
        {
            _countByWeight = countByNodeWeight;
            _precision = precision;
        }
        /// <summary>
        /// 获取聚类结果
        /// </summary>
        /// <param name="eps">密度聚类的半径</param>
        /// <param name="minPts">最小聚类数量(如果少于这个点数的聚类结果会被丢掉，这里请谨慎设置)</param>
        /// <param name="maxPts">一类中最大的数量</param>
        /// <param name="mergeMinCluster">是否合并小的聚类结果</param>
        /// <returns></returns>
        public List<List<IGraphNode>> GetClusters(double eps, int minPts, int maxPts, bool mergeMinCluster = false)
        {
            _eps = eps;
            _minPts = minPts;
            _maxPts = maxPts;

            var clusters = new List<List<IGraphNode>>();
            if (null == _allNodes || _allNodes.Count < 1 || _allNodeRelations == null)
                return clusters;
            InitData();
            int clusterId = InitClusterId();

            int maxClusterId = (int)_allNodes.OrderBy(p => (int)p.NodeType).Last().NodeType;
            if (maxClusterId < 1) 
                return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) 
                clusters.Add(new List<IGraphNode>());
            foreach (IGraphNode p in _allNodes)
            {
                if (null == p.NodeType)
                    continue;
                if ((int)p.NodeType > 0) 
                    clusters[(int)p.NodeType - 1].Add(p);
            }
            var clusterCenters = ClusterCenters(ref clusters, maxClusterId);
            if(mergeMinCluster)
                MergeClusters(ref clusters, maxClusterId, ref clusterCenters);
            return clusters;
        }
        void InitData() 
        {
            foreach (var item in _allNodes)
            {
                if (null == item || item.GraphNode == null)
                    continue;
                item.NodeType = _unClassifyId;
            }
        }
        int InitClusterId() 
        {
            int clusterId = 1;
            for (int i = 0; i < _allNodes.Count; i++)
            {
                IGraphNode p = _allNodes[i];
                if ((int)p.NodeType == _unClassifyId)
                {
                    if (ExpandCluster(_allNodes, p, clusterId))
                        clusterId++;
                }
            }
            return clusterId;
        }
        List<IGraphNode> ClusterCenters(ref List<List<IGraphNode>> clusters, int maxClusterId) 
        {
            List<IGraphNode> clusterCenters = new List<IGraphNode>();
            //split large cluster into smaller ones
            for (int i = 0; i < maxClusterId; i++)
            {
                var count = _countByWeight? (int)clusters[i].Sum(x => x.NodeWeight): clusters[i].Count;
                if (count > _maxPts)
                {
                    maxClusterId++;
                    clusters.Add(new List<IGraphNode>());
                    int cycleTimes = clusters[i].Count / 2;
                    for (int clu = 0; clu < cycleTimes - 1; clu++)
                    {
                        clusters[clusters.Count - 1].Add(clusters[i][0]);
                        clusters[i].RemoveAt(0);
                    }
                }
                clusterCenters.Add(_allNodes.First().CenterGraphNode(clusters[i]));
            }
            return clusterCenters;
        }
        void MergeClusters(ref List<List<IGraphNode>> clusters,int maxClusterId,ref List<IGraphNode> clusterCenters) 
        {
            //merge small clusters into larger one
            int maxId = maxClusterId;
            var tempMaxId = maxId;
            for (int i = 0; i < tempMaxId; i++)
            {
                var thisCount = _countByWeight ? (int)clusters[i].Sum(x => x.NodeWeight) : clusters[i].Count;
                if (thisCount >= _isMinCluster)
                    continue;
                int nearestNeighbor = CenterDistOfSquared(clusterCenters[i], clusterCenters);

                var count = _countByWeight? ((int)clusters[i].Sum(x => x.NodeWeight) + (int)clusters[nearestNeighbor].Sum(x => x.NodeWeight))
                    :(clusters[i].Count + clusters[nearestNeighbor].Count);
                if (count > _maxPts)
                    continue;
                foreach (IGraphNode p in clusters[i])
                {
                    clusters[nearestNeighbor].Add(p);
                }
                //update new center for larger cluster
                clusterCenters[nearestNeighbor] = _allNodes.First().CenterGraphNode(clusters[i]);
                //remove rubbish
                clusters.RemoveAt(i);
                clusterCenters.RemoveAt(i);
                i--;
                tempMaxId--;
            }
        }
        bool ExpandCluster(List<IGraphNode> nodes, IGraphNode currentNode, int clusterId)
        {
            var seeds = GetRegion(nodes, currentNode,true);
            if (seeds.Count < _minPts)
            {
                currentNode.NodeType = _noClassifyId;
                return false;
            }
            for (int i = 0; i < seeds.Count; i++)
                seeds[i].NodeType = clusterId;
            seeds.Remove(currentNode);
            while (seeds.Count > 0)
            {
                var currentP = seeds[0];
                seeds.Remove(currentP);
                List<IGraphNode> result = GetRegion(nodes, currentP,true);
                if (result.Count < _minPts)
                    continue;
                for (int i = 0; i < result.Count; i++)
                {
                    var resultP = result[i];
                    if (null == resultP || resultP.NodeType == null)
                        continue;
                    var cId = (int)resultP.NodeType;
                    if (cId != _unClassifyId && cId != _noClassifyId)
                        continue;
                    if (cId == _unClassifyId)
                        seeds.Add(resultP);
                    resultP.NodeType = clusterId;
                }
            }
            return true;
        }

        List<IGraphNode> GetRegion(List<IGraphNode> nodes, IGraphNode currentNode,bool isWeight)
        {
            List<IGraphNode> region = new List<IGraphNode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var eNode = nodes[i];
                if (isWeight)
                {
                    var nodeRealtion = GetNodeToNodeRelation(currentNode, eNode, _precision);
                    if (null == nodeRealtion)
                        continue;
                    double weight = nodeRealtion.Weight;
                    if (weight <= _eps)
                        region.Add(nodes[i]);
                }
                else 
                {
                    double weight = currentNode.NodeDistanceToNode(eNode);
                    if (weight <= _eps)
                        region.Add(nodes[i]);
                }
            }
            if (region.Count < 1)
                region.Add(currentNode);
            return region;
        }
        int CenterDistOfSquared(IGraphNode node, List<IGraphNode> listNodes)
        {
            double[] distanceOfSquared = new double[listNodes.Count];
            int location = -1;
            for (int i = 0; i < listNodes.Count; i++)
            {
                distanceOfSquared[i] = node.NodeDistanceToNode(listNodes[i]); 
            }
            var v = distanceOfSquared.Select((m, index) => new { index, m }).OrderBy(n => n.m).Take(2);
            foreach (var e in v)
            {
                location = e.index;
            }
            return location;
        }
    }
}
