using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThCADCore.NTS;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using Dreambuild.AutoCAD;
using DotNetARX;

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

        public static List<Line> SegLineCut(List<Line> lines, Polyline area)
        {
            var cutLines = new List<Line>();
            for(int i =0; i < lines.Count; i++)
            {
                var pts = new List<Point3d>();
                var line = lines[i];
                var spt = line.StartPoint;
                pts.AddRange(line.Intersect(area, 0));//求与边界的交点
                for(int j = 0; j < lines.Count;j++)
                {
                    if (i == j) continue;
                    pts.AddRange(line.Intersect(lines[j], 0));//求与其他分割线的交点
                }
                var orderPts = pts.OrderBy(p => p.DistanceTo(line.StartPoint));
                cutLines.Add(new Line(orderPts.First(), orderPts.Last()));
            }

            return cutLines;
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
        public static List<Polyline> Split(Polyline area, Dictionary<int, Line> seglineDic, ThCADCoreNTSSpatialIndex buildLinesSpatialIndex, 
            ref List<double> maxVals, ref List<double> minVals, out Dictionary<int, List<int>> seglineIndexDic)
        {
            var areas = new List<Polyline>() { area };
            seglineIndexDic = GetSegLineIndexDic(seglineDic);//获取线的邻接表
            var segLines = GetExtendSegline(seglineDic, seglineIndexDic);//进行线的延展
            var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            var cutlines = SegLineCut(segLines, area);
            foreach (var l in cutlines)
            {
                var rect1 = l.GetHalfBuffer(true);//上、右半区域
                var rect2 = l.GetHalfBuffer(false);//下、左半区域
                var buildLines1 = buildLinesSpatialIndex.SelectCrossingPolygon(rect1);
                var buildLines2 = buildLinesSpatialIndex.SelectCrossingPolygon(rect2);
                var boundPt1 = l.GetBoundPt(buildLines1, rect1);
                var boundPt2 = l.GetBoundPt(buildLines2, rect2);
                maxVals.Add(l.GetMinDist(boundPt1) - 2760);
                minVals.Add(l.GetMinDist(boundPt2) + 2760);
            }
            return rstAreas;
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
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                foreach (var seg in segLines)
                {
                    currentDb.CurrentSpace.Add(seg);
                }

            }
#endif
            var rstAreas = segLines.SplitArea(areas);//基于延展线进行区域分割
            return rstAreas;
        }
    }
}
