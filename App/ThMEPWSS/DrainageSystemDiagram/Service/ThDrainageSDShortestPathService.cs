using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemDiagram
{
    class ThDrainageSDShortestPathService
    {
        public static double[,] createGraphForArea(List<ThToiletRoom> roomList, out List<Point3d> allPtGraph)
        {
            allPtGraph = new List<Point3d>();
            double[,] cost = null;

            var roomPlList = roomList.Select(x => x.outline).ToList();
            var room0 = roomList[0];
            cost = createGraphForRoom(room0, out var ptGraph0);
            allPtGraph.AddRange(ptGraph0);
            for (int i = 1; i < roomList.Count; i++)
            {
                var room1 = roomList[i];
                var cost1 = createGraphForRoom(room1, out var ptGraph1);

                cost = mergeCost(cost, allPtGraph, cost1, ptGraph1, roomPlList);

                allPtGraph.AddRange(ptGraph1);
            }

            return cost;
        }

        private static double[,] createGraphForRoom(ThToiletRoom room, out List<Point3d> ptGraph)
        {
            ptGraph = new List<Point3d>();
            var roomPt = new List<Point3d>();
            roomPt.AddRange(room.outlinePtList);

            var toiletOnWallPts = InsertToiletToWall(room);

            intersectToiletOnWallToRoomOutline(toiletOnWallPts, roomPt);

            roomPt = roomPt.Distinct().ToList();

            int roomOutlineCount = roomPt.Count();

            ptGraph.AddRange(roomPt);
            ptGraph.AddRange(room.toilet.SelectMany(x => x.SupplyCoolOnBranch));

            var cost = createGraph(ptGraph, room.outline, roomOutlineCount);

            return cost;

        }

        private static double[,] createGraph(List<Point3d> ptGraph, Polyline room, int roomOutlineCount)
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
                        (i < roomOutlineCount && j < roomOutlineCount && i == j - 1) ||
                        (i == 0 && j == roomOutlineCount - 1))
                    {
                        bBuild = true;
                        bCheck = true;
                    }

                    var line = new Line(ptGraph[i], ptGraph[j]);
                    if (bCheck == false)
                    {
                        var pts = room.Intersect(line, Intersect.OnBothOperands);
                        if (pts.Count > 0)
                        {
                            var pt = pts.Where(x => x.IsEqualTo(ptGraph[i], new Tolerance(10, 10)) == false && x.IsEqualTo(ptGraph[j], new Tolerance(10, 10)) == false);
                            if (pt.Count() > 0)
                            {
                                bBuild = false;
                                bCheck = true;
                            }
                        }

                        if (bCheck == false && room.Contains(line.GetCenter()) == false)
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

        private static double[,] mergeCost(double[,] cost0, List<Point3d> ptGraph0, double[,] cost1, List<Point3d> ptGraph1, List<Polyline> roomPlList)
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

            //房间框线之间
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

        private static List<Point3d> InsertToiletToWall(ThToiletRoom room)
        {
            List<Point3d> pl = new List<Point3d>();
            var tol = new Tolerance(10, 10);
            int tolCloseWallForIsland = 10000;

            foreach (var toilet in room.toilet)
            {
                toilet.SupplyCoolOnWall.ForEach(ptOnWall =>
                {
                    var onWall = room.wallList.Where(wall => wall.ToCurve3d().IsOn(ptOnWall, tol));
                    if (onWall.Count() > 0)
                    {
                        pl.Add(ptOnWall);
                    }
                    else
                    {
                        //岛
                        var wallList = room.wallList;
                        var ptOnWallIsland = ThDrainageSDCoolPtService.findPtOnWall(wallList, toilet, tolCloseWallForIsland, true);
                        pl.AddRange(ptOnWallIsland);
                    }
                });
            }

            return pl;
        }

        private static void intersectToiletOnWallToRoomOutline(List<Point3d> ptsOnWall, List<Point3d> outline)
        {
            var tol = new Tolerance(10, 10);
            foreach (var pt in ptsOnWall)
            {
                for (int i = 0; i < outline.Count - 1; i++)
                {
                    var line = new Line(outline[i], outline[i + 1]);
                    if (line.IsPointOnCurve(pt, tol))
                    {
                        outline.Insert(i + 1, pt);
                        break;
                    }
                }
            }
        }
    }
}
