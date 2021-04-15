using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.Command
{
    public class ThLaneLineCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
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
                    var frame = acadDatabase.Element<Polyline>(frameId);
                    var lines = LoadLaneLines(acadDatabase.Database, frame);
                    if (lines.Count > 0)
                    {
                        CleanLaneLines(lines).Cast<Curve>().ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.ColorIndex = 256;
                            o.Layer = ThMEPCommon.LANELINE_LAYER_NAME;
                        });
                    }
                }
            }
        }

        private DBObjectCollection CleanLaneLines(DBObjectCollection curves)
        {
            var service = new ThLaneLineCleanService()
            {
                CollinearGap = 150.0,
                ExtendDistance = 150.0,
            };
            return service.Clean(curves);
        }

        private DBObjectCollection LoadLaneLines(Database database, Polyline frame)
        {
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                var nFrame = ThMEPFrameService.NormalizeEx(frame);
                if (nFrame.Area > 1)
                {
                    // 提取车道中心线
                    var bFrame = ThMEPFrameService.Buffer(nFrame, 100000.0);
                    laneLineEngine.Recognize(database, bFrame.Vertices());

                    // 车道中心线处理
                    var curves = laneLineEngine.Spaces.Select(o => o.Boundary).ToList();
                    var lines = ThLaneLineSimplifier.Simplify(curves.ToCollection(), 1500);
                    // 框线相交处打断
                    return ThCADCoreNTSGeometryClipper.Clip(nFrame, lines.ToCollection());
                }   
                else
                {
                    Active.Editor.WriteLine("\n选择的框线有问题，请检查是否有自交、不相连等情况。");
                }
                return new DBObjectCollection();
            }
        }
    }
}
