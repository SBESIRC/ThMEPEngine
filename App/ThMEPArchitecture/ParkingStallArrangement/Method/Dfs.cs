using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using Linq2Acad;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public class Dfs
    {
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

        /// <summary>
        /// 生成随机的分割线，自动
        /// </summary>
        /// <param name="orgArea"></param>
        /// <param name="area"></param>
        /// <param name="seglineCnt"></param>
        /// <param name="visited"></param>
        /// <param name="buildingSpatialIndex"></param>
        /// <param name="rstSegLines"></param>
        /// <param name="successedSeg"></param>
        /// <param name="seglineDir"></param>
        public static void GetAreaRandSeglinesByDfs(Polyline orgArea, Polyline area, int seglineCnt, List<SegLineEx> visited,
            ThCADCoreNTSSpatialIndex buildingSpatialIndex, ref List<SegLineEx> rstSegLines, ref bool successedSeg, int seglineDir = 0)
        {
            if (visited.Count == seglineCnt)
            {
                var segLinesGroup = SeglinesGrouping(visited, orgArea);
                if (segLinesGroup.Count > 1)
                {
                    AddConnectLine(orgArea, visited, buildingSpatialIndex);
                }
                var tmp = new List<SegLineEx>();
                visited.ForEach(seg => tmp.Add(seg.Clone()));
                rstSegLines.AddRange(tmp);

                successedSeg = true;
                return;
            }
            var spliters = GetAreaAllSeglines(area, buildingSpatialIndex);
            if (spliters is null) return;

            if (spliters.Count == 0)
            {
                return;
            }

            var rstGetRandSeg = GetRandSeg(spliters, out AutoSegLines spliter, seglineDir);
            if(!rstGetRandSeg)
            {
                return;
            }
            var randomSegline = spliter.GetRandomLine();

            var subAreas = randomSegline.Segline.SplitByLine(area); //split area
            
            visited.Add(randomSegline.Clone());
            foreach (var subArea in subAreas)
            {
                GetAreaRandSeglinesByDfs(orgArea, subArea, seglineCnt, visited, buildingSpatialIndex, ref rstSegLines, ref successedSeg);
                if (successedSeg) return;
            }
        }

        private static bool GetRandSeg(List<AutoSegLines> spliters, out AutoSegLines spliter, int seglineDir = 0)
        {
            spliter = null;
            if (seglineDir == 1)//竖直优先
            {
                int index = 0;
                while(index < spliters.Count)
                {
                    spliter = spliters[index];//选中的分割线
                    if (spliter.MaxValues > spliter.MinValues)
                    {
                        return true;
                    }
                    index++;
                }
                return false;
            }
            else if(seglineDir == -1)//水平优先
            {
                int index = spliters.Count - 1;
                while (index >= 0)
                {
                    spliter = spliters[index];//选中的分割线
                    if (spliter.MaxValues > spliter.MinValues)
                    {
                        return true;
                    }
                    index--;
                }
                return false;
            }
            else
            {
                var randLs = General.Utils.RandChoice(spliters.Count);
                int index = 0;
                while (index < spliters.Count)
                {
                    var selectSegNum = randLs[index];
                    spliter = spliters[selectSegNum];//选中的分割线
                    if (spliter.MaxValues > spliter.MinValues)
                    {
                        return true;
                    }
                    index++;
                }
                return false;
            }
        }

        public static void AddConnectLine(Polyline area, List<SegLineEx> visited,
            ThCADCoreNTSSpatialIndex buildingSpatialIndex)
        {
            var rstSeglinesList = SeglinesGrouping(visited, area);//分组
            var rstConnectLines = GetNearestLines(rstSeglinesList);//找到候选连接线
            foreach (var rst in rstConnectLines)
            {
                var line1 = rst[0];
                var line2 = rst[1];
                double sval = 0;
                double eval = 0;
                double constantVal = 0;
                if (line1.Direction)//竖直
                {
                    var val1 = line1.Segline.StartPoint.Y;
                    var val2 = line1.Segline.EndPoint.Y;
                    constantVal = line1.Segline.EndPoint.X;
                    sval = Math.Min(val1, val2);
                    eval = Math.Max(eval, val1);

                }
                else
                {
                    var val1 = line1.Segline.StartPoint.X;
                    var val2 = line1.Segline.EndPoint.X;
                    constantVal = line1.Segline.EndPoint.Y;
                    sval = Math.Min(val1, val2);
                    eval = Math.Max(val1, val2);
                }
                var rstLine = GetConnectLine(constantVal, sval, eval, line2, area, buildingSpatialIndex);
                visited.Add(rstLine);
            }
        }

        private static SegLineEx GetConnectLine(double constantVal, double sval, double eval, SegLineEx line2, Polyline area,
            ThCADCoreNTSSpatialIndex buildingSpatialIndex)
        {
            var areals = new List<Polyline>() { area };
            var areaSpatialIndex = new ThCADCoreNTSSpatialIndex(areals.ToCollection());
            double curVal = sval + 5500;
            Line curLine = new Line();
            while (curVal < eval)
            {
                curLine = CreateTempLine(constantVal, curVal, line2);
                var spt = curLine.StartPoint;
                var ept = curLine.EndPoint;
                var ptInArea = area.Contains(spt) && area.Contains(ept);
                var linebuffer = curLine.Buffer(1.0);
                var areaCnt = areaSpatialIndex.SelectCrossingPolygon(linebuffer).Count;
                var buildCnt = buildingSpatialIndex.SelectCrossingPolygon(linebuffer).Count;
                if (areaCnt > 0 || buildCnt > 0 || !ptInArea)
                {
                    curVal += 5500;
                    continue;
                }
                break;
            }
            var width = WindmillSplit.GetMaxWidth(area);
            curLine.GetMaxMinVal(area, buildingSpatialIndex, null, width, out double maxVal2, out double minVal2);
            return new SegLineEx(curLine, maxVal2, minVal2);
        }

        private static Line CreateTempLine(double constantVal, double val, SegLineEx line2)
        {
            Line line = new Line();
            if (line2.Direction)//竖直
            {
                var spt = new Point3d(constantVal, val, 0);
                var ept = new Point3d(line2.Segline.StartPoint.X, val, 0);
                line = new Line(spt, ept);
            }
            else
            {
                var spt = new Point3d(val, constantVal, 0);
                var ept = new Point3d(val, line2.Segline.StartPoint.Y, 0);
                line = new Line(spt, ept);
            }
            return line;
        }

        /// <summary>
        /// 找到最邻近的两根线
        /// </summary>
        /// <param name="rstSeglinesList"></param>
        /// <returns></returns>
        private static List<List<SegLineEx>> GetNearestLines(List<List<SegLineEx>> rstSeglinesList)
        {
            var rstLines = new List<List<SegLineEx>>();
            var distLineDic = new Dictionary<double, SegLineEx>();
            if (rstSeglinesList.Count == 2)
            {
                if (rstSeglinesList[0].Count == 1)
                {
                    var targetSeg = rstSeglinesList[0][0];
                    foreach (var seg in rstSeglinesList[1])
                    {
                        if (seg.Direction == targetSeg.Direction)
                        {
                            var dist = GetLinesDist(seg.Segline, targetSeg.Segline);
                            if (!distLineDic.ContainsKey(dist))
                            {
                                distLineDic.Add(dist, seg);
                            }
                        }
                    }
                    var rst = new List<SegLineEx>();

                    rst.Add(targetSeg);
                    rst.Add(distLineDic.OrderBy(d => d.Key).First().Value);
                    rstLines.Add(rst);
                }

                else if (rstSeglinesList[1].Count == 1)
                {
                    var targetSeg = rstSeglinesList[1][0];
                    foreach (var seg in rstSeglinesList[0])
                    {
                        if (seg.Direction == targetSeg.Direction)
                        {
                            var dist = GetLinesDist(seg.Segline, targetSeg.Segline);
                            if (!distLineDic.ContainsKey(dist))
                            {
                                distLineDic.Add(dist, seg);
                            }
                        }
                    }
                    var rst = new List<SegLineEx>();

                    rst.Add(targetSeg);
                    rst.Add(distLineDic.OrderBy(d => d.Key).First().Value);
                    rstLines.Add(rst);
                }
            }
            if (rstSeglinesList.Count > 2)
            {
                var dir = rstSeglinesList[0][0].Direction;
                if (dir)
                {
                    rstSeglinesList = rstSeglinesList.OrderBy(rst => rst[0].Segline.StartPoint.X).ToList();
                }
                else
                {
                    rstSeglinesList = rstSeglinesList.OrderBy(rst => rst[0].Segline.StartPoint.Y).ToList();
                }
                for (int i = 0; i < rstSeglinesList.Count - 1; i++)
                {
                    var rst = new List<SegLineEx>();
                    rst.Add(rstSeglinesList[i][0]);
                    rst.Add(rstSeglinesList[i + 1][0]);
                    rstLines.Add(rst);
                }
            }
            return rstLines;
        }

        private static double GetLinesDist(Line line1, Line line2)
        {
            var spt1 = line1.StartPoint;
            var nearestPt = line2.GetClosestPointTo(spt1, true);
            return nearestPt.DistanceTo(spt1);
        }

        /// <summary>
        /// 根据分割线的连接关系进行分组
        /// </summary>
        /// <param name="segLines"></param>
        /// <returns></returns>
        public static List<List<SegLineEx>> SeglinesGrouping(List<SegLineEx> segLines, Polyline area)
        {
            var rstSeglinesList = new List<List<SegLineEx>>();

            var segLinesCnt = segLines.Count;

            var seglineDic = new Dictionary<int, List<int>>();
            for (int i = 0; i < segLinesCnt - 1; i++)
            {
                for (int j = i + 1; j < segLinesCnt; j++)
                {
                    var seglinei = segLines[i].Segline;
                    var seglinej = segLines[j].Segline;
                    if (seglinei.IsIntersect(seglinej, area))
                    {
                        AddItem(seglineDic, i, j);
                    }
                }
                if (!seglineDic.ContainsKey(i))
                {
                    seglineDic.Add(i, new List<int>());
                }
            }
            if (!seglineDic.ContainsKey(segLinesCnt - 1))
            {
                seglineDic.Add(segLinesCnt - 1, new List<int>());
            }
            var usedLines = new List<int>();

            foreach (var si in seglineDic.Keys)
            {
                if (usedLines.Contains(si)) continue;
                var rstLineGroup = new List<int>();
                GetLineGroup(si, seglineDic, ref rstLineGroup, ref usedLines);
                if (rstLineGroup.Count > 0)
                {
                    var rstSegs = new List<SegLineEx>();
                    rstLineGroup.ForEach(i => rstSegs.Add(segLines[i]));
                    rstSeglinesList.Add(rstSegs);
                }
            }
            return rstSeglinesList;
        }

        private static void GetLineGroup(int startNum, Dictionary<int, List<int>> seglineDic, ref List<int> rstLineGroup, ref List<int> usedLines)
        {
            rstLineGroup.Add(startNum);
            usedLines.Add(startNum);
            var cur = startNum;

            var neighbors = seglineDic[cur];
            if (neighbors.Count == 0) return;
            foreach (var p in neighbors)
            {
                if (!rstLineGroup.Contains(p))
                {
                    GetLineGroup(p, seglineDic, ref rstLineGroup, ref usedLines);
                }
            }
        }

        private static void AddItem(Dictionary<int, List<int>> seglineDic, int i, int j)
        {
            if (seglineDic.ContainsKey(i))
            {
                seglineDic[i].Add(j);
            }
            else
            {
                seglineDic.Add(i, new List<int>() { j });
            }

            if (seglineDic.ContainsKey(j))
            {
                seglineDic[j].Add(i);
            }
            else
            {
                seglineDic.Add(j, new List<int>() { i });
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
            var seglineCnt = outerBrder.Buildings.Count - 1;//二分法，分割线数目是障碍物数目减一
            var buildingSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.Buildings.ToCollection());//建筑物索引

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
        /// 生成一种随机的分割线方案, 只用于自动生成分割线
        /// </summary>
        /// <param name="outerBrder"></param>
        /// <returns></returns>
        public static List<SegLineEx> GetRandomSeglines(OuterBrder outerBrder, int seglineDir = 0)
        {
            var seglineCnt = outerBrder.Buildings.Count - 1;//二分法，分割线数目是障碍物数目减一
            var buildingSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.Buildings.ToCollection());//建筑物索引
            var rstSegLines = new List<SegLineEx>();
            var successedSeg = false;
            var maxSegCnt = 20;
            var cnt = 0;
            while (!successedSeg && cnt++ < maxSegCnt)
            {
                var area = outerBrder.WallLine;
                var orgArea = outerBrder.WallLine;
                var segs = new List<SegLineEx>();
                rstSegLines = new List<SegLineEx>();
                GetAreaRandSeglinesByDfs(orgArea, area, seglineCnt, segs, buildingSpatialIndex, ref rstSegLines, ref successedSeg, seglineDir);
            }

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

        /// <summary>
        /// 获取当前区域的全部竖向分割线方案
        /// </summary>
        /// <param name="当前区域"></param>
        /// <param name="建筑物索引"></param>
        /// <param name="分割线列表"></param>
        private static void GetAllVerticalSeg(Polyline area, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, List<AutoSegLines> autoSegLinesList)
        {
            double dist = 1.0;
            double laneWidth = 5500;
            var orderPtsByX = area.GetPoints().OrderBy(pt => pt.X);
            var orderPtsByY = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPtsByX.First().X;
            var right = orderPtsByX.Last().X;
            var buttom = orderPtsByY.First().Y - dist;
            var upper = orderPtsByY.Last().Y + dist;

            var startX = left;
            var throughBuilding = false;//判断分割线是否穿过了障碍物
            while (startX < right)//直线在框内
            {
                var upPt = new Point3d(startX, upper - dist, 0);
                var buttomPt = new Point3d(startX, buttom + dist, 0);
                var segLine = new Line(upPt, buttomPt);
                var segRect = segLine.Buffer(1.0);
                var segRectInBuild = buildLinesSpatialIndex.SelectCrossingPolygon(segRect);
                if (segRectInBuild.Count > 0)
                {
                    throughBuilding = true;
                    startX += laneWidth;
                    continue;//分割线穿墙直接删除
                }
                var segFlag = AreaSplit.IsCorrectSegLines(segLine, area, buildLinesSpatialIndex, out double maxVal, out double minVal);
                if (throughBuilding && segFlag)//分割方案存在
                {
                    throughBuilding = false;
                    var autoSegLines = new AutoSegLines(segLine, maxVal, minVal);

                    autoSegLinesList.Add(autoSegLines);
                }
                startX += laneWidth;
            }
        }

        /// <summary>
        /// 获取当前区域的全部横向分割线方案
        /// </summary>
        /// <param name="当前区域"></param>
        /// <param name="建筑物索引"></param>
        /// <param name="分割线列表"></param>
        public static void GetAllHorizontalSeg(Polyline area, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, List<AutoSegLines> autoSegLinesList)
        {
            double dist = 1.0;
            double laneWidth = 5500;
            var orderPtsByX = area.GetPoints().OrderBy(pt => pt.X);
            var orderPtsByY = area.GetPoints().OrderBy(pt => pt.Y);
            var left = orderPtsByX.First().X - dist;
            var right = orderPtsByX.Last().X + dist;
            var buttom = orderPtsByY.First().Y;
            var upper = orderPtsByY.Last().Y;

            var startX = buttom;
            var throughBuilding = false;//判断分割线是否穿过了障碍物
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
                var segFlag = AreaSplit.IsCorrectSegLines(segLine, area, buildLinesSpatialIndex, out double maxVal, out double minVal);
                if (throughBuilding && segFlag)//分割方案存在
                {
                    throughBuilding = false;
                    var autoSegLines = new AutoSegLines(segLine, maxVal, minVal);
                    autoSegLinesList.Add(autoSegLines);
                }
                startX += laneWidth;
            }
        }
    }
}
