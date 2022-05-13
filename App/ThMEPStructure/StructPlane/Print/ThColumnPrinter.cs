using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThColumnPrinter
    {
        private HatchPrintConfig HatchConfig { get; set; }
        private PrintConfig OutlineConfig { get; set; }
        public ThColumnPrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
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

        public static PrintConfig GetUpperColumnConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.ColumnLayerName,
            };
        }
        public static HatchPrintConfig GetUpperColumnHatchConfig()
        {
            return new HatchPrintConfig
            {
                LayerName = ThPrintLayerManager.ColumnHatchLayerName,
                PatternName = "SOLID",
                PatternScale = 1.0,
            };
        }

        public static PrintConfig GetBelowColumnConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.BelowColumnLayerName,
            };
        }

        public static HatchPrintConfig GetBelowColumnHatchConfig()
        {
            return new HatchPrintConfig
            {
                LayerName = ThPrintLayerManager.BelowColumnHatchLayerName,
                //PatternName = "钢筋混凝土",
                PatternName = "AR-CONC",
                PatternScale = 30.0,
            };
        }
    }
}
