using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPLighting.EmgLight;
using ThMEPLighting.EmgLight.Common;
using ThMEPLighting.EmgLight.Service;
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
                //获取外包框
                var frameList = selectFrame();

                if (frameList.Count == 0)
                {
                    return;
                }

                //获取转换点
                var pt = frameList.First().StartPoint;
                ThMEPOriginTransformer transformer = new ThMEPOriginTransformer(pt);
                frameList.ForEach(x => transformer.Transform(x));

                //处理外包框
                var frameTransProcess = HandleFrame(frameList);

                var scale = LayoutUISettingService.Instance.scale;
                var blkType = LayoutUISettingService.Instance.blkType;
                var blkName = blkType == 0 ? ThMEPLightingCommon.EmgLightBlockName : ThMEPLightingCommon.EmgLightDoubleBlockName;

                foreach (var nFrame in frameTransProcess)
                {
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
                    DrawUtils.CreateLayer(EmgLightCommon.LayerComment, Color.FromColorIndex(ColorMethod.ByLayer, EmgLightCommon.LayerCommentColor));

                    //取块
                    var getBlockS = new GetBlockService();
                    getBlockS.getBlocksData(bufferTransFrame, transformer);
                    Dictionary<BlockReference, BlockReference> evacBlk = new Dictionary<BlockReference, BlockReference>();
                    getBlockS.evacR.ForEach(x => evacBlk.Add(x.Key, x.Value));
                    getBlockS.evacRL.ForEach(x => evacBlk.Add(x.Key, x.Value));
                    var revCloud = GetSourceDataService.ExtractRevCloud(bufferTransFrame, EmgLightCommon.LayerComment, EmgLightCommon.LayerCommentColor, transformer);

                    //清除layer
                    //var block = GetSourceDataService.ExtractBlock(bufferFrame, ThMEPLightingCommon.EmgLightLayerName, ThMEPLightingCommon.EmgLightBlockName, transformer);
                    //RemoveBlockService.ClearEmergencyLight(block);
                    RemoveBlockService.ClearEmergencyLight(getBlockS.emgLight);
                    RemoveBlockService.ClearEmergencyLight(getBlockS.emgLightDouble);
                    RemoveBlockService.ClearPolyline(revCloud);

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
                    var commentList = commentLineService.getCommentLine(layoutInfo, columns);


                    //如果应急灯和疏散灯重合则移动应急灯
                    layoutEngine.moveEmg(ref layoutInfo);

                    //换回布置
                    layoutEngine.ResetResult(ref layoutInfo, transformer);
                    layoutEngine.ResetResult(ref commentList, transformer);

                    //布置构建
                    //double scale = LayoutEmgLightEngine.getScale(getBlockS);
                    InsertLightService.InsertSprayBlock(layoutInfo, scale, blkName);

                    InsertLightService.InsertRevcloud(commentList);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THYJZMLX", CommandFlags.Modal)]
        public void ThEmgLightConnect()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //获取外包框
                var frameList = selectFrame();
                if (frameList.Count == 0)
                {
                    return;
                }

                var ALEOri = selectALE();
                if (ALEOri == null)
                {
                    return;
                }

                //确定位移中心
                var transOriPt = ALEOri.Position;
                //transOriPt = new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0);
                var transformer = new ThMEPOriginTransformer(transOriPt);

                //转换框线
                frameList.ForEach(x => transformer.Transform(x));

                //处理外包框
                var frameTransProcess = HandleFrame(frameList);

                //处理洞
                var frameListHoles = frameAnalysisService.analysisHoles(frameTransProcess);

                foreach (var nFrameHoles in frameListHoles)
                {

                    var nFrame = nFrameHoles.Key;
                    var nHoles = nFrameHoles.Value;

                    DrawUtils.ShowGeometry(nFrame, EmgLightCommon.LayerFrame, 130, 35);

                    //为了获取卡在外包框的建筑元素，这里做了一个Buffer处理
                    var bufferFrame = ThMEPFrameService.Buffer(nFrame, EmgLightCommon.BufferFrame);
                    var shrinkFrame = ThMEPFrameService.Buffer(nFrame, -EmgLightCommon.BufferFrame);

                    //如果没有layer 创建layer
                    DrawUtils.CreateLayer(ThMEPLightingCommon.EmgLightConnectLayerName, Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightConnectLayerColor), true);

                    //清除连线。待补

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

                    var min = ConnectUISettingService.Instance.groupMin;
                    var max = ConnectUISettingService.Instance.groupMax;

                    var connectLine = ConnectEmgLightEngine.ConnectLight(mergedOrderedLane, blockList, nFrame, nHoles, min, max);

                    ConnectEmgLightEngine.ResetResult(ref connectLine, transformer);

                    InsertConnectLineService.InsertConnectLine(connectLine);
                }
            }
        }

        /// <summary>
        /// 选取框线
        /// </summary>
        /// <returns></returns>
        private static List<Polyline> selectFrame()
        {
            var frameList = new List<Polyline>();

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
                return frameList;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {   //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    frameList.Add(frame.WashClone() as Polyline);
                }
            }

            return frameList;
        }

        /// <summary>
        /// 选取ALE块
        /// </summary>
        /// <returns></returns>
        private static BlockReference selectALE()
        {
            BlockReference blk = null;
            //获取ALE起点
            PromptSelectionOptions sOptions = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择配电箱",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
             {
                    RXClass.GetClass(typeof(BlockReference )).DxfName,
             };

            var filter = ThSelectionFilterTool.Build(dxfNames);

            var sResult = Active.Editor.GetSelection(sOptions, filter);
            if (sResult.Status != PromptStatus.OK)
            {
                return blk;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var ALEOri = (acdb.Element<BlockReference>(sResult.Value.GetObjectIds().First()) as BlockReference);
                blk = ALEOri;
            }
            return blk;
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Polyline> frameList)
        {
            List<Polyline> resPolys = new List<Polyline>();

            foreach (var frame in frameList)
            {
                var nFrame = processFrame(frame);
                if (nFrame != null)
                {
                    resPolys.Add(nFrame);
                }
            }

            return resPolys;
        }

        /// <summary>
        /// 处理每一个外包框
        /// 根据讨论不用1000间距。不闭合框线直接硬连
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private static Polyline processFrame(Polyline frame)
        {
            Polyline nFrame = null;
            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }
            return nFrame;
        }
    }
}
