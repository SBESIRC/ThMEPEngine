using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Service;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public class ACModel
    {
        public ACModel(BlockReference block)
        {
            double width = 4 * ThElectricalUIService.Instance.Parameter.scale;
            double height = 5 * ThElectricalUIService.Instance.Parameter.scale;
            position = new Point3d(block.Position.X, block.Position.Y, 0);
            layoutDir = -block.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
            blockModel = block;
            ConnectPts = new List<Point3d>();
            var otherDir = Vector3d.ZAxis.CrossProduct(layoutDir);
            ConnectPts.Add(position + layoutDir * (height / 2));
            ConnectPts.Add(position - layoutDir * (height / 2));
            ConnectPts.Add(position + otherDir * (width / 2));
            ConnectPts.Add(position - otherDir * (width / 2));
        }

        /// <summary>
        /// 原始块
        /// </summary>
        public BlockReference blockModel { get; set; }

        /// <summary>
        /// 块基点
        /// </summary>
        public Point3d position { get; set; }

        /// <summary>
        /// 块布置方向
        /// </summary>
        public Vector3d layoutDir { get; set; }

        /// <summary>
        /// 可连接点位
        /// </summary>
        public List<Point3d> ConnectPts { get; set; }

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double Scale { get; set; }
    }

    /// <summary>
    /// 电锁按钮
    /// </summary>
    public class ACButtun : ACModel
    {
        public ACButtun(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 读卡器
    /// </summary>
    public class ACCardReader : ACModel
    {
        public ACCardReader(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 电锁
    /// </summary>
    public class ACElectricLock : ACModel
    {
        public ACElectricLock(BlockReference block) : base(block)
        {
            double width = Math.Sqrt(2 * Math.Pow(1.5 * ThElectricalUIService.Instance.Parameter.scale, 2));
            ConnectPts = new List<Point3d>();
            var otherDir = Vector3d.ZAxis.CrossProduct(layoutDir);
            ConnectPts.Add(position + layoutDir * width);
            ConnectPts.Add(position - layoutDir * width);
            ConnectPts.Add(position + otherDir * width);
            ConnectPts.Add(position - otherDir * width);
        }
    }

    /// <summary>
    /// 出入口对讲门口机
    /// </summary>
    public class ACIntercom : ACModel
    {
        public ACIntercom(BlockReference block) : base(block)
        {
            double width = 4 * ThElectricalUIService.Instance.Parameter.scale;
            double height = 8 * ThElectricalUIService.Instance.Parameter.scale;
            ConnectPts = new List<Point3d>();
            var otherDir = Vector3d.ZAxis.CrossProduct(layoutDir);
            ConnectPts.Add(position + layoutDir * (height / 2));
            ConnectPts.Add(position - layoutDir * (height / 2));
            ConnectPts.Add(position + otherDir * (width / 2));
            ConnectPts.Add(position - otherDir * (width / 2));
        }
    }
}
