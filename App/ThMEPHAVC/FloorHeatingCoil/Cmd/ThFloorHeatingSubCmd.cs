﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;

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

                var withUI = ThFloorHeatingCoilSetting.Instance.WithUI;
                var selectFrames = ThSelectFrameUtil.SelectPolyline();

                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = ThFloorHeatingCoilUtilServices.GetTransformer(selectFrames, true);//暂用（0，0，0） 需要改！

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer, withUI);

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

        public static void ShowRoute(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThFloorHeatingShowRouteCmd(vm))
                {
                    cmd.Execute();
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

        public static void AddObstacle(ThFloorHeatingCoilViewModel vm, int addType)
        {
            var selectName = "";
            if (addType == 0)
            {
                selectName = ThFloorHeatingObstacleSettingService.GetNearBlkName();
            }
            else if (addType == 1)
            {
                selectName = ThFloorHeatingObstacleSettingService.GetNearLayerName();
            }

            if (selectName != "" && addType == 0)
            {
                var frames = new List<Polyline>();
                var checkOne = vm.ObstacleBlkNameList.Where(x => x.BlkLayerName == selectName);

                if (checkOne.Any() == false)
                {
                    frames = ThFloorHeatingObstacleSettingService.ExtractBlk(selectName);
                    var blkNameModel = new ObstacleBlkLayerNameModel(selectName);
                    blkNameModel.ObsFrames.AddRange(frames);
                    vm.ObstacleBlkNameList.Add(blkNameModel);
                }
            }
            if (selectName != "" && addType == 1)
            {
                var frames = new List<Polyline>();
                var checkOne = vm.ObstacleLayerNameList.Where(x => x.BlkLayerName == selectName);

                if (checkOne.Any() == false)
                {
                    frames = ThFloorHeatingObstacleSettingService.ExtractLayerLines(selectName);
                    var blkNameModel = new ObstacleBlkLayerNameModel(selectName);
                    blkNameModel.ObsFrames.AddRange(frames);
                    vm.ObstacleLayerNameList.Add(blkNameModel);
                }
            }

            vm.UpdateHighLight();
        }

        public static void RemoveObstacle(ThFloorHeatingCoilViewModel vm, int addType)
        {
            if (addType == 0)
            {
                var selectItem = vm.SelectBlkName;
                if (selectItem != null)
                {
                    vm.ObstacleBlkNameList.Remove(selectItem);
                }
            }
            else
            {
                var selectItem = vm.SelectLayerName;
                if (selectItem != null)
                {
                    vm.ObstacleLayerNameList.Remove(selectItem);
                }
            }

            vm.UpdateHighLight();

        }

        public static void SaveObstacleFrame(ThFloorHeatingCoilViewModel vm)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFloorHeatingCoilInsertService.LoadBlockLayerToDocument(acadDatabase.Database, new List<string>(), new List<string> { ThFloorHeatingCommon.Layer_Obstacle });

                var blkNameList = vm.ObstacleBlkNameList.ToList();
                var layerNameList = vm.ObstacleLayerNameList.ToList();
                var alreadyInObstacle = ThFloorHeatingObstacleSettingService.ExtractLayerLines(ThFloorHeatingCommon.Layer_Obstacle);

                var blkList = GetNeedPrintFrame(blkNameList, alreadyInObstacle);
                var layerList = GetNeedPrintFrame(layerNameList, alreadyInObstacle);

                var print = new List<Polyline>();
                print.AddRange(blkList);
                print.AddRange(layerList);

                vm.ObstacleBlkNameList.Clear();
                vm.ObstacleLayerNameList.Clear();

                ThFloorHeatingCoilInsertService.InsertPolyline(print, ThFloorHeatingCommon.Layer_Obstacle);
                vm.CleanHighlight();
            }
        }

        private static List<Polyline> GetNeedPrintFrame(List<ObstacleBlkLayerNameModel> obstacleList, List<Polyline> alreadyInObstacle)
        {
            var needPrintFrame = new List<Polyline>();

            foreach (var obsModel in obstacleList)
            {
                if (obsModel.BlkLayerName != ThFloorHeatingCommon.Layer_Obstacle)
                {
                    foreach (var frame in obsModel.ObsFrames)
                    {
                        var alreadyIn = false;

                        foreach (var already in alreadyInObstacle)
                        {
                            alreadyIn = frame.IsSimilar(already, 0.90);
                            if (alreadyIn == true)
                            {
                                break;
                            }
                        }
                        if (alreadyIn == false)
                        {
                            needPrintFrame.Add(frame);
                            alreadyInObstacle.Add(frame);
                        }
                    }
                }
            }

            return needPrintFrame;
        }

    }
}
