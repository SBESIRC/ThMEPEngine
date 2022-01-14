using System;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThMEPEntityExtension
    {
        public static Point3dCollection EntityVertices(this Entity entity)
        {
            if (entity is Polyline polyline)
            {
                return polyline.Vertices();
            }
            else if (entity is Line line)
            {
                return line.ToPolyline().Vertices();
            }
            else if (entity is Arc arc)
            {
                return arc.ToPolyline().Vertices();
            }
            else if (entity is MPolygon mPolygon)
            {
                return mPolygon.Vertices();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static double EntityArea(this Entity polygon)
        {
            if (polygon is Polyline polyline)
            {
                return polyline.Area;
            }
            else if (polygon is MPolygon mPolygon)
            {
                return mPolygon.Area;
            }
            else if (polygon is Circle circle)
            {
                return circle.Area;
            }
            else if (polygon is Ellipse ellipse)
            {
                return ellipse.Area;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
