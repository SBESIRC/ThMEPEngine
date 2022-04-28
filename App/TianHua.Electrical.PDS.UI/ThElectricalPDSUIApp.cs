using AcHelper;
using Autodesk.AutoCAD.Runtime;
using System.Windows;
using TianHua.Electrical.PDS.Command;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.UI.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        public void Initialize()
        {

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
            try
            {
                ElecSandboxUI.InitPDSProjectData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            var win = ElecSandboxUI.TryGetCurrentWindow();
            if (win is not null) return;
            win = ElecSandboxUI.TryCreateSingleton();
            if (win == null) return;
            int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            win.Loaded += (s, e) =>
            {
                win.Width = w - 200;
                win.Height = h - 200;
                win.Left = (w - win.Width) / 2;
                win.Top = (h - win.Height) / 2;
            };
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

            // 系统图绘制
            //var graph = Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            //if (graph == null)
            //{
            //    return;
            //}
            //var vertices = graph.Vertices.ToList();
            //var drawCmd = new ThPDSSystemDiagramCommand(graph, vertices);
            //drawCmd.Execute();

            var modifyCmd = new ThPDSUpdateToDwgService();
            // 标注修改
            //modifyCmd.Update();

            // 标注定位
            //modifyCmd.Zoom();

            // 创建标注
            //modifyCmd.AddLoadDimension();
        }
    }
}