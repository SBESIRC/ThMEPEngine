using ThMEPWSS.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPWSS
{
    public class ThLayoutFlushPointCmds
    {
        [CommandMethod("TIANHUACAD", "THDXCXBZ", CommandFlags.Modal)]
        public void LayoutFlushPoint()
        {
            using (var cmd = new THLayoutFlushPointCmd())
            {
                cmd.Execute();
            }
        }
    }
}
