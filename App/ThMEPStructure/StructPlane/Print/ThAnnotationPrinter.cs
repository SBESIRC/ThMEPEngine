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
        public static AnnotationPrintConfig GetAnnotationConfig(string drawingScale)
        {
            var config = GetAnnotationConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private static AnnotationPrintConfig GetAnnotationConfig()
        {
            // 1:1
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.BeamTextLayerName,
                Height = 2.5,
                WidthFactor = 0.7,
                TextStyleName = ThPrintStyleManager.THSTYLE3,
            };
        }
        public static AnnotationPrintConfig GetHeadTextConfig(string drawingScale)
        {
            var config = GetHeadTextConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private static AnnotationPrintConfig GetHeadTextConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.HeadTextLayerName,
                Height = 8,
                WidthFactor = 0.8,
                TextStyleName = "TH-STYLE2",
            };
        }

        public static AnnotationPrintConfig GetHeadTextScaleConfig(string drawingScale)
        {
            var config = GetHeadTextScaleConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }

        private static AnnotationPrintConfig GetHeadTextScaleConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.HeadTextLayerName,
                Height = 6,
                WidthFactor = 0.8,
                TextStyleName = "TH-STYLE2",
            };
        }
    }
}
