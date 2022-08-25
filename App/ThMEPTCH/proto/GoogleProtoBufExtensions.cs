using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPTCH.proto
{
    /// <summary>
    /// Google-ProtoBuf 扩展
    /// </summary>
    public static class GoogleProtoBufExtensions
    {
        public static ThTCHPolyline ToTCHPolyline(this Polyline polyline)
        {
            ThTCHPolyline tchPolyline = new ThTCHPolyline();
            if(polyline.IsNull())
                return tchPolyline;
            tchPolyline.Points.Add(polyline.StartPoint.ToTCHPoint());
            var segments = new PolylineSegmentCollection(polyline);
            uint ptIndex = 0;
            for (int k = 0; k < segments.Count; k++)
            {
                var segment = segments[k];
                var tchSegment = new ThTCHSegment();
                tchSegment.Index.Add(ptIndex);
                if(k == segments.Count - 1 && polyline.Closed)
                {
                    tchSegment.Index.Add(0);
                    tchPolyline.Segments.Add(tchSegment);
                }
                else if (segment.IsLinear)
                {
                    // 直线段
                    tchPolyline.Points.Add(segment.EndPoint.ToTCHPoint());
                    tchSegment.Index.Add(++ptIndex);
                    tchPolyline.Segments.Add(tchSegment);
                }
                else
                {
                    // 圆弧段
                    var arc = segment.ToCircularArc();
                    // 圆弧中点
                    double p1 = arc.GetParameterOf(arc.StartPoint);
                    double p2 = arc.GetParameterOf(arc.EndPoint);
                    var midPoint = arc.EvaluatePoint(p1 + (p2 - p1) / 2.0);

                    tchPolyline.Points.Add(midPoint.ToTCHPoint());
                    tchSegment.Index.Add(++ptIndex);

                    tchPolyline.Points.Add(segment.EndPoint.ToTCHPoint());
                    tchSegment.Index.Add(++ptIndex);
                }
            }
            return tchPolyline;
        }

        public static Polyline ToPolyline(this ThTCHPolyline tchPolyline)
        {
            if (tchPolyline.Segments.Count == 0)
            {
                return null;
            }
            Polyline p = new Polyline();
            p.AddVertexAt(0, tchPolyline.Points[int.Parse(tchPolyline.Segments.First().Index.Last().ToString())].ToPoint2d(), 0, 0, 0);
            int index = 1;
            foreach (var segment in tchPolyline.Segments)
            {
                if (segment.Index.Count == 2)
                {
                    p.AddVertexAt(index++, tchPolyline.Points[int.Parse(segment.Index.Last().ToString())].ToPoint2d(), 0, 0, 0);
                }
                else
                {
                    var list = segment.Index.ToList();
                    var arc = ThArcExtension.CreateArcWith3PointsOrder(
                        tchPolyline.Points[int.Parse(list[0].ToString())].ToPoint3d(), 
                        tchPolyline.Points[int.Parse(list[1].ToString())].ToPoint3d(), 
                        tchPolyline.Points[int.Parse(list[2].ToString())].ToPoint3d());
                    p.AddVertexAt(index++, tchPolyline.Points[int.Parse(list[0].ToString())].ToPoint2d(), arc.GetArcBulge(arc.StartPoint), 0, 0);
                }
            }
            return p;
        }

        public static ThTCHPoint3d ToTCHPoint(this Point3d point)
        {
            return new ThTCHPoint3d() { X = point.X, Y = point.Y, Z = point.Z };
        }

        public static ThTCHPoint3d ToTCHPoint(this Point2d point)
        {
            return new ThTCHPoint3d() { X = point.X, Y = point.Y, Z = 0 };
        }

        public static Point2d ToPoint2d(this ThTCHPoint3d point)
        {
            return new Point2d(point.X, point.Y);
        }
        public static Point3d ToPoint3d(this ThTCHPoint3d point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
    }
}
