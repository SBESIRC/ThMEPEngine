using System.Linq;

using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace TianHua.Electrical.PDS.Engine
{
    public static class ThPDSBlcokObbEngine
    {
        public static Polyline BlockOBB(this BlockReference br)
        {
            var blockTransform = br.BlockTransform;
            br.TransformBy(br.BlockTransform.Inverse());
            var entities = new DBObjectCollection();
            br.Explode(entities);
            var filters = entities.OfType<Entity>()
                .Where(e => e is Curve || e is BlockReference)
                .Where(e => e.Visible && e.Bounds.HasValue).ToCollection();
            br.TransformBy(blockTransform);
            if (filters.Count > 0)
            {
                var result = filters.GeometricExtents().ToRectangle();
                result.TransformBy(blockTransform);
                return result;
            }
            return new Polyline();
        }
    }
}
