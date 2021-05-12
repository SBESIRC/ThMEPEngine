using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreRoomCmds
    {
        [CommandMethod("TIANHUACAD", "THKJTQ", CommandFlags.Modal)]
        public void THKJTQ()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 从外参中提取房间
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var engine = new ThRoomRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());

                // 输出房间
                var layerId = acadDatabase.Database.CreateAILayer("AI-空间框线", 30);
                engine.Elements.Cast<ThIfcRoom>().Select(r => r.Boundary as Polyline).ForEach(p =>
                {
                    p.LayerId = layerId;
                    p.ConstantWidth = 20;
                    acadDatabase.ModelSpace.Add(p);
                });
            }
        }
    }
}
