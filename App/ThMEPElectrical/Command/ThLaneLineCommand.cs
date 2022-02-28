using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;

namespace ThMEPElectrical.Command
{
    public class ThLaneLineCommand : ThMEPBaseCommand, IDisposable
    {
        public List<string> LaneLineLayers { get; set; }

        public ThLaneLineCommand()
        {
            CommandName = "THTCDX";
            ActionName = "提车道中心线";
            LaneLineLayers = new List<string>();
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
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

                acadDatabase.Database.CreateAILaneCenterLineLayer();
                foreach (var frameId in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(frameId);
                    var nFrame = ThMEPFrameService.Normalize(frame);
                    if (nFrame.Area <= 1.0)
                    {
                        continue;
                    }

                    var lines = LoadLaneLines(acadDatabase.Database, nFrame);
                    if (lines.Count == 0)
                    {
                        continue;
                    }

                    // 
                    var centerPt = nFrame.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(lines);
                    transformer.Transform(nFrame);
                    lines = ClipLaneLines(lines, nFrame);
                    if (lines.Count == 0)
                    {
                        continue;
                    }
                    lines = CleanLaneLines(lines);
                    transformer.Reset(lines);

                    lines.Cast<Curve>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.ColorIndex = (int)ColorIndex.BYLAYER;
                        o.Layer = ThMEPCommon.LANELINE_LAYER_NAME;
                    });
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

        private DBObjectCollection ClipLaneLines(DBObjectCollection curves, Polyline frame)
        {
            var lines = ThLaneLineSimplifier.Simplify(curves, 1500);
            return ThCADCoreNTSGeometryClipper.Clip(frame, lines.ToCollection());
        }

        private DBObjectCollection LoadLaneLines(Database database, Polyline frame)
        {
            using (var laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                laneLineEngine.LayerFilter = LaneLineLayers;
                var bFrame = ThMEPFrameService.Buffer(frame, 100000.0);
                laneLineEngine.Recognize(database, bFrame.Vertices());
                return laneLineEngine.Spaces.Select(o => o.Boundary).ToCollection();
            }
        }
    }
}
