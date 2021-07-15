using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractOuterBoundaryService : ThExtractService
    {
        public List<Polyline> OuterBoundaries { get; set; }
        public ThExtractOuterBoundaryService()
        {
            OuterBoundaries = new List<Polyline>();
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
                            OuterBoundaries.Add(polyline.Clone() as Polyline);
                        }
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(OuterBoundaries.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    OuterBoundaries = objs.Cast<Polyline>().ToList();
                }               
            }
        }
    }
}
