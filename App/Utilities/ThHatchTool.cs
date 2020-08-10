using System;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ThHatchTool
    {
        // https://adndevblog.typepad.com/autocad/2012/04/perimeter-of-a-hatch-using-objectarx-and-autocad-net-api.html
        public static List<Polyline> ToPolylines(this Hatch hatch)
        {
            var plines = new List<Polyline>();
            for (int index = 0; index < hatch.NumberOfLoops; index++)
            {
                var hatchLoop = hatch.GetLoopAt(index);
                if (hatchLoop.IsPolyline)
                {
                    var vertices = hatchLoop.Polyline;
                    var pline = new Polyline(vertices.Count);
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        pline.AddVertexAt(i, vertices[i].Vertex, vertices[i].Bulge, 0, 0);
                    }
                    plines.Add(pline);
                }
                else
                {
                    Plane plane = hatch.GetPlane();
                    var segments = new PolylineSegmentCollection();
                    foreach (Curve2d cv in hatchLoop.Curves)
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
                    segments.Join();
                    plines.Add(segments.ToPolyline());
                }
            }
            return plines;
        }
    }
}
