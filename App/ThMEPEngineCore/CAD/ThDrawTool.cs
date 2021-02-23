﻿using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThCADExtension;
using System.Linq;
using Dreambuild.AutoCAD;
using GeometryExtensions;

namespace ThMEPEngineCore.CAD
{
    public static class ThDrawTool
    {
        public static Polyline CreatePolyline(this Point3dCollection pts, bool isClosed = true, double lineWidth = 0.0)
        {
            Point2dCollection p2dPts = new Point2dCollection();
            foreach (Point3d pt in pts)
            {
                p2dPts.Add(new Point2d(pt.X, pt.Y));
            }
            return CreatePolyline(p2dPts, isClosed, lineWidth);
        }
        /// <summary>
        /// 创建没有圆弧的多段线
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Polyline CreatePolyline(Point2dCollection pts, bool isClosed = true, double lineWidth = 0)
        {
            Polyline polyline = new Polyline();
            if (pts.Count == 2)
            {
                Point2d minPt = pts[0];
                Point2d maxPt = pts[1];
                Vector2d vec = minPt.GetVectorTo(maxPt);
                if (vec.IsParallelTo(Vector2d.XAxis) || vec.IsParallelTo(Vector2d.YAxis))
                {
                    isClosed = false;
                }
                else
                {
                    double minX = Math.Min(pts[0].X, pts[1].X);
                    double minY = Math.Min(pts[0].Y, pts[1].Y);
                    double maxX = Math.Max(pts[0].X, pts[1].X);
                    double maxY = Math.Max(pts[0].Y, pts[1].Y);
                    pts = new Point2dCollection();
                    pts.Add(new Point2d(minX, minY));
                    pts.Add(new Point2d(maxX, minY));
                    pts.Add(new Point2d(maxX, maxY));
                    pts.Add(new Point2d(minX, maxY));
                }
            }
            for (int i = 0; i < pts.Count; i++)
            {
                polyline.AddVertexAt(i, pts[i], 0, lineWidth, lineWidth);
            }
            if (isClosed)
            {
                polyline.Closed = true;
            }
            return polyline;
        }
        public static Polyline ToOutline(Point3d startPt,Point3d endPt, double width)
        {
            Vector3d vec = startPt.GetVectorTo(endPt).GetNormal();
            Vector3d perpendVec = vec.GetPerpendicularVector();
            startPt = startPt - vec.MultiplyBy(1.0);
            endPt = endPt + vec.MultiplyBy(1.0);
            Point3d pt1 = startPt + perpendVec.MultiplyBy(width + 1.0);
            Point3d pt2 = endPt + perpendVec.MultiplyBy(width + 1.0);
            Point3d pt3 = endPt - perpendVec.MultiplyBy(width + 1.0);
            Point3d pt4 = startPt - perpendVec.MultiplyBy(width + 1.0);
            Polyline outline = new Polyline
            {
                Closed = true
            };
            outline.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, 0, 0);
            outline.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, 0, 0);
            outline.AddVertexAt(2, new Point2d(pt3.X, pt3.Y), 0, 0, 0);
            outline.AddVertexAt(3, new Point2d(pt4.X, pt4.Y), 0, 0, 0);
            return outline;
        }
        public static Polyline ToRectangle(Point3d startPt, Point3d endPt, double width)
        {
            Vector3d vec = startPt.GetVectorTo(endPt).GetNormal();
            Vector3d perpendVec = vec.GetPerpendicularVector();
            Point3d pt1 = startPt + perpendVec.MultiplyBy(width / 2.0);
            Point3d pt2 = endPt + perpendVec.MultiplyBy(width / 2.0);
            Point3d pt3 = endPt - perpendVec.MultiplyBy(width / 2.0);
            Point3d pt4 = startPt - perpendVec.MultiplyBy(width / 2.0);
            Polyline outline = new Polyline
            {
                Closed = true
            };
            outline.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, 0, 0);
            outline.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, 0, 0);
            outline.AddVertexAt(2, new Point2d(pt3.X, pt3.Y), 0, 0, 0);
            outline.AddVertexAt(3, new Point2d(pt4.X, pt4.Y), 0, 0, 0);
            return outline;
        }
        public static Polyline CreateSquare(this Point3d pt, double edgeLength)
        {
            return pt.CreateRectangle(edgeLength, edgeLength);
        }
        public static Polyline CreateRectangle(this Point3d pt, double length,double width)
        {
            Polyline polyline = new Polyline
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(pt.X + length / 2.0, pt.Y + width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(pt.X - length / 2.0, pt.Y + width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(pt.X - length / 2.0, pt.Y - width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(pt.X + length / 2.0, pt.Y - width / 2.0), 0, 0, 0);
            return polyline;
        }
        public static List<Line> GetLines(this DBObjectCollection objs,double length=10.0)
        {
            var lines = new List<Line>();
            objs.Cast<Curve>().ForEach(o =>
            {
                if (o is Line line)
                {
                    lines.Add(line);
                }
                else if (o is Polyline polyline)
                {
                    var subObjs = new DBObjectCollection();
                    polyline.Explode(subObjs);
                    lines.AddRange(subObjs.Cast<Line>().ToList());
                }
                else if(o is Arc arc)
                {
                    var arcPolyline= arc.TessellateArcWithArc(length);
                    var subObjs = new DBObjectCollection();
                    arcPolyline.Explode(subObjs);
                    lines.AddRange(GetLines(subObjs, length));
                }
                else if(o is Circle circle)
                {
                    var circlePolyline = circle.Tessellate(length);
                    var subObjs = new DBObjectCollection();
                    circlePolyline.Explode(subObjs);
                    lines.AddRange(GetLines(subObjs, length));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return lines;
        }
        public static List<Line> ToLines(this Polyline polyline,double length=50.0)
        {
            var results = new List<Line>();
            var newPolyline=polyline.TessellatePolylineWithArc(length);
            var polylineSegments = new PolylineSegmentCollection(newPolyline);
            foreach (var segment in polylineSegments)
            {
                results.Add(new Line(segment.StartPoint.ToPoint3d(), segment.EndPoint.ToPoint3d()));
            }
            return results;
        }
    }
}
