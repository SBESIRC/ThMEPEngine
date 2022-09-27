using System;
using System.Linq;
using ThCADExtension;
using Google.Protobuf;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public enum ProtoBufDataType
    {
        None = 0,
        PushType = 1,
        ZoomType = 2,
        ExternalLink = 3,
    }

    public enum PlatformType
    {
        None = 0,
        CADPlatform = 1,
        SUPlatform = 2,
    }

    public static class ThProtoBufExtension
    {
        public static ThTCHCircle ToTCHCircle(this Circle circle)
        {
            ThTCHCircle tchCircle = new ThTCHCircle();
            if (circle.IsNull())
                return tchCircle;
            tchCircle.Center = circle.Center.ToTCHPoint();
            tchCircle.Radius = circle.Radius;
            return tchCircle;
        }

        public static ThTCHLine ToTCHLine(this Line line)
        {
            ThTCHLine tchLine = new ThTCHLine();
            if (tchLine.IsNull())
                return tchLine;
            tchLine.StartPt = line.StartPoint.ToTCHPoint();
            tchLine.EndPt = line.EndPoint.ToTCHPoint();
            return tchLine;
        }

        public static ThTCHPolyline ToTCHPolyline(this Line line)
        {
            ThTCHPolyline tchPolyline = new ThTCHPolyline();
            if (line.IsNull())
                return tchPolyline;
            tchPolyline.IsClosed = false;
            tchPolyline.Points.Add(line.StartPoint.ToTCHPoint());
            tchPolyline.Points.Add(line.EndPoint.ToTCHPoint());
            var tchSegment = new ThTCHSegment();
            tchSegment.Index.Add(0);
            tchSegment.Index.Add(1);
            tchPolyline.Segments.Add(tchSegment);
            return tchPolyline;
        }

        public static ThTCHMPolygon ToTCHMPolygon(this Polyline polyline)
        {
            ThTCHMPolygon MPolygon = new ThTCHMPolygon();
            MPolygon.Shell = polyline.ToTCHPolyline();
            return MPolygon;
        }

        public static ThTCHMPolygon ToTCHMPolygon(this MPolygon mPolygon)
        {
            ThTCHMPolygon tchMPolygon = new ThTCHMPolygon();
            tchMPolygon.Shell = mPolygon.Shell().ToTCHPolyline();
            mPolygon.Holes().ForEach(o => tchMPolygon.Holes.Add(o.ToTCHPolyline()));
            return tchMPolygon;
        }

        public static ThTCHPolyline ToTCHPolyline(this Polyline polyline)
        {
            ThTCHPolyline tchPolyline = new ThTCHPolyline();
            if (polyline.IsNull())
                return tchPolyline;
            tchPolyline.IsClosed = polyline.Closed;

            tchPolyline.Points.Add(polyline.StartPoint.ToTCHPoint());
            var segments = new PolylineSegmentCollection(polyline);
            uint ptIndex = 0;
            for (int k = 0; k < segments.Count; k++)
            {
                var segment = segments[k];
                if (segment.StartPoint.GetDistanceTo(segment.EndPoint) > 10)
                {
                    var tchSegment = new ThTCHSegment();
                    tchSegment.Index.Add(ptIndex);
                    if (k == segments.Count - 1 && polyline.Closed)
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
            }
            return tchPolyline;
        }
        public static Polyline ToPolyline(this ThTCHMPolygon tchMPolygon)
        {
            if(!tchMPolygon.IsNull() && (tchMPolygon.Holes.IsNull() || tchMPolygon.Holes.Count == 0))
            {
                return tchMPolygon.Shell.ToPolyline();
            }
            return null;
        }

        public static Polyline ToPolyline(this ThTCHPolyline tchPolyline)
        {
            if (tchPolyline.Segments.Count == 0)
            {
                return null;
            }
            Polyline p = new Polyline();
            p.AddVertexAt(0, tchPolyline.Points[int.Parse(tchPolyline.Segments.First().Index.First().ToString())].ToPoint2d(), 0, 0, 0);
            int index = 1;
            for (int i = 0; i < tchPolyline.Segments.Count; i++)
            {
                var segment = tchPolyline.Segments[i];
                if (i == tchPolyline.Segments.Count - 1 && tchPolyline.IsClosed)
                {
                    //do not
                }
                else if (segment.Index.Count == 2)
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
            p.Closed = tchPolyline.IsClosed;
            return p;
        }

        public static void ZOffSet(this ThTCHMPolygon tchMPolygon, double offset)
        {
            tchMPolygon.Shell.ZOffSet(offset);
            tchMPolygon.Holes.ForEach(o => o.ZOffSet(offset));
        }

        public static void ZOffSet(this ThTCHPolyline tchPolyline, double offset)
        {
            if (tchPolyline.IsNull())
            {
                return;
            }
            tchPolyline.Points.ForEach(p => p.Z += offset);
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

        public static ThTCHVector3d ToTCHVector(this Vector3d vector)
        {
            return new ThTCHVector3d() { X = vector.X, Y = vector.Y, Z = vector.Z };
        }

        public static ThTCHMatrix3d ToTCHMatrix3d(this Matrix3d matrix)
        {
            return new ThTCHMatrix3d()
            {
                Data11 = matrix[0, 0],
                Data12 = matrix[0, 1],
                Data13 = matrix[0, 2],
                Data14 = matrix[0, 3],
                Data21 = matrix[1, 0],
                Data22 = matrix[1, 1],
                Data23 = matrix[1, 2],
                Data24 = matrix[1, 3],
                Data31 = matrix[2, 0],
                Data32 = matrix[2, 1],
                Data33 = matrix[2, 2],
                Data34 = matrix[2, 3],
                Data41 = matrix[3, 0],
                Data42 = matrix[3, 1],
                Data43 = matrix[3, 2],
                Data44 = matrix[3, 3],
            };
        }

        public static byte[] ToThBimData(this ThTCHProjectData project, ProtoBufDataType protoBufType, PlatformType platformType)
        {
            if (protoBufType == ProtoBufDataType.None || platformType == PlatformType.None)
            {
                throw new NotSupportedException();
            }
            var projectBytes = project.ToByteArray();
            var ThBimData = new byte[projectBytes.Length + 10];
            ThBimData[0] = 84;
            ThBimData[1] = 72;
            switch (protoBufType)
            {
                case ProtoBufDataType.PushType:
                    {
                        ThBimData[2] = 1;
                        break;
                    }
                case ProtoBufDataType.ZoomType:
                    {
                        ThBimData[2] = 2;
                        break;
                    }
                case ProtoBufDataType.ExternalLink:
                    {
                        ThBimData[2] = 3;
                        break;
                    }
                    default: throw new NotSupportedException();
            }
            switch (platformType)
            {
                case PlatformType.CADPlatform:
                    {
                        ThBimData[3] = 1;
                        break;
                    }
                case PlatformType.SUPlatform:
                    {
                        ThBimData[3] = 2;
                        break;
                    }
                default: throw new NotSupportedException();
            }
            projectBytes.CopyTo(ThBimData, 10);
            return ThBimData;
        }
    }
}
