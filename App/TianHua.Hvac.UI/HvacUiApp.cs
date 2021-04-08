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

        [CommandMethod("TIANHUACAD", "THDUCTPORTS", CommandFlags.Modal)]
        public void ThDuctPorts()
        {
            using (var cmd = new ThHvacDuctPortsCmd())
            {
                cmd.Execute();
            }
        }
    }
}
