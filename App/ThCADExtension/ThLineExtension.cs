using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThCADExtension
{
    public static class ThLineExtension
    {
        public static Vector3d LineDirection(this Line line)
        {
            return line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }

        public static Line ExtendLine(this Line line, double distance)
        {
            var direction = line.LineDirection();
            return new Line(line.StartPoint - direction * distance, line.EndPoint + direction * distance);
        }

        public static bool IsCollinear(this Line line, Line other)
        {
            var linearEntity = line.GetGeCurve() as LinearEntity3d;
            var linearEntityOther = other.GetGeCurve() as LinearEntity3d;
            if (linearEntity != null && linearEntityOther != null)
            {
                return linearEntity.IsColinearTo(linearEntityOther);
            }
            return false;
        }

        public static bool IsPointOnLine(this Line line, Point3d pt, bool extend, double tolerance)
        {
            var closetPt = line.GetClosestPointTo(pt, extend);
            return closetPt.DistanceTo(pt) < tolerance;
        }

        public static Polyline Tesslate(this Line line,double length)
        {
            var pts = new Point3dCollection();
            var point = line.StartPoint;
            var dir = line.LineDirection();
            pts.Add(point);
            while(true)
            {
                point += dir.MultiplyBy(length);
                if(line.StartPoint.DistanceTo(point)>= line.Length)
                {
                    pts.Add(line.EndPoint);
                    break;
                }
                else
                {
                    pts.Add(point);
                }
            }
            var poly = new Polyline();
            for(int i =0;i<pts.Count;i++)
            {
                poly.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0.0, 0.0, 0.0);
            }
            return poly;
        }

        public static bool LineIsIntersection(this Line line, Line targetLine)
        {
            //严格判断线段是否相交
            double EPS3 = 1.0e-3f;
            var s1 = line.StartPoint;
            var e1 = line.EndPoint;
            var s2 = targetLine.StartPoint;
            var e2 = targetLine.EndPoint;
            var dir = e2 - s2;
            var dValue = (s1 - s2).CrossProduct(dir).Z * (e1 - s2).CrossProduct(dir).Z;
            if (Math.Abs(dValue) > EPS3 && dValue < 0)
            {
                dir = e1 - s1;
                dValue = (s2 - s1).CrossProduct(dir).Z * (e2 - s1).CrossProduct(dir).Z;
                return Math.Abs(dValue) > EPS3 && dValue < 0;
            }
            return false;
        }
    }
}
