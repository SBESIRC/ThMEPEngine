using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Command;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS
{
    class ThWaterwellPumpCmds
    {
        /// <summary>
        /// 执行校验潜水泵命令
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSJSBJY", CommandFlags.Modal)]
        public void CheckDeepWellPump()
        {
            using (var cmd = new ThCheckDeepWellPumpCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THDXXHS", CommandFlags.Modal)]
        public void HydrantConnectPipe()
        {
            using (var cmd = new ThHydrantConnectPipeCmd())
            {
                cmd.Execute();
            }
        }
    }
}
