using System;
using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.IO;
using NFox.Cad;
using ThMEPElectrical.FireAlarm.Interfacce;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    class ThHoleExtractor : ThExtractorBase, IPrint, IGroup, ISetStorey
    {
        public Dictionary<Polyline, List<string>> HoleDic { get; private set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThHoleExtractor()
        {
            Category = BuiltInCategory.Hole.ToString();
            StoreyInfos = new List<ThStoreyInfo>();
            HoleDic = new Dictionary<Polyline, List<string>>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            HoleDic.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Key);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Key);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Value);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o.Key;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            extractService.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            var holes = extractService.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            holes.ForEach(o => o = ThHandleNonClosedPolylineService.Handle(o));
            //对Clean的结果进一步过虑
            holes = holes.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            var textService = new ThExtractTextService();
            textService.Extract(database, pts);

            // 获取洞包括的文字
            var textInfoService = new ThQueryHoleTextInfoService();
            HoleDic = textInfoService.Query(holes, textService.Texts);
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            HoleDic.ForEach(o => GroupOwner.Add(o.Key, FindCurveGroupIds(groupId, o.Key)));
        }
        public void Print(Database database)
        {
            HoleDic.Select(o=>o.Key).Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }
    }
}
