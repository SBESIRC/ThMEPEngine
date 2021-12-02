using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.ConnectWiring.Service.ConnectFactory
{
    public class ConnectMethodService
    {
        double tol = 1;
        public Polyline CennectToPoint(Polyline wiring, BlockReference block, double range, List<Point3d> connectPts)
        {
            var blockPt = new Point3d(block.Position.X, block.Position.Y, 0);
            Circle circle = new Circle(blockPt, Vector3d.ZAxis, range);
            Point3dCollection pts = new Point3dCollection();
            wiring.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                var cutPoint = pts[0];
                var connectPt = connectPts.OrderBy(x => x.DistanceTo(cutPoint)).First();
                return UpdateWiring(wiring, connectPt, cutPoint, circle);
            }

            return wiring;
        }

        public Polyline ConnectByCircle(Polyline wiring, BlockReference block, double range)
        {
            Circle circle = new Circle(block.Position, Vector3d.ZAxis, range);
            Point3dCollection pts = new Point3dCollection();
            wiring.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                var cutPoint = pts[0];
                return UpdateWiring(wiring, cutPoint, circle);
            }

            return wiring;
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="connectPt"></param>
        /// <param name="turnPt"></param>
        /// <returns></returns>
        private Polyline UpdateWiring(Polyline wiring, Point3d connectPt, Point3d turnPt, Circle circle)
        {
            if (circle.IsContains(wiring.StartPoint))
            {
                wiring.ReverseCurve();
            }
            Polyline poly = new Polyline();
            for (int i = 0; i < wiring.NumberOfVertices - 1; i++)
            {
                Point3d pt = wiring.GetPoint3dAt(i);
                Line line = new Line(pt, wiring.GetPoint3dAt((i + 1) % wiring.NumberOfVertices));
                poly.AddVertexAt(i, pt.ToPoint2D(), 0, 0, 0);
                if (line.GetClosestPointTo(turnPt, false).DistanceTo(turnPt) < tol)
                {
                    poly.AddVertexAt(i + 1, turnPt.ToPoint2D(), 0, 0, 0);
                    poly.AddVertexAt(i + 2, connectPt.ToPoint2D(), 0, 0, 0);
                    break;
                }
            }

            return poly;
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="connectPt"></param>
        /// <param name="turnPt"></param>
        /// <returns></returns>
        private Polyline UpdateWiring(Polyline wiring, Point3d connectPt, Circle circle)
        {
            if (circle.IsContains(wiring.StartPoint))
            {
                wiring.ReverseCurve();
            }
            Polyline poly = new Polyline();
            for (int i = 0; i < wiring.NumberOfVertices - 1; i++)
            {
                Point3d pt = wiring.GetPoint3dAt(i);
                Line line = new Line(pt, wiring.GetPoint3dAt((i + 1) % wiring.NumberOfVertices));
                poly.AddVertexAt(i, pt.ToPoint2D(), 0, 0, 0);
                if (line.GetClosestPointTo(connectPt, false).DistanceTo(pt) < tol)
                {
                    poly.AddVertexAt(i + 1, connectPt.ToPoint2D(), 0, 0, 0);
                    break;
                }
            }

            return poly;
        }
    }
}
