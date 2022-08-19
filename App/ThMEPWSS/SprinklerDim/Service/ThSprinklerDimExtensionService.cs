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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="rooms"></param>
        /// <param name="mixColumnWall"></param>
        /// <param name="axisCurves"></param>
        /// <param name="printTag"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static List<List<List<Point3d>>> GenerateReferenceDimensionPoint(List<ThSprinklerNetGroup> transNetList, List<MPolygon> rooms, List<Polyline> mixColumnWall, List<Line> axisCurves, double step, string printTag, out List<List<Point3d>> unDimedPts)
        {
            List<List<List<Point3d>>> dimPtsList = new List<List<List<Point3d>>>();
            unDimedPts = new List<List<Point3d>>();

            for (int i = 0; i < transNetList.Count; i++)
            {
                ThSprinklerNetGroup transNet = transNetList[i];
                MPolygon room = rooms[i];
                List<Point3d> pts = ThCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                ThCADCoreNTSSpatialIndex columnWallRoomSI = GetReferanceSpatialIndex(mixColumnWall, room);
                ThCADCoreNTSSpatialIndex axisCurvesSI = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(ThGeometryOperationService.Trim(axisCurves, room)));

                List<List<Point3d>> xDimPts = GenerateReferenceDimensionPoint(pts, transNet.Transformer, transNet.XDimension, columnWallRoomSI, axisCurvesSI, true, step, printTag, out var xUnDimedPts);
                List<List<Point3d>> yDimPts = GenerateReferenceDimensionPoint(pts, transNet.Transformer, transNet.YDimension, columnWallRoomSI, axisCurvesSI, false, step, printTag, out var yUnDimedPts);

                dimPtsList.Add(xDimPts.Concat(yDimPts).ToList());

                unDimedPts.AddRange(xUnDimedPts);
                unDimedPts.AddRange(yUnDimedPts);
            }
            
            return dimPtsList;
        }

        private static List<List<Point3d>> GenerateReferenceDimensionPoint(List<Point3d> pts, Matrix3d transformer, List<List<ThSprinklerDimGroup>> dims, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, bool isXAxis, double step, string printTag, out List<List<Point3d>> unDimedPts)
        {
            // 从长到短排序
            dims.Sort((x, y) => y.Count - x.Count);

            List<List<Point3d>> realDimPts = new List<List<Point3d>>();// 真实标注点
            List<Point3d> dimedPts = new List<Point3d>();//被管住的找到room、wall、column、axis curve的点
            unDimedPts = new List<List<Point3d>>();

            foreach (List<ThSprinklerDimGroup> dg in dims)
            {
                List<int> dim = ThDataTransformService.GetDim(dg);
                List<int> tDimedPts = ThDataTransformService.GetDimedPts(dg);


                foreach (int i in dim)
                {

                    if (Math.Abs(pts[i].X - 845617.3) < 10 && Math.Abs(pts[i].Y - 401385.2) < 10)
                    {
                        int a = 0;
                    }
                }


                Vector3d dir = ThCoordinateService.GetDirrection(transformer.Inverse(), isXAxis);
                List<List<int>> allAvailableDims = new List<List<int>>();
                if(dim.Count == 1)
                {
                    foreach(int singleDim in tDimedPts)
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
                        dimedPts.AddRange(ThDataTransformService.GetPoints(pts, tDimedPts));
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
                            dimedPts.AddRange(ThDataTransformService.GetPoints(pts, tDimedPts));
                            tag = 1;
                            break;
                        }

                    }

                }

                if (!isFound) // 标注点小于3的找已标注的点
                {
                    foreach (var d in allAvailableDims)
                    {
                        if(d.Count < 3)
                        {
                            pt = GetDimPtCloseToReference(pts, d, dir, dimedPts, roomWallColumn, step);
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
                    //if (Math.Abs(dimPts[0].X - 849544.0) < 10)
                    //{
                    //    int k = 0;
                    //}

                    dimPts.Add(pt);
                    dimPts.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                    realDimPts.Add(dimPts);

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
                else
                {
                    List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, dim);
                    unDimedPts.Add(dimPts);

                    // test
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

            return realDimPts;
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Polyline> mixCollumWall, MPolygon room)
        {
            List<Polyline> refence = ThGeometryOperationService.Trim(mixCollumWall, room);
            refence.AddRange(ThDataTransformService.Change(room));

            return ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(refence));
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


        private static Point3d GetDimPtCloseToReference(List<Point3d> pts, List<int> dim, Vector3d dir, List<Point3d> dimedPts, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance=50)
        {
            List<Point3d> tDim = ThDataTransformService.GetPoints(pts, dim);

            Point3d ptInDimedPts = tDim[0];
            double minDistance = double.MaxValue;
            foreach (Point3d pt1 in tDim)
            {
                foreach(Point3d pt2 in dimedPts)
                {
                    Line line = new Line(pt1, pt1 + dir);
                    Point3d pt = line.GetClosestPointTo(pt2, true);

                    double distance1 = pt1.DistanceTo(pt);
                    double distance2 = pt2.DistanceTo(pt);

                    if (tolerance < distance1 && distance1 < step && distance2 < 1500 && !IsDimCrossReference(pt, pt1, barrier) && !IsDimCrossReference(pt, pt2, barrier))
                    {
                        if (distance1 < minDistance)
                        {
                            minDistance = distance1;
                            ptInDimedPts = pt;
                        }
                    }
                    
                }
            }

            if (ptInDimedPts != tDim[0])
            {
                return ptInDimedPts;
            }

            return tDim[0];
        }

        private static Tuple<int, Point3d> GetDimPtCloseToReference(Point3d pt, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance=50.0)
        {
            // 选出与box相交及其内部的线
            Polyline box = ThCoordinateService.GenerateBox(pt, dir, step, step);
            List<Line> selectedLines = ThDataTransformService.Change(ThGeometryOperationService.SelectCrossingPolygon(reference, box));

            // 过滤选出与标注方向大致垂直的参考线
            List<Line> filteredLines = new List<Line>();
            foreach (Line line in selectedLines)
            {
                if (line.Length > 10 && ThCoordinateService.IsVertical(line.EndPoint - line.StartPoint, dir))
                {
                    filteredLines.Add(line);
                }
            }

            // 找出往这些参考线作的标注点
            List<Point3d> ptsIntersectReference = new List<Point3d>();
            List<Point3d> ptsNotIntersectReference = new List<Point3d>();
            if (filteredLines.Count > 0)
            {
                foreach(Line referenceLine in filteredLines)
                {
                    Point3d ptInReference = referenceLine.GetClosestPointTo(pt, false);
                    Point3d ptInReferenceExtension = referenceLine.GetClosestPointTo(pt, true);

                    Line dim = new Line(pt, pt + dir);
                    Point3d ptInDim = dim.GetClosestPointTo(ptInReference, true);

                    if (ptInDim.DistanceTo(pt) > tolerance && !IsDimCrossReference(pt, ptInDim, barrier) && !IsDimCrossReference(ptInReference, ptInDim, barrier))
                    {

                        if (ptInReference.Equals(ptInReferenceExtension)) // 与参照物相交
                            ptsIntersectReference.Add(ptInDim);
                        else
                            ptsNotIntersectReference.Add(ptInDim);

                    }
                 
                }
                
            }

            // 按照距离由近到远排序这些待选择的标注点
            ptsIntersectReference.Sort((x, y) => x.DistanceTo(pt).CompareTo(y.DistanceTo(pt)));
            ptsNotIntersectReference.Sort((x, y) => x.DistanceTo(pt).CompareTo(y.DistanceTo(pt)));

            // 优先选择与参照物相交的点
            foreach (Point3d p in ptsIntersectReference)
            {
                if (ThCoordinateService.IsTheSameDirrection(p - pt, dir))
                    return new Tuple<int, Point3d>(1, p);
            }

            // 其次选择与参照物不相交的点
            foreach (Point3d p in ptsNotIntersectReference)
            {
                if (ThCoordinateService.IsTheSameDirrection(p - pt, dir))
                    return new Tuple<int, Point3d>(2, p);
            }

            // 均无则返回原点
            return new Tuple<int, Point3d>(3, pt);
        }

        private static bool IsDimCrossReference(Point3d pt1, Point3d pt2, ThCADCoreNTSSpatialIndex reference)
        {
            return ThSprinklerDimConflictService.IsDimCrossBarrier(new Line(pt1, pt2), reference, 10);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dimPtsList"></param>
        /// <param name="rooms"></param>
        /// <param name="texts"></param>
        /// <param name="pipes"></param>
        /// <param name="printTag"></param>
        /// <returns></returns>
        public static List<ThSprinklerDimension> GenerateDimensionDirectionAndDistance(List<List<List<Point3d>>> dimPtsList, List<MPolygon> rooms, List<Polyline> texts, List<Polyline> mixColumnWall, List<Polyline> pipes, string printTag)
        {
            List<ThSprinklerDimension> realDim = new List<ThSprinklerDimension>();

            for (int i = 0; i < rooms.Count; i++)
            {
                List<List<Point3d>> dimPts = dimPtsList[i];
                MPolygon room = rooms[i];

                ThCADCoreNTSSpatialIndex textsInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(texts, room));
                ThCADCoreNTSSpatialIndex mixColumnWallInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(mixColumnWall, room));
                ThCADCoreNTSSpatialIndex pipesInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(pipes, room));
                DBObjectCollection dimedArea = new DBObjectCollection();

                List<ThSprinklerDimension> dim = GenerateDimensionDirectionAndDistance(dimPts, room, textsInRoom, mixColumnWallInRoom, ref dimedArea, pipesInRoom, printTag);
                realDim.AddRange(dim);
            }

            return realDim;
        }

        private static List<ThSprinklerDimension> GenerateDimensionDirectionAndDistance(List<List<Point3d>> dimPts, MPolygon rooms, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, ref DBObjectCollection dimedArea, ThCADCoreNTSSpatialIndex pipes, string printTag)
        {
            List<ThSprinklerDimension> realDims = new List<ThSprinklerDimension>();
            foreach (List<Point3d> dim in dimPts)
            {
                double distance = GetAdjustedDistance(dim, rooms, texts, mixColumnWall, ref dimedArea, pipes, printTag);

                double op = distance > 0 ? 1 : -1;
                Vector3d dirrection = (dim[1] - dim[0]).GetNormal().RotateBy(op*Math.PI / 2, new Vector3d(0, 0, 1));

                realDims.Add(new ThSprinklerDimension(dim, dirrection, Math.Abs(distance)));

             
                // test
                //if (distance > 0)
                //{
                //    DrawUtils.ShowGeometry(dim[0], string.Format("SSS-{0}-6Dim", printTag), 3, 50, 100);
                //}
                //else
                //{
                //    DrawUtils.ShowGeometry(dim[0], string.Format("SSS-{0}-6Dim", printTag), 2, 50, 100);
                //}

            }

            return realDims;
        }

        private static double GetAdjustedDistance(List<Point3d> dimPts, MPolygon room, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, ref DBObjectCollection dimedArea, ThCADCoreNTSSpatialIndex pipes, string printTag)
        {
            double minDistance = 0;
            long minOverlap = long.MaxValue;
            List<Polyline> minTextBoxes = new List<Polyline>();

            int isOverlap = ThCoordinateService.IsTextBoxOverlap(dimPts[0], dimPts[1], 100);
            for (double distance = 500 + isOverlap * 300; distance < 800 + isOverlap * 300; distance = distance + 50)
            {
                List<Polyline> textBoxes = GetDimTextBoxes(dimPts, distance);
                long overlap = ThSprinklerDimConflictService.GetOverlap(textBoxes, texts, mixColumnWall, dimedArea, pipes, room);

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
            for (double distance = -500 - isOverlap * 300; distance > -800 - isOverlap * 300; distance = distance - 50)
            {
                List<Polyline> textBoxes = GetDimTextBoxes(dimPts, distance);
                long overlap = ThSprinklerDimConflictService.GetOverlap(textBoxes, texts, mixColumnWall, dimedArea, pipes, room);

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

            // 文字框
            //for (int i = 0; i < dimPts.Count - 1; i++)
            //{
            //    textBoxes.Add(ThCoordinateService.GetDimTextPolyline(dimPts[i], dimPts[i + 1], distance));
            //}

            // 整框
            textBoxes.Add(ThCoordinateService.GetDimWholePolyline(dimPts[0], dimPts[dimPts.Count-1], distance));

            return textBoxes;
        }

    }
}
