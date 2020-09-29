using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThMEPWSS.Model
{
    public class SprayDataService
    {
        /// <summary>
        /// 创建喷淋对象
        /// </summary>
        /// <param name="layoutP"></param>
        /// <param name="vDir"></param>
        /// <param name="tDir"></param>
        public static List<List<SprayLayoutData>> CreateSprayModels(List<List<Point3d>> layoutP, Vector3d vDir, Vector3d tDir, double sideLength)
        {
            // 计算保护半径
            // 暂时只支持矩形保护半径
            var curve = new Polyline()
            {
                Closed = true,
            };
            double distance = Math.Sqrt(2) * (sideLength / 2.0);
            var vertices = new Point3dCollection()
            {
                Point3d.Origin + distance * (vDir + tDir).GetNormal(),
                Point3d.Origin + distance * (vDir - tDir).GetNormal(),
                Point3d.Origin - distance * (vDir + tDir).GetNormal(),
                Point3d.Origin - distance * (vDir - tDir).GetNormal()
            };
            CreatePolyline(curve, vertices);
            
            var sprays = new List<List<SprayLayoutData>>();
            foreach (var points in layoutP)
            {
                var sprayList = new List<SprayLayoutData>();
                foreach (var point in points)
                {
                    var offset = Matrix3d.Displacement(point.GetAsVector());
                    var spray = SprayLayoutData.Create(point, curve.GetTransformedCopy(offset) as Curve);
                    spray.mainDir = vDir;
                    spray.otherDir = tDir;
                    sprayList.Add(spray);
                }
                sprays.Add(sprayList);
            }

            return sprays;
        }

        /// <summary>
        /// 创建喷淋对象
        /// </summary>
        /// <param name="layoutP"></param>
        /// <returns></returns>
        public static List<SprayLayoutData> CreateSprayModels(List<Point3d> layoutP)
        {
            var sprays = new List<SprayLayoutData>();
            foreach (var point in layoutP)
            {
                var offset = Matrix3d.Displacement(point.GetAsVector());
                var spray = SprayLayoutData.Create(point, null);
                sprays.Add(spray);
            }

            return sprays;
        }

        /// <summary>
        /// 创建喷淋对象
        /// </summary>
        /// <param name="point"></param>
        /// <param name="vLine"></param>
        /// <param name="tLine"></param>
        /// <param name="vDir"></param>
        /// <param name="tDir"></param>
        /// <param name="sideLength"></param>
        /// <returns></returns>
        public static SprayLayoutData CreateSprayModelsByRay(Point3d point, Vector3d vDir, Vector3d tDir, double sideLength)
        {
            // 计算保护半径
            // 暂时只支持矩形保护半径
            var curve = new Polyline()
            {
                Closed = true,
            };
            double distance = Math.Sqrt(2) * (sideLength / 2.0);
            var vertices = new Point3dCollection()
            {
                Point3d.Origin + distance * (vDir + tDir).GetNormal(),
                Point3d.Origin + distance * (vDir - tDir).GetNormal(),
                Point3d.Origin - distance * (vDir + tDir).GetNormal(),
                Point3d.Origin - distance * (vDir - tDir).GetNormal()
            };
            CreatePolyline(curve, vertices);

            var offset = Matrix3d.Displacement(point.GetAsVector());
            var spray = SprayLayoutData.Create(point, curve.GetTransformedCopy(offset) as Curve);
            spray.mainDir = vDir;
            spray.otherDir = tDir;

            return spray;
        }

        /// <summary>
        /// 通过三维点集合创建多段线
        /// </summary>
        /// <param name="pline">多段线对象</param>
        /// <param name="pts">多段线的顶点</param>
        public static void CreatePolyline(Polyline pline, Point3dCollection pts)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                //添加多段线的顶点
                pline.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
        }
    }
}
