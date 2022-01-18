using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using System.Diagnostics;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using Linq2Acad;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
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
            if (stopwatch.Elapsed.TotalSeconds > thresholdSecond)
            {
                return false;
            }
            for (int i = 0; i < gaParameter.LineCount; i++)
            {
                if (usedLines.Contains(i))
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
                    //var rhroughSegline = AreaSplit.ThroughVerticalSeg(wallLine, ref orgAreas, buildLinesSpatialIndex, out maxVal, out minVal);
                    //if (!(rhroughSegline is null))//分割线纵向贯穿分割区域
                    //{
                    //    throughBuildNums++;
                    //    areas.Remove(area);
                    //    areas.AddRange(orgAreas);
                    //    sortSegLines.Add(rhroughSegline);
                    //    maxVals.Add(maxVal);
                    //    minVals.Add(minVal);
                    //    break;
                    //}
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

        /// <summary>
        /// 二分法找出全部的分割方案
        /// </summary>
        /// <param name="area"></param>
        /// <param name="visited"></param>
        /// <param name="buildingSpatialIndex"></param>
        /// <param name="rstSegLines"></param>
        public static void GetAreaAllSeglinesByDfs(Polyline area, int seglineCnt, List<SegLineEx> visited, ThCADCoreNTSSpatialIndex buildingSpatialIndex,
            ref List<List<SegLineEx>> rstSegLines)
        {
            if (visited.Count == seglineCnt)
            {
                foreach (var rst in rstSegLines)
                {
                    if (LineExEquals(rst, visited)) return;
                }
                var tmp = new List<SegLineEx>();
                visited.ForEach(seg => tmp.Add(seg.Clone()));
                visited.Clear();
                rstSegLines.Add(tmp);
                return;
            }
            var spliters = GetAreaAllSeglines(area, buildingSpatialIndex);
            if (spliters is null) return;

            
            foreach (var spliter in spliters)
            {
                var tmpVisited = new List<SegLineEx>();
                visited.ForEach(l => tmpVisited.Add(l.Clone()));
                var segline = spliter.Seglines;
                var subAreas = segline.SplitByLine(area); //split area
                visited.Add(new SegLineEx(segline, spliter.MaxValues, spliter.MinValues));
                //foreach (var subArea in subAreas)
                //{
                //    GetAreaAllSeglinesByDfs(subArea, seglineCnt, visited, buildingSpatialIndex, ref rstSegLines);
                //}

                GetAreaAllSeglinesByDfs(subAreas.First(), seglineCnt, visited, buildingSpatialIndex, ref rstSegLines);
                GetAreaAllSeglinesByDfs(subAreas.Last(), seglineCnt, visited, buildingSpatialIndex, ref rstSegLines);

               // visited.RemoveAt(visited.Count - 1);
            }
        }

        public static void GetAreaRandSeglinesByDfs(Polyline area, int seglineCnt, List<SegLineEx> visited, 
            ThCADCoreNTSSpatialIndex buildingSpatialIndex, ref List<SegLineEx> rstSegLines, ref bool successedSeg)
        {
            if (visited.Count == seglineCnt)
            {
                var tmp = new List<SegLineEx>();
                visited.ForEach(seg => tmp.Add(seg.Clone()));
                rstSegLines.AddRange(tmp);
//#if DEBUG
//                using (AcadDatabase acadDatabase = AcadDatabase.Active())
//                {
//                    foreach (var line in visited)
//                    {
//                        acadDatabase.CurrentSpace.Add(new Line(line.Segline.StartPoint, line.Segline.EndPoint));
//                    }
//                }
//#endif
                successedSeg = true;
                return;
            }
            var spliters = GetAreaAllSeglines(area, buildingSpatialIndex);
            if (spliters is null)  return;
            var selectSegNum = General.Utils.RandInt(spliters.Count-1);
            var spliter = spliters[selectSegNum];//选中的分割线
            var segline = spliter.Seglines;
            var subAreas = segline.SplitByLine(area); //split area
            visited.Add(new SegLineEx(segline, spliter.MaxValues, spliter.MinValues));
            foreach (var subArea in subAreas)
            {
                GetAreaRandSeglinesByDfs(subArea, seglineCnt, visited, buildingSpatialIndex, ref rstSegLines, ref successedSeg);
                if (successedSeg) return;
            }
        }

        /// <summary>
        /// 判断两组分割线是否相等
        /// </summary>
        /// <param name="lineEx1"></param>
        /// <param name="lineEx2"></param>
        /// <returns></returns>
        private static bool LineExEquals(List<SegLineEx> lineEx1, List<SegLineEx> lineEx2)
        {
            var count = lineEx1.Count;
            var orderLineEx1 = OrderedByCenterPt(lineEx1);
            var orderLineEx2 = OrderedByCenterPt(lineEx2);

            for (int i = 0; i < count; i++)
            {
                if (!orderLineEx1[i].Equals(orderLineEx2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 按照分割线的中心点先X后Y排序
        /// </summary>
        /// <param name="lineEx"></param>
        /// <returns></returns>
        private static List<SegLineEx> OrderedByCenterPt(List<SegLineEx> lineEx)
        {
            var orderRst = lineEx.OrderBy(lex => lex.Segline.GetCenterPt().X).ThenByDescending(lex => lex.Segline.GetCenterPt().Y).ToList();//从左向右，从上到下
            return orderRst;
        }
        public static List<List<SegLineEx>> GetDichotomySegline(OuterBrder outerBrder)
        {
            var seglinesList = new List<List<SegLineEx>>();
            var seglineCnt = outerBrder.Building.Count - 1;//二分法，分割线数目是障碍物数目减一
            var buildingSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.Building.ToCollection());//建筑物索引

            var area = outerBrder.WallLine;
            var segs = new List<SegLineEx>();
            var rstSegLines = new List<List<SegLineEx>>();
            GetAreaAllSeglinesByDfs(area, seglineCnt, segs, buildingSpatialIndex, ref rstSegLines);
            var index = 0;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var seglines in rstSegLines)
                {
                    string layerName = "分割线" + Convert.ToString(index++);

                    try
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerName, 30);
                        //DbHelper.EnsureLayerOn(layerName);
                    }
                    catch { }

                    foreach (var segline in seglines)
                    {
                        var seg = segline.Segline;
                        seg.Layer = layerName;
                        acadDatabase.CurrentSpace.Add(new Line(seg.StartPoint, seg.EndPoint));
                    }
                }
            }
            return seglinesList;
        }

        /// <summary>
        /// 生成一种随机的分割线方案
        /// </summary>
        /// <param name="outerBrder"></param>
        /// <returns></returns>
        public static List<SegLineEx> GetRandomSeglines(OuterBrder outerBrder)
        {
            var seglineCnt = outerBrder.Building.Count - 1;//二分法，分割线数目是障碍物数目减一
            var buildingSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.Building.ToCollection());//建筑物索引

            var area = outerBrder.WallLine;
            var segs = new List<SegLineEx>();
            var rstSegLines = new List<SegLineEx>();
            var successedSeg = false;
            GetAreaRandSeglinesByDfs(area, seglineCnt, segs, buildingSpatialIndex, ref rstSegLines,ref successedSeg);
            
            return rstSegLines;
        }

        private static List<AutoSegLines> GetAreaAllSeglines(Polyline area, ThCADCoreNTSSpatialIndex buildingSpatialIndex)
        {
            var autoSegLinesList = new List<AutoSegLines>();

            var buildRects = buildingSpatialIndex.SelectCrossingPolygon(area);//找出当前区域的全部建筑物
            if (buildRects.Count == 1)//建筑物数目为1，不用分割
            {
                return null;
            }
            GetAllVerticalSeg(area, buildingSpatialIndex, autoSegLinesList);
            GetAllHorizontalSeg(area, buildingSpatialIndex, autoSegLinesList);

            return autoSegLinesList;
        }


        private static void GetAllVerticalSeg(Polyline area, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, List<AutoSegLines> autoSegLinesList)
        {

            double dist = 5000;
            double laneWidth = 5500;
            var orderPts = area.GetPoints().OrderBy(pt => pt.X);
            var orderPts2 = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPts.First().X;
            var right = orderPts.Last().X;
            var buttom = orderPts2.First().Y - dist;
            var upper = orderPts2.Last().Y + dist;

            var startX = left;
            var throughBuilding = false;//判断分割线是否穿过了障碍物
            var buildNums = new List<int>();//分割区域内的建筑物数目，以第一个区域为准
            buildNums.Add(0);
            while (startX < right)//直线在框内
            {
                var upPt = new Point3d(startX, upper, 0);
                var buttomPt = new Point3d(startX, buttom, 0);
                var segLine = new Line(upPt, buttomPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if (segRectInBuild.Count > 0)
                {
                    throughBuilding = true;
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                var segFlag = AreaSplit.IsCorrectSegLines(segLine, area, buildLinesSpatialIndex, out double maxVal, out double minVal, out List<Polyline> segAreas);
                if (throughBuilding && segFlag)//分割方案存在
                {
                    throughBuilding = false;
                    var autoSegLines = new AutoSegLines(segLine, segAreas, maxVal, minVal);

                    autoSegLinesList.Add(autoSegLines);
                }
                startX += laneWidth;
            }
        }
        public static void GetAllHorizontalSeg(Polyline area, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, List<AutoSegLines> autoSegLinesList)
        {
            var autoSegLines = new AutoSegLines();
            double dist = 5000;
            double laneWidth = 5500;
            var orderPts = area.GetPoints().OrderBy(pt => pt.X);
            var orderPts2 = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPts.First().X - dist;
            var right = orderPts.Last().X + dist;
            var buttom = orderPts2.First().Y;
            var upper = orderPts2.Last().Y;

            var startX = buttom;
            var throughBuilding = false;//判断分割线是否穿过了障碍物
            var buildNums = new List<int>();//分割区域内的建筑物数目，以第一个区域为准
            buildNums.Add(0);
            while (startX < upper)//直线在框内
            {
                var leftPt = new Point3d(left, startX, 0);
                var rightPt = new Point3d(right, startX, 0);
                var segLine = new Line(leftPt, rightPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if (segRectInBuild.Count > 0)
                {
                    throughBuilding = true;
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                var segFlag = AreaSplit.IsCorrectSegLines(segLine, area, buildLinesSpatialIndex, out double maxVal, out double minVal, out List<Polyline> segAreas);
                if (throughBuilding && segFlag)//分割方案存在
                {
                    throughBuilding = false;
                    autoSegLines.SegAreas.AddRange(segAreas);
                    autoSegLines.Seglines = new Line(leftPt, rightPt);
                    autoSegLines.MaxValues = maxVal;
                    autoSegLines.MinValues = minVal;
                    autoSegLinesList.Add(autoSegLines);
                }
                startX += laneWidth;
            }
        }
    }
}
