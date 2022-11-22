using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThRailingPrinter
    {
        private PrintConfig Config { get; set; }
        public ThRailingPrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var railId = curve.Print(db, Config);
            if(railId!=ObjectId.Null)
            {
                results.Add(railId);
            }           
            return results;
        }
        public static PrintConfig GetPlanConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEHDWR,
            };
        }
        public static PrintConfig GetSectionConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEHDWR,
            };
        }
    }
}
