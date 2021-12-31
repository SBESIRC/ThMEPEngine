using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
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
        public static List<Polyline> ArcPolygonize(this List<Curve> curves, double arcChord)      //仅支持圆弧、直线、polyline直线（ps：最好全部用直线）
        {
            var allLines = curves.ConvertToLine(arcChord);
            allLines = allLines.Select(x => x.ExtendLine(2)).ToList();
            var polygons = allLines.ToCollection().PolygonsEx().Cast<Polyline>().Where(x => x.Area > 1).ToList();

            var resPolygons = new List<Polyline>();
            foreach (var polygon in polygons)
            {
                resPolygons.Add(ResetArcPolygon(polygon, curves));
            }

            return resPolygons;
        }

        /// <summary>
        /// Polygonize
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="arcChord"></param>
        /// <returns></returns>
        public static List<Polyline> ArcPolygonize(this List<Curve> curves, Polyline frame, double arcChord)      //仅支持圆弧、直线、polyline直线（ps：最好全部用直线）
        {
            var handleCurves = HandleLinesByFrame(frame, curves);
            //using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            //{
            //    var s = frame.Clone() as Polyline;
            //    s.ColorIndex = 4;
            //    db.ModelSpace.Add(s);
            //    foreach (var item in curves)
            //    {
            //        item.ColorIndex = 4;
            //        db.ModelSpace.Add(item);
            //    }
            //}
            var allLines = handleCurves.ConvertToLine(arcChord);
            allLines = allLines.Select(x => x.ExtendLine(50)).ToList();
            var polygons = allLines.ToCollection().Polygons().Cast<Polyline>().Where(x => x.Area > 1).ToList();

            var checkCurves = new List<Curve>(curves);
            DBObjectCollection collection = new DBObjectCollection();
            frame.Explode(collection);
            checkCurves.AddRange(collection.Cast<Curve>());
            var resPolygons = new List<Polyline>();
            foreach (var polygon in polygons)
            {
                resPolygons.Add(ResetArcPolygon(polygon, checkCurves));
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
            if (!polygon.IsCCW())
            {
                polygon.ReverseCurve();
            }
            var polygonPts = new Dictionary<Point3d, Curve>();       //判断点是否在弧上
            for (int i = 0; i < polygon.NumberOfVertices; i++)
            {
                var pt = polygon.GetPoint3dAt(i);
                var curvePts = curves.Where(x => x.GetClosestPointTo(pt, false).DistanceTo(pt) < 10).ToList();
                if (curvePts.Count > 0)
                {
                    curvePts = CleanIntersectLine(curvePts);
                    if (curvePts.Count > 1)
                    {
                        if (curvePts.Any(x => x is Arc))
                        {
                            var intersectPts = new List<Point3d>();
                            Point3dCollection pts = new Point3dCollection();
                            foreach (var checkCurve in curvePts)
                            {
                                foreach (var curvePt in curvePts.Except(new List<Curve>() { checkCurve }))
                                {
                                    curvePt.IntersectWith(checkCurve, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                                    intersectPts.AddRange(pts.Cast<Point3d>());
                                }
                                
                            }
                            if (intersectPts.Count > 0)
                            {
                                pt = intersectPts.OrderBy(x => x.DistanceTo(pt)).First();
                                if (polygon.GetPoint3dAt(i).DistanceTo(pt) > 10)
                                {
                                    pt = polygon.GetPoint3dAt(i);
                                }
                            }
                        }
                    }
                    if (!polygonPts.Keys.Contains(pt))
                    {
                        polygonPts.Add(pt, curvePts.Last());
                    }
                }
            }
            return CreateNewSimplifyPolygon(polygonPts);
        }

        /// <summary>
        /// 清洗多余求交线
        /// </summary>
        /// <param name="curves"></param>
        /// <returns></returns>
        private static List<Curve> CleanIntersectLine(List<Curve> curves)
        {
            List<Curve> resCurve = new List<Curve>();
            var lineCurves = curves.Where(x => x is Line).ToList();
            List<Vector3d> checkDirList = new List<Vector3d>();
            foreach (var line in lineCurves)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                if (!checkDirList.Any(x => x.IsParallelTo(dir, new Tolerance(0.01, 0.01))))
                {
                    resCurve.Add(line);
                }
                checkDirList.Add(dir);
            }

            var arcCurves = curves.Where(x => x is Arc).ToList();
            List<Point3d> checkArcList = new List<Point3d>();
            foreach (Arc arc in arcCurves)
            {
                if (!checkArcList.Any(x => x.DistanceTo(arc.Center) < 5))
                {
                    resCurve.Add(arc);
                }
                checkArcList.Add(arc.Center);
            }
            return resCurve;
        }

        /// <summary>
        /// 根据框线处理一下线不超过框线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="cutCurves"></param>
        private static List<Curve> HandleLinesByFrame(Polyline polyline, List<Curve> cutCurves)
        {
            var bufferFrame = polyline.Buffer(-5)[0] as Polyline;
            List<Curve> handleCurves = cutCurves.SelectMany(x => bufferFrame.Trim(x).OfType<Curve>()).ToList();
            DBObjectCollection collection = new DBObjectCollection();
            polyline.Explode(collection);
            handleCurves.AddRange(collection.Cast<Curve>());

            return handleCurves;
        }

        /// <summary>
        /// 创建新外包框
        /// </summary>
        /// <param name="polygonDic"></param>
        /// <returns></returns>
        private static Polyline CreateNewSimplifyPolygon(Dictionary<Point3d, Curve> polygonDic)
        {
            Polyline polyline = new Polyline() { Closed = true };
            var polygonKeys = polygonDic.Keys.ToList();
            for (int i = 0; i < polygonDic.Count; i++)
            {
                var checkPoly = polygonDic[polygonKeys[i]];
                double bulge = 0;
                var thisPt = polygonKeys[i];
                if (checkPoly is Arc arc)
                {
                    int j = i;
                    for (j = i; j <= polygonDic.Count; j++)
                    {
                        if (polygonDic[polygonKeys[j % polygonDic.Count]] != checkPoly &&
                            !(polygonDic[polygonKeys[j % polygonDic.Count]] is Arc polyArc && polyArc.Center.DistanceTo((checkPoly as Arc).Center) < 5
                             && Math.Abs(polyArc.Radius - (checkPoly as Arc).Radius) < 5))
                        {
                            j = j - 1;
                            break;
                        }
                    }
                    if (j > polygonDic.Count)
                    {
                        j--;
                    }
                    var nextPt = polygonKeys[j % polygonDic.Count];
                    var centerPt = arc.Center;
                    var dir1 = (thisPt - centerPt).GetNormal();
                    var dir2 = (nextPt - centerPt).GetNormal();
                    bool isClockWise = false;
                    if (Vector3d.XAxis.GetAngleTo(dir1, Vector3d.ZAxis) > Vector3d.XAxis.GetAngleTo(dir2, Vector3d.ZAxis))
                    {
                        dir1 = (nextPt - centerPt).GetNormal();
                        dir2 = (thisPt - centerPt).GetNormal();
                        isClockWise = true;
                    }
                    Arc tempArc = new Arc(centerPt, arc.Radius, Vector3d.XAxis.GetAngleTo(dir1, Vector3d.ZAxis), Vector3d.XAxis.GetAngleTo(dir2, Vector3d.ZAxis));
                    bulge = tempArc.BulgeFromCurve(isClockWise);
                    if (nextPt.DistanceTo(thisPt) > 1)
                    {
                        j = j - 1;
                    }
                    i = j;
                }
                polyline.AddVertexAt(polyline.NumberOfVertices, thisPt.ToPoint2D(), bulge, 0, 0);
            }
            return polyline;
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
            Point3d? centerPt = null;
            foreach (var polyDic in polygonDic)
            {
                double bulge = 0;
                var thisPt = polyDic.Key;
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
                    centerPt = null;
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