using THMEPCore3D.Commands;
using Autodesk.AutoCAD.Runtime;

namespace THMEPCore3D
{
    public class ThMEPCore3DCmds
    {
        [CommandMethod("TIANHUACAD", "ThEMCC", CommandFlags.Modal)]
        public void ThEMCC()
        {
            using (var cmd = new ThExtractModelCodeCmd())
            {
                cmd.Execute();
            }
        }
    }
}
