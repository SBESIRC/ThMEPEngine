using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public class ThExtractPolylineService : ThExtractService
    {
        public List<Polyline> Polys { get; set; }
        public ThExtractPolylineService()
        {
            Polys = new List<Polyline>();
        }

        public override void Extract(Database db, Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {
                Polys = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o => o.Clone())
                    .OfType<Polyline>()
                    .ToList();

                if (Polys.Count > 0)
                {
                    using (var ov = new ThCADCoreNTSArcTessellationLength(TesslateLength))
                    {
                        var objs = Polys.ToCollection();
                        var transformer = new ThMEPOriginTransformer(objs);
                        // 以下操作都需要在近点完成
                        transformer.Transform(objs);
                        var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                        // 利用空间索引将多段线打断
                        Polys = spatialIndex.SelectAll().OfType<Polyline>().ToList();

                        // 空间索引
                        if (pts.Count >= 3)
                        {
                            var newPts = new Point3dCollection();
                            pts.OfType<Point3d>().ForEach(o =>
                            {
                                var pt = new Point3d(o.X, o.Y, o.Z);
                                transformer.Transform(ref pt);
                                newPts.Add(pt);
                            });
                            Polys = spatialIndex.SelectCrossingPolygon(newPts).OfType<Polyline>().ToList();
                        }

                        // 挪回远点
                        Polys.ForEach(o => transformer.Reset(o));
                    }
                }
            }
        }
    }
}
