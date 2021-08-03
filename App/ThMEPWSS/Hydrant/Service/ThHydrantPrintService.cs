using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Colors;
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
                if(entity is Polyline)
                {
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
                    oHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                    oHatch.ColorIndex = colorIndex;
                    oHatch.Layer = LayerName;
                    oHatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
                    acadDatabase.ModelSpace.Add(oHatch);
                    //this works ok  
                    oHatch.Associative = true;
                    oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                    oHatch.EvaluateHatch(true);
                }
                else if(entity is MPolygon mPolygon)
                {
                    mPolygon.Normal = new Vector3d(0.0, 0.0, 1.0);
                    mPolygon.Elevation = 0.0;
                    mPolygon.PatternScale = 2.0;
                    mPolygon.SetPattern(HatchPatternType.PreDefined, "SOLID");
                    mPolygon.Layer = LayerName;
                    mPolygon.ColorIndex = colorIndex;
                    mPolygon.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
                    acadDatabase.ModelSpace.Add(mPolygon);
                    mPolygon.PatternColor = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
                    mPolygon.EvaluateHatch(true);
                }                
            }
        }
        public void Erase(Point3dCollection pts)
        {
            using (var databse = AcadDatabase.Use(Db))
            {
                var layerId = databse.Database.CreateAILayer(LayerName, 30);
                Db.UnOffLayer(LayerName);
                Db.UnLockLayer(LayerName);
                Db.UnFrozenLayer(LayerName);
                var ents = databse.ModelSpace
                    .OfType<Entity>()
                    .Where(e => IsElementLayer(e.Layer)).ToList();
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(ents.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    ents = objs.Cast<Entity>().ToList();
                }
                ents.ForEach(e =>
                    {
                        e.UpgradeOpen();
                        e.Erase();
                        e.DowngradeOpen();
                    });
            }
        }
        private bool IsElementLayer(string layer)
        {
            return layer.ToUpper() == LayerName.ToUpper();
        }
    }
}
