using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
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
            results.Add(beamId);
            return results;
        }
        public static PrintConfig GetBeamConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.BeamLayerName,
            };
        }
    }
}
