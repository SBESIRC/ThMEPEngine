using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.GroundingGrid.Data
{
    public class ThFloorOutlineExtractor : ThExtractorBase,IGroup,IPrint
    {
        public List<Polyline> Outlines { get; set; }
        public ThFloorOutlineExtractor()
        {
            Outlines = new List<Polyline>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var polyService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            polyService.Extract(database, pts);
            Outlines = polyService.Polys;
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Outlines.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "底板轮廓");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Outlines.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }

        public void Print(Database database)
        {
            Outlines
                .Select(o => o.Clone() as Entity)
                .ToList()
                .CreateGroup(database,ColorIndex);
        }
    }
}
