using Autodesk.AutoCAD.Runtime;
using System.Windows;
using TianHua.Electrical.PDS.Command;
using TianHua.Electrical.PDS.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                ElecSandboxUI.InitPDSProjectData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void Terminate()
        {
        }
        [CommandMethod("TIANHUACAD", "THPDSUITest", CommandFlags.Modal)]
        public void THPDSUITest()
        {
            var win = ElecSandboxUI.TryGetSingleWindow();
            if (win == null) return;
            var cmd = new ThPDSCommand();
            cmd.Execute();
            var g = Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            if (g == null) return;
            win.Graph = g;
            AcadApp.ShowModelessWindow(win);
        }
        [CommandMethod("TIANHUACAD", "THPDSTest", CommandFlags.Modal)]
        public void THPDSTest()
        {
            var cmd = new ThPDSCommand();
            cmd.Execute();
        }
    }
}