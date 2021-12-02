using System;
using ThMEPEngineCore.Command;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacOutdoorVentilationCmd : ThMEPBaseCommand, IDisposable
    {
        OutdoorParameterSetting loadCalculation;

        public ThHvacOutdoorVentilationCmd()
        {
            CommandName = "THSWSZ";
            ActionName = "室外参数设置";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new OutdoorParameterSetting();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
