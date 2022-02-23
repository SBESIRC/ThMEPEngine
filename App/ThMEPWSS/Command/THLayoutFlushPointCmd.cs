using System;
using AcHelper;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPWSS.ViewModel;

#if (ACAD2016 || ACAD2018)
using CLI;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPWSS.FlushPoint;
using ThMEPWSS.FlushPoint.Data;
using ThMEPWSS.FlushPoint.Service;
#endif

namespace ThMEPWSS.Command
{
    public class THLayoutFlushPointCmd : ThMEPBaseCommand, IDisposable
    {
        public static ThFlushPointVM FlushPointVM { get; set; }
        private ThMEPOriginTransformer Transfomer { get; set; } = new ThMEPOriginTransformer(Point3d.Origin);
        private short colorIndex { get; set; } = 1;
        private List<ThIfcRoom> rooms = new List<ThIfcRoom>(); // 房间
        private List<Polyline> columns = new List<Polyline>(); // 柱
        private List<Polyline> parkingStalls = new List<Polyline>(); //停车位
        private List<Entity> walls = new List<Entity>(); //墙：剪力墙 + 建筑墙
        private List<Entity> obstacles = new List<Entity>(); //障碍物

        public THLayoutFlushPointCmd()
        {
            ActionName = "布置";
            CommandName = "THDXCX";
        }

        public void Dispose()
        {
        }

#if (ACAD2016 || ACAD2018)
        public override void SubExecute()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                var pts = GetRange(); //获取布置范围
                if (pts.Count < 3)
                {
                    return;
                }
                Transfomer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
                //收集数据                

                var roomExtractor = GetRoomExtractor(acadDb.Database, pts, ref rooms, ref parkingStalls);
                var extractors = GetExtractors(acadDb.Database, pts, rooms);

                //收集数据
                columns = extractors.Where(o => o is ThColumnExtractor).Select(o => o as ThColumnExtractor).First().Columns;
                walls.AddRange(extractors.Where(o => o is ThShearwallExtractor).Select(o => o as ThShearwallExtractor).First().Walls);
                walls.AddRange(extractors.Where(o => o is ThArchitectureExtractor).Select(o => o as ThArchitectureExtractor).First().Walls);
                obstacles.AddRange(extractors.Where(o => o is ThObstacleExtractor).Select(o => o as ThObstacleExtractor).First()
                    .ObstacleDic.SelectMany(o => o.Value).ToList());
                var drainFacilityExtractor = extractors.Where(o => o is ThDrainFacilityExtractor).First() as ThDrainFacilityExtractor;

                // 获取GeoJson数据
                extractors.Add(roomExtractor);
                var geos = GetGeos(extractors);
                var washPoints = Calculate(geos);

                LayOut(acadDb.Database, washPoints, drainFacilityExtractor);
            }
        }
        /// <summary>
        /// 根据计算的冲洗点位来布置块和标注文字,打印到图纸上
        /// </summary>
        /// <param name="db"></param>
        /// <param name="washPoints">计算的冲洗点位</param>
        /// <param name="drainFacilityExtractor">集水井设施(集水井、排水沟、地漏)</param>
        private void LayOut(Database db, List<Point3d> washPoints,
            ThDrainFacilityExtractor drainFacilityExtractor)
        {
            // 进来的数据都是原位置的，这儿要移动到原点
            washPoints = washPoints.Select(p => Transfomer.Transform(p)).ToList();
            drainFacilityExtractor.CollectingWells.ForEach(c => Transfomer.Transform(c));
            drainFacilityExtractor.DrainageDitches.ForEach(d => Transfomer.Transform(d));
            drainFacilityExtractor.FloorDrains.ForEach(f => Transfomer.Transform(f));
            columns.ForEach(c => Transfomer.Transform(c));
            walls.ForEach(w => Transfomer.Transform(w));
            rooms.ForEach(r => Transfomer.Transform(r.Boundary));
            parkingStalls.ForEach(p => Transfomer.Transform(p));
            obstacles.ForEach(p => Transfomer.Transform(p));

            var filterService = new ThFilterWashPointsService(FlushPointVM.Parameter.NearbyDistance * 1000)
            {
                Rooms = rooms.Select(o => o.Boundary).ToList(),
                DrainFacilityExtractor = drainFacilityExtractor,
            };
            // 过滤哪些点位靠近排水设施，哪些远离排水设施
            // 因暂时不考虑对远离和靠近排水设施的过滤。注释掉相关代码，后期根据需求再打开
            filterService.Filter(washPoints);
            var layoutInfo = filterService.LayoutInfo; //用于保存插入块的结果、靠近/远离排水设施的点 
            var layOutPts = washPoints; //区域满布
            if (FlushPointVM.Parameter.ArrangePosition == ThMEPWSS.FlushPoint.Model.ArrangePositionOps.OnlyDrainageFacility)
            {
                layOutPts = layoutInfo.NearbyPoints; //仅仅排水设施附近
            }
            //调整点位位置               
            var adjustService = new ThAdjustWashPointPositionService(
                columns, parkingStalls, walls, rooms.Select(o => o.Boundary).ToList(), obstacles);
            adjustService.Adjust(layOutPts);

            ThFlushPointUtils.SortWashPoints(layOutPts); //排序

            var layoutData = new WashPointLayoutData()
            {
                Columns = columns.Cast<Entity>().ToList(),
                Walls = walls,
                Rooms = rooms.Select(o => o.Boundary).ToList(),
                Db = db,
                WashPoints = layOutPts,
                PtRange = 10.0,               
            };

            var layoutService = new ThLayoutWashPointBlockService(layoutData);
            layoutInfo.LayoutBlock = layoutService.Layout();
            layoutData.WashPoints = layoutInfo.LayoutBlock.Keys.ToList(); //直接用layOutPts可能存在重复点

            var markService = new ThLayoutWashPointMarkService(layoutData);
            markService.Layout();

            //还原
            washPoints = washPoints.Select(p => Transfomer.Reset(p)).ToList();
            drainFacilityExtractor.CollectingWells.ForEach(c => Transfomer.Reset(c));
            drainFacilityExtractor.DrainageDitches.ForEach(d => Transfomer.Reset(d));
            drainFacilityExtractor.FloorDrains.ForEach(f => Transfomer.Reset(f));
            columns.ForEach(c => Transfomer.Reset(c));
            walls.ForEach(w => Transfomer.Reset(w));
            rooms.ForEach(r => Transfomer.Reset(r.Boundary));
            parkingStalls.ForEach(p => Transfomer.Reset(p));
            obstacles.ForEach(p => Transfomer.Reset(p));

            using (var acadDb = AcadDatabase.Use(db))
            {
                markService.ObjIds.ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o);
                    entity.UpgradeOpen();
                    Transfomer.Reset(entity);
                    entity.DowngradeOpen();
                });
                layoutInfo.NearbyPoints = layoutInfo.NearbyPoints.Select(p => Transfomer.Reset(p)).ToList();
                layoutInfo.FarawayPoints = layoutInfo.FarawayPoints.Select(p => Transfomer.Reset(p)).ToList();
                var newLayoutBlock = new Dictionary<Point3d, BlockReference>();
                foreach (var item in layoutInfo.LayoutBlock)
                {
                    item.Value.UpgradeOpen();
                    Transfomer.Reset(item.Value);
                    newLayoutBlock.Add(Transfomer.Reset(item.Key), item.Value);
                    item.Value.DowngradeOpen();
                }
                layoutInfo.LayoutBlock = newLayoutBlock;
            }

            //点位标识的操作通过以下保存的结果与UI交互操作
            ThPointIdentificationService.LayoutInfo = layoutInfo;
        }
        private Point3dCollection GetRange()
        {
            var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
            if (frame.Area < 1e-4)
            {
                return new Point3dCollection();
            }
            var nFrame = ThMEPFrameService.Normalize(frame);
            return nFrame.Vertices();
        }

        private List<string> QueryBlkNames(string category)
        {
            if (FlushPointVM.Parameter.BlockNameDict.ContainsKey(category))
            {
                return FlushPointVM.Parameter.BlockNameDict[category].Distinct().ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        private ThRoomExtractor GetRoomExtractor(Database db, Point3dCollection pts,
            ref List<ThIfcRoom> rooms, ref List<Polyline> parkingStalls)
        {
            //提取房间
            var roomExtractor = new ThRoomExtractor()
            {
                ColorIndex = colorIndex++,
            };
            roomExtractor.Extract(db, pts);
            rooms = roomExtractor.Rooms;

            //提取停车位
            var parkingStallBlkNames = new List<string>();
            parkingStallBlkNames.AddRange(QueryBlkNames("机械车位"));
            parkingStallBlkNames.AddRange(QueryBlkNames("非机械车位"));
            var parkingStallExtractor = new ThParkingStallExtractor()
            {
                BlockNames = parkingStallBlkNames,
            };
            parkingStallExtractor.Extract(db, pts);
            parkingStalls = parkingStallExtractor.ParkingStalls.Cast<Polyline>().ToList();

            rooms.ForEach(r => Transfomer.Transform(r.Boundary));
            parkingStalls.ForEach(p => Transfomer.Transform(p));

            var resetService = new ThRoomNameResetService(rooms, parkingStalls.ToCollection());
            resetService.Reset();

            rooms.ForEach(r => Transfomer.Reset(r.Boundary));
            parkingStalls.ForEach(p => Transfomer.Reset(p));
            return roomExtractor;
        }

        private List<ThExtractorBase> GetExtractors(Database db, Point3dCollection pts, List<ThIfcRoom> rooms)
        {
            var floorDrainBlkNames = QueryBlkNames("地漏");
            var extractors = new List<ThExtractorBase>()
                {
                    new ThFpColumnExtractor(){ ColorIndex=colorIndex++, },
                    new ThFpShearwallExtractor(){ ColorIndex=colorIndex++, },
                    new ThFpArchitectureExtractor(){ ColorIndex=colorIndex++, },
                    new ThObstacleExtractor(){ ColorIndex=colorIndex++,},
                    new ThDrainFacilityExtractor()
                    {
                        ColorIndex=5,
                        FloorDrainBlkNames = floorDrainBlkNames,
                        DrainageBlkNames =QueryBlkNames("集水井")
                    },
                };
            extractors.ForEach(o => o.SetRooms(rooms));
            extractors.ForEach(o => o.Extract(db, pts));
            return extractors;
        }

        private List<ThGeometry> GetGeos(List<ThExtractorBase> extractors)
        {
            var geos = new List<ThGeometry>();
            extractors.ForEach(o =>
            {
                if (FlushPointVM.Parameter.OnlyLayoutOnColumn)
                {
                    //仅布置在柱子上，不需要剪力墙和建筑墙的数据
                    if (!(o is ThFpShearwallExtractor || o is ThFpArchitectureExtractor))
                    {
                        geos.AddRange(o.BuildGeometries());
                    }
                }
                else
                {
                    geos.AddRange(o.BuildGeometries());
                }
            });
            return geos;
        }

        private ThWashParamMgd BuildWashParam()
        {
            var washPara = new ThWashParamMgd();
            // 保护半径
            washPara.R = (int)FlushPointVM.Parameter.ProtectRadius;
            // 建筑空间（隔油池、水泵房、垃圾房等）
            washPara.protect_arch = FlushPointVM.Parameter.NecessaryArrangeSpaceOfProtectTarget;
            // 停车区域
            washPara.protect_park = FlushPointVM.Parameter.ParkingAreaOfProtectTarget;
            // 其它空间
            washPara.protect_other = FlushPointVM.Parameter.OtherSpaceOfProtectTarget;
            // 必布空间的点位可以保护停车区域和其他空间
            washPara.extend_arch = FlushPointVM.Parameter.NecesaryArrangeSpacePointsOfArrangeStrategy;
            // 停车区域的点位可以保护其他空间
            washPara.extend_park = FlushPointVM.Parameter.ParkingAreaPointsOfArrangeStrategy;

            // 设置点位布置的位置
            if (FlushPointVM.Parameter.OnlyLayoutOnColumn)
            {
                washPara.locate_mode = ThWashLocateMode.Interal;
            }
            else
            {
                washPara.locate_mode = ThWashLocateMode.All;
            }
            return washPara;
        }

        private List<Point3d> Calculate(List<ThGeometry> geos)
        {
            //通过传入的GeoJson数据计算要布置的点位
            var washPara = BuildWashParam(); //UI参数
            var geoContent = ThGeoOutput.Output(geos); //数据
            var washData = new ThWashGeoDataMgd();
            washData.ReadFromContent(geoContent);

            var washPointEngint = new ThWashPointLayoutEngineMgd();
            var result = washPointEngint.Run(washData, washPara);
            return ThWashPointResultParseService.Parse(result);
        }
#else
        public override void SubExecute()
        {
        }
#endif
    }
}
