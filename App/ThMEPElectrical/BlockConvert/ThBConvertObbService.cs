using DotNetARX;
using Linq2Acad;
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
    }
}
