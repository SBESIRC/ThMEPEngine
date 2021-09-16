using Autodesk.AutoCAD.Runtime;
using TianHua.Hvac.UI.Command;

namespace TianHua.Hvac.UI
{
    public class HvacUiApp : IExtensionApplication
    {
        public void Initialize()
        {

        }

        public void Terminate()
        {

        }

        [CommandMethod("TIANHUACAD", "THFJF", CommandFlags.Modal)]
        public void Thfjf()
        {
            using (var cmd = new ThHvacFjfCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THDKFPM", CommandFlags.Modal)]
        public void THDKFPM()
        {
            using (var cmd = new ThHvacDuctPortsCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THDKFPMFG", CommandFlags.Modal)]
        public void THDKFPMFG()
        {
            using (var cmd = new ThHvacDuctModifyCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THDKFPMXG", CommandFlags.Modal)]
        public void THDKFPMXG()
        {
            using (var cmd = new ThHvacPortModifyCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFPM", CommandFlags.Modal)]
        public void THFPM()
        {
            using (var cmd = new ThHvacFjfCmd(true))
            {
                cmd.Execute();
            }
        }
    }
}
