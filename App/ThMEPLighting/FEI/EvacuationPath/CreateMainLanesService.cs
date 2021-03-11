using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateMainLanesService
    {
        readonly double moveDistance = 800;
        private void CreateLine(Point3d pt, Vector3d extendDir, List<List<Line>> lanes)
        {
            //排序车道线
            lanes = lanes.OrderBy(x =>
            {
                var closetPt = x.First().GetClosestPointTo(pt, false);
                return pt.DistanceTo(closetPt);
            }).ToList();

            Point3d spt = pt;
            List<Point3d> resPts = new List<Point3d>();
            foreach (var lines in lanes)
            {
                foreach (var line in lines)
                {
                    var lineDir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                    if (extendDir.DotProduct(lineDir) < 0)
                    {
                        lineDir = -lineDir;
                    }
                }
            }
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
        private List<ExtendLineModel> ExtendLine(Point3d pt, Vector3d extendDir, List<List<Line>> lanes, List<Polyline> holes, int startNum, List<Line> preLane)
        {
            Point3d spt = pt;
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            for (int i = startNum; i < lanes.Count; i++)
            {
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
                        ExtendLineModel extendLine = new ExtendLineModel();
                        //判断是否穿洞口
                        if (CheckService.CheckIntersectWithHols(ray, holes, out List<Polyline> intersectHoles, out Point3d interPt))
                        {
                            if (spt.DistanceTo(interPt) <= spt.DistanceTo(intersectPts[0]))  //穿洞
                            {
                                var movePts = MoveStartPoint(spt, lineVerticalDir, intersectHoles);
                                foreach (var mPt in movePts)
                                {
                                    var ajustPt = AjustStartPoint(lanes[i], spt, lineVerticalDir);
                                    if (ajustPt != null)
                                    {
                                        resLines.AddRange(ExtendLine(ajustPt.Value, extendDir, lanes, holes, i, preLane));
                                    }
                                }
                                return resLines;
                            }
                        }
                        else   //未穿洞
                        {
                            extendLine.line = new Line(spt, intersectPts[0]);
                            extendLine.priority = Priority.secondLevel;
                            extendLine.startLane = lanes[i];
                            resLines.Add(extendLine);
                            spt = intersectPts[0];
                        }
                        break;
                    }
                }
            }

            return resLines;
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
                ray.BasePoint = spt - dir * 5;
                ray.UnitDir = dir;
                ray.IntersectWith(line, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);
                if (intersectPts.Count > 0)
                {
                    return intersectPts[0];
                }

                ray.BasePoint = spt + dir * 5;
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
