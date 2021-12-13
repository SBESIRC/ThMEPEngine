using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public class StructPolyService
    {
        static double arcChord = 1000;
        /// <summary>
        /// 将所有柱连成柱网
        /// </summary>
        /// <param name="points"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Polyline StructPointToPolygon(Point3dCollection points, Polyline frame, out List<Polyline> triangles)
        {
            triangles = GetDelaunayTriangle(points, frame);
            var outBox = GeoUtils.GetLinesOutBox(triangles);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                outBox.ColorIndex = 2;
                //db.ModelSpace.Add(outBox);
            }
            return outBox;
        }

        /// <summary>
        /// 德劳内三角剖分
        /// </summary>
        /// <param name="points"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private static List<Polyline> GetDelaunayTriangle(Point3dCollection points, Polyline frame)
        {
            var triangles = points.DelaunayTriangulation().Cast<Polyline>().ToList();
            var polyTriangles = triangles.Where(x => !frame.LineIntersects(x)).ToList();
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (Polyline item in polyTriangles)
                {
                    //db.ModelSpace.Add(item);
                }
            }
            return polyTriangles;
        }

        /// <summary>
        /// 计算分割区域
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="columnInfos"></param>
        /// <param name="triangleOut"></param>
        /// <returns></returns>
        public static List<Polyline> CutRegion(Polyline frame, Dictionary<Polyline, Point3d> columnInfos, Polyline triangleOut, List<Polyline> triangles)
        {
            var allLines = frame.GetLinesByPolyline(arcChord);
            allLines.AddRange(triangles.SelectMany(x => x.GetLinesByPolyline(arcChord)));
            for (int i = 0; i < triangleOut.NumberOfVertices; i++)
            {
                var columnLst = columnInfos.Where(x => triangleOut.GetPoint3dAt(i).IsEqualTo(x.Value, new Tolerance(0.1, 0.1))).ToList();
                if (columnLst.Count > 0)
                {
                    var column = columnLst.OrderByDescending(x => x.Key.GetLinesByPolyline(500).Count).First();
                    var columnVectors = GetColumnVector(column.Key, column.Value, triangleOut);     //寻找切割方向
                    var cutLines = GetCutLine(frame, column.Value, columnVectors, triangleOut);     //寻找切割线
                    allLines.AddRange(cutLines);
                }
            }
            var cutRegionPoly = allLines.ToCollection().PolygonsEx().Cast<Polyline>().ToList();
            cutRegionPoly.AddRange(frame.Difference(cutRegionPoly.ToCollection()).Cast<Polyline>());
            return cutRegionPoly;
        }

        /// <summary>
        /// 计算切割线
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="centerPt"></param>
        /// <param name="vecs"></param>
        /// <param name="triangleOut"></param>
        /// <returns></returns>
        private static List<Line> GetCutLine(Polyline frame, Point3d centerPt, List<Vector3d> vecs, Polyline triangleOut)
        {
            List<Line> cutLines = new List<Line>();
            foreach (var vec in vecs)
            {
                Ray ray = new Ray();
                ray.BasePoint = centerPt;
                ray.UnitDir = vec;
                var intersectPts = frame.Intersect(ray, Intersect.OnBothOperands).Where(x => !x.IsEqualTo(centerPt, new Tolerance(1, 1))).ToList();
                if (intersectPts.Count > 0)
                {
                    var cutPt = intersectPts.OrderBy(x => x.DistanceTo(centerPt)).First() + vec * 10;
                    var cutLine = new Line(centerPt, cutPt);
                    var checkPts = triangleOut.Intersect(cutLine, Intersect.OnBothOperands).Where(x => !x.IsEqualTo(centerPt, new Tolerance(1, 1))).ToList();
                    if (checkPts.Count <= 0)
                    {
                        cutLines.Add(cutLine);
                    }
                }
            }
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in cutLines)
                {
                    item.ColorIndex = 2;
                    //db.ModelSpace.Add(item);
                }
            }
            return cutLines;
        }

        /// <summary>
        /// 计算柱方向
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private static List<Vector3d> GetColumnVector(Polyline column, Point3d centerPt, Polyline triangleOut)
        {
            var lines = column.GetLinesByPolyline(500).ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatial = new ThCADCoreNTSSpatialIndex(lines);
            var objs = thCADCoreNTSSpatial.SelectCrossingPolygon(triangleOut);
            var vectors = new List<Vector3d>();
            foreach (Line line in lines)
            {
                if (!objs.Contains(line) && line.Length > 50)
                {
                    var dir = (line.GetClosestPointTo(centerPt, false) - centerPt).GetNormal();
                    vectors.Add(dir);
                }
            }

            return vectors;
        }

        /// <summary>
        /// 调整ucs边界
        /// </summary>
        /// <param name="ucsPolys"></param>
        public static Dictionary<Polyline, Vector3d> AdjustUCSPolys(Dictionary<Polyline, Vector3d> ucsPolys, Polyline frame)
        {
            if (ucsPolys.Count < 1)
            {
                return ucsPolys;
            }
            var checkUcsPoly = new Dictionary<Polyline, Vector3d>(ucsPolys);
            var poly = checkUcsPoly.First();
            checkUcsPoly.Remove(poly.Key);
            var cutLines = new List<List<Line>>();
            while (checkUcsPoly.Count > 0)
            {
                var intersectPolys = checkUcsPoly.Where(x => x.Key.Intersects(poly.Key)).ToList();
                foreach (var otherPoly in intersectPolys)
                {
                    var adjustPolys = CalOverlapLineOnPoly(poly.Key, otherPoly.Key);
                    cutLines.Add(adjustPolys);
                }
                poly = checkUcsPoly.First();
                checkUcsPoly.Remove(poly.Key);
            }
            var allLines = frame.GetLinesByPolyline(500);
            allLines.AddRange(cutLines.SelectMany(x => x));
            allLines = allLines.Select(x => x.ExtendLine(5)).ToList();
            var cutPolys = allLines.ToCollection().PolygonsEx();
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in allLines)
                {
                    db.ModelSpace.Add(item);
                }
            }
           
            return CheckUcsPolys(ucsPolys, cutPolys.Cast<Polyline>().ToList());
        }

        /// <summary>
        /// 计算两个polyline的overlap的部分为直线
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="otherPoly"></param>
        /// <returns></returns>
        private static List<Line> CalOverlapLineOnPoly(Polyline poly, Polyline otherPoly)
        {
            List<List<Point3d>> overlapPts = new List<List<Point3d>>();
            var lstPt = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint3dAt(i);
                if (otherPoly.Distance(pt) < 0.1)
                {
                    lstPt.Add(pt);
                }
                else
                {
                    if (lstPt.Count > 2)
                    {
                        overlapPts.Add(lstPt);
                    }
                    lstPt = new List<Point3d>();
                }
            }

            var resLines = new List<Line>();
            foreach (var pts in overlapPts)
            {
                var pt1 = pts.First();
                var pt2 = pts.Last();
                resLines.Add(new Line(pt1, pt2));
            }

            return resLines;
        }

        /// <summary>
        /// 找出需要的ucs区域
        /// </summary>
        /// <param name="ucsPolys"></param>
        /// <param name="cutPolys"></param>
        /// <returns></returns>
        private static Dictionary<Polyline, Vector3d> CheckUcsPolys(Dictionary<Polyline, Vector3d> ucsPolys, List<Polyline> cutPolys)
        {
            var resPolys = new Dictionary<Polyline, Vector3d>();
            foreach (var poly in cutPolys)
            {
                var polyCol = new DBObjectCollection() { poly };
                var hasPoly = ucsPolys.Where(x => x.Key.Intersection(polyCol).OfType<Polyline>().Sum(y => y.Area) / poly.Area > 0.5).FirstOrDefault(); //占比大于7成则算是需要的框线
                if (!default(KeyValuePair<Polyline, Vector3d>).Equals( hasPoly))
                {
                    resPolys.Add(poly, hasPoly.Value);
                }
            }

            return resPolys;
        }

        ///// <summary>
        ///// 调整ucs边界
        ///// </summary>
        ///// <param name="ucsPolys"></param>
        //public static Dictionary<Polyline, Vector3d> AdjustUCSPolys(Dictionary<Polyline, Vector3d> ucsPolys)
        //{
        //    var resPolys = new Dictionary<Polyline, Vector3d>();
        //    if (ucsPolys.Count < 1)
        //    {
        //        return resPolys;
        //    }
        //    var poly = ucsPolys.First();
        //    ucsPolys.Remove(poly.Key);
        //    while (ucsPolys.Count > 0)
        //    {
        //        var intersectPolys = ucsPolys.Where(x => x.Key.Intersects(poly.Key)).ToList();
        //        foreach (var otherPoly in intersectPolys)
        //        {
        //            var adjustPolys = AdjusrOverlapLineOnPoly(poly.Key, otherPoly.Key);
        //            poly = new KeyValuePair<Polyline, Vector3d>(adjustPolys[0], poly.Value);
        //            ucsPolys.Remove(otherPoly.Key);
        //            ucsPolys.Add(adjustPolys[1], otherPoly.Value);
        //        }
        //        resPolys.Add(poly.Key, poly.Value);
        //        poly = ucsPolys.First();
        //        ucsPolys.Remove(poly.Key);
        //    }
        //    resPolys.Add(poly.Key, poly.Value);

        //    return resPolys;
        //}

        /// <summary>
        /// 计算两个polyline的overlap的部分为直线
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="otherPoly"></param>
        /// <returns></returns>
        private static List<Polyline> AdjusrOverlapLineOnPoly(Polyline poly, Polyline otherPoly)
        {
            List<List<Point3d>> overlapPts = new List<List<Point3d>>();
            var lstPt = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint3dAt(i);
                if (otherPoly.Distance(pt) < 0.1)
                {
                    lstPt.Add(pt);
                }
                else
                {
                    if (lstPt.Count > 2)
                    {
                        overlapPts.Add(lstPt);
                    }
                    lstPt = new List<Point3d>();
                }
            }

            var resPolys = new List<Polyline>();
            resPolys.Add(AdjustOverlapPolylineToLine(poly, overlapPts));
            resPolys.Add(AdjustOverlapPolylineToLine(otherPoly, overlapPts));

            return resPolys;
        }

        /// <summary>
        /// 将交界的边转从polyline直连成line
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="overlapPts"></param>
        /// <returns></returns>
        private static Polyline AdjustOverlapPolylineToLine(Polyline poly, List<List<Point3d>> overlapPts) 
        {
            Polyline resPoly = new Polyline();
            int index = 0;
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint3dAt(i);
                resPoly.AddVertexAt(index, pt.ToPoint2d(), 0, 0, 0);
                index++;
                var overPts = overlapPts.Where(x => x.Any(y => y.IsEqualTo(pt, new Tolerance(0.1, 0.1)))).FirstOrDefault();
                if (overPts != null)
                {
                    if (overPts.Last().IsEqualTo(pt, new Tolerance(0.1, 0.1))) overPts.Reverse();
                    resPoly.AddVertexAt(index, overPts.First().ToPoint2d(), 0, 0, 0);
                    resPoly.AddVertexAt(index + 1, overPts.First().ToPoint2d(), 0, 0, 0);
                    i = i + overPts.Count;

                    index += 2;
                }
            }

            return resPoly;
        }
    }
}
