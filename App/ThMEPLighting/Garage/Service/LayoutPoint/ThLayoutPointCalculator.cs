using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    internal class ThLayoutPointCalculator
    {
        private double Margin { get; set; }
        private double Interval { get; set; }
        private double LampLength { get; set; }
        private List<Line> UnLayoutLines { get; set; }
        public List<Point3d> Results { get; set; }
        private double Step = 5.0;
        private double RangeRatio = 0.1;
        private Polyline Path { get; set; }

        private ThCADCoreNTSSpatialIndex UnLayoutLineIndex { get; set; }
        
        public ThLayoutPointCalculator(Polyline path, List<Line> unLayoutLines,double interval,double margin,double lampLength)
        {
            Margin = margin;
            Path = path;
            Interval = interval;
            LampLength = lampLength;
            Results = new List<Point3d>();
            UnLayoutLines = unLayoutLines;
            UnLayoutLineIndex = new ThCADCoreNTSSpatialIndex(unLayoutLines.ToCollection());
        }

        public void Layout()
        {
            Layout1(); 
        }

        private void Layout1()
        {
            var lines = Path.ToLines();
            var pts = Distribute(this.Margin, lines);
            if (pts.Count == 0)
            {
                Results = Distribute(Path, this.Margin, Interval);
            }
            else
            {
                Results = pts;
            }
            lines.ToCollection().ThDispose();
        }

        private void Layout2()
        {
            var pts = new List<Point3d>();
            var length = this.Margin;
            var interval = this.Interval;
            bool isSucess = false;
            while (true)
            {
                if(length>=Path.Length)
                {
                    isSucess = true;
                    break;
                }
                if(!IsValid(interval))
                {
                    break;
                }
                var pt = Path.GetPolylinePt(length);
                if(IsValid(pt))
                {
                    pts.Add(pt);
                    length += interval;
                }
                else
                {
                    // 点与障碍物碰撞,将点调整到障碍物的两边
                    var newPt = Avoid(pt, pts);
                    if(newPt.HasValue)
                    {
                        pts.Add(newPt.Value);
                        length += interval;
                    }
                    else
                    {
                        // 走不通，重新开始
                        length = this.Margin;
                        interval -= this.Step;
                        pts = new List<Point3d>();
                    }
                }
            }
            if(isSucess)
            {
                Results = pts;
            }
            else
            {
                Results = Distribute(Path, this.Margin, Interval);
            }
        }

        private Point3d? Avoid(Point3d pt,List<Point3d> preLayoutPts)
        {
            var canLayoutPts = GetCanLayoutPoints(pt);
            var usefulPts = GetUsefulPts(canLayoutPts);
            usefulPts = usefulPts.Where(p => p.IsOn(Path)).ToList();
            if (usefulPts.Count==0)
            {
                return null;
            }
            if(preLayoutPts.Count==0)
            {
                return usefulPts.OrderBy(o => Path.GetDistAtPoint(o)).First();
            }
            else
            {
                var prePt = preLayoutPts.Last();
                usefulPts = usefulPts.Where(o=> CalculateInterval(Path,o,prePt)<=Interval).OrderByDescending(o => Path.GetDistAtPoint(o)).ToList();
                if(usefulPts.Count>0)
                {
                    return usefulPts.First();
                }
                else
                {
                    return null;
                }
            }
        }

        private bool IsValid(double interval)
        {
            return interval >= 0.9 * Interval && interval <= Interval;
        }

        private double CalculateInterval(Polyline path,Point3d first,Point3d second)
        {
            double firstDis = path.GetDistAtPoint(first);
            double secondDis = path.GetDistAtPoint(second);
            return Math.Abs(secondDis - firstDis);
        }

        private List<Point3d> GetUsefulPts(List<Tuple<Point3d, Point3d>> canLayoutPts)
        {
            var usefulPts = new List<Point3d>();
            canLayoutPts.ForEach(o =>
            {
                var dir = o.Item1.GetVectorTo(o.Item2);
                var firstWire = CreateLightWire(o.Item1, dir, LampLength);
                if (!IsConflict(firstWire.Item1, firstWire.Item2))
                {
                    usefulPts.Add(o.Item1);
                }
                var secondWire = CreateLightWire(o.Item2, dir, LampLength);
                if (!IsConflict(secondWire.Item1, secondWire.Item2))
                {
                    usefulPts.Add(o.Item2);
                }
            });
            return usefulPts;
        }

        private List<Tuple<Point3d, Point3d>> GetCanLayoutPoints(Point3d pt)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            var lineSegs = GetLineSegments(pt);
            foreach(var lineSeg in lineSegs)
            {
                var lightWire = CreateLightWire(pt, lineSeg.Direction, LampLength);
                var outline = CreateOutline(lightWire.Item1, lightWire.Item2, 1.0);
                var unLayoutLines = SelectCrossingPolygon(outline, UnLayoutLineIndex);
                var collinears = unLayoutLines
                    .OfType<Line>()
                    .Where(o => IsCollinear(lineSeg.StartPoint, lineSeg.EndPoint, o.StartPoint, o.EndPoint))
                    .ToList();
                results.AddRange(collinears.Select(o => CalculatePoint(o,5.0)));
            }
            return results;
        }

        private Tuple<Point3d,Point3d> CalculatePoint(Line unLayoutLine,double interval=1.0)
        {
            var dir = unLayoutLine.LineDirection();
            var length = LampLength / 2.0 + interval;
            var sp = unLayoutLine.StartPoint - dir.MultiplyBy(length);
            var ep = unLayoutLine.EndPoint + dir.MultiplyBy(length);
            return Tuple.Create(sp, ep);
        }

        private bool IsCollinear(Point3d firstSp,Point3d firstEp,Point3d secondSp,Point3d secondEp)
        {
            return ThGeometryTool.IsCollinearEx(firstSp, firstEp, secondSp, secondEp);
        }

        private bool IsValid(Point3d pt)
        {
            var lineSegs = GetLineSegments(pt);
            if(lineSegs.Count== 0)
            {
                return false;
            }
            foreach(var lineSeg in lineSegs)
            {
                var lightWire = CreateLightWire(pt, lineSeg.Direction, LampLength);
                if (IsConflict(lightWire.Item1, lightWire.Item2))
                {
                    return false;
                }
            }
            return true;
        }

        private List<LineSegment3d> GetLineSegments(Point3d pt)
        {
            var results = new List<LineSegment3d>();
            for(int i=0;i<Path.NumberOfVertices-1;i++)
            {
                if(Path.GetSegmentType(i)!=SegmentType.Line)
                {
                    continue;
                }
                var lingSeg = Path.GetLineSegmentAt(i);
                if(ThGeometryTool.IsPointInLine(lingSeg.StartPoint, lingSeg.EndPoint,pt))
                {
                    results.Add(lingSeg);
                }
            }
            return results;
        }

        private List<Point3d> Distribute(double margin,List<Line> lines)
        {
            var results = new List<Point3d>();
            for (double adjustDis = 0.0; adjustDis <= RangeRatio * Interval; adjustDis += Step)
            {
                var pts = Distribute(Path, margin, Interval - adjustDis);
                if (IsValid(pts, lines))
                {
                    results = pts;
                    break;
                }
            }
            return results;
        }

        private bool IsOnPath(Point3d pt)
        {
            return pt.IsOn(Path);
        }        

        private bool IsValid(List<Point3d> pts,List<Line> dxLines)
        {
            // 布置的点在不可布区域就是不合理的
            if(LampLength>0)
            {
                var lightWires = new List<Tuple<Point3d, Point3d>>();
                var ptDic = DistributePoints(pts, dxLines);
                ptDic.ForEach(o=>
                {
                    var dir = o.Key.LineDirection();
                    lightWires.AddRange(o.Value.Select(p => CreateLightWire(p, dir, LampLength)));
                });
                return !lightWires.Where(o=>IsConflict(o.Item1,o.Item2)).Any();
            }
            else
            {
                var dbPoints = ToDBPoints(pts); // 把点转成DBPoints
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
                var isIn = IsIn(spatialIndex); // 判断点是否在不可布区域内
                dbPoints.ThDispose(); // 释放资源
                return !isIn;
            }
        }

        private bool IsConflict(Point3d lightSp,Point3d lightEp)
        {
            var outline = CreateOutline(lightSp, lightEp, 1.0);
            var unlayoutLines = SelectCrossingPolygon(outline, UnLayoutLineIndex);
            outline.Dispose();
            return unlayoutLines.Count > 0;
        }

        private Polyline CreateOutline(Point3d sp,Point3d ep,double width)
        {
            return ThDrawTool.ToOutline(sp, ep, width);
        }

        private DBObjectCollection SelectCrossingPolygon(Polyline outline,ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return spatialIndex.SelectCrossingPolygon(outline);
        }

        private Dictionary<Line,List<Point3d>> DistributePoints(List<Point3d> pts,List<Line> edges)
        {
            return ThQueryPointService.Query(pts, edges);
        }

        private Tuple<Point3d, Point3d> CreateLightWire(Point3d basePt, Vector3d direction, double length)
        {
            var sp = basePt - direction.GetNormal().MultiplyBy(length / 2.0);
            var ep = basePt + direction.GetNormal().MultiplyBy(length / 2.0);
            return Tuple.Create(sp, ep);
        }

        private bool IsIn(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return UnLayoutLines.Where(o =>
            {
                var outline = ThDrawTool.ToOutline(o.StartPoint, o.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
                var objs = spatialIndex.SelectCrossingPolygon(outline);
                outline.Dispose();
                return objs.Count > 0;
            }).Any();
        }

        private DBObjectCollection ToDBPoints(List<Point3d> pts)
        {
            return pts.Select(p => new DBPoint(p)).ToCollection();
        }

        private List<Point3d> Distribute(Polyline path, double margin, double interval)
        {
            if(interval>=0)
            {
                var pts = GetPoints(path);
                var lineParameter = new ThLineSplitParameter
                {
                    Margin = margin,
                    Interval = interval,
                    Segment = pts,
                };
                return lineParameter.Distribute();
            }
            else
            {
                return new List<Point3d>();
            }
        }
        private List<Point3d> GetPoints(Polyline pLine)
        {
            var results = new List<Point3d>();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                results.Add(pLine.GetPoint3dAt(i));
            }
            return results;
        }
    }
}
