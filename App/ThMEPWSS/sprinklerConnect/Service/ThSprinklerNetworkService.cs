using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Operation.Relate;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;

namespace ThMEPWSS.SprinklerConnect.Service
{
    class ThSprinklerNetworkService
    {

        /// <summary>
        /// 找所有正交的德劳内三角（DT）线段
        /// find all segments having orthogonal angle in Delaunary Triangulation of points
        /// </summary>
        /// <param name="sprinkPts">点位</param>
        /// <param name="dtSeg">正交的DT线段</param>
        /// <returns></returns>
        public static List<Line> FindOrthogonalAngleFromDT(List<Point3d> sprinkPts, out List<Line> dtSeg)
        {
            var angleTol = 1;
            List<Line> dtOrthogonalSeg = new List<Line>();

            var points = sprinkPts.ToCollection();
            var dtLine = points.DelaunayTriangulation();
            var dtPls = dtLine.Cast<Polyline>().ToList();
            var dtLinesAll = ThSprinklerLineService.PolylineToLine(dtPls);

            foreach (Point3d pt in points)
            {
                var ptLines = ThSprinklerLineService.GetConnLine(pt, dtLinesAll);
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
            dtSeg = dtLinesAll.Distinct().ToList();

            DrawUtils.ShowGeometry(dtSeg, "l0DT", 154);
            //DrawUtils.ShowGeometry(dtOrthogonalSeg, "l0DTlins", 1);

            return dtOrthogonalSeg;
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

            for (int i = 0; i < 3; i++)
            {
                if (i <= lengthGroup.Count())
                {
                    lengthTemp = lengthTemp + lengthGroup[i].Key;
                    averageC = averageC + 1;
                }
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
        /// <param name="dtLines"></param>
        /// <param name="groupList"></param>
        /// <param name="pts"></param>
        /// <param name="lengthTol"></param>
        public static void AddSinglePTToGroup(List<Line> dtLines, List<KeyValuePair<double, List<Line>>> groupList, List<Point3d> pts, double lengthTol)
        {
            var angleTol = 1;
            var newAddedline = new List<Line>();
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var nearPts = pts.Where(x => x.DistanceTo(pt) <= lengthTol && x != pt).OrderBy(x => x.DistanceTo(pt)).ToList();

                for (int j = 0; j < nearPts.Count; j++)
                {
                    var nearPt = nearPts[j];
                    var newLine = new Line(pt, nearPt);

                    var overlapDT = dtLines.Where(x => OverlapLine(x, newLine) == true);
                    var overlapTempGroup = newAddedline.Where(x => OverlapLine(x, newLine) == true);

                    if (overlapDT.Count() == 0 && overlapTempGroup.Count() == 0)
                    {
                        var bAdd = AddLineToGroup(newLine, ref groupList, angleTol);
                        if (bAdd == true)
                        {
                            newAddedline.Add(newLine);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 拆分距离过远的组（没做完）
        /// </summary>
        /// <param name="net"></param>
        /// <param name="distTol"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> SeparateNetByDist(ThSprinklerNetGroup net, double distTol)
        {
            var separateNet = new List<ThSprinklerNetGroup>();
            if (net.ptsGraph.Count > 1)
            {
                var regroup = RegroupNetByDist(net, distTol);

                //SeparateNet;
            }
            else
            {
                separateNet.Add(net);
            }

            return separateNet;
        }

        /// <summary>
        /// 找组的凸包
        /// 凸包在整90度有bug不稳
        /// </summary>
        /// <param name="net"></param>
        public static void SeparateNetByConvexHull(ThSprinklerNetGroup net)
        {
            for (int i = 0; i < net.ptsGraph.Count; i++)
            {
                var netI = net.GetGraphPts(i);
                var netI2d = netI.Select(x => x.ToPoint2d()).ToList();
                netI.ForEach(x => DrawUtils.ShowGeometry(x, "l2ConvexPts", 42, 30));

                var convex = netI2d.GetConvexHull();
                for (int j = 0; j < convex.Count - 1; j++)
                {
                    DrawUtils.ShowGeometry(new Line(convex.ElementAt(j).ToPoint3d(), convex.ElementAt(j + 1).ToPoint3d()), "l2Convex", i % 7, 30);
                }

            }
        }

        /// <summary>
        /// 拆分距离过远的组
        /// 没测试！！！
        /// </summary>
        /// <param name="net"></param>
        /// <param name="distTol"></param>
        private static Dictionary<int, List<int>> RegroupNetByDist(ThSprinklerNetGroup net, double distTol)
        {
            var distGroup = new Dictionary<int, List<int>>();
            distGroup.Add(0, new List<int> { 0 });

            for (int i = 0; i < net.ptsGraph.Count; i++)
            {
                var netPs = net.GetGraphPts(i);
                for (int j = i + 1; j < net.ptsGraph.Count; j++)
                {
                    var netNextPs = net.GetGraphPts(j);
                    var minDist = NearDist(netPs, netNextPs);
                    if (minDist < distTol)
                    {
                        var keyContain = distGroup.Where(x => x.Value.Contains(i)).FirstOrDefault();
                        if (keyContain.Equals(default(KeyValuePair<int, List<int>>)) == false)
                        {
                            var jGroup = distGroup.Where(x => x.Value.Contains(j));
                            if (jGroup.Count() > 0)
                            {
                                foreach (var otherG in jGroup)
                                {
                                    if (otherG.Key != keyContain.Key)
                                    {
                                        foreach (var idxInJGroup in otherG.Value)
                                        {
                                            if (distGroup[keyContain.Key].Contains(idxInJGroup) == false)
                                            {
                                                distGroup[keyContain.Key].Add(idxInJGroup);
                                            }
                                        }
                                        distGroup.Remove(otherG.Key);
                                    }
                                }
                            }
                            if (distGroup[keyContain.Key].Contains(j) == false)
                            {
                                distGroup[keyContain.Key].Add(j);
                            }
                        }
                    }
                    else
                    {
                        var keyContain = distGroup.Where(x => x.Value.Contains(j));
                        if (keyContain.Count() == 0)
                        {
                            distGroup.Add(distGroup.Count, new List<int> { j });
                        }
                    }
                }
            }

            return distGroup;

        }

        /// <summary>
        /// 组A 组B点位最近距离
        /// </summary>
        /// <param name="ptsA"></param>
        /// <param name="ptsB"></param>
        /// <returns></returns>
        private static double NearDist(List<Point3d> ptsA, List<Point3d> ptsB)
        {
            var minDist = -1.0;

            if (ptsA.Count > 0 && ptsA.Count > 0)
            {
                minDist = ptsA[0].DistanceTo(ptsB[0]);
                for (int i = 0; i < ptsA.Count; i++)
                {
                    for (int j = 0; j < ptsB.Count; j++)
                    {
                        var dist = ptsA[i].DistanceTo(ptsB[j]);
                        if (dist <= minDist)
                        {
                            minDist = dist;
                        }
                    }
                }
            }

            return minDist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="net"></param>
        public static void SeparateNet(ThSprinklerNetGroup net)
        {

        }

        /// <summary>
        /// 检查A B是否是overlap或contains关系
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static bool OverlapLine(Line A, Line B)
        {
            var bReturn = false;
            var matrix = RelateOp.Relate(A.ToNTSLineString(), B.ToNTSLineString());
            var r1 = matrix.IsCrosses(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
            var r2 = matrix.IsOverlaps(NetTopologySuite.Geometries.Dimension.Curve, NetTopologySuite.Geometries.Dimension.Curve);
            var r3 = matrix.IsContains();
            var r4 = matrix.IsCoveredBy();
            //var r5 = matrix.IsTouches(NetTopologySuite.Geometries.Dimension.Surface, NetTopologySuite.Geometries.Dimension.Surface);

            if (r1 == false && (r2 || r3 || r4))
            {
                bReturn = true;
            }

            return bReturn;
        }

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

    }
}
