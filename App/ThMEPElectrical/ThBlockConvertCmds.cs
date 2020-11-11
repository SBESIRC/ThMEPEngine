using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.Command;
using ThMEPElectrical.BlockConvert;

[assembly: CommandClass(typeof(ThMEPElectrical.ThBlockConvertCmds))]

namespace ThMEPElectrical
{
    public class ThBlockConvertCmds
    {
        [CommandMethod("TIANHUACAD", "THPBE", CommandFlags.Modal)]
        public void ThStrongCurrentBlockConvert()
        {
            using (var cmd = new ThBConvertCommand(ConvertMode.STRONGCURRENT))
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLBE", CommandFlags.Modal)]
        public void ThWeakCurrentBlockConvert()
        {
            using (var cmd = new ThBConvertCommand(ConvertMode.WEAKCURRENT))
            {
                cmd.Execute();
            }
        }
    }
}
