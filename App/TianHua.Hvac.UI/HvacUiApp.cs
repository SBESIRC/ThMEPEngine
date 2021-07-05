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
            using (var cmd = new THHvacFjfCmd())
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
            using (var cmd = new ThHvacDuctPortModifyCmd())
            {
                cmd.Execute();
            }
        }
    }
}
