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
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using ThMEPHVAC.FloorHeatingCoil.Engine;


namespace ThMEPHVAC.FloorHeatingCoil.Cmd
{
    public class ThFloorHeatingCmd : ThMEPBaseCommand, IDisposable
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
            ActionName = "生成";
            CommandName = "THDNPG"; //地暖盘管
        }
        private void InitialSetting()
        {
            _BlockNameDict = ThFloorHeatingCoilSetting.Instance.BlockNameDict;
            WithUI = ThFloorHeatingCoilSetting.Instance.WithUI;
        }
        public override void SubExecute()
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

                var transformer = ThMEPHVACCommonUtils.GetTransformer(selectFrames[0].Vertices());
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

    public class ThFloorHeatingDistributeCmd : IDisposable
    {
        private ThFloorHeatingCoilViewModel vm;
        public ThFloorHeatingDistributeCmd(ThFloorHeatingCoilViewModel vm)
        {
            this.vm = vm;
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

        }
        public void SubExecute()
        {
            ThFlootingHeatingDistributeRouteExecute();
        }
        public void Dispose()
        {
        }
        public void ThFlootingHeatingDistributeRouteExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var roomSuggest = ThFloorHeatingDataFactory.GetRoomSuggestData(acadDatabase.Database);
                if (ProcessedData.RegionList == null || ProcessedData.RegionList.Count == 0)
                {
                    var dataQuery = ThFloorHeatingCreateSingleRegionEngin.CreateSRData(vm);
                    dataQuery.Print();

                    if (ThFloorHeatingCreateSingleRegionEngin.CheckValidDataSet(dataQuery.RoomSet))
                    {
                        vm.roomPlSuggestDict = ThFloorHeatingCoilUtilServices.PairRoomPlWithRoomSuggest(dataQuery.RoomSet[0].Room, roomSuggest);
                        ThFloorHeatingCoilUtilServices.PairRoomWithRoomSuggest(ref dataQuery.RoomSet, vm.roomPlSuggestDict, vm.SuggestDistDefualt);

                        ThFloorHeatingCoilUtilServices.PassUserParameter(vm);
                        var createSR = new UserInteraction();
                        createSR.PipelineB(dataQuery.RoomSet[0]);
                    }
                }

                var needUpdateSR = false;
                if (ProcessedData.RegionList.Count > 0)
                {
                    needUpdateSR = ThFloorHeatingUpdateSingleRegionEngine.PairSingleRegionWithRoomSuggest( ref ProcessedData.RegionList, vm.roomPlSuggestDict, vm.SuggestDistDefualt);
                }

                if (needUpdateSR == true)
                {
                    ThFloorHeatingCoilUtilServices.PassUserParameter(vm);
                    var updateSR = new UserInteraction();
                    updateSR.PipelineC();
                }

                ThFloorHeatingUpdateSingleRegionEngine.UpdateSRSuggestBlock( ProcessedData.RegionList, vm.roomPlSuggestDict);
            }
        }

    }


    public class ThFloorHeatingShowRouteCmd : IDisposable
    {
        private ThFloorHeatingCoilViewModel vm;
        public ThFloorHeatingShowRouteCmd(ThFloorHeatingCoilViewModel vm)
        {
            this.vm = vm;
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

        }

        public void Dispose()
        {
        }

        public void SubExecute()
        {
            ThFlootingHeatingShowRouteExecute();
        }

        private void ThFlootingHeatingShowRouteExecute()
        {
            if (ProcessedData.PipeList == null || ProcessedData.PipeList.Count == 0)
            {
                using (var cmd = new ThFloorHeatingDistributeCmd(vm))
                {
                    cmd.SubExecute();
                }
            }
            if (ProcessedData.PipeList.Count > 0)
            {
                PrintCoil();
                PrintCoilBlk();

                vm.CleanSelectFrameAndData();
            }
        }

        private void PrintCoil()
        {
            CleanPreviousPrintCoil();

            var pipes = ProcessedData.PipeList.SelectMany(x => x.ResultPolys).Distinct().ToList();
            var printPipe = new List<Polyline>();

            foreach (var p in pipes)
            {
                var cp = p.Clone() as Polyline;
                printPipe.Add(cp);
            }

            ThFloorHeatingCoilInsertService.InsertCoil(printPipe, true);
        }

        private void PrintCoilBlk()
        {
            CleanPreviousPrintCoilBlk();

            foreach (var pipe in ProcessedData.PipeList)
            {
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

                var route = selectRegion.MainPipe[0];
                var suggestDist = selectRegion.SuggestDist;
                var length = pipe.ResultPolys.Sum(x => x.Length); ;
                length = Math.Round(length / 1000, MidpointRounding.AwayFromZero);

                var insertPt = selectRegion.ClearedPl.GetCenter();
                ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_ShowRoute);
            }
        }
        private void CleanPreviousPrintCoil()
        {

        }
        private void CleanPreviousPrintCoilBlk()
        {

        }
    }
}
