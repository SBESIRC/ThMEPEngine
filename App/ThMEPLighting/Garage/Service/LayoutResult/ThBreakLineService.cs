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
        // 打断原则
        // 在当前Ucs下，打断线的角度在[45度,135度],[225度,315度]的线

        private Matrix3d wcsToUcs;
        private double Length { get; set; } = 200.0;
        private Matrix3d WcsToUcs { get; set; }
        private double TesslateLength = 100.0;

        public ThBreakLineService(Matrix3d currentUserCoordinateSystem, double length)
        {
            Length = length;
            wcsToUcs = currentUserCoordinateSystem.Inverse();
        }
        public DBObjectCollection Break(DBObjectCollection wires)
        {
            if(Length<=0.0)
            {
                return wires;
            }
            using (var ov = new ThCADCoreNTSArcTessellationLength(TesslateLength))
            {
                var results = new DBObjectCollection();
                var breakLines = wires.OfType<Line>().Where(l => IsQualified(l.LineDirection())).ToList();
                var otherCurves = wires
                    .OfType<Curve>()
                    .Where(o=>o is Line || o is Arc)
                    .Where(l => !breakLines.Contains(l)).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(otherCurves);
                breakLines.ForEach(l =>
                {
                    if(l.Length > Length)
                    {
                        var onCurves = Query(l, spatialIndex);
                        var res = Break(l, onCurves, Length);
                        res.ForEach(o => results.Add(o));
                    }
                    else
                    {
                        results.Add(l);
                    }
                });
                results = results.Union(otherCurves);
                return results;
            }
        }

        private List<Line> Break(Line line,DBObjectCollection onCurves,double length)
        {
            var overlapLines = new List<Line>();
            var direction = line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
            onCurves.OfType<Curve>().ForEach(e =>
            {
                var inters = GetIntersPtNotOnPort(line, e);
                inters.OfType<Point3d>().ForEach(p =>
                {
                    var ptPair = BuildBreakLineSP(p, direction, length);
                    if(IsInLine(ptPair.Item1,line,1.0) && IsInLine(ptPair.Item2, line, 1.0))
                    {
                        overlapLines.Add(new Line(ptPair.Item1, ptPair.Item2));
                    }
                });
            });
            if (overlapLines.Count==0)
            {
                return new List<Line> { line };
            }
            else
            {
                var results = line.Difference(overlapLines).Where(o => o.Length > 1.0).ToList();
                overlapLines.ForEach(o => o.Dispose());
                return results;
            }
        }

        private bool IsQualified(Vector3d vec)
        {
            var newVec = vec.TransformBy(wcsToUcs);
            var ang = newVec.GetAngleTo(Vector3d.XAxis).RadToAng();
            return (ang >= 45 && ang <= 135) || (ang >= 225 && ang <= 315);
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
        private DBObjectCollection Query(Line line, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var rec = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
            var objs = spatialIndex.SelectCrossingPolygon(rec);
            objs.Remove(line);
            return objs;
        }
    }
}
