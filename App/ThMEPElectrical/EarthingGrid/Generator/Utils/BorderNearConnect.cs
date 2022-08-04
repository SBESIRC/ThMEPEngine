using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class BorderNearConnect
    {
        public static void ConnectBorderNear(Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts, Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts, 
            HashSet<Point3d> columnPts, List<Polyline> allOutlines, HashSet<Polyline> buildingOutline, double findLength, ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph)
        {
            var nonBuildingOutline = new HashSet<Polyline>();
            allOutlines.ForEach(ol => { if (!buildingOutline.Contains(ol)) nonBuildingOutline.Add(ol); });

            //1、近点连接到边界点
            NearConnectToBorder(outlinewithBorderPts, outlinewithNearPts, ref nearBorderGraph);

            //获取所有的边界点 // 应当只获得非forbidden的边界点
            var allBorderPts = new HashSet<Point3d>();
            foreach(var outlinewithBorderPt in outlinewithBorderPts)
            {
                outlinewithBorderPt.Value.ForEach(pt => allBorderPts.Add(pt));
            }
            //2、墙点和墙点连接
            BorderConnectToNear(allBorderPts, columnPts, allOutlines, nonBuildingOutline, outlinewithBorderPts, ref nearBorderGraph, findLength, Math.PI / 6);

            //3、删除无用的墙墙连接，删除无用的近点-墙连接
            DeleteUselessConnects(allOutlines, ref nearBorderGraph);
        }

        private static void NearConnectToBorder(Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts,
            Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts, ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph)
        {
            Point3d curBorderPt;
            Point3d verticalPt;
            Vector3d aimDirection;
            Point3d aimWallPt;
            foreach(var outlinewithNearPt in outlinewithNearPts)
            {
                Polyline curOutline = outlinewithNearPt.Key;
                if (!outlinewithBorderPts.ContainsKey(curOutline))
                {
                    continue;
                }
                foreach (var nearPt in outlinewithNearPt.Value)
                {
                    Vector3d baseDirection = curOutline.GetClosePoint(nearPt) - nearPt;
                    for (int i = 0; i < 4; ++i)
                    {
                        aimDirection = baseDirection.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis).GetNormal();
                        //向这个方向找点，如果找不到，则最近的点（垂直点）和它生成连接
                        curBorderPt = GetObjects.GetRangePointByDirection(nearPt, aimDirection, outlinewithBorderPts[curOutline], Math.PI / 6, 90000);
                        verticalPt = GetObjects.GetClosestPointByDirection(nearPt, aimDirection, 20000, curOutline);
                        if (curBorderPt.DistanceTo(verticalPt) > 4000) 
                        {
                            aimWallPt = verticalPt;
                        }
                        else
                        {
                            aimWallPt = curBorderPt;
                        }
                        GraphDealer.AddLineToGraph(aimWallPt, nearPt, ref nearBorderGraph);
                    }
                }
            }
        }

        private static void BorderConnectToNear(HashSet<Point3d> allBorderPts, HashSet<Point3d> columnPts, List<Polyline> allOutlines, HashSet<Polyline> nonBuildingOutline,
            Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts, ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph, double findLength, double findDegree)
        {
            foreach(var curOutline in nonBuildingOutline )
            {
                if (outlinewithBorderPts.ContainsKey(curOutline))
                {
                    foreach(var borderPt in outlinewithBorderPts[curOutline])
                    {
                        Vector3d baseDirection = new Vector3d();
                        GetObjects.GetCloestLineOfPolyline(curOutline, borderPt, ref baseDirection);
                        for (int i = 0; i < 4; ++i)
                        {
                            var curDirection = baseDirection.RotateBy(i * Math.PI / 2, Vector3d.ZAxis).GetNormal();

                            //1.2 获取墙点与柱点的连接
                            if (!WallCntColumnPt(borderPt, curDirection, columnPts, findLength, findDegree, allOutlines, ref nearBorderGraph))
                            {
                                //1.3 获取墙点与墙点的连接(仅生成单线，若双向生成的成为加固) 
                                WallCntWallPt(borderPt, curDirection, allBorderPts, curOutline, findLength, findDegree, ref nearBorderGraph);
                                //1.4 获取墙点到墙的垂线（生成双线）
                                WallCntVertilePt(borderPt, curDirection, nonBuildingOutline, curOutline, findLength, findDegree, ref nearBorderGraph);
                            }
                        }
                    }
                }
            }
        }

        private static void DeleteUselessConnects(List<Polyline> allOutlines, ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph)
        {
            //删除掉夹角小的线对中长度较长的线
            GraphDealer.ReduceSimilarLine(ref nearBorderGraph, Math.PI / 18 * 5);
            //删除掉相交线中长度较长的线
            GraphDealer.RemoveIntersectLines(ref nearBorderGraph);
        }

        /// <summary>
        /// 墙上的点和柱点进行连接
        /// </summary>
        public static bool WallCntColumnPt(Point3d wallPt, Vector3d direction, HashSet<Point3d> columnPts,
            double findLength, double findDegree, List<Polyline> allOutlines, ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall)
        {
            Point3d nearPt = GetObjects.GetPointByDirectionB(wallPt, direction, columnPts, findDegree, findLength);

            if (nearPt != wallPt)
            {
                Line reducedLine = LineDealer.ReduceLine(wallPt, nearPt, 1000);
                foreach (var outline in allOutlines)
                {
                    outline.Closed = true;
                    var pl = outline.Buffer(500)[0] as Polyline;
                    if (pl.Intersects(reducedLine))
                    {
                        return false;
                    }
                }
                GraphDealer.AddLineToGraph(wallPt, nearPt, ref wallConnectWall);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取墙和墙之间的连接
        /// 直接在范围内找最合适的墙连接点
        /// </summary>
        public static void WallCntWallPt(Point3d wallPt, Vector3d direction, HashSet<Point3d> allWallPts, Polyline wall, double findLength,
            double findDegree, ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall)
        {
            //查找：如果能查找到，则优先连接角度最小的
            Point3d cntWallPt = GetObjects.GetPointByDirectionB(wallPt, direction, allWallPts, findDegree, findLength);
            if (cntWallPt != wallPt + direction * 500)
            {
                GraphDealer.AddLineToGraph(wallPt, cntWallPt, ref wallConnectWall);
            }
        }

        //如果查找不到，则生成一条垂线
        public static void WallCntVertilePt(Point3d wallPt, Vector3d direction, HashSet<Polyline> allOutlines, Polyline wall,
            double findLength, double findDegree, ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall)
        {
            //查看先交的点这条线距离是否约等于到这个outline的最短距离
            foreach (var outline in allOutlines)
            {
                Point3d closestIntersectPt = GetObjects.GetClosestPointByDirection(wallPt + direction * 500, direction, findLength, outline);
                if (closestIntersectPt != wallPt + direction * 500)
                {
                    //判断当前点的合理性
                    //在中点生成一个半径1000的圆，如果没有和wall相交，则可以
                    Point3d middlePt = new Point3d((wallPt.X + closestIntersectPt.X) / 2, (wallPt.Y + closestIntersectPt.Y) / 2, 0);
                    Circle circle = new Circle(middlePt, Vector3d.ZAxis, 1000);
                    if (!wall.Intersects(circle) && !wall.Contains(middlePt))
                    {
                        GraphDealer.AddLineToGraph(wallPt, closestIntersectPt, ref wallConnectWall);
                    }
                }
            }
        }
    }
}
