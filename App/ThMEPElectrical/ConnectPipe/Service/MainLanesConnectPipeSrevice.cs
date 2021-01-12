using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.ConnectPipe.Model;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class MainLanesConnectPipeSrevice
    {
        readonly double tol = 10;   //误差10以内我们认为车道线穿过广播连接点
        readonly double rotateAngle = 30 * Math.PI / 180;

        public void ConnectPipe(Dictionary<Polyline, List<BroadcastModel>> linesDic)
        {
            foreach (var dic in linesDic)
            {
                var poly = dic.Key;
                var broadcasts = dic.Value;
                //获取移动信息
                GetLaneMoveInfo(poly, broadcasts, out double distance, out Vector3d dir);

                //移动车道线
                var newPoly = movePolyline(poly, dir, distance);

                //创建连管
                CreatePipeLines(broadcasts, newPoly);
            }
        }

        /// <summary>
        /// 创建连管线
        /// </summary>
        /// <param name="broadcasts"></param>
        /// <param name="line"></param>
        private void CreatePipeLines(List<BroadcastModel> broadcasts, Polyline line)
        {
            var xDir = (line.EndPoint - line.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            var broadcastModel = broadcasts.SelectMany(x => 
                    new List<KeyValuePair<Point3d, BroadcastModel>>() {
                    new KeyValuePair<Point3d, BroadcastModel>(x.LeftConnectPt, x ),
                    new KeyValuePair<Point3d, BroadcastModel>(x.RightConnectPt, x ),})
                .OrderBy(x => x.Key.TransformBy(matrix).X)
                .ToList();
            broadcastModel.RemoveAt(0);                             //去掉第一个点
            broadcastModel.RemoveAt(broadcastModel.Count - 1);      //去掉最后一个点
            var connectLines = CreateLines(broadcastModel, line);

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in connectLines)
                {
                    acdb.ModelSpace.Add(item);
                }
            }
        }

        /// <summary>
        /// 创建连管线
        /// </summary>
        /// <param name="ptInfo"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> CreateLines(List<KeyValuePair<Point3d, BroadcastModel>> ptInfo, Polyline polyline)
        {
            List<Polyline> connectLines = new List<Polyline>();
            for (int i = 0; i < ptInfo.Count; i = i + 2)
            {
                Polyline poly = new Polyline();
                var pts = CheckPtOnPolyline(ptInfo[i], polyline, true);
                pts.AddRange(CheckPtOnPolyline(ptInfo[i + 1], polyline, false));
                for (int j = 0; j < pts.Count; j++)
                {
                    poly.AddVertexAt(0, pts[j].ToPoint2D(), 0, 0, 0);
                }
                connectLines.Add(poly);
            }

            return connectLines;
        }

        /// <summary>
        /// 计算是否需要伸出支管链接
        /// </summary>
        /// <param name="ptInfo"></param>
        /// <param name="polyline"></param>
        /// <param name="isFirst"></param>
        /// <returns></returns>
        private List<Point3d> CheckPtOnPolyline(KeyValuePair<Point3d, BroadcastModel> ptInfo, Polyline polyline, bool isFirst)
        {
            List<Point3d> pts = new List<Point3d>();
            Point3d closetPt = polyline.GetClosestPointTo(ptInfo.Key, true);
            if (ptInfo.Key.DistanceTo(closetPt) < tol)
            {
                pts.Add(ptInfo.Key);
            }
            else
            {
                var dir = (ptInfo.Key - ptInfo.Value.Position).GetNormal();
                var compariDir = (closetPt - ptInfo.Key).GetNormal();
                var rayDir = dir.RotateBy(rotateAngle, Vector3d.ZAxis);
                if (rayDir.DotProduct(compariDir) < 0)
                {
                    rayDir = dir.RotateBy(-rotateAngle, Vector3d.ZAxis);
                }

                Ray ray = new Ray();
                ray.BasePoint = ptInfo.Key;
                ray.UnitDir = rayDir;
                Point3dCollection ptCollection = new Point3dCollection();
                ray.IntersectWith(polyline, Intersect.OnBothOperands, ptCollection, (IntPtr)0, (IntPtr)0);
                if (isFirst)
                {
                    pts.Add(ptInfo.Key);
                    if (ptCollection.Count > 0)
                    {
                        pts.Add(ptCollection[0]);
                    }
                }
                else
                {
                    if (ptCollection.Count > 0)
                    {
                        pts.Add(ptCollection[0]);
                    }
                    pts.Add(ptInfo.Key);
                }
            }

            return pts;
        }

        /// <summary>
        /// 移动polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="dir"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Polyline movePolyline(Polyline polyline, Vector3d dir, double distance)
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
        /// 获取移动信息
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcasts"></param>
        /// <param name="distance"></param>
        /// <param name="dir"></param>
        private void GetLaneMoveInfo(Polyline polyline, List<BroadcastModel> broadcasts, out double distance, out Vector3d dir)
        {
            List<KeyValuePair<Point3d, Point3d>> ptInfo = new List<KeyValuePair<Point3d, Point3d>>();
            foreach (var broadcast in broadcasts)
            {
                var closetPt = polyline.GetClosestPointTo(broadcast.Position, false);
                ptInfo.Add(new KeyValuePair<Point3d, Point3d>(broadcast.Position, closetPt));
            }
            
            distance = ptInfo.Select(x => x.Key.DistanceTo(x.Value))
                .OrderBy(x => x).GroupBy(x => Math.Floor(x / 10))
                .OrderByDescending(x => x.Count())
                .First()
                .Key * 10;
            dir = broadcasts.Select(x => -x.BroadcastDirection).First();
        }
    }
}
