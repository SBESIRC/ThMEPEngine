using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public class  PlumbingUIApp : IExtensionApplication
    {
        private fmSprinklerLayout SprinklerLayout { get; set; }
        private fmFloorDrain FmFloorDrain { get; set; }
        
        public void Initialize()
        {
            SprinklerLayout = null;
        }

        public void Terminate()
        {
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
    }
}
