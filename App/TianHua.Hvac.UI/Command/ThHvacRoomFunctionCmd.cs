using System;
using AcHelper.Commands;
using TianHua.Hvac.UI.LoadCalculation.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacRoomFunctionCmd : IAcadCommand, IDisposable
    {
        RoomNumber loadCalculation;
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            if (null != loadCalculation && loadCalculation.IsLoaded)
                return;
            loadCalculation = new RoomNumber();
            AcadApp.ShowModelessWindow(loadCalculation);
        }
    }
}
