using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using DotNetARX;
using System;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using AcHelper;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS
{
    class ThWaterwellPumpCmds
    {
        /// <summary>
        /// 执行生成泵命令
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSJSB", CommandFlags.Modal)]
        public void CreateWaterWellPump()
        {
            using (var cmd = new ThCreateWaterWellPumpCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 执行生成提资表命令
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSJSBTZ", CommandFlags.Modal)]
        public void CreateWithdrawalForm() 
        {
            using (var cmd = new ThCreateWithdrawalFormCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 执行校验潜水泵命令
        /// </summary>
        [CommandMethod("TIANHUACAD", "THSJSBJY", CommandFlags.Modal)]
        public void CheckDeepWellPump()
        {
            using(var cmd = new ThCheckDeepWellPumpCmd())
            {
                cmd.Execute();
            }
        }
    }
}
