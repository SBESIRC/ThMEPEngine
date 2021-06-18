using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractArcService : ThExtractService
    {
        public List<Arc> Arcs { get; set; }
        public ThExtractArcService()
        {
            Arcs = new List<Arc>();
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                Arcs = acadDatabase.ModelSpace
                    .OfType<Arc>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o=>o.Clone() as Arc)
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Arcs.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Arcs = objs.Cast<Arc>().ToList();
                }
            }
        }        

        public override bool IsElementLayer(string layer)
        {
            return layer.ToUpper() == ElementLayer.ToUpper();
        }
    }
}
