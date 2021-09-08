using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NFox.Cad;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPOriginTransformer
    {
        public Matrix3d Displacement { get; set; }

        public ThMEPOriginTransformer(Point3d center)
        {
            var vector = center.GetVectorTo(Point3d.Origin);
            Displacement = Matrix3d.Displacement(vector);
        }

        public ThMEPOriginTransformer(DBObjectCollection objs)
        {
            var center = objs.GeometricExtents().CenterPoint();
            var vector = center.GetVectorTo(Point3d.Origin);
            Displacement = Matrix3d.Displacement(vector);
        }

        public ThMEPOriginTransformer()
        {
        }

        public void Transform(Entity entity)
        {
            var matrix = Displacement;
            TransformBy(new DBObjectCollection() { entity }, matrix);
        }

        public void Transform(DBObjectCollection objs)
        {
            var matrix = Displacement;
            TransformBy(objs, matrix);
        }

        public Point3d Transform(Point3d point)
        {
            return point.TransformBy(Displacement);
        }

        public void Transform(ref Point3d point)
        {
            var matrix = Displacement;
            TransformBy(ref point, matrix);
        }
        public Point3dCollection Transform(Point3dCollection pts)
        {
            return pts.OfType<Point3d>().Select(o => Transform(o)).ToCollection();
        }

        public void Reset(Entity entity)
        {
            var matrix = Displacement.Inverse();
            TransformBy(new DBObjectCollection() { entity }, matrix);
        }
        public void Reset(DBObjectCollection objs)
        {
            var matrix = Displacement.Inverse();
            TransformBy(objs, matrix);
        }

        public void Reset(ref Point3d point)
        {
            var matrix = Displacement.Inverse();
            TransformBy(ref point, matrix);
        }

        public Point3d Reset(Point3d point)
        {
            var matrix = Displacement.Inverse();
            return point.TransformBy(matrix);
        }

        private void TransformBy(DBObjectCollection objs, Matrix3d matrix)
        {
            objs.Cast<Entity>().ForEachDbObject(e => e.TransformBy(matrix));
        }

        private void TransformBy(ref Point3d point, Matrix3d matrix)
        {
            point = point.TransformBy(matrix);
        }

    }
}
