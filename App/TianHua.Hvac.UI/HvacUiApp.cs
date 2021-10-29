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
        [CommandMethod("TIANHUACAD", "THXFJ", CommandFlags.Modal)]
        public void THXFJ()
        {
            using (var cmd = new ThHvacXfjCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFHJS", CommandFlags.Modal)]
        public void THFHJS()
        {
            using (var cmd = new ThHvacLoadCalculationCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSWTF", CommandFlags.Modal)]
        public void THSWTF()
        {
            using (var cmd = new ThHvacOutdoorVentilationCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFJBH", CommandFlags.Modal)]
        public void THFJBH()
        {
            using (var cmd = new ThHvacRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }
    }
}
