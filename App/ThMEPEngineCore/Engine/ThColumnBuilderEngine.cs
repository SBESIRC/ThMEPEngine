using System;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
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
        public ThColumnBuilderEngine()
        {
        }
        public void Dispose()
        {
        }

        public List<ThIfcColumn> Build(Database db, Point3dCollection pts)
        {
            //从原始数据提取
            var columns = new DBObjectCollection();
            var columnExtractor = new ThColumnExtractionEngine();
            columnExtractor.Extract(db);
            var db3ColumnExtractor = new ThDB3ColumnExtractionEngine();
            db3ColumnExtractor.Extract(db);
            columnExtractor.Results.ForEach(e=>columns.Add(e.Geometry));
            db3ColumnExtractor.Results.ForEach(e => columns.Add(e.Geometry));

            //移动到原点
            var center = GetCenter(columns);
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(columns);
            var newPts = new Point3dCollection();
            foreach(Point3d pt in pts)
            {
                var tempPt = new Point3d(pt.X,pt.Y,pt.Z);
                transformer.Transform(ref tempPt);
                newPts.Add(tempPt);
            }
            columns = FilterInRange(columns, newPts);
            columns = Preprocess(columns);
            transformer.Reset(columns);

            return columns.Cast<Polyline>().Select(e => ThIfcColumn.Create(e)).ToList();
        }        
        private DBObjectCollection Preprocess(DBObjectCollection columns)
        {
            var simplifier = new ThElementSimplifier();
            var results = columns.FilterSmallArea(1.0);
            results = simplifier.Tessellate(columns);
            results = results.UnionPolygons();
            results = results.FilterSmallArea(1.0);
            results = simplifier.MakeValid(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.Normalize(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(1.0);
            return results;
        }
        private Point3d GetCenter(DBObjectCollection columns)
        {
            var pts = new List<Point3d>();
            columns.Cast<Polyline>()
                .ForEach(o => pts.AddRange(o.EntityVertices().Cast<Point3d>().ToList()));
            var minX = pts.Select(o => o.X).OrderBy(o => o).FirstOrDefault();
            var minY = pts.Select(o => o.Y).OrderBy(o => o).FirstOrDefault();
            var maxX = pts.Select(o => o.X).OrderByDescending(o => o).FirstOrDefault();
            var maxY = pts.Select(o => o.Y).OrderByDescending(o => o).FirstOrDefault();
            return new Point3d((minX + maxX) / 2.0, (minY + maxY) / 2.0, 0.0);
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
