using Linq2Acad;
using System.Windows;
using TianHua.Plumbing.UI.View;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Water.UI
{
    public class ThWaterUIApp:IExtensionApplication
    {
        FlushPointUI flushPointUI = null;
        public void Initialize()
        {
            flushPointUI = null;
        }

        public void Terminate()
        {
            flushPointUI = null;
        }

        [CommandMethod("TIANHUACAD", "THDXCX", CommandFlags.Modal)]

        public void THCXDW()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if (flushPointUI != null && flushPointUI.IsLoaded)
                {
                    return;
                }

                flushPointUI = new FlushPointUI();
                flushPointUI.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                AcadApp.ShowModelessWindow(flushPointUI);
            }
        }
    }
}
