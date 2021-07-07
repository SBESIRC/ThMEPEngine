#if ACAD_ABOVE_2016
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

        public void Check(Database db, Point3dCollection pts)
        {
            var extractors = Extract(db, pts); //获取数据
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间

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
        }

        private List<ThExtractorBase> Extract(Database db, Point3dCollection pts)
        {
            //提取
            var extractors = new List<ThExtractorBase>()
                {
                    new ThArchitectureExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        ElementLayer=AiLayerManager.ArchitectureWallLayer,
                    },
                    new ThShearwallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        ElementLayer=AiLayerManager.ShearWallLayer,
                    },
                    new ThColumnExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        ElementLayer = AiLayerManager.ColumnLayer,
                    },
                    new ThHydrantDoorOpeningExtractor()
                    { 
                        UseDb3Engine=false,
                        ElementLayer = AiLayerManager.DoorOpeningLayer,
                    },
                    new ThExternalSpaceExtractor()
                    {
                        UseDb3Engine=false,
                        ElementLayer=AiLayerManager.OuterBoundaryLayer,
                    }, 
                    new ThFireHydrantExtractor(),
                    new ThRoomExtractor()
                    {
                        UseDb3Engine=true,
                    },
                };
            extractors.ForEach(o => o.Extract(db, pts));
            return extractors;
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
            var context = new ThProtectionContextMgd();
            context.HydrantHoseLength = FireHydrantVM.Parameter.FireHoseWalkRange;
            context.HydrantClearanceRadius = FireHydrantVM.Parameter.SprayWaterColumnRange;
            return context;
        }
        private List<Point3d> ContainsPts(Entity polygon,List<Point3d> pts)
        {
            return pts.Where(p=>polygon.IsContains(p)).ToList();
        }
        public void Print(Database db)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(db))
            {
                int colorIndex = 1;
                Covers.ForEach(o =>
                {
                    var ents = new List<Entity>();
                    ents.Add(o.Item1.Clone() as Entity);
                    ents.Add(new Circle(o.Item2, Vector3d.ZAxis, 200.0));
                    ents.CreateGroup(acadDb.Database, colorIndex++);
                });
            }
        }
    }
}
#endif
