using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public class ThExtractLineService : ThExtractService
    {
        public List<Line> Lines { get; private set; }
        public ThExtractLineService()
        {
            Lines = new List<Line>();
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                Lines = acadDatabase.ModelSpace
                    .OfType<Line>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o=> o.Clone() as Line)
                    .ToList();
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Lines.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Lines = objs.Cast<Line>().ToList();
                }
            }
        }        

        public override bool IsElementLayer(string layer)
        {
            return layer.ToUpper() == ElementLayer.ToUpper();
        }
    }
}
