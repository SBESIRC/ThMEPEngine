using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Temp
{
    class ThFiredistrictExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {
        public List<Polyline> Firedistrict { get; private set; }

        public List<StoreyInfo> StoreyInfos { get; set; }

        public ThFiredistrictExtractor()
        {
            Firedistrict = new List<Polyline>();
            Category = "FireDistrict";
            StoreyInfos = new List<StoreyInfo>();
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Firedistrict.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
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
                Firedistrict = extractService.Polys;
            }
            // 如果楼层框线没有防火分区，就认为楼层框线是一个防火分区
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Firedistrict.ToCollection());
            StoreyInfos.ForEach(o =>
            {
                if(spatialIndex.SelectWindowPolygon(o.Boundary).Count==0)
                {
                    var bufferService = new Service.ThNTSBufferService();
                    var fireApartOutline = bufferService.Buffer(o.Boundary ,- 1.0) as Polyline;
                    Firedistrict.Add(fireApartOutline);
                }
            });
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Firedistrict.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }

        public Dictionary<Entity,string> GetFireAPartIds()
        {
            //只有分组才能获取
            var results = new Dictionary<Entity, string>();
            StoreyInfos.ForEach(o =>
            {
                var fireAparts = GroupOwner.Where(g => g.Value.Contains(o.Id)).Select(g => g.Key).ToList();
                string startCode = "";
                switch(o.StoreyType)
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
                    results.Add(f , startCode + number);
                });
            });
            return results;
        }

        public void Print(Database database)
        {
            Firedistrict.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
