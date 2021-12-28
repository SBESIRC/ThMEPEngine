using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

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
                    var center = pts.Envelope().CenterPoint();
                    var transformer = new ThMEPOriginTransformer(center);
                    var newPts = transformer.Transform(pts);
                    Lines.ForEach(o => transformer.Transform(o));
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Lines.ToCollection());
                    Lines = spatialIndex.SelectCrossingPolygon(newPts).Cast<Line>().ToList();
                    Lines.ForEach(o => transformer.Reset(o));
                }
            }
        }        
    }
}
