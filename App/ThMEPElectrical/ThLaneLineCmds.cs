using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThLaneLineCmds
    {
        [CommandMethod("TIANHUACAD", "THTCD", CommandFlags.Modal)]
        public void ThLaneLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneRecognitionEngine laneLineEngine = new ThLaneRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n请选择需要提取车道中心线的范围框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 提取车道中心线
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                laneLineEngine.Recognize(Active.Database, frame.Vertices());

                // 车道线处理
                var curves = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                var lines = ThLaneLineSimplifier.Simplify(curves.ToCollection(), 1500);
                if (lines.Count == 0)
                {
                    return;
                }

                // 框线相交处打断
                acadDatabase.Database.CreateLaneLineLayer();
                ThCADCoreNTSGeometryClipper.Clip(frame, lines.ToCollection()).Cast<Curve>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.Layer = ThMEPCommon.LANELINE_LAYER_NAME;
                });
            }
        }
    }
}
