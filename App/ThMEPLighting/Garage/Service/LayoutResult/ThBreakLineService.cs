using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThBreakLineService
    {
        private Matrix3d wcsToUcs =Matrix3d.Identity;
        private double TesslateLength = 100.0;
        private ThCADCoreNTSSpatialIndex ArcSpatialIndex { get; set; }
        public ThBreakLineService(Matrix3d currentUserCoordinateSystem)
        {
            wcsToUcs = currentUserCoordinateSystem.Inverse();
        }
        public DBObjectCollection BreakByHeight(DBObjectCollection wires,double height)
        {
            if(height <= 0.0)
            {
                return wires;
            }
            using (var ov = new ThCADCoreNTSArcTessellationLength(TesslateLength))
            {
                var results = new DBObjectCollection();
                var arcs = wires.OfType<Arc>().ToCollection(); // 圆弧不参与打断
                ArcSpatialIndex = new ThCADCoreNTSSpatialIndex(arcs);
                wires = wires.OfType<Line>().ToCollection();   // 只考虑线的打断
                var breakLines = FindIntersectLines(wires,height); //要被打段的
                var unBreakLines = wires.Difference(breakLines); // 不会与其它相交的线
                results = results.Union(arcs);
                results = results.Union(unBreakLines);
                while (true)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(breakLines);
                    var breakLine = GetBreakLine(breakLines, height);
                    if (breakLine == null)
                    {
                        break;
                    }
                    else
                    {
                        var res = BreakByHeight(breakLine.Item1, breakLine.Item2, height);
                        if(res ==null || res.Item2.Count==0)
                        {
                            break;
                        }
                        else
                        {
                            breakLines.Remove(res.Item1);
                            res.Item2.ForEach(o=>breakLines.Add(o));
                        }
                    }
                }
                results = results.Union(breakLines);
                return results;
            }
        }

        private Tuple<Line,List<Line>> BreakByHeight(Line line, Curve curve, double length)
        {
            if(curve is Line line1)
            {
                return BreakByHeight(line, line1, length);
            }
            else if(curve is Arc arc)
            {
                // 对于弧的打断只能按长度
                return BreakByLength(line, arc, length*2.0);
            }
            return null;
        }

        private Tuple<Line, List<Line>> BreakByLength(Line first,Line second,double length)
        {
            // 更偏向于Y轴的线，被更偏向于X轴的线打断
            if (AngToYAxis(first.LineDirection()) < AngToYAxis(second.LineDirection()))
            {
                // first 更靠近Y轴 , 打断 first
                return Tuple.Create(first, BreakLineByLength(first, second, length));
            }
            else
            {
                // second 更靠近Y轴, 打断 second
                return Tuple.Create(second, BreakLineByLength(second, first, length));
            }
        }

        private Tuple<Line, List<Line>> BreakByHeight(Line first, Line second, double height)
        {
            // 更偏向于Y轴的线，被更偏向于X轴的线打断
            if (AngToYAxis(first.LineDirection()) < AngToYAxis(second.LineDirection()))
            {
                // first 更靠近Y轴 , 打断 first
                return Tuple.Create(first, BreakLineByHeight(first, second, height));
            }
            else
            {
                // second 更靠近Y轴, 打断 second
                return Tuple.Create(second, BreakLineByHeight(second, first, height));
            }
        }

        private List<Line> BreakLineByLength(Line first,Line second,double length)
        {
            var inters = GetIntersPtNotOnPort(first, second);
            var overlapLines = new List<Line>();
            inters.OfType<Point3d>().ForEach(p =>
            {
                var ptPair = BuildBreakLineSP(p, first.LineDirection(), length);
                overlapLines.Add(new Line(ptPair.Item1, ptPair.Item2));
            });
            var results = first.Difference(overlapLines).Where(o => o.Length > 1.0).ToList();
            overlapLines.ForEach(o => o.Dispose());
            return results;
        }

        private List<Line> BreakLineByHeight(Line first, Line second, double height)
        {
            var results = new List<Line>();
            var inters = GetIntersPtNotOnPort(first, second);
            if(inters.Count==1)
            {
                var intersPt = inters[0];
                var firstDir = first.LineDirection();
                var secondDir = second.LineDirection();
                var jiajiao = firstDir.GetAngleTo(secondDir);
                double length = height / Math.Sin(jiajiao);
                var ptPair = BuildBreakLineSP(intersPt, firstDir, length*2.0);

                var overlapLines = new List<Line>();
                overlapLines.Add(new Line(ptPair.Item1, ptPair.Item2));
                results = first.Difference(overlapLines).Where(o => o.Length > 1.0).ToList();
                overlapLines.ForEach(o => o.Dispose());
            }
            return results;
        }

        private double AngToYAxis(Vector3d vec)
        {
            var newVec = vec.TransformBy(wcsToUcs);
            return Math.Min(newVec.GetAngleTo(Vector3d.YAxis).RadToAng(), 
                newVec.GetAngleTo(Vector3d.YAxis.Negate()).RadToAng());
        }
        private Tuple<Line, List<Line>> BreakByLength(Line first, Arc second, double length)
        {
            var direction = first.StartPoint.GetVectorTo(first.EndPoint).GetNormal();
            var inters = GetIntersPtNotOnPort(first, second);
            var overlapLines = new List<Line>();
            inters.OfType<Point3d>().ForEach(p =>
            {
                var ptPair = BuildBreakLineSP(p, direction, length);
                overlapLines.Add(new Line(ptPair.Item1, ptPair.Item2));
            });
            var results = first.Difference(overlapLines).Where(o => o.Length > 1.0).ToList();
            overlapLines.ForEach(o => o.Dispose());
            return Tuple.Create(first,results);
        }
        private Tuple<Line,Curve> GetBreakLine(DBObjectCollection wires, double length)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(wires);
            foreach (Line line in wires.OfType<Line>())
            {
                var curve = GetIntersectCurve(line, spatialIndex, length);
                if(curve!=null)
                {
                    return Tuple.Create(line, curve);
                }
            }           
            return null;
        }
        private DBObjectCollection FindIntersectLines(DBObjectCollection wires,double length)
        {
            // 先找到与其他线相交的线
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(wires);
            wires.OfType<Line>().ForEach(l =>
            {
                if(IsIntersectLine(l, spatialIndex, length))
                {
                    results.Add(l);
                }
            });
            return results;
        }        
        private bool IsIntersectLine(Line line ,ThCADCoreNTSSpatialIndex spatialIndex,double length)
        {
            var onCurves = FindCurves(line, spatialIndex);
            var direction = line.LineDirection();
            return onCurves.OfType<Curve>()
            .Where(e =>
            {
                var inters = GetIntersPtNotOnPort(line, e);
                return inters.OfType<Point3d>().Where(p =>
                {
                    var ptPair = BuildBreakLineSP(p, direction, length);
                    return IsInLine(ptPair.Item1, line, 1.0) && IsInLine(ptPair.Item2, line, 1.0);
                }).Any();
            }).Any();
        }
        private Curve GetIntersectCurve(Line line, ThCADCoreNTSSpatialIndex spatialIndex, double length)
        {
            var onCurves = FindCurves(line, spatialIndex);
            var direction = line.LineDirection();
            foreach(Curve curve in onCurves.OfType<Curve>())
            {
                var inters = GetIntersPtNotOnPort(line, curve);
                if(inters.OfType<Point3d>().Where(p =>
                {
                    var ptPair = BuildBreakLineSP(p, direction, length);
                    return IsInLine(ptPair.Item1, line, 1.0) && IsInLine(ptPair.Item2, line, 1.0);
                }).Any())
                {
                    return curve;
                }
            }
            return null;   
        }
        private bool IsInLine(Point3d pt,Line line,double tolerance=0.0)
        {
            return ThGeometryTool.IsPointInLine(line.StartPoint, line.EndPoint, pt, tolerance);
        }
        private Tuple<Point3d,Point3d> BuildBreakLineSP(Point3d pt,Vector3d direction, double length)
        {
            // direction is normal
            var pt1 = pt - direction.MultiplyBy(length / 2.0);
            var pt2 = pt + direction.MultiplyBy(length / 2.0);
            return Tuple.Create(pt1, pt2);
        }
        private Point3dCollection GetIntersPtNotOnPort(Curve first, Curve second)
        {
            var inters = first.IntersectWithEx(second);
            if(inters.Count==0)
            {
                return new Point3dCollection();
            }
            var ports = new List<Point3d>() { first.StartPoint, first.EndPoint, 
                second.StartPoint, second.EndPoint };
            return inters
                .OfType<Point3d>()
                .Where(o => !IsCloseToPorts(o, ports, ThGarageLightCommon.RepeatedPointDistance))
                .ToCollection();
        }
        private bool IsCloseToPorts(Point3d intersPt,List<Point3d> portPts,double tolerance)
        {
            return portPts.Where(p => intersPt.DistanceTo(p) <= tolerance).Any();
        }
        private DBObjectCollection FindCurves(Line line, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var results = new DBObjectCollection();           
            results = results.Union(Query(line, spatialIndex));
            results = results.Union(Query(line, ArcSpatialIndex));
            results.Remove(line);
            return results;
        }
        private DBObjectCollection Query(Line line, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var results = new DBObjectCollection();
            var rec = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
            results = spatialIndex.SelectCrossingPolygon(rec);
            results.Remove(line);
            return results;
        }
    }
}
