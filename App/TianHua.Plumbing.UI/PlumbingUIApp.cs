using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.UI
{
    public class  PlumbingUIApp : IExtensionApplication
    {
        private fmSprinklerLayout SprinklerLayout { get; set; }
        
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
    }
}
