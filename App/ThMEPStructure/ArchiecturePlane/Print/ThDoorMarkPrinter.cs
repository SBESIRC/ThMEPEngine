﻿using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThDoorMarkPrinter
    {
        private AnnotationPrintConfig Config { get; set; }
        public ThDoorMarkPrinter(AnnotationPrintConfig config)
        {
            Config = config;
        }
        public ObjectIdCollection Print(Database db, DBText text)
        {
            var results = new ObjectIdCollection();
            var beamId = text.Print(db, Config);
            results.Add(beamId);
            return results;
        }
        public static AnnotationPrintConfig GetConfig()
        {
            return new AnnotationPrintConfig
            {
                Height = 150,
                WidthFactor = 0.7,
                LayerName = ThArchPrintLayerManager.DEFPOINTS,
                TextStyleName = ThArchPrintStyleManager.THSTYLE3,
            };
        }
    }
}
