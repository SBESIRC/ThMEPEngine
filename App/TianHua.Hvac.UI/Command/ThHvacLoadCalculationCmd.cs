using System;
using ThMEPEngineCore.Command;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacLoadCalculationCmd : ThMEPBaseCommand, IDisposable
    {
        LoadCalculationMainUI loadCalculation;

        public ThHvacLoadCalculationCmd()
        {
            CommandName = "THFHJS";
            ActionName = "负荷通风计算";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new LoadCalculationMainUI();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
