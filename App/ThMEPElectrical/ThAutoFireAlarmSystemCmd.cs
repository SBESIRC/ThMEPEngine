using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThAutoFireAlarmSystemCmd
    {

        [CommandMethod("TIANHUACAD", "THHZXTP", CommandFlags.Modal)]
        public void THHZXTP()
        {
            using (var cmd = new ThPolylineAutoFireAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZXTF", CommandFlags.Modal)]
        public void THHZXTF()
        {
            using (var cmd = new ThFrameFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZXTA", CommandFlags.Modal)]
        public void THHZXTA()
        {
            using (var cmd = new ThAllDrawingsFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }
    }
}
