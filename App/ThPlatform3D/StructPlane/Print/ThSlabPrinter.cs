using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThSlabPrinter
    {
        public static ObjectIdCollection Print(AcadDatabase acadDb,Polyline polygon, PrintConfig outlineConfig, HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            if (polygon==null || polygon.Area<=1.0)
            {
                return results;
            }            
            var outlineId = polygon.Print(acadDb, outlineConfig);
            var objIds = new ObjectIdCollection { outlineId };
            if(hatchConfig != null)
            {
                var hatchId = objIds.Print(acadDb, hatchConfig);
                results.Add(hatchId);
            }            
            results.Add(outlineId);            
            return results;
        }

        public static ObjectIdCollection Print(AcadDatabase acadDb, MPolygon polygon, PrintConfig outlineConfig, HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            if (polygon == null || polygon.Area <= 1.0)
            {
                return results;
            }
            var hatchIds = polygon.Print(acadDb, outlineConfig, hatchConfig);
            hatchIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            return results;
        }

        public static PrintConfig GetSlabConfig()
        {
            return new PrintConfig()
            {
                LayerName = ThPrintLayerManager.SlabLayerName,
            };
        }

        public static List<HatchPrintConfig> GetSlabHatchConfigs()
        {
            var results = new List<HatchPrintConfig>();
            results.Add(new HatchPrintConfig
            {
                PatternName = "CROSS",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI31",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "STARS",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANGLE",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "TRIANG",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI37",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "SQUARE",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "HONEY",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "GRAVEL",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "HEX",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });            
            results.Add(new HatchPrintConfig
            {
                PatternName = "HOUND",
                PatternAngle = 0.0,
                PatternScale = 2000,
                PatternSpace = 2000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "NET3",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "SACNCR",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "LINE",
                PatternAngle = 45.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "INSUL",
                PatternAngle = 45.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });            
            results.Add(new HatchPrintConfig
            {
                PatternName = "MUDST",
                PatternAngle = 45.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "NET",
                PatternAngle = 45.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "SWAMP",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "TRANS",
                PatternAngle = 0.0,
                PatternScale = 1000,
                PatternSpace = 1000,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ZIGZAG",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "BRASS",
                PatternAngle = 45.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
                      
            results.Add(new HatchPrintConfig
            {
                PatternName = "STEEL",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });            
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI32",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI33",
                PatternAngle = 0.0,
                PatternScale = 35,
                PatternSpace = 35.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI34",
                PatternAngle = 0.0,
                PatternScale = 1000.0,
                PatternSpace = 1000.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI36",
                PatternAngle = 0.0,
                PatternScale = 35,
                PatternSpace = 35.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            results.Add(new HatchPrintConfig
            {
                PatternName = "ANSI38",
                PatternAngle = 0.0,
                PatternScale = 50,
                PatternSpace = 50.0,
                LayerName = ThPrintLayerManager.SlabHatchLayerName,
                PatternType = HatchPatternType.PreDefined,
            });
            return results;
        }
    }
}
