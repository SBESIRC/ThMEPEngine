using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public static class ThHydrantConnectPipeUtils
    {
        public static double GetDistFireHydrantToPipe(ThHydrant fireHydrant, ThHydrantPipe pipe)
        {
            var centroidPt = fireHydrant.FireHydrantObb.GetCentroidPoint();
            return centroidPt.DistanceTo(pipe.PipePosition);
        }
        public static bool PipeIsContainBranchLine(ThHydrantPipe pipe, List<Line> branchLines)
        {
            foreach (var l in branchLines)
            {
                double dist = l.DistanceToPoint(pipe.PipePosition);
                if (dist < 10.0)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool HydrantIsContainPipe(ThHydrant fireHydrant, List<ThHydrantPipe> pipes)
        {
            double minDist = 9999.0;
            foreach (var pipe in pipes)
            {
                if (fireHydrant.IsContainsPipe(pipe, 500.0))
                {
                    double tmpDist = GetDistFireHydrantToPipe(fireHydrant, pipe);
                    if (minDist > tmpDist)
                    {
                        fireHydrant.FireHydrantPipe = pipe;
                        minDist = tmpDist;
                    }
                }
            }
            return pipes.Remove(fireHydrant.FireHydrantPipe);
        }
        public static bool HydrantIsContainPipe1(ThHydrant fireHydrant, List<ThHydrantPipe> pipes)
        {
            double minDist = 9999.0;
            foreach (var pipe in pipes)
            {
                if (fireHydrant.IsContainsPipe(pipe, 500.0))
                {
                    double tmpDist = GetDistFireHydrantToPipe(fireHydrant, pipe);
                    if (minDist > tmpDist)
                    {
                        fireHydrant.FireHydrantPipe = pipe;
                        minDist = tmpDist;
                    }
                }
            }
            if (fireHydrant.FireHydrantPipe == null)
            {
                return false;
            }
            return true;
        }
        public static List<Curve> GetSegment(Polyline polyline, Point3d pt, double tolerance = 1.0)
        {
            var segments = new List<Curve>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var segmentType = polyline.GetSegmentType(i);
                if (segmentType == SegmentType.Line)
                {
                    var lineSegment = polyline.GetLineSegmentAt(i);
                    if (lineSegment.IsOn(pt, new Tolerance(tolerance, tolerance)))
                    {
                        segments.Add(new Line(lineSegment.StartPoint, lineSegment.EndPoint));
                    }
                }
                else if (segmentType == SegmentType.Arc)
                {
                    var arcSegment = polyline.GetArcSegmentAt(i);
                    if (arcSegment.IsOn(pt, new Tolerance(tolerance, tolerance)))
                    {
                        segments.Add(new Arc(arcSegment.Center, arcSegment.Normal, arcSegment.Radius, arcSegment.StartAngle, arcSegment.EndAngle));
                    }
                }
            }
            return segments;
        }
        public static List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            return resHoles;
        }
        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> polylines, Polyline line, double bufferWidth)
        {
            DBObjectCollection dBObject = new DBObjectCollection() { line };
            foreach (Polyline polyline in dBObject.Buffer(bufferWidth))
            {
                if (SelelctCrossing(polylines, polyline).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
        public static bool LineIntersctBySelect(List<Polyline> lines, Polyline pl)
        {
            foreach (var l in lines)
            {
                if (l.IsIntersects(pl))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断是否和外框线相交
        /// </summary>
        /// <param name="line"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithFrame(Curve line, Polyline frame)
        {
            return frame.IsIntersects(line);
        }
        public static Polyline CreateMapFrame(List<Point3d> pts, double expandLength)
        {
            //Polyline polyLine = new Polyline() { Closed = true };
            //var allPts = pts.OrderBy(x => x.X).ToList();
            //double minX = allPts.First().X;
            //double maxX = allPts.Last().X;
            //allPts = allPts.OrderBy(x => x.Y).ToList();
            //double minY = allPts.First().Y;
            //double maxY = allPts.Last().Y;
            //polyLine.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
            //polyLine.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
            //polyLine.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
            //polyLine.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
            //var mapFrame = polyLine.Buffer(expandLength);
            //return mapFrame[0] as Polyline;
            Polyline polyLine = new Polyline();
            polyLine.AddVertexAt(0, pts[0].ToPoint2D(), 0, 0, 0);
            polyLine.AddVertexAt(1, pts[1].ToPoint2D(), 0, 0, 0);
            var objcet = polyLine.BufferPL(expandLength)[0];
            return objcet as Polyline;
        }
        public static Polyline CreateMapFrame(Point3d pt, double radius)
        {
            Circle circle = new Circle(pt, new Vector3d(0, 0, 1), radius);
            return circle.ToRectangle();
        }
        public static List<Line> GetNearbyLine(Point3d pt, List<Line> lines, int N = 3)
        {
            List<Line> returnLines = new List<Line>();
            if (lines.Count <= N)
            {
                return lines;
            }

            lines = lines.OrderBy(o => o.DistanceToPoint(pt)).ToList();
            for (int i = 0; i < N; i++)
            {
                returnLines.Add(lines[i]);
            }
            return returnLines;
        }
        public static bool IsIntersect(Entity firstLine, Entity secLine)
        {
            var ptLst = new Point3dCollection();
            firstLine.IntersectWith(secLine, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
            if (ptLst.Count != 0)
            {
                return true;
            }
            return false;
        }
        public static List<Line> CleanLines(List<Line> lines)
        {
            if (lines.Count == 0)
            {
                return lines;
            }
            var mt = Matrix3d.Displacement(lines[0].GetMidpoint().GetVectorTo(Point3d.Origin));
            lines.ForEach(o => o.TransformBy(mt));
            var retLines = new List<Line>();
            ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            var allLineColles = cleanServiec.CleanNoding(lines.ToCollection());
            foreach (var l in allLineColles)
            {
                var line = l as Line;
                retLines.Add(line);
            }
            retLines.ForEach(o => o.TransformBy(mt.Inverse()));
            return retLines;
        }
        public static List<Line> FindInlineLines(Point3d pt, ref List<Line> targetLines, double tolerance)//差点关联线
        {
            var retLines = new List<Line>();
            var objectLine = FindLines(pt, ref targetLines, tolerance);
            if (objectLine == null)
            {
                return retLines;
            }
            retLines.Add(objectLine);
            retLines.AddRange(FindInlineLines(objectLine, ref targetLines, tolerance));
            return retLines;
        }
        public static List<Line> FindInlineLines(Line objectLine, ref List<Line> targetLines, double tolerance)//差点关联线
        {
            var retLines = new List<Line>();
            var findedLine = FindLines(objectLine, ref targetLines, tolerance);
            foreach (var line in findedLine)
            {
                retLines.AddRange(FindInlineLines(line, ref targetLines, tolerance));
            }
            retLines.AddRange(findedLine);
            return retLines;
        }
        public static Line FindLines(Point3d pt, ref List<Line> targetLines, double tolerance)
        {
            foreach (var l in targetLines)
            {
                if (l.StartPoint.DistanceTo(pt) < tolerance)
                {
                    targetLines.Remove(l);
                    return l;
                }
                else if (l.EndPoint.DistanceTo(pt) < tolerance)
                {
                    targetLines.Remove(l);
                    return l;
                }
            }
            return null;
        }
        public static List<Line> FindLines(Line objectLine, ref List<Line> targetLines, double tolerance)
        {
            var retLines = new List<Line>();
            var remLines = new List<Line>();
            foreach (var line in targetLines)
            {
                if (IsInlineLine(objectLine, line, tolerance))
                {
                    retLines.Add(line);
                    remLines.Add(line);
                }
            }
            targetLines = targetLines.Except(remLines).ToList();
            return retLines;
        }
        public static bool IsInlineLine(Line objectLine, Line targetLine, double tolerance)
        {
            var objectPt1 = objectLine.StartPoint;
            var objectPt2 = objectLine.EndPoint;
            var targetPt1 = targetLine.StartPoint;
            var targetPt2 = targetLine.EndPoint;
            if (targetLine.PointOnLine(objectPt1, false, tolerance) || targetLine.PointOnLine(objectPt2, false, tolerance))
            {
                return true;
            }
            else if (objectLine.PointOnLine(targetPt1, false, tolerance) || objectLine.PointOnLine(targetPt2, false, tolerance))
            {
                return true;
            }
            return false;
        }
    }
}
