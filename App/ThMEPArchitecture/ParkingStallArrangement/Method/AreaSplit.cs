using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ParkingStallArrangement.General;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class AreaSplit
    {
        public static List<Polyline> Split(this Line line, Polyline polygon, double tor = 5.0)
        {
            var lines = polygon.ToLines();
            lines.Add(line);

            var extendLines = lines.Select(l => l.ExtendLine(tor)).ToCollection();
            var areas = extendLines.PolygonsEx();


            var rst = new List<Polyline>();
            foreach (var area in areas)
            {
                rst.Add(area as Polyline);
            }

            return rst;
        }



        public static List<Polyline> Split2(this Line line, Polyline polygon, double tor = 1.0)
        {
            var lines = polygon.ToLines();
            var lineEx = line.ExtendLine(1e6);
            lines.Add(lineEx);

            var extendLines = lines.Select(l => l.ExtendLine(tor)).ToCollection();
            var areas = extendLines.PolygonsEx();

            var rst = new List<Polyline>();
            foreach (var area in areas)
            {
                rst.Add(area as Polyline);
            }

            return rst;
        }

        public static bool IsCorrectSegLines(int i, ref List<Polyline> areas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex,
            GaParameter gaParameter, out double maxVal, out double minVal)
        {
            maxVal = 0;
            minVal = 0;
            double simplifyFactor = 1.0;
            var segLine = gaParameter.SegLine[i];//分割线
            for (int k = areas.Count - 1; k >= 0; k--)//区域遍历
            {
                areas[k] = areas[k].TPSimplify(simplifyFactor);
                var area = areas[k];
                var segAreas = segLine.Split(area);

                if (segAreas.Count == 2)
                {
                    areas.RemoveAt(k);
                    areas.AddRange(segAreas);
                    foreach(var segArea in segAreas)
                    {
                        var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(segArea);
                        var boundPt = segLine.GetBoundPt(buildLines, segArea);
                        if(segLine.GetValueType(boundPt))
                        {
                            maxVal = segLine.GetMinDist(boundPt)-2760;
                        }
                        else
                        {
                            minVal = -segLine.GetMinDist(boundPt)+2760;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool IsCorrectSegLines(Line line, ref List<Polyline> areas)
        {
            var segLine = line;//分割线
            var breakFlag = false;
            for (int k = areas.Count - 1; k >= 0; k--)//区域遍历
            {
                var area = areas[k];
                var segAreas = segLine.Split(area);

                if (segAreas.Count == 2)
                {
                    foreach(var a in segAreas)
                    {
                        if(a.Area < 100)
                        {
                            breakFlag = true;
                            return false;
                        }
                    }
                    if (breakFlag)
                        continue;
                    areas.RemoveAt(k);
                    areas.AddRange(segAreas);
                    return true;
                }
                else if(segAreas.Count >2)
                {
                    try
                    {
                        var sortedAreas = segAreas.OrderByDescending(e => e.Area).ToList();
                        for (int i = 0; i < 2; i++)
                        {
                            var a = sortedAreas[i];
                            if (a.Area < 100)
                            {
                                return false;
                            }
                        }
                        areas.RemoveAt(k);
                        areas.Add(sortedAreas[0]);
                        areas.Add(sortedAreas[1]);
                        return true;
                    }
                    catch(Exception ex)
                    {
                        ;
                    }
                }
            }
            return false;
        }

        public static bool IsCorrectSegLines2(Line line, ref List<Polyline> areas)
        {
            var segLine = line;//分割线
            var middlePt = segLine.GetMiddlePt();
            
            for (int k = areas.Count - 1; k >= 0; k--)//区域遍历
            {
                var breakFlag = false;
                var area = areas[k];
                if(area.Contains(middlePt))
                {
                    var segAreas = segLine.Split2(area);
                    if (segAreas.Count == 2)
                    {
                        foreach (var a in segAreas)
                        {
                            if (a.Area < 100)
                            {
                                breakFlag = true; 
                                break;
                            }
                        }
                        if (breakFlag)
                            continue;
                        areas.RemoveAt(k);
                        areas.AddRange(segAreas);
                        return true;
                    }
                    else if (segAreas.Count > 2)
                    {
                        try
                        {
                            var sortedAreas = segAreas.OrderByDescending(e => e.Area).ToList();
                            for (int i = 0; i < 2; i++)
                            {
                                var a = sortedAreas[i];
                                if (a.Area < 100)
                                {
                                    breakFlag = true;
                                    continue ;
                                }
                            }
                            areas.RemoveAt(k);
                            areas.Add(sortedAreas[0]);
                            areas.Add(sortedAreas[1]);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        private static Point3d GetBoundPt(this Line line, DBObjectCollection buildLines, Polyline segArea)
        {
            
            if (buildLines.Count == 0)//区域内没有建筑物
            {
                var pts = segArea.GetPoints().ToList();
                return pts.OrderBy(e => line.GetMinDist(e)).Last();//返回最远距离
            }
            var closedPts = new List<Point3d>();
            foreach(var build in buildLines)
            {
                var br = build as BlockReference;
                var pline = br.GetRect();
                var pts = pline.GetPoints().ToList();
                closedPts.Add(pts.OrderBy(e => line.GetMinDist(e)).First());
            }
            return closedPts.OrderBy(e => line.GetMinDist(e)).First();//返回最近距离
        }
        
        private static bool GetValueType(this Line line, Point3d pt)
        {
            //判断点是直线的上限还是下限
            var dir = line.GetDirection() > 0;
            if (dir)
            {
                return pt.X > line.StartPoint.X;
            }
            else
            {
                return pt.Y > line.StartPoint.Y;
            }
        }
        private static double GetMinDist(this Line line, Point3d pt)
        {
            var targetPt = line.GetClosestPointTo(pt, true);
            return pt.DistanceTo(targetPt);
        }
    }
    public class Dfs
    {
        public static void dfsSplit(ref HashSet<int> usedLines, ref List<Polyline> areas, ref List<Line> sortSegLines, 
            ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, GaParameter gaParameter, ref List<double> maxVals, ref List<double> minVals)
        {
            if (usedLines.Count == gaParameter.LineCount)//分割线使用完毕, 退出递归
            {
                return;
            }
            for (int i = 0; i < gaParameter.LineCount; i++)
            {
                if(usedLines.Contains(i))
                {
                    continue;
                }
                var line = gaParameter.SegLine[i];
                if (AreaSplit.IsCorrectSegLines(i, ref areas, buildLinesSpatialIndex, gaParameter, out double maxVal, out double minVal))//分割线正好分割区域
                {
                    sortSegLines.Add(line);
                    maxVals.Add(maxVal);
                    minVals.Add(minVal);
                    usedLines.Add(i);
                }
            }

            //递归搜索
            dfsSplit(ref usedLines, ref areas, ref sortSegLines, buildLinesSpatialIndex, gaParameter, ref maxVals, ref minVals);
        }
    }
}

