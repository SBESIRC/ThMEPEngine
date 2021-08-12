using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThShearwallBuilderEngine : IDisposable
    {
        private const double AREATOLERANCE = 1.0;
        private const double BUFFERTOLERANCE = 1.0;

        public ThShearwallBuilderEngine()
        {
        }
        public void Dispose()
        {
        }

        public List<ThIfcWall> Build(Database db, Point3dCollection pts)
        {
            // 获取数据
            var walls = new DBObjectCollection();
            var shearwallExtractor = new ThShearWallExtractionEngine();
            shearwallExtractor.Extract(db);
            var db3ShearwallExtractor = new ThDB3ShearWallExtractionEngine();
            db3ShearwallExtractor.Extract(db);
            shearwallExtractor.Results.ForEach(e => walls.Add(e.Geometry));
            db3ShearwallExtractor.Results.ForEach(e => walls.Add(e.Geometry));

            // 处理极远情况（>1E+10）
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(walls);
            var newPts = pts.OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();

            // 后处理
            walls = FilterInRange(walls, newPts);
            
            // 回复到原位置
            transformer.Reset(walls);

            // 返回
            return walls.Cast<Polyline>().Select(e => ThIfcWall.Create(e)).ToList();
        }        

        private DBObjectCollection FilterInRange(DBObjectCollection objs, Point3dCollection pts)
        {
            if (pts.Count > 2)
            {
                var results = new DBObjectCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(pts);
                foreach (DBObject filterObj in spatialIndex.SelectCrossingPolygon(pline))
                {
                    results.Add(filterObj);
                }
                return results;
            }
            else
            {
                return objs;
            }
        }
    }
}
