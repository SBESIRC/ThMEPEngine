using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThWindowMarkPrinter
    {
        private AnnotationPrintConfig Config { get; set; }
        public ThWindowMarkPrinter(AnnotationPrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, DBText text)
        {
            var results = new ObjectIdCollection();
            var beamId = text.Print(db, Config);
            results.Add(beamId);
            return results;
        }
        public static AnnotationPrintConfig GetConfig(string drawingScale)
        {
            var config = GetConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private static AnnotationPrintConfig GetConfig()
        {
            return new AnnotationPrintConfig
            {
                Height = 1.5,
                WidthFactor = 0.7,
                LayerName = ThArchPrintLayerManager.DEFPOINTS,
                TextStyleName = ThArchPrintStyleManager.THSTYLE3,
            };
        }
    }
}
