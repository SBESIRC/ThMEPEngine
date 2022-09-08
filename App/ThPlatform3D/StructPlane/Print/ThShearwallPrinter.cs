﻿using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThShearwallPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb, Polyline polygon,PrintConfig outlineConfig,HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            var outlineId =  polygon.Print(acadDb, outlineConfig);
            var objIds = new ObjectIdCollection { outlineId };
            var hatchId = objIds.Print(acadDb, hatchConfig);
            results.Add(outlineId);
            results.Add(hatchId);
            return results;
        }
        public static ObjectIdCollection Print(AcadDatabase acadDb, MPolygon polygon,PrintConfig outlineConfig, HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            if (polygon == null || polygon.Area <= 1.0)
            {
                return results;
            }
            if (hatchConfig != null && polygon.Hatch != null)
            {
                var hatchIds = polygon.Print(acadDb, outlineConfig,hatchConfig);
                hatchIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            }
            return results;
        }
        public static PrintConfig GetUpperShearWallConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.ShearWallLayerName,
            };
        }
        public static HatchPrintConfig GetUpperShearWallHatchConfig()
        {
            return new HatchPrintConfig
            {
                LayerName = ThPrintLayerManager.ShearWallHatchLayerName,
                PatternName = "SOLID",
                PatternScale = 1.0,
            };
        }

        public static PrintConfig GetBelowShearWallConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.BelowShearWallLayerName,
            };
        }

        public static HatchPrintConfig GetBelowShearWallHatchConfig()
        {
            return new HatchPrintConfig
            {
                //PatternType = HatchPatternType.CustomDefined,
                //PatternName = "钢筋混凝土",
                PatternName = "AR-CONC",
                PatternScale = 50.0,
                LayerName = ThPrintLayerManager.BelowShearWallHatchLayerName,
            };
        }
    }
}