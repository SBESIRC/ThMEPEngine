using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSGeExtension
    {
        // Comment from "Alex Januszkiewicz's Article On AutoCad Accuracy":
        //  But be aware that with almost EVERY geometrical operation performed on an entity, the accuracy is reduced.
        //  When entities are moved, rotated, scaled and stretched etc., complex mathematical transformations are being applied to their geometry.
        //  The results are stored back in the drawing database in AutoCAD with double precision floating point accuracy and in Microstation with 32 bit integer accuracy.
        //  Both math transformation and storage, are REDUCING accuracy of a drawing.
        //  Where an AutoCAD user can safely ignore 3 or 4 significant digit reduction in accuracy on the drawing
        //  that has been modified many times over the years (it still has 12 precise digits),
        //  the same cannot be said about Microstation that has maximum of 10 precise digits and loses 3 of them in complex processing.
        // 使用AutoCAD全局容差（默认值为1E-10)
        private static readonly double SCALE = 1.0 / Tolerance.Global.EqualPoint;
        public static Point3d ToAcGePoint3d(this Point point)
        {
            return point.Coordinate.ToAcGePoint3d();
        }
        public static Point3d ToAcGePoint3d(this Coordinate coordinate)
        {
            return new Point3d(coordinate.X, coordinate.Y, 0);
        }
        public static Point ToNTSPoint(this Point3d point)
        {
            return point.ToPoint2D().ToNTSPoint();
        }
        public static Point ToNTSPoint(this Point2d point)
        {
            return new Point(point.X, point.Y);
        }
        public static Coordinate ToNTSCoordinate(this Point3d point)
        {
            return point.ToPoint2D().ToNTSCoordinate();
        }
        public static Coordinate ToNTSCoordinate(this Point2d point)
        {
            return new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(NTSRounding(point.X)),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(NTSRounding(point.Y)));
        }
        public static Coordinate[] ToNTSCoordinates(this Point3dCollection points)
        {
            var coordinates = new List<Coordinate>();
            foreach (Point3d pt in points)
            {
                coordinates.Add(pt.ToNTSCoordinate());
            }
            return coordinates.ToArray();
        }
        public static Point3dCollection ToAcGePoint3ds(this Coordinate[] coordinates)
        {
            var points = new Point3dCollection();
            foreach (var coordinate in coordinates)
            {
                points.Add(coordinate.ToAcGePoint3d());
            }
            return points;
        }
        private static double NTSRounding(double value)
        {
            /*.Net's default rounding algorithm is "Bankers Rounding" which turned
             * out to be no good for JTS/NTS geometry operations */
            // return Math.Round(val * scale) / scale;

            // This is "Asymmetric Arithmetic Rounding"
            // http://en.wikipedia.org/wiki/Rounding#Round_half_up
            return Math.Floor(value * SCALE + 0.5d) / SCALE;
        }
    }
}
