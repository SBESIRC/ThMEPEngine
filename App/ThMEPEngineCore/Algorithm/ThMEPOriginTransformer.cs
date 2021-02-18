using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using Linq2Acad;

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

        public void Reset(DBObjectCollection objs)
        {
            var matrix = Displacement.Inverse();
            TransformBy(objs, matrix);
        }

        private void TransformBy(DBObjectCollection objs, Matrix3d matrix)
        {
            objs.Cast<Entity>().ForEachDbObject(e => e.TransformBy(matrix));
        }
    }
}
