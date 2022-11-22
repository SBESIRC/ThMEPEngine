using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThKanXianPrinter
    {
        private PrintConfig Config { get; set; }
        public ThKanXianPrinter(PrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var kanxianId = curve.Print(db, Config);
            if(kanxianId!=ObjectId.Null)
            {
                results.Add(kanxianId);
            }            
            return results;
        }
        public static PrintConfig GetConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEFLOR,
            };
        }
    }
}
