using System;
using DotNetARX;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThPolylineExtension
    {
        public static Polyline ExpandBy(this Polyline rectangle, double deltaX, double deltaY)
        {
            var v0 = rectangle.GetPoint3dAt(0) - rectangle.GetPoint3dAt(3);
            var v1 = rectangle.GetPoint3dAt(0) - rectangle.GetPoint3dAt(1);
            var p0 = rectangle.GetPoint3dAt(0) + (v0.GetNormal() * deltaY + v1.GetNormal() * deltaX);
            var p1 = rectangle.GetPoint3dAt(1) + (v0.GetNormal() * deltaY - v1.GetNormal() * deltaX);
            var p2 = rectangle.GetPoint3dAt(2) - (v0.GetNormal() * deltaY + v1.GetNormal() * deltaX);
            var p3 = rectangle.GetPoint3dAt(3) - (v0.GetNormal() * deltaY - v1.GetNormal() * deltaX);
            return CreateRectangle(p0, p1, p2, p3);
        }

        public static Point3dCollection Vertices(this Polyline pLine)
        {
            //https://keanw.com/2007/04/iterating_throu.html
            Point3dCollection vertices = new Point3dCollection();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                // 暂时不考虑“圆弧”的情况
                vertices.Add(pLine.GetPoint3dAt(i));
            }

            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (pLine.Closed && !vertices[0].Equals(vertices[vertices.Count - 1]))
            {
                vertices.Add(vertices[0]);
            }

            return vertices;
        }

        public static Polyline CreateRectangle(Point3d pt1, Point3d pt2, Point3d pt3, Point3d pt4)
        {
            var points = new Point3dCollection()
            {
                pt1,
                pt2,
                pt3,
                pt4
            };
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(points);
            return pline;
        }

        public static Polyline CreateRectangle(Extents3d extents)
        {
            Point3d pt1 = extents.MinPoint;
            Point3d pt3 = extents.MaxPoint;
            Point3d pt2 = new Point3d(pt3.X, pt1.Y, pt1.Z);
            Point3d pt4 = new Point3d(pt1.X, pt3.Y, pt1.Z);
            return CreateRectangle(pt1,pt2,pt3,pt4);
        }

        public static Vector3d LineDirection(this Line line)
        {
            return line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }

        /// <summary>
        /// 根据弦长分割Polyline中的弧段
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Polyline TessellatePolylineWithChord(this Polyline poly, double chord)
        {
            var polyline = new PolylineSegmentCollection(poly);
            var TessellatePolyline = new PolylineSegmentCollection();
            foreach (var segment in polyline)
            {
                // 分割段是直线
                if (segment.IsLinear)
                {
                    TessellatePolyline.Add(segment);
                }
                // 分割线是弧线
                else
                {
                    var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
                    // 排除弦长大于弧直径的情况
                    if (chord > 2 * circulararc.Radius)
                    {
                        TessellatePolyline.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
                    }
                    else 
                    {
                        var angle = 2 * Math.Asin(chord / (2 * circulararc.Radius));
                        var ArcSegment = segment.DoTessellate(angle);
                        foreach (var item in ArcSegment)
                        {
                            TessellatePolyline.Add(item);
                        }
                    }
                }
            }
            return TessellatePolyline.ToPolyline();
        }

        /// <summary>
        /// 根据弧长分割Polyline中的弧段
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline TessellatePolylineWithArc(this Polyline poly, double length)
        {
            var polyline = new PolylineSegmentCollection(poly);
            var TessellatePolyline = new PolylineSegmentCollection();
            foreach (var segment in polyline)
            {
                // 分割线是直线
                if (segment.IsLinear)
                {
                    TessellatePolyline.Add(segment);
                }
                // 分割线是弧线
                else
                {
                    var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
                    // 排除分割长度大于弧的周长的情况
                    if (length >= 2 * Math.PI * circulararc.Radius)
                    {
                        TessellatePolyline.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
                    }
                    else
                    {
                        var angle = length / circulararc.Radius;
                        var ArcSegment = segment.DoTessellate(angle);
                        foreach (var item in ArcSegment)
                        {
                            TessellatePolyline.Add(item);
                        }
                    }
                }
            }
            return TessellatePolyline.ToPolyline();
        }

        public static Polyline TessellateArcWithChord(this Arc arc, double chord)
        {
            if (chord > 2 * arc.Radius)
            {
                var arcPolyline = new PolylineSegment(arc.StartPoint.ToPoint2d(), arc.EndPoint.ToPoint2d());
                return new PolylineSegmentCollection(arcPolyline).ToPolyline();
            }
            else
            {
                var angle = 2 * Math.Asin(chord / (2 * arc.Radius));
                var length = angle * arc.Radius;
                return arc.TessellateArcWithArc(length);
            }
        }

        public static Polyline TessellateArcWithArc(this Arc arc, double length)
        {
            if (length >= arc.Length)
            {
                var arcPolyline = new PolylineSegment(arc.StartPoint.ToPoint2d(), arc.EndPoint.ToPoint2d());
                return new PolylineSegmentCollection(arcPolyline).ToPolyline();
            }
            else
            {
                var segmentCollection = new PolylineSegmentCollection();
                var angle = length / arc.Radius;
                int num = Convert.ToInt32(Math.Floor(arc.TotalAngle / angle)) + 1;
                for (int i = 1; i <= num; i++)
                {
                    var startAngle = arc.StartAngle + (i - 1) * angle;
                    var endAngle = arc.StartAngle + i * angle;
                    if (i == num)
                    {
                        endAngle = arc.EndAngle;
                    }
                    startAngle = (startAngle > 8 * Math.Atan(1)) ? startAngle - 8 * Math.Atan(1) : startAngle;
                    startAngle = (startAngle < 0.0) ? startAngle + 8 * Math.Atan(1) : startAngle;
                    endAngle = (endAngle > 8 * Math.Atan(1)) ? endAngle - 8 * Math.Atan(1) : endAngle;
                    endAngle = (endAngle < 0.0) ? endAngle + 8 * Math.Atan(1) : endAngle;
                    var arcSegment = new Arc(arc.Center, arc.Radius, startAngle, endAngle);
                    segmentCollection.Add(new PolylineSegment(arcSegment.StartPoint.ToPoint2d(), arcSegment.EndPoint.ToPoint2d()));
                }
                return segmentCollection.ToPolyline();
            }
        }

        /// <summary>
        /// 根据角度分割弧段(保证起始点终止点不变)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static PolylineSegmentCollection DoTessellate(this PolylineSegment segment, double angle)
        {
            var TessellateArc = new PolylineSegmentCollection();
            var circulararc = new CircularArc2d(segment.StartPoint, segment.EndPoint, segment.Bulge, false);
            var angleRange = 4 * Math.Atan(segment.Bulge);
            // 判断弧线是否是顺时针方向
            int IsClockwise = (segment.Bulge < 0.0) ? -1 : 1;
            if (angle >= (angleRange * IsClockwise))
            {
                TessellateArc.Add(new PolylineSegment(segment.StartPoint, segment.EndPoint));
            }
            else
            {
                // 如果方向向量与y轴正方向的角度 小于等于90° 则方向向量在一三象限或x轴上，此时方向向量与x轴的角度不需要变化，否则需要 2PI - 与x轴角度
                double StartAng = (circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.StartPoint).GetAngleTo(new Vector2d(1.0, 0.0)));

                double EndAng = (circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(0.0, 1.0)) <= Math.PI / 2.0) ?
                    circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)) :
                    (Math.PI * 2.0 - circulararc.Center.GetVectorTo(segment.EndPoint).GetAngleTo(new Vector2d(1.0, 0.0)));
                int num = Convert.ToInt32(Math.Floor(angleRange * IsClockwise / angle)) + 1;

                for (int i = 1; i <= num; i++)
                {
                    var startAngle = StartAng + (i - 1) * angle * IsClockwise;
                    var endAngle = StartAng + i * angle * IsClockwise;
                    if (i == num)
                    {
                        endAngle = EndAng;
                    }
                    startAngle = (startAngle > 8 * Math.Atan(1)) ? startAngle - 8 * Math.Atan(1) : startAngle;
                    startAngle = (startAngle < 0.0) ? startAngle + 8 * Math.Atan(1) : startAngle;
                    endAngle = (endAngle > 8 * Math.Atan(1)) ? endAngle - 8 * Math.Atan(1) : endAngle;
                    endAngle = (endAngle < 0.0) ? endAngle + 8 * Math.Atan(1) : endAngle;
                    // Arc的构建方向是逆时针的，所以如果是顺时针的弧段，需要反向构建
                    if (segment.Bulge < 0.0)
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, endAngle, startAngle);
                        TessellateArc.Add(new PolylineSegment(arc.EndPoint.ToPoint2d(), arc.StartPoint.ToPoint2d()));
                    }
                    else
                    {
                        var arc = new Arc(circulararc.Center.ToPoint3d(), circulararc.Radius, startAngle, endAngle);
                        TessellateArc.Add(new PolylineSegment(arc.StartPoint.ToPoint2d(), arc.EndPoint.ToPoint2d()));
                    }
                }
            }
            return TessellateArc;
        }
    }
}


