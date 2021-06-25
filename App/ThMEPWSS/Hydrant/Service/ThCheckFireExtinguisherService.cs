﻿#if ACAD2016
using CLI;
using System;
using System.Linq;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Data;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
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
        public List<Tuple<Entity, Point3d,List<Entity>>> Covers { get; set; }
        public List<string> FireExtinguisherBlkNames { get; set; }

        private ThAILayerManager AiLayerManager { get; set; }

        public ThCheckFireExtinguisherService(ThFireHydrantVM fireHydrantVM)
        {
            Rooms = new List<ThIfcRoom>();            
            FireHydrantVM = fireHydrantVM;
            Covers = new List<Tuple<Entity, Point3d, List<Entity>>>();
            AiLayerManager = ThHydrantExtractLayerManager.Config();
            FireExtinguisherBlkNames = new List<string>() { "手提式灭火器", "推车式灭火器" };
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
                var extinguisherExtractor = ExtractFireExtinguisher(db, pts, o);
                if (extinguisherExtractor.FireExtinguishers.Count>0)
                {
                    newExtractors.Add(extinguisherExtractor);
                    string geoContent = OutPutGeojson(newExtractors);
                    var context = BuildHydrantParam(o);
                    var hydrant = new ThHydrantEngineMgd();
                    var regions = hydrant.Validate(geoContent, context);
                    Covers = ThHydrantResultParseService.Parse(regions);
                }
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

        private string OutPutGeojson(List<ThExtractorBase> extractors)
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
            var context = new ThProtectionContextMgd();
            context.HydrantHoseLength = FireHydrantVM.QueryMaxProtectDistance(fireExtinguisherName);
            context.HydrantClearanceRadius = 0;
            return context;
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
                    ents.Add(o.Item1.Clone() as Entity);
                    ents.Add(new Circle(o.Item2, Vector3d.ZAxis, 200.0));
                    ents.CreateGroup(acadDb.Database, colorIndex++);
                });
            }
        }
    }
#endif
}
