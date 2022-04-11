using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundWaterSystem.Command;

namespace ThMEPWSS
{
    public class ThPipeCmds
    {
        [CommandMethod("TIANHUACAD", "THLGBZ", CommandFlags.Modal)]
        public void THLGBZ()
        {
            using (var cmd = new ThPipeCreateCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THLGLC", CommandFlags.Modal)]
        public void THLGLC()
        {
            using (var cmd = new ThPipeInsertFloorFrameCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THLGYY", CommandFlags.Modal)]
        public static void THLGYY()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThApplyPipesEngine.Apply(ThTagParametersService.sourceFloor, ThTagParametersService.targetFloors);
            }
        }
        [CommandMethod("TIANHUACAD", "THDXSXT_TEMP", CommandFlags.Modal)]
        public static void THDXSXT()
        {
            using(var cmd = new ThUndergroundWaterSystemCmd())
            {
                cmd.Execute();
            }
        }
    }
}
