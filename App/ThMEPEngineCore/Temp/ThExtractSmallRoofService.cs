using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractSmallRoofService : ThExtractService
    {
        public List<Entity> SmallRoofs { get; set; }
        public ThExtractSmallRoofService()
        {
            SmallRoofs = new List<Entity>();
        }
        public override void Extract(Database db, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (!IsValidType(ent))
                    {
                        continue;
                    }
                    if (ent is Hatch hatch)
                    {
                        if (IsElementLayer(hatch.Layer))
                        {
                            var polygons = hatch.ToPolygons();
                            SmallRoofs.AddRange(polygons.Select(o => o.ToDbEntity()));
                        }
                    }
                }
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(SmallRoofs.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    SmallRoofs = objs.Cast<Entity>().ToList();
                }
            }
        }

        public override bool IsElementLayer(string layer)
        {
            return layer==ElementLayer;
        }
    }
}
