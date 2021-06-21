#if ACAD2016
using CLI;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
#endif

namespace ThMEPWSS.Hydrant.Service
{
#if ACAD2016
    public class ThCheckFireExtinguisherService : ICheck
    {

        private ThFireHydrantVM FireHydrantVM { get; set; }
        public List<ThIfcRoom> Rooms { get; set; }
        public List<Entity> Covers { get; set; }

        public List<string> FireExtinguisherBlkNames { get; set; }

        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireExtinguisherService(ThFireHydrantVM fireHydrantVM)
        {
            Rooms = new List<ThIfcRoom>();
            Covers = new List<Entity>();
            FireHydrantVM = fireHydrantVM;
            FireExtinguisherBlkNames = new List<string>() { "手提式灭火器", "推车式灭火器" };
            AiLayerManager = ThHydrantExtractLayerManager.Config();
        }

        public void Check(Database db, Point3dCollection pts)
        {
            var extractors = Extract(db, pts); //获取数据
            var roomExtractor = extractors.Where(o => o is ThRoomExtractor).First() as ThRoomExtractor;
            Rooms = roomExtractor.Rooms; //获取房间
            //用于判断私立空间或公立空间
            IRoomPrivacy privacyCheck = new ThJudgeRoomPrivacyService();
            roomExtractor.iRoomPrivacy = privacyCheck;

            FireExtinguisherBlkNames.ForEach(o =>
            {
                var newExtractors = new List<ThExtractorBase>();
                extractors.ForEach(o => newExtractors.Add(o));
                newExtractors.Add(ExtractFireExtinguisher(db, pts, o));
                string geoContent = OutPutGeojson(newExtractors);
                var context = BuildHydrantParam(o);
                var hydrant = new ThHydrantEngineMgd();
                var regions = hydrant.Validate(geoContent, context);
                var polygons = ThHydrantResultParseService.Parse(regions);
                Covers.AddRange(ThHydrantResultParseService.ToDbEntities(polygons));
            });
        }

        public List<ThExtractorBase> Extract(Database db, Point3dCollection pts)
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
                    new ThDoorOpeningExtractor()
                    { 
                        UseDb3Engine=false,
                        ElementLayer = AiLayerManager.DoorOpeningLayer,
                    },
                    new ThExternalSpaceExtractor()
                    {
                        UseDb3Engine=false,
                        ElementLayer=AiLayerManager.OuterBoundaryLayer,
                    }, //暂时通过图层判断
                    new ThRoomExtractor()
                    {
                        UseDb3Engine=true,
                        RoomBoundaryLayerFilter= new List<string>{ AiLayerManager.RoomBoundaryLayer },
                        RoomMarkLayerFilter = new List<string>{ AiLayerManager .RoomMarkLayer},
                    },
                };
            extractors.ForEach(o => o.Extract(db, pts));
            return extractors;
        }

        private ThFireExtinguisherExtractor ExtractFireExtinguisher(Database db, Point3dCollection pts,string name)
        {
            var visitor = new ThFireExtinguisherExtractionVisitor()
            {
                BlkNames = new List<string> { name },
            };
            var extrator = new ThFireExtinguisherExtractor(visitor);
            extrator.Extract(db, pts);
            return extrator;
        }

        public string OutPutGeojson(List<ThExtractorBase> extractors)
        {
            //用于孤立判断
            extractors.ForEach(o => o.SetRooms(Rooms));
            //输出Geojson
            var geos = new List<ThGeometry>();
            extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));
            return ThGeoOutput.Output(geos);
        }

        private ThProtectionContextMgd BuildHydrantParam(string fireExtinguisherName)
        {
            var maxProtectDis = FireHydrantVM.FireTypeDataManager.Query(
                FireHydrantVM.Parameter.FireType, 
                FireHydrantVM.Parameter.DangerLevel,
                fireExtinguisherName);
            var context = new ThProtectionContextMgd();
            context.HydrantHoseLength = maxProtectDis;
            context.HydrantClearanceRadius = 0;
            return context;
        }
    }
#endif
}
