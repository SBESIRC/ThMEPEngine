using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
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
            var doorId = curve.Print(db, Config);
            if(doorId!=ObjectId.Null)
            {
                results.Add(doorId);
            }           
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
