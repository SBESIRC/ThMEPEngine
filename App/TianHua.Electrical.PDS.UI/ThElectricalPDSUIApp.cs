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
        public static void THDLXT()
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
                if (win.Width > 1300) win.Width = 1300;
                if (win.Height > 850) win.Height = 850;
                win.Left = (w - win.Width) / 2;
                win.Top = (h - win.Height) / 2;
            };
            AcadApp.ShowModelessWindow(win);
        }

        /// <summary>
        /// 更新平面图
        /// </summary>
        [CommandMethod("TIANHUACAD", "THPDSUPDATEDWG", CommandFlags.Modal)]
        public static void THPDSUPDATEDWG()
        {
            new ThPDSUpdateToDwgService().Update();
        }
    }
}