using Autodesk.AutoCAD.Runtime;
using TianHua.Electrical.PDS.UI.UI;
using TianHua.Electrical.PDS.Service;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        // 全局界面
        // https://through-the-interface.typepad.com/through_the_interface/2006/10/perdocument_dat_1.html
        // https://forums.autodesk.com/t5/net/any-reason-to-make-a-class-defining-commands-as-static/td-p/5449525
        private static ElecSandboxUI _PDSUI;

        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        /// <summary>
        /// 电力系统
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDLXT", CommandFlags.Modal)]
        public static void THDLXT()
        {
            if (_PDSUI == null)
            {
                // 初始化全局界面
                _PDSUI = new ElecSandboxUI();
                _PDSUI.LoadProject();
            }

            if (_PDSUI != null)
            {
                // 初始化窗口位置
                int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                _PDSUI.Loaded += (s, e) =>
                {
                    _PDSUI.Width = w - 200;
                    _PDSUI.Height = h - 200;
                    if (_PDSUI.Width > 1300) _PDSUI.Width = 1300;
                    if (_PDSUI.Height > 850) _PDSUI.Height = 850;
                    _PDSUI.Left = (w - _PDSUI.Width) / 2;
                    _PDSUI.Top = (h - _PDSUI.Height) / 2;
                };

                // 显示窗口
                AcadApp.ShowModelessWindow(_PDSUI);
            }
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