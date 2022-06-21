using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThDoorOutlinePrinter
    {
        private PrintConfig Config { get; set; }
        public ThDoorOutlinePrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            results.Add(curve.Print(db, Config));
            return results;
        }
        public static PrintConfig GetConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEDOORINSD,
            };
        }
    }
}
