using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Extension
{
    public static class ThGeometryExtension
    {
        public static void ProjectOntoXYPlane(this List<ThGeometry> geos)
        {
            var objs = new DBObjectCollection();
            Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
            geos.ForEach(g =>
            {
                if (g.Boundary != null)
                {
                    if (g.Boundary is Polyline polyline)
                    {
                        g.Boundary.TransformBy(matrix);
                    }
                    else if (g.Boundary is MPolygon mPolygon)
                    {
                        g.Boundary.TransformBy(matrix);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            });
        }
    }
}
