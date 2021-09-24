using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPConcaveBuilder
    {
        private const double Point3dTolerance = 1.0;
        private DBObjectCollection Elements { get; set; }
        private double TesslateLength { get; set; }
        private double Thershold { get; set; }
        public ThMEPConcaveBuilder(DBObjectCollection objs, double thershold)
        {
            Elements = objs;
            Thershold = thershold;
            TesslateLength = Thershold / 1.2; //建议值
        }
        public DBObjectCollection Build()
        {
            // 移动
            var transform = new ThMEPOriginTransformer(Elements);
            transform.Transform(Elements);

            // 炸成基本元素(Line,Arc,Circle)
            var baseElements =  Explode(Elements); 

            // 打散
            var polys = Tesslate(baseElements);

            // 炸线
            var lines = Explode(polys);

            // 转点集合并过滤相近点
            var pts = TransPoints(lines);
            pts = FilterClosedPoints(pts, Point3dTolerance);

            // 构建
            var concaveHull = new ThCADCoreNTSConcaveHull(pts.ToNTSGeometry(), Thershold);
            var results = concaveHull.getConcaveHull().ToDbCollection();

            // 还原
            transform.Reset(results);
            transform.Reset(Elements);
            return results;
        }

        private Point3dCollection TransPoints(DBObjectCollection lines)
        {
            var results = new Point3dCollection();
            lines.Cast<Line>().ForEach(l =>
            {
                results.Add(l.StartPoint);
                results.Add(l.EndPoint);
            });
            return results;
        }

        private Point3dCollection FilterClosedPoints(Point3dCollection pts,double tolerance)
        {
            var kdTree = new ThCADCoreNTSKdTree(tolerance);
            pts.Cast<Point3d>().ForEach(p => kdTree.InsertPoint(p));
            return kdTree.Nodes.Keys
                .Select(k => k.Coordinate.ToAcGePoint3d())
                .ToCollection();
        }

        private DBObjectCollection Explode(DBObjectCollection objs)
        {
            var curves = new DBObjectCollection();
            objs.Cast<Entity>().ForEach(c =>
            {
                if(c is Arc || c is Circle || c is Line)
                {
                    curves.Add(c);
                }
                else if(c is Polyline poly)
                {
                    var subObjs = new DBObjectCollection();
                    poly.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(e => curves.Add(e));
                }
                else if(c is MPolygon mPolygon)
                {
                    mPolygon.Loops().ForEach(l=>curves.Add(l));
                }
                else 
                {
                    throw new NotSupportedException();
                }
            });
            return curves;
        }

        private DBObjectCollection Tesslate(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.Cast<Curve>().ForEach(c =>
            {
                if (c is Line line)
                {
                    results.Add(line.Tesslate(TesslateLength));
                }
                else if (c is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(TesslateLength));
                }
                else if(c is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(TesslateLength));
                }
                else if(c is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(TesslateLength));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
    }
}
