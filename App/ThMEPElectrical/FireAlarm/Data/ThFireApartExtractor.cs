using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.FireAlarm.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.IO;

namespace FireAlarm.Data
{
    class ThFireApartExtractor : ThExtractorBase, IPrint, IGroup
    {
        public List<Polyline> FireAparts { get; private set; }

        public List<StoreyInfo> StoreyInfos { get; set; }

        public Dictionary<Entity, string> FireApartIds { get; private set; }

        public ThFireApartExtractor()
        {
            FireAparts = new List<Polyline>();
            Category = BuiltInCategory.FireApart.ToString();
            StoreyInfos = new List<StoreyInfo>();
            FireApartIds = new Dictionary<Entity, string>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireAparts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, FireApartIds[o]);
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
                //geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                throw new NotImplementedException();
            }
            else
            {
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                extractService.Extract(database, pts);
                FireAparts = extractService.Polys;
            }
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
    }
}
