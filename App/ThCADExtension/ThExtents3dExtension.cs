using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThCADExtension
{
    public static class ThExtents3dExtension
    {
        public static Polyline ToRectangle(this Extents3d extents)
        {
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreateRectangle(extents.MinPoint.ToPoint2d(), extents.MaxPoint.ToPoint2d());
            return pline;
        }

        public static double Width(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
        }

        public static double Height(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
        }
    }
}
