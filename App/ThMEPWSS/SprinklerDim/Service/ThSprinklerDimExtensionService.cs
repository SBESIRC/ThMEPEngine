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
using Dreambuild.AutoCAD;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimExtensionService
    {
        public static List<ThSprinklerDimension> GenerateRealDimension(List<ThSprinklerNetGroup> transNetList, List<MPolygon> rooms, List<Polyline> walls, List<Polyline> axisCurves, string printTag, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            for (int i = 0; i < transNetList.Count; i++)
            {
                ThSprinklerNetGroup transNet = transNetList[i];
                List<Point3d> pts = ThCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                ThCADCoreNTSSpatialIndex wallsSI = GetReferanceSpatialIndex(walls, rooms[i]);
                ThCADCoreNTSSpatialIndex axisCurvesSI = GetReferanceSpatialIndex(axisCurves, rooms[i]);
                List<Line> dimLines = new List<Line>();
                foreach (ThSprinklerGraph graph in transNet.PtsGraph)
                {
                    dimLines.AddRange(graph.Print(pts));
                }
                ThCADCoreNTSSpatialIndex dimLinesSI = GetReferanceSpatialIndex(dimLines, rooms[i]);


                List<List<int>> xCollineation = new List<List<int>>();
                List<List<int>> yCollineation = new List<List<int>>();
                foreach (List<List<int>> x in transNet.XCollineationGroup)
                {
                    xCollineation.AddRange(x);
                }
                foreach (List<List<int>> y in transNet.YCollineationGroup)
                {
                    yCollineation.AddRange(y);
                }

                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.XDimension, xCollineation, wallsSI, axisCurvesSI, dimLinesSI, true, step, printTag));
                realDim.AddRange(GenerateRealDimension(pts, transNet.Transformer, transNet.YDimension, yCollineation, wallsSI, axisCurvesSI, dimLinesSI, false, step, printTag));
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

        private static List<ThSprinklerDimension> GenerateRealDimension(List<Point3d> pts, Matrix3d transformer, List<List<int>> dims, List<List<int>> anotherCollineation, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, ThCADCoreNTSSpatialIndex dimensionedLines, bool isXAxis, double step, string printTag)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            foreach (List<int> dim in dims)
            {
                if (dim == null || dim.Count == 0)
                    continue;

                
                Vector3d dir = ThCoordinateService.GetDirrection(transformer.Inverse(), isXAxis);

                List<List<int>> allAvailableDims = new List<List<int>>();
                if(dim.Count == 1)
                {
                    if (Math.Abs(pts[dim[0]].X- 848821.6) < 10)
                    {
                        int a = 0;
                    }
                    List<int> singleDims = anotherCollineation.Where(x => x.Contains(dim[0])).ToList()[0];
                    foreach(int singleDim in singleDims)
                        allAvailableDims.Add(new List<int> { singleDim });
                }
                else
                {
                    dim.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                    allAvailableDims.Add(dim);
                }

                bool isFound = false;
                int tag = 0;
                Point3d pt = pts[dim[0]];
                List<int> tDim = dim;
                foreach (var d in allAvailableDims) // 房间框线、墙、柱
                {
                    pt = GetDimPtCloseToReference(pts, d, dir, roomWallColumn, step);
                    if (!pt.Equals(pts[d[0]]))
                    {
                        isFound = true;
                        tDim = d;
                        break;
                    }
                }

                if (!isFound) // 轴网
                {
                    foreach (var d in allAvailableDims)
                    {
                        pt = GetDimPtCloseToReference(pts, d, dir, axisCurves, step);
                        if (!pt.Equals(pts[d[0]]))
                        {
                            isFound = true;
                            tDim = d;
                            tag = 1;
                            break;
                        }

                    }

                }

                if (!isFound) // 标注点小于3的找之前的格网，还不能判断这个格网是否标注
                {
                    foreach (var d in allAvailableDims)
                    {
                        if(d.Count < 3)
                        {
                            pt = GetDimPtCloseToReference(pts, d, dir, roomWallColumn, step);
                            if (!pt.Equals(pts[d[0]]))
                            {
                                isFound = true;
                                tDim = d;
                                tag = 2;
                                break;
                            }
                        }

                    }

                }

                if (isFound)
                {
                    List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, tDim);
                    dimPts.Add(pt);
                    dimPts.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                    ///////////////////////
                    realDim.Add(new ThSprinklerDimension(dimPts, new Vector3d(), tag));
                    DrawUtils.ShowGeometry(pt, string.Format("SSS-{0}-6Dim", printTag), 11, 50, 100);
                }

                //foreach (var d in allAvailableDims)
                //{
                //    pt = GetDimPtCloseToReference(pts, d, dir, roomWallColumn, step);

                //    // test
                //    int tag = 0;

                //    if (pt.Equals(pts[d[0]]))
                //    {
                //        tag = 1;
                //        pt = GetDimPtCloseToReference(pts, d, dir, axisCurves, step);
                //    }

                //    if (pt.Equals(pts[d[0]]) && d.Count < 3)
                //    {
                //        tag = 2;
                //        pt = GetDimPtCloseToReference(pts, d, dir, dimensionedLines, step);
                //    }

                //    if (!pt.Equals(pts[d[0]]))
                //    {
                //        List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, d);
                //        dimPts.Add(pt);
                //        dimPts.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                //        ///////////////////////
                //        realDim.Add(new ThSprinklerDimension(dimPts, new Vector3d(), tag));
                //        DrawUtils.ShowGeometry(pt, string.Format("SSS-{0}-6Dim", printTag), 11, 50, 100);
                //    }
                //}
                

            }

            return realDim;
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Line> dimLines, MPolygon room)
        {
            List<Line> allLines = new List<Line>();
            allLines.AddRange(dimLines);
            allLines.AddRange(ThDataTransformService.Change(room));

            return ThDataTransformService.GenerateSpatialIndex(allLines);
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Polyline> reference, MPolygon room)
        {
            // 把房间框出的参考物拆解为线,做成空间索引
            List<Polyline> referenceInShell = new List<Polyline>();
            referenceInShell.AddRange(GetReference(reference, room.Shell(), false));

            List<Polyline> referenceInRoom = referenceInShell;
            foreach (Polyline hole in room.Holes())
            {
                referenceInRoom = GetReference(referenceInRoom, hole, true);
            }

            referenceInRoom.Add(room.Shell());
            foreach (Polyline hole in room.Holes())
            {
                referenceInRoom.Add(hole);
            }

            return ThDataTransformService.GenerateSpatialIndex(referenceInRoom);
        }

        private static List<Polyline> GetReference(List<Polyline> reference, Polyline room, bool inverted)
        {
            // 把房间框出的参考物拆解为线,做成空间索引
            List<Polyline> polylines = new List<Polyline>();

            foreach (Polyline r in reference)
            {
                DBObjectCollection dboc = room.Trim(r, inverted);
                polylines.AddRange(ThDataTransformService.GetPolylines(dboc));
            }

            return polylines;
        }

        
        

        private static Point3d GetDimPtCloseToReference(List<Point3d> pts, List<int> dim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, double step)
        {
            Tuple<int, Point3d> ptMin = GetDimPtCloseToReference(pts[dim[0]], -dir, reference, step);
            Tuple<int, Point3d> ptMax = GetDimPtCloseToReference(pts[dim[dim.Count - 1]], dir, reference, step);
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
                        Tuple<int, Point3d> t = GetDimPtCloseToReference(pts[dim[i]], dir, reference, step);
                        if (t.Item1 != 3)
                            return t.Item2;
                    }
                }

            }

            return pts[dim[0]];
        }

        private static Tuple<int, Point3d> GetDimPtCloseToReference(Point3d pt, Vector3d dir, ThCADCoreNTSSpatialIndex reference, double step, double tolerance=50.0)
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

                if ((Math.Abs(angle - Math.PI / 2) < Math.PI / 180 || Math.Abs(angle - Math.PI * 3 / 2) < Math.PI / 180) && line.Length > 10)
                {
                    filteredLines.Add(line);
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

                    if (pt2.DistanceTo(pt) > tolerance && !IsConflicted(pt, pt2, reference) && !IsConflicted(pt1, pt2, reference))
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

        private static bool IsConflicted(Point3d pt1, Point3d pt2, ThCADCoreNTSSpatialIndex reference)
        {
            return ThSprinklerDimConflictService.IsDimCrossReference(new Line(pt1, pt2), reference, 10);
        }


        private static bool IsConflicted()
        {
            return false;
        }

        



    }
}
