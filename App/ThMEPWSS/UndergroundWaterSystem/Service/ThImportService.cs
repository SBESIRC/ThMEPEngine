using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.IO;
using ThCADExtension;
using ThMEPEngineCore;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThImportService
    {
        public ThImportService()
        {

        }
        public void Import()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var file = ThCADCommon.WSSDwgPath();
                using (AcadDatabase Db = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
                {
                    //导入图块
                    List<string> blockNames = new() { "皮带水嘴系统" };
                    foreach (var brname in blockNames)
                    {
                        if (!adb.Blocks.Contains(brname)) adb.Blocks.Import(Db.Blocks.ElementOrDefault(brname));
                    }
                    //导入不存在的图层并确认图层unlocked
                    List<string> layerNames = new() { "W-NOTE", "W-WSUP-EQPM", "W-WSUP-DIMS", "AI-辅助" };
                    List<short> layerColorIndex = new() { 9, 0, 0, 30 };
                    for (int i = 0; i < layerNames.Count; i++)
                    {
                        if (!adb.Layers.Contains(layerNames[i]))
                        {
                            ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, layerNames[i], layerColorIndex[i]);
                        }
                        else
                        {
                            try
                            {
                                DbHelper.EnsureLayerOn(layerNames[i]);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }
}
