using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThAEwallPrinter
    {
        private HatchPrintConfig HatchConfig { get; set; }
        private PrintConfig OutlineConfig { get; set; }
        public ThAEwallPrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            HatchConfig = hatchConfig;
            OutlineConfig = outlineConfig;
        }
        public ObjectIdCollection Print(Database db, Entity entity)
        {
            var printer = new ThHatchPrinter(HatchConfig, OutlineConfig);
            return printer.Print(db, entity);
        }

        public static PrintConfig GetAEWallConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
        }
    }
}
