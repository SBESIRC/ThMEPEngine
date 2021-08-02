using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.FireAlarm.Interfacce;

namespace FireAlarm.Data
{
    public class ThFaWindowExtractor : ThWindowExtractor, ISetStorey
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThFaWindowExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //From DB3
            var db3Windows = new List<Polyline>();
            using (var windowEngine = new ThDB3WindowRecognitionEngine())
            {
                windowEngine.Recognize(database, pts);
                db3Windows = windowEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            //From Local
            var localWindows = new List<Polyline>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            localWindows = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            localWindows = localWindows.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localWindows.Cast<Entity>().ToList(),
                db3Windows.Cast<Entity>().ToList());
            conflictService.Handle();
            Windows = conflictService.Results.Cast<Polyline>().ToList();
            Windows = Windows.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Windows.ForEach(o =>
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
