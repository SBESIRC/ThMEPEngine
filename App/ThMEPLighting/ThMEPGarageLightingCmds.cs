using ThMEPLighting.Garage;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPLighting
{
    public class ThMEPGarageLightingCmds
    {
        [CommandMethod("TIANHUACAD", "THDXC", CommandFlags.Modal)]
        public void ThDxc()
        {
            using (var dxCmd = new ThDXDrawingCmd())
            {
                dxCmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THFDXC", CommandFlags.Modal)]
        public void ThFdxc()
        {
            using (var fdxCmd = new ThFDXDrawingCmd())
            {
                fdxCmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THDDXC", CommandFlags.Modal)]
        public void THDDXC()
        {
            using (var ddxCmd = new ThSingleRowCenterDrawingCmd())
            {
                ddxCmd.Execute();
            }
        }
    }
}
