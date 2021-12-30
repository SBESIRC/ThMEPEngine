using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using NFox.Cad;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;
using Autodesk.AutoCAD.EditorInput;
using DotNetARX;
using Dreambuild.AutoCAD;
using System;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.SprinklerConnect.Cmd
{
    public class ThSprinklerConnectCmd_test : ThMEPBaseCommand, IDisposable
    {
        public static Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public bool ParameterFromUI { get; set; } 
        public string LayoutDirection { get; set; }
        public ThSprinklerConnectCmd_test()
        {
            ActionName = "生成支管";
            CommandName = "THPTLGBZ";       
        }

        public override void SubExecute()
        {
            SprinklerConnectExecute();
        }

        public void SprinklerConnectExecute()
        {
            using (var doclock = Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frames = ThSprinklerConnectUtils.GetFrames();
                var isVertical = true;
                if(ParameterFromUI)
                {
                    isVertical = LayoutDirection=="垂直";
                }
                else
                {
                    var options = new PromptKeywordOptions("\n选择连接方式");
                    options.Keywords.Add("垂直车道", "V", "垂直车道(V)");
                    options.Keywords.Add("平行车道", "P", "平行车道(P)");
                    options.Keywords.Default = "垂直车道";
                    var result2 = Active.Editor.GetKeywords(options);
                    if (result2.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    if (result2.StringResult == "平行车道")
                    {
                        isVertical = false;
                    }
                }

                var frameIndex = new ThCADCoreNTSSpatialIndex(frames.ToCollection());
                var frameClone = frameIndex.SelectAll().OfType<Polyline>();

                // 提取数据
                var allSprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData();
                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, new Point3dCollection()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();
                var archIndex = new ThCADCoreNTSSpatialIndex(dataQuery.ArchitectureWallList.ToCollection());
                var shearIndex = new ThCADCoreNTSSpatialIndex(dataQuery.ShearWallList.ToCollection());
                var columnIndex = new ThCADCoreNTSSpatialIndex(dataQuery.ColumnList.ToCollection());
                var roomIndex = new ThCADCoreNTSSpatialIndex(dataQuery.RoomList.ToCollection());
                var geometryWithoutColumn = new List<Polyline>();
                var obstacle = new List<Polyline>();

                // 提取车位
                var parkingStallService = new ThSprinklerConnectParkingStallService();
                parkingStallService.BlockNameDict = BlockNameDict;
                parkingStallService.ParkingStallExtractor(acadDatabase.Database, new Polyline());

                foreach (var frame in frameClone)
                {
                    if (frame == null || frame.Area < 10)
                    {
                        continue;
                    }

                    var room = roomIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ToList();
                    var exactFrames = new DBObjectCollection
                    {
                        frame,
                    };

                    //简略的写提取管子和点位（需要改）
                    var sprinklerPts = new List<Point3d>();
                    room.ToList().ForEach(r =>
                    {
                        var difference = r.Difference(frame).OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
                        if (difference == null || difference.Area / r.Area < 0.25)
                        {
                            exactFrames.Add(r);
                            sprinklerPts.AddRange(allSprinklerPts.Where(pt => r.Contains(pt)).ToList());
                        }
                    });

                    var exactFrame = exactFrames.Outline().OfType<Polyline>().OrderByDescending(o => o.Area).First();
                    var mainPipe = ThSprinklerConnectDataFactory.GetPipeData(exactFrame, ThWSSCommon.Sprinkler_Connect_MainPipe);
                    var subMainPipe = ThSprinklerConnectDataFactory.GetPipeData(exactFrame, ThWSSCommon.Sprinkler_Connect_SubMainPipe);
                    var mainPipeLine = ThSprinklerConnectDataFactory.GetPipeLineData(exactFrame, ThWSSCommon.Sprinkler_Connect_MainPipe);
                    var subMainPipeLine = ThSprinklerConnectDataFactory.GetPipeLineData(exactFrame, ThWSSCommon.Sprinkler_Connect_SubMainPipe);

                    //打散管线
                    ThSprinklerPipeService.ThSprinklerPipeToLine(mainPipe, subMainPipe, out var mainLine, out var subMainLine, out var allLines);
                    mainLine.AddRange(mainPipeLine);
                    subMainLine.AddRange(subMainPipeLine);
                    allLines.AddRange(mainPipeLine);
                    allLines.AddRange(subMainPipeLine);

                    if (sprinklerPts.Count == 0 || subMainPipe.Count == 0)
                    {
                        continue;
                    }

                    var sprinklerParameter = new ThSprinklerParameter
                    {
                        SprinklerPt = sprinklerPts.DistinctPoints(),
                        MainPipe = mainLine,
                        SubMainPipe = subMainLine,
                        AllPipe = allLines
                    };

                    CleanPipe(ThWSSCommon.Sprinkler_Connect_Pipe, exactFrame);

                    geometryWithoutColumn = new List<Polyline>();
                    obstacle = new List<Polyline>();
                    var architectureWall = archIndex.SelectCrossingPolygon(exactFrame).OfType<Polyline>().ToList();
                    var shearWall = shearIndex.SelectCrossingPolygon(exactFrame).OfType<Polyline>().ToList();
                    var column = columnIndex.SelectCrossingPolygon(exactFrame).OfType<Polyline>().ToList();

                    geometryWithoutColumn.AddRange(architectureWall);
                    geometryWithoutColumn.AddRange(shearWall);
                    geometryWithoutColumn.AddRange(room);
                    obstacle.AddRange(shearWall);
                    obstacle.AddRange(column);

                    var smallRooms = room.Where(r => r.Area < 1.5e8).ToList();
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

                    //转回原点
                    //var transformer = ThSprinklerConnectUtil.transformToOrig(pts, geos);

                    var bufferArea = 1.0;
                    var roomsArea = 1.0;
                    if (room.Count() > 0)
                    {
                        var lanelineWidth = 3500.0;
                        var largestRooms = room.Where(r => r.Area >= 1.5e8).ToList();
                        roomsArea = largestRooms.Select(r => r.Area).Sum();
                        var buffer = largestRooms.Select(r => r.Buffer(-lanelineWidth).Buffer(lanelineWidth).OfType<Polyline>()).ToList();
                        bufferArea = buffer.Select(r => r.Select(room => room.Area).Sum()).Sum();
                    }

                    var engine = new ThSprinklerConnectEngine();
                    if (bufferArea / roomsArea > 0.6)
                    {
                        //提取车位外包框
                        var reducedFrame = exactFrame.Buffer(-50.0).OfType<Polyline>().OrderByDescending(o => o.Area).First();
                        var doubleStall = parkingStallService.GetParkingStallOBB(reducedFrame);
                        var layerName = "AI-车位排-双排";
                        CleanPline(layerName, reducedFrame);
                        StallPresent(doubleStall, layerName);
                        //var doubleStall = ThSprinklerConnectDataFactory.GetCarData(reducedFrame, ThSprinklerConnectCommon.Layer_DoubleCar);

                        engine.SprinklerConnectEngine(sprinklerParameter, geometryWithoutColumn, doubleStall, smallRoomsWithSpr,
                            obstacle, column, isVertical);
                    }
                    else
                    {
                        engine.SprinklerConnectEngine(sprinklerParameter, geometryWithoutColumn, new List<Polyline>(), smallRoomsWithSpr,
                            obstacle, column, isVertical);
                    }
                }

                //Active.Editor.Regen();
            }
        }

        private void StallPresent(List<Polyline> results, string layerName)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAILayer(layerName, 5);
                results.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                });
            }
        }

        public void CleanPipe(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = new DBObjectCollection();
                // 图层上的所有图元
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .Where(o => o.Layer == layerName)
                    .ForEach(o => objs.Add(o));

                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<Entity>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }

        public void CleanPline(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == layerName).ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<Polyline>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
