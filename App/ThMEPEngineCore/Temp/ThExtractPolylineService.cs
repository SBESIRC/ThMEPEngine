using NFox.Cad;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractPolylineService:ThExtractService
    {
        public List<Polyline> Polys { get; set; }
        public ThExtractPolylineService()
        {
            Polys = new List<Polyline>();
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                Polys = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o=>o.Clone() as Polyline)
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Polys.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Polys = objs.Cast<Polyline>().ToList();
                }
            }
        }        

        public override bool IsElementLayer(string layer)
        {
            return layer == ElementLayer;
        }
    }
}
