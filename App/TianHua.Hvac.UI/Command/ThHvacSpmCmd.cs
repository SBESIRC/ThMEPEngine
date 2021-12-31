using System;
using ThMEPEngineCore.Command;
using TianHua.Hvac.UI.UI.FanConnect;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace TianHua.Hvac.UI.Command
{
    public class ThHvacSpmCmd : ThMEPBaseCommand, IDisposable
    {
        private static uiWaterPipeConnectWidget Widget;
        public static uiWaterPipeConnectWidget GetWidgetInstance()
        {
            if (Widget == null)
            {
                Widget = new uiWaterPipeConnectWidget();
            }
            return Widget;
        }
        public ThHvacSpmCmd()
        {
            ActionName = "水平面";
            CommandName = "THSPM";
        }

        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            AcadApp.ShowModelessWindow(GetWidgetInstance());
        }
    }
}
