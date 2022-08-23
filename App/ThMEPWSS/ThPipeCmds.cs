using AcHelper;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Pipe;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Service;

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

        [CommandMethod("TIANHUACAD", "THHTSL", CommandFlags.Modal)]
        public static void THHTSL()
        {
            using (var acdb = AcadDatabase.Active())
            {
                ThInsertStoreyFrameService.ImportHouseTypeSplitLineLayer();
                acdb.Database.SetCurrentLayer(ThWPipeCommon.HouseTypeSplitLineLayer);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }
        [CommandMethod("TIANHUACAD", "THCSL", CommandFlags.Modal)]
        public static void THCSL()
        {
            using (var acdb = AcadDatabase.Active())
            {
                ThInsertStoreyFrameService.ImportCellSplitLineLayer();
                acdb.Database.SetCurrentLayer(ThWPipeCommon.CellSplitLineLayer);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }
    }
}
