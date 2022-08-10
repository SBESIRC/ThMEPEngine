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
using ThCADExtension;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimExtensionService
    {
        public static List<ThSprinklerDimension> GenerateRealDimension(List<ThSprinklerNetGroup> transNetList, List<MPolygon> rooms, List<Polyline> walls, List<Polyline> axisCurves, string printTag, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            ThCADCoreNTSSpatialIndex wallsSI = GetReferanceSpatialIndex(walls);
            ThCADCoreNTSSpatialIndex axisCurvesSI = GetReferanceSpatialIndex(axisCurves);

            for (int i = 0; i < transNetList.Count; i++)
            {
                ThSprinklerNetGroup transNet = transNetList[i];
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                List<Line> dimLines = new List<Line>();
                foreach(ThSprinklerGraph graph in transNet.PtsGraph)
                {
                    dimLines.AddRange(graph.Print(pts));
                }
                ThCADCoreNTSSpatialIndex dimLinesSI = GetReferanceSpatialIndex(dimLines);

                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.XDimension, wallsSI, axisCurvesSI, dimLinesSI, rooms[i], true, step));
                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.YDimension, wallsSI, axisCurvesSI, dimLinesSI, rooms[i], false, step));
            }

            // test
            foreach(ThSprinklerDimension dim in realDim)
            {
                List<Line> lineList = new List<Line>();
                List<Point3d> pts = dim.DimPts;
                for(int i = 0; i < pts.Count-1; i++)
                {
                    lineList.Add(new Line(pts[i], pts[i + 1]));
                }

                if (dim.Distance < 0.5) // 0
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 2, 35);
                }
                else if (dim.Distance < 1.5) // 1
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 3, 35);
                }
                else if (dim.Distance < 2.5) // 2
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 4, 35);
                }

            }
            

            return realDim;
        }

        private static List<ThSprinklerDimension> GenerateRealDimension(List<Point3d> pts, Matrix3d transformer, List<List<int>> dims, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, ThCADCoreNTSSpatialIndex dimensionedLines, MPolygon room, bool isXAxis, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            foreach (List<int> dim in dims)
            {
                if (dim == null || dim.Count == 0)
                    continue;

                dim.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                Vector3d dir = GetDirrection(transformer, isXAxis);
                Point3d pt = GetDimPtCloseToReference(pts, dim, dir, roomWallColumn, room, step);

                // test
                int tag = 0;

                if (pt.Equals(pts[dim[0]]))
                {
                    tag = 1;
                    pt = GetDimPtCloseToReference(pts, dim, dir, axisCurves, room, step);
                }

                if (pt.Equals(pts[dim[0]]) && dim.Count < 3)
                {
                    tag = 2;
                    pt = GetDimPtCloseToReference(pts, dim, dir, dimensionedLines, room, step);
                }

                if (!pt.Equals(pts[dim[0]]))
                {
                    List<Point3d> dimPts = GetPoints(pts, dim);
                    dimPts.Add(pt);
                    dimPts.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(y, isXAxis)));

                    ///////////////////////
                    realDim.Add(new ThSprinklerDimension(dimPts, new Vector3d(), tag));
                }

            }

            return realDim;
        }

        private static DBObjectCollection GetReferanceSpatialIndex(List<Polyline> reference, MPolygon room)
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
            DBObjectCollection dbSelect = linesSI.SelectWindowPolygon(room);
            return dbSelect;
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

        private static Point3d GetDimPtCloseToReference(List<Point3d> pts, List<int> dim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, MPolygon room, double step)
        {
            Tuple<int, Point3d> ptMin = GetDimPtCloseToReference(pts[dim[0]], -dir, reference, room, step);
            Tuple<int, Point3d> ptMax = GetDimPtCloseToReference(pts[dim[dim.Count - 1]], dir, reference, room, step);
            if (ptMin.Item1==1 && ptMax.Item1==1)
            {
                if (ptMin.Item2.DistanceTo(pts[dim[0]]) < ptMax.Item2.DistanceTo(pts[dim[dim.Count - 1]]))
                    return ptMin.Item2;
                else
                    return ptMax.Item2;
            }
            else if (ptMin.Item1==1)
            {
                return ptMin.Item2;
            }
            else if (ptMax.Item1==1)
            {
                return ptMax.Item2;
            }
            else
            {
                if(ptMin.Item1 == 2 && ptMax.Item1 == 2)
                {
                    if (ptMin.Item2.DistanceTo(pts[dim[0]]) < ptMax.Item2.DistanceTo(pts[dim[dim.Count - 1]]))
                        return ptMin.Item2;
                    else
                        return ptMax.Item2;
                }
                else if (ptMin.Item1 == 2)
                {
                    return ptMin.Item2;
                }
                else if(ptMax.Item1 == 2)
                {
                    return ptMax.Item2;
                }
                else
                {
                    for(int i = 0; i < dim.Count-1; i++)
                    {
                        Tuple<int, Point3d> t = GetDimPtCloseToReference(pts[dim[i]], dir, reference, room, step);
                        if (t.Item1 != 3)
                            return t.Item2;
                    }
                }

            }

            return pts[dim[0]];
        }
        private static Tuple<int, Point3d> GetDimPtCloseToReference(Point3d pt, Vector3d dir, ThCADCoreNTSSpatialIndex reference, MPolygon room, double step, double tolerance=50.0)
        {
            // 选出与box相交及其内部的线
            Polyline box = GenerateBox(pt, dir, step, step);
            List<Line> selectedLines = new List<Line>();
            DBObjectCollection dbSelect = reference.SelectCrossingPolygon(box);
            foreach (DBObject dbo in dbSelect)
            {
                selectedLines.Add((Line)dbo);
            }

            // 过滤选出与标注方向大致垂直的 且 在当前房间框线内的 参考线,
            List<Line> filteredLines = new List<Line>();
            foreach (Line line in selectedLines)
            {
                Vector3d t = (line.StartPoint - line.EndPoint).GetNormal();
                double angle = t.GetAngleTo(dir);

                if (Math.Abs(angle - Math.PI / 2) < Math.PI / 180 || Math.Abs(angle - Math.PI * 3 / 2) < Math.PI / 180)
                {

                    if (ThSprinklerNetGroupListService.IsContained(room, line.StartPoint) && ThSprinklerNetGroupListService.IsContained(room, line.EndPoint) && line.Length > 45)
                    {
                        filteredLines.Add(line);
                    }

                }

            }

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


                    if(ThSprinklerNetGroupListService.IsContained(room, pt1) && ThSprinklerNetGroupListService.IsContained(room, pt2))
                    {
                        if (pt2.DistanceTo(pt) > tolerance)
                        {
                            if (pt1.Equals(pt3))
                                pts1.Add(pt2);

                            else
                                pts2.Add(pt2);
                        }
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
                    return new Tuple<int, Point3d>(1, p);
            }

            // 其次选择与参照物不相交的点
            foreach (Point3d p in pts2)
            {
                if ((p - pt).GetNormal().DotProduct(dir) > 0)
                    return new Tuple<int, Point3d>(2, p);
            }

            // 均无则返回原点
            return new Tuple<int, Point3d>(3, pt);
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
