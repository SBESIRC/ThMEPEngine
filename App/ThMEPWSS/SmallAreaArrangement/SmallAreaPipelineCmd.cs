﻿using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.DrainageSystemDiagram;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;
using System.Collections.Generic;
using THMEPWSS.RoomOrderingService;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using ThMEPWSS.SprinklerConnect;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;
using DotNetARX;
using NFox.Cad;

namespace ThMEPWSS.SmallAreaArrangement.Cmd
{
    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "THSmallRoomPipeline", CommandFlags.Modal)]
        public void SmallAreaArrange()
        {
            var cmd = new SmallAreaPipelineCmd();
            cmd.Execute();
        }

           
    }
    public class SmallAreaPipelineCmd : ThMEPBaseCommand
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();

        public SmallAreaPipelineCmd()
        {

        }

        public override void SubExecute()
        {
            SprinklerConnectExecute();
        }

        public void SprinklerConnectExecute()
        {
            //using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            //using (AcadDatabase acadDatabase = AcadDatabase.Active())
            //{

            //    var frame = ThSprinklerDataService.GetFrame();
            //    if (frame == null || frame.Area < 10)
            //    {
            //        return;
            //    }

            //    //简略的写提取管子和点位（需要改）
            //    var sprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);

            //    if (sprinklerPts.Count == 0 )
            //    {
            //        return;
            //    }


            //    var dataset = new ThSprinklerConnectDataFactory();
            //    var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
            //    var dataQuery = new ThSprinklerDataQueryService(geos);
            //    dataQuery.ClassifyData();

            //    var geometry = new List<Polyline>();
            //    geometry.AddRange(dataQuery.ArchitectureWallList);
            //    geometry.AddRange(dataQuery.ShearWallList);
            //    geometry.AddRange(dataQuery.ColumnList);
            //    geometry.AddRange(dataQuery.RoomList);

            //    //var smallRoom = dataQuery.RoomList.Where(r => r.Area < 1e8).ToList();
            //    var bound = 1e8;
            //    var ptInSmallRoom = new HashSet<Point3d>();

            //    var orderService = new OrderingService();
            //    orderService.sprinkler.AddRange(sprinklerPts);
            //    orderService.PipelineArrange(dataQuery.RoomList, dataQuery.ShearWallList, bound);
            //    var allLines = orderService.gridLines;
            //    var allGrid = orderService.orthogonalGrid;
            //    //List<Polyline> temp1 = new List<Polyline>();
            //    //List<Polyline> temp2 = new List<Polyline>();
            //    //allGrid.ForEach(o => o.ForEach(l => temp1.Add(l.Buffer(1))));
            //    //allLines.ForEach(o => o.ForEach(l => temp2.Add(l.Buffer(1))));
            //    //var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(ptInSmallRoom.ToList(), out var dtSeg);
            //    //DrawUtils.ShowGeometry(temp1, "gridBuffered", 6);
            //    //DrawUtils.ShowGeometry(temp2, "linesBuffered", 1);
            //    allGrid.ForEach(o => DrawUtils.ShowGeometry(o, "OrthorDT", 1));
            //    allLines.ForEach(o=>DrawUtils.ShowGeometry(o, "AllDT", 3));
            //    DrawUtils.ShowGeometry(dataQuery.RoomList, "roomBoundary", 5);
            //    DrawUtils.ShowGeometry(dataQuery.ShearWallList, "shearwall", 4);
            //}


            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThSprinklerDataService.GetFrame();
                if (frame == null || frame.Area < 10)
                {
                    return;
                }

                var options = new PromptKeywordOptions("\n选择连接方式");
                options.Keywords.Add("垂直车道", "V", "垂直车道(V)");
                options.Keywords.Add("平行车道", "P", "平行车道(P)");
                options.Keywords.Default = "垂直车道";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var isVertical = true;
                if (result2.StringResult == "平行车道")
                {
                    isVertical = false;
                }

                CleanLine("AI-喷淋连管", frame);

                //简略的写提取管子和点位（需要改）
                var sprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);
                var mainPipe = ThSprinklerConnectDataFactory.GetPipeData(frame, ThSprinklerConnectCommon.Layer_MainPipe);
                var subMainPipe = ThSprinklerConnectDataFactory.GetPipeData(frame, ThSprinklerConnectCommon.Layer_SubMainPipe);

                if (sprinklerPts.Count == 0 || subMainPipe.Count == 0)
                {
                    return;
                }

                //打散管线
                ThSprinklerPipeService.ThSprinklerPipeToLine(mainPipe, subMainPipe, out var mainLine, out var subMainLine, out var allLines);
                //DrawUtils.ShowGeometry(mainLine, "l0mainline", 22, 30);
                //DrawUtils.ShowGeometry(subMainLine, "l0submainline", 142, 30);
                //DrawUtils.ShowGeometry(allLines, "l0all", 2, 30);

                var sprinklerParameter = new ThSprinklerParameter();
                sprinklerParameter.SprinklerPt = DistinctSprinkler(sprinklerPts);
                sprinklerParameter.MainPipe = mainLine;
                sprinklerParameter.SubMainPipe = subMainLine;
                sprinklerParameter.AllPipe = allLines;

                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();

                var geometry = new List<Polyline>();
                var obstacle = new List<Polyline>();
                geometry.AddRange(dataQuery.ArchitectureWallList);
                geometry.AddRange(dataQuery.ShearWallList);
                geometry.AddRange(dataQuery.ColumnList);
                geometry.AddRange(dataQuery.RoomList);
                obstacle.AddRange(dataQuery.ShearWallList);
                obstacle.AddRange(dataQuery.ColumnList);

                var smallRooms = dataQuery.RoomList.Where(r => r.Area < 1.5e8).ToList();
                var smallRoomsWithSpr = new List<Polyline>();
                // 将有无喷头的小房间进行分类
                var sprinklerIndex = new ThCADCoreNTSSpatialIndex(sprinklerParameter.SprinklerPt.Select(pt => new DBPoint(pt)).ToCollection());
                smallRooms.ForEach(r =>
                {
                    var filter = sprinklerIndex.SelectCrossingPolygon(r);
                    if (filter.Count > 0)
                    {
                        smallRoomsWithSpr.Add(r);
                    }
                    else
                    {
                        obstacle.Add(r);
                    }
                });

                //geometry.ForEach(g => acadDatabase.ModelSpace.Add(g));

                //转回原点
                //var transformer = ThSprinklerConnectUtil.transformToOrig(pts, geos);

                var bufferArea = 1.0;
                var roomsArea = 1.0;
                if (dataQuery.RoomList.Count > 0)
                {
                    var lanelineWidth = 3500.0;
                    var largestRooms = dataQuery.RoomList.Where(r => r.Area >= 1.5e8).ToList();
                    roomsArea = largestRooms.Select(r => r.Area).Sum();
                    var buffer = largestRooms.Select(r => r.Buffer(-lanelineWidth).Buffer(lanelineWidth).OfType<Polyline>()).ToList();
                    bufferArea = buffer.Select(r => r.Select(room => room.Area).Sum()).Sum();
                }

                var engine = new ThSprinklerConnectEngine(sprinklerParameter, geometry);
                if (bufferArea / roomsArea > 0.6)
                {
                    //提取车位外包框
                    //var parkingStallService = new ThSprinklerConnectParkingStallService();
                    //parkingStallService.BlockNameDict = BlockNameDict;
                    //var doubleStall = parkingStallService.GetParkingStallOBB(acadDatabase.Database, frame);
                    //var layerName = "AI-车位排-双排";
                    //CleanPline(layerName, frame);
                    //StallPresent(doubleStall, layerName);
                    var doubleStall = ThSprinklerConnectDataFactory.GetCarData(frame, ThSprinklerConnectCommon.Layer_DoubleCar);

                    engine.SprinklerConnectEngine(doubleStall, smallRoomsWithSpr, obstacle, isVertical);
                }
                else
                {
                    engine.SprinklerConnectEngine(new List<Polyline>(), smallRoomsWithSpr, obstacle, isVertical);
                }

                Active.Editor.Regen();
            }

        }
        private List<Point3d> DistinctSprinkler(List<Point3d> list)
        {
            var kdTree = new ThCADCoreNTSKdTree(1.0);
            list.ForEach(o => kdTree.InsertPoint(o));
            return kdTree.SelectAll().OfType<Point3d>().ToList();
        }

        public void CleanLine(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<Line>()
                    .Where(o => o.Layer == layerName).ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<Line>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }

    }
}
