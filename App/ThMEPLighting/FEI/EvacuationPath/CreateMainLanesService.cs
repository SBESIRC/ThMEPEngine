using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.LaneLine;
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
                var closetPt = x.First().GetClosestPointTo(pt, true);
                return pt.DistanceTo(closetPt);
            }).ToList();

            var extendLines = ExtendLine(frame, pt, extendDir, lanes, holes, 1, null);

            return extendLines;
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
        private List<ExtendLineModel> ExtendLine(Polyline frame, Point3d pt, Vector3d extendDir, List<List<Line>> lanes, List<Polyline> holes, int startNum, List<Line> preLines)
        {
            Point3d spt = pt;
            List<Line> preLanes = preLines;
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
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

                    Ray ray = new Ray();
                    ray.BasePoint = spt;
                    ray.UnitDir = lineVerticalDir;
                    Point3dCollection intersectPts = new Point3dCollection();
                    ray.IntersectWith(line, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);
                    if (intersectPts.Count > 0)
                    {
                        //判断是否穿洞口
                        if (CheckService.CheckIntersectWithHols(ray, holes, out List<Polyline> intersectHoles, out Point3d interPt))
                        {
                            if (spt.DistanceTo(interPt) <= spt.DistanceTo(intersectPts[0]))  //穿洞
                            {
                                var movePts = MoveStartPoint(spt, lineVerticalDir, intersectHoles);
                                foreach (var mPt in movePts)
                                {
                                    var ajustPt = AjustStartPoint(preLanes, mPt, lineVerticalDir);
                                    if (ajustPt != null)
                                    {
                                        resLines.AddRange(ExtendLine(frame, ajustPt.Value, extendDir, lanes, holes, i, preLanes));
                                    }
                                }

                                return resLines;
                            }
                        }
                        //未穿洞
                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, spt.ToPoint2D(), 0, 0, 0);
                        polyline.AddVertexAt(0, intersectPts[0].ToPoint2D(), 0, 0, 0);
                        extendLine.line = polyline;
                        extendLine.priority = Priority.secondLevel;
                        extendLine.endLane = lanes[i];
                        resLines.Add(extendLine);
                        spt = intersectPts[0];
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
                        if (CheckIntersectWithFrame(frame, preLanes, lanes[i], spt, out Line overlapLine))
                        {
                            var newLine = CreateExtendLineAvoidFrame(overlapLine, lanes[i], spt, frame, holes);
                            if (newLine != null)
                            {
                                var dir = (newLine.EndPoint - newLine.StartPoint).GetNormal();
                                var ajustPt = AjustStartPoint(preLanes, newLine.StartPoint, dir);
                                if (ajustPt != null)
                                {
                                    Polyline polyline = new Polyline();
                                    polyline.AddVertexAt(0, ajustPt.Value.ToPoint2D(), 0, 0, 0);
                                    polyline.AddVertexAt(0, newLine.EndPoint.ToPoint2D(), 0, 0, 0);
                                    extendLine.line = polyline;
                                    extendLine.priority = Priority.secondLevel;
                                    extendLine.endLane = lanes[i];
                                    resLines.Add(extendLine);
                                    spt = newLine.EndPoint;
                                    preLanes = lanes[i];
                                }
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
        private bool CheckIntersectWithFrame(Polyline frame, List<Line> lines, List<Line> otherLines, Point3d sPt, out Line overlapLine)
        {
            overlapLine = GeUtils.LineOverlap(lines, otherLines);
            if (overlapLine == null || overlapLine.Length < lapDistance)
            {
                return false;
            }

            var closetPt = otherLines.Select(x => x.GetClosestPointTo(sPt, true)).OrderBy(x => x.DistanceTo(sPt)).First();
            Line extendLine = new Line(closetPt, sPt);

            return CheckService.CheckIntersectWithFrame(extendLine, frame);
        }

        private Line CreateExtendLineAvoidFrame(Line line, List<Line> otherLines, Point3d sPt, Polyline frame, List<Polyline> holes)
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

            var transSP = startPt.TransformBy(matrix);
            var transEP = endPt.TransformBy(matrix);
            var moveDir = (transEP - transSP).GetNormal();
            var sP = transSP + moveDir * moveStep;
            while ( sP.X < transEP.X)
            {
                var extendPt = sP.TransformBy(matrix.Inverse());
                var closePt = otherLines.Select(x => x.GetClosestPointTo(extendPt, false)).OrderBy(x => x.DistanceTo(extendPt)).FirstOrDefault();
                if (closePt != null)
                {
                    var extendLine = new Line(extendPt, closePt);
                    if (!CheckService.CheckIntersectWithFrame(extendLine, frame) &&
                        !CheckService.CheckIntersectWithHols(extendLine, holes, out List<Polyline> interHoles))
                    {
                        return extendLine;
                    }
                }

                sP = sP + xDir * moveStep;
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
        private List<Point3d> MoveStartPoint(Point3d spt, Vector3d dir, List<Polyline> holes)
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

            var allPts = holes.SelectMany(x =>
            {
                var pts = new List<Point3d>();
                for (int i = 0; i < x.NumberOfVertices; i++)
                {
                    pts.Add(x.GetPoint3dAt(i).TransformBy(matrix.Inverse()));
                }
                return pts;
            }).OrderBy(x => x.X).ToList();

            var transPt = spt.TransformBy(matrix.Inverse());
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
            Ray ray = new Ray();
            Point3dCollection intersectPts = new Point3dCollection();
            foreach (var line in lane)
            {
                ray.BasePoint = spt - dir * 50;
                ray.UnitDir = dir;
                ray.IntersectWith(line, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);
                if (intersectPts.Count > 0)
                {
                    return intersectPts[0];
                }

                ray.BasePoint = spt + dir * 50;
                ray.UnitDir = -dir;
                ray.IntersectWith(line, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);
                if (intersectPts.Count > 0)
                {
                    return intersectPts[0];
                }
            }

            return null;
        }
    }
}
