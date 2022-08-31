using Autodesk.AutoCAD.Runtime;

using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public class ThHydrantConnectPipeConnectCmd
    {
        public static ThHydrantConnectPipeConfigInfo ConfigInfo;

        [CommandMethod("TIANHUACAD", "THFHPC", CommandFlags.Modal)]
        public void THMEPGARAGELAYOUT()
        {
            if (ConfigInfo == null)
            {
                return;
            }

            var cmd = new ThHydrantConnectPipeCmd(ConfigInfo)
            {
                CommandName = "THDXXHS",
                ActionName = "连管",
            };
            cmd.Execute();
        }
    }
}
