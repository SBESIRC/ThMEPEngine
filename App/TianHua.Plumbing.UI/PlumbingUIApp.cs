using Autodesk.AutoCAD.Runtime;
using System.Windows;
using TianHua.Plumbing.UI.View;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public class  PlumbingUIApp : IExtensionApplication
    {
        private fmSprinklerLayout SprinklerLayout { get; set; }
        private fmFloorDrain FmFloorDrain { get; set; }
        private FlushPointUI FlushPointUI { get; set; }
        public void Initialize()
        {
            FlushPointUI = null;
            SprinklerLayout = null;
        }

        public void Terminate()
        {
            FlushPointUI = null;
            SprinklerLayout = null;
        }

        [CommandMethod("TIANHUACAD", "THPL", CommandFlags.Modal)]
        public void ThWSSUI()
        {
            if (SprinklerLayout == null)
            {
                SprinklerLayout = new fmSprinklerLayout();
            }
            AcadApp.ShowModelessDialog(SprinklerLayout);
        }


        [CommandMethod("TIANHUACAD", "THPYS", CommandFlags.Modal)]
        public void THPYS()
        {
            if (FmFloorDrain == null)
            {
                FmFloorDrain = new fmFloorDrain();
            }
            AcadApp.ShowModelessDialog(FmFloorDrain);
        }

        [CommandMethod("TIANHUACAD", "THDXCX", CommandFlags.Modal)]

        public void THDXCX()
        {
            if (FlushPointUI == null)
            {
                FlushPointUI = new FlushPointUI();
            }

            FlushPointUI.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(FlushPointUI);
        }
    }
}
