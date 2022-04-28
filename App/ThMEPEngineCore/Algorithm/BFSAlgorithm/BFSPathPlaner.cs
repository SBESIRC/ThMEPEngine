using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm.BFSAlgorithm
{
    public class BFSPathPlaner
    {
        Point3d?[][] nodes;  //map
        int columns = 0;
        int rows = 0;
        double startX = 0;
        double startY = 0;
        double step = 400;
        List<Polyline> holes = new List<Polyline>();
        public BFSPathPlaner(double _step, List<Polyline> _holes = null)
        {
            step = _step;
            if (holes != null)
            {
                holes = _holes;
            }
        }

        /// <summary>
        /// 寻找最近线
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endLines"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public Line FindingClosetLine(Point3d startPt, List<Line> endLines, Polyline frame)
        {
            //起点已经达到终点
            var sLine = IsEndNode(endLines, startPt);
            if (sLine != null)
            {
                return sLine;
            }

            InitializeMap(frame); //初始化地图
            var startNode = InitializeStartNode(startPt); //初始化起点

            //起点不是有效点则返回空
            if (startNode == null)
            {
                return null;
            }

            var resLine = BFSFind(startNode, endLines);
            return resLine;
        }

        /// <summary>
        /// 初始化地图
        /// </summary>
        /// <param name="polyline"></param>
        private void InitializeMap(Polyline polyline)
        {
            var allPts = polyline.Vertices().Cast<Point3d>().ToList();
            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            columns = Convert.ToInt32(Math.Ceiling(Math.Abs(maxX - minX) / step));
            rows = Convert.ToInt32(Math.Ceiling(Math.Abs(maxY - minY) / step));
            startX = minX;
            startY = minY;

            nodes = new Point3d?[columns][];
            for (int i = 0; i < columns; i++)
            {
                this.nodes[i] = new Point3d?[rows];
            }

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Point3d pt = new Point3d(minX + i * step, minY + j * step, 0);
                    if (polyline.Contains(pt))
                    {
                        nodes[i][j] = pt;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化起点
        /// </summary>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private BFSNode InitializeStartNode(Point3d startPt)
        {
            int x = Convert.ToInt32(Math.Floor(Math.Abs(startPt.X - startX) / step));
            int y = Convert.ToInt32(Math.Floor(Math.Abs(startPt.Y - startY) / step));

            if (nodes[x][y] != null)
            {
                return new BFSNode(x, y, nodes[x][y].Value);
            }
            else if (nodes[x + 1][y] != null)
            {
                return new BFSNode(x + 1, y, nodes[x + 1][y].Value);
            }
            else if (nodes[x][y + 1] != null)
            {
                return new BFSNode(x, y + 1, nodes[x][y + 1].Value);
            }
            else if (nodes[x + 1][y + 1] != null)
            {
                return new BFSNode(x + 1, y + 1, nodes[x + 1][y + 1].Value);
            }

            return null;
        }

        /// <summary>
        /// BFS遍历
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="endLines"></param>
        /// <returns></returns>
        private Line BFSFind(BFSNode startNode, List<Line> endLines)
        {
            Queue<BFSNode> q = new Queue<BFSNode>();
            nodes[startNode.X][startNode.Y] = null;
            q.Enqueue(startNode);
            while (q.Count != 0)
            {
                BFSNode node = q.Dequeue();
                var adjNodes = GetAdjNodes(node);
                //遍历
                foreach (var aNode in adjNodes)
                {
                    if (!holes.Any(x=>x.Contains(aNode.point)))
                    {
                        aNode.parent = node;
                        q.Enqueue(aNode);
                        nodes[aNode.X][aNode.Y] = null;
                        var line = IsEndNode(endLines, aNode.point);
                        if (line != null)
                        {
                            return line;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 判断是否已经到了终点
        /// </summary>
        /// <param name="endLines"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private Line IsEndNode(List<Line> endLines, Point3d point)
        {
            foreach (var line in endLines)
            {
                if (line.GetClosestPointTo(point, false).DistanceTo(point) < step)
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>
        /// 寻找周围节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<BFSNode> GetAdjNodes(BFSNode node)
        {
            List<BFSNode> adjNodes = new List<BFSNode>();
            if (node.X + 1 < columns)
            {
                if (nodes[node.X + 1][node.Y] != null)
                {
                    BFSNode newNode = new BFSNode(node.X + 1, node.Y, nodes[node.X + 1][node.Y].Value);
                    adjNodes.Add(newNode);
                }
            }
            if (node.X - 1 >= 0)
            {
                if (nodes[node.X - 1][node.Y] != null)
                {
                    BFSNode newNode = new BFSNode(node.X - 1, node.Y, nodes[node.X - 1][node.Y].Value);
                    adjNodes.Add(newNode);
                }
            }
            if (node.Y + 1 < rows)
            {
                if (nodes[node.X][node.Y + 1] != null)
                {
                    BFSNode newNode = new BFSNode(node.X, node.Y + 1, nodes[node.X][node.Y + 1].Value);
                    adjNodes.Add(newNode);
                }
            }
            if (node.Y - 1 >= 0)
            {
                if (nodes[node.X][node.Y - 1] != null)
                {
                    BFSNode newNode = new BFSNode(node.X, node.Y - 1, nodes[node.X][node.Y - 1].Value);
                    adjNodes.Add(newNode);
                }
            }

            return adjNodes;
        }
    }
}
