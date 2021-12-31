using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
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
                acdb.Database.CreateAIWaterPipeRouteLayer();
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.WaterPipeRoute);
                Active.Document.SendStringToExecute("_Polyline ", true, false, true);
            }
        }
    }
}
