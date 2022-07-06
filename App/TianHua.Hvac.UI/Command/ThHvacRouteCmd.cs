using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacRouteCmd : ThMEPBaseCommand, IDisposable
    {
        public ThHvacRouteCmd()
        {
            ActionName = "绘制风管路由";
            CommandName = "THFGLY";
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            using (var acdb = AcadDatabase.Active())
            {
                ImportLayer(); // 优先使用从模板中导入的图层
                CreateLayer(); // 没有所需图层，则创建
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.HAVCRoute);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }

        private void ImportLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPEngineCoreLayerUtils.HAVCRoute), true);
                acadDb.Database.OpenAILayer(ThMEPEngineCoreLayerUtils.HAVCRoute);
            }
        }

        private void CreateLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(ThMEPEngineCoreLayerUtils.HAVCRoute))
                {
                    acdb.Database.CreateAIHAVCRouteLayer();
                }
            }
        }
    }
}
