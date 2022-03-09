using System;
using ThMEPEngineCore.Command;
using TianHua.Electrical.UI.ElectricalLoadCalculation;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI.Command
{
    public class ThElectricalLoadCalculationUICmd : ThMEPBaseCommand, IDisposable
    {
        ElectricalLoadCalculationUI loadCalculation;

        public ThElectricalLoadCalculationUICmd()
        {
            CommandName = "THYDFHJS";
            ActionName = "用电负荷计算";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new ElectricalLoadCalculationUI();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
