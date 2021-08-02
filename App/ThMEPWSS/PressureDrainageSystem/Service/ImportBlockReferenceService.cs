using Linq2Acad;
using System.IO;
using ThCADExtension;
namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class ImportBlockReferenceService
    {
        public ImportBlockReferenceService()
        {
        }
        public void Import()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var file = Path.Combine(ThCADCommon.SupportPath(), "地上给水排水平面图模板.dwg");
                using (AcadDatabase blockDb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                {
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("潜水泵出水管阀组"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("潜水泵系统"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("套管系统"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("标高"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("排水管径100"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("重力流雨水井编号"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("污废合流井编号"));
                    adb.Blocks.Import(blockDb.Blocks.ElementOrDefault("污水井编号"));
                }
            }
        }
    }
}
