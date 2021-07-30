using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPEngineCore.GeojsonExtractor.Service
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
                    .Select(o=>(o.Clone() as Polyline).Tessellate(TesslateLength))
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Polys.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Polys = objs.Cast<Polyline>().ToList();
                }
            }
        }        
    }
}
