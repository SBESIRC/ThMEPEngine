using Autodesk.AutoCAD.Runtime;
using TianHua.Electrical.PDS.Command;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }
        [CommandMethod("TIANHUACAD", "THPDSUITest", CommandFlags.Modal)]
        public void THPDSUITest()
        {
            var uiTest = new ElecSandboxUI();
            AcadApp.ShowModelessWindow(uiTest);
        }
        [CommandMethod("TIANHUACAD", "THPDSTest", CommandFlags.Modal)]
        public void THPDSTest()
        {
            PDSProject center = PDSProject.Instance;
            center.Load("");
            var cmd = new ThPDSCommand();
            cmd.Execute();
        }
    }
}
