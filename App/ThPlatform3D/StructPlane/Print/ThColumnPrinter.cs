using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThColumnPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase db, Polyline polygon, PrintConfig outlineConfig,HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            var outlineId =  polygon.Print(db, outlineConfig);
            var objIds = new ObjectIdCollection { outlineId };
            var hatchId = objIds.Print(db, hatchConfig);
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
                PatternName = "钢筋混凝土",
                //PatternName = "AR-CONC",
                PatternScale = 30.0,
            };
        }
    }
}
