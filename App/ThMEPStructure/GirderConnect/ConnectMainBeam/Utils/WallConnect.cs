using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class WallConnect
    {
        /// <summary>
        /// 有优先级的通过墙上的点获取其要连接的近点或者另一个墙上的点，该操作意在弥补borderpt与nearpt连接的缺失
        /// </summary>
        /// <param name="outline2BorderNearPts"></param>
        /// <param name="outline2WallPts"></param>
        /// <param name="walls">已知的限定边界</param>
        /// <param name="columnPts">已知的柱点</param>
        /// <param name="priority1stBorderNearTuples"></param>
        /// <param name="findLength">搜索的最大长度</param>
        /// <param name="findDegree">搜索的最大摆角</param>
        /// <returns>返回墙点与墙点的连接</returns>
        public static void WallConnection(ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts,
            Dictionary<Polyline, HashSet<Point3d>> outline2WallPts, HashSet<Polyline> walls, HashSet<Point3d> columnPts, List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples,
            Dictionary<Polyline, Polyline> outline2OriOutline, HashSet<Point3d> allWallPts, double findLength = 11000, double findDegree = Math.PI / 6)
        {
            allWallPts = PointsCenters.PointsCores(allWallPts.ToList(), 500).ToHashSet();
            List<Polyline> allOutlines = outline2BorderNearPts.Keys.ToList();
            var wall2WallPts = PointsDealer.GetOutline2BorderPts(allOutlines, allWallPts.ToList());
            foreach(var outline in allOutlines)
            {
                if (walls.Contains(outline) && outline2WallPts.ContainsKey(outline) && wall2WallPts.ContainsKey(outline))
                {
                    outline2WallPts[outline].Clear();
                    outline2WallPts[outline] = wall2WallPts[outline];
                }
            }
            HashSet<Point3d> ptss = new HashSet<Point3d>();
            foreach(var pts in outline2WallPts.Values)
            {
                foreach(var pt in pts)
                {
                    ptss.Add(pt);
                }
            }
            Dictionary<Point3d, HashSet<Point3d>> priorityWallCntWall = new Dictionary<Point3d, HashSet<Point3d>>();

            //1、墙墙连 + 墙连柱
            foreach (var outline2WallPt in outline2WallPts)
            {
                var curWall = outline2WallPt.Key;
                if (walls.Contains(curWall) && outline2BorderNearPts.ContainsKey(curWall)) //只对外长条线操作
                {
                    HashSet<Point3d> lonelyWallPts = new HashSet<Point3d>();
                    lonelyWallPts = outline2WallPt.Value.ToHashSet();
                    foreach (var wallPt in lonelyWallPts)
                    {
                        Vector3d baseDirection = new Vector3d();
                        GetObject.GetCloestLineOfPolyline(outline2OriOutline[curWall], wallPt, ref baseDirection);

                        for (int i = 0; i < 4; ++i)
                        {
                            var curDirection = baseDirection.RotateBy(i * Math.PI / 2, Vector3d.ZAxis).GetNormal();

                            //1.2 获取墙点与柱点的连接
                            if (!WallCntColumnPt(wallPt, curDirection, columnPts, curWall, findLength, findDegree, allOutlines, ref outline2BorderNearPts))
                            {
                                //1.3 获取墙点与墙点的连接(仅生成单线)
                                WallCntWallPt(wallPt, curDirection, ptss, curWall, findLength, findDegree, ref wallConnectWall, ref priorityWallCntWall);
                                //1.4 获取墙点到墙的垂线（生成双线）
                                WallCntVertilePt(wallPt, curDirection, walls, curWall, findLength, findDegree, ref wallConnectWall);
                            }
                        }
                    }
                }
            }
            //2、删减无用的”墙与墙之间的连线“
            UpdateWallConnectWall(ref wallConnectWall, ref priorityWallCntWall, allOutlines);

            //3、删减无用的“BorderPt与NearPt的连接”
            outline2BorderNearPts = UpdateBorder2NearPts(outline2BorderNearPts, priority1stBorderNearTuples);
        }

        //删除无用的”墙与墙之间的连线“
        private static void UpdateWallConnectWall(ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall, 
            ref Dictionary<Point3d, HashSet<Point3d>> priorityWallCntWall, List<Polyline> allOutlines)
        {
            //1、删除掉和墙相交的线
            StructureDealer.RemoveLinesInterSectWithOutlines(allOutlines, ref wallConnectWall);
            //2、有优先级的删除掉角度相近的线
            ReduceSimilarLineB(ref wallConnectWall, priorityWallCntWall, Math.PI / 18 * 5);
            //3、删除掉两两相交的线
            DicTuplesDealer.RemoveIntersectLines(ref wallConnectWall);
        }

        /// <summary>
        /// 删减无用的“BorderPt与NearPt的连接”
        /// </summary>
        private static Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> UpdateBorder2NearPts(
            Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples)
        {
            var dicTuples = LineDealer.RemoveLineIntersectWithOutline(outline2BorderNearPts, ref priority1stBorderNearTuples, 600);
            var outlines = outline2BorderNearPts.Keys.ToList();
            Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            priority1stBorderNearTuples.ForEach(o => DicTuplesDealer.AddLineTodicTuples(o.Item1, o.Item2, ref priority1stDicTuples));

            StructureDealer.RemoveLinesNearOutlines(outlines, ref priority1stDicTuples);
            ReduceSimilarLine(ref dicTuples, priority1stDicTuples, Math.PI / 18 * 5);
            DicTuplesDealer.RemoveIntersectLines(ref dicTuples);
            return PointsDealer.CreateOutline2BorderNearPts(dicTuples, outlines);
        }

        /// <summary>
        /// 在双线结构中只保留双线
        /// </summary>
        public static void RemainDoubleTuples(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            List<Point3d> pts = dicTuples.Keys.ToList();
            foreach(var pt in pts)
            {
                if (dicTuples.ContainsKey(pt))
                {
                    List<Point3d> toPts = dicTuples[pt].ToList();
                    foreach(var toPt in toPts)
                    {
                        if(!dicTuples.ContainsKey(toPt) || !dicTuples[toPt].Contains(pt))
                        {
                            dicTuples[pt].Remove(toPt);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除掉附近有链接近点的墙点的墙点 若该点附近有其他的连接点，则不进行连接查找
        /// </summary>
        public static void RemoveConnectedWallPts(HashSet<Point3d> oriWallPts, HashSet<Point3d> RemovePts, ref HashSet<Point3d> lonelyWallPts, double tolerance = 2000)
        {
            lonelyWallPts = oriWallPts.ToHashSet();
            foreach (var rmPt in RemovePts)
            {
                double l = rmPt.X - tolerance;
                double r = rmPt.X + tolerance;
                double t = rmPt.Y + tolerance;
                double b = rmPt.Y - tolerance;
                foreach (var oriWallPt in oriWallPts)
                {
                    if(oriWallPt.X > l && oriWallPt.X < r && oriWallPt.Y < t && oriWallPt.Y > b)
                    {
                        lonelyWallPts.Remove(oriWallPt);
                    }
                }
            }
        }

        private static void GetDirections(HashSet<Vector3d> vectors, ref List<Vector3d> directionList)
        {
            int n = vectors.Count;
            if(n == 0)
            {
                return;
            }
            Dictionary<Vector3d, bool> vecVisit = new Dictionary<Vector3d, bool>();
            for(int i = 0; i < 4; ++i)
            {
                vecVisit.Add(vectors.First().RotateBy(i * Math.PI / 2, Vector3d.ZAxis), false);
            }
            foreach(var vec in vectors)
            {
                foreach(Vector3d vector in vecVisit.Keys)
                {
                    if(vecVisit[vector] == false && vec.GetAngleTo(vector) < Math.PI / 4)
                    {
                        
                        vecVisit[vector] = true;
                        break;
                    }
                }
            }
            foreach (Vector3d vector in vecVisit.Keys)
            {
                if (vecVisit[vector] == false)
                {
                    directionList.Add(vector);
                }
            }
        }

        /// <summary>
        /// 墙上的点和柱点进行连接
        /// </summary>
        public static bool WallCntColumnPt(Point3d wallPt, Vector3d direction, HashSet<Point3d> columnPts, Polyline wall,
            double findLength, double findDegree, List<Polyline> allOutlines, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Point3d nearPt = GetObject.GetPointByDirectionB(wallPt, direction, columnPts, findDegree, findLength);
            
            if (nearPt != wallPt)
            {
                Line reducedLine = LineDealer.ReduceLine(wallPt, nearPt, 1000);
                foreach (var outline in allOutlines)
                {
                    var pl = outline.Buffer(500)[0] as Polyline;
                    if (pl.Intersects(reducedLine))
                    {
                        return false;
                    }
                }

                if (!outline2BorderNearPts[wall].ContainsKey(wallPt))
                {
                    outline2BorderNearPts[wall].Add(wallPt, new HashSet<Point3d>());
                }
                if (!outline2BorderNearPts[wall][wallPt].Contains(nearPt))
                {
                    outline2BorderNearPts[wall][wallPt].Add(nearPt);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取墙和墙之间的连接（支持两面有墙点或某一面墙有墙点，不支持两面墙都没有墙点）
        /// 直接在范围内找最合适的墙连接点
        /// </summary>
        public static void WallCntWallPt(Point3d wallPt, Vector3d direction, HashSet<Point3d> allWallPts, Polyline wall, double findLength,
            double findDegree, ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall, ref Dictionary<Point3d, HashSet<Point3d>> priorityWallCntWall)
        {
            //查找：如果能查找到，则优先连接角度最小的
            Point3d cntWallPt = GetObject.GetPointByDirectionB(wallPt, direction, allWallPts, findDegree, findLength);
            if (cntWallPt != wallPt + direction * 500)
            {
                Point3d middlePt = new Point3d((wallPt.X + cntWallPt.X) / 2, (wallPt.Y + cntWallPt.Y) / 2, 0);
                Circle circle = new Circle(middlePt, Vector3d.ZAxis, 500);
                if (!wall.Intersects(circle) && !wall.Contains(middlePt))
                {
                    DicTuplesDealer.AddLineTodicTuples(wallPt, cntWallPt, ref wallConnectWall);
                    DicTuplesDealer.AddLineTodicTuples(wallPt, cntWallPt, ref priorityWallCntWall);
                }
            }
        }

        //如果查找不到，则生成一条垂线
        public static void WallCntVertilePt(Point3d wallPt, Vector3d direction, HashSet<Polyline> allOutlines, Polyline wall,
            double findLength, double findDegree, ref Dictionary<Point3d, HashSet<Point3d>> wallConnectWall)
        {
            //查看先交的点这条线距离是否约等于到这个outline的最短距离
            foreach(var outline in allOutlines)
            {
                Point3d closestIntersectPt = GetObject.GetClosestPointByDirection(wallPt + direction * 500, direction, findLength, outline);
                if(closestIntersectPt != wallPt + direction * 500)
                {
                    //判断当前点的合理性
                    //在中点生成一个半径1000的圆，如果没有和wall相交，则可以
                    Point3d middlePt = new Point3d((wallPt.X  + closestIntersectPt.X)/2, (wallPt.Y + closestIntersectPt.Y) / 2, 0);
                    Circle circle = new Circle(middlePt, Vector3d.ZAxis, 1000);
                    if (!wall.Intersects(circle) && !wall.Contains(middlePt))
                    {
                        DicTuplesDealer.AddLineTodicTuples(wallPt, closestIntersectPt, ref wallConnectWall);
                    }
                }
            }
        }

        /// <summary>
        /// Reduce Similar line to only one
        /// </summary>
        private static void ReduceSimilarLine(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, 
            Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples = null, double tolerance = Math.PI / 4)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                int cnt = dicTuples[key].Count;
                while (cnt-- > 1)
                {
                    if (!dicTuples.ContainsKey(key))
                    {
                        break;
                    }
                    var value = dicTuples[key];
                    int n = value.Count;
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        if (cntPts[i % n].DistanceTo(cntPts[i - 1]) < 1.0 || key.DistanceTo(cntPts[i - 1]) < 1.0 || cntPts[i % n].DistanceTo(key) < 1.0)
                        {
                            continue;
                        }
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    if (minDegree > tolerance)
                    {
                        break;
                    }
                    Point3d rmPt = new Point3d();
                    var ptA = minDegreePairPt.Item1;
                    var ptB = minDegreePairPt.Item2;
                    if (priority1stDicTuples != null && priority1stDicTuples.ContainsKey(key))
                    {
                        if (priority1stDicTuples[key].Contains(ptA) && !priority1stDicTuples[key].Contains(ptB))
                        {
                            rmPt = ptB;
                        }
                        else if (priority1stDicTuples[key].Contains(ptB) && !priority1stDicTuples[key].Contains(ptA))
                        {
                            rmPt = ptA;
                        }
                    }
                    if (rmPt == new Point3d())
                    {
                        if (ptA.DistanceTo(key) >= ptB.DistanceTo(key))
                        {
                            rmPt = ptA;
                        }
                        else
                        {
                            rmPt = ptB;
                        }
                    }
                    DicTuplesDealer.DeleteFromDicTuples(rmPt, key, ref dicTuples);
                }
            }
        }
        private static void ReduceSimilarLineB(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples,
            Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples = null, double tolerance = Math.PI / 4)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                int cnt = dicTuples[key].Count;
                while (cnt-- > 1)
                {
                    if (!dicTuples.ContainsKey(key))
                    {
                        break;
                    }
                    var value = dicTuples[key];
                    int n = value.Count;
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        if (cntPts[i % n].DistanceTo(cntPts[i - 1]) < 1.0 || key.DistanceTo(cntPts[i - 1]) < 1.0 || cntPts[i % n].DistanceTo(key) < 1.0)
                        {
                            continue;
                        }
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    if (minDegree > tolerance)
                    {
                        break;
                    }
                    Point3d rmPt = new Point3d();
                    var ptA = minDegreePairPt.Item1;
                    var ptB = minDegreePairPt.Item2;
                    var disA = ptA.DistanceTo(key);
                    var disB = ptB.DistanceTo(key);
                    if (priority1stDicTuples != null && priority1stDicTuples.ContainsKey(key))
                    {
                        if (priority1stDicTuples[key].Contains(ptA) && !priority1stDicTuples[key].Contains(ptB))
                        {
                            if (disA - disB > 1200)
                            {
                                rmPt = ptA;
                            }
                            else rmPt = ptB;
                        }
                        else if (priority1stDicTuples[key].Contains(ptB) && !priority1stDicTuples[key].Contains(ptA))
                        {
                            if (disB - disA > 1200)
                            {
                                rmPt = ptB;
                            }
                            else rmPt = ptA;
                        }
                    }
                    if (rmPt == new Point3d())
                    {
                        if (disA >= disB)
                        {
                            rmPt = ptA;
                        }
                        else
                        {
                            rmPt = ptB;
                        }
                    }
                    DicTuplesDealer.DeleteFromDicTuples(rmPt, key, ref dicTuples);
                }
            }
        }
    }
}
