using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractOuterOtherShearWallService : ThExtractService,IShearWallData
    {
        public List<Entity> OuterShearWalls { get; set; }
        public List<Entity> OtherShearWalls { get; set; }
        public int OuterColorIndex { get; set; }
        public int OtherColorIndex { get; set; }
        public ThExtractOuterOtherShearWallService()
        {
            OuterShearWalls = new List<Entity>();
            OtherShearWalls = new List<Entity>();
            OuterColorIndex = 3;
            OtherColorIndex = 256;
        }

        public override void Extract(Database db, Point3dCollection pts)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var shearWalls = acadDb.ModelSpace.OfType<Polyline>().Where(o => IsElementLayer(o.Layer)).ToList();
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(shearWalls.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    shearWalls = objs.Cast<Polyline>().ToList();
                }
                // 0 ->ByBlock,256->ByLayer
                OuterShearWalls = shearWalls.Where(o => o.ColorIndex == OuterColorIndex).Select(o=>o.Clone() as Entity).ToList();
                OtherShearWalls = shearWalls.Where(o => o.ColorIndex == OtherColorIndex).Select(o => o.Clone() as Entity).ToList();
            }
        }

        public override bool IsElementLayer(string layer)
        {
            return ElementLayer.ToUpper() == layer;
        }
    }
}
