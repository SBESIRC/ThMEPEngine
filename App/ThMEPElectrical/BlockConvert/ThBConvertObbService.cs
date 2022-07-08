using System.Linq;

using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertObbService
    {
        public static Polyline BlockObb(AcadDatabase acadDatabase, ObjectId objectId)
        {
            if (!objectId.Equals(ObjectId.Null))
            {
                var block = acadDatabase.Element<BlockReference>(objectId, true);
                var name = objectId.GetBlockName();
                if (name.KeepChinese().Equals(ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION))
                {
                    var objs = new DBObjectCollection();
                    block.Explode(objs);
                    var motor = objs.OfType<BlockReference>().First();
                    return motor.ToOBB();
                }
                else if (name.KeepChinese().Equals(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                {
                    return block.Position.CreateSquare(100.0);
                }
                else
                {
                    return block.ToOBB();
                }
            }
            return new Polyline();
        }
    }
}
