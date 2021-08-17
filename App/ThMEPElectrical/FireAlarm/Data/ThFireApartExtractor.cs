using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPElectrical.FireAlarm.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    class ThFireApartExtractor : ThExtractorBase, IPrint, IGroup,ITransformer
    {
        public List<Polyline> FireAparts { get; protected set; }

        public List<ThStoreyInfo> StoreyInfos { get; set; }

        public Dictionary<Entity, string> FireApartIds { get; private set; }

        public ThFireApartExtractor()
        {
            FireAparts = new List<Polyline>();
            Category = BuiltInCategory.FireApart.ToString();
            StoreyInfos = new List<ThStoreyInfo>();
            FireApartIds = new Dictionary<Entity, string>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireAparts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                if(FireApartIds.ContainsKey(o))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, FireApartIds[o]);
                }
                else
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, "");
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
                //geometry.Boundary = o;
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

            extractService.Polys.ForEach(o => Transformer.Transform(o));
            FireAparts = extractService.Polys.ToList();

            // 如果楼层框线没有防火分区，就认为楼层框线是一个防火分区
            var spatialIndex = new ThCADCoreNTSSpatialIndex(FireAparts.ToCollection());
            StoreyInfos.ForEach(o =>
            {
                if (spatialIndex.SelectWindowPolygon(o.Boundary).Count == 0)
                {
                    var bufferService = new ThNTSBufferService();
                    var fireApartOutline = bufferService.Buffer(o.Boundary, -1.0) as Polyline;
                    FireAparts.Add(fireApartOutline);
                }
            });
            FireAparts = FireAparts
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => ThCleanEntityService.Tesslate(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            FireAparts = FireAparts.ToCollection().FilterSmallArea(SmallAreaTolerance).Cast<Polyline>().ToList();
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            FireAparts.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }

        public void BuildFireAPartIds()
        {
            //只有分组才能获取
            StoreyInfos.ForEach(o =>
            {
                var fireAparts = GroupOwner.Where(g => g.Value.Contains(o.Id)).Select(g => g.Key).ToList();
                string startCode = "";
                switch (o.StoreyType)
                {
                    case "大屋面":
                        startCode = "JF";
                        break;
                    case "小屋面":
                        startCode = "RF";
                        break;
                    default:
                        startCode = o.StoreyNumber.Split(',')[0];
                        break;
                }
                int startIndex = 1;
                fireAparts.ForEach(f =>
                {
                    string number = startIndex++.ToString().PadLeft(2, '0');
                    FireApartIds.Add(f, startCode + number);
                });
            });
        }

        public void Print(Database database)
        {
            FireAparts.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Transform()
        {
            Transformer.Transform(FireAparts.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(FireAparts.ToCollection());
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
