using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.ArchitecturePlane.Service;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThCommonPrinter
    {
        private PrintConfig Config { get; set; }
        public ThCommonPrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var beamId = curve.Print(db, Config);
            results.Add(beamId);
            return results;
        }
        public static PrintConfig GetCommonConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.CommonLayer,
                LineType = "ByLayer",
            };
        }
    }
}
