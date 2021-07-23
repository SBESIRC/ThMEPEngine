using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThParkingStallExtractor : ThExtractorBase, IPrint
    {
        public List<Curve> ParkingStalls { get; set; }
        private List<string> BlockNames { get; set; }
        private List<string> LayerNames { get; set; }
        public ThParkingStallExtractor()
        {
            Category = BuiltInCategory.ParkingStall.ToString();
            ParkingStalls = new List<Curve>();
            BlockNames = new List<string>() { "车位", "Park", "Car", "子母", "电车" };
            LayerNames = new List<string>() { "AE-EQPM-CAR", "CARS", "车位", "子母车", "微型车", "电车" };
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            // 只看块名
            using (var engine = new ThParkingStallRecognitionEngine())
            {
                var visitor = new ThParkingStallExtractionVisitor();
                visitor.CheckQualifiedBlockName = CheckBlockNameQualified;
                visitor.CheckQualifiedLayer = (Entity e) => true;
                engine.Visitor = visitor;
                engine.Recognize(database, pts);
                engine.RecognizeMS(database, pts);
                ParkingStalls.AddRange(engine.Elements.Cast<ThIfcParkingStall>().Select(o => o.Boundary).ToList());
            }
            // 只看图层
            using (var engine = new ThParkingStallRecognitionEngine())
            {
                var visitor = new ThParkingStallExtractionVisitor();
                visitor.CheckQualifiedLayer = CheckLayerNameQualified;
                visitor.CheckQualifiedBlockName = (Entity e) => true;
                engine.Recognize(database, pts);
                engine.RecognizeMS(database, pts);
                ParkingStalls.AddRange(engine.Elements.Cast<ThIfcParkingStall>().Select(o => o.Boundary).ToList());
            }
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ParkingStalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private bool CheckBlockNameQualified(Entity entity)
        {
            if (entity is BlockReference br)
            {
                string name = br.GetEffectiveName().ToUpper();
                return BlockNames.Where(o => name.Contains(o.ToUpper())).Any();
            }
            return false;
        }
        private bool CheckLayerNameQualified(Entity entity)
        {
            string name = entity.Layer.ToUpper();
            return LayerNames.Where(o => name.Contains(o.ToUpper())).Any();
        }

        public void Print(Database database)
        {
            ParkingStalls.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
