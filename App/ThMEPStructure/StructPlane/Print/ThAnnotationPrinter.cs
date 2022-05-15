using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThAnnotationPrinter
    {
        private AnnotationPrintConfig Config { get; set; }
        public ThAnnotationPrinter(AnnotationPrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, DBText dbText)
        {
            var results = new ObjectIdCollection();
            var textId = dbText.Print(db, Config);
            results.Add(textId);
            return results;
        }
        public static AnnotationPrintConfig GetAnnotationConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.BeamTextLayName,
                Height = 250,
                WidthFactor = 0.7,
                TextStyleName = "TH-STYLE3",
            };
        }
        public static AnnotationPrintConfig GetHeadTextConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.HeadTextLayerName,
                Height = 800,
                WidthFactor = 0.8,
                TextStyleName = "TH-STYLE2",
            };
        }
        public static AnnotationPrintConfig GetHeadTextScaleConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.HeadTextLayerName,
                Height = 600,
                WidthFactor = 0.8,
                TextStyleName = "TH-STYLE2",
            };
        }
    }
}
