using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.Temp
{
    public class ThExtractWaterSupplyStartService : ThExtractService
    {
        public List<Curve> WaterSupplyStarts { get; set; }
        public string WaterSupplyStartLayer { get; set; }
        public ThExtractWaterSupplyStartService()
        {
            WaterSupplyStarts = new List<Curve>();
            WaterSupplyStartLayer = "给水起点";
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                WaterSupplyStarts = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => IsWaterSupplyStartLayer(o.Layer))
                    .Select(o=>o.Clone() as Curve)
                    .ToList();

                for (int i = 0; i < WaterSupplyStarts.Count; i++)
                {
                    if (WaterSupplyStarts[i] is Circle circle)
                    {
                        WaterSupplyStarts[i] = circle.GeometricExtents.ToRectangle();
                    }
                }

                if (pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(WaterSupplyStarts.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    WaterSupplyStarts = objs.Cast<Curve>().ToList();
                }
            }
        }        

        private bool IsWaterSupplyStartLayer(string layerName)
        {
            return layerName == WaterSupplyStartLayer;
        }
    }
}
