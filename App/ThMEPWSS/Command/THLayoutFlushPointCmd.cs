using System;
using NFox.Cad;
using AcHelper;
using AcHelper.Commands;
using ThMEPWSS.ViewModel;
using ThMEPWSS.FlushPoint.Model;
using ThMEPEngineCore.GeojsonExtractor;

#if (ACAD2016 || ACAD2018)
using CLI;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.FlushPoint;
using ThMEPWSS.FlushPoint.Data;
using ThMEPWSS.FlushPoint.Service;
#endif

namespace ThMEPWSS.Command
{
    public class THLayoutFlushPointCmd : IAcadCommand, IDisposable
    {
        public static ThFlushPointVM FlushPointVM { get; set; }
        public void Dispose()
        {
        }

#if (ACAD2016 || ACAD2018)
        public void Execute()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(frame);
                var pts = nFrame.Vertices();
                //收集数据
                var parkingStalls = new List<Polyline>(); //停车位
                var walls = new List<Entity>(); //墙：剪力墙 + 建筑墙
                var columns = new List<Polyline>(); // 柱
                var rooms = new List<ThIfcRoom>(); // 房间

                //提取房间
                var roomExtractor = new ThRoomExtractor()
                {
                    ColorIndex = 6,
                };
                roomExtractor.Extract(acadDb.Database, pts);
                rooms = roomExtractor.Rooms;

                //提取停车位
                var parkingStallExtractor = new ThParkingStallExtractor();
                parkingStallExtractor.Extract(acadDb.Database, pts);
                parkingStalls = parkingStallExtractor.ParkingStalls.Cast<Polyline>().ToList();
                var resetService = new ThRoomNameResetService(roomExtractor.Rooms,
                    parkingStallExtractor.ParkingStalls.ToCollection());
                resetService.Reset();

                var extractors = new List<ThExtractorBase>()
                {
                    new ThFpColumnExtractor(){ ColorIndex=1},
                    new ThFpShearwallExtractor(){ ColorIndex=2},
                    new ThFpArchitectureExtractor(){ ColorIndex=3},
                    new ThObstacleExtractor(){ ColorIndex=4,},
                    new ThDrainFacilityExtractor(){ ColorIndex=5,},
                };
                extractors.ForEach(o => o.SetRooms(rooms));
                extractors.ForEach(o => o.Extract(acadDb.Database, pts));

                //收集数据
                columns = (extractors[0] as ThColumnExtractor).Columns;
                walls.AddRange((extractors[1] as ThShearwallExtractor).Walls);
                walls.AddRange((extractors[2] as ThArchitectureExtractor).Walls);

                var geos = new List<ThGeometry>();
                extractors.Add(roomExtractor);
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

                var washPara = BuildWashParam(); //UI参数
                var geoContent = ThGeoOutput.Output(geos); //数据
                var washData = new ThWashGeoDataMgd();
                washData.ReadFromContent(geoContent);

                var washPointEngint = new ThWashPointLayoutEngineMgd();
                var result = washPointEngint.Run(washData, washPara);
                var washPoints = ThWashPointResultParseService.Parse(result);

                // 过滤哪些点位靠近排水设施，哪些远离排水设施
                // 因暂时不考虑对远离和靠近排水设施的过滤。注释掉相关代码，后期根据需求再打开
                var filterService = new ThFilterWashPointsService()
                {
                    Rooms = roomExtractor.Rooms.Select(o=>o.Boundary).ToList(),
                    DrainFacilityExtractor = extractors[4] as ThDrainFacilityExtractor,
                };
                //filterService.Filter(washPoints);
                var layoutInfo = filterService.LayoutInfo; //用于保存插入块的结果、靠近/远离排水设施的点 

                var layOutPts = washPoints; //区域满布
                //if (FlushPointVM.Parameter.ArrangePosition == ArrangePositionOps.OnlyDrainageFacility)
                //{
                //    layOutPts = layoutInfo.NearbyPoints; //仅仅排水设施附近
                //}
                //调整点位位置               
                var adjustService = new ThAdjustWashPointPositionService(columns, parkingStalls, walls);
                adjustService.Adjust(layOutPts);

                // 打印块
                ThFlushPointUtils.SortWashPoints(layOutPts);

                var layoutData = new WashPointLayoutData()
                {
                    Columns = columns.Cast<Entity>().ToList(),
                    Walls = walls,
                    Rooms = rooms.Select(o => o.Boundary).ToList(),
                    Db = acadDb.Database,
                    WashPoints = layOutPts,
                    PtRange = 10.0,
                };
                var layoutService = new ThLayoutWashPointBlockService(layoutData);
                layoutInfo.LayoutBlock = layoutService.Layout();
                layoutData.WashPoints = layoutInfo.LayoutBlock.Keys.ToList(); //直接用layOutPts可能存在重复点

                var markService = new ThLayoutWashPointMarkService(layoutData);
                markService.Layout();

                //点位标识的操作通过以下保存的结果与UI交互操作
                ThPointIdentificationService.LayoutInfo = layoutInfo;
            }
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
            if(FlushPointVM.Parameter.OnlyLayoutOnColumn)
            {
                washPara.locate_mode = ThWashLocateMode.Interal;
            }
            else
            {
                washPara.locate_mode = ThWashLocateMode.All;
            }
            return washPara;
        }
#else
        public void Execute()
        {
        }
#endif
    }
}
