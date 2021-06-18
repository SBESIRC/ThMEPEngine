using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThLightningReceivingBeltExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Line> SpecialBelts { get; set; }
        public List<Line> DualpurposeBelts { get; set; }
        public string SpecialBeltLayer { get; set; }
        public string DualpurposeBeltLayer { get; set; }
        public ThLightningReceivingBeltExtractor()
        {
            SpecialBelts = new List<Line>();
            DualpurposeBelts = new List<Line>();
            SpecialBeltLayer = "E-THUN-WIRE";
            DualpurposeBeltLayer = "E-GRND-WIRE";
            Category = BuiltInCategory.LightningReceivingBelt.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service1 = new ThExtractLineService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service1.Extract(database, pts);
            SpecialBelts = service1.Lines;

            var service2 = new ThExtractLineService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service2.Extract(database, pts);
            DualpurposeBelts = service2.Lines;
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            SpecialBelts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "专设接闪带");
                if(GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            DualpurposeBelts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "兼用接闪带");
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            SpecialBelts.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
            DualpurposeBelts.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                SpecialBelts.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
                DualpurposeBelts.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
