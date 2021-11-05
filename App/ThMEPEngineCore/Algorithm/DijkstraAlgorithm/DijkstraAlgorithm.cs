using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm.DijkstraAlgorithm
{
    public class DijkstraAlgorithm
    {
        List<Curve> edges;
        public List<Node> nodes;
        public DijkstraAlgorithm(List<Curve> lines)
        {
            nodes = new List<Node>();
            edges = lines;
            foreach (var edge in edges)
            {
                if (!nodes.Any(x => x.NodePt.IsEqualTo(edge.StartPoint, new Tolerance(1, 1))))
                {
                    nodes.Add(new Node() { NodePt = edge.StartPoint });
                }
                if (!nodes.Any(x => x.NodePt.IsEqualTo(edge.EndPoint, new Tolerance(1, 1))))
                {
                    nodes.Add(new Node() { NodePt = edge.EndPoint });
                }
            }
        }

        /// <summary>
        /// 起点到达所有节点的最短路径距离
        /// </summary>
        /// <param name="spt"></param>
        /// <returns></returns>
        public List<double> FindingAllPathMinLength(Point3d spt)
        {
            return FindingPath(spt).Select(x => x.value).ToList();
        }

        /// <summary>
        /// 起点到达终点的最短路径距离
        /// </summary>
        /// <param name="spt"></param>
        /// <returns></returns>
        public Dictionary<Point3d, double> FindingAllPathMinNodeLength(Point3d spt)
        {
            return FindingPath(spt).ToDictionary(x => x.NodePt, y => y.value);
        }

        /// <summary>
        /// 计算已知终点的最短路径
        /// </summary>
        /// <param name="spt"></param>
        /// <param name="ept"></param>
        /// <returns></returns>
        public List<Point3d> FindingMinPath(Point3d spt, Point3d ept)
        {
            var allPath = FindingPath(spt);

            List<Point3d> path = new List<Point3d>();
            var endNode = allPath.FirstOrDefault(x => x.NodePt.IsEqualTo(ept, new Tolerance(1, 1)));

            while (endNode != null)
            {
                path.Add(endNode.NodePt);
                endNode = endNode.Parent;
            }
            return path;
        }

        /// <summary>
        /// 计算已知和所有终点的最短路径(达不到路径则不返回)
        /// </summary>
        /// <param name="spt"></param>
        /// <param name="ept"></param>
        /// <returns></returns>
        public Dictionary<Point3d, List<Point3d>> FindingAllMinPath(Point3d spt)
        {
            var allPath = FindingPath(spt);

            Dictionary<Point3d, List<Point3d>> pathInfo = new Dictionary<Point3d, List<Point3d>>();
            foreach (var node in allPath)
            {
                List<Point3d> path = new List<Point3d>();
                var endNode = node;
                while (endNode != null)
                {
                    path.Add(endNode.NodePt);
                    endNode = endNode.Parent;
                }
                if (!pathInfo.Keys.Contains(node.NodePt) && path.Last().IsEqualTo(spt, new Tolerance(1, 1)))
                {
                    pathInfo.Add(node.NodePt, path);
                }
            }

            return pathInfo;
        }

        /// <summary>
        /// 寻找起点到所有点的最短距离
        /// </summary>
        /// <param name="spt"></param>
        private List<Node> FindingPath(Point3d spt)
        {
            var s = nodes.Where(x => x.NodePt.IsEqualTo(spt, new Tolerance(1, 1))).ToList();
            if (s.Count == 0)
            {
                return new List<Node>();
            }
            s.ForEach(x => x.value = 0);
            var u = nodes.Where(x => !x.NodePt.IsEqualTo(spt, new Tolerance(1, 1))).ToList();
            u.ForEach(x => x.value = double.PositiveInfinity);

            while (u.Any())
            {
                var startNode = s.Last();

                //更新未使用点到起点的距离
                UpdateDistanceToNode(u, startNode);

                var minNode = u.OrderBy(x => x.value).First();
                s.Add(minNode);
                u.Remove(minNode);
            }

            return s;
        }

        /// <summary>
        /// 更新所有点到起始点的距离
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="sNode"></param>
        private void UpdateDistanceToNode(List<Node> nodes, Node sNode)
        {
            var matchEdges = edges.Where(x => x.StartPoint.IsEqualTo(sNode.NodePt, new Tolerance(1, 1)) ||
                   x.EndPoint.IsEqualTo(sNode.NodePt, new Tolerance(1, 1))).ToList();
            foreach (var edge in matchEdges)
            {
                var mNode = nodes.FirstOrDefault(x => edge.StartPoint.IsEqualTo(x.NodePt, new Tolerance(1, 1)) || edge.EndPoint.IsEqualTo(x.NodePt, new Tolerance(1, 1)));
                var length = edge.GetLength();
                if (mNode != null && mNode.value > length)
                {
                    mNode.Parent = sNode;
                    mNode.value = length + sNode.value;
                }
            }
        }
    }

    public class Node
    {
        /// <summary>
        /// 当前节点
        /// </summary>
        public Point3d NodePt { get; set; }

        /// <summary>
        /// 当前节点到起始点的距离
        /// </summary>
        public double value { get; set; }

        /// <summary>
        /// 父节点
        /// </summary>
        public Node Parent { get; set; }
    }
}
