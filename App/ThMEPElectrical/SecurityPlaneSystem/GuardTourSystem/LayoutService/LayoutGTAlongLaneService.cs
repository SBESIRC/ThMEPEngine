using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.Utls;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    public class LayoutGTAlongLaneService
    {
        double layoutStairDis = 6000;
        double layoutSpace = 15000;
        public void Layout(List<List<Line>> lanes, List<Polyline> columns, List<Point3d> stairPts)
        {
            List<(Point3d, Vector3d)> laneLayoutInfo = new List<(Point3d, Vector3d)>();
            foreach (var lane in lanes)
            {
                var separateCols = SeparateColumnsByLine(columns, lane, 6000);
                var upColumns = separateCols[0];
                var usefulPts = CalClosetStairPts(lane, stairPts);
                laneLayoutInfo.AddRange(CalLayoutPtInfo(lane, usefulPts, upColumns));
            }
        }

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="usefulPts"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private List<(Point3d, Vector3d)> CalLayoutPtInfo(List<Line> lane, List<Point3d> usefulPts, List<Polyline> columns)
        {
            CalLaneLayoutInfo(lane, out Point3d sPt, out Point3d ePt, out Vector3d dir);
            List<Point3d> allPts = usefulPts.Select(x => UtilService.GetProjectPtOnLane(lane, x)).ToList();
            allPts.Add(sPt);
            allPts.Add(ePt);
            var orderPts = UtilService.OrderPoints(allPts, dir);

            List<(Point3d, Vector3d)> layoutInfo = new List<(Point3d, Vector3d)>();
            for (int i = 0; i < orderPts.Count - 1; i++)
            {
                var p1 = orderPts[i];
                var p2 = orderPts[i + 1];
                var distance = p1.DistanceTo(p2);
                if (distance > layoutSpace)
                {
                    var num = Math.Floor(distance / layoutSpace);
                    var moveLength = distance / num;
                    var indexPt = p1;
                    for (int j = 0; j < num; j++)
                    {
                        indexPt = indexPt + moveLength * dir;
                        var closetCol = columns.OrderBy(x => x.Distance(indexPt)).First();
                        columns.Remove(closetCol);
                        var layoutPt = GetColumnLayoutPoint(closetCol, indexPt, dir);
                        if (layoutPt != null)
                        {
                            layoutInfo.Add(layoutPt.Value);
                        }
                    }
                }
            }

            return layoutInfo;
        }

        /// <summary>
        /// 计算柱上排布点和方向
        /// </summary>
        /// <param name="column"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private (Point3d, Vector3d)? GetColumnLayoutPoint(Polyline column, Point3d pt, Vector3d dir)
        {
            var layoutLine = GetLayoutStructLine(column, pt, dir, out Point3d closetPt);
            if (layoutLine == null)
            {
                return null;
            }

            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;

            //计算排布点
            var layoutPt = new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0);

            //计算排布方向
            var layoutDir = Vector3d.ZAxis.CrossProduct((ePt - sPt).GetNormal());
            var compareDir = (pt - layoutPt).GetNormal();
            if (layoutDir.DotProduct(compareDir) < 0)
            {
                layoutDir = -layoutDir;
            }

            return (layoutPt, layoutDir);
        }

        /// <summary>
        /// 找到构建的布置边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Line GetLayoutStructLine(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }

            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var layoutLine = lines.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
                }).FirstOrDefault();

            return layoutLine;
        }

        /// <summary>
        /// 计算车道线布置信息
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="sPt"></param>
        /// <param name="dir"></param>
        /// <param name="length"></param>
        private void CalLaneLayoutInfo(List<Line> lane, out Point3d sPt, out Point3d ePt, out Vector3d dir)
        {
            var maxLengthLane = lane.OrderByDescending(x => x.Length).First();
            var xDir = (maxLengthLane.EndPoint - maxLengthLane.StartPoint).GetNormal();
            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;

            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            var pts = lane.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint })
                .Select(x => x.TransformBy(matrix))
                .ToList();

            sPt = pts.OrderBy(x => x.X).First();
            ePt = pts.OrderByDescending(x => x.X).First();
            dir = (maxLengthLane.EndPoint - maxLengthLane.StartPoint).GetNormal();
        }

        /// <summary>
        /// 找到车到周边范围内楼梯间布置的的点位
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="stairPts"></param>
        /// <returns></returns>
        private List<Point3d> CalClosetStairPts(List<Line> lane, List<Point3d> stairPts)
        {
            return stairPts.Where(x => lane.Any(y => y.GetClosestPointTo(x, false).DistanceTo(x) < layoutStairDis)).ToList();
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private List<List<Polyline>> SeparateColumnsByLine(List<Polyline> columns, List<Line> lines, double length)
        {
            var newLines = lines.Select(x => x.Normalize()).ToList();
            List<Polyline> linePolys = new List<Polyline>();
            foreach (var line in newLines)
            {
                var bufferLength = length;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                if (Math.Abs(lineDir.X) > Math.Abs(lineDir.Y))
                {
                    if (lineDir.X < 0)
                    {
                        bufferLength = -bufferLength;
                    }
                }
                else
                {
                    if (lineDir.Y < 0)
                    {
                        bufferLength = -bufferLength;
                    }
                }
                linePolys.AddRange(new DBObjectCollection() { line }.SingleSidedBuffer(bufferLength).Cast<Polyline>().ToList());
            }

            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var col in columns)
            {
                var intersecRes = linePolys.Where(x => x.Contains(col) || x.Intersects(col)).ToList();
                if (intersecRes.Count > 0)
                {
                    upPolyline.Add(col);
                }
                else
                {
                    downPolyline.Add(col);
                }
            }

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }
    }
}
