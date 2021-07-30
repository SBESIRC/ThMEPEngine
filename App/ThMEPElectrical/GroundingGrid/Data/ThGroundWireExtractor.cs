using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPElectrical.GroundingGrid.Data
{
    public class ThGroundWireExtractor : ThExtractorBase,IGroup,IPrint
    {
        public List<Curve> Wires { get; private set; }
        public ThGroundWireExtractor()
        {
            Wires = new List<Curve>();
            TesslateLength = 50.0;
            Category = BuiltInCategory.LightningReceivingBelt.ToString();
        }        

        public override void Extract(Database database, Point3dCollection pts)
        {
            var lineService = new ThExtractLineService()
            {
                ElementLayer = this.ElementLayer,
            };
            lineService.Extract(database, pts);
            Wires.AddRange(lineService.Lines);

            var arcService = new ThExtractArcService()
            {
                ElementLayer = this.ElementLayer,
            };
            arcService.Extract(database, pts);
            Wires.AddRange(arcService.Arcs);

            var polyService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
                TesslateLength = this.TesslateLength,
            };
            polyService.Extract(database, pts);
            Wires.AddRange(polyService.Polys);
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Wires.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "兼用接闪带");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Wires.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }

        public void Print(Database database)
        {
            Wires
                .Select(o => o.Clone() as Entity)
                .ToList()
                .CreateGroup(database, ColorIndex);
        }
    }
}
