using System;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ThHatchTool
    {
        public static List<Polyline> ToPolylines(this Hatch hatch)
        {
            var plines = new List<Polyline>();
            for (int index = 0; index < hatch.NumberOfLoops; index++)
            {
                if (hatch.GetLoopAt(index).IsPolyline)
                {
                    var p = new Polyline()
                    {
                        Closed = true,
                    };
                    var loop = hatch.GetLoopAt(index).Polyline;
                    for (int i = 0; i < loop.Count -1; i++)
                    {
                        p.AddVertexAt(i, loop[i].Vertex, loop[i].Bulge, 0, 0);
                    }
                    plines.Add(p);
                }
                else
                {
                    var segments = new PolylineSegmentCollection();
                    for (int i = 0; i < hatch.NumberOfLoops; i++)
                    {
                        Plane plane = hatch.GetPlane();
                        HatchLoop loop = hatch.GetLoopAt(i);
                        foreach (Curve2d cv in loop.Curves)
                        {
                            LineSegment2d line2d = cv as LineSegment2d;
                            CircularArc2d arc2d = cv as CircularArc2d;
                            EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
                            NurbCurve2d spline2d = cv as NurbCurve2d;
                            if (line2d != null)
                            {
                                segments.Add(new PolylineSegment(line2d));
                            }
                            else if (arc2d != null)
                            {
                                segments.Add(new PolylineSegment(arc2d));
                            }
                            else if (ellipse2d != null)
                            {
                                NurbCurve2d nurbCurve = new NurbCurve2d(ellipse2d);
                                segments.AddRange(nurbCurve.ToPolylineSegments(plane));
                            }
                            else if (spline2d != null)
                            {
                                segments.AddRange(spline2d.ToPolylineSegments(plane));
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }
                    segments.Join();
                    plines.Add(segments.ToPolyline());
                }
            }
            return plines;
        }
    }
}
