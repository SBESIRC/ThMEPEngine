using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPTCH.Model;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Service;
using ThMEPTCH.TCHDrawServices;

namespace ThMEPWSS
{
    public class ThPipeCmds
    {
        /// <summary>
        /// 立管标注
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLGBZ", CommandFlags.Modal)]
        public void THLGBZ()
        {
            using (var cmd = new ThPipeCreateCmd())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// 楼层框线
        /// </summary>
        [CommandMethod("TIANHUACAD", "THLCKX", CommandFlags.Modal)]
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
    }
}
