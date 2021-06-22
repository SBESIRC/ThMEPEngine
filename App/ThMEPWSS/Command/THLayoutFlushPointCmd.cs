using System;
using NFox.Cad;
using AcHelper;
using AcHelper.Commands;
using ThMEPWSS.ViewModel;
using ThMEPWSS.FlushPoint.Model;
using ThMEPEngineCore.GeojsonExtractor;

#if ACAD2016
using CLI;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.FlushPoint;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPWSS.FlushPoint.Data;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace ThMEPWSS.Command
{
    public class THLayoutFlushPointCmd : IAcadCommand, IDisposable
    {
        public static ThFlushPointVM FlushPointVM { get; set; }
        public void Dispose()
        {
        }

#if ACAD2016
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
                var roomExtractor = new ThRoomExtractor()
                {
                    ColorIndex = 6,
                    RoomMarkLayerFilter = new List<string> { "AI-房间名称" },
                    RoomBoundaryLayerFilter = new List<string> { "AI-房间框线" },
                };
                roomExtractor.Extract(acadDb.Database, pts);
                var parkingStallExtractor = new ThParkingStallExtractor();
                parkingStallExtractor.Extract(acadDb.Database, pts);
                var resetService = new ThRoomNameResetService(roomExtractor.Rooms,
                    parkingStallExtractor.ParkingStalls.ToCollection());
                resetService.Reset();

                var extractors = new List<ThExtractorBase>()
                {
                    new ThColumnExtractor(){ ColorIndex=1,IsolateSwitch=true},
                    new ThShearwallExtractor(){ ColorIndex=2,IsolateSwitch=true},
                    new ThArchitectureExtractor(){ ColorIndex=3,IsolateSwitch=true},
                    new ThObstacleExtractor(){ ColorIndex=4,},
                    new ThDrainFacilityExtractor(){ ColorIndex=5,},
                };
                extractors.ForEach(o => o.SetRooms(roomExtractor.Rooms));
                extractors.ForEach(o => o.Extract(acadDb.Database, pts));

                var geos = new List<ThGeometry>();
                extractors.Add(roomExtractor);
                extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));

                //extractors.ForEach(o => (o as IPrint).Print(acadDb.Database));
                //ThFlushPointUtils.OutputGeo(Active.Document.Name, geos);

                var washPara = BuildWashParam(); //UI参数
                var geoContent = ThGeoOutput.Output(geos); //数据
                var washData = new ThWashGeoData();
                washData.ReadFromContent(geoContent);

                var washPointEngint = new ThWashPointLayoutEngine();
                double[] points = washPointEngint.Layout(washData, washPara);
                var washPoints = ThFlushPointUtils.GetPoints(points);

                // 过滤哪些点位靠近排水设施，哪些远离排水设施
                var filterService = new ThFilterWashPointsService()
                {
                    DrainFacilityExtractor = extractors[4] as ThDrainFacilityExtractor,
                };
                filterService.Filter(washPoints);

                var layoutInfo = filterService.LayoutInfo; //用于保存插入块的结果、靠近/远离排水设施的点 

                var layOutPts = washPoints; //区域满布
                if (FlushPointVM.Parameter.ArrangePosition == ArrangePositionOps.OnlyDrainageFacility)
                {
                    layOutPts = layoutInfo.NearbyPoints; //仅仅排水设施附近
                }

                // 打印块
                ThFlushPointUtils.SortWashPoints(layOutPts);
                var columns = (extractors[0] as ThColumnExtractor).Columns;
                var walls = new List<Entity>();
                walls.AddRange((extractors[1] as ThShearwallExtractor).Walls);
                walls.AddRange((extractors[2] as ThArchitectureExtractor).Walls);
                var layoutData = new WashPointLayoutData()
                {
                    Columns = columns.Cast<Entity>().ToList(),
                    Walls = walls,
                    Rooms = roomExtractor.Rooms.Select(o => o.Boundary).ToList(),
                    WashPointBlkName = "给水角阀平面",
                    WashPointLayerName = "W-WSUP-EQPM",
                    WashPoints = layOutPts,
                    Db = acadDb.Database,
                    PtRange = 10.0,
                };
                var layoutService = new ThLayoutWashPointBlockService(layoutData);
                layoutInfo.LayoutBlock = layoutService.Layout();

                var markService = new ThLayoutWashPointMarkService(layoutData);
                markService.Layout();

                //点位标识的操作通过以下保存的结果与UI交互操作
                ThPointIdentificationService.LayoutInfo = layoutInfo;
            }
        }
        private ThWashParam BuildWashParam()
        {
            var washPara = new ThWashParam();
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
            return washPara;
        }
#else
        public void Execute()
        {
        }
#endif
    }
}
