using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public static class OptimizeStartExtendLineService
    {
        public static Polyline CreateMapFrame(Line lane, Point3d startPt, double expandLength)
        {
            Vector3d xDir = (lane.EndPoint - lane.StartPoint).GetNormal();
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            Line cloneLine = lane.Clone() as Line;
            cloneLine.TransformBy(matrix);
            Point3d transPt = startPt.TransformBy(matrix);

            List<Point3d> pts = new List<Point3d>() { transPt, cloneLine.StartPoint, cloneLine.EndPoint };
            var polyline = GetBoungdingBox(pts).Buffer(expandLength)[0] as Polyline;
            polyline.TransformBy(matrix.Inverse());

            return polyline;
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private static Polyline GetBoungdingBox(List<Point3d> points)
        {
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

            return polyline;
        }
    }
}
