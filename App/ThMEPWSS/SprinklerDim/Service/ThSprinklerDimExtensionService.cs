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
        /// 给每根标注线找参考点，可参考的东西有柱墙房间、轴网、已标注的点
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="rooms"></param>
        /// <param name="mixColumnWall"></param>
        /// <param name="axisCurves"></param>
        /// <param name="printTag"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static List<List<List<Point3d>>> FindReferencePoint(List<ThSprinklerNetGroup> transNetList, List<MPolygon> rooms, List<Polyline> mixColumnWall, List<Line> axisCurves, double step, string printTag, out List<List<Point3d>> unDimedPts)
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

                List<List<int>> xCollineation = ThDataTransformService.Mix(transNet.XCollineationGroup);
                List<List<int>> yCollineation = ThDataTransformService.Mix(transNet.YCollineationGroup);

                List<List<Point3d>> xDimPts = FindReferencePointForNetGroup(pts, transNet.Transformer, transNet.XDimension, xCollineation, columnWallRoomSI, axisCurvesSI, true, step, printTag, out var xUnDimedPts);
                List<List<Point3d>> yDimPts = FindReferencePointForNetGroup(pts, transNet.Transformer, transNet.YDimension, yCollineation, columnWallRoomSI, axisCurvesSI, false, step, printTag, out var yUnDimedPts);

                dimPtsList.Add(xDimPts.Concat(yDimPts).ToList());

                unDimedPts.AddRange(xUnDimedPts);
                unDimedPts.AddRange(yUnDimedPts);
            }
            
            return dimPtsList;
        }

        public static List<List<Point3d>> FindReferencePoint(List<ThSprinklerNetGroup> transNetList, MPolygon room, List<Polyline> mixColumnWall, List<Line> axisCurves, double step, string printTag, out List<List<Point3d>> unDimedPts)
        {
            List<List<Point3d>> dimPtsList = new List<List<Point3d>>();
            unDimedPts = new List<List<Point3d>>();

            ThCADCoreNTSSpatialIndex columnWallRoomSI = GetReferanceSpatialIndex(mixColumnWall, room);
            ThCADCoreNTSSpatialIndex axisCurvesSI = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(ThGeometryOperationService.Trim(axisCurves, room)));

            for (int i = 0; i < transNetList.Count; i++)
            {
                ThSprinklerNetGroup transNet = transNetList[i];
                List<Point3d> pts = ThCoordinateService.MakeTransformation(transNet.Pts, transNet.Transformer.Inverse());

                List<List<int>> xCollineation = ThDataTransformService.Mix(transNet.XCollineationGroup);
                List<List<int>> yCollineation = ThDataTransformService.Mix(transNet.YCollineationGroup);

                List<List<Point3d>> xDimPts = FindReferencePointForNetGroup(pts, transNet.Transformer, transNet.XDimension, xCollineation, columnWallRoomSI, axisCurvesSI, true, step, printTag, out var xUnDimedPts);
                List<List<Point3d>> yDimPts = FindReferencePointForNetGroup(pts, transNet.Transformer, transNet.YDimension, yCollineation, columnWallRoomSI, axisCurvesSI, false, step, printTag, out var yUnDimedPts);

                dimPtsList.AddRange(xDimPts);
                dimPtsList.AddRange(yDimPts);

                unDimedPts.AddRange(xUnDimedPts);
                unDimedPts.AddRange(yUnDimedPts);
            }

            return dimPtsList;
        }

        private static List<List<Point3d>> FindReferencePointForNetGroup(List<Point3d> pts, Matrix3d transformer, List<List<ThSprinklerDimGroup>> dims, List<List<int>> anotherCollineation, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, bool isXAxis, double step, string printTag, out List<List<Point3d>> unDimedLines)
        {
            // 标注从长到短排序
            dims.Sort((x, y) => y.Count - x.Count);

            List<List<Point3d>> dimedLines = new List<List<Point3d>>(); // 已找到参照物的标注线
            unDimedLines = new List<List<Point3d>>(); // 未找到参照物的标注线
            List<Point3d> dimedPts = new List<Point3d>(); //被管住的找到room、wall、column、axis curve的点
            

            foreach (List<ThSprinklerDimGroup> dg in dims)
            {
                List<int> dim = ThDataTransformService.GetDim(dg);
                Vector3d dir = ThCoordinateService.GetDirrection(transformer.Inverse(), isXAxis);

                List<Point3d> dimLine = new List<Point3d>();
                if(dim.Count == 1) // 标注为点
                {
                    List<List<Point3d>> allAvailableDims = GetAllAvailableDims(pts, dim[0], anotherCollineation);
                    dimLine = FindReferencePointForSinglePoint(allAvailableDims, dir, roomWallColumn, axisCurves, dimedPts, isXAxis, step, printTag);
                }
                else // 标注为线
                {
                    dim.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                    List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, dim);
                    dimLine = FindReferencePointForLine(dimPts, dir, roomWallColumn, axisCurves, dimedPts, isXAxis, step, printTag);
                }

                if(dimLine != null && dimLine.Count > 1)// 找到
                {
                    dimedPts.AddRange(ThDataTransformService.GetPoints(pts, ThDataTransformService.GetDimedPts(dg)));
                    dimedLines.Add(dimLine);
                }
                else  // 没找到
                {
                    List<Point3d> dimPts = ThDataTransformService.GetPoints(pts, dim);
                    unDimedLines.Add(dimPts);


                    // test
                    List<Line> lineList = new List<Line>();
                    for (int i = 0; i < dimPts.Count - 1; i++)
                    {
                        lineList.Add(new Line(dimPts[i], dimPts[i + 1]));

                        DrawUtils.ShowGeometry(dimPts[i], string.Format("SSS-{0}-7UnDim", printTag), 1, 50, 500);
                    }
                    DrawUtils.ShowGeometry(dimPts[dimPts.Count - 1], string.Format("SSS-{0}-7UnDim", printTag), 1, 50, 500);
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-7UnDim", printTag), 1, 35);
                    /////////////////////////


                }
            }

            return dimedLines;
        }


        
        private static List<Point3d> FindReferencePointForSinglePoint( List<List<Point3d>> allAvailableDims, Vector3d dir, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, List<Point3d> dimedPts, bool isXAxis, double step, string printTag)
        {
            int tag = 0;

            // 房间框线、墙、柱
            ThSprinklerDimReferencePoint referencePoint = FindReferencePointForSinglePoint(allAvailableDims, dir, roomWallColumn, roomWallColumn, isXAxis, step, printTag);

            // 轴网
            if (referencePoint.Type == 3)
            {
                tag = 1;
                referencePoint = FindReferencePointForSinglePoint(allAvailableDims, dir, axisCurves, roomWallColumn, isXAxis, step, printTag);
            }

            // 已标注的点
            if (referencePoint.Type == 3)
            {
                tag = 2;
                referencePoint = GetReferencePtCloseToDimedPts(allAvailableDims, dir, dimedPts, roomWallColumn, step);
            }

            if (referencePoint.Type != 3) // 找到
            {
                List<Point3d> dim = new List<Point3d> { referencePoint.SprinklerDimPt };
                dim.Add(referencePoint.ReferencePt);
                dim.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                /// test
                DrawUtils.ShowGeometry(referencePoint.ReferencePt, string.Format("SSS-{0}-6Dim", printTag), 11, 50, 100);

                List<Line> lineList = new List<Line>();
                for (int i = 0; i < dim.Count - 1; i++)
                {
                    lineList.Add(new Line(dim[i], dim[i + 1]));
                }

                if (tag == 0) // 0 房间  白线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 0, 35);
                }
                else if (tag == 1) // 1 轴网  绿线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 3, 35);
                }
                else if (tag == 2) // 2 网格  蓝线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 4, 35);
                }
                /////////////////////////

                return dim;
            }
            else
            {
                return null;
            }
                
        }

        private static ThSprinklerDimReferencePoint FindReferencePointForSinglePoint(List<List<Point3d>> allAvailableDims, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, bool isXAxis, double step, string printTag)
        {
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();

            referencePts.Add(GetReferencePtCloseToReferenceForSprinklerLine(allAvailableDims[0], dir, reference, barrier, step));
            referencePts.Add(GetReferencePtCloseToReferenceForSprinklerLine(allAvailableDims[allAvailableDims.Count - 1], dir, reference, barrier, step));
            ThSprinklerDimReferencePoint referencePoint = GetOptimalReferencePoint(referencePts);

            if (referencePoint.Type == 3)
            {
                for (int i = 1; i < allAvailableDims.Count - 1; i++)
                {
                    var d = allAvailableDims[i];
                    referencePts.Add(GetReferencePtCloseToReferenceForSprinklerLine(d, dir, reference, barrier, step));
                }
            }

            return GetOptimalReferencePoint(referencePts);
        }



        private static ThSprinklerDimReferencePoint GetReferencePtCloseToDimedPts(List<List<Point3d>> allAvailableDims, Vector3d dir, List<Point3d> dimedPts, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance = 50)
        {
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();

            referencePts.Add(GetReferencePtCloseToDimedPts(allAvailableDims[0], dir, dimedPts, barrier, step));
            referencePts.Add(GetReferencePtCloseToDimedPts(allAvailableDims[allAvailableDims.Count - 1], dir, dimedPts, barrier, step));
            ThSprinklerDimReferencePoint referencePoint = GetOptimalReferencePoint(referencePts);

            if (referencePoint.Type == 3)
            {
                for (int i = 1; i < allAvailableDims.Count - 1; i++)
                {
                    var d = allAvailableDims[i];
                    referencePts.Add(GetReferencePtCloseToDimedPts(d, dir, dimedPts, barrier, step));
                }
            }

            return GetOptimalReferencePoint(referencePts);
        }

        private static ThSprinklerDimReferencePoint GetReferencePtCloseToDimedPts( List<Point3d> dim, Vector3d dir, List<Point3d> dimedPts, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance = 50)
        {
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();
            referencePts.Add(GetReferencePtCloseToDimedPts(dim[0], -dir, dimedPts, barrier, step));
            referencePts.Add(GetReferencePtCloseToDimedPts(dim[dim.Count - 1], dir, dimedPts, barrier, step));
            ThSprinklerDimReferencePoint referencePoint = GetOptimalReferencePoint(referencePts);
            if (referencePoint.Type != 3)
            {
                return referencePoint;
            }
            else
            {
                referencePts.Clear();
                for (int i = 0; i < dim.Count - 1; i++)
                {
                    referencePts.Add(GetReferencePtCloseToDimedPts(dim[i], dir, dimedPts, barrier, step));
                }

                return GetOptimalReferencePoint(referencePts);

            }
        }

        private static ThSprinklerDimReferencePoint GetReferencePtCloseToDimedPts(Point3d dim, Vector3d dir, List<Point3d> dimedPts, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance = 50)
        {
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();

            foreach (Point3d pt2 in dimedPts)
            {
                Line line = new Line(dim, dim + dir);
                Point3d referencePt = line.GetClosestPointTo(pt2, true);

                double dimensionDistance = dim.DistanceTo(referencePt);
                double verticalDistance = pt2.DistanceTo(referencePt);

                if (tolerance < dimensionDistance && dimensionDistance < step && verticalDistance < 1500 && ThCoordinateService.IsTheSameDirrection(referencePt-dim, dir))
                //if (tolerance < dimensionDistance && dimensionDistance < step && verticalDistance < 1500 && !IsDimCrossReference(pt, pt1, barrier) && !IsDimCrossReference(pt, pt2, barrier))
                {
                    referencePts.Add(new ThSprinklerDimReferencePoint(2, dim, referencePt, dimensionDistance, verticalDistance));
                }

            }

            return GetOptimalReferencePoint(referencePts);
        }



        private static List<Point3d> FindReferencePointForLine(List<Point3d> dim, Vector3d dir, ThCADCoreNTSSpatialIndex roomWallColumn, ThCADCoreNTSSpatialIndex axisCurves, List<Point3d> dimedPts, bool isXAxis, double step, string printTag)
        {
            int tag = 0;
            // 房间框线、墙、柱
            ThSprinklerDimReferencePoint referencePoint = GetReferencePtCloseToReferenceForSprinklerLine(dim, dir, roomWallColumn, roomWallColumn, step);

            // 轴网
            if (referencePoint.Type == 3)
            {
                tag = 1;
                referencePoint = GetReferencePtCloseToReferenceForSprinklerLine(dim, dir, axisCurves, roomWallColumn, step);
            }

            // 标注点小于3的找已标注的点
            if (referencePoint.Type == 3 && dim.Count < 3)
            {
                tag = 2;
                referencePoint = GetReferencePtCloseToDimedPts(dim, dir, dimedPts, roomWallColumn, step);
            }

            if (referencePoint.Type != 3) // 找到
            {
                dim.Add(referencePoint.ReferencePt);
                dim.Sort((x, y) => ThCoordinateService.GetOriginalValue(x, isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(y, isXAxis)));

                /// test
                DrawUtils.ShowGeometry(referencePoint.ReferencePt, string.Format("SSS-{0}-6Dim", printTag), 11, 50, 100);

                List<Line> lineList = new List<Line>();
                for (int i = 0; i < dim.Count - 1; i++)
                {
                    lineList.Add(new Line(dim[i], dim[i + 1]));
                }

                if (tag == 0) // 0 房间  白线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 0, 35);
                }
                else if (tag == 1) // 1 轴网  绿线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 3, 35);
                }
                else if (tag == 2) // 2 网格  蓝线
                {
                    DrawUtils.ShowGeometry(lineList, string.Format("SSS-{0}-6Dim", printTag), 4, 35);
                }
                /////////////////////////

                return dim;
            }
            else
            {
                return null;
            }

        }

        private static ThSprinklerDimReferencePoint GetReferencePtCloseToReferenceForSprinklerLine(List<Point3d> dim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, double step)
        {
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();
            referencePts.Add(GetReferencePtCloseToReferenceForSprinklerPt(dim[0], -dir, reference, barrier, step));
            referencePts.Add(GetReferencePtCloseToReferenceForSprinklerPt(dim[dim.Count - 1], dir, reference, barrier, step));
            ThSprinklerDimReferencePoint referencePoint = GetOptimalReferencePoint(referencePts);
            if (referencePoint.Type != 3)
            {
                return referencePoint;
            }
            else
            {
                referencePts.Clear();
                for (int i = 0; i < dim.Count - 1; i++)
                {
                    referencePts.Add(GetReferencePtCloseToReferenceForSprinklerPt(dim[i], dir, reference, barrier, step));
                }

                return GetOptimalReferencePoint(referencePts);

            }

        }
        
        private static ThSprinklerDimReferencePoint GetReferencePtCloseToReferenceForSprinklerPt(Point3d ptInDim, Vector3d dir, ThCADCoreNTSSpatialIndex reference, ThCADCoreNTSSpatialIndex barrier, double step, double tolerance=50.0)
        {
            // 选出与box相交及其内部的线
            Polyline box = ThCoordinateService.GenerateBox(ptInDim, dir, step, step);
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

            // 找出标注方向延长相交、不相交的标注点
            List<ThSprinklerDimReferencePoint> referencePts = new List<ThSprinklerDimReferencePoint>();
            if (filteredLines.Count > 0)
            {
                foreach(Line referenceLine in filteredLines)
                {
                    Point3d ptInReference = referenceLine.GetClosestPointTo(ptInDim, false);
                    Point3d ptInReferenceExtension = referenceLine.GetClosestPointTo(ptInDim, true);

                    Line referenceDim = new Line(ptInDim, ptInDim + dir);
                    Point3d referencePt = referenceDim.GetClosestPointTo(ptInReference, true);

                    //if (referencePt.DistanceTo(ptInDim) > tolerance && ThCoordinateService.IsTheSameDirrection(referencePt - ptInDim, dir) && !IsDimCrossReference(ptInDim, referencePt, barrier) && !IsDimCrossReference(ptInReference, referencePt, barrier))
                    if (referencePt.DistanceTo(ptInDim) > tolerance && ThCoordinateService.IsTheSameDirrection(referencePt - ptInDim, dir))
                    {
                        if (ptInReference.Equals(ptInReferenceExtension)) // 与参照物相交
                            referencePts.Add(new ThSprinklerDimReferencePoint(1, ptInDim, referencePt, referencePt.DistanceTo(ptInDim), referencePt.DistanceTo(ptInReference)));
                        else
                            referencePts.Add(new ThSprinklerDimReferencePoint(2, ptInDim, referencePt, referencePt.DistanceTo(ptInDim), referencePt.DistanceTo(ptInReference)));
                    }
                 
                }
                
            }

            return GetOptimalReferencePoint(referencePts);
        }



        private static List<List<Point3d>> GetAllAvailableDims(List<Point3d> pts, int dim, List<List<int>> anotherCollineation)
        {
            List<List<Point3d>> allAvailableDims = new List<List<Point3d>>();

            List<int> singleDims = anotherCollineation.Where(x => x.Contains(dim)).ToList()[0];
            foreach (int i in singleDims)
                allAvailableDims.Add(new List<Point3d> { pts[i] });

            return allAvailableDims;
        }

        /// <summary>
        /// 相交 》 不相交  
        /// 标注垂直方向 短 》 长
        /// 标注方向 短 》 长
        /// </summary>
        /// <param name="referencePts"></param>
        /// <returns></returns>
        private static ThSprinklerDimReferencePoint GetOptimalReferencePoint(List<ThSprinklerDimReferencePoint> referencePts)
        {
            List<ThSprinklerDimReferencePoint> intersectionReferencePts = referencePts.Where((pt) => pt.Type == 1).ToList();
            intersectionReferencePts.Sort((x, y) => x.DimensionDistance.CompareTo(y.DimensionDistance));
            if (intersectionReferencePts.Count > 0)
                return intersectionReferencePts[0];

            List<ThSprinklerDimReferencePoint> unIntersectionReferencePts = referencePts.Where((pt) => pt.Type == 2).ToList();
            unIntersectionReferencePts.Sort((x, y) => x.VerticalDistance == y.VerticalDistance ? x.DimensionDistance.CompareTo(y.DimensionDistance) : x.VerticalDistance.CompareTo(y.VerticalDistance));
            if (unIntersectionReferencePts.Count > 0)
                return unIntersectionReferencePts[0];

            return new ThSprinklerDimReferencePoint(3);
        }

        private static ThCADCoreNTSSpatialIndex GetReferanceSpatialIndex(List<Polyline> mixCollumWall, MPolygon room)
        {
            List<Polyline> refence = ThGeometryOperationService.Trim(mixCollumWall, room);
            refence.AddRange(ThDataTransformService.Change(room));

            return ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(refence));
        }

        private static bool IsDimCrossReference(Point3d pt1, Point3d pt2, ThCADCoreNTSSpatialIndex reference)
        {
            return ThSprinklerDimConflictService.IsDimCrossBarrier(new Line(pt1, pt2), reference, 10);
        }






        /// <summary>
        /// 对找好参考点的标线上下拉，找到一个碰撞最少的位置
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

                List<Polyline> wall = ThGeometryOperationService.Intersection(mixColumnWall, room);
                List<Polyline> wallAfter = ThGeometryOperationService.RemoveOverlap(wall);
                ThCADCoreNTSSpatialIndex mixColumnWallInRoom = ThDataTransformService.GenerateSpatialIndex(wallAfter);
               
                ThCADCoreNTSSpatialIndex pipesInRoom = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(ThGeometryOperationService.Trim(pipes, room)));
                DBObjectCollection dimedArea = new DBObjectCollection();

                List<ThSprinklerDimension> dim = GenerateDimensionDirectionAndDistance(dimPts, room, textsInRoom, mixColumnWallInRoom, ref dimedArea, pipesInRoom, printTag);
                realDim.AddRange(dim);
            }

            return realDim;
        }

        public static List<ThSprinklerDimension> GenerateDimensionDirectionAndDistance(List<List<Point3d>> dimPtsList, MPolygon room, List<Polyline> texts, List<Polyline> mixColumnWall, List<Polyline> pipes, string printTag)
        {
            ThCADCoreNTSSpatialIndex textsInRoom = ThDataTransformService.GenerateSpatialIndex(ThGeometryOperationService.Intersection(texts, room));

            List<Polyline> wall = ThGeometryOperationService.Intersection(mixColumnWall, room);
            List<Polyline> wallAfter = ThGeometryOperationService.RemoveOverlap(wall);
            ThCADCoreNTSSpatialIndex mixColumnWallInRoom = ThDataTransformService.GenerateSpatialIndex(wallAfter);

            ThCADCoreNTSSpatialIndex pipesInRoom = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(ThGeometryOperationService.Trim(pipes, room)));
            DBObjectCollection dimedArea = new DBObjectCollection();

            List<ThSprinklerDimension> realDim = GenerateDimensionDirectionAndDistance(dimPtsList, room, textsInRoom, mixColumnWallInRoom, ref dimedArea, pipesInRoom, printTag);
            return realDim;
        }

        private static List<ThSprinklerDimension> GenerateDimensionDirectionAndDistance(List<List<Point3d>> dimPts, MPolygon rooms, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, ref DBObjectCollection dimedArea, ThCADCoreNTSSpatialIndex pipes, string printTag)
        {
            // 标注从长到短排序
            dimPts.Sort((x, y) => y.Count - x.Count);

            List<ThSprinklerDimension> realDims = new List<ThSprinklerDimension>();
            foreach (List<Point3d> dim in dimPts)
            {
                double distance = GetAdjustedDistance(dim, rooms, texts, mixColumnWall, ref dimedArea, pipes, printTag);

                double op = distance > 0 ? 1 : -1;
                Vector3d dirrection = (dim[dim.Count-1] - dim[0]).GetNormal().RotateBy(op*Math.PI / 2, new Vector3d(0, 0, 1));

                realDims.Add(new ThSprinklerDimension(dim, dirrection, Math.Abs(distance)));


                // test 起始点与方向
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

            double beg = 500, end = 800, lap = 300;
            for (double distance = beg + isOverlap * lap; distance < end + isOverlap * lap; distance = distance + 50)
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
            for (double distance = -beg - isOverlap * lap; distance > -end - isOverlap * lap; distance = distance - 50)
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

            // 整框
            textBoxes.Add(ThCoordinateService.GetDimWholePolyline(dimPts[0], dimPts[dimPts.Count-1], (dimPts[1]- dimPts[0]).GetNormal(), distance));

            return textBoxes;
        }

    }
}
