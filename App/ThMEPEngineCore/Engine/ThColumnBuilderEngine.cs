using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnBuilderEngine : IDisposable
    {
        private const double AREATOLERANCE = 1.0;
        private const double BUFFERTOLERANCE = 1.0;

        public ThColumnBuilderEngine()
        {
        }
        public void Dispose()
        {
        }

        public List<ThIfcColumn> Build(Database db, Point3dCollection pts)
        {
            // 获取数据
            var columns = new DBObjectCollection();
            var columnExtractor = new ThColumnExtractionEngine();
            columnExtractor.Extract(db);
            var db3ColumnExtractor = new ThDB3ColumnExtractionEngine();
            db3ColumnExtractor.Extract(db);         
            columnExtractor.Results.ForEach(e => columns.Add(e.Geometry));
            db3ColumnExtractor.Results.ForEach(e => columns.Add(e.Geometry));

            // 处理极远情况（>1E+10）
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(columns);
            var newPts = pts.OfType<Point3d>()
                .Select(o => transformer.Transform(o))
                .ToCollection();

            // 后处理
            columns = FilterInRange(columns, newPts);
            columns = Buffer(columns, -BUFFERTOLERANCE);
            columns = Preprocess(columns);
            columns = Buffer(columns, BUFFERTOLERANCE);

            // 回复到原位置
            transformer.Reset(columns);

            // 返回
            return columns.Cast<Polyline>().Select(e => ThIfcColumn.Create(e)).ToList();
        }        
        private DBObjectCollection Preprocess(DBObjectCollection columns)
        {
            var simplifier = new ThElementSimplifier();
            var results = columns.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Tessellate(columns);
            results = results.UnionPolygons();
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.MakeValid(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Normalize(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(AREATOLERANCE);
            return results;
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
        private DBObjectCollection Buffer(DBObjectCollection objs,double length)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            objs.Cast<Entity>().ForEach(e =>
            {
                var entity = bufferService.Buffer(e,length);
                if(entity!=null && GetArea(entity)>= AREATOLERANCE)
                {
                    results.Add(entity);
                }
            });
            return results;
        }
        private double GetArea(Entity polygon)
        {
            return polygon.ToNTSPolygon().Area;
        }
    }
}
