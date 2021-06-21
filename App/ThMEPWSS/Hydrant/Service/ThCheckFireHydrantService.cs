#if ACAD2016
using CLI;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThCheckFireHydrantService : ICheck
    {
        private ThFireHydrantVM FireHydrantVM { get; set; }
        public List<ThIfcRoom> Rooms { get; set; }
        public List<Entity> Covers { get; set; }
        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireHydrantService(ThFireHydrantVM fireHydrantVM)
        {
            Rooms = new List<ThIfcRoom>();
            Covers = new List<Entity>();
            FireHydrantVM = fireHydrantVM;
            AiLayerManager = ThHydrantExtractLayerManager.Config();
        }

        public void Check(Database db, Point3dCollection pts)
        {            
            var extractors = Extract(db, pts); //获取数据
            string geoContent = OutPutGeojson(extractors);

            var context = BuildHydrantParam();
            var hydrant = new ThHydrantEngineMgd();
            var regions = hydrant.Validate(geoContent, context);
            var polygons = ThHydrantResultParseService.Parse(regions);
            Covers = ThHydrantResultParseService.ToDbEntities(polygons);
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
                    }, 
                    new ThFireHydrantExtractor(),
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

        public string OutPutGeojson(List<ThExtractorBase> extractors)
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
    }
}
#endif
