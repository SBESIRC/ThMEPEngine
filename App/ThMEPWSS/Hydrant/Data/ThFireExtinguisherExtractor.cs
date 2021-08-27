using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThFireExtinguisherExtractor : ThExtractorBase, IPrint
    {
        public List<DBPoint> FireExtinguishers { get; set; }
        private ThFireExtinguisherExtractionVisitor Vistor { get; set; }
        public ThFireExtinguisherExtractor(ThFireExtinguisherExtractionVisitor visitor)
        {
            Vistor = visitor;
            FireExtinguishers = new List<DBPoint>();
            Category = BuiltInCategory.Equipment.ToString();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var fireExtinguisherExtractor = new ThFireExtinguisherRecognitionEngine(Vistor);
            fireExtinguisherExtractor.Recognize(database, pts);
            //Vistor.Results = new List<ThRawIfcDistributionElementData>();

            //fireExtinguisherExtractor.RecognizeMS(database, pts);
            //Vistor.Results = new List<ThRawIfcDistributionElementData>();

            var centerPoints = fireExtinguisherExtractor.Elements.Select(o => GetCenter(o.Outline as Polyline)).ToList();
            FireExtinguishers = centerPoints.Select(o => new DBPoint(o)).ToList();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireExtinguishers.ForEach(e =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "FireExtinguisher");
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
            FireExtinguishers
                .Select(o => new Circle(o.Position, Vector3d.ZAxis, 5.0))
                .Cast<Entity>().ToList()
                .CreateGroup(database, ColorIndex);
        }
    }
}
