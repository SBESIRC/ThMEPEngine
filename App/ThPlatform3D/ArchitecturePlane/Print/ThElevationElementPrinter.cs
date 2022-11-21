using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThElevationElementPrinter
    {
        private PrintConfig Config { get; set; }
        public ThElevationElementPrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var id = curve.Print(db, Config);
            if(id!=ObjectId.Null)
            {
                results.Add(id);
            }            
            return results;
        }
        public static PrintConfig GetConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEELEV3,
            };
        }
    }
}
