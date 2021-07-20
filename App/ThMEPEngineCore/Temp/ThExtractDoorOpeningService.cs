using NFox.Cad;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractDoorOpeningService : ThExtractService
    {
        public List<Polyline> Openings { get; set; }
        public ThExtractDoorOpeningService()
        {
            Openings = new List<Polyline>();
        }

        public override void Extract(Database db, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsElementLayer(polyline.Layer))
                        {
                            Openings.Add(polyline.Clone() as Polyline);
                        }
                    }
                }
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Openings.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Openings = objs.Cast<Polyline>().ToList();
                }
            }
        }
    }
}
