using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;

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
        [CommandMethod("TIANHUACAD", "THTQKJ", CommandFlags.Modal)]
        public static void THTQKJ()
        {
            using (var cmd = new ThPipeExtractSpaceCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THHZKJ", CommandFlags.Modal)]
        public static void THHZKJ()
        {
            using (var cmd = new ThPipeDrawSpaceCmd())
            {
                cmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THKJMC", CommandFlags.Modal)]
        public static void THKJMC()
        {
            using (var cmd = new ThPipeDrawSpaceNameCmd())
            {
                cmd.Execute();
            }
        }
    }
}
