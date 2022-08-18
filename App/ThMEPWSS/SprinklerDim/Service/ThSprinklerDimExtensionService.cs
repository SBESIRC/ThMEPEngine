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
        public static List<ThSprinklerDimension> GenerateReferenceDimensionPoint(List<ThSprinklerNetGroup> transNetList, List<MPolygon> rooms, List<Polyline> mixColumnWall, List<Polyline> axisCurves, List<Polyline> texts, List<Polyline> pipes, string printTag, double step)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            for (int i = 0; i < transNetList.Count; i++)
            {
                ThSprinklerNetGroup transNet = transNetList[i];
                List<Point3d> pts = ThCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                ThCADCoreNTSSpatialIndex columnWallsSI = GetReferanceSpatialIndex(mixColumnWall, rooms[i]);
                ThCADCoreNTSSpatialIndex axisCurvesSI = GetReferanceSpatialIndex(axisCurves, rooms[i]);
                List<Line> dimLines = new List<Line>();
                foreach (ThSprinklerGraph graph in transNet.PtsGraph)
                {
                    dimLines.AddRange(graph.Print(pts));
                }
                ThCADCoreNTSSpatialIndex dimLinesSI = GetReferanceSpatialIndex(dimLines, rooms[i]);

                List<List<int>> xCollineation = ThDataTransformService.Change(transNet.XCollineationGroup);
                List<List<int>> yCollineation = ThDataTransformService.Change(transNet.YCollineationGroup);

                ThCADCoreNTSSpatialIndex textsInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(texts, rooms[i]));
                ThCADCoreNTSSpatialIndex mixColumnWallInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(mixColumnWall, rooms[i]));
                ThCADCoreNTSSpatialIndex pipesInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(pipes, rooms[i]));

                DBObjectCollection dimedArea = new DBObjectCollection();



                realDim.AddRange(GenerateReferenceDimensionPoint(pts, transNet.Transformer, transNet.XDimension, xCollineation, columnWallsSI, axisCurvesSI, dimLinesSI, rooms[i], textsInRoom, mixColumnWallInRoom, ref dimedArea, pipesInRoom, true, step, printTag));
                realDim.AddRange(GenerateReferenceDimensionPoint(pts, transNet.Transformer, transNet.YDimension, yCollineation, columnWallsSI, axisCurvesSI, dimLinesSI, rooms[i], textsInRoom, mixColumnWallInRoom, ref dimedArea, pipesInRoom, false, step, printTag));
            }
            
            return realDim;
        }

        private static List<ThSprinklerDimension> GenerateReferenceDimensionPoint(List<Point3d> pts, Matrix3d transformer, List<List<ThSprinklerDimGroup>> dims, List<List<int>> anotherCollineation, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, ThCADCoreNTSSpatialIndex dimensionedLines, MPolygon room, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, ref DBObjectCollection dimedArea , ThCADCoreNTSSpatialIndex pipes, bool isXAxis, double step, string printTag)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();
            dims.Sort((x, y) =>y.Count - x.Count);

            List<List<int>> tdims = new List<List<int>>();
            foreach(List<ThSprinklerDimGroup> p in dims)
            {
                List<int> td = new List<int>();
                p.ForEach(q => td.Add(q.pt));
                tdims.Add(td);
            }

            foreach (List<int> dim in tdims)
            {
                if (dim == null || dim.Count == 0)
                    continue;

                
                Vector3d dir = ThCoordinateService.GetDirrection(transformer.Inverse(), isXAxis);

                List<List<int>> allAvailableDims = new List<List<int>>();
                if(dim.Count == 1)
                {
                    //if (Math.Abs(pts[dim[0]].X - 1024133.9725) < 10)
                    //{
                    //    int a = 0;
                    //}
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
                    pt = GetDimPtCloseToReference(pts, d, dir, roomWallColumn, roomWallColumn, step);
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
                        pt = GetDimPtCloseToReference(pts, d, dir, axisCurves, roomWallColumn, step);
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
                            pt = GetDimPtCloseToReference(pts, d, dir, dimensionedLines, roomWallColumn, step);
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

                    // test
                    if (Math.Abs(dimPts[0].X - 849544.0) < 10)
                    {
                        int k = 0;
                    }

                    dimPts.Add(pt);
                    dimPts.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                    double distance = GetAdjustedDistance(dimPts, room, texts, mixColumnWall, ref dimedArea, pipes, printTag);
                    if(distance != 0)
                    {
                        if (distance > 0)
                        {
                            Vector3d dirrection = (dimPts[1] - dimPts[0]).GetNormal().RotateBy(Math.PI / 2, new Vector3d(0, 0, 1));
                            realDim.Add(new ThSprinklerDimension(dimPts, dirrection, distance));

                            DrawUtils.ShowGeometry(dimPts[0], string.Format("SSS-{0}-6Dim", printTag), 3, 50, 100);
                        }
                        else
                        {
                            Vector3d dirrection = (dimPts[1] - dimPts[0]).GetNormal().RotateBy(-Math.PI / 2, new Vector3d(0, 0, 1));
                            realDim.Add(new ThSprinklerDimension(dimPts, dirrection, -distance));

                            DrawUtils.ShowGeometry(dimPts[0], string.Format("SSS-{0}-6Dim", printTag), 2, 50, 100);
                        }


                        /// test
                        DrawUtils.ShowGeometry(pt, string.Format("SSS-{0}-6Dim", printTag), 11, 50, 100);

                        List<Line> lineList = new List<Line>();
                        for (int i = 0; i < dimPts.Count - 1; i++)
                        {
                            lineList.Add(new Line(dimPts[i], dimPts[i + 1]));
                        }

                        if (tag < 0.5) // 0 房间
                        {
                            DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 0, 35);
                        }
                        else if (tag < 1.5) // 1 轴网
                        {
                            DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 3, 35);
                        }
                        else if (tag < 2.5) // 2 网格
                        {
                            DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 4, 35);
                        }
                        /////////////////////////
                        
                    }



                }
                else
                {
                    List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, dim);
                    List<Line> lineList = new List<Line>();
                    for (int i = 0; i < dimPts.Count - 1; i++)
                    {
                        lineList.Add(new Line(dimPts[i], dimPts[i + 1]));

                        DrawUtils.ShowGeometry(dimPts[i], string.Format("SSS-{0}-7UnDim", printTag), 1, 50, 500);
                    }
                    DrawUtils.ShowGeometry(dimPts[dimPts.Count - 1], string.Format("SSS-{0}-7UnDim", printTag), 1, 50, 500);
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-7UnDim", printTag), 1, 35);
                }
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
            List<Polyline> referenceInRoom = ThGeometryOperationService.Trim(reference, room);

            referenceInRoom.Add(room.Shell());
            foreach (Polyline hole in room.Holes())
            {
                referenceInRoom.Add(hole);
            }

            return ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(referenceInRoom));
        }

        

        

        private static Point3d GetDimPtCloseToReference(List<Point3d> pts, List<int> dim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, double step)
        {
            Tuple<int, Point3d> ptMin = GetDimPtCloseToReference(pts[dim[0]], -dir, reference, barrier, step);
            Tuple<int, Point3d> ptMax = GetDimPtCloseToReference(pts[dim[dim.Count - 1]], dir, reference, barrier, step);
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
                        Tuple<int, Point3d> t = GetDimPtCloseToReference(pts[dim[i]], dir, reference, barrier, step);
                        if (t.Item1 != 3)
                            return t.Item2;
                    }
                }

            }

            return pts[dim[0]];
        }

        private static Tuple<int, Point3d> GetDimPtCloseToReference(Point3d pt, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance=50.0)
        {
            // 选出与box相交及其内部的线
            Polyline box = ThCoordinateService.GenerateBox(pt, dir, step, step);
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

                    if (pt2.DistanceTo(pt) > tolerance && !IsDimCrossReference(pt, pt2, barrier) && !IsDimCrossReference(pt1, pt2, barrier))
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

        private static bool IsDimCrossReference(Point3d pt1, Point3d pt2, ThCADCoreNTSSpatialIndex reference)
        {
            return ThSprinklerDimConflictService.IsDimCrossReference(new Line(pt1, pt2), reference, 10);
        }


        
        private static double GetAdjustedDistance(List<Point3d> dimPts, MPolygon room, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, ref DBObjectCollection dimedArea, ThCADCoreNTSSpatialIndex pipes, string printTag)
        {
            double minDistance = 0;
            double minOverlap = double.MaxValue;
            List<Polyline> minTextBoxes = new List<Polyline>();

            int isOverlap = ThCoordinateService.IsTextBoxOverlap(dimPts[0], dimPts[1], 100);
            for (double distance = 500 + isOverlap * 400; distance < 800 + isOverlap * 400; distance = distance + 50)
            {
                List<Polyline> textBoxes = GetDimTextBoxes(dimPts, distance);
                double overlap = ThSprinklerDimConflictService.GetOverlap(textBoxes, texts, mixColumnWall, dimedArea, pipes, room);

                if (overlap < minOverlap)
                {
                    minDistance = distance;
                    minOverlap = overlap;
                    minTextBoxes = textBoxes;
                }



                if (minOverlap < 40000)
                    break;
            }

            isOverlap = 1-isOverlap;
            for (double distance = -500 - isOverlap * 400; distance > -800 - isOverlap * 400; distance = distance - 50)
            {
                List<Polyline> textBoxes = GetDimTextBoxes(dimPts, distance);
                double overlap = ThSprinklerDimConflictService.GetOverlap(textBoxes, texts, mixColumnWall, dimedArea, pipes, room);

                if (overlap < minOverlap)
                {
                    minDistance = distance;
                    minOverlap = overlap;
                    minTextBoxes = textBoxes;
                }
                else if (overlap == minOverlap && Math.Abs(distance) < Math.Abs(minDistance))
                {
                    minDistance = distance;
                    minOverlap = overlap;
                    minTextBoxes = textBoxes;
                }


                if (minOverlap < 40000)
                    break;
            }

            foreach (Polyline p in minTextBoxes)
            {
                dimedArea.Add(p);
            }

            /// test
            DrawUtils.ShowGeometry(minTextBoxes, "SSS-"+ printTag + "-dimBox" );

            return minDistance;
        }

        private static List<Polyline> GetDimTextBoxes(List<Point3d> dimPts, double distance)
        {
            List<Polyline> textBoxes = new List<Polyline>();

            //for (int i = 0; i < dimPts.Count - 1; i++)
            //{
            //    textBoxes.Add(ThCoordinateService.GetDimTextPolyline(dimPts[i], dimPts[i + 1], distance));
            //}

            textBoxes.Add(ThCoordinateService.GetDimWholePolyline(dimPts[0], dimPts[dimPts.Count-1], distance));

            return textBoxes;
        }

    }
}
