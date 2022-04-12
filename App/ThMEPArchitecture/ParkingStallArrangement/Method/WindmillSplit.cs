using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using DotNetARX;
using System;
using AcHelper;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPEngineCore;
using Linq2Acad;
using ThMEPArchitecture.ViewModel;
namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class WindmillSplit
    {
        public static Dictionary<int,List<int>> GetSegLineIndexDic(Dictionary<int, Line> seglineDic)
        {
            var seglineTndexDic = new Dictionary<int, List<int>>();

            for(int i =0; i < seglineDic.Count; i++)
            {
                for(int j = 0; j < seglineDic.Count; j++)
                {
                    if (i == j) continue;
                    if(seglineDic[i].HasIntersection(seglineDic[j]))
                    {
                        seglineTndexDic.DicAdd(i, j);
                    }
                }
            }
            return seglineTndexDic;
        }

        public static List<bool> SegLineCut(List<Line> lines, Polyline area, out List<Line> cutLines)
        {
            var cutRsts = new List<bool>();
            cutLines = new List<Line>();
            for(int i =0; i < lines.Count; i++)
            {
                var pts = new List<Point3d>();
                var line = lines[i];
                var spt = line.StartPoint;
                pts.AddRange(line.Intersect(area, 0));//求与边界的交点
                cutRsts.Add(pts.Count > 0);//有交点为true
                for (int j = 0; j < lines.Count;j++)
                {
                    if (i == j) continue;
                    pts.AddRange(line.Intersect(lines[j], 0));//求与其他分割线的交点
                }
                var orderPts = pts.OrderBy(p => p.DistanceTo(line.StartPoint));
                cutLines.Add(new Line(orderPts.First(), orderPts.Last()));
            }

            return cutRsts;
        }

        private static void DicAdd(this Dictionary<int, List<int>> seglineTndexDic, int index,int target)
        {
            if(seglineTndexDic.ContainsKey(index))
            {
                seglineTndexDic[index].Add(target);
            }
            else
            {
                seglineTndexDic.Add(index, new List<int>() { target });
            }
        }

        public static List<Line> GetExtendSegline(Dictionary<int, Line> seglineDic, Dictionary<int, List<int>> seglineIndexDic)
        {
            var segLines = new List<Line>();
            foreach (var i in seglineIndexDic.Keys)
            {
                foreach (var j in seglineIndexDic[i])
                {
                    if (seglineDic[i].HasIntersection(seglineDic[j]))//邻接表中连接的线不需要扩展
                    {
                        continue;
                    }
                    //两条线没有交上，进行延展
                    var linei = seglineDic[i];
                    var linej = seglineDic[j];
                    ExtendLines(ref linei, ref linej);
                    seglineDic[i] = linei;
                    seglineDic[j] = linej;
                }
            }
            foreach(var line in seglineDic.Values)
            {
                segLines.Add(line);
            }
            return segLines;
        }
        public static List<Line> GetExtendSegline(List< Line> seglineList, Dictionary<int, List<int>> seglineIndexDic)
        {
            var segLines = new List<Line>();
            seglineList.ForEach(line => segLines.Add(line.Clone() as Line));
            for (int i = 0; i < seglineList.Count; i++)
            {
                foreach (var j in seglineIndexDic[i])
                {
                    if (segLines[i].HasIntersection(segLines[j]))//邻接表中连接的线不需要扩展
                    {
                        continue;
                    }
                    //两条线没有交上，进行延展
                    var linei = segLines[i];
                    var linej = segLines[j];
                    ExtendLines(ref linei, ref linej);
                    segLines[i] = linei;
                    segLines[j] = linej;
                }
            }
            return segLines;
        }
        public static void ExtendLines(ref Line linei, ref Line linej)
        {
            var intersectPt = linei.Intersect(linej, (Intersect)3).First();//两根线都延展求交点
            linei = ExtendLineToPt(linei, intersectPt);
            linej = ExtendLineToPt(linej, intersectPt);
        }

        public static Line ExtendLineToPt(Line line, Point3d pt)
        {
            double tor = 1.0;
            var closedPt = line.GetClosestPointTo(pt, false);
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            if(closedPt.DistanceTo(pt) > tor)//需要延展
            {
                if(closedPt.DistanceTo(spt) < tor)
                {
                    return new Line(pt, ept);
                }
                else
                {
                    return new Line(pt, spt);
                }
            }
            return line;
        }

        public static double GetMaxWidth(Polyline area)
        {
            var maxPt = area.GeometricExtents.MaxPoint;
            var minPt = area.GeometricExtents.MinPoint;
            return Math.Max(Math.Abs(maxPt.X - minPt.X), Math.Abs(maxPt.Y - minPt.Y));
        }

        public static bool Split(bool isDirectlyArrange, OuterBrder outerBrder, Dictionary<int, Line> seglineDic, 
            ref List<double> maxVals, ref List<double> minVals, out Dictionary<int, List<int>> seglineIndexDic, out int segAreasCnt)
        {
            if(isDirectlyArrange)//无迭代速排
            {
                var rst = DirectlyArrangeParaGet(outerBrder, ref maxVals, ref minVals, out seglineIndexDic, out segAreasCnt);
                return rst;
            }

            var area = outerBrder.WallLine;
            var buildLinesSpatialIndex = outerBrder.BuildingSpatialIndex;
            var attachedRampSpatialIndex = outerBrder.AttachedRampSpatialIndex;
            var buildingWithoutRampSpatialIndex = outerBrder.BuildingWithoutRampSpatialIndex;
            var BoundarySpatialIndex = outerBrder.BoundarySpatialIndex;
            var areas = new List<Polyline>() { area };
            seglineIndexDic = GetSegLineIndexDic(seglineDic);//获取线的邻接表
            var segLines = GetExtendSegline(seglineDic, seglineIndexDic);//进行线的延展
            var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            segAreasCnt = rstAreas.Count;
            SegLineCut(segLines, area, out List<Line> cutlines);

            var width = GetMaxWidth(area);

            var areaPts = area.GetPoints().ToList();//获取墙线的全部交点
            var dbPts = new List<DBPoint>();
            areaPts.ForEach(p => dbPts.Add(new DBPoint(p)));
            var ptsIndex = new ThCADCoreNTSSpatialIndex(dbPts.ToCollection());
            

            for (int i = 0; i < cutlines.Count; i++)
            {
                var l = cutlines[i];
                if (attachedRampSpatialIndex?.SelectFence(l.ExtendLineEx(10.0, 3)).Count > 0
                    || outerBrder.LonelyRampSpatialIndex?.SelectFence(l.ExtendLineEx(10.0, 3)).Count > 0)
                {
                    maxVals.Add(0);
                    minVals.Add(0);
                }
                else
                {
                    l = HelperEX.GetVaildSegLine(i, cutlines, area);
                    l.GetMaxMinVal(area, ptsIndex, buildLinesSpatialIndex, buildingWithoutRampSpatialIndex, width, out double maxVal2, out double minVal2);
                    if (maxVal2 < minVal2)
                    {
                        Active.Editor.WriteMessage("存在范围小于车道宽度的分割线！");
                        return false;
                    }
                    maxVals.Add(maxVal2);
                    minVals.Add(minVal2);
                }
            }
            return true;
        }

        public static bool DirectlyArrangeParaGet(OuterBrder outerBrder, ref List<double> maxVals, ref List<double> minVals, 
            out Dictionary<int, List<int>> seglineIndexDic, out int segAreasCnt)
        {
            seglineIndexDic = null;
            var areas = new List<Polyline>() { outerBrder.WallLine };
            var buildingSpatialIndex = outerBrder.BuildingWithoutRampSpatialIndex;
            var segLines = outerBrder.SegLines;
            for(int i = 0; i < segLines.Count; i++)
            {
                //var segline = segLines[i];
                //var rect = segline.Buffer(2750);
                //var rsts = buildingSpatialIndex.SelectCrossingPolygon(rect);
                //if(rsts.Count > 0)
                //{
                //    var pts = new List<Point3d>();
                //    foreach(var rst in rsts)
                //    {
                //        var objs = new DBObjectCollection();
                //        var building = rst as BlockReference;
                //        building.Explode(objs);
                //        foreach(var obj in objs)
                //        {
                //            if(obj is Polyline pline)
                //            {
                //                pts.AddRange(pline.GetPoints());
                //            }
                //        }
                //    }
                //    var sortedPt = pts
                //        .Where(pt => areas[0].Contains(pt))
                //        .OrderBy(p => segline.GetClosestPointTo(p, false).DistanceTo(p));
                //    var closedPt = sortedPt.First();
                //    if (segline.GetClosestPointTo(closedPt, false).DistanceTo(closedPt) < 2749)
                //    {
                //        Active.Editor.WriteMessage("分割线宽度小于车道宽！");
                //        var line= segline.Clone() as Line;
                //        line.ColorIndex = ((int)ColorIndex.Red);
                //        line.AddToCurrentSpace();
                //        segAreasCnt = 0;
                //        return false;
                //    }
                //}
                maxVals.Add(0);
                minVals.Add(0);
            }
            var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            segAreasCnt = rstAreas.Count;
            return true;
        }

        public static void GetMaxMinVal(this Line line, Polyline area, ThCADCoreNTSSpatialIndex ptsIndex, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, ThCADCoreNTSSpatialIndex buildingWithoutRampSpatialIndex, double width, out double maxVal, out double minVal)
        {
            double halfCarLaneWidth = (ParameterStock.RoadWidth / 2);
            //var areaPts = area.GetPoints().ToList();//获取墙线的全部交点
            //var dbPts = new List<DBPoint>();
            //areaPts.ForEach(p => dbPts.Add(new DBPoint(p)));
            //var ptsIndex = new ThCADCoreNTSSpatialIndex(dbPts.ToCollection());
            var rect1 = line.GetHalfBuffer(true, width);//上、右半区域
            var rect2 = line.GetHalfBuffer(false, width);//下、左半区域
            var buildLines1 = buildLinesSpatialIndex.SelectCrossingPolygon(rect1);
            var buildLines2 = buildLinesSpatialIndex.SelectCrossingPolygon(rect2);
            var boundPt1 = line.GetBoundPt(buildLines1, buildingWithoutRampSpatialIndex, rect1, area, ptsIndex, out bool hasBuilding);
            var boundPt2 = line.GetBoundPt(buildLines2, buildingWithoutRampSpatialIndex, rect2, area, ptsIndex, out bool hasBuilding2);
            maxVal = line.GetMinDist(boundPt1) - halfCarLaneWidth;
            minVal = -line.GetMinDist(boundPt2) + halfCarLaneWidth;
        }

        public static Polyline GetHalfBuffer(this Line line, bool flag, double tor = 99999)
        {
            var dir = line.GetDirection();
            var pts = new Point2dCollection();
            var pline = new Polyline();
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            if (dir == 1)//竖直
            {
                if(flag)//右半部分
                {
                    pts.Add(new Point2d(spt.X, spt.Y));
                    pts.Add(new Point2d(ept.X, ept.Y));
                    pts.Add(new Point2d(ept.X + tor, ept.Y));
                    pts.Add(new Point2d(spt.X + tor, spt.Y));
                    pts.Add(new Point2d(spt.X, spt.Y));
                }
                else
                {
                    pts.Add(new Point2d(spt.X, spt.Y));
                    pts.Add(new Point2d(ept.X, ept.Y));
                    pts.Add(new Point2d(ept.X - tor, ept.Y));
                    pts.Add(new Point2d(spt.X - tor, spt.Y));
                    pts.Add(new Point2d(spt.X, spt.Y));
                }
            }
            else if(dir == -1)//水平
            {
                if (flag)//上半部分
                {
                    pts.Add(new Point2d(spt.X, spt.Y));
                    pts.Add(new Point2d(spt.X, spt.Y + tor));
                    pts.Add(new Point2d(ept.X, ept.Y + tor));
                    pts.Add(new Point2d(ept.X, ept.Y));
                    pts.Add(new Point2d(spt.X, spt.Y));
                }
                else
                {
                    pts.Add(new Point2d(spt.X, spt.Y));
                    pts.Add(new Point2d(spt.X, spt.Y - tor));
                    pts.Add(new Point2d(ept.X, ept.Y - tor));
                    pts.Add(new Point2d(ept.X, ept.Y));
                    pts.Add(new Point2d(spt.X, spt.Y));
                }
            }
            pline.CreatePolyline(pts);
            return pline;
        }

        public static List<Polyline> Split(Polyline area, Dictionary<int, Line> seglineDic, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex,
            Dictionary<int,List<int>> seglineIndexDic)
        {
            var areas = new List<Polyline>() { area };
            var segLines = GetExtendSegline(seglineDic, seglineIndexDic);//进行线的延展
            segLines = SeglineTools.SeglinePrecut(segLines, area);//预切割
            if (!segLines.Allconnected()) return new List<Polyline>();//判断车道是否相连
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var splitterDebugLayerName = "AI-分割线-Debug";
                if (!currentDb.Layers.Contains(splitterDebugLayerName))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(currentDb.Database, splitterDebugLayerName, 30);
                }

                foreach (var seg in segLines)
                {
                    seg.Layer = splitterDebugLayerName;
                    currentDb.CurrentSpace.Add(seg);
                }
            }
#endif

            var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            return rstAreas;
        }
    }
}
