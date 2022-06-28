﻿using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchitecturePlane.Print
{
    internal class ThDoorStonePrinter
    {
        private PrintConfig Config { get; set; }
        public ThDoorStonePrinter(PrintConfig config)
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
        public static PrintConfig GetConfig()
        {
            return new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.DEFPOINTS1,
            };
        }
    }
}