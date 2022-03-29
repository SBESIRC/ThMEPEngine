using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Command;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "-THWTKSB", CommandFlags.Modal)]
        public void ThTestBlockConfig()
        {
            using (var cmd = new BlockNameConfig.Cmd(null))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THWTKBQ", CommandFlags.Modal)]
        public void THWTKBQ()
        {
            using (var cmd = new ThBlockTagCmd())
            {
                cmd.Execute();
            }
        }
    }
}
