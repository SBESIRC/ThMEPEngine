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
        private Dictionary<string, List<string>> _BlockNameDict;
        private bool WithUI = false;

        public ThFloorHeatingCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            //ActionName = "生成";
            //CommandName = "THDNPG"; //地暖盘管
        }
        private void InitialSetting()
        {
            _BlockNameDict = ThFloorHeatingCoilSetting.Instance.BlockNameDict;
            WithUI = ThFloorHeatingCoilSetting.Instance.WithUI;
        }
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
                var selectFrames = ThSelectFrameUtil.SelectPolyline();
                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = new ThMEPOriginTransformer(selectFrames[0].GetPoint3dAt(0));
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var dataFactory = new ThFloorHeatingDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = _BlockNameDict,
                };

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer);
                dataQuery.Print();

                //过程写在这里
                Run run0 = new Run(dataQuery);
                run0.Pipeline();
                /////////////

            }
        }
    }

    public class ThFloorHeatingShowRouteCmd : ThMEPBaseCommand, IDisposable
    {
        private ThFloorHeatingCoilViewModel vm;
        public ThFloorHeatingShowRouteCmd(ThFloorHeatingCoilViewModel vm)
        {
            this.vm = vm;
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


                    if (ProcessedData.PipeList == null || ProcessedData.PipeList.Count == 0)
                    {
                        //如果没做过，先生成singleRegion (sr)
                        var roomSuggest = ThFloorHeatingCoilUtilServices.GetRoomSuggestData(acadDatabase.Database);
                        if (ProcessedData.RegionList == null || ProcessedData.RegionList.Count == 0)
                        {
                            var dataQuery = ThFloorHeatingCreateService.CreateSRData(vm);
                            dataQuery.Print();

                            if (ThFloorHeatingCreateService.CheckValidDataSet(dataQuery.RoomSet))
                            {
                                vm.RoomPlSuggestDict = ThFloorHeatingCoilUtilServices.PairRoomPlWithRoomSuggest(dataQuery.RoomSet[0].Room, roomSuggest, dataQuery.Transformer);
                                ThFloorHeatingCoilUtilServices.PairRoomWithRoomSuggest(ref dataQuery.RoomSet, vm.RoomPlSuggestDict, vm.SuggestDistDefualt);

                                ThFloorHeatingCoilUtilServices.PassUserParameter(vm);
                                var createSR = new UserInteraction();
                                createSR.PipelineB(dataQuery.RoomSet[0]);
                            }
                        }

                        var needUpdateSR = false;
                        if (ProcessedData.RegionList != null && ProcessedData.RegionList.Count > 0)
                        {
                            //检测sr和回路图块是否匹配
                            needUpdateSR = ThFloorHeatingCreateService.PairSingleRegionWithRoomSuggest(ref ProcessedData.RegionList, vm.RoomPlSuggestDict, vm.SuggestDistDefualt);
                        }

                        if (needUpdateSR == true)
                        {
                            //更新回路
                            ThFloorHeatingCoilUtilServices.PassUserParameter(vm);
                            var updateSR = new UserInteraction();
                            updateSR.PipelineC();
                        }

                        if (ProcessedData.RegionList != null && ProcessedData.RegionList.Count > 0)
                        {
                            //更新回路分配图块
                            var updateWaterSeparatorRoom = false;
                            if (vm.PrivatePublicMode == 0)
                            {
                                updateWaterSeparatorRoom = true;
                            }
                            ThFloorHeatingCreateService.UpdateSRSuggestBlock(ProcessedData.RegionList, ProcessedData.PipeList, vm.RoomPlSuggestDict, updateWaterSeparatorRoom);
                        }
                    }

                    if (ProcessedData.PipeList != null && ProcessedData.PipeList.Count > 0)
                    {
                        //打印最终结果
                        PrintCoil();
                        PrintCoilBlk();

                        vm.CleanSelectFrameAndData();
                    }
                }
                catch (System.Exception ex)
                {
                    if (ex.Message == ThFloorHeatingCommon.Error_privateOneDoor)
                    {
                        Active.Editor.WriteMessage(string.Format("\n{0}\n", ex.Message));
                        vm.CleanSelectFrameAndData();
                        return;
                    }
                    else
                    {
                        vm.CleanSelectFrameAndData();
                        throw ex;
                    }
                }
            }
        }

        private void PrintCoil()
        {
            //CleanPreviousPrintCoil();

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

            ThFloorHeatingCoilInsertService.InsertCoil(printPipe, ThFloorHeatingCommon.Layer_Coil, true);
        }

        private void PrintCoilBlk()
        {
            //CleanPreviousPrintCoilBlk();
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

                //var route = selectRegion.MainPipe[0];
                var route = i;
                var suggestDist = selectRegion.SuggestDist;
                var length = pipe.ResultPolys.Sum(x => x.Length); ;
                length = Math.Round(length / 1000, MidpointRounding.AwayFromZero);

                var insertPt = selectRegion.ClearedPl.GetCenter();
                ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_ShowRoute, true);
            }
        }

        private void CleanPreviousPrintCoil()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectFrame = vm.SelectFrame.ToList();

                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = ThFloorHeatingCommon.Layer_Coil,
                };
                extractService.Extract(acadDatabase.Database, new Point3dCollection());
                var RoomRouteSuggestBlk = extractService.Polys.ToList();
            }


        }
        private void CleanPreviousPrintCoilBlk()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //extract
                //var extractService = new ThBlockReferenceExtractor()
                var extractService = new ThExtractBlockReferenceService()
                {
                    BlockName = ThFloorHeatingCommon.BlkName_ShowRoute,
                };
                extractService.Extract(acadDatabase.Database, new Point3dCollection());
                var showRouteBlk = extractService.Blocks.ToList();

                //select
                var obj = showRouteBlk.ToCollection();
                var idx = new ThCADCoreNTSSpatialIndex(obj);

                var selectBlk = new List<BlockReference>();
                var selectFrame = vm.SelectFrame.ToList();

                foreach (var frame in selectFrame)
                {
                    var selectobj = idx.SelectCrossingPolygon(frame);
                    selectBlk.AddRange(selectobj.OfType<BlockReference>());
                }
                selectBlk = selectBlk.Distinct().ToList();

                //remove
                foreach (var blk in selectBlk)
                {
                    blk.UpgradeOpen();
                    blk.Erase();
                    blk.DowngradeOpen();
                }
            }
        }
    }
}
