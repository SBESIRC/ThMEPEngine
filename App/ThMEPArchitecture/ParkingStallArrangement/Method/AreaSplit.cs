using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ParkingStallArrangement.General;
using System.Diagnostics;
using NetTopologySuite.Geometries;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class AreaSplit
    {
        public static List<Polyline> SplitByLine(this Line line, Polyline polygon, double tor = 5.0)
        {
            var lines = polygon.ToLines();
            lines.Add(line);

            //return new List<Polyline>();

            var extendLines = lines.Select(l => l.ExtendLine(tor)).ToCollection();
           
            var areas = extendLines.PolygonsEx();
            foreach (DBObject e in extendLines)
            {
                e.Dispose();
            }
            extendLines.Dispose();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                lines[i].Dispose();
            }
            lines.Clear();
            var rst = new List<Polyline>();
            foreach (DBObject area in areas)
            {
                rst.Add(area as Polyline);
                //area.Dispose();
            }
            return rst;
        }

        public static List<Polyline> SplitArea(this List<Line> splitterLines, List<Polyline> polyLines, double tor = 5.0)
        {
            var rst = new List<Polyline>();

            var allLines = new List<Line>();
            var boundlines = polyLines.SelectMany(pl=>pl.ToLines());
            allLines.AddRange(splitterLines);
            allLines.AddRange(boundlines);
            var extendLineStrings = allLines.Select(l => {
                var el = l.ExtendLine(tor);
                var linestring = el.ToNTSLineString();
                el.Dispose();
                return linestring;
            });
           
            var multiLineStrings = new MultiLineString(extendLineStrings.ToArray());

            foreach(var l in boundlines)
            {
                l.Dispose();
            }

            var geos = multiLineStrings.Polygonize();
            foreach (Polygon plg in geos)
            {
                rst.Add(plg.ToDbEntity() as Polyline);
            }

            return rst;
        }
        public static List<Polyline> SplitByExtentedLine(this Line line, Polyline polygon, double tor = 1.0)
        {
            var lines = polygon.ToLines();
            var lineEx = line.ExtendLine(1e6);
            lines.Add(lineEx);

            var extendLines = lines.Select(l => l.ExtendLine(tor)).ToCollection();
            var areas = extendLines.PolygonsEx();

            lines.ForEach(e => e.Dispose());
            lines.Clear();
            foreach (DBObject e in extendLines)
            {
                e.Dispose();
            }

            extendLines.Dispose();
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
                var segAreas = segLine.SplitByLine(area);

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
                if(segAreas.Count > 2)
                {
                    var sortedAreas = segAreas.OrderByDescending(a => a.Area).ToList();
                    var res = sortedAreas.Take(2);
                    areas.RemoveAt(k);
                    areas.AddRange(res);
                    foreach (var segArea in res)
                    {
                        var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(segArea);
                        var boundPt = segLine.GetBoundPt(buildLines, segArea);
                        if (segLine.GetValueType(boundPt))
                        {
                            maxVal = segLine.GetMinDist(boundPt) - 2760;
                        }
                        else
                        {
                            minVal = -segLine.GetMinDist(boundPt) + 2760;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool SplitAreasByOrgiginLine(Line line, ref List<Polyline> areas)
        {
            double minArea = 1e8;
            var segLine = line;//分割线
            var breakFlag = false;
            for (int k = areas.Count - 1; k >= 0; k--)//区域遍历
            {
                var area = areas[k];

                var segAreas = segLine.SplitByLine(area);             
               
                
                if (segAreas.Count == 2)
                {
                    foreach (var a in segAreas)
                    {
                        if (a.Area < minArea)
                        {
                            breakFlag = true;
                            //return false;
                        }
                    }
                    if (breakFlag)
                    {
                        breakFlag = false;
                        continue;
                    }
                    areas.RemoveAt(k);
                    areas.AddRange(segAreas);
                    return true;
                }
                else if (segAreas.Count > 2)
                {
                    var sortedAreas = segAreas.OrderByDescending(e => e.Area).ToList();
                    for (int i = 0; i < 2; i++)
                    {
                        var a = sortedAreas[i];
                        if (a.Area < minArea)
                        {
                            return false;
                        }
                    }
                    areas.RemoveAt(k);
                    areas.Add(sortedAreas[0]);
                    areas.Add(sortedAreas[1]);
                    return true;
                }
            }
            return false;
        }

        public static bool SplitAreasByExtentedLine(Line line, ref List<Polyline> areas)
        {
            double minArea = 1e8;
            var segLine = line;//分割线
            var middlePt = segLine.GetMiddlePt();
            
            for (int k = areas.Count - 1; k >= 0; k--)//区域遍历
            {
                var breakFlag = false;
                var area = areas[k];
                if(area.Contains(middlePt))
                {
                    var segAreas = segLine.SplitByExtentedLine(area);
                   
                    if (segAreas.Count == 2)
                    {
                        foreach (var a in segAreas)
                        {
                            if (a.Area < minArea)
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
                        var sortedAreas = segAreas.OrderByDescending(e => e.Area).ToList();
                        for (int i = 0; i < 2; i++)
                        {
                            var a = sortedAreas[i];
                            if (a.Area < minArea)
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

        public static Line VerticalSeg(ref List<Polyline> orgAreas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, out double maxVal, out double minVal)
        {
            double dist = 5000;
            double laneWidth = 5500;
            maxVal = 0;
            minVal = 0;
            var area = orgAreas.First();//进来就一个区域
            var orderPts = area.GetPoints().OrderBy(pt => pt.X);
            var orderPts2 = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPts.First().X;
            var right = orderPts.Last().X;
            var buttom = orderPts2.First().Y - dist;
            var upper = orderPts2.Last().Y + dist;

            var startX = left;
            while(startX < right)//直线在框内
            {
                var upPt = new Point3d(startX, upper, 0);
                var buttomPt = new Point3d(startX, buttom, 0);
                var segLine = new Line(upPt, buttomPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if(segRectInBuild.Count > 0)
                {
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                //var areas = new List<Polyline>() { area };
                if (IsCorrectSegLines(segLine, ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal))
                {
                    return segLine;
                }
                startX += laneWidth;
            }
            return null;
        }
        public static Line ThroughVerticalSeg(Polyline wallLine, ref List<Polyline> orgAreas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, out double maxVal, out double minVal)
        {
            double dist = 5000;
            double laneWidth = 5500;
            maxVal = 0;
            minVal = 0;
            var area = orgAreas.First();//进来就一个区域
            var orderPts = area.GetPoints().OrderBy(pt => pt.X);
            var orderPts2 = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPts.First().X;
            var right = orderPts.Last().X;
            var buttom = orderPts2.First().Y - dist;
            var upper = orderPts2.Last().Y + dist;

            var startX = left;
            while (startX < right)//直线在框内
            {
                var upPt = new Point3d(startX, upper, 0);
                var buttomPt = new Point3d(startX, buttom, 0);
                var segLine = new Line(upPt, buttomPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);//分割线穿过墙
                if (segRectInBuild.Count == 1)
                {
                    var build = segRectInBuild[0] as BlockReference;
                    var buildRectEx = build.GetRectExtend(laneWidth);//建筑物外框扩展
                    if (!buildRectEx.Intersects(wallLine))//建筑物和墙线不相交
                    {
                        startX += laneWidth;
                        continue;//分割线穿墙直接删除
                    }
                }
                if (segRectInBuild.Count > 1)
                {
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                if (IsCorrectSegLines2(segLine, ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal))
                {
                    return segLine;
                }
                startX += laneWidth;
            }
            return null;
        }

        public static Line HorizontalSeg(ref List<Polyline> orgAreas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, out double maxVal, out double minVal)
        {
            double dist = 5000;
            double laneWidth = 5500;
            maxVal = 0;
            minVal = 0;
            var area = orgAreas.First();//进来就一个区域
            var orderPts = area.GetPoints().OrderBy(pt => pt.X);
            var orderPts2 = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPts.First().X - dist;
            var right = orderPts.Last().X + dist;
            var buttom = orderPts2.First().Y;
            var upper = orderPts2.Last().Y;

            var startX = buttom;
            while (startX < upper)//直线在框内
            {
                var leftPt = new Point3d(left, startX, 0);
                var rightPt = new Point3d(right, startX, 0);
                var segLine = new Line(leftPt, rightPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if (segRectInBuild.Count > 0)
                {
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                //var areas = new List<Polyline>() { area };
                if (IsCorrectSegLines(segLine, ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal))
                {
                    return segLine;
                }
                startX += laneWidth;
            }
            return null;
        }

        public static bool IsCorrectSegLines(Line segLine, ref List<Polyline> areas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex,
          out double maxVal, out double minVal)
        {
            maxVal = 0;
            minVal = 0;
            var area = areas.First();//其实进来也就一个区域

            var segAreas = segLine.SplitByLine(area);
            if(segAreas.Count < 2)
            {
                return false;
            }
                
            var sortedAreas = segAreas.OrderByDescending(a => a.Area).ToList();
            var res = sortedAreas.Take(2);
            foreach (var segArea in res)
            {
                var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(segArea);
                if (buildLines.Count == 0)
                {
                    return false;//没有建筑物要他作甚
                }
                var boundPt = segLine.GetBoundPt(buildLines, segArea);
                if (segLine.GetValueType(boundPt))
                {
                    maxVal = segLine.GetMinDist(boundPt) - 2760;
                }
                else
                {
                    minVal = -segLine.GetMinDist(boundPt) + 2760;
                }
            }
            areas.RemoveAt(0);
            areas.AddRange(res);
            return true;
        }
        public static bool IsCorrectSegLines2(Line segLine, ref List<Polyline> areas, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex,
  out double maxVal, out double minVal)
        {
            maxVal = 0;
            minVal = 0;
            var area = areas.First();//其实进来也就一个区域

            var segAreas = segLine.SplitByLine(area);
            if (segAreas.Count < 2)
            {
                return false;
            }

            var sortedAreas = segAreas.OrderByDescending(a => a.Area).ToList();
            var res = sortedAreas.Take(2);
            foreach (var segArea in res)
            {
                var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(segArea);
                if (buildLines.Count == 0)
                {
                    return false;//没有建筑物要他作甚
                }
                var boundPt = segLine.GetBoundPt(buildLines, segArea);
                if (segLine.GetValueType(boundPt))
                {
                    maxVal = segLine.GetMinDist(boundPt) - 2760;
                }
                else
                {
                    minVal = -segLine.GetMinDist(boundPt) + 2760;
                }
            }
            areas.RemoveAt(0);
            areas.AddRange(res);
            return true;
        }
    }
    public class Dfs
    {
        public static bool dfsSplit(ref HashSet<int> usedLines, ref List<Polyline> areas, ref List<Line> sortSegLines, 
            ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, GaParameter gaParameter, ref List<double> maxVals, ref List<double> minVals
            , Stopwatch stopwatch, double thresholdSecond)
        {
            if (usedLines.Count == gaParameter.LineCount)//分割线使用完毕, 退出递归
            {
                return true;
            }
            if(stopwatch.Elapsed.TotalSeconds > thresholdSecond)
            {
                return false;
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
            return dfsSplit(ref usedLines, ref areas, ref sortSegLines, buildLinesSpatialIndex, gaParameter, ref maxVals, ref minVals,
                stopwatch, thresholdSecond);
        }

        public static bool dfsSplitWithoutSegline(Polyline wallLine, int throughBuildNums, ref List<Polyline> areas, ref List<Line> sortSegLines,
    ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, double buildNums, ref List<double> maxVals, ref List<double> minVals, Stopwatch stopwatch, double threshSecond)
        {
            if (areas.Count + throughBuildNums == buildNums)//楼层分割完毕, 退出递归
            {
                return true;
            }
            if (stopwatch.Elapsed.TotalSeconds > threshSecond)
            {
                return false;
            }
            foreach (var area in areas)
            {
                var orgAreas = new List<Polyline>() { area };
                var builds = buildLinesSpatialIndex.SelectCrossingPolygon(area);
                if (builds.Count > 1)//建筑物数目大于1
                {
                    var verticalSegline = AreaSplit.VerticalSeg(ref orgAreas, buildLinesSpatialIndex, out double maxVal, out double minVal);
                    if (!(verticalSegline is null))//分割线正好纵向分割区域
                    {
                        areas.Remove(area);
                        areas.AddRange(orgAreas);
                        sortSegLines.Add(verticalSegline);
                        maxVals.Add(maxVal);
                        minVals.Add(minVal);
                        break;
                    }
                    var horizontalSegline = AreaSplit.HorizontalSeg(ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal);
                    if (!(horizontalSegline is null))//分割线正好横向分割区域
                    {
                        areas.Remove(area);
                        areas.AddRange(orgAreas);
                        sortSegLines.Add(horizontalSegline);
                        maxVals.Add(maxVal);
                        minVals.Add(minVal);
                        break;
                    }
                    var rhroughSegline = AreaSplit.ThroughVerticalSeg(wallLine, ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal);
                    if (!(rhroughSegline is null))//分割线纵向贯穿分割区域
                    {
                        throughBuildNums++;
                        areas.Remove(area);
                        areas.AddRange(orgAreas);
                        sortSegLines.Add(rhroughSegline);
                        maxVals.Add(maxVal);
                        minVals.Add(minVal);
                        break;
                    }
                }
            }

            //递归搜索
            return dfsSplitWithoutSegline(wallLine, throughBuildNums, ref areas, ref sortSegLines, buildLinesSpatialIndex, buildNums, ref maxVals, ref minVals, stopwatch, threshSecond);
        }


        public static bool dfsSplitWithoutSegline2(int num, string prob, Polyline wallLine, int throughBuildNums, ref List<Polyline> areas, ref List<Line> sortSegLines,
            ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, double buildNums, ref List<double> maxVals, ref List<double> minVals, Stopwatch stopwatch, double threshSecond)
        {
            if (areas.Count + throughBuildNums == buildNums)//楼层分割完毕
            {
                return true;
            }
            //if (stopwatch.Elapsed.TotalSeconds > threshSecond)
            //{
            //    return false;
            //}
            var dir = prob[num];//当前分割线的方向
            var flag = false;
            if (dir == '1')
            {
                foreach (var area in areas)
                {
                    var orgAreas = new List<Polyline>() { area };
                    var builds = buildLinesSpatialIndex.SelectCrossingPolygon(area);
                    if (builds.Count > 1)//建筑物数目大于1
                    {
                        var verticalSegline = AreaSplit.VerticalSeg(ref orgAreas, buildLinesSpatialIndex, out double maxVal, out double minVal);
                        if (!(verticalSegline is null))//分割线正好纵向分割区域
                        {
                            areas.Remove(area);
                            areas.AddRange(orgAreas);
                            sortSegLines.Add(verticalSegline);
                            maxVals.Add(maxVal);
                            minVals.Add(minVal);
                            flag = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var area in areas)
                {
                    var orgAreas = new List<Polyline>() { area };
                    var builds = buildLinesSpatialIndex.SelectCrossingPolygon(area);
                    if (builds.Count > 1)//建筑物数目大于1
                    {
                        var horizontalSegline = AreaSplit.HorizontalSeg(ref orgAreas, buildLinesSpatialIndex, out double maxVal, out double minVal);
                        if (!(horizontalSegline is null))//分割线正好横向分割区域
                        {
                            areas.Remove(area);
                            areas.AddRange(orgAreas);
                            sortSegLines.Add(horizontalSegline);
                            maxVals.Add(maxVal);
                            minVals.Add(minVal);
                            flag = true;
                            break;
                        }
                    }
                }

            }
            if (!flag)
            {
                return false;
            }
            num++;
            //递归搜索
            return dfsSplitWithoutSegline2(num, prob, wallLine, throughBuildNums, ref areas, ref sortSegLines, buildLinesSpatialIndex, buildNums, ref maxVals, ref minVals, stopwatch, threshSecond);
        }
    }
}


