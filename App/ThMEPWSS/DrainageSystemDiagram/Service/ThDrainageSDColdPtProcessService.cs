using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDColdPtProcessService
    {

        public static Dictionary<string, List<ThIfcSanitaryTerminalToilate>> classifyToilate(List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupToilate = new Dictionary<string, List<ThIfcSanitaryTerminalToilate>>();

            //classify
            for (int i = 0; i < toilateList.Count; i++)
            {
                string groupid = toilateList[i].GroupId;
                if (groupid != null && groupToilate.ContainsKey(groupid) == false)
                {
                    groupToilate.Add(groupid, new List<ThIfcSanitaryTerminalToilate>() { toilateList[i] });
                }
                else if (groupid != null)
                {
                    groupToilate[groupid].Add(toilateList[i]);
                }
            }

            //debug draw
            for (int i = 0; i < groupToilate.Count(); i++)
            {
                groupToilate.ElementAt(i).Value.ForEach(toilate =>
                 {
                     toilate.SupplyCoolOnWall.ForEach(x => DrawUtils.ShowGeometry(x, "l0group", (Int16)(i % 6), 25, 40, "C"));
                 });
            }


            return groupToilate;

        }

        public static List<Point3d> orderSupplyPtInGroup(List<ThIfcSanitaryTerminalToilate> groupToilate)
        {
            var pts = groupToilate.SelectMany(x => x.SupplyCoolOnBranch).ToList();

            if (pts.Count > 1)
            {
                var pt = pts.First();
                var otherPtInGroup = pts.Where(x => x.IsEqualTo(pt, new Tolerance(10, 10)) == false).First();
                var dirGroup = (otherPtInGroup - pt).GetNormal();

                var rotationangle = Vector3d.XAxis.GetAngleTo(dirGroup, Vector3d.ZAxis);
                var matrix = Matrix3d.Displacement(pt.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

                var ptsDict = pts.ToDictionary(x => x, x => x.TransformBy(matrix));
                var ptsOrder = ptsDict.OrderBy(x => x.Value.X).Select(x => x.Key).ToList();

                pts = ptsOrder;
            }
            return pts;
        }

        public static double[,] createGraph(List<Point3d> ptGraph, Polyline room)
        {
            int n = ptGraph.Count();
            double[,] cost = new double[n, n];
            int inf = 1000000;

            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    var bBuild = true;
                    var bCheck = false;

                    if (i == j ||
                        (i < room.NumberOfVertices && j < room.NumberOfVertices && i == j - 1))
                    {
                        bBuild = true;
                        bCheck = true;
                    }

                    if (bCheck == false)
                    {
                        var line = new Line(ptGraph[i], ptGraph[j]);

                        var pts = room.Intersect(line, Intersect.OnBothOperands);
                        if (pts.Count > 0)
                        {
                            var pt = pts.Where(x => x.IsEqualTo(ptGraph[i], new Tolerance(10, 10)) == false && x.IsEqualTo(ptGraph[j], new Tolerance(10, 10)) == false);
                            if (pt.Count() > 0)
                            {
                                bBuild = false;
                            }
                        }

                        if (room.Contains(line) == false)
                        {
                            bBuild = false;
                        }
                    }
                    if (bBuild == true)
                    {
                        var dist = i == j ? 0 : ptGraph[i].DistanceTo(ptGraph[j]);
                        cost[i, j] = dist;
                        cost[j, i] = dist;
                    }
                    else
                    {
                        cost[i, j] = inf;
                        cost[j, i] = inf;
                    }
                }
            }

            return cost;

        }

        public static double[,] mergeCost(double[,] cost0, List<Point3d> ptGraph0, double[,] cost1, List<Point3d> ptGraph1, List<Polyline> roomPlList)
        {
            int n0 = ptGraph0.Count();
            int n1 = ptGraph1.Count();
            double[,] cost = new double[n0 + n1, n0 + n1];
            int inf = 1000000;
            int overDist = 5000;
            int overWallWeight = 10000;

            //初始化
            for (int i = 0; i < n0 + n1; i++)
            {
                for (int j = 0; j < n0 + n1; j++)
                {
                    cost[i, j] = inf;
                }
            }

            //插入room cost
            for (int i = 0; i < n0; i++)
            {
                for (int j = 0; j < n0; j++)
                {
                    cost[i, j] = cost0[i, j];
                }
            }
            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < n1; j++)
                {
                    cost[n0 + i, n0 + j] = cost1[i, j];
                }
            }

            for (int i = 0; i < n0; i++)
            {
                for (int j = n0; j < n0 + n1; j++)
                {
                    var pt0 = ptGraph0[i];
                    var pt2 = ptGraph1[j - n0];
                    var dist = pt0.DistanceTo(pt2);
                    if (dist <= overDist)
                    {
                        var line = new Line(pt0, pt2);
                        var pts = new List<Point3d>();
                        roomPlList.ForEach(x => pts.AddRange(x.Intersect(line, Intersect.OnBothOperands)));
                        var bBuild = true;
                        if (pts.Count > 0)
                        {
                            var pt = pts.Where(x => x.IsEqualTo(pt0, new Tolerance(10, 10)) == false && x.IsEqualTo(pt2, new Tolerance(10, 10)) == false);
                            if (pt.Count() > 0)
                            {
                                bBuild = false;
                            }
                        }
                        if (bBuild == true)
                        {
                            cost[i, j] = dist + overWallWeight;
                            cost[j, i] = dist + overWallWeight;
                        }
                    }
                }
            }

            return cost;

        }

        public static List<int> ShortestPath(double[,] cost, int start, int end)
        {
            List<int> path = new List<int>();
            int g = start;//出发点
            int h = end;//终点
            int n = cost.GetLength(0);
            int[] book = new int[n]; //book[i]=0表示此结点最短路未确定，为1表示已确定
                                     //double[,] cost = Matrix.LoadData("cost1.txt", '\t');//路之间的权值，即距离

            double[] distance = new double[n];//出发点到各点最短距离
            int[] last1 = new int[n];//存储最短路径，每个结点的上一个结点
            double min;
            int u = 0;
            int inf = 1000000;

            //初始化distance，这是出发点到各点的初始距离
            for (int i = 0; i < n; i++)
            {
                //distance[i] = cost[0, i];
                distance[i] = cost[g, i];
            }

            //初始化出发点的book
            for (int i = 0; i < n; i++)
            {

                last1[i] = g;
            }

            last1[g] = -1;
            book[g] = 1;

            //核心算法
            for (int i = 0; i < n - 1; i++)
            {
                min = inf;

                //找到离g号结点最近的点
                for (int j = 0; j < n; j++)
                {
                    if (book[j] == 0 && distance[j] < min && distance[j] != 0)
                    {
                        min = distance[j];
                        u = j;
                    }
                }
                book[u] = 1;

                for (int v = 0; v < n; v++)
                {
                    if (cost[u, v] < inf && cost[u, v] != 0)
                    {
                        if (distance[v] > distance[u] + cost[u, v])
                        {
                            distance[v] = distance[u] + cost[u, v];
                            last1[v] = u;
                        }

                    }
                }
            }

            int k = h;
            path.Add(k);

            while (k != g)
            {
                if (distance[k] >= inf)
                {
                    break;
                }
                k = last1[k];

                path.Add(k);
            }
            path.Reverse();

            return path;
        }








        /// <summary>
        /// 有问题 没写完
        /// </summary>
        /// <param name="pl"></param>
        /// <returns></returns>
        private static List<Point3d> findConcavePT(Polyline pl)
        {
            var concavePtList = new List<Point3d>();

            var convexPt = pl.GetPoint3dAt(0);
            int convexIdx = 0;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint3dAt(i % pl.NumberOfVertices);
                if (pt.X < convexPt.X)
                {
                    convexPt = pt;
                    convexIdx = i;
                }
                else if (pt.X == convexPt.X && pt.Y > convexPt.Y)
                {
                    convexPt = pt;
                    convexIdx = i;
                }
            }
            DrawUtils.ShowGeometry(convexPt, "l0convex", 3, 25, 20, "S");

            Vector3d preConvex = pl.GetPoint3dAt((convexIdx - 1) % pl.NumberOfVertices) - convexPt;
            Vector3d nextConvex = pl.GetPoint3dAt((convexIdx + 1) % pl.NumberOfVertices) - convexPt;
            var a = preConvex.CrossProduct(nextConvex);

            for (int i = convexIdx + 1; i < pl.NumberOfVertices + convexIdx; i++)
            {
                var thisPt = pl.GetPoint3dAt(i % pl.NumberOfVertices);
                var prePT = pl.GetPoint3dAt((i - 1) % pl.NumberOfVertices);
                var nextPT = pl.GetPoint3dAt((i + 1) % pl.NumberOfVertices);

                Vector3d preV = thisPt - prePT;
                Vector3d nextV = nextPT - thisPt;
                var b = preV.CrossProduct(nextV);

                //if (a*b >0)
                //{

                //}
            }


            return concavePtList;
        }
    }
}
