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

        public static bool EntityContains(this Entity ent, Point3d pt)
        {
            if (ent is Polyline polyline)
            {
                return polyline.Contains(pt);
            }
            else if (ent is MPolygon mPolygon)
            {
                return mPolygon.Contains(pt);
            }
            else if (ent is Circle circle)
            {
                return pt.DistanceTo(circle.Center) < circle.Radius;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool EntityContains(this Entity A, Entity B)
        {
            if (A is Polyline firstPoly)
            {
                return Contains(firstPoly, B);
            }
            else if (A is MPolygon mPolygon)
            {
                return Contains(mPolygon, B);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static bool Contains(Polyline poly, Entity entity)
        {
            if (entity is Curve curve)
            {
                return poly.Contains(curve);
            }
            else if (entity is MPolygon mPolygon)
            {
                return poly.Contains(mPolygon);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static bool Contains(MPolygon mPolygon, Entity entity)
        {
            if (entity is Curve curve)
            {
                return mPolygon.Contains(curve);
            }
            else if (entity is MPolygon mPolygon2)
            {
                return mPolygon.Contains(mPolygon2);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static void ProjectOntoXYPlane(this Entity geos)
        {
            if (geos != null)
            {
                // Reference:
                // https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/how-to-flatten-a-drawing-in-autocad.html
                geos.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                geos.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
            }
        }
    }
}
