using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThMEPElectrical.FireAlarm.Interface;
using ThMEPElectrical.FireAlarm.Service;

namespace ThMEPElectrical.FireAlarm.Data
{
    public class ThFaRailingExtractor :ThRailingExtractor, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThFaRailingExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Railings = ExtractDb3Railing(pts);
            var localRailings = ExtractMsRailing(database, pts);

            //对Clean的结果进一步过虑
            localRailings = localRailings.FilterSmallArea(SmallAreaTolerance);

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localRailings.Cast<Entity>().ToList(),
                db3Railings.Cast<Entity>().ToList());
            conflictService.Handle();
            var handleObjs = conflictService.Results.ToCollection().FilterSmallArea(SmallAreaTolerance);
            Railing = handleObjs.Cast<Polyline>().ToList();
        }
        private DBObjectCollection ExtractDb3Railing(Point3dCollection pts)
        {
            var railingEngine = new ThDB3RailingRecognitionEngine();
            var newPts = Transformer.Transform(pts);            
            railingEngine.Recognize(Db3ExtractResults, newPts);
            return railingEngine.Elements.Select(o => o.Outline as Polyline).ToCollection();
        }
        private DBObjectCollection ExtractMsRailing(Database database, Point3dCollection pts)
        {
            var localRailings = new DBObjectCollection();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);

            instance.Polys.ForEach(o => Transformer.Transform(o));
            localRailings = instance.Polys.ToCollection();

            ThCleanEntityService clean = new ThCleanEntityService();
            return localRailings.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Railing.ForEach(o =>
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
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public void Transform()
        {
            Transformer.Transform(Railing.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Railing.ToCollection());
        }
    }
}
