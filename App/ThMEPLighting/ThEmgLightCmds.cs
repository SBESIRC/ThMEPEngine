using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;

namespace ThMEPLighting
{
    public class ThEmgLightCmds
    {
        [CommandMethod("TIANHUACAD", "THYJZM", CommandFlags.Modal)]
        public void ThEmgLight()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var frameClone = frame.WashClone() as Polyline;
                    var centerPt = frameClone.StartPoint;

                    //处理外包框
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(frameClone);
                    var nFrame = ThMEPFrameService.NormalizeEx(frameClone);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    //为了获取卡在外包框的建筑元素，这里做了一个Buffer处理
                    var bufferFrame = ThMEPFrameService.Buffer(nFrame, EmgLightCommon.BufferFrame);
                    var shrinkFrame = ThMEPFrameService.Buffer(nFrame, -EmgLightCommon.BufferFrame);
                    DrawUtils.ShowGeometry(bufferFrame, EmgLightCommon.LayerFrame, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight035);
                    DrawUtils.ShowGeometry(shrinkFrame, EmgLightCommon.LayerFrame, Color.FromColorIndex(ColorMethod.ByColor, 130), LineWeight.LineWeight035);

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightLayerColor), true);

                    //清除layer,has bug if change the coordiantes of dwg
                    var block = RemoveBlockService.ExtractClearEmergencyLight(transformer);
                    bufferFrame.ClearEmergencyLight(block);

                    RemoveBlockService.ClearDrawing();

                    var b = false;
                    if (b == true)
                    {
                        continue;
                    }


                    //获取车道线
                    var mergedOrderedLane = GetSourceDataService.BuildLanes(shrinkFrame, bufferFrame, acdb, transformer);

                    //获取建筑信息（柱和墙）
                    GetSourceDataService.GetStructureInfo(acdb, bufferFrame, transformer, out List<Polyline> columns, out List<Polyline> walls);

                    //主车道布置信息
                    LayoutEmgLightEngine layoutEngine = new LayoutEmgLightEngine();
                    var layoutInfo = layoutEngine.LayoutLight(bufferFrame, mergedOrderedLane, columns, walls);

                    //换回布置
                    layoutEngine.ResetResult(ref layoutInfo, transformer);

                    //布置构建
                    InsertLightService.InsertSprayBlock(layoutInfo);
                }
            }
        }
    }
}
