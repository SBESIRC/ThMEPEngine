using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThHydrantPrintService
    {        
        private Database Db { get; set; }
        private string LayerName { get; set; }
        public ThHydrantPrintService(Database database,string layerName)
        {
            Db = database;
            LayerName = layerName;
        }
        /// <summary>
        /// 打印区域校核的结果
        /// </summary>
        /// <param name="checkRegionResults"></param>
        public void Print(Dictionary<Entity, List<Tuple<Entity, bool, int>>> checkRegionResults)
        {
            checkRegionResults.ForEach(o=>
            {
                o.Value.ForEach(v =>
                {
                    if (v.Item2 == false)
                    {
                        Print(v.Item1, v.Item3);
                    }

                });
            });
        }
        private void Print(Entity entity,int colorIndex)
        {
            // 填充
            using (var acadDatabase = AcadDatabase.Use(Db))
            {
                var layerId = acadDatabase.Database.CreateAILayer(LayerName, 30);
                var ObjIds = new ObjectIdCollection();
                var clone = entity.Clone() as Entity;
                ObjIds.Add(acadDatabase.ModelSpace.Add(clone));
                clone.ColorIndex = colorIndex;
                clone.Layer = LayerName;

                Hatch oHatch = new Hatch();
                var normal = new Vector3d(0.0, 0.0, 1.0);
                oHatch.Normal = normal;
                oHatch.Elevation = 0.0;
                oHatch.PatternScale = 2.0;
                oHatch.SetHatchPattern(HatchPatternType.PreDefined, "ZIGZAG");
                oHatch.ColorIndex = colorIndex;
                oHatch.Layer = LayerName;
                acadDatabase.ModelSpace.Add(oHatch);
                //this works ok  
                oHatch.Associative = true;
                oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                oHatch.EvaluateHatch(true);
            }
        }
    }
}
