using NFox.Cad;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractToiletGroupService : ThExtractService
    {
        public List<Polyline> ToiletGroups { get; set; }
        public ThExtractToiletGroupService()
        {
            ToiletGroups = new List<Polyline>();
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                ToiletGroups = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o=>o.Clone() as Polyline)
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(ToiletGroups.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    ToiletGroups = objs.Cast<Polyline>().ToList();
                }
            }
        }     
    }
}
