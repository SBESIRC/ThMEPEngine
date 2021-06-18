using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

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
    }
}
