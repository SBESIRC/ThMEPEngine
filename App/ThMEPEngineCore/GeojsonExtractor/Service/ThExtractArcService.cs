using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public class ThExtractArcService : ThExtractService
    {
        public List<Polyline> Arcs { get; set; }
        public ThExtractArcService()
        {
            Arcs = new List<Polyline>();
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                Arcs = acadDatabase.ModelSpace
                    .OfType<Arc>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o=>(o.Clone() as Arc).TessellateArcWithArc(TesslateLength))
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Arcs.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Arcs = objs.Cast<Polyline>().ToList();
                }
            }
        }        

        public override bool IsElementLayer(string layer)
        {
            return layer.ToUpper() == ElementLayer.ToUpper();
        }
    }
}
