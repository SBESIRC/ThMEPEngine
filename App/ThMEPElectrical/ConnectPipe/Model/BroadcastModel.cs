using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.ConnectPipe.Model
{
    public class BroadcastModel
    {
        readonly Point3d topBasicPt = new Point3d(0, -1.5, 0);
        readonly Point3d leftBasicPt = new Point3d(2, 0, 0);
        readonly Point3d rightBasicPt = new Point3d(-2, 0, 0);

        /// <summary>
        /// 广播
        /// </summary>
        public BlockReference Broadcast;

        /// <summary>
        /// 广播基点
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 广播方向
        /// </summary>
        public Vector3d BroadcastDirection { get; set; }

        /// <summary>
        /// 广播图块顶部连接点
        /// </summary>
        public Point3d TopConnectPt { get; set; }

        /// <summary>
        /// 广播图块左连接点
        /// </summary>
        public Point3d LeftConnectPt { get; set; }

        /// <summary>
        /// 广播图块右连接点
        /// </summary>
        public Point3d RightConnectPt { get; set; }

        /// <summary>
        /// 连接小支管
        /// </summary>
        public Dictionary<Point3d, List<Polyline>> ConnectInfo { get; set; }

        public BroadcastModel(BlockReference block)
        {
            Broadcast = block;
            Position = block.Position;
            BroadcastDirection = -block.BlockTransform.CoordinateSystem3d.Xaxis;

            //计算连接点
            var matrix = block.BlockTransform;
            TopConnectPt = topBasicPt.TransformBy(matrix);
            LeftConnectPt = leftBasicPt.TransformBy(matrix);
            RightConnectPt = rightBasicPt.TransformBy(matrix);

            //初始化连接信息
            ConnectInfo = new Dictionary<Point3d, List<Polyline>>();
            ConnectInfo.Add(TopConnectPt, new List<Polyline>());
            ConnectInfo.Add(LeftConnectPt, new List<Polyline>());
            ConnectInfo.Add(RightConnectPt, new List<Polyline>());
        }
    }
}
