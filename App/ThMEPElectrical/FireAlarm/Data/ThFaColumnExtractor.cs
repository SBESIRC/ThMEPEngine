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
using NFox.Cad;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarm.Interfacce;

namespace FireAlarm.Data
{
    public class ThFaColumnExtractor :ThColumnExtractor, IGroup, ISetStorey
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public ThFaColumnExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //From DB3
            var db3Columns = new List<Polyline>();
            using (var columnEngine = new ThColumnRecognitionEngine())
            {
                columnEngine.Recognize(database, pts);
                db3Columns = columnEngine.Elements.Select(o => o.Outline as Polyline).ToList();
            }
            //From Local
            var localColumns = new List<Polyline>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            localColumns = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            localColumns = localColumns.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            //处理重叠
            var conflictService = new ThHandleConflictService(
                localColumns.Cast<Entity>().ToList(),
                db3Columns.Cast<Entity>().ToList());
            conflictService.Handle();
            ThHandleContainsService handlecontain = new ThHandleContainsService();
            Columns = conflictService.Results.Cast<Polyline>().ToList();
            Columns = handlecontain.Handle(Columns.Cast<Entity>().ToList()).Cast<Polyline>().ToList();

            Columns = Columns.ToCollection().FilterSmallArea(SmallAreaTolerance).Cast<Polyline>().ToList();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Columns.ForEach(o =>
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

        public void Group(Dictionary<Entity, string> groupId)
        {
            Columns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }
    }
}
