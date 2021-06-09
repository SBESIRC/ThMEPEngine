using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Command;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        //[CommandMethod("TIANHUACAD", "THFCSDTEST", CommandFlags.Modal)]
        public void ThCreateFireControlSystemDiagram()
        {
            using (var cmd = new ThFireControlSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }
    }
}
