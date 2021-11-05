using System;
using AcHelper.Commands;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacExtractRoomFunctionCmd : IAcadCommand, IDisposable
    {
        ExtractRoomFunction _ui;
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            if (null != _ui && _ui.IsLoaded)
                return;
            _ui = new ExtractRoomFunction();
            AcadApp.ShowModelessWindow(_ui);
        }
    }
}
