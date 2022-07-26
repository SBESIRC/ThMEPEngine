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
        public static ObjectIdCollection Print(this DBObjectCollection objs, Database db)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var results = new ObjectIdCollection();
                objs.OfType<Entity>().ForEach(e =>
                    {
                        results.Add(acadDatabase.ModelSpace.Add(e));
                    });
                return results;
            }
        }
        public static ObjectId Print(this Entity entity, Database db, PrintConfig config)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var objId = acadDatabase.ModelSpace.Add(entity);
                entity.Layer = config.LayerName;
                entity.Linetype = config.LineType;
                entity.LineWeight = config.LineWeight;
                entity.ColorIndex = config.Color;
                if(config.LineTypeScale.HasValue)
                {
                    entity.LinetypeScale = config.LineTypeScale.Value;
                }                
                return objId;
            }
        }
        public static ObjectId Print(this DBText dbText, Database db, AnnotationPrintConfig config)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                // 传入的文字几何位置已经确定了，这儿只设置相关属性                
                var objId = acadDatabase.ModelSpace.Add(dbText);
                dbText.Layer = config.LayerName;
                dbText.Height = config.Height;
                dbText.WidthFactor = config.WidthFactor;
                dbText.ColorIndex = config.Color;
                dbText.TextStyleId = DbHelper.GetTextStyleId(config.TextStyleName);
                return objId;
            }
        }
        public static ObjectId Print(this ObjectIdCollection objIds, Database db,  HatchPrintConfig config)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
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
                var hatchId = acadDatabase.ModelSpace.Add(oHatch);
                oHatch.Associative = config.Associative;
                oHatch.AppendLoop((int)HatchLoopTypes.Default, objIds);
                oHatch.EvaluateHatch(true);
                return hatchId;
            }
        }

        public static ObjectIdCollection Print(this MPolygon polygon, Database db,HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                var results = new ObjectIdCollection();
                var shell = polygon.Shell();
                var holes = polygon.Holes();                
                var shellId = shell.Print(db, outlineConfig);
                var holeIds = new ObjectIdCollection();
                holes.ForEach(h => holeIds.Add(h.Print(db, outlineConfig)));
                if(hatchConfig!=null)
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
                    var hatchId = acadDatabase.ModelSpace.Add(oHatch);
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
}
