using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPEngineCore.Diagnostics
{
    public static class ThMEPDebugService
    {
        public static bool IsEnabled()
        {
            return Convert.ToInt16(AcadApp.GetSystemVariable("USERR2")) == 1;
        }
    }
}
