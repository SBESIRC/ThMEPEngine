using System;
using AcHelper.Commands;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacLoadCalculationCmd : IAcadCommand, IDisposable
    {
        LoadCalculationMainUI loadCalculation;
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new LoadCalculationMainUI();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
