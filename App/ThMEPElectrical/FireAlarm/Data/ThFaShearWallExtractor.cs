using NFox.Cad;
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
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    public class ThFaShearWallExtractor : ThShearwallExtractor, IGroup, ISetStorey, ITransformer
    {
        public ThFaShearWallExtractor()
        {
        }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Walls = ExtractDb3Wall(database, pts);
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
                db3Walls.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            ThHandleContainsService handlecontain = new ThHandleContainsService();
            handleObjs = handlecontain.Handle(handleObjs.Cast<Entity>().ToList()).ToCollection();
            Walls = handleObjs.Cast<Entity>().ToList();
        }
        private DBObjectCollection ExtractDb3Wall(Database database, Point3dCollection pts)
        {
            var db3Walls = new DBObjectCollection();
            var db3ShearWallExtractionEngine = new ThDB3ShearWallExtractionEngine();
            db3ShearWallExtractionEngine.Extract(database); //提取跟NTS算法没有关系
            db3ShearWallExtractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));
            var wallEngine = new ThDB3ShearWallRecognitionEngine();
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
            wallEngine.Recognize(db3ShearWallExtractionEngine.Results, newPts);
            db3Walls = wallEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();

            return db3Walls;
        }
        private DBObjectCollection ExtractMsWall(Database database, Point3dCollection pts)
        {
            var localWalls = new DBObjectCollection();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            localWalls = instance.Polys.ToCollection();

            return localWalls;
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
