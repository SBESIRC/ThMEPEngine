using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.Algorithm.ArcAlgorithm
{
    public static class ThArcPolygonize
    {
        /// <summary>
        /// Polygonize
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="arcChord"></param>
        /// <returns></returns>
        public static List<Polyline> Polygonize(List<Curve> curves, double arcChord)      //仅支持圆弧、直线、polyline直线（ps：最好全部用直线）
        {
            var allLines = curves.ConvertToLine(arcChord);
            allLines = allLines.Select(x => x.ExtendLine(2)).ToList();
            var polygons = allLines.ToCollection().PolygonsEx().Cast<Polyline>().Where(x => x.Area > 1).ToList();

            var resPolygons = new List<Polyline>();
            foreach (var polygon in polygons)
            {
                var polygonPts = new Dictionary<Point3d, Curve>();       //判断点是否在弧上
                for (int i = 0; i < polygon.NumberOfVertices; i++)
                {
                    var pt = polygon.GetPoint3dAt(i);
                    var curvePt = curves.Where(x => x.GetClosestPointTo(pt, false).DistanceTo(pt) < 1).FirstOrDefault();
                    if (!polygonPts.Keys.Contains(pt))
                    {
                        polygonPts.Add(pt, curvePt);
                    }
                }
                resPolygons.Add(CreateNewPolygon(polygonPts));
            }

            return resPolygons;
        }

        /// <summary>
        /// 根据polygon和切割出polygon的线还原出带弧的polygon
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static Polyline ResetArcPolygon(this Polyline polygon, List<Curve> curves)
        {
            var polygonPts = new Dictionary<Point3d, Curve>();       //判断点是否在弧上
            for (int i = 0; i < polygon.NumberOfVertices; i++)
            {
                var pt = polygon.GetPoint3dAt(i);
                var curvePt = curves.Where(x => x.GetClosestPointTo(pt, false).DistanceTo(pt) < 5).FirstOrDefault();
                if (!polygonPts.Keys.Contains(pt))
                {
                    polygonPts.Add(pt, curvePt);
                }
            }
            return CreateNewPolygon(polygonPts);
        }

        /// <summary>
        /// 创建新外包框
        /// </summary>
        /// <param name="polygonDic"></param>
        /// <returns></returns>
        private static Polyline CreateNewPolygon(Dictionary<Point3d, Curve> polygonDic)
        {
            Curve preCurve = null;
            Point3d? prePoint = null;
            Polyline polyline = new Polyline() { Closed = true };
            foreach (var polyDic in polygonDic)
            {
                double bulge = 0;
                var thisPt = polyDic.Key;
                Point3d? centerPt = null;
                if (preCurve == polyDic.Value || preCurve == null)
                {
                    if (polyDic.Value is Arc arcPoly)
                    {
                        centerPt = arcPoly.Center;
                    }
                }
                else
                {
                    if (preCurve is Arc || polyDic.Value is Arc)
                    {
                        Point3dCollection pts = new Point3dCollection();
                        preCurve.IntersectWith(polyDic.Value, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count > 0)
                        {
                            thisPt = pts[0];
                        }
                        if (polyDic.Value is Arc arcPolyValue)
                        {
                            centerPt = arcPolyValue.Center;
                        }
                        else if (preCurve is Arc arcPoly)
                        {
                            centerPt = arcPoly.Center;
                        }
                    }
                }
                if (prePoint != null && centerPt != null)
                {
                    var dir1 = (prePoint.Value - centerPt.Value).GetNormal();
                    var dir2 = (polyDic.Key - centerPt.Value).GetNormal();
                    var angle = Math.Abs(dir1.GetAngleTo(dir2));
                    bulge = Math.Tan(angle / 4);
                    if (angle > Math.PI)
                    {
                        bulge = -bulge;
                    }
                }
                polyline.AddVertexAt(polyline.NumberOfVertices, thisPt.ToPoint2D(), bulge / 4, 0, 0);
                preCurve = polyDic.Value;
                prePoint = polyDic.Key;
            }

            return polyline;
        }

        /// <summary>
        /// 延申线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static Line ExtendLine(this Line line, double tol)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            return new Line(line.StartPoint - dir * tol, line.EndPoint + dir * tol);
        }

        /// <summary>
        /// 将curve转成line
        /// </summary>
        /// <param name="girds"></param>
        /// <param name="arcChord"></param>
        /// <returns></returns>
        private static List<Line> ConvertToLine(this List<Curve> curves, double arcChord)
        {
            List<Line> resLines = new List<Line>();
            foreach (var curve in curves)
            {
                if (curve is Line line)
                {
                    if (line.Length > 1)
                    {
                        resLines.Add(line);
                    }
                }
                else if (curve is Polyline)
                {
                    var objs = new DBObjectCollection();
                    curve.Explode(objs);
                    resLines.AddRange(objs.Cast<Line>());
                }
                else if (curve is Arc arc)
                {
                    var polyline = arc.TessellateArcWithChord(arcChord);
                    var entitySet = new DBObjectCollection();
                    polyline.Explode(entitySet);
                    foreach (var obj in entitySet)
                    {
                        resLines.Add(obj as Line);
                    }
                }
            }
            return resLines;
        }
    }
}