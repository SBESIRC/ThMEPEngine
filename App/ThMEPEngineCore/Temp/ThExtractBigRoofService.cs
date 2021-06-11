using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractBigRoofService : ThExtractService
    {
        public List<Entity> BigRoofs { get; set; }
        public ThExtractBigRoofService()
        {
            BigRoofs = new List<Entity>();
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
                    if (ent is Polyline polyline)
                    {
                        if (IsElementLayer(polyline.Layer))
                        {                            
                            BigRoofs.Add(polyline.Clone() as Polyline);
                        }
                    }
                }
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(BigRoofs.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    BigRoofs = objs.Cast<Entity>().ToList();
                }
            }
        }

        public override bool IsElementLayer(string layer)
        {
            return layer==ElementLayer;
        }
    }
}
