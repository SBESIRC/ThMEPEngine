using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Algorithm
{
    public class ThHatchToPolylineService
    {
        private double PointOffsetDistance = 2.0;
        private List<Polyline> BuildPolylines { get; set; }
        private Hatch CurrentHatch { get; set; }
        private ThHatchToPolylineService(Hatch hatch)
        {
            BuildPolylines = new List<Polyline>();
            CurrentHatch = hatch;
        }
        public static List<Polyline> ToGapPolyline(Hatch hatch)
        {
            var intstance = new ThHatchToPolylineService(hatch);
            intstance.ToGapPolyline();
            return intstance.BuildPolylines;
        }
        private void ToGapPolyline()
        {
            var polygons = ToPolygons();
            polygons.ForEach(o =>
            {
                BuildPolylines.Add(ToGapPolyline(o));
            });
        }
        private Polyline ToGapPolyline(Polygon polygon)
        {
            Polyline shell= polygon.Shell.ToDbPolyline();
            var holes = polygon.Holes;
            if (holes.Length == 0)
            {
                return shell;
            }
            else if(holes.Length == 1)
            {
                Polyline shellOutline = shell.ToNTSLineString().ToDbPolyline();
                return BuildOutermostPolyline(shellOutline, holes[0].ToDbPolyline());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private List<Polygon> ToPolygons()
        {
            var results = new List<Polygon>();
            var polygons = new List<Polygon>();
            CurrentHatch.Boundaries().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    polygons.Add(polyline.ToNTSLineString().ToDbPolyline().ToNTSPolygon());
                }
                else if (o is Circle circle)
                {
                    var circlePolygon = circle.ToNTSPolygon();
                    if (circlePolygon != null)
                    {
                        polygons.Add(circlePolygon);
                    }
                }
            });
            MultiPolygon multiPolygon = ThCADCoreNTSService.Instance.
                GeometryFactory.CreateMultiPolygon(polygons.ToArray());
            ThCADCoreNTSBuildArea buildArea = new ThCADCoreNTSBuildArea();
            var result = buildArea.Build(multiPolygon);
            foreach (var ploygon in FilterPolygons(result))
            {
                results.Add(ploygon);
            }
            return results;
        }
        private static List<Polygon> FilterPolygons(Geometry geometry)
        {
            var objs = new List<Polygon>();
            if (geometry.IsEmpty)
            {
                return objs;
            }
            if (geometry is Polygon polygon)
            {
                objs.Add(polygon);
            }
            else if (geometry is MultiPolygon polygons)
            {
                polygons.Geometries.ForEach(g => objs.AddRange(FilterPolygons(g)));
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }
        private Polyline BuildOutermostPolyline(Polyline outerPolyline, Polyline innerPolyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.ModelSpace.Add(outerPolyline);
                acadDatabase.ModelSpace.Add(innerPolyline);
            }
            List<Line> lines = new List<Line>();
            List<Tuple<int, Point3d, Point3d>> splitSegments = new List<Tuple<int, Point3d, Point3d>>();
            for (int i = 0; i < innerPolyline.NumberOfVertices; i++)
            {
                var firstLineSegment = innerPolyline.GetLineSegmentAt(i);
                if(firstLineSegment.Length<= PointOffsetDistance*2)
                {
                    continue;
                }
                var pts = BuildOffsetPoints(firstLineSegment, PointOffsetDistance);
                if (pts.Count != 2)
                {
                    continue;
                }
                Vector3d? extendVec = LineExtendVector(innerPolyline, pts[0], pts[0].GetVectorTo(pts[1]));
                if (extendVec == null)
                {
                    continue;
                }
                Point3d firstProjectPt = outerPolyline.GetClosestPointTo(pts[0], extendVec.Value, false);
                Point3d secondProjectPt = outerPolyline.GetClosestPointTo(pts[1], extendVec.Value, false);
                for (int j = 0; j < outerPolyline.NumberOfVertices; j++)
                {
                    var secondLineSegment = outerPolyline.GetLineSegmentAt(j);
                    if (secondLineSegment.Length <= PointOffsetDistance * 2)
                    {
                        continue;
                    }
                    if (IsIn(secondLineSegment, firstProjectPt) &&
                        IsIn(secondLineSegment, secondProjectPt))
                    {
                        splitSegments.Add(Tuple.Create(i, pts[0], pts[1]));
                        splitSegments.Add(Tuple.Create(j, firstProjectPt, secondProjectPt));
                        lines.Add(new Line(pts[0], firstProjectPt));
                        lines.Add(new Line(pts[1], secondProjectPt));
                        break;
                    }
                }
                if (splitSegments.Count > 0)
                {
                    break;
                }
            }
            if (splitSegments.Count == 2)
            {
                lines.AddRange(GetLines(innerPolyline, splitSegments[0]));
                lines.AddRange(GetLines(outerPolyline, splitSegments[1]));
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                lines.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
            DBObjectCollection dbObjs = new DBObjectCollection();
            lines.ForEach(o => dbObjs.Add(o));
            var unionObjs = dbObjs.Polygonize();
            List<Polyline> polygonPolyines = new List<Polyline>();
            unionObjs.ForEach(o =>
            {
                if (o is Polygon polygon)
                {
                    polygonPolyines.Add(polygon.Shell.ToDbPolyline());
                }
            });
            return polygonPolyines.OrderByDescending(o => o.Area).First();
        }
        private Vector3d? LineExtendVector(Polyline self,Point3d pt,Vector3d ptOnLineVec)
        {
            var perpendVec = ptOnLineVec.GetPerpendicularVector().GetNormal();
            Point3d extendPt1 = pt - perpendVec.MultiplyBy(1e6);
            Point3d extendPt2 = pt + perpendVec.MultiplyBy(1e6);
            if(IntsertPoints(pt, extendPt1, self).Count==1)
            {
                return pt.GetVectorTo(extendPt1).GetNormal();
            }
            else if(IntsertPoints(pt, extendPt2, self).Count == 1)
            {
                return pt.GetVectorTo(extendPt2).GetNormal();
            }
            else
            {
                return null;
            }
        }
        private Point3dCollection IntsertPoints(Point3d basePt,Point3d extendPt, Polyline polyline)
        {
            Point3dCollection intersectPts = new Point3dCollection();
            Line extendLine = new Line(basePt, extendPt);
            extendLine.IntersectWith(polyline, Intersect.OnBothOperands, intersectPts, IntPtr.Zero, IntPtr.Zero);
            return intersectPts;
        }
        private List<Line> GetLines(Polyline polyline , Tuple<int, Point3d, Point3d> segmentItem)
        {
            List<Line> lines = new List<Line>();
            for(int i=0;i<polyline.NumberOfVertices;i++)
            {
                var lineSegment = polyline.GetLineSegmentAt(i);
                if (i != segmentItem.Item1)
                {
                    lines.Add(new Line(lineSegment.StartPoint, lineSegment.EndPoint));
                }
                else
                {
                    if(lineSegment.StartPoint.DistanceTo(segmentItem.Item2)<
                        lineSegment.StartPoint.DistanceTo(segmentItem.Item3))
                    {
                        lines.Add(new Line(lineSegment.StartPoint, segmentItem.Item2));
                        lines.Add(new Line(segmentItem.Item3, lineSegment.EndPoint));
                    }
                    else
                    {
                        lines.Add(new Line(lineSegment.StartPoint, segmentItem.Item3));
                        lines.Add(new Line(segmentItem.Item2, lineSegment.EndPoint));
                    }
                }
            }
            return lines.Where(o=>o.Length>0).ToList();
        }
        private List<Point3d> BuildOffsetPoints(LineSegment3d lineSegment, double offsetDis)
        {
            List<Point3d> offsetPts = new List<Point3d>();
            Point3d midPt = ThGeometryTool.GetMidPt(lineSegment.StartPoint, lineSegment.EndPoint);
            Point3d firstPt = midPt + midPt.GetVectorTo(lineSegment.StartPoint).GetNormal().MultiplyBy(1.0);
            Point3d secondPt = midPt + midPt.GetVectorTo(lineSegment.EndPoint).GetNormal().MultiplyBy(1.0);
            if(IsIn(lineSegment, firstPt))
            {
                offsetPts.Add(firstPt);
            }
            if (IsIn(lineSegment, secondPt))
            {
                offsetPts.Add(secondPt);
            }            
            return offsetPts;
        }
        private bool IsIn(LineSegment3d lineSegment,Point3d pt,double tolerance=1.0)
        {
            return ThGeometryTool.IsPointInLine(lineSegment.StartPoint, lineSegment.EndPoint, pt, tolerance);
        }
    }
}
