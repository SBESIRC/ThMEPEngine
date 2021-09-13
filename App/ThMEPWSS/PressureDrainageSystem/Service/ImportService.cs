using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.IO;
using ThCADExtension;
using ThMEPEngineCore;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class ImportService
    {
        public ImportService()
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
                    List<string> blockNames = new() { "潜水泵出水管阀组-AI", "潜水泵系统", "套管系统", "标高", "排水管径100", "重力流雨水井编号", "污废合流井编号", "污水井编号" };
                    foreach (var brname in blockNames)
                    {
                        if (!adb.Blocks.Contains(brname)) adb.Blocks.Import(Db.Blocks.ElementOrDefault(brname));
                    }
                    //导入不存在的图层并确认图层unlocked
                    List<string> layerNames = new() { "W-RAIN-PIPE", "W-NOTE", "W-RAIN-Y-PIPE", "W-BUSH", "W-DRAI-DOME-PIPE" };
                    List<short> layerColorIndex = new() { 255, 255, 210, 255, 255 };
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
                    try
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, "AI-辅助", 255);
                    }
                    catch { }
                }
            }
        }
    }
}