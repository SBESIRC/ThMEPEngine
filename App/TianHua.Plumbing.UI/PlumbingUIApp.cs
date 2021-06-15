using Autodesk.AutoCAD.Runtime;
using System.Windows;
using TianHua.Plumbing.UI.View;
using TianHua.Plumbing.UI.ViewModel;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public class  PlumbingUIApp : IExtensionApplication
    {
        private fmSprinklerLayout SprinklerLayout { get; set; }
        private fmFloorDrain FmFloorDrain { get; set; }
        public FlushPointUI FlushPointUI { get; set; }
        private static FlushPointVM FlushPointVM { get; set; }
        public void Initialize()
        {
            FlushPointUI = null;
            SprinklerLayout = null;
            FlushPointVM = new FlushPointVM();
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
            if(FlushPointUI != null && FlushPointUI.IsLoaded)
            {
                return; 
            }
            FlushPointUI = new FlushPointUI(FlushPointVM);
            FlushPointUI.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AcadApp.ShowModelessWindow(FlushPointUI);
        }
    }
}
