﻿using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using NFox.Cad;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerArchitectureWallExtractor : ThArchitectureExtractor, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        /// <summary>
        /// 从图纸中获取的原始建筑墙元素
        /// 已经移动到原点处
        /// </summary>
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }

        public ThSprinklerArchitectureWallExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            //提取,并移动到原点
            var db3Walls = ExtractDb3Wall(pts);

            var localWalls = ExtractMsWall(database, pts);

            //清洗
            var clean = new ThSprinklerCleanEntityService();
            localWalls = localWalls.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localWalls = localWalls.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThSprinklerHandleConflictService(
                localWalls.Cast<Entity>().ToList(),
                db3Walls.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            Walls = handleObjs.Cast<Entity>().ToList();
        }

        private DBObjectCollection ExtractDb3Wall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var wallEngine = new ThDB3ArchWallRecognitionEngine();
            wallEngine.Recognize(Db3ExtractResults, newPts);
            return wallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractMsWall(Database database, Point3dCollection pts)
        {
            //提取了本地图纸中的墙，并移动到原点
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
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
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            //ToDo
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            transformer.Transform(Walls.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Walls.ToCollection());
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
