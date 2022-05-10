using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;

using ThCADExtension;

namespace TianHua.Electrical.PDS.Engine
{
    public static class ThPDSBlcokObbEngine
    {
        public static Polyline BlockOBB(this BlockReference br)
        {
            var entities = new DBObjectCollection();
            ThBlockReferenceExtensions.Burst(br, entities);
            var filters = entities.OfType<Entity>()
                .Where(e => e is Curve || e is BlockReference)
                .Where(e => e.Visible && e.Bounds.HasValue).ToCollection();
            if (filters.Count > 0)
            {
                return filters.GeometricExtents().ToRectangle();
            }
            return new Polyline();
        }
    }
}
