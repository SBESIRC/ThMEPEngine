using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimExtensionService
    {
        public static List<ThSprinklerDimension> GenerateRealDimension(List<ThSprinklerNetGroup> transNetList, List<Polyline> walls, List<Line> axisCurves, string printTag, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();
            foreach(ThSprinklerNetGroup transNet in transNetList)
            {
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                List<Line> dimLines = new List<Line>();
                foreach(ThSprinklerGraph graph in transNet.PtsGraph)
                {
                    dimLines.AddRange(graph.Print(pts));
                }

                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.XDimension, walls, dimLines, axisCurves, true, step));
                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.YDimension, walls, dimLines, axisCurves, false, step));
            }

            // test
            List<Line> lineList = new List<Line>();
            foreach(ThSprinklerDimension dim in realDim)
            {
                List<Point3d> pts = dim.DimPts;
                for(int i = 0; i < pts.Count-1; i++)
                {
                    lineList.Add(new Line(pts[i], pts[i + 1]));
                }
            }
            DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 3, 35);

            return realDim;
        }

        private static List<ThSprinklerDimension> GenerateRealDimension(List<Point3d> pts, Matrix3d transformer, List<List<int>> dims, List<Polyline> roomWallColumn, List<Line> lines, List<Line> axisCurves, bool isXAxis, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();
            foreach (List<int> dim in dims)
            {
                if (dim == null || dim.Count == 0)
                    continue;

                dim.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                Vector3d dir = GetDirrection(transformer, isXAxis);
                Point3d pt = GetDimPtCloseToReference(pts, dim, dir, GetReferanceSpatialIndex(roomWallColumn), step);

                if (pt.Equals(pts[dim[0]]))
                {
                    pt = GetDimPtCloseToReference(pts, dim, dir, GetReferanceSpatialIndex(axisCurves), step);
                }

                if (pt.Equals(pts[dim[0]]))
                {
                    pt = GetDimPtCloseToReference(pts, dim, dir, GetReferanceSpatialIndex(lines), step);
                }

                if (!pt.Equals(pts[dim[0]]))
                {
                    List<Point3d> dimPts = GetPoints(pts, dim);
                    dimPts.Add(pt);
                    dimPts.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(y, isXAxis)));

                    ///////////////////////
                    realDim.Add(new ThSprinklerDimension(dimPts, new Vector3d(), 0));
                }

            }

            return realDim;
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Polyline> reference)
        {
            // 把参考物拆解为线,做成空间索引
            DBObjectCollection referenceLines = new DBObjectCollection();
            foreach (Polyline r in reference)
            {
                for (int i = 0; i < r.NumberOfVertices; i++)
                {
                    referenceLines.Add(new Line(r.GetPoint3dAt(i), r.GetPoint3dAt((i + 1) % r.NumberOfVertices)));
                }

            }
            ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(referenceLines);
            return linesSI;
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Line> reference)
        {
            // 把参考物拆解为线,做成空间索引
            DBObjectCollection referenceLines = new DBObjectCollection();
            foreach (Line r in reference)
            {
                    referenceLines.Add(r);
            }
            ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(referenceLines);
            return linesSI;
        }

        private static Vector3d GetDirrection(Matrix3d transformer, bool isXAxis)
        {
            Point3d startPoint = new Point3d(0, 0, 0);
            Point3d endPoint = new Point3d();

            if(isXAxis)
            {
                endPoint = new Point3d(1, 0, 0);
            }
            else
            {
                endPoint = new Point3d(0, 1, 0);
            }


            List<Point3d> pts = new List<Point3d> { startPoint, endPoint};
            pts = ThChangeCoordinateService.MakeTransformation(pts, transformer.Inverse());

            Vector3d dir = (pts[1] - pts[0]).GetNormal();
            return dir;
        }

        private static Point3d GetDimPtCloseToReference(List<Point3d> pts, List<int> dim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, double step)
        {
            Tuple<bool, Point3d> ptMin = GetDimPtCloseToReference(pts[dim[0]], -dir, reference, step);
            Tuple<bool, Point3d> ptMax = GetDimPtCloseToReference(pts[dim[dim.Count - 1]], dir, reference, step);
            if (ptMin.Item1 && ptMax.Item1)
            {
                if (ptMin.Item2.DistanceTo(pts[dim[0]]) < ptMax.Item2.DistanceTo(pts[dim[dim.Count - 1]]))
                    return ptMin.Item2;
                else
                    return ptMax.Item2;
            }
            else if (ptMin.Item1)
            {
                return ptMin.Item2;
            }
            else if (ptMax.Item1)
            {
                return ptMax.Item2;
            }
            else
            {
                if(!ptMin.Item2.Equals(pts[dim[0]]) && !ptMax.Item2.Equals(pts[dim[dim.Count - 1]]))
                {
                    if (ptMin.Item2.DistanceTo(pts[dim[0]]) < ptMax.Item2.DistanceTo(pts[dim[dim.Count - 1]]))
                        return ptMin.Item2;
                    else
                        return ptMax.Item2;
                }
                else if (!ptMin.Item2.Equals(pts[dim[0]]))
                {
                    return ptMin.Item2;
                }
                else if(!ptMax.Item2.Equals(pts[dim[dim.Count - 1]]))
                {
                    return ptMax.Item2;
                }
                else
                {
                    for(int i = 0; i < dim.Count-1; i++)
                    {
                        Tuple<bool, Point3d> t = GetDimPtCloseToReference(pts[dim[i]], dir, reference, step);
                        if (!t.Item2.Equals(pts[dim[i]]))
                            return t.Item2;
                    }
                }

            }

            return pts[dim[0]];
        }
        private static Tuple<bool, Point3d> GetDimPtCloseToReference(Point3d pt, Vector3d dir, ThCADCoreNTSSpatialIndex reference, double step, double tolerance=50.0)
        {
            // 选出与box相交及其内部的线
            Polyline box = GenerateBox(pt, dir, step, step);
            List<Line> selectedLines = new List<Line>();
            DBObjectCollection dbSelect = reference.SelectCrossingPolygon(box);
            foreach (DBObject dbo in dbSelect)
            {
                selectedLines.Add((Line)dbo);
            }

            // 过滤选出与标注方向大致垂直的参考线
            List<Line> filteredLines = new List<Line>();
            foreach (Line line in selectedLines)
            {
                Vector3d t = (line.StartPoint - line.EndPoint).GetNormal();
                double angle = t.GetAngleTo(dir);

                if (Math.Abs(angle - Math.PI / 2) < Math.PI / 180 || Math.Abs(angle - Math.PI * 3 / 2) < Math.PI / 180)
                {
                    filteredLines.Add(line);
                }

            }
            //DrawUtils.ShowGeometry(filteredLines, "SSS-#filteredLines", 2, 35);

            // 找出往这些参考线作的标注点
            List<Point3d> pts1 = new List<Point3d>();
            List<Point3d> pts2 = new List<Point3d>();
            if (filteredLines.Count > 0)
            {
                foreach(Line l in filteredLines)
                {
                    Point3d pt1 = l.GetClosestPointTo(pt, false);
                    Point3d pt3 = l.GetClosestPointTo(pt, true);

                    Line t = new Line(pt, pt + dir);
                    Point3d pt2 = t.GetClosestPointTo(pt1, true);

                    if (pt2.DistanceTo(pt) > tolerance)
                    {
                        if (pt1.Equals(pt3))
                            pts1.Add(pt2);

                        else
                            pts2.Add(pt2);
                    }
                   
                }
                
            }

            // 按照距离由近到远排序这些待选择的标注点
            pts1.Sort((x, y) => x.DistanceTo(pt).CompareTo(y.DistanceTo(pt)));
            pts2.Sort((x, y) => x.DistanceTo(pt).CompareTo(y.DistanceTo(pt)));

            // 优先选择与参照物相交的点
            foreach (Point3d p in pts1)
            {
                if ((p - pt).GetNormal().DotProduct(dir) > 0)
                    return new Tuple<bool, Point3d>(true, p);
            }

            // 其次选择与参照物不相交的点
            foreach (Point3d p in pts2)
            {
                if ((p - pt).GetNormal().DotProduct(dir) > 0)
                    return new Tuple<bool, Point3d>(false, p);
            }

            // 均无则返回原点
            return new Tuple<bool, Point3d>(false, pt);
        }

        private static Polyline GenerateBox(Point3d pt, Vector3d dir, double sTol=2000.0, double dTol=1500.0)
        {
            Polyline box  = new Polyline();
            Vector3d tDir = dir.RotateBy(Math.PI / 2, new Vector3d(0, 0, 1));

            Point3d a = pt - sTol * dir + dTol * tDir;
            Point3d b = pt + sTol * dir + dTol * tDir;
            Point3d c = pt + sTol * dir - dTol * tDir;
            Point3d d = pt - sTol * dir - dTol * tDir;

            box.AddVertexAt(0, a.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(1, b.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(2, c.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(3, d.ToPoint2D(), 0, 0, 0);

            box.Closed = true;

            //DrawUtils.ShowGeometry(box, "SSS-#BOX");

            return box;
        }

        private static bool IsConflicted()
        {
            return false;
        }

        private static List<Point3d> GetPoints(List<Point3d> pts, List<int> idxs)
        {
            List<Point3d> line = new List<Point3d>();

            foreach(int i in idxs)
            {
                line.Add(pts[i]);
            }

            return line;
        }



    }
}
