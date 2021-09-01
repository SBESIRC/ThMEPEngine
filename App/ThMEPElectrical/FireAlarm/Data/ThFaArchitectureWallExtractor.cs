using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPElectrical.FireAlarm.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFaArchitectureWallExtractor : ThArchitectureExtractor, IGroup, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        /// <summary>
        /// 从图纸中获取的原始建筑墙元素
        /// 没有偏移过的
        /// </summary>
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }

        public ThFaArchitectureWallExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //提取,并移动到原点
            var db3Walls = ExtractDb3Wall(pts);

            var localWalls = ExtractMsWall(database, pts);

            //清洗
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
                db3Walls.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            Walls = handleObjs.Cast<Entity>().ToList();
        }

        private DBObjectCollection ExtractDb3Wall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            Db3ExtractResults.ForEach(o => Transformer.Transform(o.Geometry));
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
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

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            Walls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));            
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
