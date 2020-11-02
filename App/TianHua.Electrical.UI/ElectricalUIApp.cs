using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }


        public void Initialize()
        {
            SmokeLayoutUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
        }

        [CommandMethod("TIANHUACAD", "THYWG", CommandFlags.Modal)]
        public void THYWG()
        {
            if (SmokeLayoutUI == null)
            {
                SmokeLayoutUI = new fmSmokeLayout();
            }
            AcadApp.ShowModelessDialog(SmokeLayoutUI);
        }
    }
}
