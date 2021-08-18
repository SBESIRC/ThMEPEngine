using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model
{
    public class VMModel : BlockModel
    {
        public VMModel(BlockReference block) : base(block)
        {

        }

        /// <summary>
        /// 块布置方向
        /// </summary>
        public Vector3d layoutDir { get; set; }

        /// <summary>
        /// 可连接点位
        /// </summary>
        public List<Point3d> ConnectPts { get; set; }
    }

    /// <summary>
    /// 枪式摄像机
    /// </summary>
    public class VMGunCamera : VMModel
    {
        public VMGunCamera(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 云台摄像机
    /// </summary>
    public class VMPantiltCamera : VMModel
    {
        public VMPantiltCamera(BlockReference block) : base(block) { }
    }

    /// <summary>
    /// 人脸识别摄像机
    /// </summary>
    public class VMFaceCamera : VMModel
    {
        public VMFaceCamera(BlockReference block) : base(block) { }
    }
}
