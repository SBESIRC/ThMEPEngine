using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThBeamPrinter
    {
        private PrintConfig Config { get; set; }
        public ThBeamPrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var beamId = curve.Print(db, Config);
            if(beamId!=ObjectId.Null)
            {
                results.Add(beamId);
            }            
            return results;
        }
        public static PrintConfig GetSectionConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.SBEAM,
            };
        }
    }
}
