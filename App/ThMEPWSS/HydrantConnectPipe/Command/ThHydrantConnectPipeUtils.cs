using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public static class ThHydrantConnectPipeUtils
    {
        public static double GetDistFireHydrantToPipe(ThHydrant fireHydrant, ThHydrantPipe pipe)
        {
            return ThCADCoreNTSDistance.Distance(fireHydrant.FireHydrantObb,pipe.PipePosition);
        }
        public static bool HydrantIsContainPipe(ThHydrant fireHydrant, List<ThHydrantPipe> pipes)
        {
            double minDist = 9999.0;
            foreach(var pipe in pipes)
            {
                if (fireHydrant.IsContainsPipe(pipe, 500.0))
                {
                    double tmpDist = ThHydrantConnectPipeUtils.GetDistFireHydrantToPipe(fireHydrant, pipe);
                    if(minDist > tmpDist)
                    {
                        fireHydrant.FireHydrantPipe = pipe;
                        minDist = tmpDist;
                    }
                }
            }
            return pipes.Remove(fireHydrant.FireHydrantPipe);
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
            Polyline polyLine = new Polyline() { Closed = true };
            var allPts = pts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;
            polyLine.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
            polyLine.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
            polyLine.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
            polyLine.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
            var mapFrame = polyLine.Buffer(expandLength);
            return mapFrame[0] as Polyline;
        }

        public static List<Line> GetNearbyLine4(Point3d pt,List<Line> lines)
        {
            List<Line> returnLines = new List<Line>();
            if (lines.Count <=2 )
            {
                return lines;
            }

            lines = lines.OrderBy(o => o.DistanceToPoint(pt)).ToList();
            returnLines.Add(lines[0]);
            returnLines.Add(lines[1]);
            return returnLines;
        }
    }
}
