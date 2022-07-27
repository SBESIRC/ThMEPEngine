using System;
using System.Collections.Generic;
using System.Linq;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Operation.Relate;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerNetworkService
    {

        public static Dictionary<Point3d, double> GetAngleToPt(List<Point3d> sprinkPts, List<Line> pipeLine)
        {
            var dict = new Dictionary<Point3d, double>();

            foreach (var pt in sprinkPts)
            {
                if (dict.ContainsKey(pt) == false)
                {
                    var connectL = pipeLine.Where(x => x.GetDistToPoint(pt, false) <= 200).OrderBy(x => x.GetDistToPoint(pt, false));
                    if (connectL.Any())
                    {
                        var angle = connectL.First().Angle;
                        dict.Add(pt, angle);
                    }
                }
            }
            return dict;
        }

        public static List<Line> GetDTSeg(List<Point3d> sprinkPts)
        {
            var points = sprinkPts.ToCollection();
            var dtLine = points.DelaunayTriangulation();
            var dtPls = dtLine.Cast<Polyline>().ToList();
            var dtLinesAll = ThSprinklerLineService.PolylineToLine(dtPls);
            dtLinesAll = dtLinesAll.Distinct().ToList();

            return dtLinesAll;
        }

        /// <summary>
        /// 找所有正交的德劳内三角（DT）线段
        /// find all segments having orthogonal angle in Delaunary Triangulation of points
        /// </summary>
        /// <param name="sprinkPts">点位</param>
        /// <param name="dtSeg">正交的DT线段</param>
        /// <returns></returns>
        public static List<Line> FindOrthogonalAngleFromDT(List<Point3d> sprinkPts,List<Line> dtSeg)
        {
            var angleTol = 1;
            List<Line> dtOrthogonalSeg = new List<Line>();

            //var points = sprinkPts.ToCollection();
            //var dtLine = points.DelaunayTriangulation();
            //var dtPls = dtLine.Cast<Polyline>().ToList();
            //var dtLinesAll = ThSprinklerLineService.PolylineToLine(dtPls);

            foreach (Point3d pt in sprinkPts)
            {
                var ptLines = ThSprinklerLineService.GetConnLine(pt, dtSeg);
                if (ptLines.Count > 0)
                {
                    for (int i = 0; i < ptLines.Count; i++)
                    {
                        for (int j = i + 1; j < ptLines.Count; j++)
                        {
                            if (ThSprinklerLineService.IsOrthogonalAngle(ptLines[i].Angle, ptLines[j].Angle, angleTol))
                            {
                                dtOrthogonalSeg.Add(ptLines[i]);
                                dtOrthogonalSeg.Add(ptLines[j]);
                            }
                        }
                    }
                }
            }

            dtOrthogonalSeg = dtOrthogonalSeg.Distinct().ToList();
          
            return dtOrthogonalSeg;
        }


        public static void FilterTooLongSeg(ref List<Line> dtOdtSeg, double DTTol)
        {
            dtOdtSeg = dtOdtSeg.Where(x => x.Length <= DTTol).ToList();
        }

        /// <summary>
        /// 找DT数量最多长度前3名平均数。用于后面作为tolerance
        /// </summary>
        /// <param name="dtOrthogonalSeg">正交的DT线段</param>
        /// <returns></returns>
        public static double GetDTLength(List<Line> dtOrthogonalSeg)
        {
            var length = 2500.0;

            var lengthGroup = dtOrthogonalSeg.GroupBy(x => x.Length).ToDictionary(g => g.Key, g => g.ToList()).OrderByDescending(x => x.Value.Count).ToList();

            var averageC = 0;
            var lengthTemp = 0.0;

            for (int i = 0; i < 3 && i < lengthGroup.Count(); i++)
            {
                lengthTemp += lengthGroup[i].Key;
                averageC++;
            }
            if (lengthTemp > 0)
            {
                length = lengthTemp / averageC;
            }

            return length;
        }

        /// <summary>
        /// 容差1角度制内角度分类。可能有容差带来的累计错误
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <returns></returns>
        public static List<KeyValuePair<double, List<Line>>> ClassifyOrthogonalSeg(List<Line> dtOrthogonalSeg)
        {
            var angleTol = 1;
            var angleGroupDict = new Dictionary<double, List<Line>>();

            var angleGroupTemp = dtOrthogonalSeg.GroupBy(x => x.Angle).ToDictionary(g => g.Key, g => g.ToList()).OrderByDescending(x => x.Value.Count).ToDictionary(x => x.Key, x => x.Value);
            angleGroupDict.Add(angleGroupTemp.ElementAt(0).Key, angleGroupTemp.ElementAt(0).Value);

            for (int i = 1; i < angleGroupTemp.Count; i++)
            {
                var angleA = angleGroupTemp.ElementAt(i).Key;
                var bAdded = false;
                for (int j = 0; j < angleGroupDict.Count; j++)
                {
                    var angleB = angleGroupDict.ElementAt(j).Key;

                    if (ThSprinklerLineService.IsOrthogonalAngle(angleA, angleB, angleTol))
                    {
                        angleGroupDict[angleB].AddRange(angleGroupTemp[angleA]);
                        bAdded = true;
                        break;
                    }
                }
                if (bAdded == false)
                {
                    angleGroupDict.Add(angleA, angleGroupTemp[angleA]);
                }
            }

            var angleGroupList = angleGroupDict.ToList();

            return angleGroupList;
        }

        /// <summary>
        /// 将角度在容差范围内的DT散线加入组
        /// </summary>
        /// <param name="dtSeg"></param>
        /// <param name="groupList"></param>
        /// <param name="lengthTol"></param>
        public static void AddSingleDTLineToGroup(List<Line> dtSeg, List<KeyValuePair<double, List<Line>>> groupList, double lengthTol)
        {
            var angleTol = 1;
            var dtSegNotIn = dtSeg.Where(x => groupList.Where(g => g.Value.Contains(x)).Count() == 0 && x.Length <= lengthTol).ToList();

            for (int i = 0; i < dtSegNotIn.Count; i++)
            {
                AddLineToGroup(dtSegNotIn[i], ref groupList, angleTol);
            }
        }

        /// <summary>
        /// 点找容差范围内的点，形成的线在容差范围内，加入组
        /// </summary>
        /// <param name="dtSeg"></param>
        /// <param name="groupList"></param>
        /// <param name="pts"></param>
        /// <param name="lengthTol"></param>
        public static void AddSinglePTToGroup(List<KeyValuePair<double, List<Line>>> groupList, List<Point3d> pts, double lengthTol)
        {
            var lineList = new List<Line>();
            groupList.ForEach(o => lineList.AddRange(o.Value));

            var angleTol = 1;
            var newAddedline = new List<Line>();
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var nearPts = pts.Where(x => x.DistanceTo(pt) <= lengthTol && x != pt).OrderBy(x => x.DistanceTo(pt)).ToList();

                for (int j = 0; j < nearPts.Count; j++)
                {
                    var newLine = new Line(pt, nearPts[j]);

                    var n = 0;
                    for (; n < lineList.Count; n++)
                    {
                        var angleChecker = Math.Abs(lineList[n].LineDirection().DotProduct(newLine.LineDirection())) > 0.998;
                        // 如果存在两条线段overlap，则退出循环
                        if (angleChecker
                            && newLine.DistanceTo(lineList[n].StartPoint, false) < 1.0
                            && newLine.DistanceTo(lineList[n].EndPoint, false) < 1.0)
                        {
                            break;
                        }
                    }

                    // 当dtLines中没有重合线时
                    if (n == lineList.Count)
                    {
                        // 添加新线
                        if (AddLineToGroup(newLine, ref groupList, angleTol))
                        {
                            newAddedline.Add(newLine);
                            lineList.Add(newLine);
                        }
                    }
                }
            }
        }

        ///// <summary>
        ///// 将距离在容差范围内的短线加入组
        ///// </summary>
        ///// <param name="dtSeg"></param>
        ///// <param name="groupList"></param>
        ///// <param name="lengthTol"></param>
        //public static void AddShortLineToGroup(List<KeyValuePair<double, List<Line>>> groupList,
        //    List<Point3d> pts, List<Line> subMainPipe, double lengthTol)
        //{
        //    if (subMainPipe.Count == 0)
        //    {
        //        return;
        //    }

        //    //pts = pts.OrderBy(pt => pt.X).ToList();

        //    var lineList = new List<Line>();
        //    groupList.ForEach(o => lineList.AddRange(o.Value));

        //    var angleTol = 1;
        //    for (int i = 0; i < pts.Count; i++)
        //    {
        //        if (SearchClosePt(pts[i], subMainPipe, lengthTol, out var ptList))
        //        {
        //            ptList.ForEach(closePt =>
        //            {
        //                var newLine = new Line(pts[i], closePt);
        //                var reduceLine = newLine.ExtendLine(-10.0);
        //                var n = 0;
        //                for (; n < lineList.Count; n++)
        //                {
        //                    var angleChecker = Math.Abs(lineList[n].LineDirection().DotProduct(newLine.LineDirection())) > 0.998;
        //                    // 如果存在两条线段overlap，则退出循环
        //                    if (angleChecker
        //                        && (lineList[n].DistanceTo(reduceLine.StartPoint, false) < 1.0
        //                        || lineList[n].DistanceTo(reduceLine.EndPoint, false) < 1.0))
        //                    {
        //                        break;
        //                    }
        //                }

        //                // 当dtLines中没有重合线时
        //                if (n == lineList.Count)
        //                {
        //                    AddLineToGroup(LineExtend(newLine), ref groupList, angleTol);
        //                    lineList.Add(newLine);
        //                }
        //            });
        //        }
        //    }
        //}

        //public static List<KeyValuePair<double, List<Line>>> DeleteWallLine(List<KeyValuePair<double, List<Line>>> groupList, List<Polyline> geometry)
        //{
        //    var tempGroup = new List<KeyValuePair<double, List<Line>>>();
        //    groupList.ForEach(group =>
        //    {
        //        var spatialIndex = new ThCADCoreNTSSpatialIndex(group.Value.ToCollection());
        //        var vaildLines = new List<Line>();
        //        geometry.ForEach(g =>
        //        {
        //            vaildLines.AddRange(spatialIndex.SelectFence(g).OfType<Line>());
        //        });
        //        var filter = group.Value.Where(line => !vaildLines.Contains(line)).ToList();
        //        var pair = new KeyValuePair<double, List<Line>>(group.Key, filter);
        //        tempGroup.Add(pair);
        //    });

        //    return tempGroup;
        //}

        /////// <summary>
        /////// 拆分距离过远的组
        /////// </summary>
        /////// <param name="net"></param>
        /////// <param name="distTol"></param>
        /////// <returns></returns>
        ////public static List<KeyValuePair<double, List<Line>>> SeparateNetByDist(ThSprinklerNetGroup net, double distTol)
        ////{
        ////    var tempGroup = new List<KeyValuePair<double, List<Line>>>();

        ////    if (net.PtsGraph.Count > 1)
        ////    {
        ////        var regroup = RegroupNetByDist(net, distTol);

        ////        tempGroup.AddRange(SeparateNet(net, regroup));
        ////    }
        ////    else
        ////    {
        ////        tempGroup.Add(new KeyValuePair<double, List<Line>>(net.Angle, net.GetGraphLines(0)));
        ////    }

        ////    return tempGroup;
        ////}

        /////// <summary>
        /////// 找组的凸包
        /////// 凸包在整90度有bug不稳
        /////// </summary>
        /////// <param name="net"></param>
        ////public static List<Polyline> FilterGroupNetByConvexHull(ref List<ThSprinklerNetGroup> netList)
        ////{
        ////    var newNetList = new List<ThSprinklerNetGroup>();
        ////    netList = netList.OrderByDescending(x => x.Pts.Count).ToList();
        ////    var convexList = new List<Polyline>();

        ////    for (int i = 0; i < netList[0].PtsGraph.Count; i++)
        ////    {
        ////        var convex = GraphConvexHull(netList[0], i);
        ////        convexList.Add(convex);
        ////        //DrawUtils.ShowGeometry(convex, string.Format("l4Convex{0}-{1}", 0, i), 0 % 7, 30);
        ////    }

        ////    newNetList.Add(netList[0]);

        ////    for (int i = 1; i < netList.Count; i++)
        ////    {
        ////        var net = netList[i];
        ////        var lineList = new List<Line>();

        ////        for (int j = net.PtsGraph.Count - 1; j >= 0; j--)
        ////        {
        ////            var convex = GraphConvexHull(net, j);
        ////            if (convex.Area < 1.0)
        ////            {
        ////                continue;
        ////            }
        ////            //DrawUtils.ShowGeometry(convex, string.Format("l4Convex{0}-{1}", i, j), i % 7, 30);

        ////            var containby = convexList.Where(x => x.Contains(convex));
        ////            if (containby.Count() == 0)
        ////            {
        ////                convexList.Add(convex);
        ////                lineList.AddRange(net.GetGraphLines(j));
        ////            }
        ////        }
        ////        if (lineList.Count > 0)
        ////        {
        ////            var newNet = ThSprinklerNetGraphService.CreateNetwork(net.Angle, lineList);
        ////            newNetList.Add(newNet);
        ////        }
        ////    }
        ////    netList = newNetList;

        ////    return convexList;
        ////}

        //private static Polyline GraphConvexHull(ThSprinklerNetGroup net, int graphIdx)
        //{
        //    var convexPl = new Polyline();
        //    var netI = net.GetGraphPts(graphIdx);
        //    if (netI.Count < 3)
        //    {
        //        return new Polyline();
        //    }

        //    var netI2d = netI.Select(x => x.ToPoint2d()).ToList();
        //    //netI.ForEach(x => DrawUtils.ShowGeometry(x, "l4ConvexPts", 42, 30));

        //    if (netI2d.Select(o => o.X).Distinct().Count() > 1 && netI2d.Select(o => o.Y).Distinct().Count() > 1)
        //    {
        //        var convex = netI2d.GetConvexHull();
        //        for (int j = 0; j < convex.Count; j++)
        //        {
        //            convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
        //        }
        //        convexPl.Closed = true;

        //        return convexPl;
        //    }
        //    else
        //    {
        //        netI = netI.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
        //        var longLine = new Line(netI.First(), netI[netI.Count - 1]);
        //        return longLine.Buffer(1.0);
        //    }

        //}

        ///// <summary>
        ///// 拆分距离过远的组
        ///// 没测试！！！
        ///// </summary>
        ///// <param name="net"></param>
        ///// <param name="distTol"></param>
        //private static Dictionary<int, List<int>> RegroupNetByDist(ThSprinklerNetGroup net, double distTol)
        //{
        //    var distGroup = new Dictionary<int, List<int>>();
        //    distGroup.Add(0, new List<int> { 0 });

        //    for (int i = 0; i < net.PtsGraph.Count; i++)
        //    {
        //        var netPs = net.GetGraphPts(i);
        //        for (int j = i + 1; j < net.PtsGraph.Count; j++)
        //        {
        //            var netNextPs = net.GetGraphPts(j);
        //            var minDist = NearDist(netPs, netNextPs);
        //            if (minDist < distTol)
        //            {
        //                var keyContain = distGroup.Where(x => x.Value.Contains(i)).FirstOrDefault();
        //                if (keyContain.Equals(default(KeyValuePair<int, List<int>>)) == false)
        //                {
        //                    var jGroup = distGroup.Where(x => x.Value.Contains(j)).ToList();
        //                    if (jGroup.Count() > 0)
        //                    {
        //                        foreach (var otherG in jGroup)
        //                        {
        //                            if (otherG.Key != keyContain.Key)
        //                            {
        //                                foreach (var idxInJGroup in otherG.Value)
        //                                {
        //                                    if (distGroup[keyContain.Key].Contains(idxInJGroup) == false)
        //                                    {
        //                                        distGroup[keyContain.Key].Add(idxInJGroup);
        //                                    }
        //                                }
        //                                distGroup.Remove(otherG.Key);
        //                            }
        //                        }
        //                    }
        //                    if (distGroup[keyContain.Key].Contains(j) == false)
        //                    {
        //                        distGroup[keyContain.Key].Add(j);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var keyContain = distGroup.Where(x => x.Value.Contains(j));
        //                if (keyContain.Count() == 0)
        //                {
        //                    distGroup.Add(distGroup.Count, new List<int> { j });
        //                }
        //            }
        //        }
        //    }

        //    return distGroup;

        //}

        ///// <summary>
        ///// 组A 组B点位最近距离
        ///// </summary>
        ///// <param name="ptsA"></param>
        ///// <param name="ptsB"></param>
        ///// <returns></returns>
        //private static double NearDist(List<Point3d> ptsA, List<Point3d> ptsB)
        //{
        //    var minDist = -1.0;

        //    if (ptsA.Count > 0 && ptsA.Count > 0)
        //    {
        //        minDist = ptsA[0].DistanceTo(ptsB[0]);
        //        for (int i = 0; i < ptsA.Count; i++)
        //        {
        //            for (int j = 0; j < ptsB.Count; j++)
        //            {
        //                var dist = ptsA[i].DistanceTo(ptsB[j]);
        //                if (dist <= minDist)
        //                {
        //                    minDist = dist;
        //                }
        //            }
        //        }
        //    }

        //    return minDist;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="net"></param>
        //public static List<KeyValuePair<double, List<Line>>> SeparateNet(ThSprinklerNetGroup net, Dictionary<int, List<int>> group)
        //{
        //    var newGroup = new List<KeyValuePair<double, List<Line>>>();

        //    for (int i = 0; i < group.Count; i++)
        //    {
        //        var groupTemp = new KeyValuePair<double, List<Line>>(net.Angle, new List<Line>());
        //        var groupIdx = group.ElementAt(i).Value;
        //        groupIdx.ForEach(x => groupTemp.Value.AddRange(net.GetGraphLines(x)));
        //        newGroup.Add(groupTemp);
        //    }

        //    return newGroup;

        //}

        /// <summary>
        /// line和group同方向，加入组
        /// </summary>
        /// <param name="line"></param>
        /// <param name="angleGroup"></param>
        /// <param name="angleTol"></param>
        /// <returns></returns>
        private static bool AddLineToGroup(Line line, ref List<KeyValuePair<double, List<Line>>> angleGroup, double angleTol)
        {
            var bAdd = false;
            var angleA = line.Angle;

            for (int j = 0; j < angleGroup.Count; j++)
            {
                var angleB = angleGroup[j].Key;

                if (ThSprinklerLineService.IsOrthogonalAngle(angleA, angleB, angleTol))
                {
                    angleGroup[j].Value.Add(line);
                    bAdd = true;
                    break;
                }
            }
            return bAdd;
        }

        /// <summary>
        /// 删掉除了最多线以外组里出现过的线（删斜线）
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <returns></returns>
        public static List<KeyValuePair<double, List<Line>>> FilterGroupByPt(List<KeyValuePair<double, List<Line>>> groupList)
        {
            var tol = new Tolerance(10, 10);
            var filterGroup = new List<KeyValuePair<double, List<Line>>>();
            var groupPt = new List<Point3d>();

            groupList = groupList.OrderByDescending(x => x.Value.Count).ToList();
            filterGroup.Add(new KeyValuePair<double, List<Line>>(groupList[0].Key, groupList[0].Value));
            groupPt.AddRange(ThSprinklerLineService.LineListToPtList(groupList[0].Value));

            for (int i = 1; i < groupList.Count; i++)
            {
                var lineList = groupList[i].Value;

                for (int j = lineList.Count - 1; j >= 0; j--)
                {
                    var startInList = groupPt.Where(x => x.IsEqualTo(lineList[j].StartPoint, tol));
                    var endInList = groupPt.Where(x => x.IsEqualTo(lineList[j].EndPoint, tol));

                    //if (startInList.Count() > 0 || endInList.Count() > 0) 同时在两个组里的点会被删掉
                    if (startInList.Count() > 0 && endInList.Count() > 0)
                    {
                        lineList.RemoveAt(j);
                    }
                }
                if (lineList.Count > 0)
                {
                    filterGroup.Add(new KeyValuePair<double, List<Line>>(groupList.ElementAt(i).Key, lineList));
                    groupPt.AddRange(ThSprinklerLineService.LineListToPtList(lineList));
                }
            }

            return filterGroup;
        }

        public static List<Line> FilterDTOrthogonalToPipe(List<Line> dtOrthogonalSeg, Dictionary<Point3d, double> ptAngleDict)
        {
            var filterDTOrth = new List<Line>();

            var tol = new Tolerance(1, 1);
            var angleTol = 1;
            foreach (var pair in ptAngleDict)
            {
                var pt = pair.Key;

                var connectLine = ThSprinklerLineService.GetConnLine(pt, dtOrthogonalSeg);
                if (connectLine.Any())
                {
                    var sameWithPt = connectLine.Where(x => ThSprinklerLineService.IsOrthogonalAngle(x.Angle, pair.Value, angleTol));
                    if (sameWithPt.Any())
                    {
                        filterDTOrth.AddRange(sameWithPt);
                    }
                    else
                    {
                        filterDTOrth.AddRange(connectLine);
                    }
                }
            }

            return filterDTOrth;
        }


    }
}
