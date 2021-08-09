using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;

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
                    var objs = Polys.ToCollection();
                    var transformer = new ThMEPOriginTransformer(objs);
                    var newPts = new Point3dCollection();
                    pts.Cast<Point3d>().ForEach(o =>
                    {
                        var pt = new Point3d(o.X,o.Y,o.Z);
                        transformer.Transform(ref pt);
                        newPts.Add(pt);
                    });                                 
                    transformer.Transform(objs);
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    var querys = spatialIndex.SelectCrossingPolygon(newPts);
                    transformer.Reset(querys);
                    Polys = querys.Cast<Polyline>().ToList();
                }
            }
        }        
    }
}
