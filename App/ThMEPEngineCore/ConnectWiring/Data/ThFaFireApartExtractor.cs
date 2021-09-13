using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    class ThFaFireApartExtractor : ThExtractorBase/*, IPrint, IGroup, ITransformer*/
    {
        public List<Polyline> FireAparts { get; protected set; }

        public List<ThStoreyInfo> StoreyInfos { get; set; }

        public Dictionary<Entity, string> FireApartIds { get; private set; }

        public ThFaFireApartExtractor()
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
                if (FireApartIds.ContainsKey(o))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, FireApartIds[o]);
                }
                else
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, "");
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
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
                .Select(o => o.Tessellate(10))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            FireAparts = FireAparts.ToCollection().FilterSmallArea(SmallAreaTolerance).Cast<Polyline>().ToList();
        }
    }
}
