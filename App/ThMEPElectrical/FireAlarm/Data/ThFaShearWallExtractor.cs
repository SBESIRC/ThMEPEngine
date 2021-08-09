﻿using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarm.Interfacce;

namespace FireAlarm.Data
{
    public class ThFaShearWallExtractor : ThShearwallExtractor, IGroup, ISetStorey
    {
        public ThFaShearWallExtractor()
        {
        }
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //From DB3
            var db3Walls = new List<Entity>();
            using (var wallEngine = new ThShearWallRecognitionEngine())
            {
                wallEngine.Recognize(database, pts);
                db3Walls = wallEngine.Elements.Select(o => o.Outline as Polyline).Cast<Entity>().ToList();
            }
            //From Local
            var localWalls = new List<Entity>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            localWalls = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToList();
            //对Clean的结果进一步过虑
            localWalls = localWalls.ToCollection().FilterSmallArea(1.0).Cast<Entity>().ToList();

            //处理重叠
            var conflictService = new ThHandleConflictService(localWalls, db3Walls);
            conflictService.Handle();
            Walls = conflictService.Results.Cast<Entity>().ToList();
            ThHandleContainsService handlecontain = new ThHandleContainsService();
            Walls = handlecontain.Handle(Walls);

            Walls = Walls.ToCollection().FilterSmallArea(SmallAreaTolerance).Cast<Entity>().ToList();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Walls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }
    }
}
