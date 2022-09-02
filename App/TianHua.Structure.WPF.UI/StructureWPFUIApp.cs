using Autodesk.AutoCAD.Runtime;
using ThMEPStructure.Reinforcement.Command;
using TianHua.Structure.WPF.UI.Command;
using TianHua.Structure.WPF.UI.Reinforcement;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Structure.WPF.UI
{
    public class StructureWPFUIApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THZLSCUI", CommandFlags.Session)]
        public void THZLUI()
        {
            using (var cmd = new MainBeamCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THCLSCUI", CommandFlags.Session)]
        public void THCLUI()
        {
            using (var cmd = new SecondaryBeamCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSXSCUI", CommandFlags.Session)]
        public void THSXUI()
        {
            using (var cmd = new BuildBeamCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THQZSZ", CommandFlags.Session)]
        public void THQZSZ()
        {
            var ui = new WallColumnReinforceSetUI();
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);
        }

        [CommandMethod("TIANHUACAD", "THQZPJ", CommandFlags.Session)]
        public void THQZPJ()
        {
            var vm = new EdgeComponentDrawVM();
            var ui = new EdgeComponentDrawUI(vm);
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);
        }

        [CommandMethod("TIANHUACAD", "THQZPJ1", CommandFlags.Session)]
        public void THQZPJ1()
        {
            using (var cmd  = new ThTSSDWallColumnReinforceDrawCmd())
            {
                cmd.Execute();
            }
        }        

        [CommandMethod("TIANHUACAD", "THTSDP", CommandFlags.Modal)]
        public void THTSDP()
        {
            // 加载TSDP2022 profile
            using (var cmd = new ThTSSDProfileSwitchCmd())
            {
                cmd.Execute();
            }
        }
    }
}
