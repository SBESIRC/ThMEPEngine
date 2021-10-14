using Linq2Acad;
using ThCADExtension;

namespace ThMEPWSS.UndergroundSpraySystem.Block
{
    public class SprayPipeBlockNames
    {
        public const string SprayNodeMark = "喷淋总管标记";
    }

    public class SprayPipeUtils
    {
        public static void ImportNecessaryBlocks()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(SprayPipeBlockNames.SprayNodeMark));
            }
        }
    }
}