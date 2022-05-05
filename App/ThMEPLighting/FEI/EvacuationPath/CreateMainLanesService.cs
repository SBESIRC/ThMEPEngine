using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateMainLanesService
    {
        readonly double moveDistance = 800;
        readonly double lapDistance = 500;
        readonly double moveStep = 500;
        public List<ExtendLineModel> CreateLines(Polyline frame, Point3d pt, Vector3d extendDir, List<List<Line>> lanes, List<Polyline> holes)
        {
            //排序车道线
            lanes = lanes.OrderBy(x =>
            {
                var closetPtInfo = x.Select(y => y.GetClosestPointTo(pt, false).DistanceTo(pt));
                return closetPtInfo.OrderBy(y => y).First();
            }).ToList();

            return ExtendLine(frame, pt, extendDir, lanes, holes, 0, null);
        }

        /// <summary>
        /// 计算延伸线
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="extendDir"></param>
        /// <param name="lanes"></param>
        /// <param name="holes"></param>
        /// <param name="startNum"></param>
        /// <param name="isFirst"></param>
        /// <returns></returns>
        private List<ExtendLineModel> ExtendLine(Polyline frame, Point3d spt, Vector3d extendDir, List<List<Line>> lanes,
            List<Polyline> holes, int startNum, List<Line> preLines, Priority priority = Priority.firstLevel)
        {
            List<Line> preLanes = preLines;
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            int thisNum = startNum;
            for (int i = startNum; i < lanes.Count; i++)
            {
                bool avoidFrame = true;
                ExtendLineModel extendLine = new ExtendLineModel();
                foreach (var line in lanes[i])
                {
                    var lineVerticalDir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                    if (extendDir.DotProduct(lineVerticalDir) < 0)
                    {
                        lineVerticalDir = -lineVerticalDir;
                    }

                    if (GeUtils.GetIntersectPtByDir(spt, lineVerticalDir, line, out Point3dCollection intersectPts))
                    {
                        if (preLanes == null || preLanes.Count <= 0)
                        {
                            if (!intersectPts[0].IsEqualTo(spt, new Tolerance(1, 1)))
                            {
                                continue;
                            }
                        }

                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, spt.ToPoint2D(), 0, 0, 0);
                        polyline.AddVertexAt(0, intersectPts[0].ToPoint2D(), 0, 0, 0);
                        if (CheckService.CheckIntersectWithFrame(polyline, frame))  //穿外包框跳过
                        {
                            continue;
                        }
                        //判断是否穿洞口
                        if (CheckService.CheckIntersectWithHols(polyline, holes, out List<Polyline> intersectHoles))
                        {
                            Polyline interHoleBox = GetAllIntersectHoles(intersectHoles, holes, frame, polyline);
                            if (interHoleBox != null)
                            {
                                var movePts = MoveStartPoint(spt, lineVerticalDir, interHoleBox);
                                foreach (var mPt in movePts)
                                {
                                    var ajustPt = AjustStartPoint(preLanes, mPt, lineVerticalDir);
                                    if (ajustPt != null)
                                    {
                                        resLines.AddRange(ExtendLine(frame, ajustPt.Value, extendDir, lanes, holes, thisNum, preLanes, Priority.secondLevel));
                                    }
                                }
                            }

                            return resLines;
                        }
                        if (!intersectPts[0].IsEqualTo(spt, new Tolerance(1, 1)))
                        {
                            extendLine.line = polyline;
                            extendLine.priority = priority;
                            extendLine.endLane = lanes[i];
                            extendLine.startLane = preLanes;
                            resLines.Add(extendLine);
                            spt = intersectPts[0];
                        }
                        thisNum = i;
                        preLanes = lanes[i];
                        avoidFrame = false;
                        break;
                    }
                }

                //如果需要躲框线就躲框线
                if (avoidFrame)
                {
                    if (preLanes != null)
                    {
                        if (CheckIntersectWithFrame(preLanes, lanes[i], extendDir, spt, frame, out Line overlapLine))
                        {
                            var newLine = CreateExtendLineAvoidFrame(overlapLine, lanes[i], preLanes, spt, frame, holes);
                            if (newLine != null)
                            {
                                extendLine.line = newLine;
                                extendLine.priority = Priority.secondLevel;
                                extendLine.endLane = lanes[i];
                                extendLine.startLane = preLanes;
                                resLines.Add(extendLine);
                                spt = newLine.EndPoint;
                                preLanes = lanes[i];
                            }
                        }
                    }
                }
            }

            return resLines;
        }

        /// <summary>
        /// 检查是否需要躲避外包框
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="lines"></param>
        /// <param name="otherLines"></param>
        /// <param name="sPt"></param>
        /// <returns></returns>
        private bool CheckIntersectWithFrame(List<Line> lines, List<Line> otherLines, Vector3d extendDir, Point3d sPt, Polyline frame, out Line overlapLine)
        {
            overlapLine = GeUtils.LineOverlap(lines, otherLines);
            if (overlapLine == null || overlapLine.Length < lapDistance)
            {
                return false;
            }

            var closetPt = otherLines.Select(x => x.GetClosestPointTo(sPt, true)).OrderBy(x => x.DistanceTo(sPt)).First();
            Line extendLine = new Line(sPt, closetPt);

            //判断是否和延伸方向一致,不一致侧不用再做延伸线
            if ((closetPt - sPt).GetNormal().DotProduct(extendDir) < 0 || !CheckService.CheckIntersectWithFrame(extendLine, frame))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建延伸线(躲避外包框)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLines"></param>
        /// <param name="sPt"></param>
        /// <param name="frame"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Polyline CreateExtendLineAvoidFrame(Line line, List<Line> otherLines, List<Line> preLanes, Point3d sPt, Polyline frame, List<Polyline> holes)
        {
            var startPt = line.StartPoint.DistanceTo(sPt) < line.EndPoint.DistanceTo(sPt) ? line.StartPoint : line.EndPoint;
            var endPt = line.StartPoint.DistanceTo(sPt) > line.EndPoint.DistanceTo(sPt) ? line.StartPoint : line.EndPoint;

            var xDir = (endPt - startPt).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            var transSP = startPt.TransformBy(matrix.Inverse());
            var transEP = endPt.TransformBy(matrix.Inverse());
            var moveDir = (transEP - transSP).GetNormal();
            var sP = transSP + moveDir * moveStep;
            while (sP.X < transEP.X)
            {
                var extendPt = sP.TransformBy(matrix);
                var closeInfo = otherLines.ToDictionary(x => x, y => y.GetClosestPointTo(extendPt, false))
                    .OrderBy(x => x.Value.DistanceTo(extendPt))
                    .First();
                var oLineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (oLineDir.IsParallelTo((closeInfo.Value - extendPt).GetNormal(), new Tolerance(0.001, 0.001)))
                {
                    var extendLine = new Line(extendPt, closeInfo.Value);
                    if (!CheckService.CheckIntersectWithFrame(extendLine, frame) &&
                        !CheckService.CheckIntersectWithHols(extendLine, holes, out List<Polyline> interHoles))
                    {
                        var dir = (extendLine.EndPoint - extendLine.StartPoint).GetNormal();
                        var ajustPt = AjustStartPoint(preLanes, extendLine.StartPoint, dir);
                        if (ajustPt != null)
                        {
                            Polyline polyline = new Polyline();
                            polyline.AddVertexAt(0, ajustPt.Value.ToPoint2D(), 0, 0, 0);
                            polyline.AddVertexAt(1, extendLine.EndPoint.ToPoint2D(), 0, 0, 0);

                            return polyline;
                        }
                    }
                }

                sP = sP + moveDir * moveStep;
            }

            return null;
        }

        /// <summary>
        /// 计算偏移起始点
        /// </summary>
        /// <param name="spt"></param>
        /// <param name="dir"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Point3d> MoveStartPoint(Point3d spt, Vector3d dir, Polyline hole)
        {
            var zDir = Vector3d.ZAxis;
            var xDir = zDir.CrossProduct(dir);
            Matrix3d matrix = new Matrix3d(
                new double[]{
                    xDir.X, dir.X, zDir.X, 0,
                    xDir.Y, dir.Y, zDir.Y, 0,
                    xDir.Z, dir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            var allPts = GeUtils.GetAllPolylinePts(hole).Select(x => x.TransformBy(matrix)).OrderBy(x => x.X).ToList(); 

            var transPt = spt.TransformBy(matrix);
            var moveDir = xDir;
            var leftMoveLength = Math.Abs(allPts.Last().X - transPt.X) + moveDistance;
            var rightMoveLength = Math.Abs(allPts.First().X - transPt.X) + moveDistance;
            if (transPt.DistanceTo(allPts.Last()) > transPt.DistanceTo(allPts.First()))
            {
                leftMoveLength = Math.Abs(allPts.First().X - transPt.X) + moveDistance;
                rightMoveLength = Math.Abs(allPts.Last().X - transPt.X) + moveDistance;
                moveDir = -moveDir;
            }

            List<Point3d> resPt = new List<Point3d>();
            resPt.Add(spt + moveDir * leftMoveLength);
            resPt.Add(spt - moveDir * rightMoveLength);
            return resPt;
        }

        /// <summary>
        /// 调整起始点到车道线上
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="spt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Point3d? AjustStartPoint(List<Line> lane, Point3d spt, Vector3d dir)
        {
            if (lane == null)
            {
                return spt;
            }

            Point3dCollection intersectPts = new Point3dCollection();
            foreach (var line in lane)
            {
                if (GeUtils.GetIntersectPtByDir(spt - dir * 50, dir, line, out intersectPts))
                {
                    return intersectPts[0];
                }

                if (GeUtils.GetIntersectPtByDir(spt + dir * 50, -dir, line, out intersectPts))
                {
                    return intersectPts[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 计算所有穿越障碍物
        /// </summary>
        /// <param name="intersectHoles"></param>
        /// <param name="allHoles"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Polyline GetAllIntersectHoles(List<Polyline> intersectHoles, List<Polyline> allHoles, Polyline frame, Polyline polyline)
        {
            List<Polyline> checkHoles = allHoles.Except(intersectHoles).ToList();
            var allPts = intersectHoles.SelectMany(x => GeUtils.GetAllPolylinePts(x)).ToList();
            allPts.AddRange(GeUtils.GetAllPolylinePts(polyline));

            Vector3d xDir = (polyline.EndPoint - polyline.StartPoint).GetNormal();
            var boungdingBox = GeUtils.GetBoungdingBox(allPts, xDir);
            var bufferBox = boungdingBox.Buffer(moveDistance)[0] as Polyline;

            if (CheckService.CheckIntersectWithFrame(polyline, frame))
            {
                return null;
            }

            var interHoles = SelectService.SelelctCrossing(checkHoles, bufferBox);
            if (interHoles.Count > 0)
            {
                interHoles.AddRange(intersectHoles);
                return GetAllIntersectHoles(interHoles, checkHoles, frame, polyline);
            }
            
            return boungdingBox;
        }
    }
}
