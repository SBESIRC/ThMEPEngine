using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThElectricalLoadCalculationCmd
    {
        [CommandMethod("TIANHUACAD", "THYDFHSC", CommandFlags.Session)]
        public void THYDFHSC()
        {
            using (var cmd = new ThElectricalLoadCalculationCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THCRFJGNBZ", CommandFlags.Modal)]
        //天华电气房间块布置
        public void THCRFJGNBZ()
        {
            using (var cmd = new ThInsertRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }
    }
}
