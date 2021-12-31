using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
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
                acdb.Database.CreateAIHAVCRouteLayer();
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.HAVCRoute);
                Active.Document.SendStringToExecute("_Polyline ", true, false, true);
            }
        }
    }
}
