using System;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Buffer;
using ThCADExtension;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSLineExtension
    {
        public static bool Overlaps(this Line line, Line other)
        {
            return line.ToNTSLineString().Overlaps(other.ToNTSLineString());
        }

        public static Point3d Intersection(this Line line, Polyline other)
        {
            var geometry = line.ToNTSLineString().Intersection(other.ToNTSLineString());
            if (geometry is Point point)
            {
                return point.ToAcGePoint3d();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Point3d Intersection(this Line line, Line other)
        {
            var geometry = line.ToNTSLineString().Intersection(other.ToNTSLineString());
            if (geometry is Point point)
            {
                return point.ToAcGePoint3d();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool IsOnLine(this Line line, Point3d pt)
        {
            return PointLocation.IsOnLine(pt.ToNTSCoordinate(), line.ToNTSGeometry().Coordinates);
        }

        public static Line Normalize(this Line line)
        {
            var geometry = line.ToNTSLineString();
            geometry.Normalize();
            return geometry.ToDbline();
        }

        public static Coordinate Intersection(this Line line1, Line line2, Intersect intersectType)
        {
            if (line1.IsCollinear(line2))
            {
                return null;
                //throw new NotSupportedException();
            }
            var linesegment1 = new LineSegment(line1.StartPoint.ToNTSCoordinate(), line1.EndPoint.ToNTSCoordinate());
            var linesegment2 = new LineSegment(line2.StartPoint.ToNTSCoordinate(), line2.EndPoint.ToNTSCoordinate());
            switch (intersectType)
            {
                case Intersect.ExtendBoth:
                    var intersectPt = linesegment1.LineIntersection(linesegment2);
                    if (intersectPt == null)
                    {
                        throw new NotSupportedException();
                    }
                    return intersectPt;
                case Intersect.OnBothOperands:
                    var geometry = line1.ToNTSLineString().Intersection(line2.ToNTSLineString());
                    if (geometry is Point point)
                    {
                        return point.Coordinate;
                    }
                    break;
            }
            return null;
        }

        public static Polyline Buffer(this Line line, double distance)
        {
            return line.ToNTSLineString().Buffer(distance, EndCapStyle.Flat).ToDbObjects()[0] as Polyline;
        }

        public static Polyline BufferSquare(this Line line, double distance)
        {
            return line.ToNTSLineString().Buffer(distance, EndCapStyle.Square).ToDbObjects()[0] as Polyline;
        }
    }
}
