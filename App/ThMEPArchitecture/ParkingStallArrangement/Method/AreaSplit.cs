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
using NetTopologySuite.Geometries;
using Linq2Acad;
using Dreambuild.AutoCAD;
using AcHelper;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class AreaSplit
    {
        public static List<Polyline> SplitByLine(this Line line, Polyline polygon, double tor = 5.0)
        {
            var defaultTesselateLength = 100;
            var lines = polygon.ToLines(defaultTesselateLength);
            double extendTor = polygon.GetMaxWidth();
            line = line.ExtendLineEx(extendTor, 3);
            lines.Add(line);
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
            }

            return rst;
        }

        public static Polyline SplitByRamp(this Polyline polygon,  DBObjectCollection rstRamps, double tor = 5.0)
        {
            var defaultTesselateLength = 100;
            var lines = polygon.ToLines(defaultTesselateLength);

            foreach(var ramp in rstRamps)
            {
                var objs = new DBObjectCollection();
                (ramp as BlockReference).Explode(objs);
                var tmpLines = (objs[0] as Polyline).ToLines(defaultTesselateLength);
                lines.AddRange(tmpLines);
            }
            var extendLines = lines.Select(l => l.ExtendLine(tor)).ToCollection();
            var areas = extendLines.PolygonsEx();


            var rst = new List<Polyline>();
            foreach (DBObject area in areas)
            {
                rst.Add(area as Polyline);
            }
            rst = rst.OrderByDescending(a => a.Area).ToList();
            return rst.First();
        }


        public static List<Polyline> SplitArea(this List<Line> splitterLines, List<Polyline> polyLines, double tor = 5.0)
        {
            var rst = new List<Polyline>();
            var allLines = new List<Line>();

            var defaultTesselateLength = 100;
            var boundlines = polyLines.SelectMany(pl => pl.ToLines(defaultTesselateLength));
            allLines.AddRange(splitterLines);
            allLines.AddRange(boundlines);
            var extendLineStrings = allLines.Select(l =>
            {
                var el = l.ExtendLine(tor);
                var linestring = el.ToNTSLineString();
                el.Dispose();
                return linestring;
            });

            var multiLineStrings = new MultiLineString(extendLineStrings.ToArray());

            foreach (var l in boundlines)
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
            var defaultTesselateLength = 100;
            var lines = polygon.ToLines(defaultTesselateLength);
            double extendTor = polygon.GetMaxWidth();
            var lineEx = line.ExtendLine(extendTor);
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
                if (area.Contains(middlePt))
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
                                continue;
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

        public static Point3d GetBoundPt(this Line line, DBObjectCollection buildLines, ThCADCoreNTSSpatialIndex buildingWithoutRampSpatialIndex, Polyline segArea, Polyline area, ThCADCoreNTSSpatialIndex areaPtsIndex, out bool hasBuilding)
        {
            hasBuilding = true;
            if (buildLines.Count == 0)//区域内没有建筑物
            {
                hasBuilding = false;
                if (areaPtsIndex is null)//之前二分逻辑的距离
                {
                    var pts = segArea.GetPoints().ToList();
                    return pts.OrderBy(e => line.GetMinDist(e)).Last();//返回最远距离
                }
                else
                {
                    try
                    {
                        var objs = areaPtsIndex.SelectCrossingPolygon(segArea);//找到切割区域内的全部点
                        var pts = new List<Point3d>();
                        foreach (var obj in objs)
                        {
                            var dbPt = obj as DBPoint;
                            var pt = dbPt.Position;
                            pts.Add(pt);
                        }
                        if (pts.Count == 0)
                        {
                            var Ls = segArea.ToLines();
                            foreach (Line l in Ls)
                            {
                                pts.AddRange(l.Intersect(area, Intersect.OnBothOperands));
                            }

                        }
                        return pts.OrderBy(e => line.GetMinDist(e)).Last();//返回最远距离
                    }
                    catch (Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
            }
            var closedPts = new List<Point3d>();
            if (line.GetDirection() == -1)//水平线才需要考虑穿障碍物
            {
                var buildList = new List<BlockReference>();
                foreach (var build in buildLines)
                {
                    buildList.Add(build as BlockReference);
                }
                if (buildList.Count == 0)
                {
                    var pts = segArea.Intersect(area, 0);
                    var sortPts = pts.OrderBy(p => line.GetClosestPointTo(p, false).DistanceTo(p)).ToList();
                    return sortPts.First();
                }
                //var closeBuild = buildList.OrderBy(blk => line.GetMinDist(blk.GetRect().GetCenter())).ToList().First();
                var closeBuild = buildList.OrderBy(blk => line.dist2Build(blk)).ToList().First();
                var rect = closeBuild.GetRect();
                var plines = GetPlines(closeBuild);
                plines.ForEach(pl => pl.GetPoints().ForEach(p => closedPts.Add(p)));
                closedPts = closedPts.OrderBy(p => p.DistanceTo(new Point3d(p.X, line.StartPoint.Y, 0))).ToList();
                if(buildingWithoutRampSpatialIndex.SelectCrossingPolygon(rect).Count == 0)//该建筑物是坡道
                {
                    if(closedPts.Count > 0)
                    {
                        return closedPts.First();
                    }
                }
                foreach (var pt in closedPts)
                {
                    var tempLine = new Line(new Point3d(line.StartPoint.X, pt.Y, 0), new Point3d(line.EndPoint.X, pt.Y, 0));
                    var intersectRsts = tempLine.IsIntersectPt(pt, plines, area);
                    if (!intersectRsts)
                    {
                        return pt;
                    }
                }
                if (closedPts.Count == 0)//没有障碍物返回当前线与墙线的交点
                {
                    var pts = segArea.Intersect(area, 0);
                    var sortPts = pts.OrderBy(p => line.GetClosestPointTo(p, false).DistanceTo(p)).ToList();
                    return sortPts.First();
                }
                return closedPts.First();
            }
            else//竖直线
            {
                foreach (var build in buildLines)
                {
                    try
                    {
                        var br = build as BlockReference;
                        var pline = br.GetRect();
                        var pts = pline.GetPoints().ToList();
                        closedPts.Add(pts.OrderBy(e => line.GetMinDist(e)).First());

                    }
                    catch (Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
            }

            if (closedPts.Count == 0)
            {
                var pts = segArea.Intersect(area, 0);
                var sortPts = pts.OrderBy(p => line.GetClosestPointTo(p, false).DistanceTo(p)).ToList();
                return sortPts.First();
            }
            return closedPts.OrderBy(e => line.GetMinDist(e)).First();//返回最近距离
        }

        private static double dist2Build(this Line line, BlockReference build)
        {
            var objs = new DBObjectCollection();
            build.Explode(objs);
            double minDist = line.GetMinDist(build.GetRect().GetCenter());
            foreach (var obj in objs)
            {
                if (obj is Polyline pline)
                {
                    var pts = pline.GetPoints();
                    foreach (var pt in pts)
                    {
                        var dist = line.GetMinDist(pt);
                        if (dist < minDist)
                        {
                            minDist = dist;
                        }
                    }
                }
            }
            return minDist;
        }

        public static Point3d GetBoundPt(this Line line, DBObjectCollection buildLines, Polyline segArea, ThCADCoreNTSSpatialIndex areaPtsIndex, out bool hasBuilding)
        {
            hasBuilding = true;
            if (buildLines.Count == 0)//区域内没有建筑物
            {
                hasBuilding = false;
                if (areaPtsIndex is null)//之前二分逻辑的距离
                {
                    var pts = segArea.GetPoints().ToList();
                    return pts.OrderBy(e => line.GetMinDist(e)).Last();//返回最远距离
                }
                else
                {
                    var objs = areaPtsIndex.SelectCrossingPolygon(segArea);//找到切割区域内的全部点
                    var pts = new List<Point3d>();
                    foreach (var obj in objs)
                    {
                        var dbPt = obj as DBPoint;
                        var pt = dbPt.Position;
                        pts.Add(pt);
                    }

                    return pts.OrderBy(e => line.GetMinDist(e)).Last();//返回最远距离
                }

            }
            var closedPts = new List<Point3d>();

            foreach (var build in buildLines)
            {
                var br = build as BlockReference;
                var pline = br.GetRect();
                var pts = pline.GetPoints().ToList();
                closedPts.Add(pts.OrderBy(e => line.GetMinDist(e)).First());
            }

            return closedPts.OrderBy(e => line.GetMinDist(e)).First();//返回最近距离
        }

        private static bool IsIntersectPt(this Line line, Point3d targetPt, List<Polyline> plines, Polyline area)
        {
            double tor = 5500;
            var pts = new List<Point3d>();
            foreach (var pline in plines)
            {
                pts.AddRange(line.Intersect(pline, 0));
            }
            var flag = false;
            foreach (var pt in pts)
            {
                if (pt.DistanceTo(targetPt) < 1.0)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                pts.Add(targetPt);
            }
            if (pts.Count > 2)
            {
                return false;
            }
            var lineIntersectWithAreaPts = line.Intersect(area, Intersect.ExtendThis);
            foreach (var pt in pts)
            {
                foreach (var pt2 in lineIntersectWithAreaPts)
                {
                    if (pt.DistanceTo(pt2) < tor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static List<Polyline> GetPlines(BlockReference bkr)
        {
            var plines = new List<Polyline>();
            var objs = new DBObjectCollection();
            bkr.Explode(objs);

            //objs.Cast<Entity>().Where(e => e is Polyline && (e as Polyline).Closed).ForEach(e => plines.Add(e as Polyline));
            objs.Cast<Entity>().Where(e => e is Polyline).ForEach(e => plines.Add(e as Polyline));
            return plines;
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

        public static double GetMinDist(this Line line, Point3d pt)
        {
            var targetPt = line.GetClosestPointTo(pt, false);
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
            while (startX < right)//直线在框内
            {
                var upPt = new Point3d(startX, upper, 0);
                var buttomPt = new Point3d(startX, buttom, 0);
                var segLine = new Line(upPt, buttomPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if (segRectInBuild.Count > 0)
                {
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
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
                var boundPt = segLine.GetBoundPt(buildLines, segArea, null, out bool flag);
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

        public static bool IsCorrectSegLines(Line segLine, Polyline area, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex,
          out double maxVal, out double minVal)
        {
            maxVal = 0;
            minVal = 0;

            var segAreas = segLine.SplitByLine(area);
            if (segAreas.Count != 2)//不是两个区域直接退出
            {
                return false;
            }

            var buildingNums = new List<int>();//分割区域内的建筑物数目
            var sortedAreas = segAreas.OrderByDescending(a => a.Area).ToList();
            var res = sortedAreas.Take(2);
            foreach (var segArea in res)
            {
                var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(segArea);
                var buildCnt = buildLines.Count;
                if (buildCnt == 0)
                {
                    return false;//没有建筑物直接退出
                }
                buildingNums.Add(buildCnt);
                var boundPt = segLine.GetBoundPt(buildLines, segArea, null, out bool flag);
                if (segLine.GetValueType(boundPt))
                {
                    maxVal = segLine.GetMinDist(boundPt) - 2760;
                }
                else
                {
                    minVal = -segLine.GetMinDist(boundPt) + 2760;
                }
            }
            if(maxVal < minVal)
            {
                return false;
            }
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
                var boundPt = segLine.GetBoundPt(buildLines, segArea, null, out bool flag);
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
}


