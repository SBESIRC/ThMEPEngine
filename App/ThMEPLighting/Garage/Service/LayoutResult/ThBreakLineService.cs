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
            var results = new DBObjectCollection();
            var breakLines = wires.OfType<Line>().Where(l => IsQualified(l.LineDirection())).ToList();
            var otherLines = wires.OfType<Line>().Where(l => !breakLines.Contains(l)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(otherLines);
            breakLines.Where(l => l.Length > Length).ForEach(l =>
                {
                    var onLines = Query(l, spatialIndex);
                    var res = Break(l, onLines, Length);
                    res.ForEach(o => results.Add(o));
                });

            results = results.Union(otherLines);
            return results;
        }
        private List<Line> Break(Line line,DBObjectCollection onLines,double length)
        {
            var overlapLines = new List<Line>();
            onLines.OfType<Line>().ForEach(e =>
            {
                var inters = GetValidIntersPt(line, e);
                if (inters.HasValue)
                {
                   var overlapLine = CalculateSplitLine(line, inters.Value, length);
                    if(overlapLine.Length>1e-6)
                    {
                        overlapLines.Add(overlapLine);
                    }
                }
            });

            if(overlapLines.Count==0)
            {
                return new List<Line> { line };
            }
            else
            {
                return line.Difference(overlapLines);
            }
        }

        private bool IsQualified(Vector3d vec)
        {
            var newVec = vec.TransformBy(wcsToUcs);
            var ang = newVec.GetAngleTo(Vector3d.XAxis).RadToAng();
            return (ang >= 45 && ang <= 135) || (ang >= 225 && ang <= 315);
        }
        private Line CalculateSplitLine(Line line,Point3d pt, double length)
        {
            var vec = line.LineDirection();
            var pt1 = pt - vec.MultiplyBy(length / 2.0);
            var pt2 = pt + vec.MultiplyBy(length / 2.0);
            if(ThGeometryTool.IsPointInLine(line.StartPoint, line.EndPoint, pt1,1.0) &&
                ThGeometryTool.IsPointInLine(line.StartPoint, line.EndPoint, pt2, 1.0))
            {
                return new Line(pt1, pt2);
            }  
            else
            {
                return new Line();
            }
        }
        private Point3d? GetValidIntersPt(Line first,Line second)
        {
            var inters = first.IntersectWithEx(second);
            if(inters.Count==0)
            {
                return null;
            }
            var ports = new List<Point3d>() { first.StartPoint, first.EndPoint, 
                second.StartPoint, second.EndPoint };
            if(IsCloseToPorts(inters[0], ports))
            {
                return null;
            }
            return inters[0];
        }
        private bool IsCloseToPorts(Point3d intersPt,List<Point3d> portPts)
        {
            return portPts.Where(p => intersPt.DistanceTo(p) <= ThGarageLightCommon.RepeatedPointDistance).Any();
        }
        private DBObjectCollection Query(Line line, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var rec = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
            var objs = spatialIndex.SelectCrossingPolygon(rec);
            return objs
                .OfType<Line>()
                .Where(l => !ThGeometryTool.IsCollinearEx(line.StartPoint, line.EndPoint, l.StartPoint, l.EndPoint))
                .ToCollection();
        }
    }
}
