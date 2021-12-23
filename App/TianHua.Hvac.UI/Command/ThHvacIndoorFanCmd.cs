using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using TianHua.Hvac.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    class ThHvacIndoorFanCmd : ThMEPBaseCommand, IDisposable
    {
        public ThHvacIndoorFanCmd() 
        {
            CommandName = "THSNJ";
            ActionName = "THSNJ";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            uiIndoorFan indoorFan = new uiIndoorFan();
            AcadApp.ShowModelessWindow(indoorFan);
        }
    }
}
