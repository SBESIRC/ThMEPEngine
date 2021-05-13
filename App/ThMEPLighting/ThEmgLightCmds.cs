using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Common;
using ThMEPLighting.EmgLightConnect;
using ThMEPLighting.EmgLightConnect.Service;


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

                    //取块
                    var getBlockS = new GetBlockService();
                    getBlockS.getBlocksData(bufferFrame, transformer);


                    //清除layer
                    //var block = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer);
                    //RemoveBlockService.ClearEmergencyLight(block);
                    RemoveBlockService.ClearEmergencyLight(getBlockS.emgLight);

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

                    //如果应急灯和疏散灯重合则移动应急灯
                    layoutEngine.moveEmg(getBlockS, ref layoutInfo);

                    //换回布置
                    layoutEngine.ResetResult(ref layoutInfo, transformer);

                    //布置构建
                    double scale = LayoutEmgLightEngine.getScale(getBlockS);
                    InsertLightService.InsertSprayBlock(layoutInfo, scale);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THYJZMLX", CommandFlags.Modal)]
        public void ThEmgLightConnect()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
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

                //获取ALE起点
                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择配电箱",
                    RejectObjectsOnLockedLayers = true,
                };
                dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference )).DxfName,
                };

                filter = ThSelectionFilterTool.Build(dxfNames);

                var sResult = Active.Editor.GetSelection(sOptions, filter);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var ALEOri = (acdb.Element<BlockReference>(sResult.Value.GetObjectIds().First()) as BlockReference);

                //确定位移中心
                var centerPt = ALEOri.Position;
                if (Math.Abs(centerPt.X) < 10E7)
                {
                    centerPt = new Point3d();
                }
                var transformer = new ThMEPOriginTransformer(centerPt);



                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var frameClone = frame.WashClone() as Polyline;

                    //处理外包框
                    transformer.Transform(frameClone);
                    var nFrame = ThMEPFrameService.NormalizeEx(frameClone);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frameListHoles = frameAnalysisService.analysisHoles(frameList);

                foreach (var nFrameHoles in frameListHoles)
                {

                    var nFrame = nFrameHoles.Key;
                    var nHoles = nFrameHoles.Value;

                    //为了获取卡在外包框的建筑元素，这里做了一个Buffer处理
                    var bufferFrame = ThMEPFrameService.Buffer(nFrame, EmgLightCommon.BufferFrame);
                    var shrinkFrame = ThMEPFrameService.Buffer(nFrame, -EmgLightCommon.BufferFrame);

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightConnectLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightConnectLayerColor), true);

                    //清除连线。待补

                    var b = false;
                    if (b == true)
                    {
                        continue;
                    }
                    //取块
                    var getBlockS = new GetBlockService();
                    getBlockS.getBlocksData(bufferFrame, transformer, nHoles);

                    var blockList = new Dictionary<EmgBlkType.BlockType, List<BlockReference>>();
                    getBlockS.getBlockList(blockList);

                    BlockReference ALE = ALEOri.Clone() as BlockReference;
                    transformer.Transform(ALE);
                    blockList.Add(EmgBlkType.BlockType.ale, new List<BlockReference> { ALE });



                    //获取车道线
                    var mergedOrderedLane = GetSourceDataService.BuildLanes(shrinkFrame, bufferFrame, acdb, transformer);

                    if (mergedOrderedLane.Count == 0 || (blockList[EmgBlkType.BlockType.emgLight].Count == 0 && blockList[EmgBlkType.BlockType.evac].Count == 0 && blockList[EmgBlkType.BlockType.otherSecBlk].Count == 0))
                    {
                        return;
                    }
                    var connectLine = ConnectEmgLightEngine.ConnectLight(mergedOrderedLane, blockList, nFrame, nHoles);

                    ConnectEmgLightEngine.ResetResult(ref connectLine, transformer);

                    InsertConnectLineService.InsertConnectLine(connectLine);
                }
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "CleanDebugLayer", CommandFlags.Modal)]
        public void ThCleanDebugLayer()
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                RemoveBlockService.ClearDrawing();
            }

        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "CleanDebugConnect", CommandFlags.Modal)]
        public void ThCleanDebugConnectLayer()
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                RemoveBlockService.ClearEmgConnect();
            }

        }
    }
}
