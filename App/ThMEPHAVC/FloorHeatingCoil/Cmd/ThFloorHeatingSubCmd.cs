using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

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


namespace ThMEPHVAC.FloorHeatingCoil.Cmd
{
    public class ThFloorHeatingSubCmd
    {
        public static void CheckRoomConnectivity(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkList = new List<string> { };
                var layerList = new List<string> { ThFloorHeatingCommon.Layer_RoomSetFrame };
                ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var selectFrames = ThSelectFrameUtil.SelectPolyline();

                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = ThMEPHVACCommonUtils.GetTransformer(selectFrames[0].Vertices());
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer);

                if (dataQuery.RoomSet.Count > 0 && dataQuery.RoomSet[0].Room.Count > 0)
                {
                    var checkConnectiviry = new UserInteraction();
                    checkConnectiviry.PipelineA(dataQuery.RoomSet[0]);

                    var roomGraph = checkConnectiviry.RegionGraphList;
                    PrintConnectivity(roomGraph, dataQuery.RoomSet[0]);
                }
            }
        }

        private static void PrintConnectivity(List<List<int>> roomGraph, ThRoomSetModel roomSet)
        {
            for (int i = 0; i < roomGraph.Count; i++)
            {
                var graph = roomGraph[i];
                var roomPl = graph.Select(x => roomSet.Room[x].RoomBoundary.Clone() as Polyline).ToList();
                ThFloorHeatingCoilInsertService.ShowConnectivity(roomPl, ThFloorHeatingCommon.Layer_RoomSetFrame, i % 6);
            }
        }

        public static void DistributeRoute(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThFloorHeatingDistributeCmd(vm))
                {
                    cmd.SubExecute();
                }
            }
        }

        public static void ShowRoute(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThFloorHeatingShowRouteCmd(vm))
                {
                    cmd.SubExecute();
                }
            }
        }

        public static void InsertWaterSeparatorBlk(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkList = new List<string> { ThFloorHeatingCommon.BlkName_WaterSeparator };
                var layerList = new List<string> { ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_WaterSeparator] };
                ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    //  double length = (vm.RouteNum * 2 + 1) * 50;
                    var routeNum = string.Format("{0}路", vm.RouteNum);
                    var dynDic = new Dictionary<string, object>() {
                        { ThFloorHeatingCommon.BlkSettingAttrName_WaterSeparator, routeNum } ,

                    };

                    ThFloorHeatingCoilInsertService.InsertBlk(wcsPt, ThFloorHeatingCommon.BlkName_WaterSeparator, dynDic);
                }
            }
        }

        public static void InsertSuggestBlk(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkList = new List<string> { ThFloorHeatingCommon.BlkName_RoomSuggest };
                var layerList = new List<string> { ThFloorHeatingCommon.BlkLayerDict[ThFloorHeatingCommon.BlkName_RoomSuggest] };
                ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    var suggestDict = vm.SuggestDist;

                    ThFloorHeatingCoilInsertService.InsertSuggestBlock(wcsPt, -1, suggestDict, -1, ThFloorHeatingCommon.BlkName_RoomSuggest);
                }
            }

        }
    }
}
