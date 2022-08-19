using System.Linq;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Model.Printer;

namespace ThMEPStructure.Common
{
    internal static class ThHatchPrintService
    {
        public static ObjectIdCollection Print(this DBObjectCollection objs, AcadDatabase acadDb)
        {
            var results = new ObjectIdCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                results.Add(acadDb.ModelSpace.Add(e));
            });
            return results;
        }

        public static ObjectId Print(this Entity entity, Database db, PrintConfig config)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return entity.Print(acadDb, config);
            }
        }

        public static ObjectId Print(this Entity entity, AcadDatabase acadDb, PrintConfig config)
        {
            var objId = acadDb.ModelSpace.Add(entity);
            entity.Layer = config.LayerName;
            entity.Linetype = config.LineType;
            entity.LineWeight = config.LineWeight;
            entity.ColorIndex = config.Color;
            if (config.LineTypeScale.HasValue)
            {
                entity.LinetypeScale = config.LineTypeScale.Value;
            }
            return objId;
        }

        public static ObjectId Print(this DBText dbText, Database db, AnnotationPrintConfig config)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return dbText.Print(acadDb,config);
            } 
        }

        public static ObjectId Print(this DBText dbText, AcadDatabase acadDb, AnnotationPrintConfig config)
        {
            // 事务在外部开启
            // 传入的文字几何位置已经确定了，这儿只设置相关属性                
            var objId = acadDb.ModelSpace.Add(dbText);
            dbText.Layer = config.LayerName;
            dbText.Height = config.Height;
            dbText.WidthFactor = config.WidthFactor;
            dbText.ColorIndex = config.Color;
            dbText.TextStyleId = config.TextStyleId;
            return objId;
        }

        public static ObjectId Print(this ObjectIdCollection objIds, Database db,  HatchPrintConfig config)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return objIds.Print(acadDb, config);
            }
        }

        public static ObjectId Print(this ObjectIdCollection objIds, AcadDatabase acadDb, HatchPrintConfig config)
        {
            Hatch oHatch = new Hatch();
            oHatch.HatchObjectType = HatchObjectType.HatchObject;
            oHatch.Normal = config.Normal;
            oHatch.Elevation = config.Elevation;
            //oHatch.PatternAngle = config.PatternAngle;
            oHatch.PatternScale = config.PatternScale;
            //oHatch.PatternSpace = config.PatternSpace;
            oHatch.SetHatchPattern(config.PatternType, config.PatternName);
            oHatch.ColorIndex = config.Color;
            oHatch.Layer = config.LayerName;
            //oHatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
            var hatchId = acadDb.ModelSpace.Add(oHatch);
            oHatch.Associative = config.Associative;
            oHatch.AppendLoop((int)HatchLoopTypes.Default, objIds);
            oHatch.EvaluateHatch(true);
            return hatchId;
        }

        public static ObjectIdCollection Print(this MPolygon polygon, Database db, PrintConfig outlineConfig,HatchPrintConfig hatchConfig)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return polygon.Print(acadDb, outlineConfig,hatchConfig);
            }
        }

        public static ObjectIdCollection Print(this MPolygon polygon, AcadDatabase acadDb, PrintConfig outlineConfig, HatchPrintConfig hatchConfig)
        {
            var results = new ObjectIdCollection();
            var shell = polygon.Shell();
            var holes = polygon.Holes();
            var shellId = shell.Print(acadDb, outlineConfig);
            var holeIds = new ObjectIdCollection();
            holes.ForEach(h => holeIds.Add(h.Print(acadDb, outlineConfig)));
            if (hatchConfig != null)
            {
                Hatch oHatch = new Hatch();
                oHatch.HatchObjectType = HatchObjectType.HatchObject;
                oHatch.Normal = hatchConfig.Normal;
                oHatch.Elevation = hatchConfig.Elevation;
                //oHatch.PatternAngle = config.PatternAngle;
                oHatch.PatternScale = hatchConfig.PatternScale;
                //oHatch.PatternSpace = config.PatternSpace;
                oHatch.SetHatchPattern(hatchConfig.PatternType, hatchConfig.PatternName);
                oHatch.ColorIndex = (int)ColorIndex.BYLAYER;
                oHatch.Layer = hatchConfig.LayerName;
                var hatchId = acadDb.ModelSpace.Add(oHatch);
                oHatch.Associative = hatchConfig.Associative;
                if (holes.Count == 0)
                {
                    oHatch.AppendLoop((int)HatchLoopTypes.Default,
                        new ObjectIdCollection { shellId });
                }
                else
                {
                    oHatch.AppendLoop(HatchLoopTypes.Outermost,
                            new ObjectIdCollection { shellId });
                    holeIds.OfType<ObjectId>().ForEach(o =>
                    {
                        oHatch.AppendLoop(HatchLoopTypes.Default,
                            new ObjectIdCollection { o });
                    });
                }
                oHatch.EvaluateHatch(true);
                results.Add(hatchId);
            }
            results.Add(shellId);
            holeIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            return results;
        }
    }
}
