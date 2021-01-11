using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public static class BlockUtils
    {
        /// <summary>
        /// 获取移动信息
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcasts"></param>
        /// <param name="distance"></param>
        /// <param name="dir"></param>
        public static void GetLaneMoveInfo(Polyline polyline, List<BlockReference> blocks, out double distance, out Vector3d dir)
        {
            List<KeyValuePair<Point3d, Point3d>> ptInfo = new List<KeyValuePair<Point3d, Point3d>>();
            foreach (var broadcast in blocks)
            {
                var closetPt = polyline.GetClosestPointTo(broadcast.Position, false);
                ptInfo.Add(new KeyValuePair<Point3d, Point3d>(broadcast.Position, closetPt));
            }

            distance = ptInfo.Select(x => x.Key.DistanceTo(x.Value))
                .OrderBy(x => x).GroupBy(x => Math.Floor(x / 10))
                .OrderByDescending(x => x.Count())
                .First()
                .Key * 10;
            dir = blocks.Select(x => x.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal()).First();
        }

        /// <summary>
        /// 移动polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="dir"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Polyline movePolyline(Polyline polyline, Vector3d dir, double distance)
        {
            var newPolyline = polyline.GetOffsetCurves(distance)[0] as Polyline;
            var polyDir = (newPolyline.StartPoint - newPolyline.EndPoint).GetNormal();
            if (polyDir.DotProduct(dir) < 0)
            {
                newPolyline = polyline.GetOffsetCurves(-distance)[0] as Polyline;
            }

            return newPolyline;
        }

        /// <summary>
        /// 计算块在线上的连接线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public static List<Polyline> GetBlockLines(Polyline polyline, List<BlockReference> blocks)
        {
            List<Polyline> lines = new List<Polyline>();
            if (blocks.Count < 2)
            {
                return lines;
            }
            var xDir = (polyline.EndPoint - polyline.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });
            var blockPts = blocks.Select(x => x.Position.TransformBy(matrix)).OrderBy(x => x.X).ToList();

            List<Point3d> polyPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                polyPts.Add(polyline.GetPoint3dAt(i).TransformBy(matrix));
            }

            for (int i = 0; i < blockPts.Count - 1; i++)
            {
                Polyline resLine = new Polyline();
                var currentPt = polyline.GetClosestPointTo(blockPts[i].TransformBy(matrix.Inverse()), false);
                var nextPt = polyline.GetClosestPointTo(blockPts[i + 1].TransformBy(matrix.Inverse()), false);
                var matchPolyPts = polyPts.Where(x => blockPts[i].X < x.X && x.X < blockPts[i + 1].X).ToList();
                resLine.AddVertexAt(0, currentPt.ToPoint2D(), 0, 0, 0);
                for (int j = 0; j < matchPolyPts.Count; j++)
                {
                    resLine.AddVertexAt(j + 1, matchPolyPts[j].TransformBy(matrix.Inverse()).ToPoint2D(), 0, 0, 0);
                }
                resLine.AddVertexAt(matchPolyPts.Count + 1, nextPt.ToPoint2D(), 0, 0, 0);
                lines.Add(resLine);
            }

            return lines;
        }
    }
}
