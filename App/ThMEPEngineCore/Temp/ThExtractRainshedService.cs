using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractRainshedService : ThExtractService
    {
        public List<Entity> Rainsheds { get; set; }
        public ThExtractRainshedService()
        {
            Rainsheds = new List<Entity>();
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
                            Rainsheds.AddRange(polygons.Select(o => o.ToDbEntity()));
                        }
                    }
                }
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Rainsheds.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Rainsheds = objs.Cast<Entity>().ToList();
                }
            }
        }

        public override bool IsElementLayer(string layer)
        {
            return layer==ElementLayer;
        }
    }
}
