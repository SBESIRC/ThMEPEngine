using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPOriginTransformer
    {
        private Matrix3d Displacement { get; set; }

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

        public void Transform(ref Point3d point)
        {
            var matrix = Displacement;
            TransformBy(ref point, matrix);
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
