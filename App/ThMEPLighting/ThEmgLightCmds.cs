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
using NFox.Cad;

using ThCADCore.NTS;
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

                var scale = LayoutUISettingService.Instance.scale;
                var blkType = LayoutUISettingService.Instance.blkType;


                var blkName = blkType == 0 ? ThMEPLightingCommon.EmgLightBlockName : ThMEPLightingCommon.EmgLightDoubleBlockName;

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    ////获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var transOriPt = frame.StartPoint;
                    var transformer = new ThMEPOriginTransformer(transOriPt);
                    var nFrame = processFrame(frame, transformer);
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
                var transOriPt = ALEOri.Position;
                if (Math.Abs(transOriPt.X) < 10E7)
                {
                    transOriPt = new Point3d();
                }

                var transformer = new ThMEPOriginTransformer(transOriPt);

                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    var nFrame = processFrame(frame, transformer);
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

        private static Polyline processFrame(Polyline frame, ThMEPOriginTransformer transformer)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            transformer.Transform(frameClone);
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;

        }

        [CommandMethod("TIANHUACAD", "THtestIntersection", CommandFlags.Modal)]
        public void thTestIntersection()
        {
            var a = new Polyline();
            a.AddVertexAt(a.NumberOfVertices, new Point2d(100, 0), 0, 0, 0);
            a.AddVertexAt(a.NumberOfVertices, new Point2d(200, 100), 0, 0, 0);
            a.AddVertexAt(a.NumberOfVertices, new Point2d(0, 100), 0, 0, 0);
            a.Closed = true;
            DrawUtils.ShowGeometry(a, "l0test", 3);

            //0.000001行 但0.00001找不到
            var b = new Polyline();
            b.AddVertexAt(b.NumberOfVertices, new Point2d(50, 100.00001), 0, 0, 0);
            b.AddVertexAt(b.NumberOfVertices, new Point2d(150, 100.00000001), 0, 0, 0);
            b.AddVertexAt(b.NumberOfVertices, new Point2d(150, 150), 0, 0, 0);
            b.AddVertexAt(b.NumberOfVertices, new Point2d(50, 150), 0, 0, 0);
            b.Closed = true;
            DrawUtils.ShowGeometry(b, "l0test", 4);

            var intpts = a.Intersect(b, Intersect.OnBothOperands);
            intpts.ForEach(x => DrawUtils.ShowGeometry(x, "l0test", 4, 25, 10));

            var intpts2 = b.Intersect(a, Intersect.OnBothOperands);
            intpts2.ForEach(x => DrawUtils.ShowGeometry(x, "l0test", 4, 25, 10, "S"));

            var interC = a.Intersection(new DBObjectCollection() { b });
            var interCE = interC.Cast<Entity>().ToList();
            DrawUtils.ShowGeometry(interCE, "l0test", 7);

            var interC2 = b.Intersection(new DBObjectCollection() { a });
            var interCE2 = interC2.Cast<Entity>().ToList();
            DrawUtils.ShowGeometry(interCE2, "l0test", 1);

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
