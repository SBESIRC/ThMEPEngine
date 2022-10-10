using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThGridPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb, Curve curve, PrintConfig config)
        {
            var results = new ObjectIdCollection();
            results.Add(curve.Print(acadDb, config));
            return results;
        }

        public static ObjectIdCollection Print(AcadDatabase acadDb, AlignedDimension dimension, DimensionPrintConfig config)
        {
            var results = new ObjectIdCollection();
            results.Add(dimension.Print(acadDb, config));
            return results;
        }

        public static ObjectIdCollection Print(AcadDatabase acadDb, DBText text, AnnotationPrintConfig config)
        {
            var results = new ObjectIdCollection();
            results.Add(text.Print(acadDb, config));
            return results;
        }

        public static PrintConfig GridLineConfig
        {
            get
            {
                return new PrintConfig
                {
                    LayerName = ThArchPrintLayerManager.ADAXISAXIS,
                };
            }
        }

        public static DimensionPrintConfig DimensionConfig
        {
            get
            {
                return new DimensionPrintConfig()
                {
                    LayerName = ThArchPrintLayerManager.ADAXISDIMS,
                    DimStyleName = ThArchPrintDimStyleManager.TCHARCH,
                };
            }
        }

        public static PrintConfig CircleLabelLeaderConfig
        {
            get
            {
                return new PrintConfig()
                { 
                    LayerName = ThArchPrintLayerManager.ADAXISDIMS,
                };
            }
        }

        public static PrintConfig CircleLabelCircleConfig
        {
            get
            {
                return new PrintConfig()
                {
                    LayerName = ThArchPrintLayerManager.ADAXISDIMS,
                };
            }
        }

        public static AnnotationPrintConfig CircleLabelTextConfig
        {
            get
            {
                return new AnnotationPrintConfig()
                {
                    Color = 7,
                    Height =500, 
                    WidthFactor=0.7,
                    LayerName = ThArchPrintLayerManager.ADAXISDIMS,
                    TextStyleName = ThArchPrintStyleManager.TCHAXIS,
                };
            }
        }
    }
}
