﻿using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASShearWallExtractor : ThShearwallExtractor, IGroup, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }
        public List<ThRawIfcBuildingElementData> NonDb3ExtractResults { get; set; }
        public ThAFASShearWallExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
            Db3ExtractResults = new List<ThRawIfcBuildingElementData>();
            NonDb3ExtractResults = new List<ThRawIfcBuildingElementData>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Walls = ExtractDb3Wall(pts);
            var nonDb3Walls = ExtractNonDb3Wall(pts);
            var xRefWalls = new DBObjectCollection();
            db3Walls.Cast<Entity>().ForEach(e => xRefWalls.Add(e));
            nonDb3Walls.Cast<Entity>().ForEach(e => xRefWalls.Add(e));

            var localWalls = ExtractMsWall(database, pts);

            ThCleanEntityService clean = new ThCleanEntityService();
            localWalls = localWalls.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localWalls = localWalls.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localWalls.Cast<Entity>().ToList(),
                xRefWalls.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            var handleContainService = new ThHandleContainsService();
            handleObjs = handleContainService.Handle(handleObjs.Cast<Entity>().ToList()).ToCollection();
            Walls = handleObjs.Cast<Entity>().ToList();
        }
        private DBObjectCollection ExtractDb3Wall(Point3dCollection pts)
        {
            var wallEngine = new ThDB3ShearWallRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            wallEngine.Recognize(Db3ExtractResults, newPts);
            return wallEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
        }
        private DBObjectCollection ExtractNonDb3Wall(Point3dCollection pts)
        {
            var wallEngine = new ThShearWallRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            wallEngine.Recognize(NonDb3ExtractResults, newPts);
            return wallEngine.Elements
                .Select(o => o.Outline as Polyline)
                .ToCollection();
        }

        private DBObjectCollection ExtractMsWall(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
                //TesslateLength = 1000,!!!!!!!!!!!!!!!!
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            return instance.Polys.ToCollection();
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
                    if (storeyInfo != null)
                    {
                        parentId = storeyInfo.Id;
                    }
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            //Walls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            foreach (var o in Walls)
            {
                if (GroupOwner.ContainsKey(o) == false)
                {
                    GroupOwner.Add(o, FindCurveGroupIds(groupId, o));
                }
                else
                {
                    GroupOwner[o] = FindCurveGroupIds(groupId, o);
                }
            }
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.EntityContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public void Transform()
        {
            Transformer.Transform(Walls.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Walls.ToCollection());
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
