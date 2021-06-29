using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThLightningReceivingBeltExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Curve> SpecialBelts { get; set; }
        public List<Curve> DualpurposeBelts { get; set; }
        public string SpecialBeltLayer { get; set; }
        public string DualpurposeBeltLayer { get; set; }
        public ThLightningReceivingBeltExtractor()
        {
            TesslateLength = 200.0;
            SpecialBelts = new List<Curve>();
            DualpurposeBelts = new List<Curve>();
            SpecialBeltLayer = "E-THUN-WIRE";
            DualpurposeBeltLayer = "E-GRND-WIRE";
            Category = BuiltInCategory.LightningReceivingBelt.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var specialBelts = ExtractSpecialBelt(database, pts);
            var dualpurposeBelts = ExtractDualpurposeBelt(database, pts);

            SpecialBelts = specialBelts.Select(o => ThTesslateService.Tesslate(o,TesslateLength) as Curve).ToList();
            DualpurposeBelts = dualpurposeBelts.Select(o=> ThTesslateService.Tesslate(o, TesslateLength) as Curve).ToList();
        }

        private List<Curve> ExtractSpecialBelt(Database database, Point3dCollection pts)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service1.Extract(database, pts);
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service2.Extract(database, pts);
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service3.Extract(database, pts);
            results.AddRange(service3.Polys);

            return results;
        }

        private List<Curve> ExtractDualpurposeBelt(Database database, Point3dCollection pts)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service1.Extract(database, pts);
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service2.Extract(database, pts);
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service3.Extract(database, pts);
            results.AddRange(service3.Polys);
            return results;
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
