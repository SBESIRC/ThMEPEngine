using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Hvac.UI.UI.FanConnect;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace TianHua.Hvac.UI.Command
{
    public class ThHvacSpmCmd : IAcadCommand, IDisposable
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
        public void Dispose()
        {
        }
        public void Execute()
        {
            AcadApp.ShowModelessWindow(GetWidgetInstance());
        }
    }
}
