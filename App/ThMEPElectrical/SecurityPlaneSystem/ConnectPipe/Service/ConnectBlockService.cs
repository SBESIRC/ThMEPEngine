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

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service
{
    public class ConnectBlockService
    {
        double tol = 5;
        double angle = (Math.PI / 180) * 45;
        /// <summary>
        /// 根据连接点调整连接线
        /// </summary>
        /// <param name="connectPts"></param>
        /// <param name="connectLine"></param>
        /// <returns></returns>
        public Polyline ConnectByPoint(List<Point3d> connectPts, Polyline connectLine, bool getClosetPt = false)
        {
            //var closetPt = connectPts.OrderBy(x => connectLine.StartPoint.DistanceTo(x)).First();
            //var closetPt = connectPts.OrderBy(x => connectLine.GetClosestPointTo(x, false).DistanceTo(x)).First();
            var allLines = connectLine.GetAllLinesInPolyline(false);
            var closetPt = connectPts.OrderBy(x => allLines.Select(y => y.GetClosestPointTo(x, false).DistanceTo(x)).Sum()).First();
            if (getClosetPt)
            {
                closetPt = connectPts.OrderBy(x => connectLine.StartPoint.DistanceTo(x)).First();
            }
            var lastPt = connectLine.GetPoint3dAt(connectLine.NumberOfVertices - 1);
            var polyPts = allLines.Select(x => x.GetClosestPointTo(closetPt, false));
            if (polyPts.Where(x => !x.IsEqualTo(lastPt, new Tolerance(1, 1))).Count() <= 0)
            {
                return connectLine;
            }
            var polyPt = polyPts.Where(x => !x.IsEqualTo(lastPt, new Tolerance(1, 1))).OrderBy(x => x.DistanceTo(closetPt)).First();
            var distance = polyPt.DistanceTo(closetPt);
            Polyline resPoly = null;
            if (distance < tol)
            {
                resPoly = CreateConnectPoly(connectLine, new List<Point3d>() { closetPt });
            }
            else
            {
                var dir = (polyPt - closetPt).GetNormal();
                var checkDir = (polyPt - lastPt).GetNormal();
                var rotateDir = dir.RotateBy(angle, Vector3d.ZAxis);
                if (rotateDir.DotProduct(checkDir) < 0)
                {
                    rotateDir = dir.RotateBy(-angle, Vector3d.ZAxis);
                }
                Ray ray = new Ray() { BasePoint = closetPt, UnitDir = rotateDir };
                var pts = new Point3dCollection();
                connectLine.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    resPoly = CreateConnectPoly(connectLine, new List<Point3d>() { pts[0], closetPt });
                }
                else
                {
                    resPoly = CreateConnectPoly(connectLine, new List<Point3d>() { closetPt }); ;
                }
            }
            return resPoly;
        }

        /// <summary>
        /// 根据外包圆调整连接线
        /// </summary>
        /// <param name="connectPts"></param>
        /// <param name="connectLine"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public Polyline ConnectByCircle(List<Point3d> connectPts, Polyline connectLine, double range)
        {
            var closetPt = connectPts.OrderBy(x => connectLine.GetClosestPointTo(x, false).DistanceTo(x)).First();
            var circle = new Circle(closetPt, Vector3d.ZAxis, range);
            var pts = new Point3dCollection();
            connectLine.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            Polyline resPoly = null;
            if (pts.Count > 0)
            {
                resPoly = CreateConnectPoly(connectLine, new List<Point3d>() { pts[0] });
            }
            else
            {
                resPoly = connectLine;
            }

            return resPoly;
        }

        /// <summary>
        /// 解决路径相交的问题
        /// </summary>
        public Polyline AjustPathIntersection(Polyline ePath, Polyline aPath, Point3d ePt, Point3d bPt, double moveVal)
        {
            if (ePath == null || aPath == null)
            {
                return ePath;
            }
            var ePathNum = ePath.NumberOfVertices;
            var aPathNum = aPath.NumberOfVertices;
            if(ePathNum < 2 || aPathNum < 2)
            {
                return ePath;
            }
            var eLine = new Line(ePath.GetPoint3dAt(ePathNum - 2), ePath.GetPoint3dAt(ePathNum - 1));
            var aLine = new Line(aPath.GetPoint3dAt(aPathNum - 2), aPath.GetPoint3dAt(aPathNum - 1));
            var eDir = (eLine.EndPoint - eLine.StartPoint).GetNormal();
            var aDir = (aLine.EndPoint - aLine.StartPoint).GetNormal();
            if (eDir.IsParallelTo(aDir, new Tolerance(0.01, 0.01)) && aLine.GetClosestPointTo(eLine.StartPoint, true).DistanceTo(eLine.StartPoint) < moveVal)
            {
                var checkDir = (ePt - bPt).GetNormal();
                var dir = Vector3d.ZAxis.CrossProduct(eDir);
                if (checkDir.DotProduct(dir) < 0)
                {
                    dir = -dir;
                }

                var pt1 = eLine.StartPoint + dir * moveVal;
                var pt2 = eLine.EndPoint + dir * moveVal;
                var pts = new List<Point3d>() { pt1, pt2 };
                return CreateAjustPath(ePath, pts);
            }

            return ePath;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="connectLine"></param>
        /// <param name="addPts"></param>
        /// <returns></returns>
        private Polyline CreateAjustPath(Polyline connectLine, List<Point3d> addPts)
        {
            Polyline resPoly = new Polyline();
            for (int i = 0; i < connectLine.NumberOfVertices - addPts.Count; i++)
            {
                resPoly.AddVertexAt(i, connectLine.GetPoint2dAt(i), 0, 0, 0);
            }
            var num = resPoly.NumberOfVertices;
            for (int i = 0; i < addPts.Count; i++)
            {
                resPoly.AddVertexAt(num + i, addPts[i].ToPoint2D(), 0, 0, 0);
            }

            return resPoly;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="connectLine"></param>
        /// <param name="addPts"></param>
        /// <returns></returns>
        private Polyline CreateConnectPoly(Polyline connectLine, List<Point3d> addPts)
        {
            Polyline resPoly = new Polyline();
            var pt = addPts.First();
            for (int i = 0; i < connectLine.NumberOfVertices - 1; i++)
            {
                resPoly.AddVertexAt(i, connectLine.GetPoint2dAt(i), 0, 0, 0);
                if (connectLine.GetLineSegment2dAt(i).ToCurve().GetClosestPointTo(pt, false).DistanceTo(pt) < 5)
                {
                    break;
                }
            }
            var num = resPoly.NumberOfVertices;
            for (int i = 0; i < addPts.Count; i++)
            {
                resPoly.AddVertexAt(num + i, addPts[i].ToPoint2D(), 0, 0, 0);
            }

            return resPoly;
        }
    }
}
