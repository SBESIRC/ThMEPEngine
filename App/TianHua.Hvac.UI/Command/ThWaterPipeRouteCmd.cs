using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;

namespace TianHua.Hvac.UI.Command
{
    public class ThWaterPipeRouteCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeRouteCmd()
        {
            ActionName = "绘制水管路由";
            CommandName = "THSGLY";
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            using (var acdb = AcadDatabase.Active())
            {
                ImportLayer();
                CreateLayer();
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.WaterPipeRoute);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }

        private void ImportLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPEngineCoreLayerUtils.WaterPipeRoute), true);
                acadDb.Database.OpenAILayer(ThMEPEngineCoreLayerUtils.WaterPipeRoute);
            }
        }

        private void CreateLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(ThMEPEngineCoreLayerUtils.WaterPipeRoute))
                {
                    acdb.Database.CreateAIWaterPipeRouteLayer();
                }
            }
        }
    }
}
