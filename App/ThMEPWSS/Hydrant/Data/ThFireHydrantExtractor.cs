using System.Linq;
using ThMEPWSS.Engine;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThFireHydrantExtractor : ThExtractorBase, IPrint
    {
        public List<DBPoint> FireHydrants { get; set; }
        public ThFireHydrantExtractor()
        {
            FireHydrants = new List<DBPoint>();
            Category = BuiltInCategory.Equipment.ToString();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var vistor = new ThFireHydrantExtractionVisitor()
            {
                BlkNames = new List<string>() { "室内消火栓平面" },
            };
            //后期再做远距离移动
            var hydrantExtractor = new ThFireHydrantRecognitionEngine(vistor);
            hydrantExtractor.Recognize(database, pts);
            hydrantExtractor.RecognizeMS(database, pts);
            var centerPoints = hydrantExtractor.Elements.Select(o => GetCenter(o.Outline as Polyline)).ToList();
            FireHydrants = centerPoints.Select(o => new DBPoint(o)).ToList();

            if(FilterMode == FilterMode.Window)
            {
                FireHydrants = FilterWindowPolygon(pts, FireHydrants.Cast<Entity>().ToList()).Cast<DBPoint>().ToList();
            }
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireHydrants.ForEach(e =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "FireHydrant");
                geometry.Boundary = e;
                geos.Add(geometry);
            });
            return geos;
        }

        private Point3d GetCenter(Polyline rec)
        {
            return rec.GetPoint3dAt(0).GetMidPt(rec.GetPoint3dAt(2));
        }

        public void Print(Database database)
        {
            FireHydrants
                .Select(o=>new Circle(o.Position,Vector3d.ZAxis,5.0))
                .Cast<Entity>().ToList()
                .CreateGroup(database, ColorIndex);
        }
    }
}
