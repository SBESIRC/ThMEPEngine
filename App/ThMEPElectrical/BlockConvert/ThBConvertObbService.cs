using System.Linq;

using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertObbService
    {
        public static Polyline BlockObb(AcadDatabase acadDatabase, ObjectId objectId, double scale)
        {
            if (!objectId.Equals(ObjectId.Null))
            {
                var block = acadDatabase.Element<BlockReference>(objectId, true);
                var name = objectId.GetBlockName();
                if (ThBConvertLabelBlockList.BlockList.Contains(name))
                {
                    return block.Position.CreateSquare(250.0 * scale);
                }
                else
                {
                    return block.ToOBB(block.BlockTransform);
                }
            }
            return new Polyline();
        }

        public static Polyline BlockLabelObb(AcadDatabase acadDatabase, ObjectId objectId, double scale)
        {
            if (!objectId.Equals(ObjectId.Null))
            {
                var block = acadDatabase.Element<BlockReference>(objectId, true);
                var objs = new DBObjectCollection();
                block.Explode(objs);
                var labels = objs.OfType<Line>().Where(o => o.Layer.Equals(ThBConvertCommon.BLOCK_LOAD_DIMENSION_LAYER)).ToList();
                labels.ForEach(o => o.TransformBy(block.BlockTransform.Inverse()));
                var rectangle = labels.Where(o => o.DistanceTo(Point3d.Origin, false) > 5.0).ToCollection().GeometricExtents().ToRectangle();
                return rectangle.GetTransformedRectangle(block.BlockTransform).FlattenRectangle();
            }
            return new Polyline();
        }
    }
}
