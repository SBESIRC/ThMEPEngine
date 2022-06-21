using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchiecturePlane.Print
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
        public static AnnotationPrintConfig GetConfig()
        {
            return new AnnotationPrintConfig
            {
                Height = 150.0,
                WidthFactor = 0.7,
                LayerName = ThArchPrintLayerManager.DEFPOINTS,
                TextStyleName = ThArchPrintStyleManager.THSTYLE3,
            };
        }
    }
}
