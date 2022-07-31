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
            // Reference:
            // https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/how-to-flatten-a-drawing-in-autocad.html
            if (geos is Line line)
            {
                if (ValidVector(line.Normal))
                {
                    line.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    line.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Polyline polyline)
            {
                if (ValidVector(polyline.Normal))
                {
                    polyline.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    polyline.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Circle circle)
            {
                if (ValidVector(circle.Normal))
                {
                    circle.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    circle.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Arc arc)
            {
                if (ValidVector(arc.Normal))
                {
                    arc.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    arc.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is MPolygon mPolygon)
            {
                if (ValidVector(mPolygon.Normal))
                {
                    mPolygon.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    mPolygon.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is BlockReference block)
            {
                // 由于精度原因，非严格右手系的图元也会进行TransformBy，导致图元异常，故暂不处理
                //if (ValidVector(block.Normal))
                //{
                //    block.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                //    block.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                //    return;
                //}
            }
            else if (geos is DBPoint point)
            {
                if (ValidVector(point.Normal))
                {
                    point.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    point.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is DBText text)
            {
                if (ValidVector(text.Normal))
                {
                    text.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    text.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Dimension dimension)
            {
                if (ValidVector(dimension.Normal))
                {
                    dimension.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    dimension.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Hatch hatch)
            {
                if (ValidVector(hatch.Normal))
                {
                    hatch.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    hatch.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Leader leader)
            {
                if (ValidVector(leader.Normal))
                {
                    leader.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    leader.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is MLeader mLeader)
            {
                if (ValidVector(mLeader.Normal))
                {
                    mLeader.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    mLeader.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is MText mText)
            {
                if (ValidVector(mText.Normal))
                {
                    mText.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    mText.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }
            else if (geos is Solid solid)
            {
                if (ValidVector(solid.Normal))
                {
                    solid.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1E99)));
                    solid.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1E99)));
                    return;
                }
            }

            // 其余类型暂不做变换
        }

        private static bool ValidVector(Vector3d vector)
        {
            var rightSystem = new Vector3d(0, 0, 1);
            var leftSystem = new Vector3d(0, 0, -1);
            return vector.Equals(rightSystem) || vector.Equals(leftSystem);
        }
    }
}
