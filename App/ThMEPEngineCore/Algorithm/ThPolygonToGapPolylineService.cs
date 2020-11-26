using System;
using AcHelper;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThPolygonToGapPolylineService
    {
        ///目前主要用于支撑对剪力墙带一个洞的裁剪
        ///暂时将线延伸设为5mm,之前设1,2mm有没成功的情况
        ///打断点偏移的距离要大于线延伸的距离
        private double PointOffsetDistance = 25.0; 
        private const double LineExtendDistance = 10.0;        
        private Polygon CurrentPolygon { get; set; }
        private ThPolygonToGapPolylineService(Polygon polygon)
        {
            CurrentPolygon = polygon;
            if(PointOffsetDistance<= LineExtendDistance*2)
            {
                PointOffsetDistance= LineExtendDistance * 2 + 10.0;
            }
        }

        public static List<Polyline> ToGapPolyline(Polygon polygon)
        {
            List<Polyline> gapPolylines = new List<Polyline>();
            if(polygon==null || polygon.Shell==null)
            {
                return gapPolylines;
            }
            using (var fixedPrecision = new ThCADCoreNTSFixedPrecision())
            {
                var intstance = new ThPolygonToGapPolylineService(polygon);
                var gapPolyline = intstance.ToGapPolyline();
                if (gapPolyline != null && gapPolyline.Area > 0.0)
                {
                    gapPolylines.Add(gapPolyline);
                }
            }
            return gapPolylines;
        }

        private Polyline ToGapPolyline()
        {
            var shell= CurrentPolygon.Shell.ToDbPolyline();
            var holes = new List<Polyline>();
            CurrentPolygon.Holes.ForEach(o => holes.Add(o.ToDbPolyline()));
            if (holes.Count == 0)
            {
                return shell;
            }
            else if(shell.Area==0.0 && holes.Count==1)
            {
                return holes[0];
            }
            else if(shell.Area > 0.0 && holes.Count >0)
            {
                Polyline shellOutline = shell.ToNTSLineString().ToDbPolyline();
                List<Polyline> holeOutlines = new List<Polyline>();
                holes.ForEach(o => holeOutlines.Add(o.ToNTSLineString().ToDbPolyline()));
                return BuildOutermostPolyline(shellOutline, holeOutlines);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private Polyline BuildOutermostPolyline(Polyline shell, List<Polyline> holes)
        {
            Polyline shellOutline = shell.Clone() as Polyline;
            while (holes.Count>0)
            {
                holes = holes.OrderBy(o=>o.Distance(shellOutline)).ToList();
                var first = holes.First();
                holes.Remove(first);
                shellOutline = BuildOutermostPolyline(shellOutline, first, holes);
                if(shellOutline.Area==0.0)
                {
                    return shellOutline;
                }
            }
            return shellOutline;
        }

        private Polyline BuildOutermostPolyline(Polyline shell, Polyline hole,List<Polyline> otherHoles)
        {
            ThPolygonSplitParameter splitParameter = new ThPolygonSplitParameter
            {
                Shell = shell,
                Hole = hole,
                OffsetDistance = PointOffsetDistance,
                OtherHoles = otherHoles
            };
            var instance=ThPolygonSplitPointAnalysis.Split(splitParameter);
            if(!instance.IsFind)
            {
                return new Polyline();
            }
            List<Line> lines = new List<Line>();
            lines.AddRange(GetLines(shell, instance.ShellSegmentSplitPts));
            lines.AddRange(GetLines(hole, instance.HoleSegmentSplitPts));
            lines.Add(new Line(instance.HoleSegmentSplitPts.Item2, instance.ShellSegmentSplitPts.Item2));
            lines.Add(new Line(instance.HoleSegmentSplitPts.Item3, instance.ShellSegmentSplitPts.Item3));
            lines=lines.Where(o => o.Length > 1.0).ToList();
            var mergelines = ThLineMerger.Merge(lines);
            List<Line> extendLines = new List<Line>();
            mergelines.ForEach(o =>
            {
                Point3d sp = o.StartPoint - o.LineDirection().MultiplyBy(LineExtendDistance);
                Point3d ep = o.EndPoint + o.LineDirection().MultiplyBy(LineExtendDistance);
                extendLines.Add(new Line(sp, ep));
            });
            lines.ForEach(o => o.Dispose());            
            DBObjectCollection dbObjs = new DBObjectCollection();
            mergelines.ForEach(o => dbObjs.Add(o));
            var unionObjs = dbObjs.Polygonize();
            List<Polyline> polygonPolyines = new List<Polyline>();
            unionObjs.ForEach(o =>
            {
                if (o is Polygon polygon)
                {
                    polygonPolyines.Add(polygon.Shell.ToDbPolyline());
                }
            });
            return polygonPolyines.Count > 0 ? polygonPolyines.OrderByDescending(o => o.Area).First() : new Polyline();
        }

        private List<Line> GetLines(Polyline polyline , Tuple<LineSegment3d, Point3d, Point3d> segmentItem)
        {
            List<Line> lines = new List<Line>();
            for(int i=0;i<polyline.NumberOfVertices;i++)
            {
                var lineSegment = polyline.GetLineSegmentAt(i);
                if (!segmentItem.Item1.IsEqualTo(lineSegment))
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
    }
}
