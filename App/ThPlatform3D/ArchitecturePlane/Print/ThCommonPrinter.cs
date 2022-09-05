using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.ArchitecturePlane.Service;

namespace ThPlatform3D.ArchitecturePlane.Print
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
