using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThDrawTool
    {
        public static Polyline CreatePolyline(this Point3dCollection pts, bool isClosed = true, double lineWidth = 0.0)
        {
            Point2dCollection p2dPts = new Point2dCollection();
            foreach (Point3d pt in pts)
            {
                p2dPts.Add(new Point2d(pt.X, pt.Y));
            }
            return CreatePolyline(p2dPts, isClosed, lineWidth);
        }
        /// <summary>
        /// 创建没有圆弧的多段线
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Polyline CreatePolyline(Point2dCollection pts, bool isClosed = true, double lineWidth = 0)
        {
            Polyline polyline = new Polyline()
            {
                Closed= isClosed,
            };
            if (pts.Count == 2 && isClosed)
            {
                //如果要闭合，且画的是斜线,创建一个矩形
                Point2d minPt = pts[0];
                Point2d maxPt = pts[1];
                Vector2d vec = minPt.GetVectorTo(maxPt);
                if(vec.Length<1e-4)
                {
                    return polyline;
                }
                if (vec.IsParallelTo(Vector2d.XAxis) || vec.IsParallelTo(Vector2d.YAxis))
                {
                    isClosed = false;
                }
                else
                {
                    double minX = Math.Min(pts[0].X, pts[1].X);
                    double minY = Math.Min(pts[0].Y, pts[1].Y);
                    double maxX = Math.Max(pts[0].X, pts[1].X);
                    double maxY = Math.Max(pts[0].Y, pts[1].Y);
                    pts = new Point2dCollection();
                    pts.Add(new Point2d(minX, minY));
                    pts.Add(new Point2d(maxX, minY));
                    pts.Add(new Point2d(maxX, maxY));
                    pts.Add(new Point2d(minX, maxY));
                }
            }
            for (int i = 0; i < pts.Count; i++)
            {
                polyline.AddVertexAt(i, pts[i], 0, lineWidth, lineWidth);
            }
            if (isClosed)
            {
                polyline.Closed = true;
            }
            return polyline;
        }
        public static Polyline ToOutline(Point3d startPt,Point3d endPt, double width)
        {
            var line = new Line(startPt, endPt);
            return line.ExtendLine(1.0).Buffer(width / 2.0);
        }
        public static Polyline ToRectangle(Point3d startPt, Point3d endPt, double width)
        {
            var line = new Line(startPt, endPt);
            return line.Buffer(width / 2.0);
        }
        public static Polyline CreateSquare(this Point3d pt, double edgeLength)
        {
            return pt.CreateRectangle(edgeLength, edgeLength);
        }
        public static Polyline CreateRectangle(this Point3d pt, double length,double width)
        {
            Polyline polyline = new Polyline
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(pt.X + length / 2.0, pt.Y + width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(pt.X - length / 2.0, pt.Y + width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(pt.X - length / 2.0, pt.Y - width / 2.0), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(pt.X + length / 2.0, pt.Y - width / 2.0), 0, 0, 0);
            return polyline;
        }
        public static Polyline CreateRectangle(this Point3d center, Vector3d direction, double length, double width)
        {
            var sp = center - direction.GetNormal().MultiplyBy(length / 2.0);
            var ep = center + direction.GetNormal().MultiplyBy(length / 2.0);
            return ToRectangle(sp, ep, width);
        }
        public static List<Line> GetLines(this DBObjectCollection objs,double length=10.0)
        {
            var lines = new List<Line>();
            objs.Cast<Curve>().ForEach(o =>
            {
                if (o is Line line)
                {
                    lines.Add(line);
                }
                else if (o is Polyline polyline)
                {
                    var subObjs = new DBObjectCollection();
                    polyline.Explode(subObjs);
                    lines.AddRange(subObjs.Cast<Line>().ToList());
                }
                else if(o is Arc arc)
                {
                    var arcPolyline= arc.TessellateArcWithArc(length);
                    var subObjs = new DBObjectCollection();
                    arcPolyline.Explode(subObjs);
                    lines.AddRange(GetLines(subObjs, length));
                }
                else if(o is Circle circle)
                {
                    var circlePolyline = circle.Tessellate(length);
                    var subObjs = new DBObjectCollection();
                    circlePolyline.Explode(subObjs);
                    lines.AddRange(GetLines(subObjs, length));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return lines;
        }
        public static List<Line> ToLines(this Polyline polyline,double length=50.0)
        {
            var results = new List<Line>();
            var newPolyline=polyline.TessellatePolylineWithArc(length);
            var polylineSegments = new PolylineSegmentCollection(newPolyline);
            foreach (var segment in polylineSegments)
            {
                results.Add(new Line(segment.StartPoint.ToPoint3d(), segment.EndPoint.ToPoint3d()));
            }
            newPolyline.Dispose();
            polylineSegments.Clear();
            polylineSegments = null;
            return results;
        }

        public static DBObjectCollection ToLines(this DBObjectCollection objs,double arcTessellateLength)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(o =>
            {
                if (o is Line line)
                {
                    results.Add(line.Clone() as Line);
                }
                else if (o is Polyline polyline)
                {
                    polyline.ToLines(arcTessellateLength).ForEach(l => results.Add(l));
                }
                else if (o is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell();
                    var holes = mPolygon.Holes();
                    shell.ToLines(arcTessellateLength).ForEach(l => results.Add(l));
                    holes.SelectMany(h => h.ToLines(arcTessellateLength)).ForEach(l => results.Add(l));
                }
                else if (o is Arc arc)
                {
                    var newPoly = arc.TessellateArcWithArc(arcTessellateLength);
                    newPoly.ToLines().ForEach(l => results.Add(l));
                }
                else if(o is Circle circle)
                {
                    var newPoly = circle.TessellateCircleWithArc(arcTessellateLength);
                    newPoly.ToLines().ForEach(l => results.Add(l));
                }
                else
                {
                    //ToDo
                }
            });
            return results;
        }

        public static List<Line> ExplodeLines(this Polyline polyline,double arcLength=5.0)
        {
            var results = new List<Line>();
            var objs = new DBObjectCollection();
            polyline.Explode(objs);
            foreach (Curve curve in objs)
            {
                if(curve.GetLength()==0.0)
                {
                    continue;
                }
                if (curve is Line line)
                {
                    results.Add(line.WashClone() as Line);
                }
                else if(curve is Arc arc)
                {
                   var arcPoly = arc.TessellateArcWithArc(arcLength);
                    results.AddRange(ExplodeLines(arcPoly, arcLength));
                }
                else if (curve is Polyline subPoly)
                {
                    results.AddRange(ExplodeLines(subPoly, arcLength));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }

        /// <summary>
        /// 对于比例块或GeometryExtents有问题的块会有异常
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public static DBObjectCollection Explode(BlockReference br)
        {
            var results = new DBObjectCollection();
            try 
            {
                var objs = new DBObjectCollection();
                br.Explode(objs);
                foreach (Entity ent in objs)
                {
                    if (ent is BlockReference nestBr)
                    {
                        var nestObjs = Explode(nestBr);
                        foreach (Entity nestEnt in nestObjs)
                        {
                            results.Add(nestEnt);
                        }
                    }
                    else
                    {
                        results.Add(ent);
                    }
                }
            }
            catch
            {
                //当块为空块，或块定义为空，或块内部设置了不能炸开 会出现报错问题,这里进行简便处理，拦截错误。
            }
            return results;
        }
        public static Polyline ToRectangle(this Circle circle)
        {
            var polyline = new Polyline()
            {
                Closed = true,
            };
            polyline.AddVertexAt(0, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius), 0, 0, 0);
            polyline.AddVertexAt(1, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y + circle.Radius), 0, 0, 0);
            polyline.AddVertexAt(2, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius), 0, 0, 0);
            polyline.AddVertexAt(3, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y - circle.Radius), 0, 0, 0);
            return polyline;
        }
        public static Circle ToBoundingSphere(this List<Point3d> pts)
        {
            if (pts.Count == 0)
            {
                return new Circle();
            }
            //Find the max and min along the x-axie, y-axie, z-axie  
            int maxX = pts.IndexOf(pts.OrderByDescending(o => o.X).First());
            int minX = pts.IndexOf(pts.OrderBy(o => o.X).First());

            int maxY = pts.IndexOf(pts.OrderByDescending(o => o.Y).First());
            int minY = pts.IndexOf(pts.OrderBy(o => o.Y).First());

            int maxZ = pts.IndexOf(pts.OrderByDescending(o => o.Z).First());
            int minZ = pts.IndexOf(pts.OrderBy(o => o.Z).First());

            Vector3d vec1 = new Vector3d(pts[maxX].X, pts[maxX].Y, pts[maxX].Z);
            Vector3d vec2 = new Vector3d(pts[minX].X, pts[minX].Y, pts[minX].Z);
            vec1 = vec1.Subtract(vec2);
            double x = vec1.DotProduct(vec1);

            vec1 = new Vector3d(pts[maxY].X, pts[maxY].Y, pts[maxY].Z);
            vec2 = new Vector3d(pts[minY].X, pts[minY].Y, pts[minY].Z);
            vec1 = vec1.Subtract(vec2);
            double y = vec1.DotProduct(vec1);

            vec1 = new Vector3d(pts[maxZ].X, pts[maxZ].Y, pts[maxZ].Z);
            vec2 = new Vector3d(pts[minZ].X, pts[minZ].Y, pts[minZ].Z);
            vec1 = vec1.Subtract(vec2);
            double z = vec1.DotProduct(vec1);

            double dia = 0;
            int max = maxX, min = minX;
            if (z >= x && z >= y)
            {
                max = maxZ;
                min = minZ;
                dia = z;
            }
            else if (y >= x && y >= z)
            {
                max = maxY;
                min = minY;
                dia = y;
            }
            else if (x >= y && x >= z)
            {
                max = maxX;
                min = minX;
                dia = x;
            }

            //Compute the center point  
            Point3d center = new Point3d(
                0.5 * (pts[max].X + pts[min].X),
                0.5 * (pts[max].Y + pts[min].Y),
                0.5 * (pts[max].Z + pts[min].Z));

            //Compute the radious  
            double radious = 0.5 * Math.Sqrt(dia);

            //Fix it  
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3d d = pts[i] - center;
                double dist2 = d.DotProduct(d);
                if (dist2 > radious * radious)
                {
                    double dist = Math.Sqrt(dist2);
                    double newRadious = (dist + radious) * 0.5;
                    double k = (newRadious - radious) / dist;  //平移距离就是新半径-原来的半径，/dist是为了下面将方向向量归一化
                    radious = newRadious;
                    Vector3d temp = d.MultiplyBy(k);
                    center = center.Add(temp);
                }// end if  
            }// end for vertex_num

            return new Circle(center, Vector3d.ZAxis, radious);
        }
    }
}
