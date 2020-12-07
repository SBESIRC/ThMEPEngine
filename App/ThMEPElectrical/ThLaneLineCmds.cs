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
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    RejectObjectsOnLockedLayers = true,
                    MessageForAdding = "请选择需要提取车道中心线的范围框线"
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,

                };
                var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames)
                );
                var result = Active.Editor.GetSelection(options, filterlist);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                acadDatabase.Database.CreateLaneLineLayer();
                foreach (var frameId in result.Value.GetObjectIds())
                {
                    using (ThLaneRecognitionEngine laneLineEngine = new ThLaneRecognitionEngine())
                    {
                        // 提取车道中心线
                        Polyline frame = acadDatabase.Element<Polyline>(frameId);
                        laneLineEngine.Recognize(Active.Database, frame.Vertices());

                        // 车道中心线处理
                        var curves = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                        var lines = ThLaneLineSimplifier.Simplify(curves.ToCollection(), 1500);

                        // 框线相交处打断
                        ThCADCoreNTSGeometryClipper.Clip(frame, lines.ToCollection()).Cast<Curve>().ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.Layer = ThMEPCommon.LANELINE_LAYER_NAME;
                        });
                    }
                }
            }
        }
    }
}
