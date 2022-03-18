using System.Windows;
using Autodesk.AutoCAD.Runtime;
using TianHua.Electrical.PDS.UI.UI;
using TianHua.Electrical.PDS.Command;
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

        /// <summary>
        /// 电力系统
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDLXT", CommandFlags.Modal)]
        public void THDLXT()
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

        /// <summary>
        /// 电力系统（内部使用）
        /// </summary>
        [CommandMethod("TIANHUACAD", "THPDS", CommandFlags.Modal)]
        public void THPDS()
        {
            var cmd = new ThPDSCommand();
            cmd.Execute();
        }
    }
}