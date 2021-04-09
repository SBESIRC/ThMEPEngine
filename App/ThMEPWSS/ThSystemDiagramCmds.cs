using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;

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
    }
}
