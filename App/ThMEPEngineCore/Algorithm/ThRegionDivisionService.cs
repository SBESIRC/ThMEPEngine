using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Algorithm
{
    public class ThRegionDivisionService
    {
        public double tol = 0.001;

        public List<Polyline> DivisionRegion(Polyline room)
        {
            room = ThMEPFrameService.Normalize(room);
            List<Line> pLines = new List<Line>();
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < room.NumberOfVertices; i++)
            {
                var current = room.GetPoint3dAt(i);
                var next = room.GetPoint3dAt((i + 1) % room.NumberOfVertices);
                int j = i - 1;
                if (j < 0)
                {
                    j = room.NumberOfVertices - 1;
                }
                var pre = room.GetPoint3dAt(j);
                pLines.Add(new Line(current, next));

                int res = IsConvexPoint(room, current, next, pre);
                if (res == 1)   //凹点要分割
                {
                    points.Add(current);
                }
            }
            
            var bLines = GetDivisionLines(pLines, points);
            var diviPoly = GetMinPolygon(bLines).Where(x => x.Area > 0).ToList();
            
            return diviPoly;
        }

        /// <summary>
        /// 合并polygon
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public List<Polyline> MergePolygon(List<Polyline> polygon)
        {
            List<Polyline> allPolygon = new List<Polyline>(polygon);
            List<Polyline> resPolys = new List<Polyline>();
            while (allPolygon.Count > 0)
            {
                bool needCheck = false;
                List<Polyline> checkPolys = new List<Polyline>(allPolygon);
                foreach (var poly in checkPolys)
                {
                    var interPolys = GetIntersectPoly(poly, allPolygon);
                    var samePolys = GetSameDirectionPolys(poly, interPolys);
                    if (samePolys.Count > 0)
                    {
                        samePolys.Add(poly);
                        allPolygon = allPolygon.Except(samePolys).ToList();
                        samePolys = samePolys.Select(x => x.Buffer(1)[0] as Polyline).ToList();
                        var unionPolys = samePolys.ToCollection().UnionPolygons().Cast<Polyline>().ToList();
                        unionPolys = unionPolys.Select(x => x.Buffer(-1)[0] as Polyline).ToList();
                        allPolygon.AddRange(unionPolys);
                        allPolygon.Remove(poly);
                        needCheck = true;
                        break;
                    }
                }

                if (!needCheck)
                {
                    break;
                }
            }

            return allPolygon;
        }

        /// <summary>
        /// 找到相交polyline
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="polylines"></param>
        private List<Polyline> GetIntersectPoly(Polyline poly, List<Polyline> polylines)
        {
            List<Polyline> checkPolys = new List<Polyline>(polylines);
            checkPolys.Remove(poly);
            var bufferPoly = poly.Buffer(1)[0] as Polyline;
            List<Polyline> intersectPolys = new List<Polyline>();
            foreach (var pl in checkPolys)
            {
                if (bufferPoly.Intersects(pl))
                {
                    intersectPolys.Add(pl);
                }
            }

            return intersectPolys;
        }

        private List<Polyline> GetSameDirectionPolys(Polyline poly, List<Polyline> polylines)
        {
            List<Polyline> resPolys = new List<Polyline>();
            var polyOBB = poly.CalObb();
            var polyDir = GetPolylineDir(polyOBB);
            var otherDir = Vector3d.ZAxis.CrossProduct(polyDir);
            foreach (var pl in polylines)
            {
                var plOBB = pl.CalObb();
                using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
                {
                    //db.ModelSpace.Add(plOBB);
                }
                var plDir = GetPolylineDir(plOBB);
                if (plDir.IsParallelTo(polyDir, new Tolerance(tol, tol)) || plDir.IsParallelTo(otherDir, new Tolerance(tol, tol)))
                {
                    resPolys.Add(pl);
                }
            }

            return resPolys;
        }

        /// <summary>
        /// 计算polyline最长边方向
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Vector3d GetPolylineDir(Polyline polyline)
        {
            List<Line> allLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            var line = allLines.OrderByDescending(x => x.Length).First();
            var polyDir = (line.EndPoint - line.StartPoint).GetNormal();

            return polyDir;
        }

        /// <summary>
        /// 计算区域分割线
        /// </summary>
        /// <param name="allLines"></param>
        /// <param name="allPoints"></param>
        /// <returns></returns>
        private List<Line> GetDivisionLines(List<Line> allLines, List<Point3d> allPoints)
        {
            List<Line> divLines = new List<Line>();
            foreach (var pt in allPoints)
            {
                var tempLines = allLines.Where(x => x.StartPoint.IsEqualTo(pt, Tolerance.Global) || x.EndPoint.IsEqualTo(pt, Tolerance.Global)).ToList();
                var otherLines = allLines.Except(tempLines).ToList();
                Point3d bPoint = pt;
                Line bLine = null;
                foreach (var line in tempLines)
                {
                    Vector3d dir = line.Delta.GetNormal();
                    if (!line.EndPoint.IsEqualTo(pt, Tolerance.Global))
                    {
                        dir = -dir;
                    }

                    foreach (var oLine in otherLines)
                    {
                        Ray ray = new Ray();
                        ray.BasePoint = pt;
                        ray.UnitDir = dir;
                        Point3dCollection points = new Point3dCollection();
                        oLine.IntersectWith(ray, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d tempP in points)
                        {
                            if (bPoint == pt)
                            {
                                bPoint = tempP;
                                bLine = oLine;
                                continue;
                            }

                            if (tempP.DistanceTo(pt) < pt.DistanceTo(bPoint))
                            {
                                bPoint = tempP;
                                bLine = oLine;
                            }
                        }
                    }
                }
                if (bPoint != pt)
                {
                    divLines.Add(new Line(bPoint, pt));
                }
            }

            while (divLines.Count > 0)
            {
                var firLine = divLines[0];
                var interLines = divLines.Where(x => x.IsIntersects(firLine)).ToList();
                interLines.Add(firLine);
                interLines = interLines.OrderBy(x => x.Length).ToList();
                allLines.Add(interLines[0]);
                divLines = divLines.Except(interLines).ToList();
            }

            return allLines;
        }

        /// <summary>
        /// 获得所有最小轮廓线
        /// </summary>
        /// <param name="bLines"></param>
        /// <returns></returns>
        private List<Polyline> GetMinPolygon(List<Line> bLines)
        {
            DBObjectCollection dBObject = new DBObjectCollection();
            foreach (var bLine in bLines)
            {
                dBObject.Add(bLine);
            }
            var objCollection = dBObject.Polygons();
            List<Polyline> polygons = objCollection.Cast<Polyline>().ToList();
            polygons = polygons.Select(x => x.DPSimplify(0.1)).ToList();

            return polygons;
        }

        /// <summary>
        /// 判断当前点是凸点还是凹点(-1，凸点；1，凹点；0，点在线上，不是拐点)
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="pt"></param>
        /// <param name="nextP"></param>
        /// <param name="preP"></param>
        /// <returns></returns>
        private int IsConvexPoint(Polyline poly, Point3d pt, Point3d nextP, Point3d preP)
        {
            Vector3d nextV = (nextP - pt).GetNormal();
            Vector3d preV = (pt - preP).GetNormal();
            Point3d movePt = pt - nextV * 1 + preV * 1;

            if (poly.OnBoundary(movePt))
            {
                return 0;
            }

            if (!poly.Contains(movePt))
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}
