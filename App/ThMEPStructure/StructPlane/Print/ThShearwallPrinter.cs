using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System.Linq;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThShearwallPrinter
    {
        private HatchPrintConfig HatchConfig { get; set; }
        private PrintConfig OutlineConfig { get; set; }
        public ThShearwallPrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            HatchConfig = hatchConfig;
            OutlineConfig = outlineConfig;
        }
        public ObjectIdCollection Print(Database db, Polyline polygon)
        {
            var results = new ObjectIdCollection();
            var outlineId =  polygon.Print(db, OutlineConfig);
            var objIds = new ObjectIdCollection { outlineId };
            var hatchId = objIds.Print(db, HatchConfig);
            results.Add(outlineId);
            results.Add(hatchId);
            return results;
        }
        public ObjectIdCollection Print(Database db, MPolygon polygon)
        {
            var results = new ObjectIdCollection();
            if (polygon == null || polygon.Area <= 1.0)
            {
                return results;
            }
            if (HatchConfig != null && polygon.Hatch != null)
            {
                var hatchIds = polygon.Print(db, HatchConfig, OutlineConfig);
                hatchIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            }
            return results;
        }
        public static PrintConfig GetUpperShearWallConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.ShearWallLayerName,
            };
        }
        public static HatchPrintConfig GetUpperShearWallHatchConfig()
        {
            return new HatchPrintConfig
            {
                LayerName = ThPrintLayerManager.ShearWallHatchLayerName,
                PatternName = "SOLID",
                PatternScale = 1.0,
            };
        }

        public static PrintConfig GetBelowShearWallConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.BelowShearWallLayerName,
            };
        }

        public static HatchPrintConfig GetBelowShearWallHatchConfig()
        {
            return new HatchPrintConfig
            {
                //PatternType = HatchPatternType.CustomDefined,
                //PatternName = "钢筋混凝土",
                PatternName = "AR-CONC",
                PatternScale = 50.0,
                LayerName = ThPrintLayerManager.BelowShearWallHatchLayerName,
            };
        }
    }
}
