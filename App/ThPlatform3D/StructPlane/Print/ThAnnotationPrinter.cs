﻿using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThAnnotationPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb, DBText dbText,AnnotationPrintConfig config)
        {
            var results = new ObjectIdCollection();
            var textId = dbText.Print(acadDb, config);
            results.Add(textId);
            return results;
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