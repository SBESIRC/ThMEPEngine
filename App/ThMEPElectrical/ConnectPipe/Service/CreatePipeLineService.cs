using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.ConnectPipe.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class CreatePipeLineService
    {
        readonly double tol = 1000;
        readonly double checkTol = 250;
        readonly double moveLineDis = 150;
        public List<Polyline> CreatePipe(List<Polyline> connectPolys, List<BlockReference> broadcasts)
        {
            //计算小支管信息
            Dictionary<Polyline, List<Polyline>> connectPtInfo = new Dictionary<Polyline, List<Polyline>>();
            List<BroadcastModel> broadcastModel = broadcasts.Select(x => new BroadcastModel(x)).ToList();
            foreach (var broadcast in broadcastModel)
            {
                var polys = connectPolys.Where(x => x.StartPoint.IsEqualTo(broadcast.Position, new Tolerance(1, 1))
                    || x.EndPoint.IsEqualTo(broadcast.Position, new Tolerance(1, 1))).ToList();
                CreateConnectPipe(polys, broadcast, connectPtInfo);
            }

            connectPtInfo = connectPtInfo.Where(x => x.Value.Count == 2).ToDictionary(x => x.Key, y => y.Value);
            //将广播全部连接起来
            var resPolys = ConnectOfftake(connectPtInfo);

            //处理有相交的连接线
            resPolys = CorrectIntersectPipeLine(resPolys, 200);

            return resPolys;
        }

        /// <summary>
        /// 计算连接信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="broadcast"></param>
        /// <param name="connectPtInfo"></param>
        private void CreateConnectPipe(List<Polyline> polylines, BroadcastModel broadcast, Dictionary<Polyline, List<Polyline>> connectPtInfo)
        {
            var handlePolys = polylines.ToDictionary(x => x, y => GeUtils.HandleConnectPolys(y))
                .OrderBy(x => x.Value.NumberOfVertices)
                .Select(x => x.Key)
                .ToList();
            foreach (var poly in handlePolys)
            {
                var resPoly = CreateOfftake(poly, broadcast);
                if (connectPtInfo.Keys.Contains(poly))
                {
                    connectPtInfo[poly].Add(resPoly);
                }
                else
                {
                    connectPtInfo.Add(poly, new List<Polyline>() { resPoly });
                }
            }
        }

        /// <summary>
        /// 计算支管连接
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        private Polyline CreateOfftake(Polyline poly, BroadcastModel broadcast)
        {
            if (poly.NumberOfVertices <= 2)
            {
                return CreateOfftakeByDirectConnection(poly, broadcast);
            }
            else
            {
                return CreateOfftakeByPolyline(poly, broadcast);
            }
        }

        /// <summary>
        /// 计算带小支管连接线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        private Polyline CreateOfftakeByPolyline(Polyline polyline, BroadcastModel broadcast)
        {
            var handleLine = GeUtils.HandleConnectPolys(polyline);
            var longestLine = GeUtils.CalLongestLineByPoly(new List<Polyline>() { handleLine }).Values.First();
            List<Point3d> pts = new List<Point3d>() { longestLine.StartPoint, longestLine.EndPoint };
            pts = pts.OrderBy(x => x.DistanceTo(broadcast.Position)).ToList();

            var connectPt = CalConenctPt(new Line(pts.First(), pts.Last()), broadcast);
            double distance = connectPt.DistanceTo(longestLine.GetClosestPointTo(connectPt, true));
            var connectPolys = broadcast.ConnectInfo[connectPt];
            Polyline resPoly = new Polyline();
            if (distance > tol)
            {
                if (connectPolys.Count > 0)
                {
                    var dir = (pts.Last() - pts.First()).GetNormal();
                    Line line = new Line(connectPt + dir * moveLineDis, pts.First() + dir * moveLineDis);
                    var resPt = ConenctAnglePt(line, connectPt, Math.Cos(Math.PI * (20.0 / 180)));
                    resPoly.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                    resPoly.AddVertexAt(1, resPt.ToPoint2D(), 0, 0, 0);
                    resPoly.AddVertexAt(2, line.EndPoint.ToPoint2D(), 0, 0, 0);
                }
                else
                {
                    var resPt = ConenctAnglePt(new Line(pts.First(), pts.Last()), connectPt, Math.Cos(Math.PI * (20.0 / 180)));
                    resPoly.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                    resPoly.AddVertexAt(1, resPt.ToPoint2D(), 0, 0, 0);
                }
            }
            else
            {
                var resPt = ConenctAnglePt(new Line(pts.First(), pts.Last()), connectPt, Math.Cos(Math.PI * (20.0 / 180)));
                resPoly.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                resPoly.AddVertexAt(1, resPt.ToPoint2D(), 0, 0, 0);
            }

            //记录连接信息
            broadcast.ConnectInfo[connectPt].Add(resPoly);
            return resPoly;
        }

        /// <summary>
        /// 计算小支管与连接点的相交点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="connectPt"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private Point3d ConenctAnglePt(Line line, Point3d connectPt, double ratio)
        {
            double radius = line.StartPoint.DistanceTo(connectPt) / ratio;
            Circle circle = new Circle(connectPt, Vector3d.ZAxis, radius);
            return line.IntersectWithEx(circle)[0];
        }

        /// <summary>
        /// 确定连接点
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        private Point3d CalConenctPt(Line line, BroadcastModel broadcast)
        {
            List<Point3d> connectPts = new List<Point3d>() { broadcast.RightConnectPt, broadcast.LeftConnectPt };
            var rightDis = line.GetClosestPointTo(broadcast.RightConnectPt, false).DistanceTo(broadcast.RightConnectPt);
            var leftDis = line.GetClosestPointTo(broadcast.LeftConnectPt, false).DistanceTo(broadcast.LeftConnectPt);
            var connectPt = rightDis > leftDis ? broadcast.LeftConnectPt : broadcast.RightConnectPt;
            var minDis = rightDis > leftDis ? leftDis : rightDis;
            if (Math.Abs(rightDis - leftDis) < 10)
            {
                connectPt = connectPts.OrderBy(x => x.DistanceTo(line.EndPoint)).First();
            }

            if (broadcast.Position.DistanceTo(line.GetClosestPointTo(broadcast.Position, true)) > checkTol)
            {
                var topDis = line.GetClosestPointTo(broadcast.TopConnectPt, true).DistanceTo(broadcast.TopConnectPt);
                if (topDis < minDis)
                {
                    connectPt = broadcast.TopConnectPt;
                }
            }

            return connectPt;
        }

        /// <summary>
        /// 计算直接连接线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        private Polyline CreateOfftakeByDirectConnection(Polyline polyline, BroadcastModel broadcast)
        {
            List<Point3d> pts = new List<Point3d>() { polyline.StartPoint, polyline.EndPoint };
            pts = pts.OrderBy(x => x.DistanceTo(broadcast.Position)).ToList();
            List<Point3d> connectPts = new List<Point3d>() { broadcast.LeftConnectPt, broadcast.RightConnectPt, broadcast.TopConnectPt };
            Point3d connectPt = connectPts.OrderBy(x => x.DistanceTo(pts.Last())).First();

            var movePt = connectPt + (pts.Last() - pts.First()).GetNormal() * 1;
            Polyline resPoly = new Polyline();
            resPoly.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(1, movePt.ToPoint2D(), 0, 0, 0);

            //记录连接信息
            broadcast.ConnectInfo[connectPt].Add(resPoly);

            return resPoly;
        }

        /// <summary>
        /// 连接广播小支管
        /// </summary>
        /// <param name="connectPtInfo"></param>
        /// <returns></returns>
        private List<Polyline> ConnectOfftake(Dictionary<Polyline, List<Polyline>> connectPtInfo)
        {
            List<Polyline> resPolys = new List<Polyline>();
            foreach (var dicInfo in connectPtInfo)
            {
                Polyline polyline = new Polyline();
                var firOfftake = dicInfo.Value.First();
                var lastOfftake = dicInfo.Value.Last();
                for (int i = 0; i < firOfftake.NumberOfVertices; i++)
                {
                    polyline.AddVertexAt(i, firOfftake.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
                }

                for (int i = 0; i < lastOfftake.NumberOfVertices; i++)
                {
                    polyline.AddVertexAt(i + firOfftake.NumberOfVertices, lastOfftake.GetPoint3dAt(lastOfftake.NumberOfVertices - i - 1).ToPoint2D(), 0, 0, 0);
                }
                resPolys.Add(polyline);
            }

            return resPolys;
        }

        /// <summary>
        /// 处理相交的连接线
        /// </summary>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private List<Polyline> CorrectIntersectPipeLine(List<Polyline> polylines, double length)
        {
            List<Polyline> resPolys = new List<Polyline>();
            polylines = polylines.OrderBy(x => GeUtils.HandleConnectPolys(x).NumberOfVertices).ToList();
            while (polylines.Count > 0)
            {
                var firPoly = polylines.First();
                polylines.Remove(firPoly);
                //找到相交线
                var intersectPolys = polylines
                    .ToDictionary(
                    x => x,
                    y =>
                    {
                        List<Point3d> pts = new List<Point3d>();
                        foreach (Point3d pt in y.IntersectWithEx(firPoly))
                        {
                            if (pt.DistanceTo(y.StartPoint) > 1 && pt.DistanceTo(y.EndPoint) > 1)
                            {
                                pts.Add(pt);
                            }
                        }
                        return pts;
                    })
                    .Where(x => x.Value.Count > 0)
                    .ToDictionary(x => x.Key, y => y.Value.First());

                if (intersectPolys.Count > 0)
                {
                    Point3d connectPt = firPoly.StartPoint.DistanceTo(intersectPolys.First().Value) < firPoly.EndPoint.DistanceTo(intersectPolys.First().Value)
                            ? firPoly.StartPoint : firPoly.EndPoint;
                    var interPoly = intersectPolys.OrderByDescending(x => x.Value.DistanceTo(connectPt)).First();
                    var distance = interPoly.Value.DistanceTo(connectPt) + length;

                    List<Polyline> polys = new List<Polyline>(intersectPolys.Select(x => x.Key));
                    polys.Add(firPoly);

                    Circle circle = new Circle(connectPt, Vector3d.ZAxis, distance);
                    Point3dCollection pts = new Point3dCollection();
                    circle.IntersectWith(firPoly, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                    if (pts.Count > 0)
                    {
                        var longestLine = GeUtils.CalIntersectPtPoly(polys, pts[0]);
                        firPoly = CreateNewConnectLine(firPoly, longestLine[firPoly], pts[0], connectPt, false);
                    }
                    foreach (var polyInfo in intersectPolys)
                    {
                        pts.Clear();
                        circle.IntersectWith(polyInfo.Key, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                        if (pts.Count > 0)
                        {
                            var longestLine = GeUtils.CalIntersectPtPoly(polys, pts[0]);
                            var secPoly = CreateNewConnectLine(polyInfo.Key, longestLine[polyInfo.Key], pts[0], connectPt, false);
                            polylines.Add(secPoly);
                            polylines.Remove(polyInfo.Key);
                        }
                    }
                    polylines.Add(firPoly);
                }
                else
                {
                    resPolys.Add(firPoly);
                }
            }

            return resPolys;
        }

        /// <summary>
        /// 处理相交的连接线
        /// </summary>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private List<Polyline> CorrectIntersectPipeLine(List<Polyline> polylines)
        {
            List<Polyline> resPolys = new List<Polyline>();
            polylines = polylines.OrderBy(x => GeUtils.HandleConnectPolys(x).NumberOfVertices).ToList();
            while (polylines.Count > 0)
            {
                var firPoly = polylines.First();
                polylines.Remove(firPoly);
                //找到相交线
                var intersectPolys = polylines
                    .ToDictionary(
                    x => x,
                    y =>
                    {
                        List<Point3d> pts = new List<Point3d>();
                        foreach (Point3d pt in y.IntersectWithEx(firPoly))
                        {
                            if (pt.DistanceTo(y.StartPoint) > 1 && pt.DistanceTo(y.EndPoint) > 1)
                            {
                                pts.Add(pt);
                            }
                        }
                        return pts;
                    })
                    .Where(x => x.Value.Count > 0)
                    .ToDictionary(x => x.Key, y => y.Value.First());

                if (intersectPolys.Count > 0)
                {
                    var intersectPoly = intersectPolys.First();
                    polylines.Remove(intersectPoly.Key);

                    List<Polyline> polys = new List<Polyline>() { firPoly, intersectPoly.Key };
                    var longestLine = GeUtils.CalLongestLineByPoly(polys);
                    bool firBool = GeUtils.IsPointOnLine(longestLine[firPoly], intersectPoly.Value);
                    bool interBool = GeUtils.IsPointOnLine(longestLine[intersectPoly.Key], intersectPoly.Value);
                    var secPoly = intersectPoly.Key;
                    if (firBool && interBool)
                    {
                        Point3d connectPt = firPoly.StartPoint.DistanceTo(intersectPoly.Value) < firPoly.EndPoint.DistanceTo(intersectPoly.Value)
                            ? firPoly.StartPoint : firPoly.EndPoint;
                        firPoly = CreateNewConnectLine(firPoly, longestLine[firPoly], intersectPoly.Value, connectPt, false);
                        secPoly = CreateNewConnectLine(intersectPoly.Key, longestLine[intersectPoly.Key], intersectPoly.Value, connectPt, true);
                    }
                    else if (firBool)
                    {
                        Point3d connectPt = intersectPoly.Key.StartPoint.DistanceTo(intersectPoly.Value) < intersectPoly.Key.EndPoint.DistanceTo(intersectPoly.Value)
                            ? intersectPoly.Key.StartPoint : intersectPoly.Key.EndPoint;
                        firPoly = CreateNewConnectLine(firPoly, longestLine[firPoly], intersectPoly.Value, connectPt, true);
                    }
                    else if (interBool)
                    {
                        Point3d connectPt = firPoly.StartPoint.DistanceTo(intersectPoly.Value) < firPoly.EndPoint.DistanceTo(intersectPoly.Value)
                            ? firPoly.StartPoint : firPoly.EndPoint;
                        secPoly = CreateNewConnectLine(intersectPoly.Key, longestLine[intersectPoly.Key], intersectPoly.Value, connectPt, true);
                    }
                    else
                    {
                        Point3d connectPt = firPoly.StartPoint.DistanceTo(intersectPoly.Value) < firPoly.EndPoint.DistanceTo(intersectPoly.Value)
                            ? firPoly.StartPoint : firPoly.EndPoint;
                        secPoly = AdjuestConnectPtPolyline(intersectPoly.Key, connectPt);
                    }
                    resPolys.Add(secPoly);
                }
                resPolys.Add(firPoly);
            }

            return resPolys;
        }

        /// <summary>
        /// 创建新的调整后的连接线
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="longetLine"></param>
        /// <param name="pt"></param>
        /// <param name="connectPt"></param>
        /// <returns></returns>
        private Polyline CreateNewConnectLine(Polyline poly, Line longetLine, Point3d pt, Point3d connectPt, bool isMove)
        {
            if (poly.Length - longetLine.Length < 10)
            {
                pt = connectPt;
            }
            Point3d movePt = longetLine.StartPoint.DistanceTo(pt) < longetLine.EndPoint.DistanceTo(pt) ? longetLine.StartPoint : longetLine.EndPoint;
            Point3d moveConnectPt = poly.StartPoint.DistanceTo(connectPt) < poly.EndPoint.DistanceTo(connectPt) ? poly.StartPoint : poly.EndPoint;

            if (isMove)
            {
                Point3d endPt = longetLine.StartPoint.DistanceTo(pt) < longetLine.EndPoint.DistanceTo(pt) ? longetLine.EndPoint : longetLine.StartPoint;
                Vector3d moveDir = (endPt - movePt).GetNormal();
                pt = pt + moveDir * 100;
            }
            List<Point3d> pts = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var polyPt = poly.GetPoint3dAt(i);
                if (polyPt.IsEqualTo(movePt, new Tolerance(5, 5)))
                {
                    polyPt = pt;
                }
                if (polyPt.IsEqualTo(moveConnectPt, new Tolerance(5, 5)))
                {
                    polyPt = connectPt;
                }
                if (!pts.Any(x => x.IsEqualTo(polyPt, new Tolerance(1, 1))))
                {
                    pts.Add(polyPt);
                }
            }

            Polyline polyline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                polyline.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return polyline;
        }

        /// <summary>
        /// 调整连接点重新创建连接线
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="connectPt"></param>
        private Polyline AdjuestConnectPtPolyline(Polyline poly, Point3d connectPt)
        {
            Point3d moveConnectPt = poly.StartPoint.DistanceTo(connectPt) < poly.EndPoint.DistanceTo(connectPt) ? poly.StartPoint : poly.EndPoint;
            List<Point3d> pts = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var polyPt = poly.GetPoint3dAt(i);
                if (polyPt.IsEqualTo(moveConnectPt))
                {
                    polyPt = connectPt;
                }
                pts.Add(polyPt);
            }

            Polyline polyline = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                polyline.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return polyline;
        }
    }
}
