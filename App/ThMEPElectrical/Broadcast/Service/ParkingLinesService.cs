using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast
{
    public class ParkingLinesService
    {
        double pointTol = 1200;
        double tol = 0.9;

        public List<Line> CreateParkingLines(Polyline roomPoly, List<Curve> parkingLines)
        {
            List<Line> pLines = new List<Line>();
            foreach (var pline in parkingLines)
            {
                if (pline is Line line)
                {
                    pLines.Add(new Line(line.StartPoint, line.EndPoint));
                }
                else if (pline is Arc arc)
                {
                    Point3d ePt = arc.StartPoint;
                    Point3d sPt = arc.EndPoint;
                    Point3d centerPt = arc.Center;
                    Point3d middlePt = new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0);
                    Vector3d eDir = (ePt - centerPt).GetNormal();
                    Vector3d sDir = (sPt - centerPt).GetNormal();
                    Vector3d moveDir = (middlePt - centerPt).GetNormal();
                    double angle = sDir.GetAngleTo(eDir);
                    double length = ePt.DistanceTo(centerPt) / Math.Cos(angle / 2);

                    Point3d cPt = centerPt + length * moveDir;
                    pLines.Add(new Line(sPt, cPt));
                    pLines.Add(new Line(ePt, cPt));
                }
                else if (pline is Polyline polyline)
                {
                    for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                    {
                        pLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1)));
                    }
                }
            }

            var resLines = HandleLines(pLines);
            resLines = resLines.SelectMany(x => roomPoly.Trim(x).Cast<Polyline>().Select(y => new Line(y.StartPoint, y.EndPoint)).ToList()).ToList();
            //using (AcadDatabase acdb = AcadDatabase.Active())
            //{
            //    foreach (var item in resLines)
            //    {
            //        acdb.ModelSpace.Add(item);
            //    }
            //}

            return pLines;
        }

        /// <summary>
        /// 将车道线分成主车道线和副车道线
        /// </summary>
        /// <param name="roomPoly"></param>
        /// <param name="parkingLines"></param>
        /// <param name="otherPLins"></param>
        /// <returns></returns>
        public List<List<Line>> CreateParkingLines(Polyline roomPoly, List<Line> parkingLines, out List<List<Line>> otherPLins)
        {
            parkingLines = parkingLines.SelectMany(x => roomPoly.Trim(x).Cast<Polyline>().Select(y => new Line(y.StartPoint, y.EndPoint))).ToList();
            var pLines = ClassifyParkingLines(parkingLines);
            var xPLines = pLines[0];
            var yPLines = pLines[1];
            var resLines = HandleLinesByDirection(xPLines);
            otherPLins = HandleLinesByDirection(yPLines);

            return resLines;
        }

        /// <summary>
        /// 将车道线分成主车道线和副车道线
        /// </summary>
        /// <param name="roomPoly"></param>
        /// <param name="parkingLines"></param>
        /// <param name="otherPLins"></param>
        /// <returns></returns>
        public List<List<Line>> CreateNodedParkingLines(Polyline roomPoly, List<Line> parkingLines, out List<List<Line>> otherPLins)
        {
            parkingLines = parkingLines.SelectMany(x => roomPoly.Trim(x).Cast<Polyline>().Select(y => new Line(y.StartPoint, y.EndPoint))).ToList();
            var objs = new DBObjectCollection();
            parkingLines.ForEach(x => objs.Add(x));
            var handleLines = objs.ToNTSNodedLineStrings().ToDbObjects()
                .SelectMany(x => {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .ToList();
            var pLines = ClassifyParkingLines(handleLines);
            var xPLines = pLines[0];
            var yPLines = pLines[1];
            var resLines = HandleLinesByDirection(xPLines);
            otherPLins = HandleLinesByDirection(yPLines);

            return resLines;
        }

        /// <summary>
        /// 根据x方向和y方向分类车位线
        /// </summary>
        /// <param name="parkingLines"></param>
        /// <returns></returns>
        public List<List<Line>> ClassifyParkingLines(List<Line> parkingLines)
        {
            List<Line> yPLines = new List<Line>();
            List<Line> xPLines = new List<Line>();
            foreach (var pLine in parkingLines)
            {
                Vector3d pDir = (pLine.EndPoint - pLine.StartPoint).GetNormal();
                double yDotValue = pDir.DotProduct(Vector3d.YAxis);
                double xDotValue = pDir.DotProduct(Vector3d.XAxis);
                if (Math.Abs(yDotValue) > Math.Abs(xDotValue))
                {
                    yPLines.Add(pLine);
                }
                else
                {
                    xPLines.Add(pLine);
                }
            }

            return new List<List<Line>>() { xPLines, yPLines };
        }

        /// <summary>
        /// 找到首尾相接的线
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public List<List<Line>> HandleLinesByDirection(List<Line> lines)
        {
            List<List<Line>> resLines = new List<List<Line>>();
            while (lines.Count > 0)
            {
                List<Line> matchLines = new List<Line>();
                Line firLine = lines.First();
                lines.Remove(firLine);
                matchLines.Add(firLine);

                Point3d sPt = firLine.StartPoint;
                Point3d ePt = firLine.EndPoint;
                while (true)
                {
                    var sMLine = lines.FirstOrDefault(x => x.StartPoint.IsEqualTo(sPt, new Tolerance(1, 1)));
                    if (sMLine != null)
                    {
                        matchLines.Add(sMLine);
                        lines.Remove(sMLine);
                        sPt = sMLine.EndPoint;
                    }

                    var eMLine = lines.FirstOrDefault(x => x.EndPoint.IsEqualTo(sPt, new Tolerance(1, 1)));
                    if (eMLine != null)
                    {
                        matchLines.Add(eMLine);
                        lines.Remove(eMLine);
                        sPt = eMLine.StartPoint;
                    }

                    if (sMLine == null && eMLine == null)
                    {
                        break;
                    }
                }

                while (true)
                {
                    var sMLine = lines.FirstOrDefault(x => x.StartPoint.IsEqualTo(ePt, new Tolerance(1, 1)));
                    if (sMLine != null)
                    {
                        matchLines.Add(sMLine);
                        lines.Remove(sMLine);
                        ePt = sMLine.EndPoint;
                    }

                    var eMLine = lines.FirstOrDefault(x => x.EndPoint.IsEqualTo(ePt, new Tolerance(1, 1)));
                    if (eMLine != null)
                    {
                        matchLines.Add(eMLine);
                        lines.Remove(eMLine);
                        ePt = eMLine.StartPoint;
                    }

                    if (sMLine == null && eMLine == null)
                    {
                        break;
                    }
                }
                resLines.Add(matchLines);
            }

            return resLines;
        }

        /// <summary>
        /// 处理车道线
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public List<Line> HandleLines(List<Line> lines)
        {
            List<Line> resLines = new List<Line>();
            foreach (var line in lines)
            {
                var otherlines = lines.Where(x => !(x.StartPoint.IsEqualTo(line.StartPoint) && x.EndPoint.IsEqualTo(line.EndPoint)))
                    .Where(x =>
                    {
                        var p1 = x.GetClosestPointTo(line.StartPoint, false);
                        var p2 = x.GetClosestPointTo(line.EndPoint, false);
                        if (p1.DistanceTo(line.StartPoint) < pointTol || p2.DistanceTo(line.EndPoint) < pointTol)
                        {
                            return true;
                        }
                        return false;
                    })
                    .Where(x => Math.Abs(line.Delta.GetNormal().DotProduct(x.Delta.GetNormal())) > tol)
                    .OrderByDescending(x => x.Length)
                    .ToList();
                if (otherlines.Count > 0)
                {
                    var firLine = otherlines.First();
                    if (firLine.Length >= line.Length)
                    {
                        Point3d sp = firLine.GetClosestPointTo(line.StartPoint, true);
                        Point3d ep = firLine.GetClosestPointTo(line.EndPoint, true);
                        resLines.Add(new Line(sp, ep));
                    }
                    else
                    {
                        resLines.Add(line);
                    }
                }
                else
                {
                    resLines.Add(line);
                }
            }

            return resLines;
        }

        /// <summary>
        /// 按首尾相接的顺序将车道线添加到集合中
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public List<Line> HandleParkingLines(List<Line> lines, out Point3d sPt, out Point3d ePt)
        {
            var xDir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            List<Point3d> allPts = lines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }).Select(x => x.TransformBy(matrix)).ToList();
            sPt = allPts.OrderBy(x => x.X).First().TransformBy(matrix.Inverse());
            ePt = allPts.OrderByDescending(x => x.X).First().TransformBy(matrix.Inverse()); ;

            var handleLines = new List<Line>(lines);
            Point3d comparePt = sPt;
            List<Line> resLines = new List<Line>();
            while (handleLines.Count > 0)
            {
                var matchLine = handleLines.Where(x => x.StartPoint.IsEqualTo(comparePt) || x.EndPoint.IsEqualTo(comparePt)).FirstOrDefault();
                if (matchLine == null)
                {
                    break;
                }

                comparePt = matchLine.StartPoint.IsEqualTo(comparePt) ? matchLine.EndPoint : matchLine.StartPoint;
                handleLines.Remove(matchLine);
                resLines.Add(matchLine);
            }

            return resLines;
        }

        /// <summary>
        /// 将线调整为从起点按顺着的方向走下去
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="sPt"></param>
        /// <returns></returns>
        public List<Line> HandleParkingLinesDir(List<Line> lines, Point3d sPt)
        {
            var handleLines = new List<Line>(lines);
            Point3d comparePt = sPt;
            List<Line> resLines = new List<Line>();
            while (handleLines.Count > 0)
            {
                var matchLine = handleLines.Where(x => x.StartPoint.IsEqualTo(comparePt, new Tolerance(1, 1)) || x.EndPoint.IsEqualTo(comparePt, new Tolerance(1, 1))).FirstOrDefault();
                if (matchLine == null)
                {
                    break;
                }

                handleLines.Remove(matchLine);
                if (matchLine.EndPoint.IsEqualTo(comparePt))
                {
                    matchLine = new Line(matchLine.EndPoint, matchLine.StartPoint);
                }
                comparePt = matchLine.EndPoint;
                resLines.Add(matchLine);
            }

            return resLines;
        }
    }
}
