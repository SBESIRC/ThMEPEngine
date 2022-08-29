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
                    else
                    {
                        dict.Add(pt, 0);
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// 生成德劳内三角（DT）
        /// </summary>
        /// <param name="sprinkPts"></param>
        /// <returns></returns>
        public static List<Line> GetDTSeg(List<Point3d> sprinkPts)
        {
            var points = sprinkPts.ToCollection();
            var dtLine = points.DelaunayTriangulation();
            var dtPls = dtLine.OfType<Polyline>().ToList();
            var dtLinesAll = ThSprinklerLineService.PolylineToLine(dtPls);
            dtLinesAll = dtLinesAll.Distinct().ToList();

            return dtLinesAll;
        }

        ///// <summary>
        ///// 找所有正交的德劳内三角（DT）线段
        ///// find all segments having orthogonal angle in Delaunary Triangulation of points
        ///// </summary>
        ///// <param name="sprinkPts">点位</param>
        ///// <param name="dtSeg">正交的DT线段</param>
        ///// <returns></returns>
        //public static List<Line> FindOrthogonalAngleFromDT(List<Point3d> sprinkPts, List<Line> dtSeg)
        //{
        //    var angleTol = 1;
        //    List<Line> dtOrthogonalSeg = new List<Line>();

        //    //var points = sprinkPts.ToCollection();
        //    //var dtLine = points.DelaunayTriangulation();
        //    //var dtPls = dtLine.Cast<Polyline>().ToList();
        //    //var dtLinesAll = ThSprinklerLineService.PolylineToLine(dtPls);

        //    foreach (Point3d pt in sprinkPts)
        //    {
        //        var ptLines = ThSprinklerLineService.GetConnLine(pt, dtSeg, ThSprinklerDimCommon.Tol_ptToLine);
        //        if (ptLines.Count > 0)
        //        {
        //            for (int i = 0; i < ptLines.Count; i++)
        //            {
        //                for (int j = i + 1; j < ptLines.Count; j++)
        //                {
        //                    if (ThSprinklerLineService.IsOrthogonalAngle(ptLines[i].Angle, ptLines[j].Angle, angleTol))
        //                    {
        //                        dtOrthogonalSeg.Add(ptLines[i]);
        //                        dtOrthogonalSeg.Add(ptLines[j]);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    dtOrthogonalSeg = dtOrthogonalSeg.Distinct().ToList();

        //    return dtOrthogonalSeg;
        //}

        /// <summary>
        /// 过滤太长的DTSeg
        /// </summary>
        /// <param name="dtOdtSeg"></param>
        /// <param name="DTTol"></param>
        public static void FilterTooLongSeg(ref List<Line> dtOdtSeg, double DTTol)
        {
            dtOdtSeg = dtOdtSeg.Where(x => x.Length <= DTTol).ToList();
        }

        /// <summary>
        /// 找DT数量最多长度前3名平均数。用于后面作为tolerance
        /// 最小返回2000
        /// </summary>
        /// <param name="dtOrthogonalSeg">正交的DT线段</param>
        /// <returns></returns>
        public static double GetDTLength(List<Line> dtOrthogonalSeg)
        {
            var length = 2600.0;

            var group = dtOrthogonalSeg.GroupBy(x => Math.Round(x.Length / 100, MidpointRounding.AwayFromZero));
            var dict = group.OrderByDescending(x => x.Count()).ToDictionary(x => x.Key, x => x.Select(x => x.Length).ToList());

            var list = new List<double>();
            var maxI = 3;
            if (dict.Count() < maxI)
            {
                maxI = dict.Count;
            }

            for (int i = 0; i < maxI; i++)
            {
                list.AddRange(dict.ElementAt(i).Value);
            }

            if (list.Count > 0)
            {
                var ave = list.Average();
                //if (ave > 2000)
                //{
                    length = ave;
                //}
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
            if (dtOrthogonalSeg.Count > 0)
            {
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
            }

            var angleGroupList = angleGroupDict.ToList();

            return angleGroupList;
        }

        ///// <summary>
        ///// 将角度在容差范围内的DT散线加入组
        ///// </summary>
        ///// <param name="dtSeg"></param>
        ///// <param name="groupList"></param>
        ///// <param name="lengthTol"></param>
        //public static void AddSingleDTLineToGroup(List<Line> dtSeg, List<KeyValuePair<double, List<Line>>> groupList, double lengthTol)
        //{
        //    var angleTol = 1;
        //    var dtSegNotIn = dtSeg.Where(x => groupList.Where(g => g.Value.Contains(x)).Count() == 0 && x.Length <= lengthTol).ToList();

        //    for (int i = 0; i < dtSegNotIn.Count; i++)
        //    {
        //        AddLineToGroup(dtSegNotIn[i], ref groupList, angleTol);
        //    }
        //}

        ///// <summary>
        ///// 点找容差范围内的点，形成的线在容差范围内，加入组
        ///// </summary>
        ///// <param name="dtSeg"></param>
        ///// <param name="groupList"></param>
        ///// <param name="pts"></param>
        ///// <param name="lengthTol"></param>
        //public static void AddSinglePTToGroup(List<KeyValuePair<double, List<Line>>> groupList, List<Point3d> pts, double lengthTol)
        //{
        //    var lineList = new List<Line>();
        //    groupList.ForEach(o => lineList.AddRange(o.Value));

        //    var angleTol = 1;
        //    var newAddedline = new List<Line>();
        //    for (int i = 0; i < pts.Count; i++)
        //    {
        //        var pt = pts[i];
        //        var nearPts = pts.Where(x => x.DistanceTo(pt) <= lengthTol && x != pt).OrderBy(x => x.DistanceTo(pt)).ToList();

        //        for (int j = 0; j < nearPts.Count; j++)
        //        {
        //            var newLine = new Line(pt, nearPts[j]);

        //            var n = 0;
        //            for (; n < lineList.Count; n++)
        //            {
        //                var angleChecker = Math.Abs(lineList[n].LineDirection().DotProduct(newLine.LineDirection())) > 0.998;
        //                // 如果存在两条线段overlap，则退出循环
        //                if (angleChecker
        //                    && newLine.DistanceTo(lineList[n].StartPoint, false) < 1.0
        //                    && newLine.DistanceTo(lineList[n].EndPoint, false) < 1.0)
        //                {
        //                    break;
        //                }
        //            }

        //            // 当dtLines中没有重合线时
        //            if (n == lineList.Count)
        //            {
        //                // 添加新线
        //                if (AddLineToGroup(newLine, ref groupList, angleTol))
        //                {
        //                    newAddedline.Add(newLine);
        //                    lineList.Add(newLine);
        //                }
        //            }
        //        }
        //    }
        //}

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

        //public static Polyline ConvexHull(List<Point3d> pts)
        //{
        //    var convexPl = new Polyline();
        //    var netI2d = pts.Select(x => x.ToPoint2d()).ToList();

        //    if (netI2d.Select(o => o.X).Distinct().Count() > 1 && netI2d.Select(o => o.Y).Distinct().Count() > 1)
        //    {
        //        var convex = netI2d.GetConvexHull();
        //        for (int j = 0; j < convex.Count; j++)
        //        {
        //            convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
        //        }
        //        convexPl.Closed = true;
        //    }
        //    else
        //    {
        //        pts = pts.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
        //        var longLine = new Line(pts.First(), pts.Last());
        //        convexPl = longLine.Buffer(1.0);
        //    }

        //    return convexPl;
        //}

        /////// <summary>
        /////// 拆分距离过远的组
        /////// 没测试！！！
        /////// </summary>
        /////// <param name="net"></param>
        /////// <param name="distTol"></param>
        ////private static Dictionary<int, List<int>> RegroupNetByDist(ThSprinklerNetGroup net, double distTol)
        ////{
        ////    var distGroup = new Dictionary<int, List<int>>();
        ////    distGroup.Add(0, new List<int> { 0 });

        ////    for (int i = 0; i < net.PtsGraph.Count; i++)
        ////    {
        ////        var netPs = net.GetGraphPts(i);
        ////        for (int j = i + 1; j < net.PtsGraph.Count; j++)
        ////        {
        ////            var netNextPs = net.GetGraphPts(j);
        ////            var minDist = NearDist(netPs, netNextPs);
        ////            if (minDist < distTol)
        ////            {
        ////                var keyContain = distGroup.Where(x => x.Value.Contains(i)).FirstOrDefault();
        ////                if (keyContain.Equals(default(KeyValuePair<int, List<int>>)) == false)
        ////                {
        ////                    var jGroup = distGroup.Where(x => x.Value.Contains(j)).ToList();
        ////                    if (jGroup.Count() > 0)
        ////                    {
        ////                        foreach (var otherG in jGroup)
        ////                        {
        ////                            if (otherG.Key != keyContain.Key)
        ////                            {
        ////                                foreach (var idxInJGroup in otherG.Value)
        ////                                {
        ////                                    if (distGroup[keyContain.Key].Contains(idxInJGroup) == false)
        ////                                    {
        ////                                        distGroup[keyContain.Key].Add(idxInJGroup);
        ////                                    }
        ////                                }
        ////                                distGroup.Remove(otherG.Key);
        ////                            }
        ////                        }
        ////                    }
        ////                    if (distGroup[keyContain.Key].Contains(j) == false)
        ////                    {
        ////                        distGroup[keyContain.Key].Add(j);
        ////                    }
        ////                }
        ////            }
        ////            else
        ////            {
        ////                var keyContain = distGroup.Where(x => x.Value.Contains(j));
        ////                if (keyContain.Count() == 0)
        ////                {
        ////                    distGroup.Add(distGroup.Count, new List<int> { j });
        ////                }
        ////            }
        ////        }
        ////    }

        ////    return distGroup;

        ////}

        /////// <summary>
        /////// 组A 组B点位最近距离
        /////// </summary>
        /////// <param name="ptsA"></param>
        /////// <param name="ptsB"></param>
        /////// <returns></returns>
        ////private static double NearDist(List<Point3d> ptsA, List<Point3d> ptsB)
        ////{
        ////    var minDist = -1.0;

        ////    if (ptsA.Count > 0 && ptsA.Count > 0)
        ////    {
        ////        minDist = ptsA[0].DistanceTo(ptsB[0]);
        ////        for (int i = 0; i < ptsA.Count; i++)
        ////        {
        ////            for (int j = 0; j < ptsB.Count; j++)
        ////            {
        ////                var dist = ptsA[i].DistanceTo(ptsB[j]);
        ////                if (dist <= minDist)
        ////                {
        ////                    minDist = dist;
        ////                }
        ////            }
        ////        }
        ////    }

        ////    return minDist;
        ////}

        /////// <summary>
        /////// 
        /////// </summary>
        /////// <param name="net"></param>
        ////public static List<KeyValuePair<double, List<Line>>> SeparateNet(ThSprinklerNetGroup net, Dictionary<int, List<int>> group)
        ////{
        ////    var newGroup = new List<KeyValuePair<double, List<Line>>>();

        ////    for (int i = 0; i < group.Count; i++)
        ////    {
        ////        var groupTemp = new KeyValuePair<double, List<Line>>(net.Angle, new List<Line>());
        ////        var groupIdx = group.ElementAt(i).Value;
        ////        groupIdx.ForEach(x => groupTemp.Value.AddRange(net.GetGraphLines(x)));
        ////        newGroup.Add(groupTemp);
        ////    }

        ////    return newGroup;

        ////}

        ///// <summary>
        ///// line和group同方向，加入组
        ///// </summary>
        ///// <param name="line"></param>
        ///// <param name="angleGroup"></param>
        ///// <param name="angleTol"></param>
        ///// <returns></returns>
        //private static bool AddLineToGroup(Line line, ref List<KeyValuePair<double, List<Line>>> angleGroup, double angleTol)
        //{
        //    var bAdd = false;
        //    var angleA = line.Angle;

        //    for (int j = 0; j < angleGroup.Count; j++)
        //    {
        //        var angleB = angleGroup[j].Key;

        //        if (ThSprinklerLineService.IsOrthogonalAngle(angleA, angleB, angleTol))
        //        {
        //            angleGroup[j].Value.Add(line);
        //            bAdd = true;
        //            break;
        //        }
        //    }
        //    return bAdd;
        //}

        ///// <summary>
        ///// 删掉除了最多线以外组里出现过的线（删斜线）
        ///// </summary>
        ///// <param name="dtOrthogonalSeg"></param>
        ///// <returns></returns>
        //public static List<KeyValuePair<double, List<Line>>> FilterGroupByPt(List<KeyValuePair<double, List<Line>>> groupList)
        //{
        //    var tol = new Tolerance(10, 10);
        //    var filterGroup = new List<KeyValuePair<double, List<Line>>>();
        //    var groupPt = new List<Point3d>();

        //    groupList = groupList.OrderByDescending(x => x.Value.Count).ToList();
        //    filterGroup.Add(new KeyValuePair<double, List<Line>>(groupList[0].Key, groupList[0].Value));
        //    groupPt.AddRange(ThSprinklerLineService.LineListToPtList(groupList[0].Value));

        //    for (int i = 1; i < groupList.Count; i++)
        //    {
        //        var lineList = groupList[i].Value;

        //        for (int j = lineList.Count - 1; j >= 0; j--)
        //        {
        //            var startInList = groupPt.Where(x => x.IsEqualTo(lineList[j].StartPoint, tol));
        //            var endInList = groupPt.Where(x => x.IsEqualTo(lineList[j].EndPoint, tol));

        //            //if (startInList.Count() > 0 || endInList.Count() > 0) 同时在两个组里的点会被删掉
        //            if (startInList.Count() > 0 && endInList.Count() > 0)
        //            {
        //                lineList.RemoveAt(j);
        //            }
        //        }
        //        if (lineList.Count > 0)
        //        {
        //            filterGroup.Add(new KeyValuePair<double, List<Line>>(groupList.ElementAt(i).Key, lineList));
        //            groupPt.AddRange(ThSprinklerLineService.LineListToPtList(lineList));
        //        }
        //    }

        //    return filterGroup;
        //}

        //public static List<Line> FilterDTOrthogonalToPipe(List<Line> dtOrthogonalSeg, Dictionary<Point3d, double> ptAngleDict)
        //{
        //    var filterDTOrth = new List<Line>();

        //    var tol = new Tolerance(1, 1);
        //    var angleTol = 1;
        //    foreach (var pair in ptAngleDict)
        //    {
        //        var pt = pair.Key;

        //        var connectLine = ThSprinklerLineService.GetConnLine(pt, dtOrthogonalSeg, tol);
        //        if (connectLine.Any())
        //        {
        //            var sameWithPt = connectLine.Where(x => ThSprinklerLineService.IsOrthogonalAngle(x.Angle, pair.Value, angleTol));
        //            if (sameWithPt.Any())
        //            {
        //                filterDTOrth.AddRange(sameWithPt);
        //            }
        //            else
        //            {
        //                filterDTOrth.AddRange(connectLine);
        //            }
        //        }
        //    }

        //    return filterDTOrth;
        //}

        /// <summary>
        /// 筛选DT：和连接点同方向的DT线
        /// </summary>
        /// <param name="ptDtOriDict"></param>
        /// <param name="ptAngleDict"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, List<Line>> FilterDTOrthogonalToPipeAngle(Dictionary<Point3d, List<Line>> ptDtOriDict, Dictionary<Point3d, double> ptAngleDict)
        {
            var angleTol = 1;
            var tol = new Tolerance(1, 1);
            var ptDtDict = new Dictionary<Point3d, List<Line>>();

            foreach (var pair in ptDtOriDict)
            {
                var pt = pair.Key;
                if (ptDtDict.ContainsKey(pt) == false)
                {
                    ptDtDict.Add(pt, new List<Line>());
                }
                ptAngleDict.TryGetValue(pt, out var ptAngle);
                var ptLines = pair.Value;
                var orthoToPtAngle = ptLines.Where(x => ThSprinklerLineService.IsOrthogonalAngle(x.Angle, ptAngle, angleTol));

                if (orthoToPtAngle.Any())
                {
                    //检查另一个点的角度,是否和自己一样
                    foreach (var line in orthoToPtAngle)
                    {
                        var otherPt = GetLineOtherPt(pt, line);
                        ptAngleDict.TryGetValue(otherPt, out var otherPtAngle);
                        if (ThSprinklerLineService.IsOrthogonalAngle(ptAngle, otherPtAngle, angleTol))
                        {
                            ptDtDict[pt].Add(line);
                        }
                    }
                }
            }
            return ptDtDict;
        }

        private static Point3d GetLineOtherPt(Point3d pt, Line line)
        {
            var tol = new Tolerance(1, 1);
            var otherPt = line.EndPoint;
            if (line.EndPoint.IsEqualTo(pt, tol))
            {
                otherPt = line.StartPoint;
            }

            return otherPt;
        }

        /// <summary>
        /// 喷淋点和DT线连接关系Dict
        /// </summary>
        /// <param name="sprinkPts"></param>
        /// <param name="dtSeg"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, List<Line>> GetConnectPtDict(List<Point3d> sprinkPts, List<Line> dtSeg)
        {
            var ptDtDict = new Dictionary<Point3d, List<Line>>();

            foreach (var pt in sprinkPts)
            {
                if (ptDtDict.ContainsKey(pt) == false)
                {
                    ptDtDict.Add(pt, new List<Line>());
                    var connectLine = ThSprinklerLineService.GetConnLine(pt, dtSeg, ThSprinklerDimCommon.Tol_ptToLine);
                    ptDtDict[pt].AddRange(connectLine);
                }
            }

            return ptDtDict;

        }

        /// <summary>
        /// 找喷淋点正交线个数为0的点，找德劳内正交
        /// 德劳内线另一端也必须是空点才行
        /// </summary>
        /// <param name="ptDtDict"></param>
        /// <param name="ptDtOriDict"></param>
        public static void AddOrthoDTIfNoLine(ref Dictionary<Point3d, List<Line>> ptDtDict, Dictionary<Point3d, List<Line>> ptDtOriDict)
        {
            var angleTol = 1.0;
            var ptDtAddOrthoDict = new Dictionary<Point3d, List<Line>>();

            for (int i = 0; i < ptDtDict.Count; i++)
            {
                var pair = ptDtDict.ElementAt(i);
                if (pair.Value.Count > 0)
                {
                    continue;
                }

                //如果是0 则找正交
                var pt = pair.Key;
                ptDtOriDict.TryGetValue(pt, out var dtSeg);

                for (int n = 0; n < dtSeg.Count; n++)
                {
                    for (int j = n + 1; j < dtSeg.Count; j++)
                    {
                        if (ThSprinklerLineService.IsOrthogonalAngle(dtSeg[n].Angle, dtSeg[j].Angle, angleTol))
                        {
                            if (IsOtherEmptyPt(pt, dtSeg[n], ptDtDict) && IsOtherEmptyPt(pt, dtSeg[j], ptDtDict))
                            {
                                if (ptDtAddOrthoDict.ContainsKey(pt) == false)
                                {
                                    ptDtAddOrthoDict.Add(pt, new List<Line>());
                                }
                                ptDtAddOrthoDict[pt].Add(dtSeg[n]);
                                ptDtAddOrthoDict[pt].Add(dtSeg[j]);
                            }
                        }
                    }
                }
            }

            foreach (var pair in ptDtAddOrthoDict)
            {
                ptDtDict[pair.Key].AddRange(pair.Value);
            }
        }

        private static bool IsOtherEmptyPt(Point3d pt, Line l, Dictionary<Point3d, List<Line>> ptDtDict)
        {
            var isOtherEmpty = false;

            var otherPtN = GetLineOtherPt(pt, l);
            var otherPtKey = GetKey(ptDtDict, otherPtN);

            if (otherPtKey != Point3d.Origin)
            {
                if (ptDtDict[otherPtKey].Count() == 0)
                {
                    isOtherEmpty = true;
                }
            }

            return isOtherEmpty;

        }

        private static Point3d GetKey(Dictionary<Point3d, List<Line>> ptDtDict, Point3d pt)
        {
            var tol = new Tolerance(1, 1);
            var key = new Point3d();
            var keypair = ptDtDict.Where(x => x.Key.IsEqualTo(pt, tol));
            if (keypair.Count() > 0)
            {
                key = keypair.First().Key;
            }

            return key;
        }

        /// <summary>
        /// 找符合方向的原本不在dt里面的线
        /// 点找容差范围内的点，形成的线在容差范围内，加入
        /// </summary>
        /// <param name="dtSeg"></param>
        /// <param name="groupList"></param>
        /// <param name="pts"></param>
        /// <param name="lengthTol"></param>
        public static void AddSinglePTToGroup(ref Dictionary<Point3d, List<Line>> ptDtDict, Dictionary<Point3d, double> ptAngleDict, double lengthTol)
        {
            var angleTol = 1;
            var allLine = ptDtDict.SelectMany(x => x.Value).Distinct().ToList();
            var addNew = new Dictionary<Point3d, List<Line>>();

            for (int i = 0; i < ptDtDict.Count(); i++)
            {
                var pt = ptDtDict.ElementAt(i).Key;
                ////debug
                //if (850764.7 - 0.5 <= pt.X && pt.X <= 850764.7 + 0.5 && 379077.3 - 0.5 <= pt.Y && pt.Y <= 379077.3 + 0.5)
                //{
                //    var a = 1;
                //}

                if (addNew.ContainsKey(pt) == false)
                {
                    addNew.Add(pt, new List<Line>());
                }

                var nearPts = ptAngleDict.Where(x => x.Key.DistanceTo(pt) <= lengthTol &&
                                                    x.Key != pt &&
                                                   ThSprinklerLineService.IsOrthogonalAngle(ptAngleDict[pt], ptAngleDict[x.Key], angleTol)
                                                ).OrderBy(x => x.Key.DistanceTo(pt)).Select(x => x.Key).ToList();

                for (int j = 0; j < nearPts.Count; j++)
                {
                    var newLine = new Line(pt, nearPts[j]);
                    var angleChecker = ThSprinklerLineService.IsOrthogonalAngle(ptAngleDict[pt], newLine.Angle, angleTol);
                    if (angleChecker == false)
                    {
                        continue;
                    }

                    //这里其实还是有bug，增加个新逻辑：检查同方向角度。后面的继续分组能找回来所以不作处理了
                    int idxCheckOverlap = 0;
                    for (; idxCheckOverlap < allLine.Count; idxCheckOverlap++)
                    {
                        var overlapCheck = ThSprinklerLineService.IsOverlapLine(allLine[idxCheckOverlap], newLine);
                        // 如果存在两条线段overlap，则退出循环
                        if (overlapCheck == true)
                        {
                            break;
                        }
                    }

                    if (idxCheckOverlap != allLine.Count)
                    {
                        continue;
                    }

                    int idxCheckOverlapNewLine = 0;
                    for (; idxCheckOverlapNewLine < addNew[pt].Count(); idxCheckOverlapNewLine++)
                    {
                        var overlapCheck = ThSprinklerLineService.IsOverlapLine(addNew[pt][idxCheckOverlapNewLine], newLine);
                        // 如果存在两条线段overlap，则退出循环
                        if (overlapCheck == true)
                        {
                            break;
                        }
                    }


                    // 当dtLines中没有重合线时
                    if (idxCheckOverlapNewLine == addNew[pt].Count)
                    {
                        addNew[pt].Add(newLine);
                    }
                }
            }

            foreach (var pair in addNew)
            {
                ptDtDict[pair.Key].AddRange(pair.Value);
            }
        }

        public static void RemoveDuplicate(ref List<Line> lines)
        {
            var tol = new Tolerance(1, 1);
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if ((lines[i].StartPoint.IsEqualTo(lines[j].StartPoint, tol) && lines[i].EndPoint.IsEqualTo(lines[j].EndPoint)) ||
                        (lines[i].EndPoint.IsEqualTo(lines[j].StartPoint, tol) && lines[i].StartPoint.IsEqualTo(lines[j].EndPoint)))
                    {
                        lines.Remove(lines[i]);
                        break;
                    }
                }
            }
        }
    }
}
