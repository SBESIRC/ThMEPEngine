using System;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADExtension
{
    public static class ThExtents3dExtension
    {
        public static AcPolygon ToRectangle(this Extents3d extents)
        {
            var pline = new AcPolygon()
            {
                Closed = true,
            };
            pline.CreateRectangle(extents.MinPoint.ToPoint2d(), extents.MaxPoint.ToPoint2d());
            return pline;
        }

        public static bool Contains(this Extents3d extents, Point3d pt)
        {
            return extents.ToRectangle().ContainsPoint(pt);
        }

        public static double Width(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
        }

        public static double Height(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
        }

        public static Extents3d Flatten(this Extents3d extents)
        {
            // 投影到XY平面
            Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
            return new Extents3d(extents.MinPoint.TransformBy(matrix), extents.MaxPoint.TransformBy(matrix));
        }
    }
}
