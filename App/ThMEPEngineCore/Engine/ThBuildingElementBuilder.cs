using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using System.Linq;
using NFox.Cad;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementBuilder
    {
        protected const double AREATOLERANCE = 1.0;
        protected const double BUFFERTOLERANCE = 1.0;
        public List<ThIfcBuildingElement> Elements { get; set; }

        public virtual void Build(Database db, Point3dCollection pts)
        {
            // 提取
            var rawelement = Extract(db);

            // 移动到近原点位置
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            rawelement.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>().Select(o => transformer.Transform(o)).ToCollection();

            // 识别
            Recognize(rawelement, newPts);

            // 恢复到原始位置
            Elements.ForEach(o => transformer.Reset(o.Outline));
        }

        public virtual void Transform(Matrix3d matrix)
        {
            Elements.ForEach(o => o.Outline.TransformBy(matrix));
        }

        public abstract List<ThRawIfcBuildingElementData> Extract(Database db);

        public abstract void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts);
    }
}
