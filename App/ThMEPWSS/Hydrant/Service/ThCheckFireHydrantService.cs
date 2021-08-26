#if (ACAD2016 || ACAD2018)
using CLI;
using System;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThCheckFireHydrantService : ICheck
    {
        public List<ThIfcRoom> Rooms { get; set; }
        public List<Tuple<Entity, Point3d, List<Entity>>> Covers { get; set; }
        private ThFireHydrantVM FireHydrantVM { get; set; }
        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireHydrantService(ThFireHydrantVM fireHydrantVM)
        {
            FireHydrantVM = fireHydrantVM;
            Rooms = new List<ThIfcRoom>();
            Covers = new List<Tuple<Entity, Point3d, List<Entity>>>();
            AiLayerManager = ThHydrantExtractLayerManager.Config();
        }

        public void Check(Database db, Point3dCollection pts, string mode)
        {
            ThStopWatchService.Start();
            var extractors = FirstExtract(db, pts); //获取数据
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间

            var ptsList = new List<Point3dCollection> { };
            if (mode == "P")
            {
                Rooms.ForEach(r => ptsList.Add(GetRoomBounds(r)));
            }
            else
            {
                ptsList.Add(pts);
            }

            SecondExtract(db, ptsList, extractors);
            ThStopWatchService.Stop();
            ThStopWatchService.Print("提取数据耗时：");

            ThStopWatchService.ReStart();
            //过滤只连接一个房间框线的门
            var doorOpeningExtractor = extractors
                .Where(o => o is ThHydrantDoorOpeningExtractor)
                .First() as ThHydrantDoorOpeningExtractor;
            doorOpeningExtractor.FilterOuterDoors(Rooms.Select(o => o.Boundary).ToList());

            var fireHydrantExtractor = extractors.Where(o => o is ThFireHydrantExtractor).First() as ThFireHydrantExtractor;
            if (fireHydrantExtractor.FireHydrants.Count == 0)
            {
                return;
            }
            string geoContent = OutPutGeojson(extractors);
            var context = BuildHydrantParam();
            var hydrant = new ThHydrantEngineMgd();
            var regions = hydrant.Validate(geoContent, context);
            Covers = ThHydrantResultParseService.Parse(regions);
            ThStopWatchService.Stop();
            ThStopWatchService.Print("保护区域计算耗时：");
        }

        private List<ThExtractorBase> FirstExtract(Database db, Point3dCollection pts)
        {
            //提取房间和外部空间
            var extractors = new List<ThExtractorBase>()
                {
                    new ThExternalSpaceExtractor()
                    {
                        UseDb3Engine=false,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.OuterBoundaryLayer,
                    },
                    new ThRoomExtractor()
                    {
                        UseDb3Engine=true,
                        FilterMode = FilterMode.Cross,
                    }
                };
            extractors.ForEach(o => o.Extract(db, pts));
            //调整不在房间内的消火栓的点位
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            return extractors;
        }

        private void SecondExtract(Database db, List<Point3dCollection> ptsList, List<ThExtractorBase> extractors)
        {
            //提取其余建筑元素
            var extractorsContainer = new List<ThExtractorBase>()
                {
                    new ThHydrantArchitectureWallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.ArchitectureWallLayer,
                    },
                    new ThHydrantShearwallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        FilterMode = FilterMode.Cross,
                        ElementLayer=AiLayerManager.ShearWallLayer,
                    },
                    new ThHydrantDoorOpeningExtractor()
                    {
                        UseDb3Engine=false,
                        FilterMode = FilterMode.Cross,
                        ElementLayer = "AI-Door,AI-门,门",
                    },
                    new ThFireHydrantExtractor()
                    {
                        FilterMode = FilterMode.Cross,
                    }
                };
            if (FireHydrantVM.Parameter.IsThinkIsolatedColumn)
            {
                extractorsContainer.Add(new ThColumnExtractor()
                {
                    UseDb3Engine = true,
                    IsolateSwitch = true,
                    FilterMode = FilterMode.Cross,
                    ElementLayer = AiLayerManager.ColumnLayer,
                });
            }
            ptsList.ForEach(p =>
            {
                extractorsContainer.ForEach(e => e.Extract(db, p));
            });

            //调整不在房间内的消火栓的点位
            extractors.AddRange(extractorsContainer);
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            var hydrantExtractor = extractors.Where(o => o is ThFireHydrantExtractor).First() as ThFireHydrantExtractor;
            hydrantExtractor.AdjustFireHydrantPosition(roomExtractor.Rooms);
        }

        private string OutPutGeojson(List<ThExtractorBase> extractors)
        {
            //用于孤立判断
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间

            extractors.ForEach(o => o.SetRooms(Rooms));

            //用于判断私立空间或公立空间
            IRoomPrivacy privacyCheck = new ThJudgeRoomPrivacyService();
            roomExtractor.iRoomPrivacy = privacyCheck;

            //输出Geojson
            var geos = new List<ThGeometry>();
            extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));
            return ThGeoOutput.Output(geos);
        }

        private ThProtectionContextMgd BuildHydrantParam()
        {
            return new ThProtectionContextMgd()
            {
                HydrantClearanceSampleLength = 1000.0,
                HydrantHoseLength = FireHydrantVM.Parameter.FireHoseWalkRange,
                HydrantClearanceRadius = FireHydrantVM.Parameter.SprayWaterColumnRange
            };
        }
        private List<Point3d> ContainsPts(Entity polygon, List<Point3d> pts)
        {
            return pts.Where(p => polygon.IsContains(p)).ToList();
        }
        public void Print(Database db)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(db))
            {
                int colorIndex = 1;
                Covers.ForEach(o =>
                {
                    var ents = new List<Entity>();
                    var cover = o.Item1.Clone() as Entity;
                    cover.Layer = ThCheckExpressionControlService.CheckExpressionLayer;
                    ents.Add(cover);
                    var circle = new Circle(o.Item2, Vector3d.ZAxis, 200.0);
                    circle.Layer = ThCheckExpressionControlService.CheckExpressionLayer;
                    ents.Add(circle);
                    ents.CreateGroup(acadDb.Database, (colorIndex++) % 256);
                });
            }
        }

        private Point3dCollection GetRoomBounds(ThIfcRoom room)
        {
            var extent = new Extents3d(room.Boundary.Bounds.Value.MinPoint, room.Boundary.Bounds.Value.MaxPoint);
            return new Point3dCollection
                    {
                        new Point3d(extent.MinPoint.X + 10,extent.MinPoint.Y + 10,extent.MinPoint.Z),
                        new Point3d(extent.MaxPoint.X + 10, extent.MinPoint.Y + 10, extent.MinPoint.Z),
                        new Point3d(extent.MaxPoint.X + 10,extent.MaxPoint.Y + 10,extent.MaxPoint.Z),
                        new Point3d(extent.MinPoint.X, extent.MaxPoint.Y, extent.MaxPoint.Z),
                        new Point3d(extent.MinPoint.X + 10,extent.MinPoint.Y + 10,extent.MinPoint.Z)
                    };
        }
    }
}
#endif
