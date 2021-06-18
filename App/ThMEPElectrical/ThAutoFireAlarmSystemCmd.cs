using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThAutoFireAlarmSystemCmd
    {

        [CommandMethod("TIANHUACAD", "ThAFASP", CommandFlags.Modal)]
        public void ThAFASB()
        {
            using (var cmd = new ThPolylineAutoFireAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThAFASF", CommandFlags.Modal)]
        public void ThAFASF()
        {
            using (var cmd = new ThFrameFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThAFASA", CommandFlags.Modal)]
        public void ThAFASA()
        {
            using (var cmd = new ThAllDrawingsFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }
    }
}
