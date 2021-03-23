using NFox.Cad;
using Linq2Acad;
using ThCADExtension;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractWaterSupplyPositionService : ThExtractService
    {
        public List<Curve> WaterSupplyPositions { get; set; }
        public string WaterSupplyPositionLayer { get; set; }
        public ThExtractWaterSupplyPositionService()
        {
            WaterSupplyPositions = new List<Curve>();
            WaterSupplyPositionLayer = "给水点位";
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                WaterSupplyPositions = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => IsWaterSupplyPositionLayer(o.Layer))
                    .Select(o=>o.Clone() as Curve)
                    .ToList();
                for(int i=0;i< WaterSupplyPositions.Count;i++)
                {
                    if (WaterSupplyPositions[i] is Circle circle)
                    {
                        WaterSupplyPositions[i] = circle.GeometricExtents.ToRectangle();
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(WaterSupplyPositions.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    WaterSupplyPositions = objs.Cast<Curve>().ToList();
                }
            }
        }        

        private bool IsWaterSupplyPositionLayer(string layerName)
        {
            return layerName == WaterSupplyPositionLayer;
        }
    }
}
