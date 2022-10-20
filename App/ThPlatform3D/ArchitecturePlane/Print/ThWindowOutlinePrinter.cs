﻿using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThWindowOutlinePrinter
    {
        private PrintConfig Config { get; set; }
        public ThWindowOutlinePrinter(PrintConfig config)
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
                LayerName = ThArchPrintLayerManager.AEWIND,
            };
        }
    }
}