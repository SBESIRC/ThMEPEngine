using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;

namespace ThMEPWSS
{
    public class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THCRSD", CommandFlags.Modal)]
        public void ThCreateRainSystemDiagram()
        {
            using (var cmd = new ThRainSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "TestExractWaterBucket", CommandFlags.Modal)]
        public void TestExractWaterBucket()
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            using (var engine = new ThWGravityWaterBucketRecognitionEngine())
            {
                var per = AcHelper.Active.Editor.GetEntity("\n选择一个框");
                if (per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var frame = db.Element<Polyline>(per.ObjectId);
                    engine.Recognize(db.Database, frame.Vertices());
                }
            }
        }
        [CommandMethod("TIANHUACAD", "DelTemps", CommandFlags.Modal)]
        public void DeleteTestGeometries()
        {
            Dbg.DeleteTestGeometries();
        }
        [CommandMethod("TIANHUACAD", "LocatePipe", CommandFlags.Modal)]
        public void ShowVerticalPipe()
        {
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return;
            }

            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                string targetLabel = rst.StringResult;
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var sv = new ThRainSystemService() { adb = adb };
                sv.InitCache();
                sv.CollectVerticalPipesData();
                foreach (var e in sv.VerticalPipes)
                {
                    if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
                    {
                        if (lb == targetLabel)
                        {
                            Dbg.ShowWhere(e);
                        }
                    }
                }
            }
        }
    }
}
