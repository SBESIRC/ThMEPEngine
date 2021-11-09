using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThParkingStallExtractor : ThExtractorBase, IPrint
    {
        public List<Curve> ParkingStalls { get; set; }
        public List<string> BlockNames { get; set; }
        public List<string> LayerNames { get; set; }
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
            if(BlockNames.Count>0)
            {
                using (var engine = new ThParkingStallRecognitionEngine())
                {
                    engine.CheckQualifiedBlockName = CheckBlockNameQualified;
                    engine.CheckQualifiedLayer = (Entity e) => true;
                    engine.Recognize(database, pts);
                    engine.RecognizeMS(database, pts);
                    ParkingStalls.AddRange(engine.Elements.Cast<ThIfcParkingStall>().Select(o => o.Boundary).ToList());
                }
            }
            
            // 只看图层
            if(LayerNames.Count>0)
            {
                using (var engine = new ThParkingStallRecognitionEngine())
                {
                    engine.CheckQualifiedLayer = CheckLayerNameQualified;
                    engine.CheckQualifiedBlockName = (Entity e) => true;
                    engine.Recognize(database, pts);
                    engine.RecognizeMS(database, pts);
                    ParkingStalls.AddRange(engine.Elements.Cast<ThIfcParkingStall>().Select(o => o.Boundary).ToList());
                }
            }
            
            DuplicatedRemove();
        }

        private void DuplicatedRemove()
        {
            ParkingStalls = ParkingStalls.Distinct().ToList();
            var results = ThCADCoreNTSGeometryFilter.GeometryEquality(ParkingStalls.ToCollection());
            ParkingStalls = results.OfType<Curve>().ToList();
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
                if(br.BlockTableRecord != null)
                {
                    string name = br.GetEffectiveName().ToUpper();
                    return BlockNames.Where(o => name.Contains(o.ToUpper())).Any();
                }
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
