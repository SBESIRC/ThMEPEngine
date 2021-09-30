using System;
using AcHelper.Commands;
using TianHua.Hvac.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacXfjCmd : IAcadCommand, IDisposable
    {
        private static uiFanLayoutMainWidget Widget;
        public static uiFanLayoutMainWidget GetWidgetInstance()
        {
            if (Widget == null)
            {
                Widget = new uiFanLayoutMainWidget();
            }
            return Widget;
        }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            AcadApp.ShowModelessWindow(GetWidgetInstance());
        }
    }
}
