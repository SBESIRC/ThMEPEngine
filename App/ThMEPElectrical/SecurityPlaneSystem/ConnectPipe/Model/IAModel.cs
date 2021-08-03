using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public class IAModel
    {
        public IAModel(BlockReference block)
        {
            position = block.Position;
            layoutDir = -block.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
            blockModel = block;
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
    /// 控制器
    /// </summary>
    public class IAControllerModel : IAModel
    {
        double width = 300;
        double height = 500;
        public IAControllerModel(BlockReference block) : base(block) {
            ConnectPts = new List<Point3d>();
            var otherDir = Vector3d.ZAxis.CrossProduct(layoutDir);
            ConnectPts.Add(position + layoutDir * (height / 2));
            ConnectPts.Add(position - layoutDir * (height / 2));
            ConnectPts.Add(position + otherDir * (width / 2));
            ConnectPts.Add(position - otherDir * (width / 2));
        }
    }

    /// <summary>
    /// 声光报警按钮
    /// </summary>
    public class IASoundLightAlarm : IAModel
    {
        public IASoundLightAlarm(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 残卫报警按钮
    /// </summary>
    public class IADisabledAlarmButtun : IAModel
    {
        public IADisabledAlarmButtun(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 紧急报警按钮
    /// </summary>
    public class IAEmergencyAlarmButton : IAModel
    {
        public IAEmergencyAlarmButton(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 红外壁装探测器
    /// </summary>
    public class IAInfraredWallDetectorModel : IAModel
    {
        public IAInfraredWallDetectorModel(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 双鉴探测器
    /// </summary>
    public class IADoubleDetectorModel : IAModel
    {
        public IADoubleDetectorModel(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 红外吊装探测器
    /// </summary>
    public class IAInfraredHositingDetectorModel : IAModel
    {
        public IAInfraredHositingDetectorModel(BlockReference block) : base(block) {
            
        }
    }
}
