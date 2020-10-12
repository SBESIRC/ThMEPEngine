using System;
using Linq2Acad;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using System.Linq;

namespace ThMEPEngineCore.Engine
{
    public class ThShearWallRecognitionEngine : ThBuildingElementRecognitionEngine, IDisposable
    {
        public ThShearWallRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(database))
            {
                shearWallDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    shearWallDbExtension.ShearWallCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = shearWallDbExtension.ShearWallCurves;
                }
                curves.ForEach(o =>
                {
                    if (o is Polyline polyline && polyline.Length > 0.0)
                    {
                        Elements.Add(ThIfcWall.CreateWallEntity(polyline.Clone() as Polyline));
                    }
                });
            }
        }

        public static List<Polyline> PolygonPartition(Polyline polyline)
        //public static Polyline PolygonPartition(Polyline polyline)
        {
            var polylines = new List<Polyline>();

            // 手动闭合Polyline
            var poly = polyline.Clone() as Polyline;
            if (!polyline.Closed)
            {
                if (polyline.StartPoint.DistanceTo(polyline.EndPoint) <= 1)
                {
                    poly.RemoveVertexAt(poly.NumberOfVertices - 1);
                    poly.AddVertexAt(polyline.NumberOfVertices - 1, polyline.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                else
                { 
                    poly.AddVertexAt(polyline.NumberOfVertices, polyline.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                poly.Closed = true;
            }
            else
            {
                if (!polyline.StartPoint.IsEqualTo(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1)))
                {
                    poly.AddVertexAt(polyline.NumberOfVertices, polyline.StartPoint.ToPoint2D(), 0, 0, 0);
                }
            }

            // 剔除无用边缘点（相邻三点是否共线）
            poly = RemoveCollinearPoints(poly);

            // 取出多段线所有边缘
            var poly_Edge = new PolylineSegmentCollection(poly);

            // 定义顶点的凹凸性(目前仅适用于2D点)：
            // 凸点(点的两条边构成的角度，在多边形内部的有90°)  - 记录为 0
            // 凹点(点的两条边构成的角度，在多边形内部的有270°) - 记录为 1
            var Vertices = GetVerticesProperty(poly);

            // 确定所有凹点的延伸方向
            var lineExtend = new List<Line>();
            var points = new List<Tuple<Point2d,int>>();
            foreach (var item in Vertices)
            {
                var vector_extend = item.Value;
                if (item.Key.Item2 == -1)
                {
                    var intersect = IntersectWith(poly_Edge, Vertices, item.Key.Item1, item.Value);
                    vector_extend = item.Key.Item1.GetVectorTo(intersect.Item2);
                    if (intersect.Item3 == 0) 
                    {
                        points.Add(Tuple.Create(intersect.Item2,0));
                    }
                    lineExtend.Add(new Line(item.Key.Item1.ToPoint3d(), (item.Key.Item1 + vector_extend).ToPoint3d()));
                }
                points.Add(Tuple.Create(item.Key.Item1, item.Key.Item2));
            }

            var pts = points.OrderBy(o => o.Item1.Y).ThenBy(o => o.Item1.X).ToList();
            while (pts.Count >= 4) 
            {
                pts = pts.OrderBy(o => Math.Floor(o.Item1.Y)).ThenBy(o => Math.Floor(o.Item1.X)).ToList();
                var pt_ld = pts.First();
                // 排除误绘点
                if (pts.Find(o => (Math.Abs(o.Item1.X - pt_ld.Item1.X) < 5.0) && (o.Item1.Y - pt_ld.Item1.Y) != 0.0) == null)
                {
                    pts.Remove(pt_ld);
                    continue;
                }

                var pt_rd = pts.FindAll(o => (Math.Abs(o.Item1.Y - pt_ld.Item1.Y) < 5.0) && ((o.Item1.X - pt_ld.Item1.X) != 0.0));
                pt_rd = pt_rd.OrderBy(o => Math.Floor(o.Item1.Y)).ThenByDescending(o => Math.Floor(o.Item1.X)).ToList();

                var pt_ru = pts.FindAll(o => ((Math.Abs(o.Item1.X - pt_rd.First().Item1.X) < 5.0) && (o.Item1.Y - pt_rd.First().Item1.Y != 0.0)));
                while (pts.Find(o => (Math.Abs(o.Item1.Y - pt_ru.First().Item1.Y) < 5.0) && (Math.Abs(o.Item1.X - pt_ld.Item1.X) < 5.0)) == null)
                {
                    pts.Remove(pt_ru.First());
                    pt_ru.Remove(pt_ru.First());
                }

                var pt_lu = pts.Find(o => (Math.Abs(o.Item1.Y - pt_ru.First().Item1.Y) < 5.0) && (Math.Abs(o.Item1.X - pt_ld.Item1.X) < 5.0));

                Polyline pline = new Polyline()
                {
                    Closed = true
                };
                pline.AddVertexAt(0, pt_ld.Item1, 0.0, 0.0, 0.0);
                pline.AddVertexAt(1, pt_rd.First().Item1, 0.0, 0.0, 0.0);
                pline.AddVertexAt(2, pt_ru.First().Item1, 0.0, 0.0, 0.0);
                pline.AddVertexAt(3, pt_lu.Item1, 0.0, 0.0, 0.0);
                polylines.Add(pline);

                pts.Remove(pt_ld);
                pt_rd.ForEach(o => pts.Remove(o));
                if (pt_ru.First().Item2 == 1) pts.Remove(pt_ru.First());
                if (pt_lu.Item2 == 1) pts.Remove(pt_lu);
            }

            return polylines;
        }

        // 移除polyline边缘线上的无用顶点
        private static Polyline RemoveCollinearPoints(Polyline poly)
        {
            var index_remove = new List<int>();
            for (int i = 0; i < (poly.NumberOfVertices - 1); i++)
            {
                var pt = poly.GetPoint2dAt(i);
                var pt_prev = (i == 0) ? (poly.GetPoint2dAt(poly.NumberOfVertices - 2)) : (poly.GetPoint2dAt(i - 1));
                var pt_next = poly.GetPoint2dAt(i + 1);
                if (Math.Sin(pt.GetVectorTo(pt_next).GetAngleTo(pt_prev.GetVectorTo(pt))) < 1e-4)
                {
                    index_remove.Add(i);
                }
            }
            index_remove.Reverse();
            foreach (int i in index_remove)
            {
                poly.RemoveVertexAt(i);
            }
            return poly;
        }

        private static Dictionary<Tuple<Point2d, int>, Vector2d> GetVerticesProperty(Polyline polyline)
        {
            var points = new Dictionary<Tuple<Point2d, int>, Vector2d>();
            var convexVertex = -1;
            var concaveVertex = 1;
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var pt = polyline.GetPoint2dAt(i);
                var pt_prev = (i == 0) ? (polyline.GetPoint2dAt(polyline.NumberOfVertices - 2)) : (polyline.GetPoint2dAt(i - 1));
                var pt_next = polyline.GetPoint2dAt(i + 1);

                // 逆时针取点，多边形区域始终在边缘的左侧
                var vector_prev = pt_prev.GetVectorTo(pt);
                var vector_next = pt.GetVectorTo(pt_next);
                var rho = Math.Asin((vector_prev.X * vector_next.Y - vector_prev.Y * vector_next.X) / (vector_prev.Length * vector_next.Length));
                var theta = (rho < 0) ?
                    Math.Acos(vector_prev.DotProduct(vector_next) / (vector_prev.Length * vector_next.Length)) * (-1) :
                    Math.Acos(vector_prev.DotProduct(vector_next) / (vector_prev.Length * vector_next.Length));

                // 确定每一个凹点的延伸方向（横向延伸）
                var vectorX_prev = vector_prev.GetAngleTo(new Vector2d(1.0, 0.0));
                var vectorY_next = vector_next.GetAngleTo(new Vector2d(1.0, 0.0));
                var vector_extend = (Math.Sin(vectorX_prev) < Math.Sin(vectorY_next)) ? vector_prev / vector_prev.Length : (-vector_next / vector_next.Length);

                // 排除绘制错误的造成的凹点
                var misPlot = ((pt.GetDistanceTo(pt_prev) <= 1) || (pt.GetDistanceTo(pt_next) <= 1)) ? -1 : 1;
                var pt_property = (ThCADCoreNTSDbExtension.IsCCW(polyline) && theta < 0) || (!ThCADCoreNTSDbExtension.IsCCW(polyline) && theta > 0) ?
                    (Tuple.Create(pt, convexVertex * misPlot, vector_extend * 2000)) :
                    (Tuple.Create(pt, concaveVertex, vector_next));
                points.Add(Tuple.Create(pt_property.Item1,pt_property.Item2),pt_property.Item3);
            }
            return points;
        }

        private static Tuple<PolylineSegment, Point2d, int> IntersectWith(PolylineSegmentCollection polylineSegments, Dictionary<Tuple<Point2d, int>, Vector2d> Vertices, Point2d pt, Vector2d vector)
        {
            var intersectPoint = new Point2d();
            var intersectLine = new PolylineSegment(pt, (pt + vector));
            var intersectpt_property = 0;

            // 找到凹点延伸线上所有与之相交的交点
            double dist = 2000.0;
            var linesegment = new LineSegment2d(pt, vector);
            var intersectPts = new List<Tuple<PolylineSegment, Point2d>>();
            polylineSegments.ForEach(o =>
            {
                var linesegmentLine = new Line(pt.ToPoint3d(), (pt + vector).ToPoint3d());
                var pts = linesegment.IntersectWith(o.ToLineSegment());
                if (pts != null)
                {
                    for (int j = 0; j < pts.Length; j++)
                    {
                        intersectPts.Add(Tuple.Create(o, pts[j]));
                    }
                }
            });

            // 找到使凹点延伸长度最近的线
            if (intersectPts == null)
            {
                throw new NotSupportedException();
            }
            else
            {
                for (int i = 0; i < intersectPts.Count; i++)
                {
                    if (pt.GetDistanceTo(intersectPts[i].Item2) <= dist &&
                        pt.GetDistanceTo(intersectPts[i].Item2) != 0)
                    {
                        dist = pt.GetDistanceTo(intersectPts[i].Item2);
                        intersectPoint = intersectPts[i].Item2;
                        intersectLine = intersectPts[i].Item1;
                    }
                }
            }

            // 如果交点附近有另一个凹点，则与另一个凹点相连
            Vertices.ForEach(o =>
            {
                var pline = (new PolylineSegmentCollection(new PolylineSegment(pt, intersectPoint))).ToPolyline();
                if ((o.Key.Item1.GetDistanceTo(intersectPoint) <= 1) ||
                ((pline.Distance(o.Key.Item1.ToPoint3d()) <= 1) && (pline.Distance(o.Key.Item1.ToPoint3d()) > 0)))
                {
                    intersectPoint = o.Key.Item1;
                    intersectpt_property = -1;
                }
            });
            return Tuple.Create(intersectLine, intersectPoint, intersectpt_property);
        }
    }
}
