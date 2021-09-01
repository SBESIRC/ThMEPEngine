using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Dreambuild.AutoCAD;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using System.Linq;

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
        //[CommandMethod("TIANHUACAD", "THXSHTEST", CommandFlags.Modal)]
        //public static void THXSHTEST()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var result = Active.Editor.GetEntity("\n选择框线");
        //        if (result.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
        //        var nFrame = ThMEPFrameService.Normalize(frame);
        //        var extractor = new ThExtractFireHydrant();
        //        extractor.Extract(acadDatabase.Database, frame.Vertices());
        //        extractor.DBobjs.Cast<Entity>().ForEach(o =>
        //        {
        //            var rec = o.GeometricExtents.ToRectangle();
        //            acadDatabase.ModelSpace.Add(rec);
        //        });
        //    }
        //}
    }
}
