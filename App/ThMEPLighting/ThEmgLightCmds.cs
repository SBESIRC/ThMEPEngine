using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using AcHelper;
using Dreambuild.AutoCAD;
using Linq2Acad;

using ThCADExtension;
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
        [CommandMethod("TIANHUACAD", "THYJZMDC", CommandFlags.Modal)]
        public void ThEmgLightSingle()
        {
            //单侧 singleside =1
            //var singleSide = UISettingService.Instance.singleSide;
            var singleSide = 1;
            ThEmgLight(singleSide);
        }

        [CommandMethod("TIANHUACAD", "THYJZMSC", CommandFlags.Modal)]
        public void ThEmgLightDouble()
        {
            //双侧 singleside =0
            //var singleSide = UISettingService.Instance.singleSide;
            var singleSide = 0;
            ThEmgLight(singleSide);
        }

        private void ThEmgLight(int singleSide)
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

                var scale = UISettingService.Instance.scale;
                var blkType = UISettingService.Instance.blkType;


                var blkName = blkType == 0 ? ThMEPLightingCommon.EmgLightBlockName : ThMEPLightingCommon.EmgLightDoubleBlockName;

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var frameClone = frame.WashClone() as Polyline;
                    var centerPt = frameClone.StartPoint;

                    //debug
                    //centerPt = new Point3d();

                    //处理外包框
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(frameClone);
                    frameClone.Closed = true;
                    var nFrame = ThMEPFrameService.NormalizeEx(frameClone);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    //为了获取卡在外包框的建筑元素，这里做了一个Buffer处理
                    var bufferTransFrame = ThMEPFrameService.Buffer(nFrame, EmgLightCommon.BufferFrame);
                    var shrinkTransFrame = ThMEPFrameService.Buffer(nFrame, -EmgLightCommon.BufferFrame);
                    var bufferFrame = bufferTransFrame.Clone() as Polyline;
                    transformer.Reset(bufferFrame);
                    DrawUtils.ShowGeometry(bufferFrame, EmgLightCommon.LayerFrame, 130, 35);
                    DrawUtils.ShowGeometry(bufferTransFrame, EmgLightCommon.LayerFrame, 130, 35);
                    DrawUtils.ShowGeometry(shrinkTransFrame, EmgLightCommon.LayerFrame, 130, 35);

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightLayerColor), true);

                    //取块
                    var getBlockS = new GetBlockService();
                    getBlockS.getBlocksData(bufferTransFrame, transformer);
                    Dictionary<BlockReference, BlockReference> evacBlk = new Dictionary<BlockReference, BlockReference>();
                    getBlockS.evacR.ForEach(x => evacBlk.Add(x.Key, x.Value));
                    getBlockS.evacRL.ForEach(x => evacBlk.Add(x.Key, x.Value));

                    //清除layer
                    //var block = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer);
                    //RemoveBlockService.ClearEmergencyLight(block);
                    RemoveBlockService.ClearEmergencyLight(getBlockS.emgLight);
                    RemoveBlockService.ClearEmergencyLight(getBlockS.emgLightDouble);

                    //获取车道线
                    var mergedOrderedLane = GetSourceDataService.BuildLanes(shrinkTransFrame, bufferTransFrame, acdb, transformer);

                    //获取建筑信息（柱和墙）
                    GetSourceDataService.GetStructureInfo(acdb, bufferFrame, bufferTransFrame, transformer, out List<Polyline> columns, out List<Polyline> walls);

                    //主车道布置信息
                    LayoutEmgLightEngine layoutEngine = new LayoutEmgLightEngine();
                    layoutEngine.frame = bufferTransFrame;
                    layoutEngine.lanes = mergedOrderedLane;
                    layoutEngine.columns = columns;
                    layoutEngine.walls = walls;
                    layoutEngine.evacBlk = evacBlk;
                    layoutEngine.singleSide = singleSide;
                    var layoutInfo = layoutEngine.LayoutLight();

                    //如果应急灯和疏散灯重合则移动应急灯
                    layoutEngine.moveEmg(ref layoutInfo);

                    //换回布置
                    layoutEngine.ResetResult(ref layoutInfo, transformer);

                    //布置构建
                    //double scale = LayoutEmgLightEngine.getScale(getBlockS);
                    InsertLightService.InsertSprayBlock(layoutInfo, scale, blkName);
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
                //debug
                //centerPt = new Point3d();
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
