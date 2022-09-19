using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;


namespace ThMEPHVAC.FloorHeatingCoil.Cmd
{
    public class ThFloorHeatingCmd : IDisposable
    {
        private bool WithUI = ThFloorHeatingCoilSetting.Instance.WithUI;
        public ThFloorHeatingCmd()
        {
            InitialCmdInfo();
            //InitialSetting();
        }
        private void InitialCmdInfo()
        {
            //ActionName = "生成";
            //CommandName = "THDNPG"; //地暖盘管
        }
        //private void InitialSetting()
        //{
        //    _BlockNameDict = ThFloorHeatingCoilSetting.Instance.BlockNameDict;
        //    WithUI = ThFloorHeatingCoilSetting.Instance.WithUI;
        //}
        public void SubExecute()
        {
            ThFlootingHeatingExecute();
        }
        public void Dispose()
        {
        }
        public void ThFlootingHeatingExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkList = new List<string> { ThFloorHeatingCommon.BlkName_ShowRoute,
                                                    ThFloorHeatingCommon.BlkName_RoomSuggest,
                                                    ThFloorHeatingCommon.BlkName_WaterSeparator,
                                                    ThFloorHeatingCommon.BlkName_BathRadiator,};

                var layerList = new List<string> { ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_ShowRoute],
                                                    ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_RoomSuggest],
                                                    ThFloorHeatingCommon.Layer_Coil };

                ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);


                var selectFrames = ThSelectFrameUtil.SelectPolyline();
                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = ThFloorHeatingCoilUtilServices.GetTransformer(selectFrames, false);//<----目前状态：移动到原点

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer, WithUI);
                dataQuery.Print();

                var roomPlSuggestDict = new Dictionary<Polyline, BlockReference>();
                if (ThFloorHeatingCreateService.CheckValidDataSet(dataQuery.RoomSet))
                {
                    var roomSuggest = ThFloorHeatingCoilUtilServices.GetRoomSuggestData(acadDatabase.Database, transformer);

                    ThFloorHeatingCoilUtilServices.PairRoomPlWithRoomSuggest(dataQuery.RoomSet[0].Room, roomSuggest, ref roomPlSuggestDict);
                    ThFloorHeatingCoilUtilServices.PairRoomWithRoomSuggest(ref dataQuery.RoomSet, roomPlSuggestDict, 250);

                    //------
                    //参考  ThFloorHeatingCoilUtilServices.PassUserParameter(VM);
                    Parameter.PublicRegionConstraint = true;
                    Parameter.IndependentRoomConstraint = true;
                    Parameter.AuxiliaryRoomConstraint = true;
                    Parameter.PrivatePublicMode = 1;
                    Parameter.TotalLength = 120 * 1000;

                    Parameter.SuggestDistanceWall = 200;

                    PublicValue.ChangeSDis = 0;
                    //直接修改参数
                    //------

                    var createSR = new UserInteraction();
                    createSR.PipelineB(dataQuery.RoomSet[0]);
                }

                var needUpdateSR = false;
                if (ProcessedData.RegionList != null && ProcessedData.RegionList.Count > 0)
                {
                    //检测sr和回路图块是否匹配,这里200是匹配不到房间的默认间距，可以修改
                    needUpdateSR = ThFloorHeatingCreateService.PairSingleRegionWithRoomSuggest(ref ProcessedData.RegionList, roomPlSuggestDict, 200);
                }
                if (needUpdateSR == true)
                {
                    var updateSR = new UserInteraction();
                    updateSR.PipelineC();
                }

            }
        }
    }

    public class ThFloorHeatingShowRouteCmd : ThMEPBaseCommand, IDisposable
    {
        private ThFloorHeatingCoilViewModel VM;
        private List<Polyline> SelectFrames = new List<Polyline>();
        private ThMEPOriginTransformer Transformer = new ThMEPOriginTransformer();
        private Dictionary<Polyline, BlockReference> RoomPlSuggestDict = new Dictionary<Polyline, BlockReference>();
        private Dictionary<BlockReference, BlockReference> RoomSuggestDict = new Dictionary<BlockReference, BlockReference>();
        private bool WithUI = ThFloorHeatingCoilSetting.Instance.WithUI;
        public ThFloorHeatingShowRouteCmd(ThFloorHeatingCoilViewModel vm)
        {
            this.VM = vm;
            InitialCmdInfo();
        }
        private void InitialCmdInfo()
        {
            ActionName = "生成";
            CommandName = "THDNPG"; //地暖盘管
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            ThFlootingHeatingShowRouteExecute();
        }

        private void ThFlootingHeatingShowRouteExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    var blkList = new List<string> { ThFloorHeatingCommon.BlkName_ShowRoute,
                                                    ThFloorHeatingCommon.BlkName_RoomSuggest,
                                                    ThFloorHeatingCommon.BlkName_WaterSeparator,
                                                    ThFloorHeatingCommon.BlkName_BathRadiator,};

                    var layerList = new List<string> { ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_ShowRoute],
                                                    ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_RoomSuggest],
                                                    ThFloorHeatingCommon.Layer_Coil };
                    ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                    VM.CleanData();

                    SelectFrames = ThSelectFrameUtil.SelectPolyline();
                    if (SelectFrames.Count == 0)
                    {
                        return;
                    }

                    Transformer = ThFloorHeatingCoilUtilServices.GetTransformer(SelectFrames, false);

                    var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, SelectFrames, Transformer, WithUI);
                    dataQuery.Print();

                    if (ThFloorHeatingCreateService.CheckValidDataSet(dataQuery.RoomSet))
                    {
                        RoomSuggestDict = ThFloorHeatingCoilUtilServices.GetRoomSuggestData(acadDatabase.Database, Transformer);
                        ThFloorHeatingCoilUtilServices.PairRoomPlWithRoomSuggest(dataQuery.RoomSet[0].Room, RoomSuggestDict, ref RoomPlSuggestDict);
                        ThFloorHeatingCoilUtilServices.PairRoomWithRoomSuggest(ref dataQuery.RoomSet, RoomPlSuggestDict, VM.SuggestDistDefualt);

                        ThFloorHeatingCoilUtilServices.PassUserParameter(VM);
                        var createSR = new UserInteraction();
                        createSR.PipelineB(dataQuery.RoomSet[0]);
                    }

                    var needUpdateSR = false;
                    if (ProcessedData.RegionList != null && ProcessedData.RegionList.Count > 0)
                    {
                        //检测sr和回路图块是否匹配
                        needUpdateSR = ThFloorHeatingCreateService.PairSingleRegionWithRoomSuggest(ref ProcessedData.RegionList, RoomPlSuggestDict, VM.SuggestDistDefualt);
                    }

                    if (needUpdateSR == true)
                    {
                        //更新回路
                        var updateSR = new UserInteraction();
                        updateSR.PipelineC();
                    }

                    if (ProcessedData.RegionList != null && ProcessedData.RegionList.Count > 0)
                    {
                        //更新回路分配图块
                        var updateWaterSeparatorRoom = false;
                        if (VM.PrivatePublicMode == 0)
                        {
                            updateWaterSeparatorRoom = true;
                        }
                        ThFloorHeatingCreateService.UpdateSRSuggestBlock(ProcessedData.RegionList, ProcessedData.PipeList, RoomPlSuggestDict, updateWaterSeparatorRoom, Transformer);
                    }

                    if (ProcessedData.PipeList != null && ProcessedData.PipeList.Count > 0)
                    {
                        //打印最终结果
                        CleanPreviousPrintCoil();
                        PrintCoil();
                        CleanPreviousPrintCoilBlk();
                        PrintCoilBlk();

                        UpdateWaterSeparatorNumber(dataQuery.RoomSet);
                        CreateRoomSeparatorAtDoor(dataQuery.RoomSet);

                        VM.CleanData();
                    }
                }
                catch (System.Exception ex)
                {
                    if (ex.Message == ThFloorHeatingCommon.Error_privateOneDoor)
                    {
                        Active.Editor.WriteMessage(string.Format("\n{0}\n", ex.Message));
                        VM.CleanData();
                        return;
                    }
                    else
                    {
                        VM.CleanData();
                        throw ex;
                    }
                }
            }
        }

        private void PrintCoil()
        {
            var pipes = ProcessedData.PipeList.SelectMany(x => x.ResultPolys).Distinct().ToList();
            var printPipe = new List<Polyline>();

            foreach (var p in pipes)
            {
                if (p.Length > 1)
                {
                    var cp = p.Clone() as Polyline;
                    printPipe.Add(cp);
                }
            }

            printPipe.ForEach(x => Transformer.Reset(x));

            ThFloorHeatingCoilInsertService.InsertPolyline(printPipe, ThFloorHeatingCommon.Layer_Coil, true);
        }

        private void PrintCoilBlk()
        {
            var pipes = ProcessedData.PipeList.Where(x => x.ResultPolys != null && x.ResultPolys.Count > 0 && x.ResultPolys[0].Length > 1).ToList();

            for (int i = 0; i < pipes.Count; i++)
            {
                var pipe = pipes[i];
                SingleRegion selectRegion;
                var sr = pipe.DomaintRegionList.Select(x => ProcessedData.RegionList[x]).ToList();
                var noPassingSR = sr.Where(x => x.PassingPipeList.Count == 1).ToList();
                if (noPassingSR.Any())
                {
                    selectRegion = noPassingSR.OrderByDescending(x => x.ClearedPl.Area).First();
                }
                else
                {
                    selectRegion = sr.OrderByDescending(x => x.ClearedPl.Area).First();
                }

                var route = i;
                var suggestDist = selectRegion.SuggestDist;
                var length = pipe.ResultPolys.Sum(x => x.Length); ;
                length = Math.Round(length / 1000, MidpointRounding.AwayFromZero);

                var insertPt = selectRegion.ClearedPl.GetCenterInPolyline();
                Transformer.Reset(ref insertPt);

                ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_ShowRoute, true);
            }
        }

        private void CleanPreviousPrintCoil()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //extract
                var elementLayer = new List<string>() { ThFloorHeatingCommon.Layer_Coil };
                var pipes = ThFloorHeatingDataFactory.ExtractPolylineMsNotClone(acadDatabase.Database, elementLayer);

                //transform
                SelectFrames.ForEach(x => Transformer.Transform(x));
                var pipesDict = new Dictionary<Polyline, Polyline>(); //key:trans value:ori
                foreach (var p in pipes)
                {
                    var tranP = p.Clone() as Polyline;
                    Transformer.Transform(tranP);
                    pipesDict.Add(tranP, p);
                }

                //select
                var obj = pipesDict.Select(x => x.Key).ToCollection();
                var idx = new ThCADCoreNTSSpatialIndex(obj);
                var selectPipes = new List<Polyline>();

                foreach (var frame in SelectFrames)
                {
                    var selectobj = idx.SelectCrossingPolygon(frame);
                    selectPipes.AddRange(selectobj.OfType<Polyline>());
                }
                selectPipes = selectPipes.Distinct().ToList();

                //remove
                foreach (var tranP in selectPipes)
                {
                    var p = pipesDict[tranP];
                    p.UpgradeOpen();
                    p.Erase();
                    p.DowngradeOpen();
                }

                SelectFrames.ForEach(x => Transformer.Reset(x));
            }


        }
        private void CleanPreviousPrintCoilBlk()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //extract
                var extractService = new ThExtractBlockReferenceService()
                {
                    BlockName = ThFloorHeatingCommon.BlkName_ShowRoute,
                };
                extractService.Extract(acadDatabase.Database, new Point3dCollection());
                var showRouteBlk = extractService.Blocks.ToList();

                //transform
                SelectFrames.ForEach(x => Transformer.Transform(x));
                var blkDict = new Dictionary<BlockReference, BlockReference>(); //key:trans value:ori
                foreach (var b in showRouteBlk)
                {
                    var tranB = b.Clone() as BlockReference;
                    Transformer.Transform(tranB);
                    blkDict.Add(tranB, b);
                }

                //select
                var obj = blkDict.Select(x => x.Key).ToCollection();
                var idx = new ThCADCoreNTSSpatialIndex(obj);
                var selectBlk = new List<BlockReference>();
                foreach (var frame in SelectFrames)
                {
                    var selectobj = idx.SelectCrossingPolygon(frame);
                    selectBlk.AddRange(selectobj.OfType<BlockReference>());
                }
                selectBlk = selectBlk.Distinct().ToList();

                //remove
                foreach (var tranBlk in selectBlk)
                {
                    var blk = blkDict[tranBlk];
                    blk.UpgradeOpen();
                    blk.Erase();
                    blk.DowngradeOpen();
                }

                SelectFrames.ForEach(x => Transformer.Reset(x));
            }
        }

        private void UpdateWaterSeparatorNumber(List<ThRoomSetModel> RoomSet)
        {
            if (ThFloorHeatingCreateService.CheckValidDataSet(RoomSet))
            {
                var pipes = ProcessedData.PipeList.Where(x => x.ResultPolys != null && x.ResultPolys.Count > 0 && x.ResultPolys[0].Length > 1).ToList();

                var routeNum = string.Format("{0}路", pipes.Count());
                var dynDic = new Dictionary<string, object>() {
                        { ThFloorHeatingCommon.BlkSettingAttrName_WaterSeparator, routeNum } ,
                    };

                var w = RoomSet[0].WaterSeparator;
                w.Blk.UpgradeOpen();
                foreach (var dyn in dynDic)
                {
                    w.Blk.ObjectId.SetDynBlockValue(dyn.Key, dyn.Value);
                }
                w.Blk.DowngradeOpen();

            }
        }

        private void CreateRoomSeparatorAtDoor(List<ThRoomSetModel> RoomSet)
        {
            var lines = new List<Polyline>();

            var connectDoor = GetConnectDoor(RoomSet[0]);

            foreach (var door in connectDoor)
            {
                var pt0 = door.GetPoint3dAt(0);
                var pt1 = door.GetPoint3dAt(1);
                var pt2 = door.GetPoint3dAt(2);
                var ptLong0 = new Point3d();
                var ptLong1 = new Point3d();
                var ptDir2 = new Point3d();
                if (pt0.DistanceTo(pt1) > pt1.DistanceTo(pt2))
                {
                    ptLong0 = pt0;
                    ptLong1 = pt1;
                    ptDir2 = pt2;
                }
                else
                {
                    ptLong0 = pt1;
                    ptLong1 = pt2;
                    ptDir2 = door.GetPoint3dAt(3);
                }
                var dir = (ptDir2 - ptLong1) / 2;

                var line = new Polyline();
                line.AddVertexAt(0, (ptLong0 + dir).ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(0, (ptLong1 + dir).ToPoint2D(), 0, 0, 0);

                lines.Add(line);

            }

            lines.ForEach(x => Transformer.Reset(x));
            ThFloorHeatingCoilInsertService.InsertPolyline(lines, ThFloorHeatingCommon.Layer_RoomSeparate);
        }

        private static List<Polyline> GetConnectDoor(ThRoomSetModel roomSet)
        {
            var doorConnect = new List<Polyline>();

            foreach (var door in roomSet.Door)
            {
                var touchedRoom = roomSet.Room.Where(x => x.RoomBoundary.Intersects(door));
                if (touchedRoom.Count() >= 2) //和2个以上的房间相邻
                {
                    doorConnect.Add(door);
                }
            }

            return doorConnect;
        }
    }

}
