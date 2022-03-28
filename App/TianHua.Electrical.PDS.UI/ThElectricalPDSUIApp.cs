using AcHelper;
using Autodesk.AutoCAD.Runtime;
using System.Linq;
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
            var graph = Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            if (graph == null) return;
            var vertices = graph.Vertices.ToList();
            for (var i = 0; i < vertices.Count && i < 10; i++)
            {
                var drawCmd = new ThPDSSystemDiagramCommand(graph, vertices[i]);
                drawCmd.Execute();
            }
            Active.Editor.Regen();
        }
    }
}