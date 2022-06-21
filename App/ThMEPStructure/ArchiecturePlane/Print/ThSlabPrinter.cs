using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThSlabPrinter
    {
        private PrintConfig OutlineConfig { get; set; }
        private HatchPrintConfig HatchConfig { get; set; }
        public ThSlabPrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            HatchConfig = hatchConfig;
            OutlineConfig = outlineConfig;
        }

        public ObjectIdCollection Print(Database db, Entity entity)
        {
            var printer = new ThHatchPrinter(HatchConfig, OutlineConfig);
            return printer.Print(db, entity);
        }
   
        public static PrintConfig GetAESTRUOutlineConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
        }
        public static HatchPrintConfig GetAESTRUHatchConfig()
        {
            return new HatchPrintConfig
            {
                PatternScale = 1.0,
                PatternName = "SOLID",
                LayerName = ThArchPrintLayerManager.AESTRUHACH,
            };
        }
    }
}
