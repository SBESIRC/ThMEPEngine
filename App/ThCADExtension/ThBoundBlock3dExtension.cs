using System;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThBoundBlock3dExtension
    {
        public static Polyline ToPolyline(this BoundBlock3d boundBlock)
        {
            if (boundBlock.IsBox)
            {
                var vertices = new Point3dCollection()
                {
                    boundBlock.BasePoint,
                    boundBlock.BasePoint + boundBlock.Direction1,
                    boundBlock.BasePoint + boundBlock.Direction1 + boundBlock.Direction2,
                    boundBlock.BasePoint + boundBlock.Direction2,
                };
                var poly = new Polyline()
                {
                    Closed = true,
                };
                poly.CreatePolyline(vertices);
                return poly;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
