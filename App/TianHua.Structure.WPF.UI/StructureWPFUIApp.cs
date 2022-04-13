using Autodesk.AutoCAD.Runtime;
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

        [CommandMethod("TIANHUACAD", "THQZCSSZ", CommandFlags.Session)]
        public void THQZCSSZ()
        {
            var ui = new WallColumnReinforceSetUI();
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);
        }

        [CommandMethod("TIANHUACAD", "THHRBYGJ", CommandFlags.Session)]
        public void THHRBYGJ()
        {
            var vm = new EdgeComponentDrawVM();
            var ui = new EdgeComponentDrawUI(vm);
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);
        }

        [CommandMethod("TIANHUACAD", "THZPJSZ", CommandFlags.Session)]
        public void THZPJSZ()
        {
            var ui = new ColumnReinforceSetUI();
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);
        }
    }
}
