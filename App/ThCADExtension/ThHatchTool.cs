using System;
using NFox.Cad;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThHatchTool
    {
        // https://forums.autodesk.com/t5/net/restore-hatch-boundaries-if-they-have-been-lost-with-net/td-p/3779514
        // https://adndevblog.typepad.com/autocad/2012/04/perimeter-of-a-hatch-using-objectarx-and-autocad-net-api.html
        public static List<Curve> Boundaries(this Hatch hatch, double tolerance = 1e-4)
        {
            var curves = new List<Curve>();
            if (hatch == null)
            {
                return curves;
            }
            Plane plane = hatch.GetPlane();
            for (int index = 0; index < hatch.NumberOfLoops; index++)
            {
                var hatchLoop = hatch.GetLoopAt(index);
                if (hatchLoop.IsPolyline)
                {
                    var vertices = hatchLoop.Polyline;
                    var pline = new Polyline(vertices.Count)
                    {
                        Closed = true,
                    };
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        pline.AddVertexAt(i, vertices[i].Vertex, vertices[i].Bulge, 0, 0);
                    }
                    curves.Add(pline);
                }
                else
                {
                    // 单独处理圆的情况
                    if (hatchLoop.Curves.Count == 1
                        && hatchLoop.Curves[0].IsClosed()
                        && hatchLoop.Curves[0] is CircularArc2d circularArc)
                    {
                        curves.Add(circularArc.ToCircle());
                        continue;
                    }

                    // 暂时只处理线和圆弧组合的情况
                    if (hatchLoop.Curves.Count == 2)
                    {
                        var circle = ToCircle(hatchLoop.Curves);
                        if (circle.Area <= 1e-6)
                        {
                            var poly = ToPolyline(hatchLoop.Curves, plane, tolerance);
                            if(poly!=null)
                            {
                                curves.Add(poly);
                            }
                        }
                        else
                        {
                            curves.Add(circle);
                        }
                    }
                    else
                    {
                        var poly = ToPolyline(hatchLoop.Curves, plane, tolerance);
                        if(poly!=null)
                        {
                            curves.Add(poly);
                        }
                        
                    }
                }
            }
            return curves;
        }
        private static Circle ToCircle(Curve2dCollection curve2ds)
        {
            var first = curve2ds[0];
            var second = curve2ds[1];
            if (first is CircularArc2d firstArc && second is CircularArc2d secondArc)
            {
                if (firstArc.StartPoint.GetDistanceTo(secondArc.StartPoint) <= 1.0 &&
                    firstArc.EndPoint.GetDistanceTo(secondArc.EndPoint) <= 1.0)
                {
                    return new Circle(new Point3d(firstArc.Center.X, firstArc.Center.Y, 0), Vector3d.ZAxis, firstArc.Radius);
                }
                else if (firstArc.StartPoint.GetDistanceTo(secondArc.EndPoint) <= 1.0 &&
                    firstArc.EndPoint.GetDistanceTo(secondArc.StartPoint) <= 1.0)
                {
                    return new Circle(new Point3d(firstArc.Center.X, firstArc.Center.Y, 0), Vector3d.ZAxis, firstArc.Radius);
                }
                else
                {
                    return new Circle();
                }
            }
            else
            {
                return new Circle();
            }
        }
        private static Polyline ToPolyline(Curve2dCollection curve2ds, Plane plane,  double tolerance = 1e-4)
        {
            var segments = new PolylineSegmentCollection();
            foreach (Curve2d cv in curve2ds)
            {
                LineSegment2d line2d = cv as LineSegment2d;
                CircularArc2d arc2d = cv as CircularArc2d;
                EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
                NurbCurve2d spline2d = cv as NurbCurve2d;
                if (line2d != null)
                {
                    var startPoint = new Point3d(plane, line2d.StartPoint);
                    var endPoint = new Point3d(plane, line2d.EndPoint);
                    segments.Add(new PolylineSegment(startPoint.ToPoint2D(), endPoint.ToPoint2D()));
                }
                else if (arc2d != null)
                {
                    segments.Add(new PolylineSegment(arc2d));
                }
                else
                {
                    // 暂时不支持由椭圆弧和样条曲线围合的面域
                    segments = new PolylineSegmentCollection();
                    break;
                }               
                //else if (spline2d != null)
                //{
                //    var poly = spline2d.ToCurve().ToPolyline() as Polyline;
                //    segments.AddRange(new PolylineSegmentCollection(poly));
                //}
                //else
                //{
                //    throw new NotSupportedException();
                //}
            }
            // TODO:
            //  存在一种“特殊”的Loop，为了用一个Loop来表达一个带洞的区域，
            //  在带洞区域的外轮廓线和内轮廓线之间拉一根线，使其能被“一笔画”
            //  需要将这种情况处理成正常的带洞区域            
            if(segments.Count>0)
            {
                segments.Join();
                var newPoly = segments.ToPolyline();
                if (newPoly.StartPoint.IsEqualTo(newPoly.EndPoint, new Tolerance(tolerance, tolerance)))
                {
                    newPoly.Closed = true;
                }
                return newPoly;
            }
            else
            {
                return null;
            }            
        }
    }
}
