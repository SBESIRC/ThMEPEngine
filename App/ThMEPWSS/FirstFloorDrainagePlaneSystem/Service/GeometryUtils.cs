﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class GeometryUtils
    {
        /// <summary>
        /// 将长边外扩（或内缩）一定距离。tips：仅支持矩形
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="tol"></param>
        public static Polyline ExtendByLengthLine(this Polyline polyline, double tol, bool longEdge = true)
        {
            var allLines = StructGeoService.GetAllLineByPolyline(polyline).OrderBy(x => x.Length).ToList();
            if (!longEdge)
            {
                allLines = StructGeoService.GetAllLineByPolyline(polyline).OrderByDescending(x => x.Length).ToList();
            }
            var firLine = allLines[0];
            var lastLine = allLines[1];
            var firExtendLine = ExtendLine(firLine, tol);
            var lastExtendLine = ExtendLine(lastLine, tol);

            Polyline resPoly = new Polyline() { Closed = true };
            resPoly.AddVertexAt(0, firExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(1, firExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(2, lastExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(3, lastExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 两边延申线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static Line ExtendLine(Line line, double tol)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var sPt = line.StartPoint - dir * tol;
            var ePt = line.EndPoint + dir * tol;
            return new Line(sPt, ePt);
        }

        /// <summary>
        /// 从集合中找到所有连接线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Line> GetConenctLine(List<Line> lines, Line line, double tol = 1)
        {
            var resLines = new List<Line>();
            var allLines = new List<Line>(lines);
            var connectLines = lines.Where(x => x.DistanceTo(line.StartPoint, false) < tol ||
                 x.DistanceTo(line.EndPoint, false) < tol ||
                 line.DistanceTo(x.StartPoint, false) < tol ||
                 line.DistanceTo(x.EndPoint, false) < tol).ToList();
            resLines.AddRange(connectLines);
            allLines = allLines.Except(connectLines).ToList();
            var resConnectLines = new List<Line>();
            foreach (var rLine in resLines)
            {
                resConnectLines.AddRange(GetConenctLine(allLines, rLine, tol));
            }
            resLines.AddRange(resConnectLines);
            return resLines;
        }

        /// <summary>
        /// 找到距离点最近的线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        public static KeyValuePair<Line, Point3d> GetClosetLine(List<Line> lines, Point3d startPt)
        {
            var lanePtInfo = lines.ToDictionary(x => x, y => y.GetClosestPointTo(startPt, false))
                .OrderBy(x => x.Value.DistanceTo(startPt))
                .First();

            return lanePtInfo;
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Polyline GetBoungdingBox(List<Point3d> points, Vector3d xDir)
        {
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });
            points = points.Select(x => x.TransformBy(matrix.Inverse())).ToList();

            points = points.OrderBy(x => x.X).ToList();
            double minX = points.First().X;
            double maxX = points.Last().X;
            points = points.OrderBy(x => x.Y).ToList();
            double minY = points.First().Y;
            double maxY = points.Last().Y;

            Point2d pt1 = new Point2d(minX, minY);
            Point2d pt2 = new Point2d(minX, maxY);
            Point2d pt3 = new Point2d(maxX, maxY);
            Point2d pt4 = new Point2d(maxX, minY);
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, pt1, 0, 0, 0);
            polyline.AddVertexAt(1, pt2, 0, 0, 0);
            polyline.AddVertexAt(2, pt3, 0, 0, 0);
            polyline.AddVertexAt(3, pt4, 0, 0, 0);
            polyline.TransformBy(matrix);

            return polyline;
        }

        /// <summary>
        /// 获取polyline所有点
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> GetAllPolylinePts(Polyline polyline)
        {
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allPts.Add(polyline.GetPoint3dAt(i));
            }

            return allPts;
        }

        /// <summary>
        /// 以一个点为中心点创建polyline
        /// </summary>
        /// <param name="point"></param>
        /// <param name="length"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Polyline CreatePolylineByPt(this Point3d point, double length, Vector3d dir)
        {
            var otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var pt1 = point + dir * length + otherDir * length;
            var pt2 = point - dir * length + otherDir * length;
            var pt3 = point - dir * length - otherDir * length;
            var pt4 = point + dir * length - otherDir * length;
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }

        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lines, Point3d startPt, Polyline polyline, double step)
        {
            var closeInfo = GeometryUtils.GetClosetLine(lines, startPt);
            Line checkLine = new Line(startPt, closeInfo.Value);
            if (!CheckService.CheckIntersectWithFrame(checkLine, polyline))
            {
                var checkDir = (closeInfo.Value - startPt).GetNormal();
                var lineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (checkDir.IsEqualTo(lineDir, new Tolerance(0.001, 0.001)))
                {
                    return closeInfo;
                }
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(step);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lines, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }

        /// <summary>
        /// 寻找出户的框线
        /// </summary>
        /// <returns></returns>
        public static Polyline FindOutFrame(Polyline polyline, List<Polyline> outrFrames, Point3d startPosition, bool lastFrame = true)
        {
            var frames = outrFrames.Where(x => polyline.IsIntersects(x)).ToList();
            var ep = polyline.EndPoint;
            if (startPosition.DistanceTo(ep) < 1)
            {
                ep = polyline.StartPoint;
            }
            
            if (lastFrame)
            {
                return frames.OrderBy(x => x.DistanceTo(ep, false)).First();
            }
            else
            {
                return frames.OrderByDescending(x => x.DistanceTo(ep, false)).First();
            }
        }

        /// <summary>
        /// 找到相交段的线
        /// </summary>
        /// <param name="routePoly"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Line FindRouteIntersectLine(Polyline routePoly, Polyline frame)
        {
            var allLines = routePoly.GetAllLineByPolyline();
            var interLines = allLines.Where(x => x.IsIntersects(frame)).ToList();
            var dir = StructGeoService.GetPolylineDir(frame);
            interLines = interLines.OrderBy(x => Math.Abs((x.EndPoint - x.StartPoint).GetNormal().DotProduct(dir))).ToList();
            return interLines.FirstOrDefault();
        }

        /// <summary>
        /// 用polyline 上的line打断polyline，并找到起点部分的被打断的polyline
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static Polyline GetBreakLine(Polyline poly, Point3d sp, Point3d ep)
        {
            Polyline breakPoly1 = new Polyline();
            breakPoly1.AddVertexAt(breakPoly1.NumberOfVertices, poly.GetPoint3dAt(0).ToPoint2D(), 0, 0, 0);
            Polyline breakPoly2 = new Polyline();
            bool isBreak = false;
            for (int i = 1; i < poly.NumberOfVertices; i++)
            {
                if (!isBreak)
                {
                    var line = new Line(poly.GetPoint3dAt(i - 1), poly.GetPoint3dAt(i));
                    if (line.GetClosestPointTo(ep, false).DistanceTo(ep) < 0.01)
                    {
                        isBreak = true;
                        breakPoly1.AddVertexAt(breakPoly1.NumberOfVertices, ep.ToPoint2D(), 0, 0, 0);
                        breakPoly2.AddVertexAt(breakPoly2.NumberOfVertices, ep.ToPoint2D(), 0, 0, 0);
                    }
                    else
                    {
                        breakPoly1.AddVertexAt(breakPoly1.NumberOfVertices, poly.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
                    }
                }
                else
                {
                    breakPoly2.AddVertexAt(breakPoly2.NumberOfVertices, poly.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
                }
            }

            if (breakPoly2.NumberOfVertices <= 0)
            {
                return breakPoly1;
            }
            return breakPoly1.GetClosestPointTo(sp, false).DistanceTo(sp) < breakPoly2.GetClosestPointTo(sp, false).DistanceTo(sp) ? breakPoly1 : breakPoly2;
        }
    }
}