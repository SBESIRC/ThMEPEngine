using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor.Model;
using NFox.Cad;
using ThMEPElectrical.FireAlarm.Interfacce;

namespace FireAlarm.Data
{
    public class ThFaRailingExtractor :ThRailingExtractor, ISetStorey
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public ThFaRailingExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //From DB3
            var db3Railings = new List<Polyline>();
            using (var railingEngine = new ThRailingRecognitionEngine())
            {
                railingEngine.Recognize(database, pts);
                db3Railings = railingEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            //From Local
            var localRailings = new List<Polyline>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            localRailings = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            localRailings = localRailings.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localRailings.Cast<Entity>().ToList(),
                db3Railings.Cast<Entity>().ToList());
            conflictService.Handle();
            Railing = conflictService.Results.Cast<Polyline>().ToList();
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
    }
}
