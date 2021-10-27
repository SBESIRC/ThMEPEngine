using System;
using AcHelper.Commands;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacOutdoorVentilationCmd : IAcadCommand, IDisposable
    {
        OutdoorParameterSetting loadCalculation;
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new OutdoorParameterSetting();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
