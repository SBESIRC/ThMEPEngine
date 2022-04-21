using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System.Linq;

namespace ThMEPStructure.StructPlane.Print
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
                dbText.ColorIndex = (int)ColorIndex.BYLAYER;
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
                oHatch.ColorIndex = (int)ColorIndex.BYLAYER;
                oHatch.Layer = config.LayerName;
                //oHatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
                var hatchId = acadDatabase.ModelSpace.Add(oHatch);
                oHatch.Associative = config.Associative;
                oHatch.AppendLoop((int)HatchLoopTypes.Default, objIds);
                oHatch.EvaluateHatch(true);
                return hatchId;
            }
        }
    }
}
